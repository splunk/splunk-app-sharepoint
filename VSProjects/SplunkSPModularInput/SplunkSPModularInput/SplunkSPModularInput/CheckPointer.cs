using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Splunk.Sharepoint.ModularInputs
{
	/// <summary>
	/// Check pointer helps to reduce re-indexing of data. The last record of indexed data is stored in a file.
	/// The getter properties helps to read data from file and setter sets the data.
	/// </summary>
	public static class CheckPointer
	{
		/// <summary>
		/// The time of occurance of the event
		/// </summary>
		private static DateTime sOccured;
		public static DateTime Occured
		{
			get
			{
				//TO-DO: if default value, read from file to get the value
				return sOccured;
			}
			set
			{
				sOccured = value;
			}
		}

		/// <summary>
		/// The item id on which the event occured
		/// </summary>
		private static Guid sItemId;
		public static Guid ItemId
		{
			get
			{
				//TO-DO: if default value, read from file to get the value
				return sItemId;
			}
			set
			{
				sItemId = value;
			}
		}

		/// <summary>
		/// The evnt name e.g. view, edit
		/// </summary>
		private static string sEvent;
		public static string Event
		{
			get
			{
				//TO-DO: if default value, read from file to get the value
				return sEvent;
			}
			set
			{
				sEvent = value;
			}
		}

		/// <summary>
		/// Set the values of event occurance date time, Item id and event type to save the check point.
		/// </summary>
		/// <param name="occured">The time of event occurance</param>
		/// <param name="itemId">The item id</param>
		/// <param name="eventName"></param>
		public static void SetCheckPoint(DateTime occured, Guid itemId, string eventName)
		{
			CheckPointer.Occured = occured;
			CheckPointer.ItemId = itemId;
			CheckPointer.Event = eventName;
		}

		/// <summary>
		/// Save the checkpoint data- Event Occured time, Item id and Event Name in a file.
		/// This data is used to check already indexed data.
		/// </summary>
		public static void SaveCheckPoint()
		{
			string splunkdir = Environment.GetEnvironmentVariable("SPLUNK_HOME");
			string logdir = Path.Combine(splunkdir, "log");
			string logfile = Path.Combine(logdir, "checkpoint" + ".txt");

			string[] checkData = {CheckPointer.Occured.ToString() , CheckPointer.ItemId.ToString() , CheckPointer.Event};
			System.IO.File.WriteAllLines(logfile, checkData);
		}

		/// <summary>
		/// 
		/// </summary>
		public static void GetCheckPoint()
		{
			string splunkdir = Environment.GetEnvironmentVariable("SPLUNK_HOME");
			string logdir = Path.Combine(splunkdir, "log");
			string logfile = Path.Combine(logdir, "checkpoint" + ".txt");

			string[] lines = System.IO.File.ReadAllLines(logfile);

			CheckPointer.Occured = Convert.ToDateTime(lines[0]);
			CheckPointer.ItemId = new Guid(lines[1]);
			CheckPointer.Event = lines[2];
		}
	}
}