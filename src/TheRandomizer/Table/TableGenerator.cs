using DiceRoller;
using DiceRoller.Evaluator;
using LB.Utility.Collections;
using LB.Utility.Extensions;
using TheRandomizer.Parameters;

namespace TheRandomizer.Table;

internal class TableGenerator : BaseGenerator
{
    #region Properties
    public override Boolean SupportsParameters => true;
    public Dictionary<String, Table> Tables { get; set; } = [];
    public String Output { get; set; } = String.Empty;
    public InsensitiveDictionary<Object?> Variables { get; set; } = [];
    private Dice Dice { get; } = new();
    #endregion

    #region Public Methods
    public override GeneratorResult Generate(params BaseParameter[] parameters)
    {
        if (Tables.Count == 0)
            throw new DefinitionException("Table definition must have at least one table.");

        if (!PreProcessParameters())
            throw new ParameterValidationException(Parameters.ErrorList);

        Dice.VariableLookup = VariableLookup;

        var context = new TableResultContext();

        foreach(var table in Tables)
        {
            if (table.Value.SkipIf.IsNullOrWhitespace() || Dice.EvaluateAs<BooleanValue>(table.Value.SkipIf).Value)
                context.Tables[table.Key] = GenerateTable(table.Key, table.Value);
            else
                context.Tables[table.Key] = EmptyTable(table.Key, table.Value);
        }

        return new() { Text = RenderOutput(context) };
    }

    public override List<String> VerifyDefinition()
    {
        throw new NotImplementedException();
    }
    #endregion

    #region Private Methods
    private Boolean PreProcessParameters()
    {
        var valid = true;
        foreach (var parameter in Parameters)
        {
            var value = parameter.Value;
            if (!parameter.HasValue && !parameter.Default.IsNullOrWhitespace())
            {
                value = parameter.Default;
            }
            Variables.Add(parameter.Name, value);
            valid &= parameter.Valid;
        }
        return valid;
    } 

    private TableResult EmptyTable(String name, Table table)
    {
        var result = new InsensitiveDictionary<String?>();
        foreach(var column in table.Columns)
        {
            result.Add(column, null);
            Variables.Add($"{name}.{column}", null);
        }
        return new TableResult(name, 0, result);
    }

    private TableResult GenerateTable(String name, Table table)
    {
        var roll = Dice.EvaluateAs<IntegerValue>(table.Roll).Value;
        var row = table.Rows.FirstOrDefault(r => r.Range.Contains(roll))
                ?? throw new DefinitionException($"Table '{name}' has no row matching roll {roll}");
        var result = new InsensitiveDictionary<String?>();
        for(var i = 0; i < table.Columns.Count; i++)
        {
            var column = table.Columns[i];
            var value = row.Cells[i];
            result.Add(column, value);
            Variables.Add($"{name}.{column}", value);
        }
        return new TableResult(name, roll, result);
    }

    private String RenderOutput(TableResultContext context)
    {
        return OutputParser.Replace(Output, token =>
        {
            var parts = token.Split('.', StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                throw new TableGeneratorException($"Invalid output token '{token}'");

            var tableName = parts[0];
            var cellName = parts[1];

            if (!context.Tables.TryGetValue(tableName, out var table))
                throw new TableGeneratorException($"Unknown table '{tableName} in output.");
            if (cellName.Equals("Roll", StringComparison.OrdinalIgnoreCase))
                return table.Roll.ToString();
            if (!table.Row.TryGetValue(cellName, out var value))
                throw new TableGeneratorException($"Unknown column '{cellName}' in table '{tableName}'.");
            return value ?? String.Empty;
        });
    }

    private Object VariableLookup(String name)
    {
        if (Variables.TryGetValue(name, out var value))
            return value ?? throw new DefinitionException($"Variable '{name}' is null.");
        throw new DefinitionException($"Unknown variable '{name}'.");
    }
    #endregion
}

