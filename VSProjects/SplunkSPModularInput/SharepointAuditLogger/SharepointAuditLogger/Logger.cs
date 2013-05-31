/// <summary>
/// The below code reads the modular input and fetch sharepoint logs using the it. The logs are send to stdout.
/// </summary>

using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System.Web;
using System.Data;
using System.IO;
using Microsoft.Office.Server;
using Microsoft.Office.Server.UserProfiles;
using Splunk.ModularInputs;
using System.Xml.Serialization;

namespace SharepointAuditLogger
{
	class Program
	{
        static void Main(string[] args)
		{
            if (args.Length > 0)
            {
                if (args[0].Equals("--scheme"))
                {
                    //Create an Introspection Scheme
                    Scheme scheme = new Scheme();
                    scheme.Title = "Microsoft Sharepoint 2010";
                    scheme.UseExternalValidation = false;
                    scheme.StreamingMode = StreamingMode.Simple;
                    Scheme.EndpointElement endpoint = scheme.Endpoint;
                    List<Argument> arguments = new List<Argument>();
                    Argument arg = new Argument();
                    arg.DataType = DataType.Number;
                    arg.Name = "interval";
                    arg.Description = "Timespan to execute the code";
                    arg.RequiredOnEdit = true;
                    arg.RequiredOnCreate = true;
                    arguments.Add(arg);
                    endpoint.Arguments = arguments;
                    scheme.Endpoint = endpoint;
                    Console.WriteLine(Serialize(scheme));
                }
            }
            else
            {
                try
                {
                    //Get the modular inputs definition from Splunk
                    Hashtable config = GetConfig();
                    int timespan = Int32.Parse(config["Interval"].ToString()) * 60000;//converting the interval into milliseconds
                MyLabel:
                    GetSharepointLogs(config);
                    Thread.Sleep(timespan);//suspends the exe for the specified timespan
                    goto MyLabel;
                }
                catch (Exception ex)
                {
                    SystemLogger.Write(LogLevel.Error, ex.Message);
                }

            }
        }

        /// <summary>
        /// Serializes object to XML output
        /// </summary>
        /// <returns>The XML String</returns>
        static string Serialize(Object obj)
        {
            XmlSerializer x = new XmlSerializer(obj.GetType());
            StringWriter sw = new StringWriter();
            x.Serialize(sw, obj);
            return sw.ToString();
        }
        
       /// <summary>
		/// This function is used to get the sharepoint logs. The data is send to stdout
		/// </summary>
        /// 
		static void GetSharepointLogs(Hashtable config)
		{
            EventStreamWriter writer = new EventStreamWriter();
            try
            {
                CheckPointer.GetCheckPoint(config["CheckDir"].ToString(),config["SchemeName"].ToString());
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    string data;
                    SystemLogger.Write(LogLevel.Debug, "Connecting to Site");
                    SPFarm farm = SPFarm.Local;
                    SPWebService service = farm.Services.GetValue<SPWebService>("");
                    foreach(SPWebApplication webapp in service.WebApplications)
                    {
                        foreach (SPSite site in webapp.Sites)
                        {
                            SystemLogger.Write(LogLevel.Debug, "Connected to Site");
                            foreach (SPWeb web in site.AllWebs)
                            {
                                SPAudit audit = web.Audit;
                                //enables auditing in SharePoint 
                                audit.AuditFlags = SPAuditMaskType.All;
                                audit.Update();
                                SPAuditQuery auditQuery = new SPAuditQuery(site);
                                auditQuery.SetRangeStart(CheckPointer.Occured);
                                auditQuery.SetRangeEnd(DateTime.Now);
                                SPAuditEntryCollection auditCol = audit.GetEntries(auditQuery);
                                SystemLogger.Write(LogLevel.Debug, "Done Reading Audit Entry");
                                string webappName = site.WebApplication.DisplayName;
                                long storage = site.Usage.Storage / 1024 / 1024;
                                int UserMaximumLevel = site.Quota.InvitedUserMaximumLevel;
                                ulong dbsize = (site.ContentDatabase.DiskSizeRequired) / 1024 / 1024;
                                long storageMaximumLevel = (site.Quota.StorageMaximumLevel) / 1024 / 1024;
                                long storageWarningLevel = (site.Quota.StorageWarningLevel) / 1024 / 1024;
                                foreach (SPAuditEntry entry in auditCol)
                                {
                                    //When the current event datetime is less than check point datetime and equal to ItemId that means the data is already indexed. In that case ,we are skipping the record.
                                    SystemLogger.Write(LogLevel.Debug, "Processing Entry with Item IE: " + entry.ItemId + " and Occured Time: " + entry.Occurred);
                                    if (entry.MachineName.Equals(config["ServerHost"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (((entry.Occurred >= CheckPointer.Occured) && (entry.ItemId != CheckPointer.ItemId)))
                                        {
                                            string userName;
                                            string groupName;
                                            string userLoginName;
                                            string department;
                                            data = entry.Occurred.ToString() + "," + "SiteId=\"" + entry.SiteId.ToString() + "\",SiteName=\"" + GetSiteNameById(entry.SiteId) + "\"," + "ItemId=\"" + entry.ItemId.ToString() + ",ItemName=\"" + GetItemNameById(entry) + "\",ItemType=\"" + entry.ItemType.ToString() + "\",UserId=" + entry.UserId + "," + "DocLocation=\"" + entry.DocLocation + "\"," + "LocationType=\"" + entry.LocationType + "\",Event=\"" + entry.Event + "\"," + "EventSource=\"" + entry.EventSource + "\"," + "MachineIP=\"" + entry.MachineIP + "\"," + "MachineName=\"" + entry.MachineName + "\",DBSizeMB=" + dbsize;
                                            if (entry.UserId == -1)
                                            {
                                                userName = @"SHAREPOINT\System";
                                                groupName = "";
                                                department = "";
                                            }
                                            else
                                            {
                                                department = "";
                                                userName = web.AllUsers.GetByID(entry.UserId).Name;
                                                userLoginName = web.AllUsers.GetByID(entry.UserId).LoginName;
                                                
                                                SPUser oUser = web.AllUsers[userLoginName];
                                                groupName = "";
                                                for (int i = 0; i < oUser.Groups.Count; i++)
                                                {
                                                    groupName = groupName + oUser.Groups[i].Name;
                                                    if (i < oUser.Groups.Count - 1)
                                                        groupName = groupName + ";";
                                                }
                                            }

                                            data = data + ",ListName=\"" + GetListName(entry.SiteId, entry.ItemType.ToString(), entry.DocLocation.ToString()) + "\"";
                                            data = data + ",ItemName=\"" + GetItemNameById(entry) + "\",,StorageMB=" + storage;
                                            data = data + ",ItemSize=" + GetSizeOfItem(entry.SiteId, entry.ItemId, entry.ItemType.ToString());
                                            data = data + "," + "Department=\"" + department + "\",UserName=\"" + userName + "\",GroupName=\"" + groupName + "\",ApplicationName=\"" + webappName + "\"" + ",StorageMaximumLevelMB=" + storageMaximumLevel + ",StorageWarningLevelMB=" + storageWarningLevel;
                                            
                                            EventElement eventElement = new EventElement
                                            {
                                                Time = entry.Occurred,
                                                Data = data
                                            };
                                            writer.Write(eventElement);

                                            SystemLogger.Write(LogLevel.Debug, "Sending Audit entry to STDOUT");
                                            CheckPointer.SetCheckPoint(entry.Occurred, entry.ItemId);
                                        }
                                        else
                                        {
                                            SystemLogger.Write(LogLevel.Debug, "Entry with Item IE: " + entry.ItemId + " and Occured Time: " + entry.Occurred + " already Processed");
                                        }
                                    }
                                }
                                
                            }
                        }
                    }
                    //Saves the checkpoint once done with streaming all the entries
                    CheckPointer.SaveCheckPoint(config["CheckDir"].ToString(),config["SchemeName"].ToString());
                    SystemLogger.Write(LogLevel.Debug, "Done sending Audit entry");
                });
            }
            catch (Exception ex)
            {
                //Saves the last successfully streamed entry into the Checkpoint
                CheckPointer.SaveCheckPoint(config["CheckDir"].ToString(), config["SchemeName"].ToString());
                SystemLogger.Write(LogLevel.Error, ex.Message);
            }
		}


		/// <summary>
		/// The function reads modular input definition
		/// </summary>
		/// <returns>Hashtable containing Service URI,Server Host, Checkpoint Dir, Session Key and Interval </returns>
		static Hashtable GetConfig()
		{
            Hashtable inputCollection = new Hashtable();
            SystemLogger.Write(LogLevel.Debug, "XML:Processing Input Configuration");
            XmlSerializer x = new XmlSerializer(typeof(InputDefinition));
            InputDefinition id = (InputDefinition)x.Deserialize(Console.In);
            if (id != null)
            {
                SystemLogger.Write(LogLevel.Debug, "XML:Found Configuration");
                inputCollection["ServerHost"] = id.ServerHost;
                inputCollection["ServerURI"] = id.ServerUri;
                inputCollection["CheckDir"] = id.CheckpointDirectory;
                inputCollection["SessionKey"] = id.SessionKey;
                if (id.Stanzas.Count > 0)
                {
                    inputCollection["SchemeName"] = id.Stanza.Name;
                    SystemLogger.Write(LogLevel.Debug, "XML: Found Stanza");
                    if (id.Stanza.Parameters.Count > 0)
                    {
                        SystemLogger.Write(LogLevel.Debug, "XML: Found Parameters");
                        inputCollection["Interval"] = id.Stanza.Parameters["interval"];
                    }
                }
            }
            else
            {
                SystemLogger.Write(LogLevel.Error, "XML:Configuration Not Found");
            }
            return inputCollection;
		}


        /// </summary>
        /// <param name="siteId">Site id as in the Audit log entry</param>
        /// <param name="itemId"></param>
        /// /// <param name="itemType"></param>
        /// <returns>A long that contains the size of the item in bytes</returns>
		
        static long GetSizeOfItem(Guid siteId,Guid itemId,string itemType)
        {
            using (SPSite site = new SPSite(siteId))
            {
                using (SPWeb web = site.OpenWeb())
                {
                    switch (itemType)
                    {
                        case "Document":
                            try
                            {
                                return web.GetFile(itemId).Length;
                            }
                            catch
                            {
                                return 0;
                            }
                        default:
                            return 0;
                    }
                }
            }
            
        }


		/// </summary>
		/// <param name="siteId">Site id as in the Audit log entry</param>
		/// <returns>A string that contains the name of the site</returns>
		static string GetSiteNameById(Guid siteId)
		{
			using (SPSite site = new SPSite(siteId))
			{
				using (SPWeb web = site.OpenWeb())
				{
					return web.Title;
				}
			}
		}


		/// <summary>
		/// This function gets the item name from Document Location. 
		/// </summary>
		/// <param name="siteId"></param>
		/// <param name="itemId"></param>
		/// <returns>A string that contains the name of the item</returns>
		static string GetItemNameById(SPAuditEntry entry)
		{
			using (SPSite site = new SPSite(entry.SiteId))
			{
				using (SPWeb web = site.OpenWeb())
				{
                    switch (entry.ItemType.ToString())
                    {
                        case "Document":
                            try
                            {
                                return web.GetFile(entry.ItemId).Title;
                            }
                            catch
                            {
                                string[] seperator = {"/"};
                                string[] filename = entry.DocLocation.ToString().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                                return filename[filename.Length - 1];
                            }
                        default:
                                string[] seperator1 = {"/"};
                                string[] filename1 = entry.DocLocation.ToString().Split(seperator1, StringSplitOptions.RemoveEmptyEntries);
                                return filename1[filename1.Length - 1];
                    }
                }
            }
		}

        
        /// <summary>
        /// This function gets the list name from item type.
        /// </summary>
        /// <param name="siteId"></param>
        /// <param name="itemType"></param>
        /// /// <param name="docLocation"></param>
        /// <returns>A string that contains the name of the list</returns>
        static string GetListName(Guid siteId, string itemType, string docLocation)
        {
            using (SPSite site = new SPSite(siteId))
            {
                using (SPWeb web = site.OpenWeb())
                {

                    switch (itemType)
                    {
                        case "Site":
                            return GetSiteNameById(siteId);
                        case "Folder":
                            return "";
                        case "Web":
                            return GetSiteNameById(siteId); 
                        default:
                            try
                            {
                                return web.GetList(docLocation).Title;
                            }
                            catch
                            {
                                string[] seperator = { "/" };
                                string[] filename = docLocation.ToString().Split(seperator, StringSplitOptions.RemoveEmptyEntries);
                                if(filename[filename.Length-1].IndexOf(".")!=-1)
                                    return filename[filename.Length-2];
                                else
                                    return filename[filename.Length - 1];
                            }
                    }
                }
            }
        }

	}
}