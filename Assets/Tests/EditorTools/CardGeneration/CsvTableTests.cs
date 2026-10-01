using System;
using System.Collections.Generic;
using Game.EditorTools.CardGeneration;
using NUnit.Framework;

namespace Game.EditorTools.Tests.CardGeneration
{
    public sealed class CsvTableTests
    {
        [Test]
        public void Parse_PlainRows_HeaderAndCellsWithLineNumbers()
        {
            CsvTable table = CsvTable.Parse("id,name\nRC_C_001,Zenkin\nRC_C_002,Silmir\n");

            Assert.That(table.Header, Is.EqualTo(new[] { "id", "name" }));
            Assert.That(table.Rows.Count, Is.EqualTo(2));
            Assert.That(table.Rows[1].Cells, Is.EqualTo(new[] { "RC_C_002", "Silmir" }));
            Assert.That(table.Rows[0].LineNumber, Is.EqualTo(2));
            Assert.That(table.Rows[1].LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void Parse_QuotedFields_KeepCommasQuotesAndLineBreaks()
        {
            CsvTable table = CsvTable.Parse("id,flavour\nRC_C_001,\"Old, \"\"wise\"\"\nand slow\"\nRC_C_002,x\n");

            Assert.That(table.Rows[0].Cell(1), Is.EqualTo("Old, \"wise\"\nand slow"));
            Assert.That(table.Rows[1].LineNumber, Is.EqualTo(4), "A line break inside quotes still counts as a line.");
        }

        [Test]
        public void Parse_CrLfAndBlankLines_AreIgnored()
        {
            CsvTable table = CsvTable.Parse("id,name\r\n\r\nRC_C_001,Zenkin\r\n\r\n");

            Assert.That(table.Rows.Count, Is.EqualTo(1));
            Assert.That(table.Rows[0].Cells, Is.EqualTo(new[] { "RC_C_001", "Zenkin" }));
            Assert.That(table.Rows[0].LineNumber, Is.EqualTo(3));
        }

        [Test]
        public void Parse_EmptyTrailingCell_IsKept()
        {
            CsvTable table = CsvTable.Parse("id,name,flavour\nRC_C_001,Zenkin,\n");

            Assert.That(table.Rows[0].Cells, Is.EqualTo(new[] { "RC_C_001", "Zenkin", string.Empty }));
        }

        [Test]
        public void Parse_UnclosedQuote_Throws()
        {
            Assert.Throws<FormatException>(() => CsvTable.Parse("id,name\nRC_C_001,\"Zenkin\n"));
        }

        [Test]
        public void Write_ThenParse_GivesTheSameCells()
        {
            string[] header = { "id", "name", "flavour" };
            var rows = new List<IReadOnlyList<string>>
            {
                new[] { "RC_C_001", "Zenkin", string.Empty },
                new[] { "RC_C_002", "Old, \"wise\"", "two\nlines" },
                new[] { "RC_C_003", " padded ", "x" },
            };

            CsvTable table = CsvTable.Parse(CsvTable.Write(header, rows));

            Assert.That(table.Header, Is.EqualTo(header));
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.That(table.Rows[i].Cells, Is.EqualTo(rows[i]));
            }
        }

        [Test]
        public void Write_PlainValues_NoQuotesAndLfLineEndings()
        {
            string text = CsvTable.Write(new[] { "id", "name" }, new List<IReadOnlyList<string>> { new[] { "RC_C_001", "Zenkin" } });

            Assert.That(text, Is.EqualTo("id,name\nRC_C_001,Zenkin\n"));
        }
    }
}
