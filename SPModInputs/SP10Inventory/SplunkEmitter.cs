using System;
using System.Collections.Generic;
using System.Text;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2010.Inventory
{
    /// <summary>
    /// Emits a SP13Inventory event
    /// </summary>
    internal class SplunkEmitter
    {
        /// <summary>
        /// The type of this cache entry
        /// </summary>
        internal CacheType CacheType { get; set; }

        /// <summary>
        /// The time stamp for this object
        /// </summary>
        internal DateTime Timestamp { get; set; }

        /// <summary>
        /// The list of parameters to this 
        /// </summary>
        private Dictionary<string, string> _params = new Dictionary<string, string>();

        /// <summary>
        /// Create a new SplunkEmitter
        /// </summary>
        internal SplunkEmitter()
        {
            CacheType = CacheType.Unknown;
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Convert the SplunkEmitter to something we actually output
        /// </summary>
        /// <returns></returns>
        internal EventElement ToEmitter()
        {
            List<string> x = new List<string>();

            x.Add(Timestamp.ToString("u"));
            x.Add(string.Format("Type=\"{0}\"", CacheType.ToString()));
 
            foreach (var p in _params)
            {
                x.Add(string.Format("{0}=\"{1}\"", p.Key, p.Value));
            }

            return new EventElement { Data=string.Join("\n", x.ToArray()), Time=Timestamp };
        }

        /// <summary>
        /// Add a new key/value pair to the list of entries.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="str">The value</param>
        internal void Add(string key, string str)
        {
            _params[key] = str;
        }
    }
}
