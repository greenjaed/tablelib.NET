﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Mono.Options;
using TableLib;

namespace TextScan
{
    class Program
    {
        static void Main(string[] args)
        {
            string romName = string.Empty;
            string tableName = string.Empty;
            int validStringLength = 5;
            string encodingName = "utf-8";
            string outputName = "output.txt";
            bool showHelp = false;
            Encoding encoding;

            var options = new OptionSet()
            {
                {"l|length=", "the minimum length for a valid string.  Default is 5", l => validStringLength = Convert.ToInt32(l) },
                {"e|encoding=", "the encoding of the table file (e.g. shift-jis).  Default is utf-8", e => encodingName = e },
                {"o|output=", "the name of the file to write the text to.  Default is output.txt", o => outputName = o},
                {"h|help", "show this message", h => showHelp = h != null}
            };

            List<string> unparsedOptions;
            try
            {
                unparsedOptions = options.Parse(args);
            }
            catch (OptionException ex)
            {
                Console.Write("Text Scanner");
                Console.WriteLine(ex.Message);
                Console.WriteLine("Try 'TextScanner --help' for more information");
                return;
            }

            if (showHelp)
            {
                showUsage(options);
                return;
            }

            if (unparsedOptions.Count < 2)
            {
                showUsage(options);
                return;
            }

            if (!checkFile(unparsedOptions[0], "rom"))
            {
                return;
            }

            romName = unparsedOptions[0];

            if (!checkFile(unparsedOptions[1], "table"))
            {
                return;
            }

            tableName = unparsedOptions[1];

            if (validStringLength <= 0)
            {
                Console.WriteLine("Error: Invalid string length");
                return;
            }

            try
            {
                encoding = Encoding.GetEncoding(encodingName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            var decoder = new TextDecoder();
            decoder.OpenTable(tableName, encodingName);
            decoder.SetHexBlock(File.ReadAllBytes(romName));
            decoder.StopOnUndefinedCharacter = true;
            scanFile(decoder, validStringLength, outputName, encoding);
        }

        private static void showUsage(OptionSet options)
        {
            Console.WriteLine("Usage: TextScanner.exe romName tableName [OPTIONS]");
            Console.WriteLine("Scans a rom for strings with a minimal valid length and dumps them.");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options.WriteOptionDescriptions(Console.Out);
        }

        private static bool checkFile(string fileName, string fileType)
        {
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Error: " + fileType + " file not found");
                return false;
            }
            return true;
        }

        private static void scanFile(TextDecoder decoder, int stringLength, string outFile, Encoding encoding)
        {
            int filePosition = 0;
            try {
                using (StreamWriter writer = new StreamWriter(outFile, false, encoding))
                {
                    foreach (var textString in decoder.GetDecodedStrings())
                    {
                        if (textString.TextString.Count >= stringLength)
                        {
                            writer.WriteLine("Position: " + filePosition.ToString("X"));
                            writer.WriteLine(string.Concat(textString.TextString));
                            writer.WriteLine();
                        }
                        filePosition += textString.BytesRead + 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }
    }

    public class DecodedString
    {
        public readonly List<string> TextString;
        public readonly int BytesRead;

        public DecodedString(List<string> textString, int bytesRead)
        {
            TextString = textString;
            BytesRead = bytesRead;
        }
    }

    public static class ExtMethods
    {
        public static IEnumerable<DecodedString> GetDecodedStrings(this TextDecoder decoder)
        {
            List<string> decodedString = new List<string>();
            int consumedBytes = 0;
            do
            {
                decodedString.Clear();
                consumedBytes = decoder.DecodeChars(decodedString, string.Empty);
                if (consumedBytes >= 0)
                {
                    yield return new DecodedString(decodedString, consumedBytes);
                }
            } while (consumedBytes >= 0 && !decoder.BlockEmpty);
        }
    }
}
