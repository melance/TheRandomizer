﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using NCalc;
using TheRandomizer.Generators.Parameter;
using System.IO;
using System.ComponentModel.DataAnnotations;
using LiteDB;
using TheRandomizer.Generators.Attributes;
using System.Collections.ObjectModel;
using TheRandomizer.Utility;
using TheRandomizer.Generators.Lua;

namespace TheRandomizer.Generators
{
    /// <summary>
    /// This is the base class for all generator types.
    /// </summary>
    [XmlRoot("generator", Namespace="")]
    public abstract class BaseGenerator : ObservableBase
    {
        #region Static Members
        private static List<Type> _knownTypes;
        #endregion

        #region Static Methods
        /// <summary>
        /// Deserializes the xml into a the appropriate generator object as the base type
        /// </summary>
        /// <param name="xml">The xml to deserialize</param>
        /// <returns>A generator object as the base type</returns>
        public static BaseGenerator Deserialize(string xml)
        {
            // Transform the document to the latest version
            xml = TransformXml.TransformToLatestVersion(xml);

            // Perform the serialization
            using (var stream = XmlReader.Create(new StringReader(xml)))
            {
                return Deserialize(stream);
            }
        }

        /// <summary>
        /// Deserializes the xml reader into a the appropriate generator object as the base type
        /// </summary>
        /// <param name="reader">The xml reader to deserialize</param>
        /// <returns>A generator object as the base type</returns>
        public static BaseGenerator Deserialize(XmlReader reader)
        {
            var serializer = new XmlSerializer(typeof(BaseGenerator), KnownTypes());
            var generator = (BaseGenerator)serializer.Deserialize(reader);
            generator.MarkClean();
            return generator;
        }
        
        /// <summary>
        /// Returns the array of types that inherit from BaseGenerator
        /// </summary>
        /// <returns>An array of type that inherit from BaseGenerator</returns>
        static Type[] KnownTypes()
        {
            if (_knownTypes == null)
            {
                _knownTypes = new List<Type>(Assembly.GetExecutingAssembly().GetTypes().Where((Type t) => typeof(BaseGenerator).IsAssignableFrom(t)).ToArray());
            }
            return _knownTypes.ToArray();
        }
        #endregion

        #region Events
        /// <summary>Requests the calling application provide the configured generator</summary>
        public event EventHandler<RequestGeneratorEventArgs> RequestGenerator;
        /// <summary>Requests the calling application provide the configured string</summary>
        public event EventHandler<RequestFileTextEventArgs> RequestFileText;
        #endregion

        #region Constants
        private const string GENERATE_FUNCTION = "Generate";
        internal const Int32 LATEST_VERSION = 2;
        private const string TAG_DELIMITER = ", ";
        #endregion

        #region Constructors
        public BaseGenerator()
        {
            _cleanHash = GetHashCode();
        }
        #endregion

        #region Members
        private Random _random;
        private static Expression _calculator;
        private int _cleanHash;
        private bool _cancelling = false;
        private bool _generating = false;
        #endregion

        #region Public Properties
        [XmlIgnore]
        [BsonId(true)]
        public Guid Id { get; set; }
        /// <summary>The name of the generator that is to be displayed to the user</summary>
        [XmlElement("name")]
        [Required]
        public string Name { get { return GetProperty<string>(); } set { SetProperty(value); } }
        /// <summary>The identifying name of the author</summary>
        [XmlElement("author")]
        public string Author { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool AuthorSpecified { get { return !string.IsNullOrWhiteSpace(Author); } }
        /// <summary>A long description of the purpose and output of the generator</summary>
        [XmlElement("description")]
        [Required]
        public string Description { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool DescriptionSpecified { get { return !string.IsNullOrWhiteSpace(Description); } }
        /// <summary>The version of the generator xml, provided for breaking changes</summary>
        [XmlAttribute("version")]
        public virtual Int32 Version { get { return GetProperty(LATEST_VERSION); } set { SetProperty(value); } }
        /// <summary>A URL that can be displayed to the user, such as the website of the author</summary>
        [XmlElement("url")]
        [Url]
        public string Url { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public bool UrlSpecified { get { return !string.IsNullOrWhiteSpace(Url); } }
        /// <summary>The format of the generator results</summary>
        [XmlIgnore]
        public OutputFormat OutputFormat { get { return GetProperty(OutputFormat.Html); } set { SetProperty(value); } } 
        /// <summary>A list of tags used to categorize the generator</summary>
        [XmlArray("tags")]
        [XmlArrayItem("tag")]
        public ObservableCollection<Tag> Tags { get; } = new ObservableCollection<Tag>();
        public bool TagsSpecified { get { return Tags.Count > 0; } }

        protected int MaxLength { get; set; }

        [XmlIgnore]
        [Required]
        [Display(Name = "Tags")]
        public string TagList
        {
            get
            {
                return string.Join(TAG_DELIMITER, Tags.Select(t => t.Value));
            }
            set
            {
                if (value == null) { value = string.Empty; }
                foreach (var tag in value.Split(new string[] { TAG_DELIMITER }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()))
                {
                    Tags.Add(tag);
                }
            }
        }

        /// <summary>A boolean that determines if the generator supports limiting the length of the generated value</summary>
        [XmlElement("supportsMaxLength")]
        [Display(Name = "Supports Max Length")]
        public virtual bool? SupportsMaxLength { get { return GetProperty(false); } set { SetProperty(value); } }
        public virtual bool SupportsMaxLengthSpecified { get { return SupportsMaxLength.HasValue; } }

        /// <summary>A list of parameters to provide to the generator</summary>
        [XmlArray("parameters")]
        [XmlArrayItem("parameter")]
        public virtual ConfigurationList Parameters { get; set; } = new ConfigurationList();
        public virtual bool ParametersSpecified { get { return Parameters != null && Parameters.Count > 0; } }

        [XmlElement("css")]
        public virtual string CSS { get { return GetProperty<string>(); } set { SetProperty(value); } }
        public virtual bool CSSSpecified { get { return !string.IsNullOrWhiteSpace(CSS); } }

        [XmlIgnore]
        public virtual bool IsDirty { get { return _cleanHash == GetHashCode(); } }

        [XmlIgnore]
        public virtual bool Published { get { return GetProperty(false); } set { SetProperty(value); } }

        [XmlIgnore]
        public GeneratorType GeneratorType
        {
            get
            {
                var attribute = (GeneratorDisplayAttribute)GetType().GetCustomAttribute(typeof(GeneratorDisplayAttribute), true);
                if (attribute != null)
                {
                    return attribute.GeneratorType;
                }
                else
                {
                    return GeneratorType.Unknown;
                }
            }
        }
        #endregion

        #region Protected Properties
        /// <summary>Returns a random number generator that lasts for the life of the object to ensure a good spread on random numbers</summary>
        protected Random Random
        {
            get
            {
                if (_random == null) _random = new Random();
                return _random;
            }
        }

        protected Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        #endregion

        #region Public Methods
        /// <summary>
        /// Stops the generation process before it is complete
        /// </summary>
        public virtual void Cancel()
        {
            if (_generating) _cancelling = true;
        }

        /// <summary>
        /// Calls the inheriting class' GenerateInteral one time with no parameters and no max length
        /// </summary>
        /// <returns>A generated value</returns>
        public virtual string Generate()
        {
            return Generate(1, null).First();
        }
        
        /// <summary>
        /// Calls the inheriting class' GenerateInternal as many times as specified with no max length or parameters
        /// </summary>
        /// <param name="repeat">The number of times to call GenerateInternal</param>
        /// <returns></returns>
        public virtual IEnumerable<string> Generate(Int32 repeat)
        {
            return Generate(repeat, null);
        }

        /// <summary>
        /// Calls the inheriting class' GenerateInternal as many times as is specified by the <paramref name="repeat">repeat</paramref> parameter.
        /// </summary>
        /// <param name="repeat">The number of times to call GenerateInternal</param>
        /// <param name="maxLength">The maximum length of each generated value, if supported.  If null then it is set to Int32.MaxLength.</param>
        /// <param name="parameters">A list of parameters to provide to the inheriting generator.</param>
        /// <returns>A list of generated values</returns>
        public virtual IEnumerable<string> Generate(Int32 repeat, Int32? maxLength)
        {
            var values = new List<string>();
            _generating = true;
            if (!maxLength.HasValue || (SupportsMaxLength == true && maxLength == 0)) maxLength = Int32.MaxValue;
            MaxLength = (int)maxLength;

            AssignParameterValues();

            for (var i=0; i < repeat && !_cancelling; i++)
            {
                values.Add(GenerateInternal(maxLength));
            }
            _generating = false;
            _cancelling = false;
            return values;
        }

        /// <summary>
        /// Serializes this generator as an XML string
        /// </summary>
        /// <returns>An xml string</returns>
        public string Serialize()
        {
            string value;
            using (var stream = new StringWriter())
            {
                var settings = new XmlWriterSettings();
                settings.Indent = true;
                using (var xml = XmlWriter.Create(stream,settings))
                {
                    Serialize(xml);
                }
                value = stream.ToString();
            }
            return value;
        }

        /// <summary>
        /// Serializes this generator to an Xml writer
        /// </summary>
        public void Serialize(XmlWriter xml)
        {
            var serializer = new XmlSerializer(typeof(BaseGenerator), KnownTypes());
            serializer.Serialize(xml, this);
        }

        public void MarkClean()
        {
            _cleanHash = GetHashCode();
        }

        public GeneratorInfo AsGeneratorInfo()
        {
            var generator = new GeneratorInfo();
            generator.Id = Id;
            generator.Name = Name;
            generator.Author = Author;
            generator.Description = Description;
            generator.OutputFormat = OutputFormat;
            generator.SupportsMaxLength = SupportsMaxLength;
            foreach (var tag in Tags)
            {
                generator.Tags.Add(tag);
            }
            generator.Url = Url;
            generator.Version = Version;
            generator.Published = Published;
            if (GetType() == typeof(Assignment.AssignmentGenerator)) generator.IsLibrary = ((Assignment.AssignmentGenerator)this).IsLibrary;
            return generator;
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Implemented in the child generator, this is the method that produces the content
        /// </summary>
        /// <param name="maxLength">The maximum length of each generated value, if supported.  If null then it is set to Int32.MaxLength.</param>
        /// <param name="parameters">A list of parameters to provide to the inheriting generator.</param>
        /// <returns>The generated value</returns>
        protected abstract string GenerateInternal(Int32? maxLength);

        /// <summary>
        /// Runs the provided expression throug the NCalc engine
        /// </summary>
        /// <param name="expression">The expression to evaluate</param>
        /// <returns>The result of the calculation</returns>
        protected string Calculate(string expression)
        {
            object value;
            _calculator = new Expression(expression, EvaluateOptions.NoCache);
            _calculator.EvaluateFunction += OnEvaluateFunction;
            _calculator.EvaluateParameter += OnEvaluateParameter;
            value = _calculator.Evaluate();
            _calculator.EvaluateFunction -= OnEvaluateFunction;
            _calculator.EvaluateParameter -= OnEvaluateParameter;
            return value.ToString();
        }

        /// <summary>
        /// Called when the base generator doesn't isn't aware of the function named
        /// </summary>
        protected virtual void EvaluateFunction(string name, FunctionArgs e) { }

        /// <summary>
        /// Called when the base generator doesn't isn't aware of the parameter named
        /// </summary>
        protected virtual void EvaluateParameter(string name, ParameterArgs e) { }        
        #endregion

        #region Private Methods
        /// <summary>
        /// Adds the parameters and their values to the variable list
        /// </summary>
        /// <param name="userParameters">The list of parameters from the user</param>
        private void AssignParameterValues()
        {
            if (Parameters != null)
            {
                // Add configured parameters
                foreach (var parameter in Parameters)
                {
                    if (!Variables.ContainsKey(parameter.Name))
                    {
                        Variables.Add(parameter.Name, parameter.Value);
                    }
                    else
                    {
                        Variables[parameter.Name] = parameter.Value;
                    }
                }                
            }
        }

        /// <summary>
        /// Handles custom functions for the NCalc engine
        /// </summary>
        protected void OnEvaluateFunction(string name, FunctionArgs e)
        {
            if (CustomNCalcFunctions.Functions.ContainsKey(name))
            {
                CustomNCalcFunctions.Functions[name].Invoke(e);
            }
            else
            {
                switch (name)
                {
                    case GENERATE_FUNCTION:
                        var parameters = e.EvaluateParameters();
                        if (parameters.Count() > 0)
                        {
                            var arg = new RequestGeneratorEventArgs(parameters[0].ToString());
                            BaseGenerator generator;
                            OnRequestGenerator(arg);
                            generator = arg.Generator;
                            var maxLength = Int32.MaxValue;
                            if (parameters.Count() > 1)
                            {
                                for (var i = 1; i < parameters.Count(); i += 2)
                                {
                                    var paramName = parameters[i].ToString();
                                    if (paramName.Equals("MaxLength", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        maxLength = Int32.Parse(parameters[i + 1].ToString());
                                    }
                                    else
                                    {
                                        generator.Parameters[paramName].Value = parameters[i + 1].ToString();
                                    }
                                }
                            }
                            generator.RequestGenerator += RequestGenerator;
                            generator.RequestFileText += RequestFileText;
                            e.Result = generator.Generate(1, maxLength).First();
                            generator.RequestGenerator -= RequestGenerator;
                            generator.RequestFileText -= RequestFileText;
                        }
                        else
                        {
                            throw new NCalc.EvaluationException("Generate requires at least one parameter.");
                        }
                        break;
                    default:
                        EvaluateFunction(name, e);
                        break;
                }
            }
        }

        /// <summary>
        /// Handles custom parameters for the NCalc engine
        /// </summary>
        protected void OnEvaluateParameter(string name, ParameterArgs e)
        {
            if (Variables != null && Variables.ContainsKey(name))
            {
                e.Result = Variables[name];
            }
            else if (name.Equals("_maxLength", StringComparison.InvariantCultureIgnoreCase))
            {
                e.Result = MaxLength;
            }
            else
            { 
                EvaluateParameter(name, e);
            }
        }

        /// <summary>
        /// Raises the Request Generator event
        /// </summary>
        protected void OnRequestGenerator(RequestGeneratorEventArgs e)
        {
            RequestGenerator?.Invoke(this, e);
        }

        protected string OnRequestFileText(string fileName)
        {
            var e = new RequestFileTextEventArgs(fileName);
            RequestFileText?.Invoke(this, e);
            return e.FileText;
        }
        #endregion
    }
}
