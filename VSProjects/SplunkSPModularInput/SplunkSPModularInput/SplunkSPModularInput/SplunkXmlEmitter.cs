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
        /// <summary>
        /// 
        /// </summary>
        [XmlElement("event")]
        public XmlEmitterEvent Event
        { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SplunkXmlEmitter()
        {
            Event = new XmlEmitterEvent();
        }

        /// <summary>
        /// 
        /// </summary>
        public string Serialize()
        {
            XmlSerializer x = new XmlSerializer(typeof(SplunkXmlEmitter));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            return sw.ToString();
        }

        /// <summary>
        /// This method emits the data into Splunk
        /// </summary>
        public void emit(string data)
        {
            XmlSerializer x = new XmlSerializer(typeof(SplunkXmlEmitter));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            Console.Out.WriteLine(sw.ToString());
            Console.Out.Flush();
        }

        /// <summary>
        /// 
        /// </summary>
        [XmlRoot("event")]
        public class XmlEmitterEvent
        {

            public XmlEmitterEvent()
            {
                data = new List<string>();
                source = "";
            }

            /// <summary>
            /// The stanza attribute is used in the <event> tag to specify the stanza for each event
            /// </summary>
            [XmlAttribute("stanza")]
            public string stanza
            {
                set;
                get;
            }

            /// <summary>
            /// 
            /// </summary>
            [XmlAttribute("unbroken")]
            public int unbroken
            {
                set;
                get;
            }

            /// <summary>
            /// Create a set of data tag. The <data> tags contains event entries
            /// </summary>
            [XmlElement("data")]
            public List<string> data
            {
                set;
                get;
            }

            /// <summary>
            ///  Create a <done> tag.The <done> tag to denote an end of a stream with unbroken events. The <done> tag tells Splunk to flush the data from its buffer rather than wait for more data before processing it
            /// </summary>
            [XmlElement("done")]
            public string done
            {
                set;
                get;
            }

            /// <summary>
            /// 
            /// </summary>
            public void AddDoneKey()
            {
                done = "";
            }

            /// <summary>
            /// 
            /// </summary>
            [XmlElement("source")]
            public string source
            {
                set;
                get;
            }

            /// <summary>
            /// 
            /// </summary>
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
