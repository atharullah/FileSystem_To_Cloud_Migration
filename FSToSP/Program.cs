using Microsoft.SharePoint.Client;
using MigrationHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace FSToSP
{
    class Program
    {
        static DataTable TblPermMapping = new DataTable();
        static readonly string DomainName = Helper.GetParseConfigValues(Constants.AppKeyDomainName);
        static string SQLiteConStr;
        static readonly string PageSize = Helper.GetParseConfigValues(Constants.AppKeyPageSize);
        static readonly List<string> CompletedItemList = new List<string>();
        static readonly string OriginalDbFilePath = Helper.GetParseConfigValues(Constants.AppKeySQLiteDBFilePath);
        static readonly string ReportDBPath = Helper.GetParseConfigValues(Constants.AppKeyReportDBFilePath);
        static string ToolMode = Helper.GetParseConfigValues(Constants.AppKeyToolMode);
        static Dictionary<string, User> UserList = new Dictionary<string, User>();
        static void Main(string[] Args)
        {
            try
            {
                if (Args.Length > 0)
                {
                    string[] ArgumentsArr = Args[0].Replace(GeneralValues.Percent20Val, " ").Split(',');
                    string SourceFolderURL = ArgumentsArr[0];
                    string WebURL = ArgumentsArr[1];
                    string DocLibName = ArgumentsArr[2];
                    string DocLibRelativeURL = ArgumentsArr[3];
                    SQLiteConStr = Helper.GetConString(ArgumentsArr[4]);
                    string BlockFileType = Helper.GetParseConfigValues(Constants.AppKeyBlockFileType);
                    // string ToolMode = Helper.GetParseConfigValues(Constants.AppKeyToolMode);

                    switch (ToolMode)
                    {
                        case "0": //Only File System Details
                            FileSystemDetails(SourceFolderURL, ReportDBPath);
                            break;
                        case "1": //File System and SharePoint Migration
                            FileSystemDetails(SourceFolderURL, ReportDBPath);
                            FSToSPMigration(WebURL, DocLibName, SourceFolderURL, DocLibRelativeURL, BlockFileType);
                            break;
                        case "2": //Only SharePoint Migration (Need manual update to DBFilePath Column in FolderMapping Table)
                            FSToSPMigration(WebURL, DocLibName, SourceFolderURL, DocLibRelativeURL, BlockFileType);
                            break;
                        case "3":
                            FSToSPMigration(WebURL, DocLibName, SourceFolderURL, DocLibRelativeURL, BlockFileType);
                            break;
                    }
                    Console.Read();
                }
                else
                    StartAllProcess();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in application - " + ex.ToString());
                Log.WriteToEventViewer(ex);
                Console.WriteLine("\nPress Any Key To Close The Application");
                Console.ReadLine();
            }
        }

        #region Start All Processes

        static void StartAllProcess()
        {
            using (SQLiteConnection conn = new SQLiteConnection(Helper.GetConString(OriginalDbFilePath)))
            {
                conn.Open();
                using (SQLiteCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandTimeout = 0; //0 means infinite, default 30 second => it indicates put connection open while performin long operation
                    cmd.CommandType = CommandType.Text;
                    StringBuilder SelectColumns = new StringBuilder();
                    SelectColumns.Append(nameof(FoldersMapping.DestDocLibName) + ",");
                    SelectColumns.Append(nameof(FoldersMapping.DestDocLibRelUrl) + ",");
                    SelectColumns.Append(nameof(FoldersMapping.DestWebUrl) + ",");
                    SelectColumns.Append(nameof(FoldersMapping.ID) + ",");
                    SelectColumns.Append(nameof(FoldersMapping.SourceUrl) + ",");
                    SelectColumns.Append(nameof(FoldersMapping.DBFilePath));
                    cmd.CommandText = Helper.GetSelectQuery(nameof(FoldersMapping), SelectColumns.ToString());

                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("Starting of Process Instancess Started");
                        string SourceDBFilePath = conn.FileName;
                        string DBDir = Path.GetDirectoryName(SourceDBFilePath);
                        while (reader.Read())
                        {
                            string DocLibName = Convert.ToString(reader[nameof(FoldersMapping.DestDocLibName)]);
                            string DocLibRelUrl = Convert.ToString(reader[nameof(FoldersMapping.DestDocLibRelUrl)]);
                            string WebUrl = Convert.ToString(reader[nameof(FoldersMapping.DestWebUrl)]);
                            string SourceUrl = Convert.ToString(reader[nameof(FoldersMapping.SourceUrl)]);
                            string MapID = Convert.ToString(reader[nameof(FoldersMapping.ID)]);
                            string DestDBFilePath = Convert.ToString(reader[nameof(FoldersMapping.DBFilePath)]);
                            if (string.IsNullOrEmpty(DestDBFilePath))
                            {
                                string SourceDirName = Path.GetFileName(SourceUrl);
                                DestDBFilePath = DBDir + "\\" + SourceDirName + "-" + DocLibName + "-" + MapID + ".db";
                                System.IO.File.Copy(SourceDBFilePath, DestDBFilePath, true);
                            }

                            UpdateDBFilePaths(MapID, DestDBFilePath);

                            StringBuilder arguments = new StringBuilder();
                            arguments.Append(SourceUrl + ",");
                            arguments.Append(WebUrl + ",");
                            arguments.Append(DocLibName + ",");
                            arguments.Append(DocLibRelUrl + ",");
                            arguments.Append(DestDBFilePath);
                            ProcessStartInfo psinfo = new ProcessStartInfo();
                            psinfo.Arguments = arguments.ToString().Replace(" ", GeneralValues.Percent20Val);
                            psinfo.FileName = Process.GetCurrentProcess().ProcessName;
                            psinfo.ErrorDialog = true;
                            psinfo.WorkingDirectory = Environment.CurrentDirectory;
                            Process MigrationProcess = new Process();
                            MigrationProcess.StartInfo = psinfo;
                            MigrationProcess.Start();
                        }
                    }
                }
            }
        }

        #endregion

        #region File System To SQLite Methods

        static void FileSystemDetails(string SourceFolderUrl, string ReportDBPath)
        {
            Console.WriteLine("Hello");
            Console.WriteLine("Following Are the Settings You Mentioned");
            Console.WriteLine("Folder Path\n" + SourceFolderUrl);

            if (Directory.Exists(SourceFolderUrl))
            {
                using (SQLiteConnection con = new SQLiteConnection(SQLiteConStr))
                {
                    Stopwatch watch = Stopwatch.StartNew();
                    con.Open();
                    using (SQLiteCommand cmd = con.CreateCommand())
                    {
                        using (SQLiteTransaction transaction = con.BeginTransaction())
                        {
                            try
                            {
                                cmd.CommandTimeout = 0;
                                cmd.CommandType = CommandType.Text;
                                Console.WriteLine("Information Collection Started");
                                FileSystemSQLiteLog(SourceFolderUrl, 0, 0, cmd);
                                Console.WriteLine("Information Collection Completed");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception in application\n" + ex.ToString());
                                Log.WriteToEventViewer(ex);
                            }
                            finally
                            {
                                transaction.Commit();
                                watch.Stop();
                                Console.WriteLine("Time To Complete File System Info - " + watch.ElapsedMilliseconds);

                                StringBuilder InsertQuery = new StringBuilder();
                                InsertQuery.Append("INSERT INTO " + nameof(TimeLog));
                                InsertQuery.Append("(");
                                InsertQuery.Append(nameof(TimeLog.SourceUrl) + ",");
                                InsertQuery.Append(nameof(TimeLog.Time));
                                InsertQuery.Append(")");
                                InsertQuery.Append("Values(");
                                InsertQuery.Append("\"" + SourceFolderUrl + "\"" + ",");
                                InsertQuery.Append(watch.ElapsedMilliseconds.ToString());
                                InsertQuery.Append(")");
                                cmd.CommandText = InsertQuery.ToString();
                                cmd.ExecuteNonQuery();

                                //AddToReportDB(InsertQuery.ToString());
                            }
                        }
                    }
                }
            }
            else
                Console.WriteLine("\nFolder Path Does Not Exist");
        }
        static void FileSystemSQLiteLog(string FolderPath, long ParentFolderID, int FolderLevel, SQLiteCommand cmd)
        {
            if (Directory.Exists(FolderPath))
            {
                DirectoryInfo dirInfo = new DirectoryInfo(FolderPath);
                long FolderResult = AddFolderToLiteDB(dirInfo, cmd, ParentFolderID, FolderLevel);
                if (FolderResult == -1)
                {
                    //no access folder work
                }
                else
                {
                    ParentFolderID = FolderResult;
                    AuthorizationRuleCollection DirAllRules = dirInfo.GetAccessControl().GetAccessRules(true, true, typeof(NTAccount));
                    IEnumerable<FileSystemAccessRule> DirPermRules = DirAllRules.Cast<FileSystemAccessRule>();
                    AddPermDetailSQLite(DirPermRules, FolderPath, false, cmd);
                    Console.WriteLine("Folder Info Successfully Completed");

                    var VisibleFiles = dirInfo.EnumerateFiles().Where(x => !(x.Attributes.HasFlag(FileAttributes.Hidden)));//.Where(x => !(x.Attributes.HasFlag(FileAttributes.System)));
                    foreach (FileInfo file in VisibleFiles)
                    {
                        string filePath = file.FullName;
                        long InsertedFileID = AddFileDToLiteDB(file, cmd, ParentFolderID);

                        if (InsertedFileID == -1)
                        {
                            //no access file work
                        }
                        else
                        {
                            FileSecurity fileSecurity = file.GetAccessControl();
                            AuthorizationRuleCollection FileAllRules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));
                            IEnumerable<FileSystemAccessRule> FilePermRules = FileAllRules.Cast<FileSystemAccessRule>();
                            AddPermDetailSQLite(FilePermRules, filePath, true, cmd);
                            Console.WriteLine("File Info Successfully Completed");
                        }
                    }
                    var VisibleDirectories = dirInfo.EnumerateDirectories().Where(x => !(x.Attributes.HasFlag(FileAttributes.Hidden)));//.Where(x=>!(x.Attributes.HasFlag(FileAttributes.System)));
                    foreach (DirectoryInfo dir in VisibleDirectories)
                    {
                        FolderLevel++;
                        FileSystemSQLiteLog(dir.FullName, ParentFolderID, FolderLevel, cmd);
                        FolderLevel--;
                    }
                }
            }
        }
        static long AddFolderToLiteDB(DirectoryInfo DirInfo, SQLiteCommand cmd, long ParentFolderID, int FolderLevel)
        {
            Console.WriteLine("Processing Folder\n" + DirInfo.FullName);
            string DirName = DirInfo.Name;
            string DirPath = DirInfo.FullName;
            Dictionary<string, string> InsertQuery = new Dictionary<string, string>();
            InsertQuery.Add(nameof(FolderDetails.CreatedDate), "\"" + DateTime.Now.ToString() + "\"");
            InsertQuery.Add(nameof(FolderDetails.FolderName), "\"" + DirName + "\"");
            InsertQuery.Add(nameof(FolderDetails.FolderPath), "\"" + DirPath + "\"");
            InsertQuery.Add(nameof(FolderDetails.Modifieddate), "\"" + DateTime.Now.ToString() + "\"");
            InsertQuery.Add(nameof(FolderDetails.ParentFolderID), ParentFolderID.ToString());
            long result;
            try
            {
                StringBuilder InvalidMsg = new StringBuilder();
                if (Helper.IsNameInvalid(DirName, InvalidMsg))
                {
                    throw new Exception(InvalidMsg.ToString());
                }
                if (DirPath.Length > 245)
                {
                    throw new Exception("Path too long for folder");
                }
                var DirSecurity = DirInfo.GetAccessControl();
                string folderOwnerName = "";
                try
                {
                    folderOwnerName = DirSecurity.GetOwner(typeof(NTAccount)).Value;
                }
                catch (IdentityNotMappedException)
                {
                    folderOwnerName = DirSecurity.GetOwner(typeof(SecurityIdentifier)).Value;
                }
                InsertQuery.Add(nameof(FolderDetails.FilesCount), DirInfo.EnumerateFiles().Count().ToString());
                InsertQuery.Add(nameof(FolderDetails.FolderCreatedDate), "\"" + DirInfo.CreationTime.ToString() + "\"");
                InsertQuery.Add(nameof(FolderDetails.FolderCreator), "\"" + folderOwnerName + "\"");
                InsertQuery.Add(nameof(FolderDetails.FolderLevel), FolderLevel.ToString());
                InsertQuery.Add(nameof(FolderDetails.FolderModifiedDate), "\"" + DirInfo.LastWriteTime.ToString() + "\"");
                InsertQuery.Add(nameof(FolderDetails.FolderModifier), "\"" + folderOwnerName + "\"");
                InsertQuery.Add(nameof(FolderDetails.Notes), "\"\"");
                InsertQuery.Add(nameof(FolderDetails.SubFolderCount), DirInfo.EnumerateDirectories().Count().ToString());

                string SQLInsertQr = Helper.GetInsertQuery(nameof(FolderDetails), InsertQuery);
                Console.WriteLine("Insert Query\n" + SQLInsertQr);
                cmd.CommandText = SQLInsertQr;
                cmd.ExecuteNonQuery();
                result = cmd.Connection.LastInsertRowId;
            }
            catch (Exception ex)
            {
                InsertQuery.Add(nameof(FolderDetails.Notes), "\"" + ex.ToString() + "\"");
                string SQLInsertQr = Helper.GetInsertQuery(nameof(FolderDetails), InsertQuery);
                Console.WriteLine("Insert Query\n" + SQLInsertQr);
                cmd.CommandText = SQLInsertQr;
                cmd.ExecuteNonQuery();
                result = -1;
            }
            return result;
        }
        static void AddPermDetailSQLite(IEnumerable<FileSystemAccessRule> PermRules, string path, bool IsFile, SQLiteCommand cmd)
        {
            Console.WriteLine("Processing Permissions");
            foreach (FileSystemAccessRule rule in PermRules)
            {
                Dictionary<string, string> InsertDict = new Dictionary<string, string>();

                InsertDict.Add(nameof(PermissionDetails.CreatedDate), "\"" + DateTime.Now.ToString() + "\"");
                InsertDict.Add(nameof(PermissionDetails.IsAllow), (rule.AccessControlType == AccessControlType.Allow).ToString());
                InsertDict.Add(nameof(PermissionDetails.IsFile), IsFile.ToString());
                InsertDict.Add(nameof(PermissionDetails.IsInherited), rule.IsInherited.ToString());
                InsertDict.Add(nameof(PermissionDetails.ModifiedDate), "\"" + DateTime.Now.ToString() + "\"");
                InsertDict.Add(nameof(PermissionDetails.Notes), "\"\"");
                InsertDict.Add(nameof(PermissionDetails.ObjectPath), "\"" + path + "\"");
                InsertDict.Add(nameof(PermissionDetails.Permissions), "\"" + rule.FileSystemRights.ToString() + "\"");
                InsertDict.Add(nameof(PermissionDetails.UserName), "\"" + rule.IdentityReference.Value + "\"");
                InsertDict.Add(nameof(PermissionDetails.IsMoveToSP), 0.ToString());
                cmd.CommandText = Helper.GetInsertQuery(nameof(PermissionDetails), InsertDict);
                cmd.ExecuteNonQuery();
            }
        }
        static long AddFileDToLiteDB(FileInfo file, SQLiteCommand cmd, long ParentFolderID)
        {
            Console.WriteLine("Processing File\n" + file.FullName);
            string FileName = file.Name;
            string filepath = file.FullName;
            Dictionary<string, string> InsertDict = new Dictionary<string, string>();
            InsertDict.Add(nameof(FilesDetails.CreatedDate), "\"" + DateTime.Now.ToString() + "\"");
            InsertDict.Add(nameof(FilesDetails.FileName), "\"" + file.Name + "\"");
            InsertDict.Add(nameof(FilesDetails.FilePath), "\"" + file.FullName + "\"");
            InsertDict.Add(nameof(FilesDetails.ModifiedDate), "\"" + DateTime.Now.ToString() + "\"");
            InsertDict.Add(nameof(FilesDetails.ParentFolderID), ParentFolderID.ToString());
            long result;
            try
            {
                StringBuilder FileResultMsg = new StringBuilder();
                bool IsFileInvalid = Helper.IsFileInvalid(FileName, Constants.AppKeyBlockFileType, FileResultMsg);
                if (IsFileInvalid)
                {
                    throw new Exception(FileResultMsg.ToString());
                }
                if (filepath.Length > 255)
                {
                    throw new Exception("Path too long for files");
                }
                var fileSecurity = file.GetAccessControl();
                string FileOwnerName = "";
                try
                {
                    FileOwnerName = fileSecurity.GetOwner(typeof(NTAccount)).Value;
                }
                catch (IdentityNotMappedException)
                {
                    FileOwnerName = fileSecurity.GetOwner(typeof(SecurityIdentifier)).Value;
                }
                InsertDict.Add(nameof(FilesDetails.FileCreator), "\"" + FileOwnerName + "\"");
                InsertDict.Add(nameof(FilesDetails.FileModifier), "\"" + FileOwnerName + "\"");
                InsertDict.Add(nameof(FilesDetails.FileCreatedDate), "\"" + file.CreationTime.ToString() + "\"");
                InsertDict.Add(nameof(FilesDetails.FileModifiedDate), "\"" + file.LastWriteTime.ToString() + "\"");
                InsertDict.Add(nameof(FilesDetails.FileSizeKB), (file.Length / 1024).ToString());
                InsertDict.Add(nameof(FilesDetails.Notes), "\"\"");
                string SQLInsertQr = Helper.GetInsertQuery(nameof(FilesDetails), InsertDict);
                Console.WriteLine("Insert Query\n" + SQLInsertQr);
                cmd.CommandText = SQLInsertQr;
                cmd.ExecuteNonQuery();
                result = cmd.Connection.LastInsertRowId;
            }
            catch (Exception ex)
            {
                InsertDict.Add(nameof(FilesDetails.Notes), "\"" + ex.ToString() + "\"");
                string SQLInsertQr = Helper.GetInsertQuery(nameof(FilesDetails), InsertDict);
                Console.WriteLine("Insert Query\n" + SQLInsertQr);
                cmd.CommandText = SQLInsertQr;
                cmd.ExecuteNonQuery();
                result = -1;
            }
            return result;
        }

        #endregion

        #region File System To SharePoint Migration Method

        public static void FSToSPMigration(string WebURL, string DocLibName, string SourceFolderURL, string DocLibRelativeURL, string BlockFileType)
        {
            Console.WriteLine("Hello");
            Console.WriteLine("Following Are the Settings You Mentioned");

            Console.WriteLine("SharePoint Web URL\n" + WebURL);
            Console.WriteLine("SharePoint Library Title\n" + DocLibName);
            Console.WriteLine("File System Path\n" + SourceFolderURL);
            Console.WriteLine("SharePoint Library Relative Folder Path\n" + DocLibRelativeURL);
            Console.WriteLine("Block File Types\n" + BlockFileType);
            if (Directory.Exists(SourceFolderURL))
            {
                Stopwatch watch = Stopwatch.StartNew();
                using (SQLiteConnection conn = new SQLiteConnection(SQLiteConStr))
                {
                    conn.Open();
                    using (SQLiteCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandTimeout = 0; //0 means infinite, default 30 second => it indicates put connection open while performin long operation
                        cmd.CommandType = CommandType.Text;
                        using (var ctx = new ClientContext(WebURL))
                        {
                            string password = Helper.GetParseConfigValues(Constants.AppKeySPPwd);
                            SecureString pwd = new SecureString();
                            foreach (char ch in password)
                                pwd.AppendChar(ch);
                            ctx.Credentials = new NetworkCredential(Helper.GetParseConfigValues(Constants.AppKeySPUserName), pwd);
                            List MyDocs = ctx.Web.Lists.GetByTitle(DocLibName);

                            Folder DestSPFolder = null;
                            if (DocLibRelativeURL == "/")
                                DestSPFolder = MyDocs.RootFolder;
                            else
                                DestSPFolder = MyDocs.RootFolder.Folders.GetByUrl(DocLibRelativeURL);
                            ctx.Load(DestSPFolder, x => x.ServerRelativeUrl);
                            ctx.ExecuteQuery();
                            Console.WriteLine("Relative SP Folder - " + DestSPFolder.ServerRelativeUrl);
                            SetSQLitePermMappings();
                            try
                            {
                                if (ToolMode == "1" || ToolMode == "2")
                                {
                                    Console.WriteLine("Folder Migration Started");
                                    SPFolderLiteMigration(cmd, SourceFolderURL, DestSPFolder, ctx);
                                    Console.WriteLine("Folder Migration Completed");
                                    Console.WriteLine("File Migration Started");
                                    Console.WriteLine("Source Folder - " + SourceFolderURL);
                                    string PathToCompare = Path.GetFileName(SourceFolderURL);
                                    Console.WriteLine("Path To Compare - " + PathToCompare);
                                    DestSPFolder = DestSPFolder.Folders.GetByUrl(PathToCompare);
                                    ctx.Load(DestSPFolder, x => x.ServerRelativeUrl);
                                    ctx.ExecuteQuery();
                                    Console.WriteLine("In This Folder - " + DestSPFolder.ServerRelativeUrl);
                                    SPFilesLiteMigration(cmd, SourceFolderURL, DestSPFolder, ctx);
                                    Console.WriteLine("Files Migration Completed");
                                }
                                else if (ToolMode == "3")
                                {
                                    Console.WriteLine("Folder Permission Migration Started");
                                    SPFolderPermMigration(cmd, SourceFolderURL, DestSPFolder, ctx);
                                    Console.WriteLine("Folder Migration Completed");
                                    Console.WriteLine("File Permission Migration Started");
                                    Console.WriteLine("Source Folder - " + SourceFolderURL);
                                    string PathToCompare = Path.GetFileName(SourceFolderURL);
                                    Console.WriteLine("Path To Compare - " + PathToCompare);
                                    DestSPFolder = DestSPFolder.Folders.GetByUrl(PathToCompare);
                                    ctx.Load(DestSPFolder, x => x.ServerRelativeUrl);
                                    ctx.ExecuteQuery();
                                    Console.WriteLine("In This Folder - " + DestSPFolder.ServerRelativeUrl);
                                    SPFilesPermMigration(cmd, SourceFolderURL, DestSPFolder, ctx);
                                    Console.WriteLine("Files Migration Completed");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Exception in application - " + ex.ToString());
                                Log.WriteToEventViewer(ex);
                            }
                            finally
                            {
                                Console.WriteLine("Updating of Completed Item Started");
                                UpdateCompletedData(conn, cmd);
                                Console.WriteLine("Updating of item completed");

                                watch.Stop();
                                Console.WriteLine("Time to Migrate using SQlite - " + watch.ElapsedMilliseconds);

                                Dictionary<string, string> TimeLogDetails = new Dictionary<string, string>();
                                TimeLogDetails.Add(nameof(TimeLog.SourceUrl), "\"" + SourceFolderURL + "\"");
                                TimeLogDetails.Add(nameof(TimeLog.Time), watch.ElapsedMilliseconds.ToString());
                                string InsertQuery = Helper.GetInsertQuery(nameof(TimeLog), TimeLogDetails);
                                cmd.CommandText = InsertQuery;
                                cmd.ExecuteNonQuery();

                                //AddToReportDB(InsertQuery);
                            }
                        }
                    }
                }
            }
            else
                Console.WriteLine("\nFolder Path Does Not Exist");
        }
        static void SPFolderLiteMigration(SQLiteCommand cmd, string SourceFolderPath, Folder ParentSPFolder, ClientContext ctx)
        {
            Console.WriteLine("Processing Folder\n" + SourceFolderPath);
            string SelectTQuery = Helper.GetSelectQuery(nameof(FolderDetails), "Count(" + nameof(FolderDetails.ID) + ")", GetFolderWhereQuery(SourceFolderPath).ToString());
            cmd.CommandText = SelectTQuery;
            cmd.CommandType = CommandType.Text;
            var result = cmd.ExecuteScalar();
            int RecordsCount = Convert.ToInt32(result);
            Console.WriteLine("Select Query\n" + SelectTQuery);
            Console.WriteLine("Folder Records count -" + RecordsCount);
            int CurrentCount = 0;
            while (RecordsCount != CurrentCount)
            {
                StringBuilder WhereQuery = GetFolderWhereQuery(SourceFolderPath);
                WhereQuery.Append(" ORDER By " + nameof(FolderDetails.FolderLevel));
                WhereQuery.Append(" Limit " + PageSize);
                WhereQuery.Append(" OFFSET " + CurrentCount);
                string SelectQuery = Helper.GetSelectQuery(nameof(FolderDetails), "*", WhereQuery.ToString());
                cmd.CommandText = SelectQuery;
                Console.WriteLine("Select Query - " + SelectQuery);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CurrentCount++;
                        string FolderPath = Convert.ToString(reader[nameof(FolderDetails.FolderPath)]);
                        string ID = Convert.ToString(reader[nameof(FolderDetails.ID)]);
                        Console.WriteLine("Processing Folder - " + FolderPath);
                        if (Directory.Exists(FolderPath))
                        {
                            string FolderName = Convert.ToString(reader[nameof(FolderDetails.FolderName)]);
                            string FolderLevel = Convert.ToString(reader[nameof(FolderDetails.FolderLevel)]);
                            string DestPath = "";
                            Folder DestFolder = null;
                            if (FolderLevel == "0")
                            {
                                DestPath = FolderName.Replace("\\", "").Replace(":", "");
                                ParentSPFolder = ParentSPFolder.Folders.AddWithOverwrite(DestPath, true);
                                DestFolder = ParentSPFolder;
                            }
                            else
                            {
                                DestPath = FolderPath.Replace(SourceFolderPath + "\\", "");
                                DestFolder = ParentSPFolder.Folders.AddWithOverwrite(DestPath, true);
                            }
                            Console.WriteLine("SharePoint Dest Path - " + DestPath);
                            ListItem DestItem = DestFolder.ListItemAllFields;

                            DestItem[CommonListColumns.Created] = Convert.ToDateTime(reader[nameof(FolderDetails.FolderCreatedDate)]);
                            DestItem[CommonListColumns.Modified] = Convert.ToDateTime(reader[nameof(FolderDetails.FolderModifiedDate)]);
                            string FolderCreator = Convert.ToString(reader[nameof(FolderDetails.FolderCreator)]);
                            User Creator = GetValidUser(FolderCreator, ctx.Web);
                            if (Creator != null)
                            {
                                DestItem[CommonListColumns.Editor] = Creator;
                                DestItem[CommonListColumns.Author] = Creator;
                            }
                            DestItem.Update();
                            ctx.ExecuteQuery();

                            CompletedItemList.Add(Constants.SPSuccessFD + ":" + ID);
                            Console.WriteLine("Folder Migrated To SharePoint");
                            SPPermLiteMigration(FolderPath, ctx, DestItem);
                            Console.WriteLine("Folder Permissions Migrated To SharePoint");
                        }
                        else
                        {
                            CompletedItemList.Add(Constants.SPFailFD + ":" + ID);
                            Console.WriteLine(string.Format(GeneralValues.MsgFolderNotExist, FolderPath));
                            Log.WriteToEventViewer(string.Format(GeneralValues.MsgFolderNotExist, FolderPath));
                        }
                    }
                }
            }
        }
        static void SPFilesLiteMigration(SQLiteCommand cmd, string SourceFolderPath, Folder ParentSPFolder, ClientContext ctx)
        {
            cmd.CommandText = Helper.GetSelectQuery(nameof(FilesDetails), "Count(" + nameof(FolderDetails.ID) + ")", GetFilesWhereQuery(SourceFolderPath).ToString());
            cmd.CommandType = CommandType.Text;
            var result = cmd.ExecuteScalar();
            int RecordsCount = Convert.ToInt32(result);
            int CurrentCount = 0;
            while (RecordsCount != CurrentCount)
            {
                StringBuilder WhereQuery = GetFilesWhereQuery(SourceFolderPath);
                WhereQuery.Append(" ORDER By " + nameof(FolderDetails.ID));
                WhereQuery.Append(" Limit " + PageSize);
                WhereQuery.Append(" OFFSET " + CurrentCount);
                string SelectQuery = Helper.GetSelectQuery(nameof(FilesDetails), "*", WhereQuery.ToString());
                cmd.CommandText = SelectQuery;
                Console.WriteLine("Select Query - " + SelectQuery);
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CurrentCount++;
                        string FilePath = Convert.ToString(reader[nameof(FilesDetails.FilePath)]);
                        Console.WriteLine("Processing File Path - " + FilePath);
                        if (System.IO.File.Exists(FilePath))
                        {
                            string FileName = Path.GetFileName(FilePath);
                            string FolderPath = Path.GetDirectoryName(FilePath);
                            Console.WriteLine("Folder Path for file - " + FolderPath);
                            string DestPath = ParentSPFolder.ServerRelativeUrl + "/";
                            if (SourceFolderPath == FolderPath)
                                DestPath += FileName;
                            else
                                DestPath += FolderPath.Replace(SourceFolderPath + "\\", "").Replace("\\", "/") + "/" + FileName;
                            Console.WriteLine("Dest Path - " + DestPath);
                            FileCreationInformation fileCreationInfo = new FileCreationInformation
                            {
                                ContentStream = System.IO.File.OpenRead(FilePath),
                                Overwrite = true,
                                Url = DestPath
                            };
                            Microsoft.SharePoint.Client.File uploadedFile = ParentSPFolder.Files.Add(fileCreationInfo);
                            ctx.Load(uploadedFile, f => f.ServerRelativeUrl);
                            ListItem uploadedItem = uploadedFile.ListItemAllFields;
                            uploadedItem[CommonListColumns.Title] = FileName;
                            uploadedItem[CommonListColumns.Created] = Convert.ToString(reader[nameof(FilesDetails.FileCreatedDate)]);
                            uploadedItem[CommonListColumns.Modified] = Convert.ToString(reader[nameof(FilesDetails.FileModifiedDate)]);
                            User fileCreator = GetValidUser(Convert.ToString(reader[nameof(FilesDetails.FileCreator)]), ctx.Web);
                            List<ListItemFormUpdateValue> lstSystemItem = new List<ListItemFormUpdateValue>();
                            //if (fileCreator != null)
                            //{
                            //    ListItemFormUpdateValue sysitemEditor = new ListItemFormUpdateValue
                            //    {
                            //        FieldName = CommonListColumns.Editor,
                            //        FieldValue = fileCreator.Id.ToString()
                            //    };
                            //    lstSystemItem.Add(sysitemEditor);

                            //    ListItemFormUpdateValue sysitemAuthor = new ListItemFormUpdateValue
                            //    {
                            //        FieldName = CommonListColumns.Author,
                            //        FieldValue = fileCreator.Id.ToString()
                            //    };
                            //    lstSystemItem.Add(sysitemAuthor);
                            //}
                            if (fileCreator != null)
                            {
                                uploadedItem[CommonListColumns.Editor] = fileCreator;
                                uploadedItem[CommonListColumns.Author] = fileCreator;
                            }
                            try
                            {
                                uploadedItem.Update();
                                //Added only to prevent creation of second version of file
                                //uploadedItem.ValidateUpdateListItem(lstSystemItem, true, "");
                                ctx.ExecuteQuery();

                                string ID = Convert.ToString(reader[nameof(FilesDetails.ID)]);
                                CompletedItemList.Add(Constants.SPSuccessFS + ":" + ID);
                                Console.WriteLine("File Migrated To SharePoint");
                                SPPermLiteMigration(FilePath, ctx, uploadedItem);
                                Console.WriteLine("File Permissions Migrated To SharePoint");
                            }
                            catch (Exception ex)
                            {
                                string ID = Convert.ToString(reader[nameof(FilesDetails.ID)]);
                                CompletedItemList.Add(Constants.SPFailFS + ":" + ID);
                                Console.WriteLine("Exception\n" + ex.ToString());
                                Log.WriteToEventViewer(ex);
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format(GeneralValues.MsgFileNotExist, FilePath));
                            Log.WriteToEventViewer(string.Format(GeneralValues.MsgFileNotExist, FilePath));
                        }
                    }
                }
            }
        }
        static void SPPermLiteMigration(string path, ClientContext ctx, ListItem SPItem)
        {
            Console.WriteLine("Processing Permission");
            //Because you can not open dataadaptor if there is already datareader open
            using (SQLiteConnection con = new SQLiteConnection(SQLiteConStr))
            {
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.Text;
                    StringBuilder WhereQuery = new StringBuilder();
                    WhereQuery.Append(nameof(PermissionDetails.ObjectPath) + "=\"" + path + "\"");
                    WhereQuery.Append(" Order By " + nameof(FolderDetails.ID));
                    cmd.CommandText = Helper.GetSelectQuery(nameof(PermissionDetails), "*", WhereQuery.ToString());
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter())
                    {
                        da.SelectCommand = cmd;
                        using (DataTable tbl = new DataTable())
                        {
                            da.Fill(tbl);
                            int count = tbl.Rows.Count;
                            var ExplicitRows = tbl.Select(nameof(PermissionDetails.IsInherited) + "=false");
                            if (ExplicitRows.Count() > 0)
                            {
                                SPItem.BreakRoleInheritance(false, true);
                                var DenyRows = tbl.Select(nameof(PermissionDetails.IsAllow) + "=false");
                                if (DenyRows.Count() > 0)
                                {
                                    using (PrincipalContext ADContext = new PrincipalContext(ContextType.Domain))
                                    {
                                        Console.WriteLine("User Info Collection Started");

                                        List<UserPermMapping> AllowMapping = new List<UserPermMapping>();
                                        List<UserPermMapping> DenyMapping = new List<UserPermMapping>();
                                        foreach (DataRow row in tbl.Rows)
                                        {
                                            string permString = Convert.ToString(row[nameof(PermissionDetails.Permissions)]);
                                            var mapPerm = TblPermMapping.Select(nameof(PermissionMapping.WinPermName) + "='" + permString + "'").FirstOrDefault();
                                            permString = Convert.ToString(mapPerm[nameof(PermissionMapping.SPPermName)]);
                                            string PermUserName = Convert.ToString(row[nameof(PermissionDetails.UserName)]);
                                            bool IsAllow = Convert.ToBoolean(row[nameof(PermissionDetails.IsAllow)]);
                                            if (IsAllow)
                                            {
                                                GetUserList(PermUserName, permString, ADContext, AllowMapping);
                                            }
                                            else
                                            {
                                                GetUserList(PermUserName, permString, ADContext, DenyMapping);
                                            }
                                        }

                                        foreach (var DenyMap in DenyMapping)
                                        {
                                            AllowMapping.RemoveAll(x => x.UserName == DenyMap.UserName && x.PermStr == DenyMap.PermStr);
                                        }

                                        foreach (UserPermMapping UPM in AllowMapping)
                                        {
                                            User itemUser = GetValidUser(UPM.UserName, ctx.Web);
                                            if (itemUser != null)
                                            {
                                                string permString = UPM.PermStr;
                                                RoleDefinitionBindingCollection roleBindings = new RoleDefinitionBindingCollection(ctx);
                                                roleBindings.Add(ctx.Web.RoleDefinitions.GetByName(permString));
                                                SPItem.RoleAssignments.Add(itemUser, roleBindings);
                                            }
                                        }
                                        Console.WriteLine("User Info Collection Ended");
                                    }
                                }
                                else
                                {
                                    foreach (DataRow row in tbl.Rows)
                                    {
                                        string UserName = Convert.ToString(row[nameof(PermissionDetails.UserName)]);
                                        User itemUser = GetValidUser(UserName.Trim(), ctx.Web);

                                        if (itemUser != null)
                                        {
                                            string permString = Convert.ToString(row[nameof(PermissionDetails.Permissions)]);
                                            var mapPerm = TblPermMapping.Select(nameof(PermissionMapping.WinPermName) + "='" + permString + "'").FirstOrDefault();
                                            string SPPermName = Convert.ToString(mapPerm[nameof(PermissionMapping.SPPermName)]);
                                            if (!string.IsNullOrEmpty(SPPermName))
                                            {
                                                RoleDefinitionBindingCollection roleBindings = new RoleDefinitionBindingCollection(ctx);
                                                roleBindings.Add(ctx.Web.RoleDefinitions.GetByName(SPPermName.Trim()));
                                                SPItem.RoleAssignments.Add(itemUser, roleBindings);
                                            }
                                            else
                                                throw new Exception(SPPermName + " - Permission String Not Found");
                                        }
                                    }
                                }
                                try
                                {
                                    SPItem.Update();
                                    ctx.ExecuteQuery();
                                    // CompletedItemList.Add(Constants.SPSuccessPM + ":" + path);
                                    Console.WriteLine("Permission Migrated To SharePoint");
                                }
                                catch (Exception ex)
                                {
                                    // CompletedItemList.Add(Constants.SPFailPM + ":" + path);
                                    Console.WriteLine("Exception\n" + ex.ToString());

                                    Log.WriteToEventViewer(ex);
                                    //StringBuilder InsertQuery = new StringBuilder();
                                    //InsertQuery.Append("INSERT INTO " + nameof(TimeLog));
                                    //InsertQuery.Append("(");
                                    //InsertQuery.Append(nameof(TimeLog.SourceUrl) + ",");
                                    //InsertQuery.Append(nameof(TimeLog.Time));
                                    //InsertQuery.Append(")");
                                    //InsertQuery.Append("Values(");
                                    //InsertQuery.Append("\"" + ex.Message + "\"" + ",");
                                    //InsertQuery.Append(0);
                                    //InsertQuery.Append(")");
                                    //cmd.CommandText = InsertQuery.ToString();
                                    //cmd.ExecuteNonQuery();

                                    // AddToReportDB(InsertQuery.ToString());
                                }
                            }
                        }
                    }
                }
            }
        }
        public static User GetValidUser(string AccountName, Web web)
        {
            string[] splitAccounts = AccountName.Split(GeneralValues.BackSlash);
            User user = null;
            if (splitAccounts.Length > 1)
            {
                if (splitAccounts[0].Equals(DomainName, StringComparison.CurrentCultureIgnoreCase))
                {
                    user = web.EnsureUser(AccountName);
                }
            }
            else if (AccountName.Equals(GeneralValues.Default_Account_Everyone, StringComparison.CurrentCultureIgnoreCase))
                user = web.EnsureUser(GeneralValues.SPEveryoneGrpSID);
            return user;
        }
        static void GetUserList(string UserName, string PermStr, PrincipalContext ADContext, List<UserPermMapping> UserMapList)
        {
            var ADPrincipal = System.DirectoryServices.AccountManagement.Principal.FindByIdentity(ADContext, UserName);

            UserPermMapping UPM = new UserPermMapping();
            UPM.UserName = UserName;
            UPM.PermStr = PermStr;
            if (ADPrincipal == null)
            {
                Console.WriteLine("Empty Principal - " + UserName);
                UserMapList.Add(UPM);
            }
            else if (ADPrincipal is UserPrincipal)
            {
                Console.WriteLine("User Principal - " + UserName);
                UserMapList.Add(UPM);
            }
            else if (ADPrincipal is GroupPrincipal)
            {
                GroupPrincipal ADGroup = ADPrincipal as GroupPrincipal;
                foreach (var GroupUser in ADGroup.Members)
                {
                    GetUserList(GroupUser.Name, PermStr, ADContext, UserMapList);
                }
            }
            else
                Console.WriteLine("Other Type Of User Need Checking");
        }
        static void SetSQLitePermMappings()
        {
            using (SQLiteConnection con = new SQLiteConnection(SQLiteConStr))
            {
                con.Open();
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    StringBuilder SelectQuery = new StringBuilder();
                    SelectQuery.Append(nameof(PermissionMapping.ID) + ",");
                    SelectQuery.Append(nameof(PermissionMapping.WinPermName) + ",");
                    SelectQuery.Append(nameof(PermissionMapping.SPPermName));
                    cmd.CommandText = Helper.GetSelectQuery(nameof(PermissionMapping), SelectQuery.ToString());
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter(cmd))
                    {
                        da.Fill(TblPermMapping);
                    }
                }
            }
        }
        static void UpdateCompletedData(SQLiteConnection conn, SQLiteCommand cmd)
        {
            using (SQLiteTransaction trans = conn.BeginTransaction())
            {
                List<UpdateResult> UpdateDict = new List<UpdateResult>();
                StringBuilder UpdateQ = new StringBuilder();
                string TblName = "";
                int IsSucess = 0;
                foreach (var updatetext in CompletedItemList)
                {
                    UpdateResult update = new UpdateResult();
                    string[] AllUpdate = updatetext.Split(':');
                    update.ItemID = AllUpdate[1];

                    if (AllUpdate[0] == Constants.SPSuccessFD)
                    {
                        IsSucess = 1;
                        TblName = nameof(FolderDetails);
                    }
                    else if (AllUpdate[0] == Constants.SPSuccessFS)
                    {
                        IsSucess = 1;
                        TblName = nameof(FilesDetails);
                    }
                    else if (AllUpdate[0] == Constants.SPSuccessPM)
                    {
                        IsSucess = 1;
                        TblName = nameof(PermissionDetails);
                    }
                    else if (AllUpdate[0] == Constants.SPFailFD)
                    {
                        IsSucess = 0;
                        TblName = nameof(FolderDetails);
                    }
                    else if (AllUpdate[0] == Constants.SPFailFS)
                    {
                        IsSucess = 0;
                        TblName = nameof(FilesDetails);
                    }
                    else if (AllUpdate[0] == Constants.SPFailPM)
                    {
                        IsSucess = 0;
                        TblName = nameof(PermissionDetails);
                    }
                    cmd.CommandText = Helper.GetUpdateByIDQuery(TblName, AllUpdate[1], nameof(FolderDetails.IsMoveToSP), IsSucess.ToString());
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
                trans.Commit();
            }
        }
        static StringBuilder GetFolderWhereQuery(string SourceFolderPath)
        {
            StringBuilder WhereQuery = new StringBuilder();
            WhereQuery.Append("(" + nameof(FolderDetails.IsMoveToSP) + " is Null");
            WhereQuery.Append(" or ");
            WhereQuery.Append(nameof(FolderDetails.IsMoveToSP) + "=0)");
            WhereQuery.Append(" and ");
            WhereQuery.Append(nameof(FolderDetails.FolderCreatedDate) + " is not NULL");
            WhereQuery.Append(" and ");
            WhereQuery.Append(nameof(FolderDetails.FolderPath) + " like \"" + SourceFolderPath + "%\"");

            return WhereQuery;
        }
        static StringBuilder GetFilesWhereQuery(string SourceFolderPath)
        {
            StringBuilder WhereQuery = new StringBuilder();
            WhereQuery.Append("(" + nameof(FilesDetails.IsMoveToSP) + " is Null");
            WhereQuery.Append(" or ");
            WhereQuery.Append(nameof(FilesDetails.IsMoveToSP) + "=0)");
            WhereQuery.Append(" and ");
            WhereQuery.Append(nameof(FilesDetails.FileCreatedDate) + " is not NULL");
            WhereQuery.Append(" and ");
            WhereQuery.Append(nameof(FilesDetails.FilePath) + " like \"" + SourceFolderPath + "%\"");

            return WhereQuery;
        }

        static StringBuilder GetFolderPermWhereQuery(string SourceFolderPath)
        {
            StringBuilder WhereQuery = new StringBuilder();
            WhereQuery.Append(nameof(FolderDetails.IsMoveToSP) + "=1");
            WhereQuery.Append(" and ");
            WhereQuery.Append(nameof(FolderDetails.FolderPath) + " like \"" + SourceFolderPath + "%\"");

            return WhereQuery;
        }
        static StringBuilder GetFilesPermWhereQuery(string SourceFolderPath)
        {
            StringBuilder WhereQuery = new StringBuilder();
            WhereQuery.Append(nameof(FilesDetails.IsMoveToSP) + "=1");
            WhereQuery.Append(" and ");
            WhereQuery.Append(nameof(FilesDetails.FilePath) + " like \"" + SourceFolderPath + "%\"");

            return WhereQuery;
        }
        static void SPFolderPermMigration(SQLiteCommand cmd, string SourceFolderPath, Folder ParentSPFolder, ClientContext ctx)
        {
            string BaseFolderURL = ParentSPFolder.ServerRelativeUrl;
            Console.WriteLine("Processing Folder\n" + SourceFolderPath);
            string SelectTQuery = Helper.GetSelectQuery(nameof(FolderDetails), "Count(" + nameof(FolderDetails.ID) + ")", GetFolderPermWhereQuery(SourceFolderPath).ToString());
            cmd.CommandText = SelectTQuery;
            cmd.CommandType = CommandType.Text;
            var result = cmd.ExecuteScalar();
            int RecordsCount = Convert.ToInt32(result);
            int CurrentCount = 0;
            while (RecordsCount != CurrentCount)
            {
                StringBuilder WhereQuery = GetFolderPermWhereQuery(SourceFolderPath);
                WhereQuery.Append(" ORDER By " + nameof(FolderDetails.FolderLevel));
                WhereQuery.Append(" Limit " + PageSize);
                WhereQuery.Append(" OFFSET " + CurrentCount);
                string SelectQuery = Helper.GetSelectQuery(nameof(FolderDetails), "*", WhereQuery.ToString());
                cmd.CommandText = SelectQuery;
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CurrentCount++;
                        string FolderPath = Convert.ToString(reader[nameof(FolderDetails.FolderPath)]);
                        string ID = Convert.ToString(reader[nameof(FolderDetails.ID)]);
                        Console.WriteLine("Processing Folder - " + FolderPath);
                        if (Directory.Exists(FolderPath))
                        {
                            string FolderName = Convert.ToString(reader[nameof(FolderDetails.FolderName)]);
                            string FolderLevel = Convert.ToString(reader[nameof(FolderDetails.FolderLevel)]);
                            string DestPath = "";
                            Folder DestFolder = null;
                            if (FolderLevel == "0")
                            {
                                DestPath = FolderName.Replace("\\", "").Replace(":", "");
                                ParentSPFolder = ParentSPFolder.Folders.GetByUrl(DestPath);
                                DestFolder = ParentSPFolder;
                            }
                            else
                            {
                                string DirPath = Path.GetDirectoryName(SourceFolderPath);
                                DestPath = FolderPath.Replace(DirPath, "").Replace("\\", "/");
                                DestFolder = ctx.Web.GetFolderByServerRelativeUrl(BaseFolderURL + "/" + DestPath);
                                // DestFolder = ParentSPFolder.Folders.GetByUrl(DestPath);
                            }
                            ctx.Load(DestFolder);
                            ctx.ExecuteQuery();
                            ListItem DestItem = DestFolder.ListItemAllFields;
                            CompletedItemList.Add(Constants.SPSuccessFD + ":" + ID);
                            SPSpecialPermLiteMigration(FolderPath, ctx, DestItem);
                            Console.WriteLine("Folder Permissions Migrated To SharePoint");
                        }
                        else
                        {
                            CompletedItemList.Add(Constants.SPFailFD + ":" + ID);
                            Console.WriteLine(string.Format(GeneralValues.MsgFolderNotExist, FolderPath));
                            Log.WriteToEventViewer(string.Format(GeneralValues.MsgFolderNotExist, FolderPath));
                        }
                    }
                }
            }
        }
        static void SPFilesPermMigration(SQLiteCommand cmd, string SourceFolderPath, Folder ParentSPFolder, ClientContext ctx)
        {
            cmd.CommandText = Helper.GetSelectQuery(nameof(FilesDetails), "Count(" + nameof(FolderDetails.ID) + ")", GetFilesPermWhereQuery(SourceFolderPath).ToString());
            cmd.CommandType = CommandType.Text;
            var result = cmd.ExecuteScalar();
            int RecordsCount = Convert.ToInt32(result);
            int CurrentCount = 0;
            while (RecordsCount != CurrentCount)
            {
                StringBuilder WhereQuery = GetFilesPermWhereQuery(SourceFolderPath);
                WhereQuery.Append(" ORDER By " + nameof(FolderDetails.ID));
                WhereQuery.Append(" Limit " + PageSize);
                WhereQuery.Append(" OFFSET " + CurrentCount);
                string SelectQuery = Helper.GetSelectQuery(nameof(FilesDetails), "*", WhereQuery.ToString());
                cmd.CommandText = SelectQuery;
                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        CurrentCount++;
                        string FilePath = Convert.ToString(reader[nameof(FilesDetails.FilePath)]);
                        Console.WriteLine("Processing File Path - " + FilePath);
                        if (System.IO.File.Exists(FilePath))
                        {
                            string FileName = Path.GetFileName(FilePath);
                            string FolderPath = Path.GetDirectoryName(FilePath);
                            string DestPath = ParentSPFolder.ServerRelativeUrl + "/";
                            if (SourceFolderPath == FolderPath)
                                DestPath += FileName;
                            else
                                DestPath += FolderPath.Replace(SourceFolderPath + "\\", "").Replace("\\", "/") + "/" + FileName;

                            //string DirPath = Path.GetDirectoryName(DestPath);
                            //ctx.Web.GetFolderByServerRelativeUrl(BaseFolderURL + "/" + DestPath);
                            //Microsoft.SharePoint.Client.File uploadedFile = ParentSPFolder.Files.GetByUrl(DestPath);
                            Microsoft.SharePoint.Client.File uploadedFile = ctx.Web.GetFileByServerRelativeUrl(DestPath);
                            ctx.Load(uploadedFile, x => x.ListItemAllFields);
                            ctx.ExecuteQuery();
                            ListItem uploadedItem = uploadedFile.ListItemAllFields;
                            try
                            {
                                string ID = Convert.ToString(reader[nameof(FilesDetails.ID)]);
                                CompletedItemList.Add(Constants.SPSuccessFS + ":" + ID);
                                SPSpecialPermLiteMigration(FilePath, ctx, uploadedItem);
                                Console.WriteLine("File Permissions Migrated To SharePoint");
                            }
                            catch (Exception ex)
                            {
                                string ID = Convert.ToString(reader[nameof(FilesDetails.ID)]);
                                CompletedItemList.Add(Constants.SPFailFS + ":" + ID);
                                Console.WriteLine("Exception\n" + ex.ToString());
                                Log.WriteToEventViewer(ex);
                            }
                        }
                        else
                        {
                            Console.WriteLine(string.Format(GeneralValues.MsgFileNotExist, FilePath));
                            Log.WriteToEventViewer(string.Format(GeneralValues.MsgFileNotExist, FilePath));
                        }
                    }
                }
            }
        }
        static void SPSpecialPermLiteMigration(string path, ClientContext ctx, ListItem SPItem)
        {
            Console.WriteLine("Processing Special Permission");
            //Because you can not open dataadaptor if there is already datareader open
            using (SQLiteConnection con = new SQLiteConnection(SQLiteConStr))
            {
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandTimeout = 0;
                    cmd.CommandType = CommandType.Text;
                    StringBuilder WhereQuery = new StringBuilder();
                    WhereQuery.Append(nameof(PermissionDetails.ObjectPath) + "=\"" + path + "\"");
                    //WhereQuery.Append(" Order By " + nameof(FolderDetails.ID));
                    string selectQuery = Helper.GetSelectQuery(nameof(PermissionDetails), "*", WhereQuery.ToString());
                    cmd.CommandText = selectQuery;
                    using (SQLiteDataAdapter da = new SQLiteDataAdapter())
                    {
                        da.SelectCommand = cmd;
                        using (DataTable tbl = new DataTable())
                        {
                            da.Fill(tbl);
                            int count = tbl.Rows.Count;
                            var ExplicitRows = tbl.Select(nameof(PermissionDetails.IsInherited) + " = false");
                            if (ExplicitRows.Count() > 0)
                            {
                                SPItem.BreakRoleInheritance(false, true);
                                var DenyRows = tbl.Select(nameof(PermissionDetails.IsAllow) + "=false");
                                if (DenyRows.Count() > 0)
                                {
                                    using (PrincipalContext ADContext = new PrincipalContext(ContextType.Domain))
                                    {
                                        Console.WriteLine("User Info Collection Started");

                                        List<UserPermMapping> AllowMapping = new List<UserPermMapping>();
                                        List<UserPermMapping> DenyMapping = new List<UserPermMapping>();
                                        foreach (DataRow row in tbl.Rows)
                                        {
                                            string permString = Convert.ToString(row[nameof(PermissionDetails.Permissions)]);
                                            var mapPerm = TblPermMapping.Select(nameof(PermissionMapping.WinPermName) + "='" + permString + "'").FirstOrDefault();
                                            permString = Convert.ToString(mapPerm[nameof(PermissionMapping.SPPermName)]);
                                            string PermUserName = Convert.ToString(row[nameof(PermissionDetails.UserName)]);
                                            bool IsAllow = Convert.ToBoolean(row[nameof(PermissionDetails.IsAllow)]);
                                            if (IsAllow)
                                            {
                                                GetUserList(PermUserName, permString, ADContext, AllowMapping);
                                            }
                                            else
                                            {
                                                GetUserList(PermUserName, permString, ADContext, DenyMapping);
                                            }
                                        }

                                        foreach (var DenyMap in DenyMapping)
                                        {
                                            AllowMapping.RemoveAll(x => x.UserName == DenyMap.UserName && x.PermStr == DenyMap.PermStr);
                                        }

                                        foreach (UserPermMapping UPM in AllowMapping)
                                        {
                                            User itemUser = null;
                                            if (UserList.ContainsKey(UPM.UserName))
                                            {
                                                itemUser = UserList[UPM.UserName];
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    itemUser = GetValidUser(UPM.UserName, ctx.Web);
                                                    ctx.ExecuteQuery();
                                                    UserList.Add(UPM.UserName, itemUser);
                                                }
                                                catch (Exception)
                                                {
                                                    UserList.Add(UPM.UserName, null);
                                                }
                                            }

                                            if (itemUser != null)
                                            {
                                                string permString = UPM.PermStr;
                                                RoleDefinitionBindingCollection roleBindings = new RoleDefinitionBindingCollection(ctx);
                                                roleBindings.Add(ctx.Web.RoleDefinitions.GetByName(permString));
                                                SPItem.RoleAssignments.Add(itemUser, roleBindings);
                                            }
                                        }

                                    }
                                    Console.WriteLine("User Info Collection Ended");
                                }
                                else
                                {
                                    foreach (DataRow row in tbl.Rows)
                                    {
                                        string UserName = Convert.ToString(row[nameof(PermissionDetails.UserName)]);
                                        User itemUser = GetValidUser(UserName, ctx.Web);
                                        try
                                        {
                                            ctx.ExecuteQuery();
                                            try
                                            {
                                                if (itemUser != null)
                                                {
                                                    string permString = Convert.ToString(row[nameof(PermissionDetails.Permissions)]);
                                                    var mapPerm = TblPermMapping.Select(nameof(PermissionMapping.WinPermName) + "='" + permString + "'").FirstOrDefault();
                                                    string SPPermName = Convert.ToString(mapPerm[nameof(PermissionMapping.SPPermName)]);
                                                    if (!string.IsNullOrEmpty(SPPermName))
                                                    {
                                                        RoleDefinitionBindingCollection roleBindings = new RoleDefinitionBindingCollection(ctx);
                                                        roleBindings.Add(ctx.Web.RoleDefinitions.GetByName(SPPermName.Trim()));
                                                        SPItem.RoleAssignments.Add(itemUser, roleBindings);
                                                    }
                                                    else
                                                        throw new Exception(SPPermName + " - Permission String Not Found");
                                                }
                                            }
                                            catch (Exception)
                                            {
                                            }
                                        }
                                        catch (Exception)
                                        {
                                        }


                                        //User itemUser = null;
                                        //if (UserList.ContainsKey(UserName))
                                        //{
                                        //    itemUser = UserList[UserName];
                                        //}
                                        //else
                                        //{
                                        //    try
                                        //    {
                                        //        itemUser = GetValidUser(UserName, ctx.Web);
                                        //        ctx.ExecuteQuery();
                                        //        UserList.Add(UserName, itemUser);
                                        //    }
                                        //    catch (Exception)
                                        //    {
                                        //        UserList.Add(UserName, null);
                                        //    }
                                        //}

                                        //try
                                        //{
                                        //    if (itemUser != null)
                                        //    {
                                        //        string permString = Convert.ToString(row[nameof(PermissionDetails.Permissions)]);
                                        //        var mapPerm = TblPermMapping.Select(nameof(PermissionMapping.WinPermName) + "='" + permString + "'").FirstOrDefault();
                                        //        string SPPermName = Convert.ToString(mapPerm[nameof(PermissionMapping.SPPermName)]);
                                        //        if (!string.IsNullOrEmpty(SPPermName))
                                        //        {
                                        //            RoleDefinitionBindingCollection roleBindings = new RoleDefinitionBindingCollection(ctx);
                                        //            roleBindings.Add(ctx.Web.RoleDefinitions.GetByName(SPPermName.Trim()));
                                        //            SPItem.RoleAssignments.Add(itemUser, roleBindings);
                                        //        }
                                        //        else
                                        //            throw new Exception(SPPermName + " - Permission String Not Found");
                                        //    }
                                        //}
                                        //catch (Exception)
                                        //{
                                        //}

                                    }
                                }
                            }
                            else
                                SPItem.ResetRoleInheritance();
                            try
                            {
                                SPItem.Update();
                                ctx.ExecuteQuery();
                                // CompletedItemList.Add(Constants.SPSuccessPM + ":" + path);
                                Console.WriteLine("Permission Migrated To SharePoint");
                            }
                            catch (Exception ex)
                            {
                                // CompletedItemList.Add(Constants.SPFailPM + ":" + path);
                                Console.WriteLine("Exception\n" + ex.ToString());

                                Log.WriteToEventViewer(ex);
                                //StringBuilder InsertQuery = new StringBuilder();
                                //InsertQuery.Append("INSERT INTO " + nameof(TimeLog));
                                //InsertQuery.Append("(");
                                //InsertQuery.Append(nameof(TimeLog.SourceUrl) + ",");
                                //InsertQuery.Append(nameof(TimeLog.Time));
                                //InsertQuery.Append(")");
                                //InsertQuery.Append("Values(");
                                //InsertQuery.Append("\"" + ex.Message + "\"" + ",");
                                //InsertQuery.Append(0);
                                //InsertQuery.Append(")");
                                //cmd.CommandText = InsertQuery.ToString();
                                //cmd.ExecuteNonQuery();

                                //AddToReportDB(InsertQuery.ToString());
                            }
                        }
                    }
                }
            }
        }
        #endregion

        //static void AddToReportDB(string InsertQuery)
        //{
        //    string ConStr = Helper.GetConString(ReportDBPath);
        //    using (SQLiteConnection con = new SQLiteConnection(ConStr))
        //    {
        //        con.Open();
        //        using (SQLiteCommand cmd = con.CreateCommand())
        //        {
        //            cmd.CommandType = CommandType.Text;
        //            cmd.CommandTimeout = 0;
        //            cmd.CommandText = InsertQuery;
        //            cmd.ExecuteNonQuery();
        //        }
        //    }
        //}
        static void UpdateDBFilePaths(string ID, string DBFilePath)
        {
            string ConStr = Helper.GetConString(ReportDBPath);
            using (SQLiteConnection con = new SQLiteConnection(ConStr))
            {
                con.Open();
                using (SQLiteCommand cmd = con.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandTimeout = 0;
                    cmd.CommandText = Helper.GetUpdateByIDQuery(nameof(FoldersMapping), ID, nameof(FoldersMapping.DBFilePath), "\"" + DBFilePath + "\"");
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
