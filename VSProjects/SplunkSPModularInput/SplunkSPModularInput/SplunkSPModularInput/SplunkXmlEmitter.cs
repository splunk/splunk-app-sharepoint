using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Splunk.Sharepoint.ModularInputs
{
    [XmlRoot("stream")]
    public class SplunkXmlEmitter : SplunkEmitter
    {
        /// <summary>
        /// The Modular Input title.
        /// </summary>
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
                source = new Source();
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
            public Source source
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

       
        [XmlRoot("source")]
        public class Source
        {
            public string src
            {
                set;
                get;
            }

            public string Serialize()
            {
                XmlSerializer x = new XmlSerializer(typeof(Source));
                StringWriter sw = new StringWriter();
                x.Serialize(sw, this);
                Dictionary<string, string> dic = new Dictionary<string, string>();
                return sw.ToString();
            }
        }
    }

}
