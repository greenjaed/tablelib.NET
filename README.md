# tablelib.NET
Klarth's popular custom text-encoding library ported to .NET

## Introduction
Like the original library, the purpose of this library is to transform text in one encoding to another.  This usually means taking binary data and converting it into a readable format.  It is especially useful for dealing with custom character encodings, most commonly found in older console games.  The library consists of a Table Reader which reads a custom character encoding table; a text decoder which extracts text in various formats; and, a text encoder which does a simple conversion from text to binary.

## What's Different
In converting the library from c++ to c#, aside from the usual changing of data scructures to .NET equivalents, I did change the interface a bit.  Instead of separate methods to get characters depending on whether you're inserting or dumping, the library exposes a single dictionary whose contents change depending on the TableReaderType.  The SetHexBlock method in the TextDecoder now consists of just one method that takes a byte array instead of the two methods of the c++ version.  Finally, for both coders, the data structure containing the errors from parsing the table is now accessed from a property instead of a method call.

As part of the conversion to .NET, the TableReader now requires that the encoding of the table file be specified.  Due to how .NET processes strings, the encoding is essential to ensuring the text is coded correctly.  The TableReader takes standard encodings, but it also allows the user to provide other encodings.

This version does take advantage of the functionality of the CLR and adds a lot of accessory methods to both coders.  The TextDecoder now has the ability to decode fixed-length strings, even those at separate intervals.  It also includes methods for returning a list of the decoded characters rather than a raw string.  Both coders now have methods which will return the direct results of the coding rather than retrieving the result from a referenced object.  And, both coders now support mass conversion rather than working one line at a time.

## Build Instructions
Currently the build process consists of opening the solution file in either MonoDevelop or Visual Studio and building the project from there.  Before building, make sure you have the latest version of Nuget so that all depencies get resolved successfully.  The solution has been successfully built in both VS2015 and MonoDevelop.

## How To Use
This section is not meant to be complete library documentation, but a general overview.

###TableReader
To use the TableReader, instantiate a new object in the correct mode and run `OpenTable` with the name of a table file.  If it loaded correctly, `OpenTable` will return true and `TableErrors` will be empty.  You can now look up values in the `LookupValue` object.  The value returned from `LookupValue` changes depending on the mode you're in.  For instance, if you have an entry "05=A" in your table, a TableReader in mode `ReadTypeDump` will return "A" when you give it "05" whereas a TableReader in mode `ReadTypeInsert` will return "05" when given "A"

###TextDecoder
To use the TextDecoder, instantiate a new object, open a table with an `OpenTable` method, set the block you want to decode with the `SetHexBlock` method, and run one of the `DecodeString` methods.  The methods vary in what arguments they take and what they return, but the most thorough method takes the string which will hold the decoded string and an endstring, the string that signifies the end of a string, and returns the number of bytes decoded.  The `DecodeString` methods also have `DecodeChars` counterparts that retrieve a list of decoded "characters" (the individual entries in the table file).
The TextDecoder also has the ability to decode fixed-length strings.  To decode these, set `StringLength` and `StringOffset` (the distance between the beginning of strings) and run `DecodeFixedLengthString`.
All the flavors of decoders also have methods for retrieving all strings as an IEnumerable.

###TextEncoder
To use the TextEncoder, instantiate a new object, open a table with an `OpenTable` method and run the `EncodeStream` method.  The base method requires the script to encode and a reference to an int that will hold the offset for a bad character if one is encountered and returns the number bytes encoded.  The encoded string is stored in the StringTable object.  An alternative method returns the encoded string instead.
