using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;


namespace Splunk.Sharepoint.ModularInputs
{
    /// <summary>
    /// The SharepointScheme class represents the XML output when a Modular Input is called
    /// with the --scheme argument.
    /// </summary>
    [XmlRoot("scheme")]
    public class SharepointScheme
    {
        /// <summary>
        /// Sets up default values for this SharepointScheme
        /// </summary>
        public SharepointScheme()
        {
            UseExternalValidation = false;
            UseSingleInstance = false;
            Endpoint = new Endpoint();
        }

        /// <summary>
        /// The Modular Input title.
        /// </summary>
        [XmlElement("title")]
        public string Title
        { get; set; }

        /// <summary>
        /// The Modular Input description.
        /// </summary>
        [XmlElement("description")]
        public string Description
        { get; set; }

        /// <summary>
        /// True if external validation is enabled for this Modular Input.
        /// Default is false.
        /// </summary>
        [XmlElement("use_external_validation")]
        public Boolean UseExternalValidation
        { get; set; }

        /// <summary>
        /// Indicates whether to launch a single instance of the script or
        /// one script instance for each input stanza.  Default is false.
        /// </summary>
        [XmlElement("use_single_instance")]
        public Boolean UseSingleInstance
        { get; set; }

        /// <summary>
        /// Bi-directional Conversion routine for StreamingMode to string
        /// </summary>
        [XmlElement("streaming_mode")]
        string StreamingMode_As_String
        {
            get 
            {
                switch (StreamingMode)
                {
                    case StreamingMode.SIMPLE:
                        return "simple";
                    case StreamingMode.XML:
                        return "xml";
                    default:
                        return "simple";
                }
            }
            set 
            {
                if (value.ToLower().Equals("simple"))
                {
                    StreamingMode = StreamingMode.SIMPLE;
                }
                else if (value.ToLower().Equals("xml"))
                {
                    StreamingMode = StreamingMode.XML;
                }
                else
                {
                    throw new ArgumentException("Invalid Argument " + value);
                }
            }
        }

        /// <summary>
        /// Streaming Mode for this Modular Input (SIMPLE or XML)
        /// </summary>
        [XmlIgnore]
        public StreamingMode StreamingMode
        { get; set; }

        /// <summary>
        /// The endpoint description for this modular input
        /// </summary>
        [XmlElement("endpoint")]
        public Endpoint Endpoint
        { get; set; }

        /// <summary>
        /// Serializes this object to XML output
        /// </summary>
        /// <returns>The XML String</returns>
        public string Serialize()
        {
            XmlSerializer x = new XmlSerializer(typeof(SharepointScheme));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            return sw.ToString();
        }
    }

    /// <summary>
    /// Enumeration of the valid values for the SharepointScheme Streaming Mode
    /// </summary>
    public enum StreamingMode
    {
        /// <summary>
        /// A plain-text modular input
        /// </summary>
        SIMPLE,
        /// <summary>
        /// Data is streamed to splunk with XML objects
        /// </summary>
        XML
    }

    /// <summary>
    /// Enumeration of the valid values for the Endpoint Argument data type.
    /// </summary>
    public enum ArgumentDataType
    {
        /// <summary>
        /// A boolean value - true or false
        /// </summary>
        BOOLEAN,
        /// <summary>
        /// A numeric value - regexp = [0-9\.]+
        /// </summary>
        NUMBER,
        /// <summary>
        /// A string - virtually everything else
        /// </summary>
        STRING
    }

    /// <summary>
    /// The Endpoint is a collection of arguments that represent parameters
    /// to the inputs.conf stanza
    /// </summary>
    [XmlRoot("endpoint")]
    public class Endpoint
    {
        private List<EndpointArgument> _args;

        /// <summary>
        /// Create a new, empty Endpoint object
        /// </summary>
        public Endpoint()
        {
            _args = new List<EndpointArgument>();
        }

        /// <summary>
        /// The list of arguments to this endpoint.  Note that this represents
        /// the parameters list for the InputDefinition as well (with some standard
        /// exceptions).
        /// </summary>
        [XmlArray("args")]
        [XmlArrayItem("arg")]
        public List<EndpointArgument> Arguments
        {
            get { return _args; }
            set { _args = value; }
        }

        /// <summary>
        /// Serializes this object to XML output
        /// </summary>
        /// <returns>The XML String</returns>
        public string Serialize()
        {
            XmlSerializer x = new XmlSerializer(typeof(Endpoint));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            return sw.ToString();
        }
    }

    /// <summary>
    /// The EndpointArgument is the XML entity that describes the arguments
    /// that can be placed in to the inputs.conf stanza for this modular
    /// input.
    /// </summary>
    [XmlRoot("arg")]
    public class EndpointArgument
    {
        /// <summary>
        /// Create a new, empty EndpointArgument object.
        /// </summary>
        public EndpointArgument()
        {
            DataType = ArgumentDataType.STRING;
            RequiredOnEdit = false;
            RequiredOnCreate = false;
        }

        /// <summary>
        /// The name is a unique Name for this parameter
        /// </summary>
        [XmlAttribute("name")]
        public string Name
        { get; set; }

        /// <summary>
        /// Provides a label for the parameter
        /// </summary>
        [XmlElement("title")]
        public string Title
        { get; set; }

        /// <summary>
        /// Provides a description of the parameter
        /// </summary>
        [XmlElement("description")]
        public string Description
        { get; set; }

        /// <summary>
        /// Defines validation rules for arguments passed to an endpoint create or edit action.
        /// </summary>
        [XmlElement("validation")]
        public string Validation
        { get; set; }

        /// <summary>
        /// For use with scripts that return data in JSON format.  Defines the
        /// data type of the parameter.  Default data type is string.
        /// </summary>
        [XmlIgnore]
        public ArgumentDataType DataType
        { get; set; }

        /// <summary>
        /// Provides bi-directional conversion between the ArgumentDataType 
        /// and strings for writing to XML streams.
        /// </summary>
        [XmlElement("data_type")]
        string DataType_As_String
        {
            get
            {
                switch (DataType)
                {
                    case ArgumentDataType.BOOLEAN:
                        return "boolean";
                    case ArgumentDataType.NUMBER:
                        return "number";
                    case ArgumentDataType.STRING:
                        return "string";
                    default:
                        return "string";
                }
            }
            set
            {
                if (value.ToLower().Equals("boolean"))
                {
                    DataType = ArgumentDataType.BOOLEAN;
                }
                else if (value.ToLower().Equals("number"))
                {
                    DataType = ArgumentDataType.NUMBER;
                }
                else if (value.ToLower().Equals("string"))
                {
                    DataType = ArgumentDataType.STRING;
                }
                else
                {
                    throw new ArgumentException("Bad Value for Data Type");
                }
            }
        }

        /// <summary>
        /// Indicates whether the parameter is required for edit.  Default behavior
        /// is that arguments for edit are optional.  Set this to true to override
        /// this behavior, and make the parameter required.
        /// </summary>
        [XmlElement("required_on_edit")]
        public Boolean RequiredOnEdit
        { get; set; }

        /// <summary>
        /// Indicates whether the parameter is required for create.  Default behavior
        /// is that arguments for create are optional.  Set this to true to override
        /// this behavior, and make the parameter required.
        /// </summary>
        [XmlElement("required_on_create")]
        public Boolean RequiredOnCreate
        { get; set; }

        /// <summary>
        /// Serializes this object to XML output
        /// </summary>
        /// <returns>The XML String</returns>
        public string Serialize()
        {
            XmlSerializer x = new XmlSerializer(typeof(EndpointArgument));
            StringWriter sw = new StringWriter();
            x.Serialize(sw, this);
            return sw.ToString();
        }
    }
}
