using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TableLib
{
	public class TextDecoder
	{
        /// <summary>
        /// A list of the errors encountered while parsing the table file.
        /// </summary>
        /// <value>The errors.</value>
		public List<TableError> Errors {
			get {
				return Table.TableErrors;
			}
		}

        /// <summary>
        /// sets a value indicating whether this <see cref="TableLib.TextDecoder"/> will stop on an undefined character.
        /// <value><c>true</c> if you wish to stop decoding on an undefined character; otherwise, <c>false</c>. Default is <c>false</c></value>
        /// <remarks>When an undefined byte is encountered when decoding a string and this is set to true, the undefined byte is consumed.
        /// The value returned by <see cref="TableLib.TextDecoder.DecodeString"/> will still return the number of bytes decoded and will not include the consumed byte.</remarks>
        /// </summary>
        public bool StopOnUndefinedCharacter { get; set; }

        /// <summary>
        /// Sets the length of the string to decode.  Used for fixed length strings
        /// </summary>
        /// <value>The length of fixed length string.</value>
        public int StringLength {
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("StringLength", "StringLength must be greater than 0");
                }

                stringLength = value;

                if (StringOffset < stringLength)
                {
                    stringOffset = stringLength;
                }
            }
            private get
            {
                return stringLength;
            }
        }
        private int stringLength;

        /// <summary>
        /// Sets the distance between the strings.  Used for fixed length strings
        /// </summary>
        /// <value>The offset between the beginning of strings.</value>
        public int StringOffset {
            set
            {
                if (value < stringLength)
                {
                    throw new ArgumentOutOfRangeException("StringOffset", "StringOffset cannot be smaller than StringLength");
                }
                stringOffset = value;
            }
            private get
            {
                return stringOffset;
            }
        }
        private int stringOffset;

        /// <summary>
        /// Gets a value determining if the hex block is empty or not.
        /// <value>Returns <c>True</c> is the decoder has finished reading the block or it has not been set and <c>False</c> otherwise.</value>
        /// </summary>
        public bool BlockEmpty
        {
            get
            {
                return tempbuf.Count == 0;
            }
        }

		private List<string> tempbuf;
		private TableReader Table;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableLib.TextDecoder"/> class.
        /// </summary>
		public TextDecoder ()
		{
            StringOffset = 1;
            StringLength = 1;
			tempbuf = new List<string>();
			Table = new TableReader(TableReaderType.ReadTypeDump);
		}

        /// <summary>
        /// Gets all the decoded fixed length string.
        /// </summary>
        /// <param name="tableName">The table name.</param>
        /// <param name="hexBlock">The Hex block to decode.</param>
        /// <param name="stringLength">The string length.</param>
        /// <param name="stringOffset">The s6tring offset.</param>
        /// <returns>The decoded fixed length strings in an IEnumerable.</returns>
        public IEnumerable<string> GetDecodedFixedLengthStrings(string tableName, byte[] hexBlock, int stringLength, int stringOffset)
        {
            OpenTable(tableName);
            SetHexBlock(hexBlock);
            StringLength = stringLength;
            StringOffset = stringOffset;
            return GetDecodedFixedLengthStrings();
        }

        /// <summary>
        /// Gets all the decoded fixed length strings.
        /// </summary>
        /// <returns>The decoded fixed length strings in an IEnumerable.</returns>
        public IEnumerable<string> GetDecodedFixedLengthStrings()
        {
            string decodedString;
            do
            {
                decodedString = DecodeFixedLengthString();
                if (!string.IsNullOrEmpty(decodedString))
                {
                    yield return decodedString;
                }
            } while (!string.IsNullOrEmpty(decodedString));
        }

        /// <summary>
        /// Decodes a fixed length string.
        /// </summary>
        /// <param name="length">The Length of the string.</param>
        /// <param name="offset">The distance between strings measured from the start of the strings.</param>
        /// <returns>The fixed length string.</returns>
        public string DecodeFixedLengthString()
        {
            List<string> decodedString = new List<string>();
            if (DecodeString(decodedString, String.Empty, Math.Min(StringLength, tempbuf.Count)) < 0)
            {
                decodedString.Clear();
            }
            tempbuf.RemoveRange(0, Math.Min(StringOffset - StringLength, tempbuf.Count));
            return string.Concat(decodedString);
        }

        /// <summary>
        /// Gets the decoded strings.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="hexBlock">The Hex block to decode</param>
        /// <param name="endString">Signified the end of a string.</param>
        /// <returns>The decoded strings in an IEnumerable.</returns>
        public IEnumerable<string> GetDecodedStrings(string tableName, byte[] hexBlock, string endString)
        {
            Table.OpenTable(tableName);
            SetHexBlock(hexBlock);
            return GetDecodedStrings(endString);
        }

        /// <summary>
        /// Gets the decoded strings.
        /// </summary>
        /// <param name="endString">The string which indicates the end of a string.</param>
        /// <returns>The decoded strings in an IEnumerable.</returns>
        public IEnumerable<string> GetDecodedStrings(string endString)
        {
            string decodedString;
            int consumedBytes = 0;
            do
            {
                decodedString = String.Empty;
                consumedBytes = DecodeString(ref decodedString, endString);
                if (consumedBytes >= 0)
                {
                    yield return decodedString;
                }
            } while (consumedBytes >= 0 && tempbuf.Count > 0);
        }

        /// <summary>
        /// Gets all decoded strings as lists of chars
        /// </summary>
        /// <param name="endString">The string which indicates the end of a string.</param>
        /// <returns>Returns an IEnumerable containing a list of all decoded chars</returns>
        public IEnumerable<List<string>> GetAllDecodedChars(string endString)
        {
            List<string> decodedCharacters = new List<string>();
            int consumedBytes = 0;
            do
            {
                decodedCharacters.Clear();
                consumedBytes = DecodeString(decodedCharacters, endString, tempbuf.Count);
                if (consumedBytes >= 0)
                {
                    yield return decodedCharacters;
                }
            } while (consumedBytes >= 0 && tempbuf.Count > 0);
        }

        /// <summary>
        /// Decodes a string as a list of chars
        /// </summary>
        /// <returns>The decoded string as a list of chars</returns>
        public List<string> DecodeChars()
        {
            return DecodeChars(string.Empty);
        }

        /// <summary>
        /// Decodes a string as a list of chars
        /// </summary>
        /// <param name="endString">The string which indicates the end of string</param>
        /// <returns>The decoded string as a list of chars</returns>
        public List<string> DecodeChars(string endString)
        {
            List<string> decodedCharacters = new List<string>();
            DecodeChars(decodedCharacters, endString);
            return decodedCharacters;
        }

        public int DecodeChars(List<string> textString, string endString)
        {
            return DecodeString(textString, endString, tempbuf.Count);
        }

        /// <summary>
        /// Decodes the hex block.
        /// </summary>
        /// <returns>The deocoded string.</returns>
        public string DecodeString ()
        {
            return DecodeString(String.Empty);
        }

        /// <summary>
        /// Decodes the hex block until endString is encountered.
        /// </summary>
        /// <returns>The decoded string. Returns an empty string when the hex block has been completely decoded. </returns>
        /// <param name="endString">The string which indicates the end of a string.</param>
        public string DecodeString (string endString)
        {
            string temp = string.Empty;
            if (DecodeString(ref temp, endString) < 0)
            {
                temp = String.Empty;
            }
            return temp;
        }

        /// <summary>
        /// Decodes the hex block until endString is encountered.
        /// textString is set to the decoded string
        /// </summary>
        /// <returns>The number of decoded characters.</returns>
        /// <param name="textString">A reference to the string that will hold the decoded string</param>
        /// <param name="endString">The string which indicates the end of a string.</param>
        public int DecodeString (ref string textString, string endString)
        {
            List<string> decodedCharacters = new List<string>();
            int bytesDecoded = DecodeString(decodedCharacters, endString, tempbuf.Count);
            textString = string.Concat(decodedCharacters);
            return bytesDecoded;
        }

        private int DecodeString (List<string> textString, string endString, int hexSize)
		{
			int hexoff = 0;
            string hexstr = string.Empty;
            string textstr = string.Empty;
			int i = 0;

            if (hexSize == 0)
            {
                return 0;
            }

			while (hexoff < hexSize)
            {
                for (i = Math.Min(Table.LongestHex, hexSize - hexoff); i > 0; --i)
                {
                    int sizeLeft = tempbuf.Count - hexoff;
                    hexstr = string.Concat(tempbuf.GetRange(hexoff, Math.Min(sizeLeft, i)));

                    if (Table.LookupValue.ContainsKey(hexstr))
                    {
                        textstr = Table.LookupValue[hexstr];
                        textString.Add(textstr);
                        hexoff += hexstr.Length / 2;

                        if (textstr == endString)
                        {
                            tempbuf.RemoveRange(0, hexoff);
                            return (hexoff);
                        }
                        break;
                    }
                    else if (Table.LinkedEntries.ContainsKey(hexstr))
                    {
                        LinkedEntry l = Table.LinkedEntries[hexstr];
                        int hexLength = hexstr.Length / 2;
                        int hexBytes = hexLength + l.Number;
                        if (hexBytes <= sizeLeft)
                        {
                            textString.Add(l.Text);
                            hexoff += hexLength;
                            for (int j = 0; j < l.Number; ++j)
                            {
                                textString.Add("<$" + tempbuf[hexoff + j] + ">");
                            }
                            hexoff += l.Number;
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    }
                }

				if (i == 0) {
                    if (StopOnUndefinedCharacter)
                    {
                        tempbuf.RemoveAt(0);
                        break;
                    }
					textString.Add("<$" + tempbuf[hexoff] + ">");
					++hexoff;
				}
			}

			tempbuf.RemoveRange(0, hexoff);
			return (hexoff);
		}

        /// <summary>
        /// Sets the encoded bytes to be decoded
        /// </summary>
        /// <param name="hexbuf">The bytes to be decoded</param>
		public void	SetHexBlock (byte[] hexbuf)
		{
			tempbuf.Clear();
            tempbuf.AddRange(hexbuf.Select(b => b.ToString("X2")));
		}

        /// <summary>
        /// Opens the table.
        /// </summary>
        /// <returns><c>true</c>, if table was opened successfully, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        public bool OpenTable(string tableFileName)
        {
            return OpenTable(tableFileName, Encoding.UTF8);
        }

        /// <summary>
        /// Opens the table file.
        /// </summary>
        /// <returns><c>true</c>, if the table was successfully opened, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        /// <param name="tableEncoding">The selected table encoding.</param>
        public bool OpenTable(string tableFileName, Encoding tableEncoding)
        {
            return OpenTable(tableFileName, tableEncoding);
        }

        /// <summary>
        /// Opens the table file.
        /// </summary>
        /// <returns><c>true</c>, if the table was successfully opened, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        /// <param name="encoding">The name of the character encoding</param>
		public bool OpenTable(string tableFileName, string encoding)
		{
			return Table.OpenTable(tableFileName, encoding);
		}

	}
}

