using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using TheRandomizer.Assignment;
using TheRandomizer.Enumerators;
using TheRandomizer.Helpers;
using TheRandomizer.Parameters;

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
            ".json" => FileFormatTypes.Json,
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

    public BaseGenerator()
    {
        RNG = new RandomGen();
    }

    public BaseGenerator(Int32 seed) : this()
    {
        RNG = new RandomGen(seed);
    }

    #region Properties
    public virtual Int32? Seed 
    { 
        get => RNG.Seed; 
        set => RNG.Seed = value; 
    }
    public virtual Version Version { get; set; } = new(1,0);
    public virtual String Name { get; set; } = String.Empty;
    public virtual String Description { get; set; } = String.Empty;
    public virtual String Author { get; set; } = String.Empty;
    public virtual OutputFormats OutputFormat { get; set; } = OutputFormats.Text;
    public virtual ParameterList Parameters { get; set; } = [];
    public abstract Boolean SupportsParameters { get; }
    public virtual String FilePath { get; protected set; } = String.Empty;
    protected virtual RandomGen RNG { get; }
    #endregion

    #region Public Methods
    public abstract GeneratorResult Generate(params BaseParameter[] parameters);

    public override String ToString() => ToString(FileFormatTypes.Json);

    public String ToString(FileFormatTypes format) => Serialize(this, format) ?? String.Empty;
    #endregion
}

