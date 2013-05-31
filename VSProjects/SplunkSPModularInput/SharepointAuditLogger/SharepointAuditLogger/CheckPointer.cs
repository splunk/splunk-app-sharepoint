using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using Splunk.ModularInputs;

namespace SharepointAuditLogger
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
        private static DateTime _occured;
        public static DateTime Occured
        {
            get
            {
                return _occured;
            }
            set
            {
                _occured = value;
            }
        }

        /// <summary>
        /// The item id on which the event occured
        /// </summary>
        private static Guid _itemId;
        public static Guid ItemId
        {
            get
            {
                return _itemId;
            }
            set
            {
                _itemId = value;
            }
        }


        /// <summary>
        /// Set the values of event occurance date time and Item id to save the check point.
        /// </summary>
        /// <param name="occured">The time of event occurance</param>
        /// <param name="itemId">The item id</param>
        public static void SetCheckPoint(DateTime occured, Guid itemId)
        {
            CheckPointer.Occured = occured;
            CheckPointer.ItemId = itemId;
        }

        /// <summary>
        /// Save the checkpoint data- Event Occured time and Item id in a file.
        /// This data is used to check already indexed data.
        /// </summary>
        /// <param name="checkpointdir">path to the check point directory</param>
        /// <param name="filename">Check point filename</param>
        public static void SaveCheckPoint(string checkpointdir, string filename)
        {
            string checkpointfile = Path.Combine(checkpointdir, filename.Replace("://", "_") + "_chkpt" + ".txt");
            string[] checkData = { CheckPointer.Occured.ToString(), CheckPointer.ItemId.ToString() };
            try
            {
                System.IO.File.WriteAllLines(checkpointfile, checkData);
            }
            catch (Exception ex)
            {
                SystemLogger.Write(LogLevel.Error, "CheckPoint:Failed to open file checkpoint.txt: " + ex.Message);
            }
        }

        /// <summary>
        /// Get the stored checkpoint data- Event Occured time and Item id from the checkpoint file
        /// This data is used to skip the already indexed data and look for the latest entries 
        /// </summary>
        public static void GetCheckPoint(string checkpointdir, string filename)
        {
            string checkpointfile = Path.Combine(checkpointdir, filename.Replace("://", "_") + "_chkpt" + ".txt");
            try
            {
                string[] lines = System.IO.File.ReadAllLines(checkpointfile);
                CheckPointer.Occured = Convert.ToDateTime(lines[0]);
                CheckPointer.ItemId = new Guid(lines[1]);
            }
            catch (Exception ex)
            {
                SystemLogger.Write(LogLevel.Error, "CheckPoint:Failed to open file checkpoint.txt: " + ex.Message);
            }
        }
    }
}