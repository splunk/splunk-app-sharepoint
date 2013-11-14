using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2013.Audit
{
    class SplunkCache 
    {
        private Dictionary<Guid, Int64> _cache = new Dictionary<Guid, Int64>();

        /// <summary>
        /// Create a new cache object based on the cache file.
        /// </summary>
        /// <param name="directory">The directory containing the file</param>
        /// <param name="filename">The name of the file within the directory</param>
        public SplunkCache(string directory, string filename)
        {
            BackingFile = Path.Combine(directory, filename);
            Load();
        }

        /// <summary>
        /// Create a new cache object based on the fully qualified cache file.
        /// </summary>
        /// <param name="filename">The fully qualified filename</param>
        public SplunkCache(string filename)
        {
            BackingFile = filename;
            Load();
        }

        /// <summary>
        /// Create a new empty cache object.  The BackingFile must be set before a
        /// save is done.
        /// </summary>
        public SplunkCache()
        {
            BackingFile = null;
            CacheIsDirty = false;
        }

        /// <summary>
        /// The name of the backing file.
        /// </summary>
        public string BackingFile 
        { 
            get; 
            set; 
        }

        /// <summary>
        /// Status of the cache - true if it needs saving.
        /// </summary>
        public bool CacheIsDirty
        {
            get;
            set;
        }

        /// <summary>
        /// Load the backing file into the cache
        /// </summary>
        public void Load()
        {
            if (!File.Exists(BackingFile))
            {
                SystemLogger.Write(LogLevel.Warn, string.Format("SplunkCache.Load: File=\"{0}\" Message=\"File does not exist\"", BackingFile));
                return;
            }

            using (StreamReader reader = new StreamReader(BackingFile))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] arr = line.Split(',');
                    if (arr.Length == 2)
                    {
                        Guid key = new Guid(arr[0]);
                        Int64 value = Int64.Parse(arr[1]);
                        Add(key, value);
                    }
                }
            }

            CacheIsDirty = false;
        }


        /// <summary>
        /// Write out the cache file to the backing store.
        /// </summary>
        public void Save()
        {
            if (!CacheIsDirty)
            {
                SystemLogger.Write(LogLevel.Debug, string.Format("Cache is not dirty - skipping save"));
                return;
            }

            if (BackingFile == null)
            {
                SystemLogger.Write(LogLevel.Warn, string.Format("Cache Backing File not set --- duplicate events will be involved"));
                return;
            }

            SystemLogger.Write(LogLevel.Debug, string.Format("SplunkCache.Save: Writing {0} Entries to cache file {1}", _cache.Count, BackingFile));
            using (StreamWriter writer = new StreamWriter(BackingFile))
            {
                foreach (var pair in _cache)
                {
                    writer.WriteLine(string.Format("{0},{1}", pair.Key, pair.Value));
                }
                writer.Flush();
                writer.Close();
            }

            CacheIsDirty = false;
        }

        /// <summary>
        /// Adds the specified key and value to the cache
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public void Add(Guid key, Int64 value)
        {
            _cache.Add(key, value);
            CacheIsDirty = true;
        }

        /// <summary>
        /// Determines whether the cache contains the specific key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>true if the key exists</returns>
        public bool ContainsKey(Guid key)
        {
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// GEts an ICollection containing the keys of the cache
        /// </summary>
        public ICollection<Guid> Keys
        {
            get { return _cache.Keys; }
        }

        /// <summary>
        /// Removes the value with the specified key form the cache
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>true if the key is removed</returns>
        public bool Remove(Guid key)
        {
            CacheIsDirty = true;
            return _cache.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value (out)</param>
        /// <returns>true if the value was filled in</returns>
        public bool TryGetValue(Guid key, out Int64 value)
        {
            return _cache.TryGetValue(key, out value);
        }

        /// <summary>
        /// Gets an ICollection containing the values of the cache
        /// </summary>
        public ICollection<Int64> Values
        {
            get { return _cache.Values; }
        }

        /// <summary>
        /// Returns the value of a specific key
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value</returns>
        public Int64 this[Guid key]
        {
            get
            {
                return _cache[key];
            }
            set
            {
                _cache[key] = value;
                CacheIsDirty = true;
            }
        }

        /// <summary>
        /// Adds the specified key and value to the cache
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<Guid, Int64> item)
        {
            _cache.Add(item.Key, item.Value);
            CacheIsDirty = true;
        }

        /// <summary>
        /// Removes all keys and values from the cache
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            CacheIsDirty = true;
        }

        /// <summary>
        /// Returns true if the cache contains the key/value pair
        /// </summary>
        /// <param name="item">The key/value pair</param>
        /// <returns>true if it exists</returns>
        public bool Contains(KeyValuePair<Guid, Int64> item)
        {
            return _cache.ContainsKey(item.Key) && _cache[item.Key].Equals(item.Value);
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the cache
        /// </summary>
        public int Count
        {
            get { return _cache.Count; }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the cache
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<Guid, Int64>> GetEnumerator()
        {
            return _cache.GetEnumerator();
        }

        /// <summary>
        /// Updates or adds the key to the cache.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public void Update(Guid key, Int64 value)
        {
            if (_cache.ContainsKey(key))
            {
                _cache[key] = value;
            }
            else
            {
                _cache.Add(key, value);
            }
            CacheIsDirty = true;
        }
    }
}
