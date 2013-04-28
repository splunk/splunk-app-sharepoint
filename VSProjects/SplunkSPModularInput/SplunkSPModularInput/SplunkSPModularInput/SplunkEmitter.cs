using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Splunk.Sharepoint.ModularInputs
{
    public interface SplunkEmitter
    {
        //writes data into Splunk
        void emit(String data);
    }
}
