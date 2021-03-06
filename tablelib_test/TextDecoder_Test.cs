﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Xunit;
using TableLib;

namespace tablelib_test
{
    public class TextDecoder_Test
    {
        //test for linked entries
        public TextDecoder_Test()
        {
            using (StreamWriter tableWriter = new StreamWriter("test.tbl"))
            {
                tableWriter.WriteLine("00=e");
                tableWriter.WriteLine("0001=E");
                tableWriter.WriteLine("01=s");
                tableWriter.WriteLine("02=t");
                tableWriter.WriteLine("/03=0");
                tableWriter.WriteLine("$05=i,1");
            }
        }

        [Fact]
        public void RunUninitialized()
        {
            TextDecoder decoder = new TextDecoder();
            string temp = string.Empty;
            int result = decoder.DecodeString(ref temp, string.Empty);
            Assert.Equal(0, result);
        }

        [Theory]
        [InlineData (new byte[] { 2, 0, 1, 2, 4, 3, 5 }, "0", 6, "tEt<$04>0", false)]
        [InlineData (new byte[] { 2, 0, 3, 1, 2 }, "0", 3, "te0", false)]
        [InlineData (new byte[] { 2, 0, 3, 1, 2 }, "", 5, "te0st", false)]
        [InlineData (new byte[] { 0, 0, 1, 1 }, "s", 4, "eEs", false)]
        [InlineData (new byte[] { 5, 0, 1}, "s", 3, "i<$00>s", false)]
        [InlineData (new byte[] { 1, 2, 2, 4, 0, 1, 2 }, "", 3, "stt", true)]
        public void NormalDecode(byte[] encodedString, string endString, int readBytes, string expectedString, bool stopOnUndefinedChar)
        {
            TextDecoder decoder = initDecoder();
            decoder.StopOnUndefinedCharacter = stopOnUndefinedChar;
            decoder.SetHexBlock(encodedString);
            string decodedString = string.Empty;
            int parsedCharacters = decoder.DecodeString(ref decodedString, endString);
            Assert.Equal(readBytes, parsedCharacters);
            Assert.Equal(expectedString, decodedString);
        }

        [Fact]
        public void MultiLineDecode()
        {
            TextDecoder decoder = initDecoder();
            decoder.SetHexBlock(new byte[] { 0, 3, 1, 3, 2 });
            var decodedStrings = decoder.GetDecodedStrings("0");
            Assert.Equal(3, decodedStrings.Count());
        }

        [Fact]
        public void InvalidStringLengthOffsetThrowsException()
        {
            TextDecoder decoder = initDecoder();
            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.StringLength = 0);
            decoder.StringLength = 5;
            Assert.Throws<ArgumentOutOfRangeException>(() => decoder.StringOffset = 4);
        }
            
        [Fact]
        public void NormalFixedLengthDecode()
        {
            TextDecoder decoder = initDecoder();
            decoder.SetHexBlock(new byte[] { 3, 2, 1, 0 });
            List<String> decodeResult = decoder.GetDecodedFixedLengthStrings().ToList();
            Assert.Equal(1, decodeResult.First().Length);
            Assert.Equal(4, decodeResult.Count());
        }

        [Fact]
        public void FixedLengthDecodeOffsetAndPartialCheck()
        {
            TextDecoder decoder = initDecoder();
            decoder.StringLength = 2;
            decoder.StringOffset = 3;
            decoder.SetHexBlock(new byte[] { 1, 2, 0xff, 0});
            string results = decoder.DecodeFixedLengthString();
            Assert.Equal("st", results);
            results = decoder.DecodeFixedLengthString();
            Assert.Equal("e", results);
        }

        [Fact]
        public void DecodingStopsOnBadLink()
        {
            var decoder = initDecoder();
            decoder.SetHexBlock(new byte[] { 5 });
            string temp = string.Empty;
            Assert.Equal(-1, decoder.DecodeString(ref temp, string.Empty));
        }

        private TextDecoder initDecoder()
        {
            TextDecoder decoder = new TextDecoder();
            decoder.OpenTable("test.tbl");
            return decoder;
        }
    }
}

