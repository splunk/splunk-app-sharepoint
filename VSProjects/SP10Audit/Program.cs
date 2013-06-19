using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2010.Audit
{
    internal class Program : Script
    {
        
        /// <summary>
        /// Main entry point - it's only job is to start the class object
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>exit code</returns>
        public static int Main(string[] args)
        {
            return Run<Program>(args);
        }

        /// <summary>
        /// Parameter for storing the Interval between polls, measured in ms
        /// </summary>
        private int Interval { get; set; }

        /// <summary>
        /// Parameter for storing the Interval between new SPSite checks for auto-audit enable, measured in ticks (see DateTime.Ticks)
        /// </summary>
        private long AutoEnableInterval { get; set; }

        /// <summary>
        /// Parameter for storing the Interval between content database checks, measured in ticks (see DateTime.Ticks)
        /// </summary>
        private long DBCheckInterval { get; set; }

        /// <summary>
        /// Parameter for storing the AutoEnable setting - true for autoenable, false for don't
        /// </summary>
        private bool AutoEnable { get; set; }

        /// <summary>
        /// Returns the introspection scheme for this modular input (required function
        /// for a Splunk Modular Input).
        /// </summary>
        public override Scheme Scheme
        {
	        get { 
                return new Scheme {
                    Title = "SharePoint 2010 Audit Log",
                    Description = "Reads (and optionally enables) the audit log for all Sites/Webs within a SharePoint 2010 farm.  Enable this on only one server.",
                    StreamingMode = StreamingMode.Xml,
                    Endpoint = {
                        Arguments = new List<Argument> {
                            new Argument {
                                Name = "interval",
                                Description = "Number of seconds to wait between polls of the SQL database (default: 15)",
                                DataType = DataType.Number,
                                Validation = "is_pos_int('interval')",
                                RequiredOnCreate = false,
                                RequiredOnEdit = false
                            },
                            new Argument {
                                Name = "autoenable",
                                Description = "Set to true to automatically enable auditing on all Sites in the SharePoint 2010 Farm (default: False)",
                                DataType = DataType.Boolean,
                                Validation = "is_bool('autoenable')",
                                RequiredOnCreate = false,
                                RequiredOnEdit = false
                            },
                            new Argument {
                                Name = "autoenableinterval",
                                Description = "Number of seconds between checking for new sites to auto-enable (default: 1 day)",
                                DataType = DataType.Number,
                                Validation = "is_pos_int('interval')",
                                RequiredOnCreate = false,
                                RequiredOnEdit = false
                            },
                            new Argument {
                                Name = "dbcheckinterval",
                                Description = "Number of seconds between checking for new content databases (default: 1 day)",
                                DataType = DataType.Number,
                                Validation = "is_pos_int('interval')",
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
        public override void StreamEvents(InputDefinition pInputDefinition)
        {
            // Obtain the settings we need to run the modular input
            // Take into account the defaults for each argument from
            // the introspection scheme.
            Stanza pStanza = pInputDefinition.Stanza;
            string sCurrentArgument;

            Interval = 15000;
            if (pStanza.SingleValueParameters.TryGetValue("interval", out sCurrentArgument)) {
                Interval = int.Parse(sCurrentArgument) * 1000;
                SystemLogger.Write(LogLevel.Info, string.Format("Polling Interval = {0} ms", Interval));
            } else {
                SystemLogger.Write(LogLevel.Info, "Polling Interval not specified - default = 15s");
            }


            AutoEnableInterval = (long)864000000000;
            if (pStanza.SingleValueParameters.TryGetValue("autoenableinterval", out sCurrentArgument)) {
                AutoEnableInterval = long.Parse(sCurrentArgument) * 10000000;
                SystemLogger.Write(LogLevel.Info, string.Format("AutoEnable Check Interval = {0} ms", AutoEnableInterval));
            } else {
                SystemLogger.Write(LogLevel.Info, "AutoEnable Check Interval not specified - default = 1d");
            }

            DBCheckInterval = (long)864000000000;
            if (pStanza.SingleValueParameters.TryGetValue("dbcheckinterval", out sCurrentArgument)) {
                DBCheckInterval = long.Parse(sCurrentArgument) * 10000000;
                SystemLogger.Write(LogLevel.Info, string.Format("Database Check Interval = {0} ms", DBCheckInterval));
            } else {
                SystemLogger.Write(LogLevel.Info, "Database Check Interval not specified - default = 1d");
            }

            AutoEnable = false;
            if (pStanza.SingleValueParameters.TryGetValue("autoenable", out sCurrentArgument)) {
                AutoEnable = sCurrentArgument.StartsWith("t", StringComparison.InvariantCultureIgnoreCase);
                SystemLogger.Write(LogLevel.Info, string.Format("AutoEnable = {0}", AutoEnable));
            } else {
                SystemLogger.Write(LogLevel.Info, "AutoEnable not specified - default = false");
            }

            // Verify that out checkpoint directory exists - create it if not.
            if (!Directory.Exists(pInputDefinition.CheckpointDirectory)) {
                SystemLogger.Write(LogLevel.Warn, string.Format("Directory {0} does not exist - creating it", pInputDefinition.CheckpointDirectory));
                Directory.CreateDirectory(pInputDefinition.CheckpointDirectory);
            }

            // Yes - goto statements are bad, mmm-kay.  However, in this case, we are having
            // to do a complete reset of the system.  This is only used when a complete reset
            // is required because the SharePoint farm or SQL Server has gone down.
            ENTRYPOINT:

            // Time stamps for the various long-term polling actions that we have to do.
            DateTime lastDBCheckPoll = DateTime.Now;
            DateTime lastAutoEnablePoll = DateTime.MinValue;

            // The current content database list
            // TODO: Capture SPExceptions and SQLExceptions as it would indicate that the farm or SQL Server is down or inaccessible
            SystemLogger.Write(LogLevel.Debug, "Initiating content database first-time discovery");
            AuditDatabaseCollection oAuditDatabaseCollection = new AuditDatabaseCollection(pInputDefinition.CheckpointDirectory);
            SystemLogger.Write(LogLevel.Debug, "Loading content database audit checkpoint files");
            oAuditDatabaseCollection.Load();

            SystemLogger.Write(LogLevel.Debug, "Entering Main Loop");
            using (EventStreamWriter writer = new EventStreamWriter())
            {
                while (true) 
                {
                    // Check to see if all SPSites are enabled, and enable the ones that are not.
                    if (AutoEnable) {
                        if ((DateTime.Now.Ticks - lastAutoEnablePoll.Ticks) > AutoEnableInterval)
                        {
                            SystemLogger.Write(LogLevel.Debug, "Initiating poll for auto-enabling all SPSites");
                            try
                            {
                                EnableAllSites();
                            }
                            catch (SPException ex)
                            {
                                SystemLogger.Write(LogLevel.Error, string.Format("SharePoint Farm Error: {0} - waiting for SharePoint Farm to be available", ex.Message));
                                WaitForSharePointFarm();
                                SystemLogger.Write(LogLevel.Warn, "Resetting Modular Input");
                                goto ENTRYPOINT;
                            }
                            lastAutoEnablePoll = DateTime.Now;
                        }
                    }

                    // Check for any new Content Databases, and add them to the list.
                    if ((DateTime.Now.Ticks - lastDBCheckPoll.Ticks) > DBCheckInterval)
                    {
                        SystemLogger.Write(LogLevel.Debug, "Initiating poll for content database discovery");
                        try
                        {
                            oAuditDatabaseCollection.Discover();
                        }
                        catch (SPException ex)
                        {
                            SystemLogger.Write(LogLevel.Error, string.Format("SharePoint Farm Error: {0} - waiting for SharePoint Farm to be available", ex.Message));
                            WaitForSharePointFarm();
                            SystemLogger.Write(LogLevel.Warn, "Resetting Modular Input");
                            goto ENTRYPOINT;
                        }
                        catch (SqlException ex)
                        {
                            SystemLogger.Write(LogLevel.Error, string.Format("SQL Server Error: {0} - discovery has been skipped for this day", ex.Message));
                        }
                        lastDBCheckPoll = DateTime.Now;
                    }

                    // Poll each content database for new audit data
                    foreach (var oAuditDatabase in oAuditDatabaseCollection)
                    {
                        SystemLogger.Write(LogLevel.Debug, string.Format("Processing database {0}", oAuditDatabase.Key));
                        try
                        {
                            List<AuditRecord> lAuditEntries = oAuditDatabase.Value.GetLatestEntries();
                            SystemLogger.Write(LogLevel.Debug, string.Format("Found {0} audit entries", lAuditEntries.Count));
                            foreach (AuditRecord oAuditRecord in lAuditEntries)
                            {
                                writer.Write(new EventElement
                                {
                                    Data = oAuditRecord.ToLogString(true),
                                    Time = DateTime.SpecifyKind(oAuditRecord.Occurred, DateTimeKind.Utc)
                                });
                            }
                            // Save current state to the checkpoint file
                            oAuditDatabase.Value.Save();
                        }
                        catch (SqlException ex)
                        {
                            SystemLogger.Write(LogLevel.Error, string.Format("SQL Server Error: {0} - audit log reading has been skipped for this poll", ex.Message));
                        }
                    }

                    // Sleep for the duration of the poll interval
                    Thread.Sleep(Interval);

                } // End of main loop
            } // end of using() clause
        } // End of StreamEvents() method

        /// <summary>
        /// Enables any sites that have not had audit enabled.  If the audit specification is set,
        /// then we don't reset it.  Thus, if you have a site that has audit set to just updates,
        /// for instance, you will never see views.
        /// </summary>
        private void EnableAllSites()
        {
            SystemLogger.Write(LogLevel.Debug, "EnableAllSites: Starting SPSecurity.RunWithElevatedPrivileges");
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                SystemLogger.Write(LogLevel.Debug, "EnableAllSites: Looping over all SPServices");
                foreach (SPService oService in SPFarm.Local.Services)
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EnableAllSites: Handling Service {0} (Type {1})", oService.Id, oService.GetType().ToString()));
                    // Skip Central Administration service - we are not interested in that
                    if (oService is SPWebService && !oService.TypeName.Equals("Central Administration"))
                    {
                        SystemLogger.Write(LogLevel.Debug, string.Format("EnableAllSites: Service {0} is a SPWebervice - looping over Web Applications", oService.Id));
                        foreach (SPWebApplication oWebApp in ((SPWebService)oService).WebApplications)
                        {
                            SystemLogger.Write(LogLevel.Debug, string.Format("EnableAllSites: WebApplication {0}: {1}", oWebApp.Id, oWebApp.DisplayName));
                            foreach (SPSite oSite in oWebApp.Sites)
                            {
                                SystemLogger.Write(LogLevel.Debug, string.Format("EnableAllSites: SPSite {0}: {1}", oSite.ID, oSite.Url));
                                foreach (SPWeb oWeb in oSite.AllWebs)
                                {
                                    SystemLogger.Write(LogLevel.Debug, string.Format("EnableAllSites: SPWeb {0}: {1}", oWeb.ID, oWeb.Name));
                                    if (oWeb.Audit.AuditFlags == SPAuditMaskType.None)
                                    {
                                        SystemLogger.Write(LogLevel.Info, string.Format("Enabling Full Audit on Web {0}: {1}", oWeb.ID, oWeb.Title));
                                        oWeb.Audit.AuditFlags = SPAuditMaskType.All;
                                        oWeb.Audit.Update();
                                    }
                                } // Web
                            } // Site
                        } // WebApplication
                    } // WebService
                } // Service
            }); // Farm
        }

        /// <summary>
        /// When the SharePoint farm is down (either because the SQL Server with the config database has gone
        /// down or permissions have been removed or the sharepoint services have been stopped), then we need
        /// to wait around for them to come up.  We try to access the SPFarm in elevated permissions until
        /// we get an answer.  For each poll, we will output an error stating that the SharePoint farm is
        /// not available.
        /// </summary>
        private void WaitForSharePointFarm()
        {
            while (true)
            {
                SystemLogger.Write(LogLevel.Debug, "Polling for SharePoint to be alive");
                try
                {
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        if (SPFarm.Local == null)
                        {
                            SystemLogger.Write(LogLevel.Warn, "SPFarm.Local is not yet available (null)");
                        }
                        else
                        {
                            SystemLogger.Write(LogLevel.Info, "WaitForSharePointFarm - farm is available again");
                            return;
                        }
                    });
                }
                catch (SPException ex)
                {
                    SystemLogger.Write(LogLevel.Warn, string.Format("SharePoint is unavailable (SPException: {0})", ex.Message));
                }
                // Sleep for 60 seconds - the waiting period
                Thread.Sleep(60000);
            }
        }

    } // End of Class "Program"
} // End of Namespace
