using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2013.Inventory
{
    class Utility
    {
        /// <summary>
        /// Retrieve a parameter value from the Modular Input Stanza
        /// </summary>
        /// <param name="stanza">The stanza object</param>
        /// <param name="parameterName">The name of the parameeter</param>
        /// <param name="defaultValue">The default value if the parameter does not exist</param>
        /// <returns>The value of the parameter</returns>
        internal static int GetParameter(Stanza stanza, string parameterName, int defaultValue)
        {
            string currentArgument;

            if (stanza.SingleValueParameters.TryGetValue(parameterName, out currentArgument))
            {
                return int.Parse(currentArgument);
            }
            return defaultValue;
        }


        /// <summary>
        /// Returns the checkpoint directory, with the side effect that it creates it if it does
        /// not exist.
        /// </summary>
        /// <param name="inputDefinition">The Input Definition</param>
        /// <returns>The Checkpoint Directory</returns>
        internal static string CheckpointDirectory(InputDefinition inputDefinition)
        {
            // Verify that out checkpoint directory exists - create it if not.
            if (!Directory.Exists(inputDefinition.CheckpointDirectory))
            {
                SystemLogger.Write(LogLevel.Warn, string.Format("Directory {0} does not exist - creating it", inputDefinition.CheckpointDirectory));
                Directory.CreateDirectory(inputDefinition.CheckpointDirectory);
            }
            return inputDefinition.CheckpointDirectory;
        }

        /// <summary>
        /// Calculate an MD5 checksum for a string
        /// </summary>
        /// <param name="input">The input string</param>
        /// <returns>The MD5 checksum</returns>
        internal static string CalculateMD5Hash(string input)
        {
            var md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Deal with a nullable object
        /// </summary>
        /// <param name="o">The object</param>
        /// <returns>A string</returns>
        internal static string Nullable(object o)
        {
            return o == null ? "" : o.ToString();
        }

        /// <summary>
        /// Returns a string that can be placed on a single line Splunk event
        /// </summary>
        /// <param name="s">the string</param>
        /// <returns>the new string</returns>
        internal static string Quotable(string s)
        {
            return s.Replace('"', '\'').Replace('\n', ' ').Replace('\r', ' ');
        }
    }
}
