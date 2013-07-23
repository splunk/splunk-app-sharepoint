using System;
using System.Collections.Generic;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2013.Audit
{
    /// <summary>
    /// A Collection of AuditDatabase objects
    /// </summary>
    class AuditDatabaseCollection
    {
        /// <summary>
        /// Dictionary store for the mapping of GUID => Audit Database object
        /// </summary>
        private Dictionary<Guid, AuditDatabase> oDatabases = new Dictionary<Guid, AuditDatabase>();

        /// <summary>
        /// Parameter for the checkpoint directory.
        /// </summary>
        private string CheckpointDirectory { get; set; }

        /// <summary>
        /// Create a new collection of audit databases.  Each database entry will load persistent
        /// data from the checkpoint directory when needed.
        /// </summary>
        /// <param name="checkpointDirectory">The checkpoint directory.</param>
        public AuditDatabaseCollection(string checkpointDirectory)
        {
            CheckpointDirectory = checkpointDirectory;
            Discover();
        }

        /// <summary>
        /// Get an audit database entry by GUID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public AuditDatabase GetByGuid(Guid id)
        {
            return oDatabases[id];
        }

        /// <summary>
        /// Return a list of all audit databases.
        /// </summary>
        /// <returns>A list of GUIDs to use with GetByGuid()</returns>
        public List<Guid> Keys()
        {
            return new List<Guid>(oDatabases.Keys);
        }

        public Dictionary<Guid, AuditDatabase>.Enumerator GetEnumerator()
        {
            return oDatabases.GetEnumerator();
        }

        /// <summary>
        /// discover the database list from the SharePoint information
        /// </summary>
        /// <param name="checkpointDirectory">Where to store checkpoint files</param>
        public void Discover()
        {
            SPSecurity.RunWithElevatedPrivileges(delegate()
            {
                foreach (SPService oService in SPFarm.Local.Services)
                {
                    if (oService is SPWebService)
                    {
                        foreach (SPWebApplication oWebApp in ((SPWebService)oService).WebApplications)
                        {
                            foreach (SPContentDatabase oContentDatabase in oWebApp.ContentDatabases)
                            {
                                if (!oDatabases.ContainsKey(oContentDatabase.Id))
                                {
                                    SystemLogger.Write(LogLevel.Debug, string.Format("Initializing Content Database {0}", oContentDatabase.Id));
                                    AuditDatabase oAuditDB = new AuditDatabase(SPFarm.Local.Id, oContentDatabase.Id, oContentDatabase.DatabaseConnectionString, CheckpointDirectory);
                                    if (!oAuditDB.TableExists("AuditData")) {
                                        SystemLogger.Write(LogLevel.Warn, string.Format("Content Database {0} does not have Audit Data - skipping", oContentDatabase.Id));
                                    } else {
                                        SystemLogger.Write(LogLevel.Info, string.Format("Adding Content Database {0} to the list of databases", oContentDatabase.Id));
                                        oDatabases.Add(oContentDatabase.Id, oAuditDB);
                                    }
                                }
                                else
                                {
                                    SystemLogger.Write(LogLevel.Debug, string.Format("Already seen Content Database {0} - skipping", oContentDatabase.Id));
                                }
                            }
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Tell every database object within the AuditDatabaseCollection to load state
        /// </summary>
        public void Load()
        {
            foreach (Guid id in oDatabases.Keys)
            {
                oDatabases[id].Load();
            }
        }

        /// <summary>
        /// Tell every database object within the AuditDatabaseCollection to save state.
        /// </summary>
        public void Save()
        {
            foreach (Guid id in oDatabases.Keys)
            {
                oDatabases[id].Save();
            }
        }
    }
}
