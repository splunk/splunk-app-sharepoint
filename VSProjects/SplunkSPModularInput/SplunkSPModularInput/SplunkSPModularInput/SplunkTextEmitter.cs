using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splunk.Sharepoint.ModularInputs
{
    public class SplunkTextEmitter : SplunkEmitter
    {
        //writes data in text format
        public void emit(String data)
        {
            Console.Out.WriteLine(data + "\r\n");
            Console.Out.Flush();
        }
    }
}
