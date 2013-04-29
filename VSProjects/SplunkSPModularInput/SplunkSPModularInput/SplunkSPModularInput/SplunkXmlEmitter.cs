using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Splunk.Sharepoint.ModularInputs
{
    /// <summary>
    /// The SplunkXmlEmitter class emits the output in XML format if the Streaming mode in the Introspection scheme is set to XML
    /// </summary>
    
    [XmlRoot("stream")]
    public class SplunkXmlEmitter : SplunkEmitter
    {
       
        [XmlElement("event")]
        public XmlEmitterEvent Event
        { get; set; }

        public SplunkXmlEmitter()
        {
            Event = new XmlEmitterEvent();
        }

        public string Serialize()
        {
            XmlSerializer x = new XmlSerializer(typeof(SplunkXmlEmitter));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            return sw.ToString();
        }

        public void emit(string data)
        {
            XmlSerializer x = new XmlSerializer(typeof(SplunkXmlEmitter));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            Console.Out.WriteLine(sw.ToString());
            Console.Out.Flush();
        }

        [XmlRoot("event")]
        public class XmlEmitterEvent
        {
            public XmlEmitterEvent()
            {
                data = new List<string>();
                source = "";
            }

            [XmlAttribute("stanza")]
            public string stanza
            {
                set;
                get;
            }
            [XmlAttribute("unbroken")]
            public int unbroken
            {
                set;
                get;
            }

            

            [XmlElement("data")]
            public List<string> data
            {
                set;
                get;
            }

            [XmlElement("done")]
            public string done
            {
                set;
                get;
            }

            public void AddDoneKey()
            {
                done = "";
            }

            [XmlElement("source")]
            public string source
            {
                set;
                get;
            }

            public string Serialize()
            {
                XmlSerializer x = new XmlSerializer(typeof(XmlEmitterEvent));
                StringWriter sw = new StringWriter();
                x.Serialize(sw, this);
                return sw.ToString();
            }

        }
        
    }

}
