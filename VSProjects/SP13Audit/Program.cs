using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2013.Audit
{
    internal class Program : Script
    {
        /// <summary>
        /// When we attempt to update the Audit flags on a site collection and fail, it is
        /// stored here so we don't attempt it again for a period of time.
        /// </summary>
        private Dictionary<Guid, DateTime> autoEnableTries = new Dictionary<Guid, DateTime>();

        /// <summary>
        /// This stores the date of the last time we polled for information.
        /// </summary>
        private SplunkCache splunkCache = null;

        /// <summary>
        /// Main entry point - it's only job is to start the class object
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>exit code</returns>
        public static int Main(string[] args)
        {
            try
            {
                return Run<Program>(args);
            }
            catch (ApplicationException ex)
            {
                SystemLogger.Write(LogLevel.Fatal, ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Parameter for storing the Interval between polls, measured in ms
        /// </summary>
        private int Interval { get; set; }

        /// <summary>
        /// Parameter for storing the AutoEnable setting - true for autoenable, false for don't
        /// </summary>
        private bool AutoEnable { get; set; }

        /// <summary>
        /// Storage for the check point directory
        /// </summary>
        public string CheckpointDirectory { get; set; }

        /// <summary>
        /// Returns the introspection scheme for this modular input (required function
        /// for a Splunk Modular Input).
        /// </summary>
        public override Scheme Scheme
        {
            get
            {
                return new Scheme
                {
                    Title = "SharePoint 2013 Audit Log",
                    Description = "Reads (and optionally enables) the audit log for all Sites/Webs within a SharePoint 2013 farm.  Enable this on only one server.",
                    StreamingMode = StreamingMode.Xml,
                    Endpoint =
                    {
                        Arguments = new List<Argument> {
                            new Argument {
                                Name = "interval",
                                Description = "Number of seconds to wait between polls of the SharePoint Audit Log (default: 15)",
                                DataType = DataType.Number,
                                Validation = "is_pos_int('interval')",
                                RequiredOnCreate = false,
                                RequiredOnEdit = false
                            },
                            new Argument {
                                Name = "autoenable",
                                Description = "Set to true to automatically enable auditing on all Sites in the SharePoint Farm (default: False)",
                                DataType = DataType.Boolean,
                                Validation = "is_bool('autoenable')",
                                RequiredOnCreate = false,
                                RequiredOnEdit = false
                            },
                        }
                    }
                };
            }
        }

        /// <summary>
        /// Given a specific configuration of this modular input, stream events to stdout as they
        /// are received.  This is the main processing loop for the Splunk Modular Input
        /// </summary>
        /// <param name="pInputDefinition">The configuration of the modular input</param>
        public override void StreamEvents(InputDefinition inputDefinition)
        {
            // Initialize the parameters we need
            Interval            = Utility.GetParameter(inputDefinition.Stanza, "interval", 15) * 1000;
            AutoEnable          = Utility.GetParameter(inputDefinition.Stanza, "autoenable", false);
            CheckpointDirectory = Utility.CheckpointDirectory(inputDefinition);

            // Create the backing file cache
            this.splunkCache = new SplunkCache(CheckpointDirectory, "sp13audit.csv");

            // Main loop
            using (EventStreamWriter writer = new EventStreamWriter())
            {
                while (true)
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        SPFarm localFarm = WaitForLocalFarm();
                        foreach (SPService service in localFarm.Services)
                        {
                            if (service is SPWebService)
                            {
                                foreach (SPWebApplication webApp in ((SPWebService)service).WebApplications)
                                {
                                    foreach (SPSite site in webApp.Sites)
                                    {
                                        if (AutoEnable && site.Audit.AuditFlags == SPAuditMaskType.None)
                                        {
                                            AutoEnableSiteAudit(site, localFarm);
                                        }
                                        ProcessSiteCollection(writer, site, localFarm);
                                    }
                                }
                            }
                        }
                    });
                    splunkCache.Save();
                    Thread.Sleep(Interval);
                }
            }
        }

        /// <summary>
        /// Handle the auto-enabling of the site audit.
        /// </summary>
        /// <param name="site">The Site Collection</param>
        /// <param name="localFarm">The Farm containing the Site Collection</param>
        private void AutoEnableSiteAudit(SPSite site, SPFarm localFarm)
        {
            if (autoEnableTries.ContainsKey(site.ID))
            {
                DateTime lastTry = autoEnableTries[site.ID];
                if (DateTime.Now < lastTry.AddDays(1))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("SP13Audit.AutoEnableSiteAudit: Result=Skip Farm={0} Site={1} Error=\"Too soon after last failure\"", localFarm.Id, site.ID));
                    return;
                }
            }
            SystemLogger.Write(LogLevel.Info, string.Format("SP13Audit.AutoEnableSiteAudit: Farm={0} Site={1} URL=\"{2}\"", localFarm.Id, site.ID, site.Url));
            try
            {
                site.Audit.AuditFlags = SPAuditMaskType.All;
                site.Audit.Update();
                if (autoEnableTries.ContainsKey(site.ID))
                {
                    // We were successful - don't need to monitor this again
                    autoEnableTries.Remove(site.ID);
                }
                SystemLogger.Write(LogLevel.Info, string.Format("SP13Audit.AutoEnableSiteAudit: Result=Success Farm={0} Site={1}", localFarm.Id, site.ID));
            }
            catch (SPException ex) 
            {
                SystemLogger.Write(LogLevel.Error, string.Format("SP13Audit.AutoEnableSiteAudit: Result=Failure Farm={0} Site={1} Error=\"{2}\"", localFarm.Id, site.ID, ex.Message));
                autoEnableTries.Add(site.ID, DateTime.Now);
            }
        }

        /// <summary>
        /// Process a single site collection for audit
        /// </summary>
        /// <param name="writer">The Event Output Stream</param>
        /// <param name="site">The Site Collection</param>
        /// <param name="webApp">The Web Application containing the Site Collection</param>
        /// <param name="service">The Service containing the Web Application</param>
        /// <param name="localFarm">The Farm containing the Service</param>
        private void ProcessSiteCollection(EventStreamWriter writer, SPSite site, SPFarm localFarm)
        {
            // Check to ensure we can audit this site collection
            SPAudit audit = site.Audit;
            if (audit.AuditFlags == SPAuditMaskType.None)
            {
                SystemLogger.Write(LogLevel.Debug, string.Format("SP13Audit.ProcessSiteCollection: Site={0} AuditFlags=None Action=Skip", site.ID));
            }
            else
            {
                SystemLogger.Write(LogLevel.Debug, string.Format("SP13Audit.ProcessSiteCollection: Site={0} AuditFlags={1} Action=Process", site.ID, audit.AuditFlags));
            }

            // Retrieve the list of audit entries since the last poll
            SPAuditEntryCollection auditEntries;
            DateTime currentPoll = DateTime.Now;
            if (splunkCache.ContainsKey(site.ID))
            {
                SPAuditQuery wssQuery = new SPAuditQuery(site);
                DateTime startDate = new DateTime(splunkCache[site.ID], DateTimeKind.Utc);
                wssQuery.SetRangeStart(startDate);
                wssQuery.SetRangeEnd(currentPoll);
                auditEntries = audit.GetEntries(wssQuery);
            }
            else
            {
                auditEntries = audit.GetEntries();
            }

            // Loop through each audit entry, convert to JSON and dump out of the event stream writer
            bool hasWritten = false;
            foreach (SPAuditEntry auditEntry in auditEntries)
            {
                var spAuditEntry = new SplunkAuditEntry(localFarm, site, auditEntry);
                writer.Write(new EventElement { Time = spAuditEntry.Occurred, Data = spAuditEntry.ToString() });
                hasWritten = true;
            }

            // Store the last poll time in the dictionary
            if (hasWritten)
                splunkCache.Update(site.ID, currentPoll.ToUniversalTime().Ticks);
        }

        /// <summary>
        /// Waits for the local farm to be available, then returns it.
        /// </summary>
        /// <returns>The Local SPFarm Object</returns>
        private SPFarm WaitForLocalFarm()
        {
            var iterations = 10;
            while (iterations > 0) {
                if (SPFarm.Local == null)
                {
                    SystemLogger.Write(LogLevel.Warn, "SP13Audit.WaitForLocalFarm: SPFarm.Local is not yet available (null)");
                    Thread.Sleep(10000); // Wait for 10 seconds;
                    iterations--;
                }
                else
                {
                    return SPFarm.Local;
                }
            }
            throw new ApplicationException("SP13Audit.WaitForLocalFarm: SPFarm.Local is not available");
        }
    }
}
