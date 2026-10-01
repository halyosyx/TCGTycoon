using System.Collections.Generic;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>One data row of a <see cref="CsvTable"/>, with the line it starts on for error messages.</summary>
    public sealed class CsvRow
    {
        public CsvRow(int lineNumber, IReadOnlyList<string> cells)
        {
            LineNumber = lineNumber;
            Cells = cells;
        }

        /// <summary>1-based line in the file where the row starts (the header is line 1).</summary>
        public int LineNumber { get; }

        public IReadOnlyList<string> Cells { get; }

        /// <summary>The cell at <paramref name="column"/>, or empty when the row is shorter.</summary>
        public string Cell(int column) => column >= 0 && column < Cells.Count ? Cells[column] : string.Empty;
    }
}
