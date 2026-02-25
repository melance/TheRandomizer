using System.Linq.Expressions;

namespace TheRandomizer.Parameters;

public class ParameterList : List<BaseParameter>
{
    public BaseParameter? this[String name]
    {
        get => this.FirstOrDefault(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        set
        {
            var current = this[name];
            if (current == null && value != null)
            {
                Add(value);
            }
            else if (current != null)
            {
                var index = IndexOf(current);
                if (value == null)
                    RemoveAt(index);
                else
                    this[index] = value;
            }
        }
    }

    public void Add(String name, Object? value)
    {
        Add(new BaseParameter() { Name = name, Value = value });
    }

    public List<String> ErrorList => [..this.SelectMany(p => p.ErrorList)];
}

