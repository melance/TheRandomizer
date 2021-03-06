﻿using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TheRandomizer.Generators.Parameter;
using TheRandomizer.Utility.Collections;

namespace TheRandomizer.WebApp.Models
{
    public class GenerateModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public Int32 MaxLength { get; set; }
        public Int32 Repeat { get; set; }
        public bool SupportsMaxLength { get; set; }
        public bool IsFavorite { get; set; }
        public bool IsOwner { get; set; }
        public ObservableList<Configuration> Parameters { get; set; }
    }
}