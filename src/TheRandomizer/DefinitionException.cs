using System;
using System.Collections.Generic;
using System.Text;

namespace TheRandomizer;

public class DefinitionException : Exception
{
    public DefinitionException() { }
    public DefinitionException(string message) : base(message) { }
}

