// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*============================================================
**
** Class:  BinaryWriter
** 
** <OWNER>gpaperin</OWNER>
**
** Purpose: Provides a way to write primitives types in 
** binary from a Stream, while also supporting writing Strings
** in a particular encoding.
**
**
===========================================================*/

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Text;

namespace GRF.IO {
    
    public class NonClosingBinaryWriter : BinaryWriter {
	    public NonClosingBinaryWriter(Stream stream) : base(stream) {
		    
	    }

		protected override void Dispose(bool disposing) {
			if (!disposing)
				return;
			this.OutStream.Flush();
			OutStream = null;
		}
    }
}