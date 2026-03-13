using LB.Utility.Extensions;
using System.CommandLine;
using System.Text;
using TheRandomizer.Assignment;

namespace TheRandomizer.CLI;

internal class Program
{    
    static List<String> Results { get; set; } = [];

    static readonly Argument<FileInfo> FileArgument = new("definition file")
    {
        Description = "The path to the generator definition file."            
    };

    #region Library Subcommand
    static readonly Option<Boolean> IsLibraryOption = new("--library", "--lib")
    {
        Description = "Tells the parser that this is an assignment library file."
    };

    static Command InfoCommand
    {
        get
        {
            Command command = new("info", "Display information about the provided definition file.")
            {
                FileArgument,
                IsLibraryOption
            };
            command.SetAction(InfoAction);
            return command;
        }
    }

    static void InfoAction(ParseResult result)
    {
        var fileInfo = result.GetValue(FileArgument) ?? throw new FileNotFoundException();
        var isLibrary = result.GetValue(IsLibraryOption);
        var text = File.ReadAllText(fileInfo.FullName);

        WriteLine(fileInfo.Name, ConsoleColor.White);
        WriteHR();
        WriteInfoLine("Last Modified", fileInfo.LastWriteTime);
        WriteInfoLine("Size", fileInfo.Length.ToDataSize());
        WriteInfoLine("Lines", text.Split(Environment.NewLine).Length);
        if (isLibrary)
        {
            var library = Utility.Serialization.Deserialize<LineItemDictionary>(text, Enumerators.FileFormatTypes.Json)
                            ?? throw new Exception("Error deserializing the file.");

            WriteLineItemDictionary(library);
        }
        else
        {
            var generator = BaseGenerator.Deserialize(text, Enumerators.FileFormatTypes.Json)
                            ?? throw new Exception("Error deserializing the file.");
                
            WriteInfoLine("Type", generator.GetType().Name);
            WriteInfoLine("Name", generator.Name);
            WriteInfoLine("Version", generator.Version);
            WriteInfoLine("Author", generator.Author);
            WriteInfoLine("Description", generator.Description);
            if (generator is AssignmentGenerator aGen)
            {
                WriteLineItemDictionary(aGen.LineItems);
            }
        }
    }

    static void WriteLineItemDictionary(LineItemDictionary dictionary)
    {
        var leftSpace = Math.Max(dictionary.Keys.Max(k => k.Length) + 1, 75);
        WriteInfoLine("Line Items", dictionary.Count.ToString("#,##0"));
        WriteLine();
        WriteLine($"{"Name".PadRight(leftSpace)} Count");
        WriteLine($"{new String('\u2500', leftSpace+6)}");
        foreach(var lineItemList in dictionary)
        {
            WriteLine($"{lineItemList.Key.PadRight(leftSpace)} {lineItemList.Value.Count,5}");
        }
        WriteHR();
    }
    #endregion

    #region Generate Subcommand
    static readonly Option<List<String>> ParameterOption = new("--parameter", "-p")
    {
        Description = "Provides a single parameter name and value",
        Arity = ArgumentArity.ZeroOrMore,
        AllowMultipleArgumentsPerToken = true
    };

    static readonly Option<Int32> RepeatOption = new("--repeat", "-r")
    {
        Description = "Number of times to run the generator.",
    };

    static readonly Option<String?> SeedOption = new("--seed")
    {
        Description = "Seed to feed to the random number generator."
    };

    static Command GenerateCommand
    {
        get
        {
            Command command = new("generate", "Generate content using the provided definition file.")
            {
                FileArgument,
                ParameterOption,
                RepeatOption,
                SeedOption
            };
            command.SetAction(Generate);
            return command;
        }
    }

    static void Generate(ParseResult result)
    {
        var fileInfo = result.GetValue(FileArgument) ?? throw new FileNotFoundException();
        var repeat = Math.Max(result.GetValue(RepeatOption), 1);
        var parameters = result.GetValue(ParameterOption);
        var seed = result.GetValue(SeedOption);
        var generator = BaseGenerator.Deserialize(fileInfo.FullName) 
                        ?? throw new Exception("Error deserializing the file.");

        if (seed != null) generator.Seed = seed;
        // Set the parameters
        if (parameters?.Count > 0)
        {
            foreach (var parameter in parameters)
            {
                var parts = parameter.Split('=');
                if (parts.Length != 2)
                    throw new Exception("Parameters must be in the format Name=Value");
                generator.Parameters[parts[0]]?.Value = parts[1];
            }
        }
        // Generate the content
        for (var i = 0; i < repeat; i++)
        {
            var content = generator?.Generate().Text;
            if (content != null)
                Results.Add(content);
            Console.WriteLine(content);
        }
    }
    #endregion

    #region Output
    static void WriteLine() => WriteLine(null);
    static void WriteLine(String? message) => Console.WriteLine(message);
    static void Write(String? message, ConsoleColor color)
    {
        var currentColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(message);
        Console.ForegroundColor = currentColor;
    }
    static void WriteLine(String? message, ConsoleColor color)
    {
        Write(message, color);
        WriteLine();
    }

    static void WriteInfoLine(String header, Object info)
    {
        WriteMuted($"{header}:".PadRight(15));
        Console.WriteLine(info);
    }

    static void WriteMuted(String message)
    {
        Write(message, ConsoleColor.DarkGray);
    }

    static void WriteHR(Int32 length = 80) => WriteLine(new String('\u2500', length));
    #endregion

    static Int32 Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var rootCommand = new RootCommand("CLI for The Randomizer.")
        {
            InfoCommand,
            GenerateCommand
        };

        var result = rootCommand.Parse(args);
        return result.Invoke();
    }
}
