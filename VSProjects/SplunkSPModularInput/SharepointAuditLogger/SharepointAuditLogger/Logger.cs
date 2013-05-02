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
using Splunk.Sharepoint.ModularInputs;

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
                    SharepointScheme scheme = new SharepointScheme();
                    scheme.Title = "Microsoft Sharepoint 2010";
                    scheme.UseExternalValidation = false;
                    scheme.StreamingMode = StreamingMode.SIMPLE;
                    Endpoint endpoint = new Endpoint();
                    List<EndpointArgument> arguments = new List<EndpointArgument>();
                    EndpointArgument arg = new EndpointArgument();
                    arg.DataType = ArgumentDataType.NUMBER;
                    arg.Name = "interval";
                    arg.Description = "Timespan to execute the code";
                    arg.RequiredOnEdit = true;
                    arg.RequiredOnCreate = true;
                    arguments.Add(arg);
                    endpoint.Arguments = arguments;
                    scheme.Endpoint = endpoint;
                    Console.WriteLine(scheme.Serialize());
                }
                else if (args[0].Equals("--test"))
                {
                    Console.WriteLine("testing");
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
                    SharepointLogger.SystemLogger(LogLevel.ERROR, ex.Message);
                }
            }
        }

       
		/// <summary>
		/// This function is used to get the sharepoint logs. The data is send to stdout
		/// </summary>
        /// 
		static void GetSharepointLogs(Hashtable config)
		{
            try
            {
                CheckPointer.GetCheckPoint(config["CheckDir"].ToString(),config["SchemeName"].ToString());
                SPSecurity.RunWithElevatedPrivileges(delegate()
                {
                    SharepointLogger.SystemLogger(LogLevel.DEBUG, "Connecting to Site");
                    SPFarm farm = SPFarm.Local;
                    SPWebService service = farm.Services.GetValue<SPWebService>("");
                    StringBuilder dataColl = new StringBuilder();
                    foreach(SPWebApplication webapp in service.WebApplications)
                    {
                        foreach (SPSite site in webapp.Sites)
                        {
                            SharepointLogger.SystemLogger(LogLevel.DEBUG, "Connected to Site");
                            foreach (SPWeb web in site.AllWebs)
                            {
                                SPAudit audit = web.Audit;
                                SPAuditQuery auditQuery = new SPAuditQuery(site);
                                auditQuery.SetRangeStart(CheckPointer.Occured);
                                auditQuery.SetRangeEnd(DateTime.Now);
                                SPAuditEntryCollection auditCol = audit.GetEntries(auditQuery);
                                SharepointLogger.SystemLogger(LogLevel.DEBUG, "Done Reading Audit Entry");

                                // SplunkTextEmitter object is used to stream the output to Splunk in text format
                                SplunkTextEmitter emitter = new SplunkTextEmitter();

                                foreach (SPAuditEntry entry in auditCol)
                                {
                                    //When the current event datetime is less than check point datetime and equal to ItemId that means the data is already indexed. In that case ,we are skipping the record.
                                    SharepointLogger.SystemLogger(LogLevel.DEBUG, "Processing Entry with Item Id: " + entry.ItemId + " and Occured Time: " + entry.Occurred);
                                    
                                    //Will look for audit logs related to the specified host in config["ServerHost"]
                                    if (entry.MachineName.Equals(config["ServerHost"].ToString(), StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        if (((entry.Occurred >= CheckPointer.Occured) && (entry.ItemId != CheckPointer.ItemId)))
                                        {
                                            string userName;
                                            String data = entry.Occurred.ToString() + "," + "SiteId=" + entry.SiteId.ToString() + "," + "ItemId=" + entry.ItemId.ToString() + "," + "ItemType=" + entry.ItemType.ToString() + "," + "UserId=" + entry.UserId + "," + "DocLocation=" + entry.DocLocation + "," + "LocationType=" + entry.LocationType + "," + "Event=" + entry.Event + "," + "EventSource=" + entry.EventSource + "," + "MachineIP=" + entry.MachineIP + "," + "MachineName=" + entry.MachineName;
                                            if (entry.UserId == -1)
                                            {
                                                userName = @"SHAREPOINT\System";
                                            }
                                            else
                                            {
                                                userName = web.AllUsers.GetByID(entry.UserId).Name;
                                            }
                                            data = data + "," + "UserName=" + userName;

                                            // Streams the data into stdout
                                            emitter.emit(data);

                                            SharepointLogger.SystemLogger(LogLevel.DEBUG, "Sending Audit entry to STDOUT");
                                            CheckPointer.SetCheckPoint(entry.Occurred, entry.ItemId);
                                        }
                                        else
                                        {
                                            SharepointLogger.SystemLogger(LogLevel.DEBUG, "Entry with Item Id: " + entry.ItemId + " and Occured Time: " + entry.Occurred + " already Processed");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    //Saves the checkpoint once done with streaming all the entries
                    CheckPointer.SaveCheckPoint(config["CheckDir"].ToString(),config["SchemeName"].ToString());
                    SharepointLogger.SystemLogger(LogLevel.DEBUG, "Done sending Audit entry");
                });
            }
            catch (Exception ex)
            {
                //Saves the last successfully streamed entry into the Checkpoint
                CheckPointer.SaveCheckPoint(config["CheckDir"].ToString(), config["SchemeName"].ToString());
                SharepointLogger.SystemLogger(LogLevel.ERROR, ex.Message);
            }
		}

       

		/// <summary>
		/// The function reads modular input definition
		/// </summary>
		/// <returns>Hashtable containing Service URI,Server Host, Checkpoint Dir, Session Key and Interval </returns>
		static Hashtable GetConfig()
		{
            SharepointLogger.SystemLogger(LogLevel.DEBUG, "XML:Processing Input Configuration");
            Hashtable inputCollection = new Hashtable();
            SharepointInputDefinition id = SharepointInputDefinition.ReadSharepointInputDefinition(Console.In);
            if (id != null)
            {
                SharepointLogger.SystemLogger(LogLevel.DEBUG, "XML:Found Configuration");
                inputCollection["ServerHost"] = id.ServerHost;
                inputCollection["ServerURI"] = id.ServerUri;
                inputCollection["CheckDir"] = id.CheckpointDirectory;
                inputCollection["SessionKey"] = id.SessionKey;
                if (id.Stanzas.Count > 0)
                {
                    inputCollection["SchemeName"] = id.Stanzas[0].Name;
                    SharepointLogger.SystemLogger(LogLevel.DEBUG, "XML: Found Stanza");
                    if (id.Stanzas[0].Parameters.Count > 0)
                    {
                        SharepointLogger.SystemLogger(LogLevel.DEBUG, "XML: Found Parameters");
                        inputCollection["Interval"] = id.Stanzas[0].GetParameterByName("interval", "10");//interval is the field obtained from inputs.conf
                    }
                }
            }
            else
            {
                SharepointLogger.SystemLogger(LogLevel.ERROR, "XML:Configuration Not Found");
            }
            return inputCollection;
		}

        

		/// <summary>
		/// This function gets the sharepoint site name.
		/// </summary>
		/// <param name="siteId">Site id as in the Audit log entry</param>
		/// <param name="webId">Web id</param>
		/// <returns></returns>
		static string GetSiteNameById(Guid siteId, SPWeb webId)
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
		/// This function gets the item name from its ID. THis need to be updated.
		/// </summary>
		/// <param name="siteId"></param>
		/// <param name="itemId"></param>
		/// <returns></returns>
		static string GetItemNameById(Guid siteId, SPWeb itemId)
		{
			using (SPSite site = new SPSite(siteId))
			{
				using (SPWeb web = site.OpenWeb())
				{
                    return web.Title;
				}
			}
		}
	}
}