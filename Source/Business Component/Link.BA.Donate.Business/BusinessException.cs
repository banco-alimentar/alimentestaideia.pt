using Microsoft.ApplicationInsights;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace Link.BA.Donate.Business
{
    public class BusinessException : Exception, ISerializable
    {

        public string ErrorMessage
        {
            get
            {
                return base.Message.ToString();
            }
        }

        public BusinessException()
        {
            // Add implementation.
        }

        public BusinessException(string message)
            : base(message)
        {
            // Add implementation.
        }

        public BusinessException(string message, Exception inner)
            : base(message, inner)
        {
            // Add implementation.
        }

        // This constructor is needed for serialization.
        protected BusinessException(SerializationInfo info, StreamingContext context)
        {
            // Add implementation.
        }

        public static void WriteExceptionToTrace(Exception exp)
        {
            string message = string.Format("[Exception:] {0}{1}[Inner Exception:] {2}", exp.Message, Environment.NewLine,
                                           ((exp.InnerException != null) ? exp.InnerException.Message : String.Empty));
            
            Trace.TraceError("Critical Trace " + message);
            //EventLog.WriteEntry("Application", "Critical Error " + message, EventLogEntryType.Error);
            /*
            EventLog eventLog = null;

            // make sure we have an event log
            if (!(EventLog.SourceExists("Banco Alimentar")))
            {
                EventLog.CreateEventSource("Banco Alimentar", "Application");
            }
            
            eventLog = new EventLog("Application") {Source = "Banco Alimentar"};

            // log the message
            eventLog.WriteEntry(string.Format("Critical Error {0}", message), EventLogEntryType.Error);

            //

            //throw new Exception(exp.Message, exp.InnerException);
            /*
            Trace.WriteLine(string.Format("[Exception:] {0}{1}[Inner Exception:] {2}", exp.Message, Environment.NewLine,
                                          ((exp.InnerException != null) ? exp.InnerException.Message : String.Empty)));
        
             */ 
        }
    }
}