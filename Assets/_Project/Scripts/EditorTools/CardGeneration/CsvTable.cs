using System;
using System.Collections.Generic;
using System.Text;

namespace Game.EditorTools.CardGeneration
{
    /// <summary>
    /// A CSV file as a header plus rows (RFC 4180: commas separate, fields holding a comma, quote or
    /// line break are quoted, a quote inside a quoted field is doubled). Blank lines are skipped. Written
    /// with LF line endings and a trailing newline, quoting only where needed, so a spreadsheet's export
    /// and a hand edit diff one line per row.
    /// </summary>
    public sealed class CsvTable
    {
        private const char Separator = ',';
        private const char Quote = '"';

        private static readonly char[] s_charactersNeedingQuotes = { Separator, Quote, '\n', '\r' };

        public CsvTable(IReadOnlyList<string> header, IReadOnlyList<CsvRow> rows)
        {
            Header = header ?? throw new ArgumentNullException(nameof(header));
            Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        }

        public IReadOnlyList<string> Header { get; }

        public IReadOnlyList<CsvRow> Rows { get; }

        /// <summary>Parses CSV text; the first non-blank record is the header.</summary>
        /// <exception cref="FormatException">A quoted field is never closed.</exception>
        public static CsvTable Parse(string text)
        {
            var records = new List<CsvRow>();
            var cells = new List<string>();
            var field = new StringBuilder();
            int line = 1;
            int recordLine = 1;
            bool isQuoted = false;
            bool isFieldStarted = false;
            text = text ?? string.Empty;

            for (int index = 0; index < text.Length; index++)
            {
                char character = text[index];
                if (isQuoted)
                {
                    if (character == Quote)
                    {
                        if (index + 1 < text.Length && text[index + 1] == Quote)
                        {
                            field.Append(Quote);
                            index++;
                        }
                        else
                        {
                            isQuoted = false;
                        }
                    }
                    else
                    {
                        if (character == '\n') line++;
                        field.Append(character);
                    }

                    continue;
                }

                switch (character)
                {
                    case Quote when field.Length == 0:
                        isQuoted = true;
                        isFieldStarted = true;
                        break;
                    case Separator:
                        cells.Add(field.ToString());
                        field.Clear();
                        isFieldStarted = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        EndRecord(records, cells, field, recordLine, isFieldStarted);
                        isFieldStarted = false;
                        line++;
                        recordLine = line;
                        break;
                    default:
                        field.Append(character);
                        isFieldStarted = true;
                        break;
                }
            }

            if (isQuoted)
            {
                throw new FormatException($"Line {recordLine}: a quoted field is never closed.");
            }

            EndRecord(records, cells, field, recordLine, isFieldStarted);
            if (records.Count == 0)
            {
                return new CsvTable(Array.Empty<string>(), Array.Empty<CsvRow>());
            }

            var rows = new List<CsvRow>(records.Count - 1);
            for (int i = 1; i < records.Count; i++)
            {
                rows.Add(records[i]);
            }

            return new CsvTable(records[0].Cells, rows);
        }

        /// <summary>CSV text for a header and rows of cells.</summary>
        public static string Write(IReadOnlyList<string> header, IEnumerable<IReadOnlyList<string>> rows)
        {
            var text = new StringBuilder();
            AppendRecord(text, header);
            foreach (IReadOnlyList<string> row in rows)
            {
                AppendRecord(text, row);
            }

            return text.ToString();
        }

        /// <summary>The column of <paramref name="name"/> in the header, or -1.</summary>
        public int ColumnOf(string name)
        {
            for (int column = 0; column < Header.Count; column++)
            {
                if (string.Equals(Header[column], name, StringComparison.Ordinal))
                {
                    return column;
                }
            }

            return -1;
        }

        private static void EndRecord(List<CsvRow> records, List<string> cells, StringBuilder field, int recordLine, bool isFieldStarted)
        {
            // A line with nothing on it is not a record.
            if (cells.Count == 0 && !isFieldStarted)
            {
                field.Clear();
                return;
            }

            cells.Add(field.ToString());
            field.Clear();
            records.Add(new CsvRow(recordLine, cells.ToArray()));
            cells.Clear();
        }

        private static void AppendRecord(StringBuilder text, IReadOnlyList<string> cells)
        {
            for (int column = 0; column < cells.Count; column++)
            {
                if (column > 0) text.Append(Separator);
                AppendField(text, cells[column] ?? string.Empty);
            }

            text.Append('\n');
        }

        private static void AppendField(StringBuilder text, string value)
        {
            bool needsQuotes = value.IndexOfAny(s_charactersNeedingQuotes) >= 0
                || (value.Length > 0 && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])));
            if (!needsQuotes)
            {
                text.Append(value);
                return;
            }

            text.Append(Quote).Append(value.Replace("\"", "\"\"")).Append(Quote);
        }
    }
}
