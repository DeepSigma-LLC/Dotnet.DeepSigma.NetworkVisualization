using DeepSigma.NetworkVisualization.Builders;

namespace DeepSigma.NetworkVisualization.Importers;

/// <summary>
/// Reads two CSV strings — one for nodes, one for edges — into a Network.
/// Nodes CSV requires an 'id' column; optional columns: label, color, group. Any other columns become entries in Node.Data.
/// Edges CSV requires 'source' and 'target' columns; optional: id, label, weight.
/// </summary>
public static class CsvImporter
{
    public const string FormatId = "csv";

    public static Network Import(string nodesCsv, string edgesCsv, bool directed = true)
    {
        var nb = NetworkBuilder.Create().Directed(directed);

        var nodeRows = ParseCsv(nodesCsv);
        if (nodeRows.Headers.Count == 0)
            throw new FormatException("Nodes CSV must have a header row.");
        var nodeIdCol = IndexOf(nodeRows.Headers, "id") ?? throw new FormatException("Nodes CSV must have an 'id' column.");
        var nodeLabelCol = IndexOf(nodeRows.Headers, "label");
        var nodeColorCol = IndexOf(nodeRows.Headers, "color") ?? IndexOf(nodeRows.Headers, "fill");
        var nodeGroupCol = IndexOf(nodeRows.Headers, "group");

        var dataColumns = nodeRows.Headers
            .Select((h, i) => (h, i))
            .Where(t => t.i != nodeIdCol && t.i != nodeLabelCol && t.i != nodeColorCol && t.i != nodeGroupCol)
            .ToArray();

        foreach (var row in nodeRows.Rows)
        {
            var id = row[nodeIdCol];
            nb.AddNode(id, n =>
            {
                if (nodeLabelCol is { } lc && lc < row.Count && !string.IsNullOrEmpty(row[lc]))
                    n.Label(row[lc]);
                if (nodeColorCol is { } cc && cc < row.Count && !string.IsNullOrEmpty(row[cc]))
                    n.Fill(row[cc]);
                if (nodeGroupCol is { } gc && gc < row.Count && !string.IsNullOrEmpty(row[gc]))
                    n.InGroup(row[gc]);
                foreach (var (h, i) in dataColumns)
                    if (i < row.Count && !string.IsNullOrEmpty(row[i]))
                        n.Data(h, row[i]);
            });
        }

        var edgeRows = ParseCsv(edgesCsv);
        if (edgeRows.Headers.Count == 0)
            throw new FormatException("Edges CSV must have a header row.");
        var sourceCol = IndexOf(edgeRows.Headers, "source") ?? throw new FormatException("Edges CSV must have a 'source' column.");
        var targetCol = IndexOf(edgeRows.Headers, "target") ?? throw new FormatException("Edges CSV must have a 'target' column.");
        var idCol = IndexOf(edgeRows.Headers, "id");
        var labelCol = IndexOf(edgeRows.Headers, "label");
        var weightCol = IndexOf(edgeRows.Headers, "weight");

        int auto = 0;
        foreach (var row in edgeRows.Rows)
        {
            var source = row[sourceCol];
            var target = row[targetCol];
            var id = idCol is { } ic && ic < row.Count && !string.IsNullOrEmpty(row[ic]) ? row[ic] : $"e{auto++}";
            nb.AddEdge(id, source, target, e =>
            {
                if (labelCol is { } lc && lc < row.Count && !string.IsNullOrEmpty(row[lc])) e.Label(row[lc]);
                if (weightCol is { } wc && wc < row.Count && double.TryParse(row[wc], out var w)) e.Weight(w);
            });
        }
        return nb.Build();
    }

    private static int? IndexOf(IReadOnlyList<string> headers, string name)
    {
        for (int i = 0; i < headers.Count; i++)
            if (string.Equals(headers[i], name, StringComparison.OrdinalIgnoreCase)) return i;
        return null;
    }

    private static (IReadOnlyList<string> Headers, IReadOnlyList<IReadOnlyList<string>> Rows) ParseCsv(string text)
    {
        var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).ToArray();
        if (lines.Length == 0) return (Array.Empty<string>(), Array.Empty<IReadOnlyList<string>>());
        var headers = ParseRow(lines[0]);
        var rows = new List<IReadOnlyList<string>>(lines.Length - 1);
        for (int i = 1; i < lines.Length; i++) rows.Add(ParseRow(lines[i]));
        return (headers, rows);
    }

    private static List<string> ParseRow(string line)
    {
        var result = new List<string>();
        var cur = new System.Text.StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { cur.Append('"'); i++; }
                    else inQuotes = false;
                }
                else cur.Append(c);
            }
            else
            {
                if (c == ',') { result.Add(cur.ToString()); cur.Clear(); }
                else if (c == '"' && cur.Length == 0) inQuotes = true;
                else cur.Append(c);
            }
        }
        result.Add(cur.ToString());
        return result;
    }
}
