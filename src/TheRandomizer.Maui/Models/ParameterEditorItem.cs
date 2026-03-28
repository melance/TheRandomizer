using System.ComponentModel;
using System.Runtime.CompilerServices;
using TheRandomizer.Parameters;

namespace TheRandomizer.Maui.Models;

public partial class ParameterEditorItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    #region Members
    private String _valueText = String.Empty;
    private Boolean _valueBoolean;
    private Int32 _valueInteger;
    private Decimal _valueDecimal;
    private Option? _valueOption;
    #endregion

    #region Properties
    public required String Name { get; init; }
    public required Type ParameterType { get; init; }
    public Boolean IsRequired { get; init; }
    public List<Option> Options { get; } = [];


    public Boolean IsText => ParameterType == typeof(TextParameter);
    public Boolean IsBoolean => ParameterType == typeof(BooleanParameter);
    public Boolean IsInteger => ParameterType == typeof(IntegerParameter);
    public Boolean IsDecimal => ParameterType == typeof(DecimalParameter);
    public Boolean IsSelect => ParameterType == typeof(SelectParameter);


    public Object? Value
    {
        get
        {
            if (ParameterType == typeof(IntegerParameter)) return ValueInteger;
            if (ParameterType == typeof(DecimalParameter)) return ValueDecimal;
            if (ParameterType == typeof(BooleanParameter)) return ValueBoolean;
            if (ParameterType == typeof(SelectParameter)) return _valueOption?.Value;
            return ValueText;
        }
    }

    public String ValueText
    {
        get => _valueText;
        set
        {
            if (value != _valueText)
            {
                _valueText = value;
                OnPropertyChanged();
            }
        }
    }

    public Boolean ValueBoolean
    {
        get => _valueBoolean;
        set
        {
            if (value != _valueBoolean)
            {
                _valueBoolean = value;
                OnPropertyChanged();
            }
        }
    }

    public Int32 ValueInteger
    {
        get => _valueInteger;
        set
        {
            if (value != _valueInteger)
            {
                _valueInteger = value;
                OnPropertyChanged();
            }
        }
    }

    public Decimal ValueDecimal
    {
        get => _valueDecimal;
        set
        {
            if (value != _valueDecimal)
            {
                _valueDecimal = value;
                OnPropertyChanged();
            }
        }
    } 

    public Option? ValueOption
    {
        get => _valueOption;
        set
        {
            if (value != _valueOption)
            {
                _valueOption = value;
                OnPropertyChanged();
            }
        }
    }
    #endregion

    public static ParameterEditorItem FromParameter(BaseParameter parameter)
    {
        var item = new ParameterEditorItem() 
        {
            Name = parameter.Name,
            ParameterType = parameter.GetType(),
            IsRequired = parameter.Required
        };
        
        if (parameter is SelectParameter select)
        {
            item.Options.AddRange(select.Options);
            if (item.Options.Count > 0)
                item.ValueOption = item.Options[0];
        }

        return item;
    }

    public BaseParameter ToParameter()
    {
        if (ParameterType == typeof(BooleanParameter))
            return new BooleanParameter() { Name = Name, Value = ValueBoolean };
        if (ParameterType == typeof(IntegerParameter))
            return new IntegerParameter() { Name = Name, Value = ValueInteger };
        if (ParameterType == typeof(DecimalParameter))
            return new DecimalParameter() { Name = Name, Value = ValueDecimal };
        return new TextParameter { Name = Name, Value = ValueText };
    }

    protected void OnPropertyChanged([CallerMemberName] String propertyName = "") 
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

