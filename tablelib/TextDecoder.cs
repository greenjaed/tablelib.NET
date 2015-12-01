using System;
using System.Collections.Generic;

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

                stringLength = value * 2;

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
                int temp = value * 2;
                if (temp < stringLength)
                {
                    throw new ArgumentOutOfRangeException("StringOffset", "StringOffset cannot be smaller than StringLength");
                }
                stringOffset = temp;
            }
            private get
            {
                return stringOffset;
            }
        }
        private int stringOffset;

		private List<char> tempbuf;
		private TableReader Table;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableLib.TextDecoder"/> class.
        /// </summary>
		public TextDecoder ()
		{
            StringOffset = 1;
            StringLength = 1;
			tempbuf = new List<char>();
			Table = new TableReader(TableReaderType.ReadTypeDump);
		}

        /// <summary>
        /// Gets all the decoded fixed length string.
        /// </summary>
        /// <returns>The decoded fixed length strings in an IEnumerable.</returns>
        /// <param name="tableName">The table name.</param>
        /// <param name="hexBlock">The Hex block to decode.</param>
        /// <param name="stringLength">The string length.</param>
        /// <param name="stringOffset">The s6tring offset.</param>
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
        /// <returns>The fixed length string.</returns>
        /// <param name="length">The Length of the string.</param>
        /// <param name="offset">The distance between strings measured from the start of the strings.</param>
        public string DecodeFixedLengthString()
        {
            string decodedString = String.Empty;
            if (DecodeString(ref decodedString, String.Empty, Math.Min(StringLength, tempbuf.Count)) < 0)
            {
                decodedString = String.Empty;
            }
            tempbuf.RemoveRange(0, Math.Min(StringOffset - StringLength, tempbuf.Count));
            return decodedString;
        }

        /// <summary>
        /// Gets the decoded strings.
        /// </summary>
        /// <returns>The decoded strings in an IEnumerable.</returns>
        /// <param name="tableName">The name of the table.</param>
        /// <param name="hexBlock">The Hex block to decode</param>
        /// <param name="endString">Signified the end of a string.</param>
        public IEnumerable<string> GetDecodedStrings(string tableName, byte[] hexBlock, string endString)
        {
            Table.OpenTable(tableName);
            SetHexBlock(hexBlock);
            return GetDecodedStrings(endString);
        }

        /// <summary>
        /// Gets the decoded strings.
        /// </summary>
        /// <returns>The decoded strings in an IEnumerable.</returns>
        /// <param name="endString">The string which indicates the end of a string.</param>
        public IEnumerable<string> GetDecodedStrings(string endString)
        {
            string decodedString = String.Empty;
            do
            {
                decodedString = DecodeString(endString);
                if (!string.IsNullOrEmpty(decodedString))
                {
                    yield return decodedString;
                }
            } while (!string.IsNullOrEmpty(decodedString));
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
            return DecodeString(ref textString, endString, tempbuf.Count);
        }

        private int DecodeString (ref string textString, string endString, int hexSize)
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
                for (i = Math.Min(Table.LongestHex * 2, hexSize - hexoff); i > 0; i -= 2)
                {
                    int sizeLeft = tempbuf.Count - hexoff;
                    char[] hexArray = new char[Math.Min(sizeLeft, i)];
                    tempbuf.CopyTo(hexoff, hexArray, 0, hexArray.Length);
                    hexstr = new string(hexArray);

                    if (Table.LookupValue.ContainsKey(hexstr))
                    {
                        textstr = Table.LookupValue[hexstr];
                        textString += textstr;
                        hexoff += hexstr.Length;

                        if (textstr == endString)
                        {
                            tempbuf.RemoveRange(0, hexoff);
                            return (hexoff >> 1);
                        }
                        break;
                    }
                    else if (Table.LinkedEntries.ContainsKey(hexstr))
                    {
                        LinkedEntry l = Table.LinkedEntries[hexstr];
                        int hexBytes = l.Text.Length + l.Number * 2;
                        if (hexBytes <= sizeLeft)
                        {
                            textString += l.Text;
                            hexoff += hexstr.Length;
                            for (int j = 0; j < l.Number; ++j)
                            {
                                textString += "<$" + tempbuf[hexoff + j * 2] +
                                    tempbuf[hexoff + j * 2 + 1] + ">";
                            }
                            hexoff += l.Number * 2;
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
                        tempbuf.RemoveRange(0, 2);
                        break;
                    }
					textString += "<$" + tempbuf[hexoff] + tempbuf[hexoff + 1] + ">";
					hexoff += 2;
				}
			}

			tempbuf.RemoveRange(0, hexoff);
			return (hexoff >> 1);
		}

        /// <summary>
        /// Sets the encoded bytes to be decoded
        /// </summary>
        /// <param name="hexbuf">The bytes to be decoded</param>
		public void	SetHexBlock (byte[] hexbuf)
		{
			string conbuf;

			tempbuf.Clear();
			tempbuf.Capacity = hexbuf.Length * 2;

			for (int i =0; i < hexbuf.Length; ++i) {
				conbuf = hexbuf[i].ToString("X2");
				tempbuf.Add(conbuf[0]);
				tempbuf.Add(conbuf[1]);
			}
		}

        /// <summary>
        /// Opens the table.
        /// </summary>
        /// <returns><c>true</c>, if table was opened successfully, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        public bool OpenTable(string tableFileName)
        {
            return OpenTable(tableFileName, TableEncoding.Utf8);
        }

        /// <summary>
        /// Opens the table file.
        /// </summary>
        /// <returns><c>true</c>, if the table was successfully opened, <c>false</c> otherwise.</returns>
        /// <param name="tableFileName">The table file name.</param>
        /// <param name="tableEncoding">The selected table encoding.</param>
        public bool OpenTable(string tableFileName, TableEncoding tableEncoding)
        {
            return OpenTable(tableFileName, TableReader.EncodingString(tableEncoding));
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

