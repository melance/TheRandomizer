namespace TheRandomizer.Table;

internal class TableRow
{
    public TableRow() { }
    public TableRow(List<String> cells)
    {
        if (cells.Count < 2) throw new TableGeneratorException("All rows require at least 2 columns.");

        Range = (TableRange)cells[0];
        for(var i = 1; i < cells.Count; i++)
        {
            Cells.Add(cells[i]);
        }
    }

    public TableRange Range { get; set; } = new();
    public List<String> Cells { get; set; } = [];
}

