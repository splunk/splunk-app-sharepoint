using System;
using System.IO;
using System.Collections.Generic;
using Splunk.ModularInputs;

namespace Splunk.Sharepoint.Audit
{
    public class CheckpointData
    {
        private string _file = null;
        private Dictionary<Guid,DateTime> dataset;

        /// <summary>
        ///     Create a new Checkpoint Dataset
        /// </summary>
        /// <param name="file">Backing Store Filename</param>
        public CheckpointData(string file)
        {
            _file = file;
            dataset = new Dictionary<Guid, DateTime>();

            if (File.Exists(Filename))
                LoadData();
        }

        /// <summary>
        ///     Returns the Backing Store Filename
        /// </summary>
        public string Filename 
        {
            get 
            { 
                return _file; 
            } 
        }

        /// <summary>
        ///     Loads the data from the backing store (if present)
        ///     Throws appropriate exceptions if the file is not readable
        /// </summary>
        public void LoadData()
        {
            if (!File.Exists(Filename))
                return;

            using (StreamReader reader = new StreamReader(Filename))
            {
                string line = reader.ReadLine();
                string[] parts = line.Split('|');
                if (parts.Length != 2)
                    throw new InvalidDataException("Invalid Data in Backing Store");

                Guid guid = new Guid(parts[0]);
                long ticks = long.Parse(parts[1]);
                DateTime timestamp = new DateTime(ticks);

                dataset.Add(guid, timestamp);
            }
        }

        /// <summary>
        ///     Write the data set out to the backing sotre
        /// </summary>
        public void SaveData()
        {
            using (StreamWriter writer = new StreamWriter(Filename))
            {
                foreach (Guid guid in dataset.Keys)
                {
                    writer.WriteLine(string.Format("{0}|{1}", guid.ToString(), dataset[guid].Ticks.ToString()));
                }
            }
        }

        /// <summary>
        ///     Returns the timestamp of a Guid
        /// </summary>
        /// <param name="guid">the guid</param>
        /// <returns>the timestamp of the guid</returns>
        public DateTime GetTimestamp(Guid guid)
        {
            if (dataset.ContainsKey(guid))
                return dataset[guid];
            else
                return DateTime.MinValue;
        }

        /// <summary>
        ///     Sets the last known timestamp of the guid
        /// </summary>
        /// <param name="guid">guid</param>
        /// <param name="timestamp">timestamp</param>
        public void SetTimestamp(Guid guid, DateTime timestamp)
        {
            if (dataset.ContainsKey(guid))
                dataset[guid] = timestamp;
            else
                dataset.Add(guid, timestamp);
        }

    }
}
 