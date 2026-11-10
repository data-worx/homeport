// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Homeport.Platform.ControlPlane.Tests.Integration.Persistence;

/// <summary>
/// The tables and columns an ER diagram in <c>docs/architecture/</c> names, read
/// from the repository checkout rather than a copy in the build output. A
/// table is a swimlane labelled with its name, optionally followed by a
/// parenthesised note; a column is a cell inside it labelled with its name.
/// </summary>
internal sealed class ErDiagram
{
    private const string DiagramDirectory = "docs/architecture";
    private const string SwimlaneStyle = "swimlane";

    private readonly Dictionary<string, HashSet<string>> columnsByTable;

    private ErDiagram(Dictionary<string, HashSet<string>> columnsByTable)
    {
        this.columnsByTable = columnsByTable;
    }

    /// <summary>Reads <c>docs/architecture/&lt;fileName&gt;</c>, an uncompressed <c>.drawio.svg</c>.</summary>
    public static ErDiagram Read(string fileName)
    {
        var path = Path.Combine(FindRoot(), DiagramDirectory, fileName);
        var content = XDocument.Load(path).Root?.Attribute("content")?.Value
            ?? throw new InvalidOperationException($"{path} carries no draw.io content attribute.");
        var cells = XDocument.Parse(content).Descendants("mxCell").ToList();

        var tables = cells
            .Where(cell => ((string?)cell.Attribute("style") ?? string.Empty).StartsWith(SwimlaneStyle, StringComparison.Ordinal))
            .ToDictionary(cell => (string)cell.Attribute("id")!, cell => TableName((string?)cell.Attribute("value") ?? string.Empty));

        var columnsByTable = tables.Values.Distinct().ToDictionary(name => name, _ => new HashSet<string>(StringComparer.Ordinal));
        foreach (var cell in cells)
        {
            if (cell.Attribute("parent") is { } parent && tables.TryGetValue(parent.Value, out var table))
            {
                columnsByTable[table].Add((string?)cell.Attribute("value") ?? string.Empty);
            }
        }

        return new ErDiagram(columnsByTable);
    }

    /// <summary>
    /// Every table and column of <paramref name="context"/>'s model the diagram
    /// does not name, as <c>Table</c> or <c>Table.Column</c>.
    /// </summary>
    public IReadOnlyList<string> MissingFrom(DbContext context)
    {
        var missing = new List<string>();
        foreach (var table in context.GetService<IDesignTimeModel>().Model.GetRelationalModel().Tables)
        {
            if (!columnsByTable.TryGetValue(table.Name, out var drawn))
            {
                missing.Add(table.Name);
                continue;
            }

            missing.AddRange(table.Columns.Where(column => !drawn.Contains(column.Name)).Select(column => $"{table.Name}.{column.Name}"));
        }

        return missing;
    }

    private static string TableName(string label)
    {
        var note = label.IndexOf(" (", StringComparison.Ordinal);
        return note < 0 ? label : label[..note];
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var git = Path.Combine(directory.FullName, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No .git found above {AppContext.BaseDirectory}; the test needs the repository checkout.");
    }
}
