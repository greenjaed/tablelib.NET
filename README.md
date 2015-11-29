# tablelib.NET
Klarth's popular custom text-encoding library ported to .NET

## Introduction
Like the original library, the purpose of this library is to transform text in one encoding to another.  This usually means taking binary data and converting it into a readable format.  It is especially useful for dealing with custom character encodings, most commonly found in older console games.  The library consists of a Table Reader which reads a custom character encoding table; a text decoder which extracts text in various formats; and, a text encoder which does a simple conversion from text to binary.

## What's Different
In converting the library from c++ to c#, aside from the usual changing of data scructures to .NET equivalents, I did change the interface a bit.  Instead of separate methods to get characters depending on whether you're inserting or dumping, the library exposes a single dictionary whose contents change depending on the TableReaderType.  The SetHexBlock method in the TextDecoder now consists of just one method that takes a byte array instead of the two methods of the c++ version.  Finally, for both coders, the data structure containing the errors from parsing the table is now accessed from a property instead of a method call.

As part of the conversion to .NET, the TableReader now requires that the encoding of the table file be specified.  Due to how .NET processes strings, the encoding is essential to ensuring the text is coded correctly.  The TableReader comes with a list of predefined encodings, but it also allows the user to provide custom encoding.

This version does take advantage of the functionality of the CLR and adds a lot of accessory methods to both coders.  The TextDecoder now has the ability to decode fixed-length strings, even those at separate intervals.  Both coders now have methods which will return the direct results of the coding rather than retrieving the result from a referenced object.  And, both coders now support mass conversion rather than working one line at a time.

## How To Use
