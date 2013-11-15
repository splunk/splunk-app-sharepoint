using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2010.Inventory
{
    internal class CacheObject
    {
        /// <summary>
        /// The type of this cache object
        /// </summary>
        public CacheType Type { get; set; }

        /// <summary>
        /// A unique string that identifies this cache object
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The last time this cache object was emitted
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// A checksum value for this object
        /// </summary>
        public string Checksum { get; set; }

        /// <summary>
        /// Create a new Cache Object based on a string that was previously generated via
        /// the ToString() call.
        /// </summary>
        /// <param name="s">The string to parse</param>
        /// <returns>A new CacheObject</returns>
        public static CacheObject Parse(string s)
        {
            Match m = Regex.Match(s, "\\{type=\\\"([^\\\"]+)\\\",id=\\\"([^\\\"]+)\\\",lastUpdated=\\\"([^\\\"]+)\\\",checksum=\\\"([^\\\"]+)\\\"");
            if (!m.Success)
            {
                throw new ApplicationException("Invalid CacheObject Data Input");
            }

            // Convert the four strings to the input types
            CacheType type = CacheObject.ParseCacheType(m.Groups[1].ToString());
            string id = m.Groups[2].ToString();
            DateTime timestamp = new DateTime(long.Parse(m.Groups[3].ToString()), DateTimeKind.Utc);
            string checksum = m.Groups[4].ToString();

            return new CacheObject { Type = type, Id = id, LastUpdated = timestamp, Checksum = checksum };
        }

        /// <summary>
        /// Return a string representation of this cache object
        /// </summary>
        /// <returns>a string</returns>
        public override string ToString()
        {
            return "{" + string.Format("type=\"{0}\",id=\"{1}\",lastUpdated=\"{2}\",checksum=\"{3}\"",
                Type.ToString(), Id, LastUpdated.ToUniversalTime().Ticks.ToString(), Checksum) + "}";
        }

        /// <summary>
        /// Return the equivalent key for this cache object
        /// </summary>
        public string Key
        {
            get
            {
                return CacheObject.GenerateKey(Type, Id);
            }
        }

        /// <summary>
        /// Given a cache type and an ID, return the unique Key
        /// </summary>
        /// <param name="cacheType">The cache type</param>
        /// <param name="id">The Id</param>
        /// <returns>The cache key</returns>
        internal static string GenerateKey(CacheType cacheType, string id)
        {
            return string.Format("{0}#{1}", cacheType.ToString(), id);
        }

        /// <summary>
        /// Convert a string cache type to a CacheType
        /// </summary>
        /// <param name="sType">The type string</param>
        /// <returns>The CacheType</returns>
        private static CacheType ParseCacheType(string sType)
        {
            if (sType.Equals("Farm"))
                return CacheType.Farm;
            else if (sType.Equals("AlternateUrl"))
                return CacheType.AlternateUrl;
            else if (sType.Equals("ApplicationPool"))
                return CacheType.ApplicationPool;
            else if (sType.Equals("ContentDatabase"))
                return CacheType.ContentDatabase;
            else if (sType.Equals("DiagnosticsProvider"))
                return CacheType.DiagnosticsProvider;
            else if (sType.Equals("Feature"))
                return CacheType.Feature;
            else if (sType.Equals("FeatureDefinition"))
                return CacheType.FeatureDefinition;
            else if (sType.Equals("List"))
                return CacheType.List;
            else if (sType.Equals("Policy"))
                return CacheType.Policy;
            else if (sType.Equals("Prefix"))
                return CacheType.Prefix;
            else if (sType.Equals("Server"))
                return CacheType.Server;
            else if (sType.Equals("ServiceInstance"))
                return CacheType.ServiceInstance;
            else if (sType.Equals("Site"))
                return CacheType.Site;
            else if (sType.Equals("User"))
                return CacheType.User;
            else if (sType.Equals("Web"))
                return CacheType.Web;
            else if (sType.Equals("WebApplication"))
                return CacheType.WebApplication;
            else if (sType.Equals("WebTemplate"))
                return CacheType.WebTemplate;
            else
                return CacheType.Unknown;
        }
    }

    /// <summary>
    /// The various types of cache we have available
    /// </summary>
    internal enum CacheType
    {
        Unknown,
        Error,
        AlternateUrl,
        ApplicationPool,
        ContentDatabase,
        DiagnosticsProvider,
        Farm,
        Feature,
        FeatureDefinition,
        List,
        Policy,
        Prefix,
        Server,
        ServiceInstance,
        Site,
        User,
        Web,
        WebApplication,
        WebTemplate,
    }
}
