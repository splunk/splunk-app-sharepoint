using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Diagnostics;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Diagnostics;

using Splunk.ModularInputs;
using Microsoft.SharePoint.MobileMessage;

namespace Splunk.SharePoint2013.Inventory
{
    internal class Program : Script
    {
        /// <summary>
        /// Main entry point - it's only job is to start the class object
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>the exit code</returns>
        static int Main(string[] args)
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
        /// Storage for the check point directory
        /// </summary>
        public string CheckpointDirectory { get; set; }

        /// <summary>
        /// Parameter for storing the cache object
        /// </summary>
        public SplunkCache Cache { get; set; }

        /// <summary>
        /// Number of errors in this run.
        /// </summary>
        public int ErrorCount { get; set; }


        /// <summary>
        /// Returns the introspection scheme for this modular input (required function
        /// for a Splunk Modular Input).
        /// </summary>
        public override Scheme Scheme
        {
            get {
                return new Scheme
                {
                    Title = "SharePOint 2013 Inventory Reporter",
                    Description = "Reads the inventory for all Sites/Webs within a SharePoint 2013 Farm.  Enable this on only one server.",
                    StreamingMode = StreamingMode.Xml,
                    Endpoint =
                    {
                        Arguments = new List<Argument> {
                            new Argument {
                                Name = "poll_interval",
                                Description = "Number of seconds to wait between checks of the Inventory (default: 86400)",
                                DataType = DataType.Number,
                                Validation = "is_pos_int('poll_interval')",
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
        /// are received.  This is the main processing loop for the Splunk Modular Input.
        /// </summary>
        /// <param name="inputDefinition">The Input Definition Object</param>
        public override void StreamEvents(InputDefinition inputDefinition)
        {
            Interval = Utility.GetParameter(inputDefinition.Stanza, "poll_interval", 86400);
            CheckpointDirectory = Utility.CheckpointDirectory(inputDefinition);

            Cache = new SplunkCache(CheckpointDirectory, "inventory.txt");
            
            using (EventStreamWriter writer = new EventStreamWriter())
            {
                while (true)
                {
                    Int64 startTime = DateTime.Now.Ticks;
                    SPSecurity.RunWithElevatedPrivileges(delegate()
                    {
                        if (SPFarm.Local == null)
                        {
                            SystemLogger.Write(LogLevel.Fatal, "SPFarm.Local is null - try adding the user to the SPShellAdmin list");
                            System.Environment.Exit(42);
                        }

                        // SPFarm
                        EmitFarm(writer, SPFarm.Local);
    
                        // SPAlternateUrlCollectionManager
                        EmitAlternateUrlCollectionManager(writer, SPFarm.Local, SPFarm.Local.AlternateUrlCollections);

                        // SPServer
                        EmitServers(writer, SPFarm.Local, SPFarm.Local.Servers);

                        // SPServiceInstance
                        foreach (var server in SPFarm.Local.Servers)
                        {
                            EmitServiceInstances(writer, SPFarm.Local, server);
                        }

                        // SPDiagnosticsProvider
                        EmitDiagnosticsProviders(writer, SPFarm.Local, SPFarm.Local.DiagnosticsProviders);

                        // SPFeatureDefinition
                        EmitFeatureDefinitions(writer, SPFarm.Local, SPFarm.Local.FeatureDefinitions);

                        // In SharePoint 2010, the SPWebTemplate objects are not contained within a specific site.
                        // In SharePoint 2013, they are, so we must iterate over the Web Sites to get the list
                        Dictionary<string,SPWebTemplate> webTemplates = new Dictionary<string,SPWebTemplate>();
                        List<SPWebApplication> webApplications = new List<SPWebApplication>();
                        Dictionary<Guid, SPApplicationPool> applicationPools = new Dictionary<Guid, SPApplicationPool>();
                        foreach (var service in SPFarm.Local.Services)
                        {
                            if (service is SPWebService)
                            {
                                foreach (var webApplication in ((SPWebService)service).WebApplications)
                                {
                                    // Build the list of Web Applications
                                    webApplications.Add(webApplication);

                                    // Build the list of Application Pools
                                    applicationPools[webApplication.Id] = webApplication.ApplicationPool;

                                    EmitWebAppFeatures(writer, SPFarm.Local, webApplication.Id, webApplication.Features);
                                    EmitWebAppPolicies(writer, SPFarm.Local, webApplication.Id, webApplication.Policies);
                                    EmitWebAppPrefixes(writer, SPFarm.Local, webApplication.Id, webApplication.Prefixes);
                                    EmitWebAppContentDatabases(writer, SPFarm.Local, webApplication.Id, webApplication.ContentDatabases);
                                    EmitWebAppSites(writer, SPFarm.Local, webApplication.Id, webApplication.Sites);

                                    foreach (SPSite site in webApplication.Sites)
                                    {
                                        try
                                        {
                                            foreach (SPWebTemplate webTemplate in site.RootWeb.GetAvailableWebTemplates((uint)site.RootWeb.Locale.LCID))
                                            {
                                                webTemplates[webTemplate.Name] = webTemplate;
                                            }

                                            EmitSiteWebs(writer, SPFarm.Local, webApplication.Id, site.ID, site.AllWebs);
                                            foreach (SPWeb web in site.AllWebs)
                                            {
                                                EmitWebUsers(writer, SPFarm.Local, webApplication.Id, site.ID, web.ID, web.AllUsers);
                                                EmitWebLists(writer, SPFarm.Local, webApplication.Id, site.ID, web, web.Lists);
                                            }
                                        }
                                        catch (SqlException sqlException)
                                        {
                                            SplunkEmitter emitter = new SplunkEmitter { CacheType = CacheType.Error, Timestamp = DateTime.Now };
                                            emitter.Add("Location", "Site");
                                            emitter.Add("SiteId", site.ID.ToString());
                                            emitter.Add("WebApplicationId", webApplication.Id.ToString());
                                            emitter.Add("FarmId", SPFarm.Local.Id.ToString());
                                            emitter.Add("Exception", sqlException.GetType().FullName);
                                            emitter.Add("Code", sqlException.ErrorCode.ToString("X"));
                                            emitter.Add("Message", Utility.Quotable(sqlException.Message));
                                            writer.Write(emitter.ToEmitter());
                                            ErrorCount++;
                                        }
                                    }
                                }
                            }
                        }

                        EmitWebTemplates(writer, SPFarm.Local, webTemplates);
                        EmitWebApplications(writer, SPFarm.Local, webApplications);
                        EmitApplicationPools(writer, SPFarm.Local, applicationPools);
                    });
                    Int64 endTime = DateTime.Now.Ticks;
                    Cache.Save();
                    EmitDebugInformation((endTime - startTime)/10000);  // ticks is # 100ns chunks, 10000 per ms
                    Thread.Sleep(Interval * 1000);
                }
            }
        }

        /// <summary>
        /// We want to emit some debug information occassionally to the splunkd.log so that we can
        /// diagnose problems.
        /// </summary>
        /// <param name="timeTaken">Number of seconds the process took</param>
        private void EmitDebugInformation(Int64 timeTaken)
        {
            List<string> debugInfo = new List<string>();
            debugInfo.Add("modinput=\"SP13Inventory\"");
            debugInfo.Add(string.Format("pid={0}", Process.GetCurrentProcess().Id));
            debugInfo.Add(string.Format("mem={0} bytes", Process.GetCurrentProcess().PrivateMemorySize64));
            debugInfo.Add(string.Format("time={0} ms", timeTaken));
            debugInfo.Add(string.Format("cache={0} objects", Cache.Count));
            debugInfo.Add(string.Format("errors={0}", ErrorCount));
            SystemLogger.Write(LogLevel.Debug, string.Join(",",debugInfo));
        }

        #region SPFarm
        /// <summary>
        /// Emit an event for the farm, but only if it has changed.
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        private void EmitFarm(EventStreamWriter writer, SPFarm localFarm)
        {
            var id   = localFarm.Id.ToString();
            var hash = Converter.ToHash(localFarm);
            var chk  = Converter.ToChecksum(hash);
            var type = CacheType.Farm;

            if (!Cache.IsUpdated(type, id, chk))
                return;
            
            SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
            emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
            foreach (var pair in hash)
            {
                emitter.Add(pair.Key, pair.Value);
            }
            writer.Write(emitter.ToEmitter());

            var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
            Cache[newCache.Type, newCache.Id] = newCache;
        }
        #endregion

        #region SPAlternateUrls
        /// <summary>
        /// Emit the AlternateUrl collections
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local Farm</param>
        /// <param name="alternateUrlCollections">The AlternateUrl Collections</param>
        private void EmitAlternateUrlCollectionManager(EventStreamWriter writer, SPFarm localFarm, SPAlternateUrlCollectionManager alternateUrlCollections)
        {
            List<string> current = new List<string>();
            var type = CacheType.AlternateUrl;

            foreach (SPAlternateUrlCollection alternateUrlCollection in alternateUrlCollections)
            {
                foreach (SPAlternateUrl alternateUrl in alternateUrlCollection)
                {
                    var id = alternateUrl.Uri.ToString();
                    var hash = Converter.ToHash(alternateUrl);
                    var chk = Converter.ToChecksum(hash);

                    if (!Cache.IsUpdated(type, id, chk))
                        return;

                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                    foreach (var pair in hash)
                    {
                        emitter.Add(pair.Key, pair.Value);
                    }
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());

                    var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                    Cache[newCache.Type, newCache.Id] = newCache;

                    current.Add(id);
                }
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitAlternateUrlCollectionManager: Deleting AlternateUrl {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Uri", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPServers
        /// <summary>
        /// Emit the list of SharePoint Servers
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="serverCollection">The list of servers</param>
        private void EmitServers(EventStreamWriter writer, SPFarm localFarm, SPServerCollection serverCollection)
        {
            List<string> current = new List<string>();
            var type = CacheType.Server;

            foreach (SPServer server in serverCollection)
            {
                var id = server.Id.ToString();
                var hash = Converter.ToHash(server);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id);
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitServers: Deleting Server {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPServiceInstances
        /// <summary>
        /// Emit the service instance changes that are on a specific server in a specific farm
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="server">The server being considered</param>
        private void EmitServiceInstances(EventStreamWriter writer, SPFarm localFarm, SPServer server)
        {
            List<string> current = new List<string>();
            var type = CacheType.ServiceInstance;

            foreach (SPServiceInstance service in server.ServiceInstances)
            {
                var id = service.Id.ToString();
                var hash = Converter.ToHash(service);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id);
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitServiceInstances: Deleting ServiceInstance {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPDiagnosticsProviders
        /// <summary>
        /// Emit the enabled diagnostics providers
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="diagProviders">The list of diag providers in the farm</param>
        private void EmitDiagnosticsProviders(EventStreamWriter writer, SPFarm localFarm, SPDiagnosticsProviderCollection diagProviders)
        {
            List<string> current = new List<string>();
            var type = CacheType.DiagnosticsProvider;

            foreach (var diagProvider in diagProviders)
            {
                var id = diagProvider.Id.ToString();
                var hash = Converter.ToHash(diagProvider);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id);
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitDiagnosticsProviders: Deleting DiagnosticsProvider {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPFeatureDefinitions
        /// <summary>
        /// Emit the feature definition list
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="featureDefinitions">The list of feature definitions</param>
        private void EmitFeatureDefinitions(EventStreamWriter writer, SPFarm localFarm, SPFeatureDefinitionCollection featureDefinitions)
        {
            List<string> current = new List<string>();
            var type = CacheType.FeatureDefinition;

            foreach (var featureDefinition in featureDefinitions)
            {
                var id = featureDefinition.Id.ToString();
                var hash = Converter.ToHash(featureDefinition);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id);
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitFeatureDefinitions: Deleting FeatureDefinition {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPWebTemplates
        /// <summary>
        /// Emit the list of web templates
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="webTemplates">The unique list of web templates</param>
        private void EmitWebTemplates(EventStreamWriter writer, SPFarm localFarm, Dictionary<string,SPWebTemplate> webTemplates)
        {
            List<string> current = new List<string>();
            var type = CacheType.WebTemplate;

            foreach (var webTemplate in webTemplates.Values)
            {
                var id = webTemplate.Name.ToString();
                var hash = Converter.ToHash(webTemplate);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id);
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebTemplates: Deleting WebTemplate {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Name", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPWebApplications
        /// <summary>
        /// Emit the list of web applications
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="webApplications">The web application list</param>
        private void EmitWebApplications(EventStreamWriter writer, SPFarm localFarm, List<SPWebApplication> webApplications)
        {
            List<string> current = new List<string>();
            var type = CacheType.WebApplication;

            foreach (var webApplication in webApplications)
            {
                var id = webApplication.Id.ToString();
                var hash = Converter.ToHash(webApplication);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id);
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebApplications: Deleting WebApplication {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region SPApplicationPools
        /// <summary>
        /// Emit the list of application pools
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="applicationPools">The list of application pools</param>
        private void EmitApplicationPools(EventStreamWriter writer, SPFarm localFarm, Dictionary<Guid, SPApplicationPool> applicationPools)
        {
            List<string> current = new List<string>();
            var type = CacheType.ApplicationPool;

            foreach (var ap in applicationPools)
            {
                var id = ap.Value.Id.ToString();
                var hash = Converter.ToHash(ap.Value);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, id, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, id) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("WebApplicationId", ap.Key.ToString());
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = id, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(ap.Value.Id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                if (!current.Contains(old))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitApplicationPools: Deleting ApplicationPool {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region WebApp-SPContentDatabases
        /// <summary>
        /// Emit a list of SharePoint Content Databases
        /// </summary>
        /// <param name="writer">The Event Stream</param>
        /// <param name="localFarm">The Local Farm</param>
        /// <param name="webAppId">The WebApp ID</param>
        /// <param name="databases">The List of Databases</param>
        private void EmitWebAppContentDatabases(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, SPContentDatabaseCollection databases)
        {
            List<string> current = new List<string>();
            var type = CacheType.ContentDatabase;

            foreach (SPContentDatabase database in databases)
            {
                var id = database.Id.ToString();
                var cacheId = string.Format("{0}/{1}", webAppId.ToString(), id);
                var hash = Converter.ToHash(webAppId, database);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;
                
                current.Add(database.Id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId/DatabaseId
                string[] cacheElements = old.Split('/');
                if (cacheElements[0].Equals(webAppId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebAppContentDatabses: Deleting WebApp Content Database {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", old);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region WebApp-SPFeatures
        /// <summary>
        /// Emit a collection of web application features
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="webAppId">The web application ID</param>
        /// <param name="features">The feature set</param>
        private void EmitWebAppFeatures(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, SPFeatureCollection features)
        {
            List<string> current = new List<string>();
            var type = CacheType.Feature;

            foreach (var feature in features)
            {
                var id = feature.DefinitionId.ToString();
                var cacheId = string.Format("{0}/{1}", webAppId.ToString(), id);
                var hash = Converter.ToHash(webAppId, feature);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId/DefinitionId
                string[] cacheElements = old.Split('/');
                if (cacheElements[0].Equals(webAppId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebAppFeatures: Deleting WebApp Feature {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", cacheElements[1]);
                    emitter.Add("WebApplicationId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region WebApp-SPPolicies
        /// <summary>
        /// Emit a collection of web application features
        /// </summary>
        /// <param name="writer">The event stream</param>
        /// <param name="localFarm">The local farm</param>
        /// <param name="webAppId">The web application ID</param>
        /// <param name="features">The feature set</param>
        private void EmitWebAppPolicies(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, SPPolicyCollection policies)
        {
            List<string> current = new List<string>();
            var type = CacheType.Policy;

            foreach (SPPolicy policy in policies)
            {
                var id = policy.UserName.ToString();
                var cacheId = string.Format("{0}/{1}", webAppId.ToString(), id);
                var hash = Converter.ToHash(webAppId, policy);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId/PolicyName
                string[] cacheElements = old.Split('/');
                if (cacheElements[0].Equals(webAppId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebAppPolicies: Deleting WebApp Policy {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("UserName", cacheElements[1]);
                    emitter.Add("WebApplicationId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region WebApp-SPPrefixes
        /// <summary>
        /// Emit a list of SharePoint Prefixes
        /// </summary>
        /// <param name="writer">The Event Stream</param>
        /// <param name="localFarm">The Local Farm</param>
        /// <param name="webAppId">The WebApp ID</param>
        /// <param name="prefixes">The List of Prefixes</param>
        private void EmitWebAppPrefixes(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, SPPrefixCollection prefixes)
        {
            List<string> current = new List<string>();
            var type = CacheType.Prefix;

            foreach (SPPrefix prefix in prefixes)
            {
                var hash = Converter.ToHash(webAppId, prefix);
                var id = hash["Name"];
                var cacheId = string.Format("{0}#{1}", webAppId.ToString(), id);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId#PrefixName
                string[] cacheElements = old.Split('#');
                if (cacheElements[0].Equals(webAppId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebAppPrefixes: Deleting WebApp Prefix {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Name", cacheElements[1]);
                    emitter.Add("WebApplicationId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            } 
        }
        #endregion

        #region WebApp-SPSites
        /// <summary>
        /// Emit a list of SharePoint Sites
        /// </summary>
        /// <param name="writer">The Event Stream</param>
        /// <param name="localFarm">The Local Farm</param>
        /// <param name="webAppId">The WebApp ID</param>
        /// <param name="sites">The List of Sites</param>
        private void EmitWebAppSites(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, SPSiteCollection sites)
        {
            List<string> current = new List<string>();
            var type = CacheType.Site;

            foreach (SPSite site in sites)
            {
                var id = site.ID.ToString();
                var cacheId = string.Format("{0}#{1}", webAppId.ToString(), id);

                try
                {
                    var hash = Converter.ToHash(webAppId, site);
                    var chk = Converter.ToChecksum(hash);

                    if (!Cache.IsUpdated(type, cacheId, chk))
                        return;

                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                    foreach (var pair in hash)
                    {
                        emitter.Add(pair.Key, pair.Value);
                    }
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());

                    var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                    Cache[newCache.Type, newCache.Id] = newCache;
                }
                catch (SqlException sqlException)
                {
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = CacheType.Error, Timestamp = DateTime.Now };
                    emitter.Add("Location", "Site");
                    emitter.Add("SiteId", site.ID.ToString());
                    emitter.Add("WebApplicationId", webAppId.ToString());
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    emitter.Add("Exception", sqlException.GetType().FullName);
                    emitter.Add("Code", sqlException.ErrorCode.ToString("X"));
                    emitter.Add("Message", Utility.Quotable(sqlException.Message));
                    writer.Write(emitter.ToEmitter());
                    ErrorCount++;
                }

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId#PrefixName
                string[] cacheElements = old.Split('#');
                if (cacheElements[0].Equals(webAppId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebAppSites: Deleting WebApp Site {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", cacheElements[1]);
                    emitter.Add("WebApplicationId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region Site-SPWebs
        /// <summary>
        /// Emit a list of SharePoint Webs
        /// </summary>
        /// <param name="writer">The Event Stream</param>
        /// <param name="localFarm">The Local Farm</param>
        /// <param name="webAppId">The WebApp ID</param>
        /// <param name="siteId">The Site Id</param>
        /// <param name="webs">The List of Webs</param>
        private void EmitSiteWebs(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, Guid siteId, SPWebCollection webs)
        {
            List<string> current = new List<string>();
            var type = CacheType.Web;

            foreach (SPWeb web in webs)
            {
                var hash = Converter.ToHash(webAppId, siteId, web);
                var id = web.ID.ToString();
                var cacheId = string.Format("{0}#{1}", siteId.ToString(), id);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId#PrefixName
                string[] cacheElements = old.Split('#');
                if (cacheElements[0].Equals(siteId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebAppWebs: Deleting WebApp Web {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", cacheElements[1]);
                    emitter.Add("SiteId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region Web-SPUsers
        /// <summary>
        /// Emit a list of SharePoint Users within a Web
        /// </summary>
        /// <param name="writer">The Event Stream</param>
        /// <param name="localFarm">The Local Farm</param>
        /// <param name="webAppId">The WebApp ID</param>
        /// <param name="siteId">The Site Id</param>
        /// <param name="webId">The Web Id</param>
        /// <param name="users">The List of Users</param>
        private void EmitWebUsers(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, Guid siteId, Guid webId, SPUserCollection users)
        {
            List<string> current = new List<string>();
            var type = CacheType.User;

            foreach (SPUser user in users)
            {
                var hash = Converter.ToHash(webAppId, siteId, webId, user);
                var id = user.ID.ToString();
                var cacheId = string.Format("{0}#{1}", webId.ToString(), id);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId#PrefixName
                string[] cacheElements = old.Split('#');
                if (cacheElements[0].Equals(webId.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebUsers: Deleting Web User {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", cacheElements[1]);
                    emitter.Add("WebId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion

        #region Web-SPLists
        /// <summary>
        /// Emit a list of SharePoint Lists within a Web
        /// </summary>
        /// <param name="writer">The Event Stream</param>
        /// <param name="localFarm">The Local Farm</param>
        /// <param name="webAppId">The WebApp ID</param>
        /// <param name="siteId">The Site Id</param>
        /// <param name="web">The SPWeb Object</param>
        /// <param name="lists">The List of Lists</param>
        private void EmitWebLists(EventStreamWriter writer, SPFarm localFarm, Guid webAppId, Guid siteId, SPWeb web, SPListCollection lists)
        {
            List<string> current = new List<string>();
            var type = CacheType.List;

            foreach (SPList list in lists)
            {
                var hash = Converter.ToHash(webAppId, siteId, web, list);
                var id = hash["Id"];
                var cacheId = string.Format("{0}#{1}", web.ID.ToString(), id);
                var chk = Converter.ToChecksum(hash);

                if (!Cache.IsUpdated(type, cacheId, chk))
                    return;

                SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                emitter.Add("Action", Cache.IsNew(type, cacheId) ? "Add" : "Update");
                foreach (var pair in hash)
                {
                    emitter.Add(pair.Key, pair.Value);
                }
                emitter.Add("FarmId", localFarm.Id.ToString());
                writer.Write(emitter.ToEmitter());

                var newCache = new CacheObject { Type = type, Id = cacheId, LastUpdated = DateTime.Now, Checksum = chk };
                Cache[newCache.Type, newCache.Id] = newCache;

                current.Add(id.ToString());
            }

            List<string> inCache = Cache.GetIdListByCacheType(type);
            foreach (var old in inCache)
            {
                // The cache key in this case is WebAppId#PrefixName
                string[] cacheElements = old.Split('#');
                if (cacheElements[0].Equals(web.ID.ToString()) && !current.Contains(cacheElements[1]))
                {
                    SystemLogger.Write(LogLevel.Debug, string.Format("EmitWebLists: Deleting Web List {0}", old));
                    SplunkEmitter emitter = new SplunkEmitter { CacheType = type, Timestamp = DateTime.Now };
                    emitter.Add("Action", "Delete");
                    emitter.Add("Id", cacheElements[1]);
                    emitter.Add("WebId", cacheElements[0]);
                    emitter.Add("FarmId", localFarm.Id.ToString());
                    writer.Write(emitter.ToEmitter());
                    Cache.Remove(type, old);
                }
            }
        }
        #endregion
    }
}
