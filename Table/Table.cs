using System.Text.Json.Serialization;

namespace TheRandomizer.Table;

internal class Table
{
    #region Members
    private String _content = String.Empty;
    #endregion

    #region Properties
    public String SkipIf { get; set; } = String.Empty;
    public String Roll { get; set; } = String.Empty;
    public String Content 
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                if (!String.IsNullOrWhiteSpace(_content)) ParsePipeTable();
            }
        }
    }
    public List<String> Columns { get; set; } = [];
    public List<TableRow> Rows { get; set; } = [];
    #endregion

    #region Public Methods
    public void ValidateRanges()
    {
        if (Rows.Count == 0) return;

        var ordered = Rows.OrderBy(r => r.Range.Min).ToList();

        for (Int32 i = 0; i < ordered.Count - 1; i++)
        {
            var current = ordered[i].Range;
            var next = ordered[i + 1].Range;

            if (current.Overlaps(next))
                throw new TableGeneratorException($"Overlapping roll ranges: {current} and {next}");
        }
    }
    #endregion

    #region Private Methods
    public void ParsePipeTable()
    {
        var lines = Content.Split('\n');
        var tableLines = (from l in lines
                          where !String.IsNullOrWhiteSpace(l)
                          select l.Trim()).ToList();

        if (tableLines.Count < 2)
            throw new TableGeneratorException("Table must have at least 2 rows, a header row and one content row.");

        // First row is the header
        var header = ParsePipeRow(tableLines.First());

        if (header.Length < 2)
            throw new TableGeneratorException("Table must have at least two cells.");

        if (!header[0].Equals("roll", StringComparison.OrdinalIgnoreCase))
            throw new TableGeneratorException("The first column of a table must be 'roll'.");

        Columns = [.. header[1..]];

        for (Int32 i = 1; i < tableLines.Count; i++)
        {
            if (!IsSeparatorRow(tableLines[i]))
            {
                var cells = ParsePipeRow(tableLines[i]);
                if (header.Length != cells.Length)
                    throw new TableGeneratorException($"Row {i + 1} has {cells.Length} cells but expected {header.Length}");
                Rows.Add(new([.. cells]));
            }
        }

        Rows = [.. Rows.OrderBy(r => r.Range.Min)];
        ValidateRanges();
    }

    private static Boolean IsSeparatorRow(String line) => line.All(c => c is '-' or '|' or ':');

    private static String[] ParsePipeRow(String line)
    {
        if (String.IsNullOrWhiteSpace(line))
            throw new TableGeneratorException("Table row cannot be empty.");

        line = line.Trim();

        if (!line.StartsWith('|') || !line.EndsWith('|'))
            throw new TableGeneratorException($"Pipe table rows must start and end with a pipe: '{line}'");

        return line.Split('|', StringSplitOptions.TrimEntries)[1..^1];
    }
    #endregion

    #region Converters

    #endregion
}