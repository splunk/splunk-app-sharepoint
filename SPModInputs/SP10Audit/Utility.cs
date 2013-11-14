using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Splunk.ModularInputs;

namespace Splunk.SharePoint2010.Audit
{
    /// <summary>
    /// Basic static methods that implement things that should go in the SDK
    /// </summary>
    internal class Utility
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
        /// Retrieve a parameter value from the Modular Input Stanza
        /// </summary>
        /// <param name="stanza">The stanza object</param>
        /// <param name="parameterName">The name of the parameeter</param>
        /// <param name="defaultValue">The default value if the parameter does not exist</param>
        /// <returns>The value of the parameter</returns>
        internal static bool GetParameter(Stanza stanza, string parameterName, bool defaultValue)
        {
            string currentArgument;

            if (stanza.SingleValueParameters.TryGetValue(parameterName, out currentArgument))
            {
                return currentArgument.StartsWith("t", StringComparison.InvariantCultureIgnoreCase);
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
    }
}
