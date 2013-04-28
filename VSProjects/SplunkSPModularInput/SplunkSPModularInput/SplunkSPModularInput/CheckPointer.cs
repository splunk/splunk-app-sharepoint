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
				return sItemId;
			}
			set
			{
				sItemId = value;
			}
		}

		

		/// <summary>
		/// Set the values of event occurance date time, Item id and event type to save the check point.
		/// </summary>
		/// <param name="occured">The time of event occurance</param>
		/// <param name="itemId">The item id</param>
		/// <param name="eventName"></param>
		public static void SetCheckPoint(DateTime occured, Guid itemId)
		{
            CheckPointer.Occured = occured;
			CheckPointer.ItemId = itemId;
		}

		/// <summary>
		/// Save the checkpoint data- Event Occured time and Item id in a file.
		/// This data is used to check already indexed data.
		/// </summary>
		public static void SaveCheckPoint(string checkpointdir,string filename)
		{
            string checkpointfile = Path.Combine(checkpointdir, filename.Replace("//", ";").Split(';')[1] + "_chkpt" + ".txt");
			string[] checkData = {CheckPointer.Occured.ToString() , CheckPointer.ItemId.ToString()};
            try
            {
                System.IO.File.WriteAllLines(checkpointfile, checkData);
            }
            catch(Exception ex)
            {
                SharepointLogger.SystemLogger(LogLevel.ERROR, "CheckPoint:Failed to open file checkpoint.txt: "+ex.Message);
            }
		}

		/// <summary>
		/// 
		/// </summary>
		public static void GetCheckPoint(string checkpointdir,string filename)
		{
            string checkpointfile = Path.Combine(checkpointdir, filename.Replace("//", ";").Split(';')[1]+ "_chkpt" + ".txt");
            try
            {
                string[] lines = System.IO.File.ReadAllLines(checkpointfile);
                CheckPointer.Occured = Convert.ToDateTime(lines[0]);
                CheckPointer.ItemId = new Guid(lines[1]);
            }
            catch (Exception ex)
            {
                SharepointLogger.SystemLogger(LogLevel.ERROR, "CheckPoint:Failed to open file checkpoint.txt: " + ex.Message);
            }
            
            
		}
	}
}