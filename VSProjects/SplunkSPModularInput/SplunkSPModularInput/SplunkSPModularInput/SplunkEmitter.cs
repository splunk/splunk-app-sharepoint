using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splunk.Sharepoint.ModularInputs
{
    /// <summary>
    /// SplunkEmitter helps to emit the data into Splunk in different formats like XML,text etc
    /// </summary>
    public interface SplunkEmitter
    {
        //writes data into Splunk
        void emit(String data);
    }
}
