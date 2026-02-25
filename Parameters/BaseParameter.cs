using System.Text.Json.Serialization;

namespace TheRandomizer.Parameters;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(BooleanParameter), "Boolean")]
[JsonDerivedType(typeof(DecimalParameter), "Decimal")]
[JsonDerivedType(typeof(IntegerParameter), "Integer")]
[JsonDerivedType(typeof(SelectParameter), "Select")]
[JsonDerivedType(typeof(TextParameter), "Text")]
public class BaseParameter
{
    public String Name { get; set; } = String.Empty;
    public Boolean Required { get; set; }
    public String? Default { get; set; }
    public Object? Value { get; set; }

    #region Validation
    public List<String> ErrorList { get; } = [];

    public virtual Boolean HasValue => Value != null;

    public Boolean Valid
    {
        get
        {
            ErrorList.Clear();
            if (Required && !HasValue)
            {
                ErrorList.Add($"Parameter {Name} is required.");
                return false;
            }
            if (!HasValue) return true;
            Validate();
            return ErrorList.Count == 0;
        }
    }

    protected virtual void Validate() { }
    #endregion
}

