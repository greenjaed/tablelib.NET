using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using TableLib;

namespace tablelib_test
{
    public class TextEncoder_Test
    {
        public TextEncoder_Test()
        {
            using (StreamWriter tableWriter = new StreamWriter( "test.tbl"))
            {
                tableWriter.WriteLine("00=e");
                tableWriter.WriteLine("01=s");
                tableWriter.WriteLine("02=t");
                tableWriter.WriteLine("/03=0");
                tableWriter.WriteLine("04=ste");
                tableWriter.WriteLine("$05=i,1");
            }
        }

        [Fact]
        public void UninitializedRun()
        {
            TextEncoder encoder = new TextEncoder();
            int errorLocation = 0;
            int encodedLength = encoder.EncodeStream(string.Empty, ref errorLocation);
            Assert.Equal(0, encodedLength);
            Assert.Equal(0, errorLocation);
            Assert.Empty(encoder.StringTable);
        }

        [Theory]
        [InlineData("test<$05>", false, new byte[] {2, 0, 1, 2, 5})]
        [InlineData("sets0", false, new byte[] {1, 0, 2, 1})]
        [InlineData("se0ts", true, new byte[] {1, 0, 3})]
        [InlineData("stet", false, new byte[] {4, 2})]
        public void NormalEncode(string rawString, bool includeEndString, byte[] expectedEncode)
        {
            TextEncoder encoder = initEncoder();
            int errorLoc = 0;
            encoder.AddEndToken = includeEndString;
            encoder.EncodeStream(rawString, ref errorLoc);
            Assert.Equal(expectedEncode, encoder.StringTable.First().Text);
        }

        [Fact]
        public void SingleStringMultipleEncodedStrings()
        {
            TextEncoder encoder = initEncoder();
            string rawString = "t0e0s0t0";
            encoder.AddEndToken = false;
            List<byte[]> encodedStrings = encoder.EncodeStream(rawString).ToList();
            Assert.Equal(4, encodedStrings.Count);
            Assert.Equal(4, encoder.StringTable.Count);
        }

        [Fact]
        public void EncodeStringNotInTableFails()
        {
            TextEncoder encoder = initEncoder();
            string rawString = "tempest";
            int errorLoc = 0;
            Assert.Throws<Exception>(() => encoder.EncodeStream(rawString).ToList());
            encoder.EncodeStream(rawString, ref errorLoc);
            Assert.Equal(2, errorLoc);
        }

        private TextEncoder initEncoder()
        {
            TextEncoder encoder = new TextEncoder();
            encoder.OpenTable("test.tbl");
            return encoder;
        }
    }
}

