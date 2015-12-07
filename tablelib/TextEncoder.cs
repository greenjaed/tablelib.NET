using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableLib
{
    public class TableString
	{
		internal List<byte> bytes;
        /// <summary>
        /// Gets the encoded text.
        /// </summary>
        /// <value>The encoded text as a byte array.</value>
		public byte[] Text {
			get {
				return bytes.ToArray ();
			}
		}
        /// <summary>
        /// The end token.
        /// </summary>
        public string EndToken { get; set; }

        public TableString()
        {
            bytes = new List<byte>();
        }
	}

	public class TextEncoder
	{
        /// <summary>
        /// Gets the string table.
        /// </summary>
        /// <value>The string table.</value>
		public List<TableString> StringTable { get; private set; }
        /// <summary>
        /// Gets a value indicating whether to add end tokens for this <see cref="TableLib.TextEncoder"/>.
        /// </summary>
        /// <value><c>true</c> if end token are to be added; otherwise, <c>false</c>.</value>
		public bool AddEndToken { get; set; }
        /// <summary>
        /// Gets the errors encountered when loading the table file.
        /// </summary>
        /// <value>The list of errors.</value>
		public List<TableError> Errors {
			get {
				return Table.TableErrors;
			}
		}
		
		private TableReader Table;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableLib.TextEncoder"/> class.
        /// </summary>
		public TextEncoder ()
		{
			StringTable = new List<TableString>();
			AddEndToken = true;
			Table = new TableReader(TableReaderType.ReadTypeInsert);
		}

        /// <summary>
        /// Encodes rawStrings as an IEnumerable of byte arrays
        /// </summary>
        /// <returns>The encoded strings.</returns>
        /// <param name="tableName">The table file name.</param>
        /// <param name="addEndTokens">If set to <c>true</c>, add end tokens.</param>
        /// <param name="rawStrings">The collection of strings to encode.</param>
        public IEnumerable<byte[]> EncodeStream (string tableName, bool addEndTokens, IEnumerable<string> rawStrings)
        {
            OpenTable(tableName);
            AddEndToken = addEndTokens;
            return rawStrings.Select(s => EncodeStream(s)).SelectMany(s => s);
        }

        /// <summary>
        /// Encodes the stream.
        /// </summary>
        /// <returns>The encoded strings.</returns>
        /// <param name="scriptBuf">The string to encode.</param>
        public IEnumerable<byte[]> EncodeStream(string scriptBuf)
        {
            var start = StringTable.Count;
            int badCharOffset = 0;
            int bytesEncoded = EncodeStream(scriptBuf, ref badCharOffset);
            if (bytesEncoded < 0)
            {
                throw new Exception("Encountered unmapped string beginning with '" + scriptBuf[badCharOffset] + "'");
            }
            if (StringTable.Count == 0)
            {
                return null;
            }

            if (start == 0)
            {
                return StringTable.Select(s => s.Text);
            }

            return Enumerable.Range(start, StringTable.Count - start).Select(n => StringTable[n].Text);
        }

        /// <summary>
        /// Encodes a string.
        /// </summary>
        /// <returns>The size of the encoded stream.</returns>
        /// <param name="scriptBuf">The string to encode.</param>
        /// <param name="BadCharOffset">The Bad char offset.</param>
		public int EncodeStream (string scriptBuf, ref int BadCharOffset)
		{
			TableString tablestring = new TableString();
            string hexstr = string.Empty;
            string subtextstr = string.Empty;
			int i = 0;
			int EncodedSize = 0;
			int BufOffset  = 0;

			BadCharOffset = 0;

            if (string.IsNullOrEmpty(scriptBuf))
            {
                return 0;
            }

			if (StringTable.Count > 0) {
				TableString restoreString = StringTable[StringTable.Count - 1];
                if (string.IsNullOrEmpty(restoreString.EndToken)) {
                    StringTable.RemoveAt(StringTable.Count - 1);
					tablestring.bytes = restoreString.bytes;
				}
			}

			while (BufOffset < scriptBuf.Length) {
                int longestText = Table.LongestText[scriptBuf[BufOffset]];
                for (i = longestText; i > 0; --i)
                {
                    if (BufOffset + i >= scriptBuf.Length)
                    {
                        subtextstr = scriptBuf.Substring(BufOffset);
                    }
                    else
                    {
                        subtextstr = scriptBuf.Substring(BufOffset, i);
                    }
                    if (Table.LookupValue.ContainsKey(subtextstr))
                    {
                        hexstr = Table.LookupValue[subtextstr];
                        EncodedSize += hexstr.Length;

                        if (Table.EndTokens.Contains(subtextstr))
                        {
                            if (AddEndToken)
                            {
                                AddToTable(hexstr, tablestring);
                            }
                            tablestring.EndToken = subtextstr;
                            StringTable.Add(tablestring);
                            tablestring = new TableString();
                        }
                        else
                        {
                            AddToTable(hexstr, tablestring);
                        }

                        BufOffset += i;
                        break;
                    }
                }

				if (i == 0) {
					BadCharOffset = BufOffset;
					return -1;
				}
			}

            if (tablestring.bytes.Count > 0)
            {
                StringTable.Add(tablestring);
            }

			return EncodedSize;
		}

        /// <summary>
        /// Opens the table.
        /// </summary>
        /// <returns><c>true</c>, if table was opened successfully, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        public bool OpenTable (string tableFileName)
        {
            return OpenTable(tableFileName, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the table.
        /// </summary>
        /// <returns><c>true</c>, if table was opened successfully, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        /// <param name="encoding">The specified character encoding.</param>
        public bool OpenTable (string tableFileName, Encoding encoding)
        {
            return Table.OpenTable(tableFileName, encoding);
        }

        /// <summary>
        /// Opens the table.
        /// </summary>
        /// <returns><c>true</c>, if table was opened, <c>false</c> otherwise.</returns>
        /// <param name="TableFileName">Table file name.</param>
        /// <param name="encoding">The name of the character encoding</param>
		public bool OpenTable (string tableFileName, string encoding)
		{
			return Table.OpenTable(tableFileName, encoding);
		}

		private void AddToTable (string HexString, TableString tableString)
		{
			for (int k = 0; k < HexString.Length; k +=2) {
				tableString.bytes.Add(Convert.ToByte(HexString.Substring(k, 2), 16));
			}
		}
	}
}

