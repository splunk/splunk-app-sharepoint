/// <summary>
/// The below code reads the modular input and fetch sharepoint logs using the it. The logs are send to stdout.
/// </summary>

using System;
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
			GetSharepointLogs();
		}

		/// <summary>
		/// This object is used to get the sharepoint logs. The list is send to STDOUT
		/// </summary>
		static void GetSharepointLogs()
		{
			try
			{
				SPSecurity.RunWithElevatedPrivileges(delegate()
				{
					Hashtable config = GetConfig();
					using (SPSite siteColl = new SPSite(config["ServerURI"] + ":" + config["PortNumber"] + "/"))
					{
						SharepointLogger.SystemLogger(LogLevel.INFO, "Connecting to Site");

						using (SPWeb site = siteColl.OpenWeb())
						{
							SPAuditQuery wssQuery = new SPAuditQuery(siteColl);
							SPAuditEntryCollection auditCol = siteColl.Audit.GetEntries(wssQuery);
							foreach (SPAuditEntry entry in auditCol)
							{
								//When the current event datetime is less than check point datetime, Id and event Name
								//that means the data is already indexed. In that case ,we are skipping the record.
								if ((entry.Occurred >= CheckPointer.Occured) && (entry.ItemId != CheckPointer.ItemId) && (entry.EventName != CheckPointer.Event))
								{
									XmlDocument auditEntryDoc = new XmlDocument();
									auditEntryDoc.LoadXml(entry.ToString());
									XmlNode root = auditEntryDoc.DocumentElement;

									Dictionary<string, string> Nodes = new Dictionary<string, string>();
									Nodes.Add("MachineIP", entry.MachineIP);
									Nodes.Add("MachineName", entry.MachineName);
									Nodes.Add("UserName", entry.UserId == -1 ? @"SHAREPOINT\System" : site.AllUsers.GetByID(entry.UserId).Name);
									Nodes.Add("SourceName", entry.SourceName);
									Nodes.Add("SiteName", GetSiteNameById(entry.SiteId, site));
									Nodes.Add("ItemName", GetItemNameById(entry.SiteId, site));

									foreach (KeyValuePair<string, string> Node in Nodes)
									{
										//Create a new node.
										XmlElement infoElement = auditEntryDoc.CreateElement(Node.Key);
										infoElement.InnerText = Node.Value;
										//Add the node to the document.
										root.AppendChild(infoElement);
									}

									CheckPointer.SetCheckPoint(entry.Occurred, entry.ItemId, entry.EventName);
									SharepointLogger.SystemLogger(LogLevel.INFO, "Sending audit log to STDOUT");
									Console.WriteLine(auditEntryDoc.InnerXml);
									Console.WriteLine("");
								}
							}
						}
					}

					CheckPointer.SaveCheckPoint();
					SharepointLogger.SystemLogger(LogLevel.INFO, "Done sending Audit entry data");
				});
			}
			catch (Exception ex)
			{
				SharepointLogger.SystemLogger(LogLevel.ERROR, ex.Message);
				Console.Write(ex);
				Console.WriteLine("Press enter to continue");
				Console.ReadLine();
			}
		}

		/// <summary>
		/// The function reads modular input definition
		/// </summary>
		/// <returns>Hashtable containging Service URI and port to connect to sharepoint</returns>
		static Hashtable GetConfig()
		{
			SharepointLogger.SystemLogger(LogLevel.INFO, "Reading InputDefinition File");
			SharepointInputDefinition id = SharepointInputDefinition.ReadSharepointInputDefinition(Console.In);

			Hashtable inputCollection = new Hashtable();
			inputCollection["ServerHost"] = id.ServerHost;
			inputCollection["ServerURI"] = id.ServerUri;
			inputCollection["PortNumber"] = id.PortNumber;
			inputCollection["CheckDir"] = id.CheckpointDirectory;

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