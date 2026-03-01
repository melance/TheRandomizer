using LB.Utility.Extensions;
using LB.Utility.Random;
using System.Text.Json.Serialization;
using TheRandomizer.Assignment;
using TheRandomizer.Enumerators;
using TheRandomizer.Parameters;
using TheRandomizer.Utility;

namespace TheRandomizer;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(AssignmentGenerator), typeDiscriminator: "Assignment")]
public abstract class BaseGenerator
{
    #region Serialization
    public static FileFormatTypes ExtensionToFormatType(String fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            var x when x.In(".json",".jsonh") => FileFormatTypes.Json,
            _ => throw new Exception($"Unrecognized file extension '{extension}'.")
        };
    }

    public static BaseGenerator? Deserialize(String filePath) 
    {
        var format = ExtensionToFormatType(filePath);
        var text = File.ReadAllText(filePath);
        var definition = Serialization.Deserialize<BaseGenerator>(text, format);
        definition?.FilePath = filePath;
        return definition;
    }

    public static BaseGenerator? Deserialize(String definition, FileFormatTypes format)
    {
        return Serialization.Deserialize<BaseGenerator>(definition, format);
    }

    public static String? Serialize<T>(T definition, FileFormatTypes type) where T : BaseGenerator
    {
        return Serialization.Serialize(definition, type);
    }
    #endregion

    public BaseGenerator() => PseudoRNG.Initialize();
    public BaseGenerator(String seed) => PseudoRNG.Initialize(seed);

    #region Properties
    public virtual Version Version { get; set; } = new(1,0);
    public virtual String Name { get; set; } = String.Empty;
    public virtual String Description { get; set; } = String.Empty;
    public virtual String Author { get; set; } = String.Empty;
    public virtual OutputFormats OutputFormat { get; set; } = OutputFormats.Text;
    public virtual ParameterList Parameters { get; set; } = [];
    public abstract Boolean SupportsParameters { get; }
    public virtual String FilePath { get; protected set; } = String.Empty;
    public virtual String? Seed
    {
        get => PseudoRNG.Instance?.Seed;
        set
        {
            if (PseudoRNG.Instance?.Seed != value)
            {
                if (value == null)
                    PseudoRNG.Initialize();
                else
                    PseudoRNG.Initialize(value);
            }
        }
    }
    #endregion

    #region Public Methods
    public abstract GeneratorResult Generate(params BaseParameter[] parameters);

    public abstract List<String> VerifyDefinition();

    public override String ToString() => ToString(FileFormatTypes.Json);

    public String ToString(FileFormatTypes format) => Serialize(this, format) ?? String.Empty;
    #endregion
}

