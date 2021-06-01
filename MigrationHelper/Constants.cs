using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationHelper
{
    public class Constants
    {
        public static readonly string HtmlEntityAmp = "&amp;";
        public static readonly string HtmlEntitySingleQuote = "&apos;";
        public static readonly string Amp = "&";
        public static readonly string SingleQuote = "'";

        public static readonly char BackSlash = '\\';
        public static readonly string DoubleQuote = "\"";

        public static string SQLPermTblName = "PermissionDetails";
        public static string SQLFolderTblName = "FolderDetails";
        public static string SQLFileTblName = "FilesDetails";
        public static string SQLIssuesTblName = "FoldersWithIssues";
    }
}
