using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MigrationHelper
{
    public class Log
    {
        //Both Should Be Same Otherwise Need to run once with admin UAC popup then work
        private static string AppName = "EG Migration Tool";
        private static string SourceName = "EG Migration Tool";
        public static void WriteToEventViewer(Exception ex)
        {
            try
            {
                using (EventLog log = new EventLog(AppName))
                {
                    log.Source = SourceName;
                    log.WriteEntry(ex.ToString(), EventLogEntryType.Error, 333);
                }
            }
            catch (Exception)
            {
            }
        }
        public static void WriteToEventViewer(string msg)
        {
            try
            {
                using (EventLog log = new EventLog(AppName))
                {
                    log.Source = SourceName;
                    log.WriteEntry(msg, EventLogEntryType.Information, 333);
                }
            }
            catch (Exception)
            {
            }
        }
        public static void WriteToEventViewer(string msg, EventLogEntryType type)
        {
            try
            {
                using (EventLog log = new EventLog(AppName))
                {
                    log.Source = SourceName;
                    log.WriteEntry(msg, type, 333);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
