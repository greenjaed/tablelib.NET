using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TableLib
{
    /// <summary>
    /// Represents an entry in the TableErrors table
    /// </summary>
	public class TableError
	{
        /// <summary>
        /// The line number the occurred on.
        /// </summary>
		public int LineNumber;
        /// <summary>
        /// A description of the error.
        /// </summary>
		public string Description;
	}

    /// <summary>
    /// Linked entry.
    /// </summary>
	public class LinkedEntry
	{
		public string Text;
		public int Number;
	}

    internal class HexValuePair
    {
        public string HexNumber;
        public string Value;
    }

    /// <summary>
    /// Table reader type.
    /// ReadTypeInsert indicates that values are being inserted.
    /// ReadTypeDump indicates that values are being dumped.
    /// </summary>
	public enum TableReaderType { ReadTypeInsert, ReadTypeDump }

    /// <summary>
    /// Table encoding.
    /// </summary>
    public enum TableEncoding
    {
        Ascii,
        Gb18030,
        ShiftJis,
        Utf7,
        Utf8,
        Utf16,
        Utf32,
        Windows1252
    }

    /// <summary>
    /// Reads and parses a thingy table.
    /// </summary>
	public class TableReader
	{
        /// <summary>
        /// Stores all the errors that occurred while processing the table file.
        /// </summary>
		public List<TableError> TableErrors { get; private set; }
        /// <summary>
        /// The longest hex string processed.
        /// </summary>
		public int LongestHex { get; private set; }
        /// <summary>
        /// An array of the longest text strings processed per hex value.
        /// </summary>
		public int [] LongestText { get; private set; }
        /// <summary>
        /// Stores the linked entries.
        /// </summary>
		public SortedList <string, LinkedEntry> LinkedEntries { get; private set; }
        /// <summary>
        /// Stores the list of all the end tokens.
        /// </summary>
        public List<string> EndTokens { get; private set; }
        /// <summary>
        /// Stores the table values. Retrieve hex values in insert mode and text values in dump mode.
        /// </summary>
        public SortedList <string, string> LookupValue { get; private set; }

		private static string DefEndLine = "<LINE>";
		private static string DefEndString = "<END>";
		private static string HexAlphaNum = "ABCDEFabcdef0123456789";
		private int LineNumber;
		private TableReaderType ReaderType;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableLib.TableReader"/> class.
        /// </summary>
        /// <param name="type">Indicates whether values are being inserted or dumped.</param>
		public TableReader (TableReaderType type)
		{
			LongestHex = 1;
			LongestText = new int[1024];
			LongestText['<'] = 6;
			ReaderType = type;
			TableErrors = new List<TableError>();
			LinkedEntries = new SortedList <string, LinkedEntry>();
			EndTokens = new List<string>();
            LookupValue = new SortedList<string, string>();

            if (ReaderType == TableReaderType.ReadTypeInsert)
            {
                InitHexTable();
            }
		}

        /// <summary>
        /// Returns the string value for the given encoding.
        /// </summary>
        /// <returns>The encoding string.</returns>
        /// <param name="encoding">The encoding.</param>
        public static string EncodingString(TableEncoding encoding)
        {
            switch (encoding)
            {
                case TableEncoding.Ascii:
                    return "ascii";
                case TableEncoding.Gb18030:
                    return "GB18030";
                case TableEncoding.ShiftJis:
                    return "shift-jis";
                case TableEncoding.Utf7:
                    return "utf-7";
                case TableEncoding.Utf8:
                    return "utf-8";
                case TableEncoding.Utf16:
                    return "utf-16";
                case TableEncoding.Utf32:
                    return "utf-32";
                case TableEncoding.Windows1252:
                    return "windows-1252";
                default:
                    return "utf-8";
            }
        }

        /// <summary>
        /// Opens the table.
        /// </summary>
        /// <returns><c>true</c>, if table was successfully processed, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">Table file name.</param>
        /// <param name="encoding">The character encoding of the table.  Defaults to <c>utf-8</c></param>
		public bool OpenTable (string tableFileName, string encoding = "utf-8")
		{
			LinkedList<string> entryList = new LinkedList<string>();

			try {
                var tableFile = File.ReadAllText(tableFileName, Encoding.GetEncoding(encoding))
                    .Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in tableFile)
                {
					entryList.AddLast(line);

					++LineNumber;
					if (line.Length == 0)
						continue;

					switch (line[0])
					{
					case '0': case '1': case '2': case '3': case '4': case '5':
					case '6': case '7': case '8': case '9': case 'a': case 'b':
					case 'c': case 'd': case 'e': case 'f': case 'A': case 'B':
					case 'C': case 'D': case 'E': case 'F':
						parseEntry(line);
						break;
					case '$':
						parseLink(line);
						break;
					case '/':
						parseEndString(line);
						break;
					case '*':
						parseEndLine(line);
						break;
					case '(': case '{': case '[':
						break;
					default:
						RecordError("First character of the line is not a" +
						            " recognized table character");
						break;
					}
				}

			} catch (Exception ex) {
                LineNumber = -1;
                RecordError(ex.Message);
				return false;
			}

            return TableErrors.Count == 0;
		}

		private bool addToMaps (string hexString, string textString)
		{
			string modString = textString;

			if (ReaderType == TableReaderType.ReadTypeDump)
            {
				modString = changeControlCodes(modString, true);
                if (LookupValue.ContainsKey(hexString))
                {
                    RecordError("Unable to add duplicate Hex tokens, causes dumping conflicts.");
                    return false;
                }
                else
                {
                    LookupValue.Add(hexString, modString);
                }
			}
            else if (ReaderType == TableReaderType.ReadTypeInsert)
            {
				modString = changeControlCodes(modString, false);
                if (LookupValue.ContainsKey(textString))
                {
                    RecordError("Unable to add duplicate Text tokens, causes dumping conflicts.");
                    return false;
                }
                else
                {
                    LookupValue.Add(modString, hexString);
                }
			}

			updateLongest(hexString, modString);
			return true;
		}

		private void InitHexTable ()
		{
			string textVal;
			string hexVal;

			for (int i = 0; i < 0x100; ++i)
            {
				hexVal = i.ToString ("X2");
				textVal = "<$" + hexVal + ">";
				LookupValue.Add (textVal, hexVal);
			}
		}

		private bool parseEndLine (string line)
		{
			line = line.Substring (1);
			string hexstr, textstr;

			HexValuePair tokens = getTokens (line, false);
            if (tokens == null)
            {
                return false;
            }

			hexstr = tokens.HexNumber;
            textstr = string.IsNullOrEmpty(tokens.Value) ? DefEndLine : tokens.Value;

			return addToMaps(hexstr, textstr);

		}

		private bool parseEndString (string line)
		{
			line = line.Substring(1);
			string hexstr, textstr;
			HexValuePair tokens;

            // /<end> type entry
			if (line.IndexOfAny (HexAlphaNum.ToCharArray ()) != 0)
            {
				EndTokens.Add (line);
				return addToMaps (string.Empty, line);
			}

			tokens = getTokens (line, false);

            if (tokens == null)
            {
                return false;
            }

			hexstr = tokens.HexNumber;
            textstr = string.IsNullOrEmpty(tokens.Value) ? DefEndString : tokens.Value;
			line = changeControlCodes(textstr, false);
			EndTokens.Add (line);
			return addToMaps(hexstr, textstr);
		}

		private bool parseEntry (string line)
		{
			HexValuePair tokens = getTokens(line, true);

            if (tokens == null)
            {
                return false;
            }

			return addToMaps(tokens.HexNumber, tokens.Value);
		}

		private bool parseLink (string line)
		{
			LinkedEntry l =  new LinkedEntry();
			int pos;
			line = line.Substring(1);
			string hexstr, textstr;
			HexValuePair tokens =  getTokens(line, true);

            if (tokens == null)
            {
                return false;
            }

			hexstr = tokens.HexNumber;
            pos = tokens.Value.LastIndexOf(',');

			if (pos == -1) {
				RecordError ("No comma, linked entry format is $XX=<text>,num");
				return false;
			}

			textstr = tokens.Value.Substring(0, pos);
			tokens.Value = tokens.Value.Substring (pos + 1);
            pos = findFirstNotOf (tokens.Value, "0123456789");

			if (pos >= 0) {
				RecordError ("Nonnumeric characters in num field, linked entry format is $XX=<text>,num");
				return false;
			}

			l.Text = textstr;
			l.Number = int.Parse (tokens.Value);

			if (ReaderType == TableReaderType.ReadTypeDump) {
                l.Text = changeControlCodes(l.Text, true);
                if (LinkedEntries.ContainsKey(hexstr))
                {
                    RecordError("Linked entry with this hex token already exists.");
                    return false;
                }
                else
                {
                    LinkedEntries.Add(hexstr, l);
                }
			} else if (ReaderType == TableReaderType.ReadTypeInsert) {
                string modString = textstr;
				modString = changeControlCodes(modString, false);
				if (LookupValue.ContainsKey(modString)) {
					RecordError("Unable to add duplicate text token, causes dumper conflicts");
					return false;
				} else {
					LookupValue.Add(modString, hexstr);
					updateLongest (hexstr, modString);
				}
			}
			return true;
		}

		private string changeControlCodes (string text, bool replace)
		{
			int pos = text.IndexOf(@"\n");
			while (pos != -1)
			{
				text = text.Remove(pos, 2);
                if (replace)
                {
                    text = text.Insert(pos, "\n");
                }
				pos = text.IndexOf(@"\n", pos);
			}
			return text;
		}

		private HexValuePair getTokens (string line, bool isNormalEntry)
		{
            HexValuePair tokens = new HexValuePair();
			int pos;

			//if "XX="
			if ((pos = line.IndexOf ('=')) == line.Length - 1)
            {
				RecordError ("Entry is incomplete");
				return null;
			}
			//if "XX"
			if (pos == -1) {
                if (isNormalEntry)
                {
                    RecordError("No string token present");
                    return null;
                }
                else
                {
                    pos = line.Length;
                }
			} 
            else
            {
                tokens.Value = line.Substring (pos + 1);
			}
            string hexString = line.Substring (0, pos).ToUpper();

            pos = findFirstNotOf (hexString, HexAlphaNum);
            if (pos >= 0)
            {
                hexString = hexString.Substring(0, pos);
            }

            if ((hexString.Length & 1) != 0)
            {
				RecordError("Incomplete hex token");
				return null;
			}
            tokens.HexNumber = hexString;
			return tokens;
		}

		//code taken from
		//http://stackoverflow.com/questions/4498176/c-sharp-equivalent-of-c-stdstring-find-first-not-of-and-find-last-not-of
		private int findFirstNotOf (string source, string chars)
		{
			for (int i = 0; i < source.Length; ++i) {
                if (chars.IndexOf(source[i]) == -1)
                {
                    return i;
                }
			}
			return -1;
		}
		
		private void updateLongest (string hexstr, string ModString)
		{
            if (LongestHex < hexstr.Length)
            {
                LongestHex = hexstr.Length;
            }
            if (ModString.Length > 0 && LongestText[(byte)ModString[0]] < ModString.Length)
            {
                LongestText[(byte)ModString[0]] = ModString.Length;
            }
		}

		private void RecordError(string errString)
		{
			TableError err = new TableError();
			err.LineNumber = LineNumber;
			err.Description = errString;
			TableErrors.Add(err);
		}
	}
}

