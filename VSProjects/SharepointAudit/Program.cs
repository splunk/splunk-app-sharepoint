using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

using Splunk.ModularInputs;


namespace Splunk.Sharepoint.Audit
{
    internal class Program : Script
    {
        /// <summary>
        ///     Name of the Checkpoint File
        /// </summary>
        private string CheckpointFile = null;

        /// <summary>
        ///     Checkpoint Data Set
        /// </summary>
        private CheckpointData DataStore = null;

        /// <summary>
        ///     Value of the auto-enable flag
        /// </summary>
        private bool AutoEnable = false;

        /// <summary>
        ///     The executable entry point
        /// </summary>
        /// <param name="args">Command Line Arguments</param>
        /// <returns>Exit Code</returns>
        public static int Main(string[] args)
        {
            return Run<Program>(args);
        }
        
        /// <summary>
        ///     Returns the introspection scheme for this object
        /// </summary>
        public override Scheme Scheme
        {
            get 
            {
                return new Scheme
                {
                    Title = "Sharepoint Audit Receiver",
                    Description = "Utilizes the Sharepoint API to retrieve audit log entries from the Sharepoint Database.",
                    StreamingMode = StreamingMode.Xml,
                    Endpoint =
                    {
                        Arguments = new List<Argument> {
                            new Argument {
                                Name = "interval",
                                Description = "Number of seconds to wait between checks of the database",
                                DataType = DataType.Number,
                                Validation = "is_pos_int('interval')",
                                RequiredOnCreate = false
                            },
                            new Argument {
                                Name = "autoenable",
                                Description = "Set to true if enabling of audit is automatic",
                                DataType = DataType.Boolean,
                                Validation = "is_bool('autoenable')",
                                RequiredOnCreate = false
                            },
                        }
                    }
                };
            }
        }

        /// <summary>
        ///     Stream events into stdout
        /// </summary>
        /// <param name="inputDefinition">Input Definition from Splunk</param>
        public override void StreamEvents(InputDefinition inputDefinition)
        {
            using (var writer = new EventStreamWriter())
            {
                var stanza = inputDefinition.Stanza;

                // Work out the interval parameter
                var interval = 5000;        // Default of 5 seconds
                string intervalParam;
                if (stanza.SingleValueParameters.TryGetValue("interval", out intervalParam))
                {
                    interval = int.Parse(intervalParam) * 1000;
                }
                SystemLogger.Write(string.Format("Polling interval is {0} ms", interval));

                // Work out the autoenable parameter
                string autoEnableParam;
                if (stanza.SingleValueParameters.TryGetValue("autoenable", out autoEnableParam))
                {
                    if (autoEnableParam.StartsWith("t", StringComparison.InvariantCultureIgnoreCase))
                        AutoEnable = true;
                }

                // Determine if we are on Central Administration host and bail if we aren't.
                if (!IsCentralAdministration())
                {
                    SystemLogger.Write(LogLevel.Fatal, string.Format("Central Administration not available - not running audit collection services"));
                    Environment.Exit(1);
                }

                // Initialize the Checkpoint directory (if it doesn't exist - it should do)
                if (!Directory.Exists(inputDefinition.CheckpointDirectory))
                {
                    SystemLogger.Write(LogLevel.Warn, string.Format("Creating checkpoint Directory: {0}", inputDefinition.CheckpointDirectory));
                    Directory.CreateDirectory(inputDefinition.CheckpointDirectory);
                }
                CheckpointFile = Path.Combine(inputDefinition.CheckpointDirectory, @"audit.dat");
                DataStore = new CheckpointData(CheckpointFile);
                
                // Main loop
                while (true)
                {
                    try {
                        SPSecurity.RunWithElevatedPrivileges(delegate()
                        {
                            PollSharepointAudit(writer);
                        });
                    } catch (Exception ex) {
                        SystemLogger.Write(LogLevel.Error, string.Format("Exception {0}: {1}", ex.GetType().ToString(), ex.Message));
                    }
                    DataStore.SaveData();
                    Thread.Sleep(interval);
                }
            }
        }

        /// <summary>
        ///     Determine if we are running on a central administration site
        /// </summary>
        /// <returns>True if we are</returns>
        public bool IsCentralAdministration()
        {
            bool returnValue = false;

            try
            {
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    // Print out our Security Principal
                    SystemLogger.Write(LogLevel.Info, string.Format("Windows Identity is \"{0}\"", System.Security.Principal.WindowsIdentity.GetCurrent().Name));

                    // Check to see if the farm is accessible
                    if (SPFarm.Local == null)
                    {
                        SystemLogger.Write(LogLevel.Fatal, "Cannot read SPFarm.Local (null) - bailing out");
                        Environment.Exit(2);
                    }

                    // Check to see if we are running Central Administration
                    foreach (SPServiceInstance svc in SPServer.Local.ServiceInstances)
                    {
                        if (!svc.TypeName.Equals("Central Administration"))
                            continue;
                        if (svc.Status == SPObjectStatus.Online)
                            returnValue = true;
                    }
                });
                return returnValue;
            }
            catch (Exception ex)
            {
                SystemLogger.Write(LogLevel.Error, string.Format("Exception {0}: {1}", ex.GetType().ToString(), ex.Message));
                return false;
            }
        }

        /// <summary>
        ///     Poll the Sharepoint Audit log once, writing out any new records.
        /// </summary>
        /// <param name="writer">The event stream to write to</param>
        /// <param name="inputDefinition">The input definition from Splunk</param>
        public void PollSharepointAudit(EventStreamWriter writer)
        {
            SPFarm farm = SPFarm.Local;
            SPWebService service = farm.Services.GetValue<SPWebService>("");


            foreach (SPWebApplication webapp in service.WebApplications)
            {
                foreach (SPSite site in webapp.Sites)
                {
                    SPServiceContext context = SPServiceContext.GetContext(site);
                    foreach (SPWeb web in site.AllWebs)
                    {
                        if (AutoEnable && web.Audit.AuditFlags == SPAuditMaskType.None)
                        {
                            web.Audit.AuditFlags = SPAuditMaskType.All;
                            web.Audit.Update();
                        }
                        if (web.Audit.AuditFlags == SPAuditMaskType.None) {
                            SystemLogger.Write(LogLevel.Debug, string.Format("Skipping web {0}: Audit not configured", web.Name));
                        } else {
                            SystemLogger.Write(LogLevel.Debug, string.Format("Processing SPWeb {0}", web.ID.ToString()));
                            PollSharepointWeb(writer, site, web);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Poll the sharepoint audit log for a specific web inside a specific site
        /// </summary>
        /// <param name="writer">The event stream to write to</param>
        /// <param name="site">The SPSite to handle</param>
        /// <param name="web">The SPWeb to handle</param>
        public void PollSharepointWeb(EventStreamWriter writer, SPSite site, SPWeb web)
        {
            SPAuditQuery auditQuery = new SPAuditQuery(site);
            auditQuery.SetRangeStart(DataStore.GetTimestamp(web.ID));
            auditQuery.SetRangeEnd(DateTime.Now);
            DateTime lastPoll = DateTime.Now;

            SPAuditEntryCollection auditLog = web.Audit.GetEntries(auditQuery);
            foreach (SPAuditEntry entry in auditLog)
            {
                OutputEntry(writer, web, entry);
            }

            DataStore.SetTimestamp(web.ID, lastPoll);
        }

        public void OutputEntry(EventStreamWriter writer, SPWeb web, SPAuditEntry entry)
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine(string.Format("HashCode:{0}", entry.GetHashCode()));
            sb.AppendLine(string.Format("Event=\"{0}\"", entry.Event.ToString()));
            sb.AppendLine(string.Format("EventName=\"{0}\"", entry.EventName));
            sb.AppendLine(string.Format("DocLocation=\"{0}\"", entry.DocLocation));
            sb.AppendLine(string.Format("EventSource=\"{0}\"", entry.EventSource.ToString()));
            sb.AppendLine(string.Format("ItemId=\"{0}\"", entry.ItemId.ToString()));
            sb.AppendLine(string.Format("ItemType=\"{0}\"", entry.ItemType.ToString()));
            sb.AppendLine(string.Format("LocationType=\"{0}\"", entry.LocationType.ToString()));
            // KB939246 - MachineIP and MachineName as disabled by design (weak)
            //sb.AppendLine(string.Format("MachineIP=\"{0}\"", entry.MachineIP));
            //sb.AppendLine(string.Format("MachineName=\"{0}\"", entry.MachineName));
            sb.AppendLine(string.Format("SiteId=\"{0}\"", entry.SiteId.ToString()));
            sb.AppendLine(string.Format("SourceName=\"{0}\"", entry.SourceName));
            sb.AppendLine(string.Format("UserId=\"{0}\"", entry.UserId.ToString()));
            try
            {
                SPUser user = web.Users.GetByID(entry.UserId);
                sb.AppendLine(string.Format("Username=\"{0}\"", user.LoginName));
            }
            catch (SPException ex)
            {
                if (!ex.Message.StartsWith("User cannot be found", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw ex;
                }
            }
            sb.AppendLine(string.Format("EventData:{0}", entry.EventData));

            EventElement eventElement = new EventElement
            {
                Time = entry.Occurred,
                Data = sb.ToString()
            };
            writer.Write(eventElement);
        }

        

    }
}
