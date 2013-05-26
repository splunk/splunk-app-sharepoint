using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splunk.Sharepoint.ModularInputs
{
    /// <summary>
    /// The SplunkTextEmitter class emits the output in text format if the Streaming mode in the Introspection scheme is set to SIMPLE
    /// </summary>
    public class SplunkTextEmitter : SplunkEmitter
    {
        /// <summary>
        /// The SplunkTextEmitter class emits the output in text format if the Streaming mode in the Introspection scheme is set to SIMPLE
        /// </summary>
        /// <param name="data">The data to be indexed into splunk</param>

        public void emit(String data)
        {
            Console.Out.WriteLine(data + "\r\n");
            Console.Out.Flush();
        }
    }
}
