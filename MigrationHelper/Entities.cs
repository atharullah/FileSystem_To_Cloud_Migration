using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationHelper
{
    public class FolderDetails
    {
        public int ID { get; set; }
        public string FolderName { get; set; }
        public string FolderPath { get; set; }
        public Nullable<int> SubFolderCount { get; set; }
        public Nullable<int> FilesCount { get; set; }
        public int FolderLevel { get; set; }
        public Nullable<int> ParentFolderID { get; set; }
        public System.DateTime FolderCreatedDate { get; set; }
        public System.DateTime FolderModifiedDate { get; set; }
        public string FolderCreator { get; set; }
        public string FolderModifier { get; set; }
        public int IsMoveToSP { get; set; }
        public string Notes { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> Modifieddate { get; set; }
    }
    public class FilesDetails
    {
        public int ID { get; set; }
        public int ParentFolderID { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int FileSizeKB { get; set; }
        public System.DateTime FileCreatedDate { get; set; }
        public System.DateTime FileModifiedDate { get; set; }
        public string FileCreator { get; set; }
        public string FileModifier { get; set; }
        public int IsMoveToSP { get; set; }
        public string Notes { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
    }
    public class PermissionDetails
    {
        public int ID { get; set; }
        public string ObjectPath { get; set; }
        public string UserName { get; set; }
        public bool IsAllow { get; set; }
        public string Permissions { get; set; }
        public bool IsInherited { get; set; }
        public bool IsFile { get; set; }
        public int IsMoveToSP { get; set; }
        public string Notes { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<System.DateTime> ModifiedDate { get; set; }
    }
    public class FoldersWithIssues
    {
        public int ID { get; set; }
        public string ObjectName { get; set; }
        public string ObjectPath { get; set; }
        public bool IsAccessError { get; set; }
        public string Notes { get; set; }
    }
    public class PermissionMapping
    {
        public int ID { get; set; }
        public string WinPermName { get; set; }
        public string SPPermName { get; set; }
    }
    public class FoldersMapping
    {
        public int ID { get; set; }
        public string SourceUrl { get; set; }
        public string DestWebUrl { get; set; }
        public string DestDocLibName { get; set; }
        public string DestDocLibRelUrl { get; set; }
        public string DBFilePath { get; set; }
    }
    public class TimeLog
    {
        public int ID { get; set; }
        public string SourceUrl { get; set; }
        public int Time { get; set; }
    }
    public class ReportTable
    {
        public int ID { get; set; }
        public string DBFileName { get; set; }
        public string FDFSPath { get; set; }
        public string Notes { get; set; }
    }
}
