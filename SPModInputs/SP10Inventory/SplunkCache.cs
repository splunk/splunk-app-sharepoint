using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2010.Inventory
{
    /// <summary>
    /// Implement a cache mechanism for the inventory system
    /// </summary>
    internal class SplunkCache
    {
        /// <summary>
        /// Internal backing store for the cache
        /// </summary>
        private Dictionary<string,CacheObject> _cache = new Dictionary<string,CacheObject>();

        /// <summary>
        /// The name of the checkpoint file
        /// </summary>
        internal string BackingFile { get; set; }

        /// <summary>
        /// True if the cache needs saving
        /// </summary>
        internal bool CacheIsDirty { get; set; }

        /// <summary>
        /// Create a new splunk cache with the specified file as the backing file
        /// </summary>
        /// <param name="checkpointDirectory">The directory containing the backing file</param>
        /// <param name="checkpointFile">The file within the directory</param>
        internal SplunkCache(string checkpointDirectory, string checkpointFile)
        {
            BackingFile = Path.Combine(checkpointDirectory, checkpointFile);
            Load();
        }

        /// <summary>
        /// Create a new splunk cache with the specified file as the backing file
        /// </summary>
        /// <param name="checkpointFile">Fully Qualified backing file path</param>
        internal SplunkCache(string checkpointFile)
        {
            BackingFile = checkpointFile;
            Load();
        }

        /// <summary>
        /// Load a cache from the backing file
        /// </summary>
        internal void Load()
        {
            if (!File.Exists(BackingFile))
            {
                SystemLogger.Write(LogLevel.Warn, string.Format("SplunkCache.Load: File does not exist: {0}", BackingFile));
                CacheIsDirty = false;
                return;
            }

            using (StreamReader reader = new StreamReader(BackingFile))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    CacheObject cacheObject = CacheObject.Parse(line);
                    Add(cacheObject);
                }
            }
            CacheIsDirty = false;
        }

        /// <summary>
        /// Save the cache to the backing file
        /// </summary>
        internal void Save()
        {
            if (!CacheIsDirty)
                return;

            if (BackingFile == null)
            {
                SystemLogger.Write(LogLevel.Error, "SplunkCache.Save: Backing file not set (not saving)");
                return;
            }

            SystemLogger.Write(LogLevel.Debug, string.Format("SplunkCache.Save: Writing {0} entries to {1}", _cache.Count, BackingFile));
            using (StreamWriter writer = new StreamWriter(BackingFile))
            {
                foreach (var pair in _cache)
                {
                    writer.WriteLine(pair.Value.ToString());
                }
                writer.Flush();
                writer.Close();
            }
            CacheIsDirty = false;
        }

        /// <summary>
        /// Add a new cache object to the cache
        /// </summary>
        /// <param name="o"></param>
        internal void Add(CacheObject o)
        {
            _cache.Add(o.Key, o);
            CacheIsDirty = true;
        }

        /// <summary>
        /// Remove a cache object from the cache
        /// </summary>
        /// <param name="cacheType">The cache type</param>
        /// <param name="id">The cache ID</param>
        internal void Remove(CacheType cacheType, string id)
        {
            _cache.Remove(CacheObject.GenerateKey(cacheType, id));
            CacheIsDirty = true;
        }

        /// <summary>
        /// Returns true if the cache contains the element provided
        /// </summary>
        /// <param name="cacheType">the cache Type</param>
        /// <param name="id">The Id of the cache element</param>
        /// <returns></returns>
        internal bool Contains(CacheType cacheType, string id)
        {
            string key = CacheObject.GenerateKey(cacheType, id);
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// Indexed accessor for the cache object
        /// </summary>
        /// <param name="cacheType">The cache type</param>
        /// <param name="id">The ID</param>
        /// <returns>The cache object</returns>
        internal CacheObject this[CacheType cacheType, string id]
        {
            get
            {
                string key = CacheObject.GenerateKey(cacheType, id);
                return _cache[key];
            }
            set 
            {
                string key = CacheObject.GenerateKey(value.Type, value.Id);
                _cache[key] = value;
                CacheIsDirty = true;
            }
        }

        /// <summary>
        /// Given a list, remove entries within the list that exist in the cache
        /// </summary>
        /// <param name="cacheType">The type of entry to check</param>
        /// <param name="current">The current list</param>
        internal List<string> GetIdListByCacheType(CacheType cacheType)
        {
            List<string> current = new List<string>();
            foreach (var entry in _cache)
            {
                if (entry.Value.Type == cacheType)
                {
                    current.Add(entry.Value.Id);
                }
            }
            return current;
        }

        /// <summary>
        /// Returns the size of the cache
        /// </summary>
        public int Count
        {
            get
            {
                return _cache.Count;
            }
        }

        /// <summary>
        /// Returns true if the entry is new or different.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <param name="checksum"></param>
        /// <returns></returns>
        public bool IsUpdated(CacheType type, string id, string checksum)
        {
            if (Contains(type, id))
            {
                if (checksum.Equals(this[type, id].Checksum))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Returns true if the type/id is a never-before-seen entry
        /// </summary>
        /// <param name="type"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool IsNew(CacheType type, string id)
        {
            return !Contains(type, id);
        }
    }
}
