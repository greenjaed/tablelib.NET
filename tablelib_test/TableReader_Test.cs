using System;
using System.IO;
using Xunit;
using TableLib;

namespace TableLib_Test
{
    public class TableReader_Test
    {
        private TableReader tableReader;

        public TableReader_Test()
        {
        }

        [Fact]
        public void BadFileName()
        {
            tableReader = new TableReader(TableReaderType.ReadTypeDump);
            var result = tableReader.OpenTable(string.Empty);
            Assert.Equal(false, result);
            Assert.NotEmpty(tableReader.TableErrors);
        }

        [Fact]
        public void ValueTableDumpInsertEntriesMatch()
        {
            string tableName = "test.tbl";

            using (StreamWriter writer = new StreamWriter(tableName))
            {
                writer.WriteLine("ff=test");
            }

            TableReader insertReader = new TableReader(TableReaderType.ReadTypeInsert);
            insertReader.OpenTable(tableName);
            TableReader dumpReader = new TableReader(TableReaderType.ReadTypeDump);
            dumpReader.OpenTable(tableName);
            Assert.Equal("test", dumpReader.LookupValue[insertReader.LookupValue["test"]]);
            Assert.Equal("FF", insertReader.LookupValue[dumpReader.LookupValue["FF"]]);
        }

        [Fact]
        public void EntriesHaveCorrectEncoding()
        {
            var reader = new TableReader(TableReaderType.ReadTypeDump);
            reader.OpenTable("../../table.shift-jis", "shift-jis");
            Assert.Equal(true, reader.LookupValue.ContainsValue("あ"));
        }

        [Theory]
        [InlineData("25", "25", "", false, TableReaderType.ReadTypeDump)]
        [InlineData("15=", "15", "", false, TableReaderType.ReadTypeDump)]
        [InlineData("333=3", "333", "", false, TableReaderType.ReadTypeDump)]
        [InlineData("80=x\n80=y", "80", "", false, TableReaderType.ReadTypeDump)]
        [InlineData("50=a\n51=a", "a", "50", false, TableReaderType.ReadTypeInsert)]
        [InlineData("00= ", "00", " ", true, TableReaderType.ReadTypeDump)]
        [InlineData("32==", "32", "=", true, TableReaderType.ReadTypeDump)]
        [InlineData("0f=5", "0F", "5", true, TableReaderType.ReadTypeDump)]
        [InlineData("*fe", "FE", "<LINE>", true, TableReaderType.ReadTypeDump)]
        [InlineData("/ff", "FF", "<END>", true, TableReaderType.ReadTypeDump)]
        [InlineData("*fe=ln", "FE", "ln", true, TableReaderType.ReadTypeDump)]
        [InlineData("/ff=end", "FF", "end", true, TableReaderType.ReadTypeDump)]
        [InlineData("/", "", "", true, TableReaderType.ReadTypeDump)]
        public void EntriesReadCorrectly(string entry, string key, string expectedResult,
            bool expectedOutcome, TableReaderType mode)
        {
            TableReader tableReader = new TableReader(mode);
            bool result = readTableFile(entry, tableReader);

            Assert.Equal(expectedOutcome, result);
            if (expectedOutcome)
            {
                Assert.Equal(expectedResult, tableReader.LookupValue[key]);
            }
            else
            {
                Assert.NotEmpty(tableReader.TableErrors);
            }

        }

        [Theory]
        [InlineData("$5e", "5E", "", -1, false, TableReaderType.ReadTypeDump)]
        [InlineData("$6c=", "6C", "", -1, false, TableReaderType.ReadTypeDump)]
        [InlineData("$4d=link", "4D", "link", -1, false, TableReaderType.ReadTypeDump)]
        [InlineData("$64=t,1\n$64=s,2", "64", "t", 1, false, TableReaderType.ReadTypeDump)]
        [InlineData("$98=a,4\n$99=a,5", "a", 98, 4, false, TableReaderType.ReadTypeInsert)]
        [InlineData("$7f=link,10", "7F", "link", 10, true, TableReaderType.ReadTypeDump)]
        [InlineData("$3a=text,more text,5", "3A", "text,more text", 5, true, TableReaderType.ReadTypeDump)]
        public void LinkEntriesReadCorrectly(string entry, string key, string expectedEntry,
            int expectedNumber, bool expectedOutcome, TableReaderType mode)
        {
            TableReader tableReader = new TableReader(mode);
            bool result = readTableFile(entry, tableReader);

            Assert.Equal(expectedOutcome, result);
            if (expectedOutcome)
            {
                Assert.Equal(expectedEntry, tableReader.LinkedEntries[key].Text);
                Assert.Equal(expectedNumber, tableReader.LinkedEntries[key].Number);
            }
            else
            {
                Assert.NotEmpty(tableReader.TableErrors);
            }
        }

        private bool readTableFile(string entry, TableReader reader)
        {
            string tableName = "test.tbl";
            using (StreamWriter writer = new StreamWriter(tableName))
            {
                writer.WriteLine(entry);
            }
            return reader.OpenTable(tableName);
        }
    }
}

