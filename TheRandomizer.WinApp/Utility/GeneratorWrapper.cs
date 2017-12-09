﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Windows;
using System.Windows.Input;
using TheRandomizer.Generators;
using TheRandomizer.Generators.Parameter;
using TheRandomizer.Utility;
using TheRandomizer.Utility.Collections;
using TheRandomizer.WinApp.Commands;
using TheRandomizer.WinApp.Models;
using TheRandomizer.WinApp.ViewModels;
using TheRandomizer.WinApp.Views;

namespace TheRandomizer.WinApp.Utility
{
    public class GeneratorWrapper : ObservableBase
    {
        
        #region Members
        private BaseGenerator _generator;
        #endregion

        #region Constructors
        public GeneratorWrapper() {}

        public GeneratorWrapper(BaseGenerator generator, string filePath)
        {
            FilePath = filePath;
            if (_generator != null) _generator.RequestGenerator -= RequestGenerator;
            _generator = generator;
            if (_generator != null) _generator.RequestGenerator += RequestGenerator;            
        }
        #endregion

        #region Properties
        public int MaxLength { get; set; } = 10;
        public int Repeat { get; set; } = 1;

        public Guid Id
        {
            get
            {
                return _generator != null ? _generator.Id : Guid.Empty;
            }
        }

        public BaseGenerator Generator { get { return _generator; } private set { _generator = value; } }

        public string Name {
            get
            {
                return _generator?.Name;
            }
        }

        public string FilePath { get; set; }

        public string Description
        {
            get
            {
                return _generator?.Description;
            }
        }

        public string Author
        {
            get
            {
                return _generator?.Author;
            }
        }

        public string GeneratorType
        {
            get
            {
                return _generator?.GeneratorType.ToString();
            }
        }

        public OutputFormat OutputFormat
        {
            get
            {
                return _generator != null ? _generator.OutputFormat : OutputFormat.Text;
            }
        }

        public string URL
        {
            get
            {
                return _generator?.Url;
            }
        }

        public ObservableCollection<Generators.Tag> Tags
        {
            get
            {
                return _generator?.Tags;
            }
        }
        
        public string TagList
        {
            get
            {
                return _generator?.TagList;
            }
            set
            {
                _generator.TagList = value;
            }
        }

        public bool SupportsMaxLength
        {
            get
            {
                return _generator != null ? _generator.SupportsMaxLength : false;
            }
        }

        public bool HasParameters
        {
            get
            {
                return _generator != null && _generator.Parameters.Count > 0;
            }
        }

        public ConfigurationList Parameters
        {
            get
            {
                return _generator?.Parameters;
            }
        }

        public string Results
        {
            get
            {
                return GetProperty<string>();
            }
            set
            {
                SetProperty(value);
            }
        }

        public ICommand GenerateContent
        {
            get
            {
                return new DelegateCommand(Generate);
            }
        }

        public ICommand EditTags
        {
            get
            {
                return new DelegateCommand(() =>
                                            {
                                                var tagEditor = new Views.TagEditor();
                                                tagEditor.DataContext = this;
                                                if (tagEditor.ShowDialog() == true)
                                                {
                                                    OnPropertyChanged("TagList");
                                                }
                                            });
            }
        }

        public ICommand Save
        {
            get
            {
                return new DelegateCommand<Window>(w => 
                                                   {
                                                       Tags.RemoveAll(s => string.IsNullOrWhiteSpace(s));
                                                       File.WriteAllText(FilePath, _generator.Serialize());
                                                       w.DialogResult = true;
                                                       w.Close();
                                                   });
            }
        }
        
        public ICommand AddTag
        {
            get
            {
                return new DelegateCommand(() => Tags.Add(string.Empty));
            }
        }

        public ICommand RemoveTag
        {
            get
            {
                return new DelegateCommand<string>(s => Tags.Remove(s));
            }
        } 
        #endregion

        #region Methods
        public void Cancel()
        {
            _generator?.Cancel();
        }

        public void Generate()
        {
            Results = FormatResults(_generator?.Generate(Repeat, MaxLength));
            OnPropertyChanged("Results");
        }
        
        private string FormatResults(IEnumerable<string> results)
        {            
            using (StringWriter sWriter = new StringWriter())
            {
                using (HtmlTextWriter writer = new HtmlTextWriter(sWriter))
                {
                    var odd = true;
                    writer.WriteFullBeginTag("head");
                    writer.WriteFullBeginTag("style");
                    writer.WriteLine("body { font-family: Consolas, Courier New, Monospace; }");
                    writer.WriteLine("div.even { background-color: #F8F8F8; }");
                    writer.WriteLine("div.error { color: red; font-weight: bold; }");
                    if (!string.IsNullOrWhiteSpace(_generator.CSS))
                    {
                        writer.WriteLine(_generator.CSS);
                    }
                    writer.WriteEndTag("style");
                    writer.WriteEndTag("head");
                    writer.WriteFullBeginTag("body");
                    if (results != null && results.ToList().Count > 0)
                    {
                        foreach (var item in results)
                        {
                            writer.WriteBeginTag("div");
                            if (odd)
                                writer.WriteAttribute("class", "odd");
                            else
                                writer.WriteAttribute("class", "even");
                            odd = !odd;
                            writer.Write(HtmlTextWriter.TagRightChar);
                            writer.Write(item);
                            writer.WriteEndTag("div");
                        }
                    }
                    else
                    {
                        writer.WriteBeginTag("div");
                        writer.WriteAttribute("class", "error");
                        writer.Write(HtmlTextWriter.TagRightChar);
                        writer.Write("No Results");
                        writer.WriteEndTag("span");
                    }
                    writer.WriteEndTag("body");
                }
                return sWriter.ToString();
            }
        }

        #endregion

        #region Event Handlers
        public void RequestGenerator(object sender, RequestGeneratorEventArgs e)
        {
            var generatorList = Application.Current.MainWindow?.DataContext as GeneratorInfoCollection;
            if (generatorList != null)
            {
                var generatorInfo = generatorList.First(gi => gi.Name.Equals(e.Name, StringComparison.InvariantCultureIgnoreCase));
                e.Generator = BaseGenerator.Deserialize(File.ReadAllText(generatorInfo.FilePath));
            }   
        }
        #endregion
    }
}
