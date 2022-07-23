using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FerrumGateService.Helper
{
    /// <summary>
    /// static log class, that logs to EventLog
    /// </summary>
    static class Logger
    {
        static EventLog eventlog=null;
        static Logger()
        {
            #if !DEBUG
            try
            {
                eventlog = new EventLog();
                if (!EventLog.SourceExists(Source))
                {
                    EventLog.CreateEventSource(Source, Log);
                }

                eventlog.Source = Source;

                eventlog.Log = Log;
            }catch(Exception ignored)
            {
                Console.WriteLine(ignored.ToString());
            }
            #endif
        }

        public const string Source = "FerrumGate";
        public const string Log = "";
        internal static void Error(String msg)
        {
            try
            {
                if (msg != null)
                {
                    Console.WriteLine(msg);
                    if (eventlog != null)
                        eventlog.WriteEntry(msg, EventLogEntryType.Error);
                }
            }catch(Exception ignored)
            {
                Console.WriteLine(ignored.ToString());
            }
                
           
        }


        internal static void Info(String msg)
        {
            try
            {
                if (msg != null)
                {
                    Console.WriteLine(msg);
                    if (eventlog != null)
                        eventlog.WriteEntry(msg, EventLogEntryType.Information);
                }
            }catch(Exception ignored)
            {
                Console.WriteLine(ignored.ToString());
            }
          
        }

        internal static void Debug(String msg)
        {
            Console.WriteLine(msg);
        }
    }
}
