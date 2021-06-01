using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationHelper
{
    public class Helper
    {
        public static string GetParseConfigValues(string key)
        {
            string ConfigValues = ConfigurationManager.AppSettings[key];
            if (ConfigValues != "")
            {
                StringBuilder ResultValue = new StringBuilder(ConfigValues);
                ResultValue.Replace(Constants.HtmlEntityAmp, Constants.Amp);
                ResultValue.Replace(Constants.HtmlEntitySingleQuote, Constants.SingleQuote);
                return ResultValue.ToString();
            }
            else
                return null;
        }
        public static string GetInsertQuery(string tableName, Dictionary<string, string> DictColVal)
        {
            StringBuilder ResultStr = new StringBuilder();
            ResultStr.Append("INSERT INTO " + tableName);
            ResultStr.Append("(" + string.Join(",", DictColVal.Keys) + ")");
            ResultStr.Append("Values(" + string.Join(",", DictColVal.Values) + ")");
            return ResultStr.ToString();
        }
        public static string GetSelectQuery(string TblName, string ColumnsName)
        {
            return "Select " + ColumnsName + " from " + TblName;
        }
        public static string GetSelectQuery(string TblName, string ColumnsName, string WhereCondition)
        {
            return "Select " + ColumnsName + " from " + TblName + " Where " + WhereCondition;
        }
        public static string GetConString(string DBFilePath)
        {
            StringBuilder ConStr = new StringBuilder();
            ConStr.Append("Data Source=" + DBFilePath + ";");
            ConStr.Append("Version=3;datetimeformat=CurrentCulture");
            return ConStr.ToString();
        }
        public static bool IsFileInvalid(string FileName, string ConfigKey, StringBuilder ResultMsg)
        {
            bool isInvalid = false;
            string notes = "File Not Uploaded Due To -";
            string blockFileLists = ConfigurationManager.AppSettings[ConfigKey];
            if (blockFileLists != "")
            {
                string Ext = Path.GetExtension(FileName);
                string[] blockFileTypes = blockFileLists.Split(',');
                isInvalid = blockFileTypes.Contains(Ext);
                if (isInvalid)
                {
                    ResultMsg.Append(notes);
                    ResultMsg.Append(" File Type " + Ext + " Block In SharePoint");
                }
                else
                    isInvalid = IsNameInvalid(FileName, ResultMsg);
            }
            //bool isFileNameBlockChar = FileName.IndexOfAny(GeneralValues.SPInvalidName) > -1;
            //if (isFileNameBlockChar)
            //{
            //    isInvalid = true;
            //    ResultMsg.Append("FileName Contain Invalid Charactor");
            //}
            return isInvalid;
        }

        public static bool IsNameInvalid(string Name, StringBuilder ResultMsg)
        {
            bool isInvalid = false;
            if (Name.IndexOfAny(GeneralValues.SPInvalidName) > -1)
            {
                isInvalid = true;
                ResultMsg.Append("-Name Contain Invalid Charactor from " + GeneralValues.SPInvalidName.ToString() + "-");
            }
            else if (Name.StartsWith(".") || Name.StartsWith("~"))
            {
                isInvalid = true;
                ResultMsg.Append("-Name Starts With . and ~ not allowed-");
            }
            else if (Name.EndsWith("."))
            {
                isInvalid = true;
                ResultMsg.Append("-Name Ends With . not allowed-");
            }
            else
            {
                Name = Name.ToLower();
                bool IsInvalidEnd = GeneralValues.SPInvalidEndsWith.Any(x => Name.EndsWith(x));
                if (IsInvalidEnd)
                {
                    isInvalid = true;
                    ResultMsg.Append("-Name Ends With something that is not allowed-");
                }
            }
            return isInvalid;
        }
        public static string GetUpdateQuery(string TblName, Dictionary<string, string> ColumnValues)
        {
            StringBuilder UpdateQuery = new StringBuilder();
            UpdateQuery.Append("Update " + TblName + " Set ");
            foreach (var item in ColumnValues)
            {
                UpdateQuery.Append(item.Key + "=" + item.Value + ",");
            }
            UpdateQuery.Remove(UpdateQuery.Length - 2, 1);
            return UpdateQuery.ToString();
        }
        public static string GetUpdateByIDQuery(string TblName, string ID, string ColumnName, string ColumnValue)
        {
            StringBuilder UpdateQuery = new StringBuilder();
            UpdateQuery.Append("Update " + TblName + " Set ");
            UpdateQuery.Append(ColumnName + "=" + ColumnValue);
            UpdateQuery.Append(" Where ");
            UpdateQuery.Append("ID=" + ID);
            return UpdateQuery.ToString();
        }
    }
    public class CommonListColumns
    {
        public const string Title = "Title";
        public const string Author = "Author";
        public const string Created = "Created";
        public const string Editor = "Editor";
        public const string Modified = "Modified";
    }
    public class GeneralValues
    {
        public const string Default_Account_BUILTIN = "BUILTIN";
        public const string Default_Account_NT_AUTHORITY = "NT AUTHORITY";
        public const string Default_Account_Creator_Owner = "CREATOR OWNER";
        public const string Default_Account_Everyone = "Everyone";

        public const string SPEveryoneGrpSID = "c:0(.s|true";
        public const string Percent20Val = "%20";

        public const char BackSlash = '\\';

        public static readonly char[] SPInvalidName = { '?', '<', '>', '#', '%', '/', '\\', '"', '*', ':', '|' };
        public static readonly string[] SPInvalidEndsWith = {".files" ,
                                                                "_files"  ,
                                                                "-dateien"    ,
                                                                "_fichiers"   ,
                                                                "_bestanden"  ,
                                                                "_file"   ,
                                                                "_archivos"   ,
                                                                "-filer"  ,
                                                                "_tiedostot"  ,
                                                                "_pliki"  ,
                                                                "_soubory"    ,
                                                                "_elemei" ,
                                                                "_ficheiros"  ,
                                                                "_arquivos"   ,
                                                                "_dosyalar"   ,
                                                                "_datoteke"   ,
                                                                "_fitxers"    ,
                                                                "_failid" ,
                                                                "_fails"  ,
                                                                "_bylos"  ,
                                                                "_fajlovi"    ,
                                                                "_fitxategiak"
                                                                };

        public static string MsgNotExistOnDest = "The permissions for '{0}' were not copied because '{0}' does not exist at the destination.";
        public static string MsgDenyPermission = "This file contains Deny permissions for {0} that are not supported by SharePoint. They will be ignored.";
        public const string MsgPermissionInherited = "Permission Inherited";
        public const string MsgFileNotExist = "{0}\nAbove File Path Doest Not Exist";
        public const string MsgFolderNotExist = "{0}\nAbove Folder Path Doest Not Exist";
        public static string MsgBlockFileType = "Unable to copy your file. This file type is blocked by SharePoint due to security risks.";
    }
    public class UserPermMapping
    {
        public string UserName { get; set; }
        public string PermStr { get; set; }
    }
    public class UpdateResult
    {
        public string TableName { get; set; }
        public string ItemID { get; set; }
        public bool IsSucess { get; set; }
    }
}
