using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace MigrationHelper
{
    public class FSDetails
    {
        //public static void FSInfoByPath(string FolderPath, string ConString)
        //{
        //    try
        //    {
        //        if (Directory.Exists(FolderPath))
        //        {
        //            using (SQLiteConnection con = new SQLiteConnection(ConString))
        //            {
        //                Stopwatch watch = Stopwatch.StartNew();
        //                con.Open();
        //                using (SQLiteCommand cmd = con.CreateCommand())
        //                {
        //                    using (SQLiteTransaction transaction = con.BeginTransaction())
        //                    {
        //                        try
        //                        {
        //                            cmd.CommandTimeout = 0;
        //                            cmd.CommandType = CommandType.Text;
        //                            Console.WriteLine("File System Information Collection Started");
        //                            FileSystemSQLiteLog(FolderPath, 0, 0, cmd, transaction);
        //                            Console.WriteLine("File System Information Collection Completed");
        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            Console.WriteLine("Exception in application - " + ex.Message);
        //                            Console.WriteLine(ex.StackTrace);
        //                            Log.WriteToEventViewer(ex);
        //                        }
        //                        finally
        //                        {
        //                            transaction.Commit();
        //                            watch.Stop();
        //                            Console.WriteLine("File System Information Collection Time - " + watch.ElapsedMilliseconds);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //            Console.WriteLine("Folder Path Does Not Exist");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Exception in application - " + ex.Message);
        //        Console.WriteLine(ex.StackTrace);
        //        Log.WriteToEventViewer(ex);
        //        Console.ReadLine();
        //    }
        //}
        //private static void FileSystemSQLiteLog(string FolderPath, long ParentFolderID, int FolderLevel, SQLiteCommand cmd, SQLiteTransaction trans)
        //{
        //    Stopwatch watch = Stopwatch.StartNew();
        //    if (Directory.Exists(FolderPath))
        //    {
        //        DirectoryInfo dirInfo = new DirectoryInfo(FolderPath);
        //        long FolderResult = AddFolderToLiteDB(dirInfo, cmd, ParentFolderID, FolderLevel);
        //        watch.Stop();
        //        Console.WriteLine("Time Taken to Insert Folder Into DB - " + watch.ElapsedMilliseconds);
        //        Console.WriteLine(FolderPath + "\nFolder Info Successfully Created");
        //        watch.Restart();
        //        if (FolderResult != -1)
        //        {
        //            ParentFolderID = FolderResult;
        //            DirectorySecurity dirSecurity = dirInfo.GetAccessControl();
        //            AuthorizationRuleCollection DirAllRules = dirSecurity.GetAccessRules(true, true, typeof(NTAccount));
        //            IEnumerable<FileSystemAccessRule> DirPermRules = DirAllRules.Cast<FileSystemAccessRule>();
        //            AddPermDetailSQLite(DirPermRules, FolderPath, false, cmd);
        //            watch.Stop();
        //            Console.WriteLine("Time Taken to Gather Permission Info - " + watch.ElapsedMilliseconds);
        //            Console.WriteLine(FolderPath + "\nFolder Permission Info Successfully Completed");

        //            var VisibleFiles = dirInfo.EnumerateFiles().Where(x => !(x.Attributes.HasFlag(FileAttributes.Hidden)));//.Where(x => !(x.Attributes.HasFlag(FileAttributes.System)));
        //            foreach (FileInfo file in VisibleFiles)
        //            {
        //                watch.Restart();
        //                string filePath = file.FullName;
        //                long InsertedFileID = AddFileDToLiteDB(file, cmd, ParentFolderID);
        //                watch.Stop();
        //                Console.WriteLine("Time Taken to Add File Info To DB - " + watch.ElapsedMilliseconds);
        //                Console.WriteLine(file.FullName + "\nFile Info Successfully Created");

        //                watch.Restart();
        //                if (InsertedFileID != -1)
        //                {
        //                    FileSecurity fileSecurity = file.GetAccessControl();
        //                    AuthorizationRuleCollection FileAllRules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));
        //                    IEnumerable<FileSystemAccessRule> FilePermRules = FileAllRules.Cast<FileSystemAccessRule>();
        //                    AddPermDetailSQLite(FilePermRules, filePath, true, cmd);

        //                    watch.Stop();
        //                    Console.WriteLine("Time Taken to gather File Permissions Info - " + watch.ElapsedMilliseconds);
        //                    Console.WriteLine(filePath + "\nFile Permission Info Successfully Completed");
        //                }
        //            }

        //            var VisibleDirectories = dirInfo.EnumerateDirectories().Where(x => !(x.Attributes.HasFlag(FileAttributes.Hidden)));//.Where(x=>!(x.Attributes.HasFlag(FileAttributes.System)));
        //            foreach (DirectoryInfo dir in VisibleDirectories)
        //            {
        //                FolderLevel++;
        //                FileSystemSQLiteLog(dir.FullName, ParentFolderID, FolderLevel, cmd, trans);
        //                FolderLevel--;
        //            }
        //        }
        //    }
        //}
        //private static long AddFolderToLiteDB(DirectoryInfo DirInfo, SQLiteCommand cmd, long ParentFolderID, int FolderLevel)
        //{
        //    string TodayDate = DateTime.Now.ToString();
        //    string FolderName = DirInfo.Name;
        //    string FolderPath = DirInfo.FullName;

        //    Console.WriteLine("Processing Folder\n" + FolderPath);
        //    Dictionary<string, string> InsertQuery = new Dictionary<string, string>();
        //    long result = 0;
        //    try
        //    {
        //        string folderOwnerName = DirInfo.GetAccessControl().GetOwner(typeof(NTAccount)).Value;
        //        InsertQuery.Add(nameof(FolderDetails.CreatedDate), Constants.DoubleQuote + TodayDate + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FilesCount), DirInfo.EnumerateFiles().Count().ToString());
        //        InsertQuery.Add(nameof(FolderDetails.FolderCreatedDate), Constants.DoubleQuote + DirInfo.CreationTime.ToString() + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderCreator), Constants.DoubleQuote + folderOwnerName + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderLevel), FolderLevel.ToString());
        //        InsertQuery.Add(nameof(FolderDetails.FolderModifiedDate), Constants.DoubleQuote + DirInfo.LastWriteTime.ToString() + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderModifier), Constants.DoubleQuote + folderOwnerName + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderName), Constants.DoubleQuote + FolderName + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderPath), Constants.DoubleQuote + FolderPath + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.Modifieddate), Constants.DoubleQuote + TodayDate + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.Notes), string.Empty);
        //        InsertQuery.Add(nameof(FolderDetails.ParentFolderID), ParentFolderID.ToString());
        //        InsertQuery.Add(nameof(FolderDetails.SubFolderCount), DirInfo.EnumerateDirectories().Count().ToString());

        //        string SQLInsertQr = Helper.GetInsertQuery(Constants.SQLFolderTblName, InsertQuery);
        //        Console.WriteLine("Insert Query\n" + SQLInsertQr);
        //        cmd.CommandText = SQLInsertQr;
        //        cmd.ExecuteNonQuery();
        //        result = cmd.Connection.LastInsertRowId;
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        InsertQuery.Add(nameof(FolderDetails.CreatedDate), Constants.DoubleQuote + TodayDate + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderName), Constants.DoubleQuote + FolderName + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderPath), Constants.DoubleQuote + FolderPath + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.Modifieddate), Constants.DoubleQuote + TodayDate + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.ParentFolderID), ParentFolderID.ToString());

        //        string SQLInsertQr = Helper.GetInsertQuery(Constants.SQLFolderTblName, InsertQuery);
        //        Console.WriteLine("Insert Query\n" + SQLInsertQr);
        //        cmd.CommandText = SQLInsertQr;
        //        cmd.ExecuteNonQuery();

        //        AddIssueTbl(true, string.Empty, FolderName, FolderPath, cmd);
        //        result = -1;
        //    }
        //    catch (Exception ex)
        //    {
        //        InsertQuery.Add(nameof(FolderDetails.CreatedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderName), Constants.DoubleQuote + FolderName + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.FolderPath), Constants.DoubleQuote + FolderPath + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.Modifieddate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertQuery.Add(nameof(FolderDetails.ParentFolderID), ParentFolderID.ToString());

        //        string SQLInsertQr = Helper.GetInsertQuery(Constants.SQLFolderTblName, InsertQuery);
        //        Console.WriteLine("Insert Query\n" + SQLInsertQr);
        //        cmd.CommandText = SQLInsertQr;
        //        cmd.ExecuteNonQuery();

        //        AddIssueTbl(false, ex.Message + "\n" + ex.StackTrace, FolderName, FolderPath, cmd);
        //        result = -1;
        //    }
        //    return result;
        //}
        //private static void AddPermDetailSQLite(IEnumerable<FileSystemAccessRule> PermRules, string path, bool IsFile, SQLiteCommand cmd)
        //{
        //    Console.WriteLine("Processing Permissions\n" + path);
        //    foreach (FileSystemAccessRule rule in PermRules)
        //    {
        //        Dictionary<string, string> InsertDict = new Dictionary<string, string>();

        //        InsertDict.Add(nameof(PermissionDetails.CreatedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(PermissionDetails.IsAllow), (rule.AccessControlType == AccessControlType.Allow).ToString());
        //        InsertDict.Add(nameof(PermissionDetails.IsFile), IsFile.ToString());
        //        InsertDict.Add(nameof(PermissionDetails.IsInherited), rule.IsInherited.ToString());
        //        InsertDict.Add(nameof(PermissionDetails.ModifiedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(PermissionDetails.Notes), string.Empty);
        //        InsertDict.Add(nameof(PermissionDetails.ObjectPath), Constants.DoubleQuote + path + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(PermissionDetails.Permissions), Constants.DoubleQuote + rule.FileSystemRights.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(PermissionDetails.UserName), Constants.DoubleQuote + rule.IdentityReference.Value + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(PermissionDetails.IsMoveToSP), false.ToString());
        //        cmd.CommandText = Helper.GetInsertQuery(Constants.SQLPermTblName, InsertDict);
        //        cmd.ExecuteNonQuery();
        //    }
        //}
        //private static long AddFileDToLiteDB(FileInfo file, SQLiteCommand cmd, long ParentFolderID)
        //{
        //    string FileName = file.Name;
        //    string FilePath = file.FullName;

        //    Console.WriteLine("Processing File\n" + FilePath);
        //    Dictionary<string, string> InsertDict = new Dictionary<string, string>();
        //    long result = 0;
        //    try
        //    {
        //        string FileOwnerName = file.GetAccessControl().GetOwner(typeof(NTAccount)).Value;
        //        InsertDict.Add(nameof(FilesDetails.CreatedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileCreatedDate), Constants.DoubleQuote + file.CreationTime.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileCreator), Constants.DoubleQuote + FileOwnerName + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileModifiedDate), Constants.DoubleQuote + file.LastWriteTime.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileModifier), Constants.DoubleQuote + FileOwnerName + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileName), Constants.DoubleQuote + FileName + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FilePath), Constants.DoubleQuote + FilePath + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileSizeKB), (file.Length / 1024).ToString());
        //        InsertDict.Add(nameof(FilesDetails.ModifiedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.Notes), string.Empty);
        //        InsertDict.Add(nameof(FilesDetails.ParentFolderID), ParentFolderID.ToString());
        //        string SQLInsertQr = Helper.GetInsertQuery(Constants.SQLFileTblName, InsertDict);
        //        Console.WriteLine("Insert Query\n" + SQLInsertQr);
        //        cmd.CommandText = SQLInsertQr;
        //        cmd.ExecuteNonQuery();
        //        result = cmd.Connection.LastInsertRowId;
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        InsertDict.Add(nameof(FilesDetails.CreatedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileName), Constants.DoubleQuote + FileName + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FilePath), Constants.DoubleQuote + FilePath + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.ModifiedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.ParentFolderID), ParentFolderID.ToString());
        //        string SQLInsertQr = Helper.GetInsertQuery(Constants.SQLFileTblName, InsertDict);
        //        Console.WriteLine("Insert Query\n" + SQLInsertQr);
        //        cmd.CommandText = SQLInsertQr;
        //        cmd.ExecuteNonQuery();

        //        AddIssueTbl(true, string.Empty, FileName, FilePath, cmd);
        //        result = -1;
        //    }
        //    catch (Exception ex)
        //    {
        //        InsertDict.Add(nameof(FilesDetails.CreatedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FileName), Constants.DoubleQuote + FileName + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.FilePath), Constants.DoubleQuote + FilePath + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.ModifiedDate), Constants.DoubleQuote + DateTime.Now.ToString() + Constants.DoubleQuote);
        //        InsertDict.Add(nameof(FilesDetails.ParentFolderID), ParentFolderID.ToString());
        //        string SQLInsertQr = Helper.GetInsertQuery(Constants.SQLFileTblName, InsertDict);
        //        Console.WriteLine("Insert Query\n" + SQLInsertQr);
        //        cmd.CommandText = SQLInsertQr;
        //        cmd.ExecuteNonQuery();

        //        AddIssueTbl(false, ex.Message+"\n"+ex.StackTrace, FileName, FilePath, cmd);
        //        result = -1;
        //    }
        //    return result;
        //}
        //private static void AddIssueTbl(bool IsPerm,string Notes,string ObjectName, string ObjectPath,SQLiteCommand cmd)
        //{
        //    Dictionary<string, string> IssueQuery = new Dictionary<string, string>();
        //    IssueQuery.Add(nameof(FoldersWithIssues.IsAccessError), IsPerm.ToString());
        //    IssueQuery.Add(nameof(FoldersWithIssues.Notes), Notes);
        //    IssueQuery.Add(nameof(FoldersWithIssues.ObjectName), Constants.DoubleQuote + ObjectName + Constants.DoubleQuote);
        //    IssueQuery.Add(nameof(FoldersWithIssues.ObjectPath), Constants.DoubleQuote + ObjectPath + Constants.DoubleQuote);

        //    cmd.CommandText = Helper.GetInsertQuery(Constants.SQLIssuesTblName, IssueQuery);
        //    cmd.ExecuteNonQuery();
        //}
    }
}
