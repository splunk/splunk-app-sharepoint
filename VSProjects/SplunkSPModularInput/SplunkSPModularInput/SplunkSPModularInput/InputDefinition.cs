using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Splunk.Sharepoint.ModularInputs
{
	/// <summary>
	/// When Splunk executes a modular input script, it reads configuration information from
	/// inputs.conf files in the system.  It then passes this configuration in XML format to
	/// the script.  The modular input script reads the configuration information from stdin.
	/// 
	/// This object is used to parse and access the XML data.
	/// </summary>
	[XmlRoot("input")]
	public class SharepointInputDefinition
	{
		private List<Stanza> _stanzas;

		/// <summary>
		/// Create a new empty Input Definition object
		/// </summary>
		public SharepointInputDefinition()
		{
            _stanzas = new List<Stanza>();
		}

		/// <summary>
		/// Read the input stream specified and return the parsed XML input.
		/// </summary>
		/// <param name="input">The input stream</param>
		/// <returns>An SharepointInputDefinition object</returns>
		public static SharepointInputDefinition ReadSharepointInputDefinition(TextReader input)
		{
			XmlSerializer x = new XmlSerializer(typeof(SharepointInputDefinition));
			SharepointInputDefinition id = (SharepointInputDefinition)x.Deserialize(input);
			return id;
		}

		/// <summary>
		/// The hostname for the splunk server
		/// </summary>
		[XmlElement("server_host")]
		public string ServerHost
		{ get; set; }

		/// <summary>
		/// The management port for the splunk server, identified by host, port and protocol
		/// </summary>
		[XmlElement("server_uri")]
		public string ServerUri
		{ get; set; }

		/// <summary>
		/// The directory used for a script to save checkpoints.  This is where splunk tracks the
		/// input state from sources from which it is reading.
		/// </summary>
		[XmlElement("checkpoint_dir")]
		public string CheckpointDirectory
		{ get; set; }

        /// <summary>
        /// The REST API session key for this modular input
        /// </summary>
        [XmlElement("session_key")]
        public string SessionKey
        { get; set; }

              
		/// <summary>
		/// The child tags for &lt;configuration&gt; are based on the schema you define in the
		/// inputs.conf.spec file for your modular input.  Splunk reads all the configurations in
		/// the Splunk installation and passes them to the script in &lt;stanza&gt; tags.
		/// </summary>
		[XmlArray("configuration")]
		[XmlArrayItem("stanza")]
		public List<Stanza> Stanzas
		{
			get { return _stanzas; }
			set { _stanzas = value; }
		}

		/// <summary>
		/// Serializes this object to XML output
		/// </summary>
		/// <returns>The XML String</returns>
		public string Serialize()
		{
			XmlSerializer x = new XmlSerializer(typeof(SharepointInputDefinition));
			StringWriter sw = new StringWriter();
			x.Serialize(sw, this);
			return sw.ToString();
		}
	}

	/// <summary>
	/// Each stanza in the inputs.conf has a set of parameters that are stored in a KV pair store.
	/// </summary>
	[XmlRoot("stanza")]
	public class Stanza
	{
		private List<Parameter> _params;

		/// <summary>
		/// Create a new empty stanza.
		/// </summary>
		public Stanza()
		{
			_params = new List<Parameter>();
		}

		/// <summary>
		/// The name of this stanza.
		/// </summary>
		[XmlAttribute("name")]
		public string Name
		{ get; set; }

		/// <summary>
		/// The list of parameters for defining this stanza.
		/// </summary>
		[XmlElement("param")]
		public List<Parameter> Parameters
		{
			get { return _params; }
			set { _params = value; }
		}

		/// <summary>
		/// When accessing the parameters, normally you will want to access the
		/// parameters by name.  This method translates the list into an associative
		/// array for access purposes.
		/// </summary>
		/// <param name="name">The name of the parameter</param>
		/// <param name="defaultValue">If not found, what should be returned</param>
		/// <returns>The value of the parameter, or defaultValue if the parameter does not exist.</returns>
		public string GetParameterByName(string name, string defaultValue)
		{
			for (int i = 0; i < _params.Count; i++)
			{
				if (_params[i].Name.Equals(name))
					return _params[i].Value;
			}
			return defaultValue;
		}

		/// <summary>
		/// Serializes this object to XML output
		/// </summary>
		/// <returns>The XML String</returns>
		public string Serialize()
		{
			XmlSerializer x = new XmlSerializer(typeof(Stanza));
			StringWriter sw = new StringWriter();
			x.Serialize(sw, this);
			return sw.ToString();
		}
	}

	/// <summary>
	/// Definition of a key-value pair in the context of an XML object
	/// </summary>
	[XmlRoot("param")]
	public class Parameter
	{
		/// <summary>
		/// The name of the parameter
		/// </summary>
		[XmlAttribute("name")]
		public string Name
		{ get; set; }

		/// <summary>
		/// The value of the parameter
		/// </summary>
		[XmlText]
		public string Value
		{ get; set; }

		/// <summary>
		/// Serializes this object to XML output
		/// </summary>
		/// <returns>The XML String</returns>
		public string Serialize()
		{
			XmlSerializer x = new XmlSerializer(typeof(Parameter));
			StringWriter sw = new StringWriter();
			x.Serialize(sw, this);
			return sw.ToString();
		}

	}
}