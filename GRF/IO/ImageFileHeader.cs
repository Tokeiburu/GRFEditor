using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GRF.IO {
	public class ImageFileHeader {
		public struct IMAGE_DOS_HEADER {      // DOS .EXE header
			public UInt16 e_magic;              // Magic number
			public UInt16 e_cblp;               // Bytes on last page of file
			public UInt16 e_cp;                 // Pages in file
			public UInt16 e_crlc;               // Relocations
			public UInt16 e_cparhdr;            // Size of header in paragraphs
			public UInt16 e_minalloc;           // Minimum extra paragraphs needed
			public UInt16 e_maxalloc;           // Maximum extra paragraphs needed
			public UInt16 e_ss;                 // Initial (relative) SS value
			public UInt16 e_sp;                 // Initial SP value
			public UInt16 e_csum;               // Checksum
			public UInt16 e_ip;                 // Initial IP value
			public UInt16 e_cs;                 // Initial (relative) CS value
			public UInt16 e_lfarlc;             // File address of relocation table
			public UInt16 e_ovno;               // Overlay number
			public UInt16 e_res_0;              // Reserved words
			public UInt16 e_res_1;              // Reserved words
			public UInt16 e_res_2;              // Reserved words
			public UInt16 e_res_3;              // Reserved words
			public UInt16 e_oemid;              // OEM identifier (for e_oeminfo)
			public UInt16 e_oeminfo;            // OEM information; e_oemid specific
			public UInt16 e_res2_0;             // Reserved words
			public UInt16 e_res2_1;             // Reserved words
			public UInt16 e_res2_2;             // Reserved words
			public UInt16 e_res2_3;             // Reserved words
			public UInt16 e_res2_4;             // Reserved words
			public UInt16 e_res2_5;             // Reserved words
			public UInt16 e_res2_6;             // Reserved words
			public UInt16 e_res2_7;             // Reserved words
			public UInt16 e_res2_8;             // Reserved words
			public UInt16 e_res2_9;             // Reserved words
			public UInt32 e_lfanew;             // File address of new exe header
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct IMAGE_FILE_HEADER {
			public UInt16 Machine;
			public UInt16 NumberOfSections;
			public UInt32 TimeDateStamp;
			public UInt32 PointerToSymbolTable;
			public UInt32 NumberOfSymbols;
			public UInt16 SizeOfOptionalHeader;
			public UInt16 Characteristics;
		}

		public static bool Is32BitHeader(IMAGE_FILE_HEADER fileHeader) {
			UInt16 IMAGE_FILE_32BIT_MACHINE = 0x0100;
			return (IMAGE_FILE_32BIT_MACHINE & fileHeader.Characteristics) == IMAGE_FILE_32BIT_MACHINE;
		}

		public static IMAGE_FILE_HEADER GetImageFileHeader(byte[] dllData) {
			IMAGE_FILE_HEADER fileHeader;
			IMAGE_DOS_HEADER dosHeader;

			using (MemoryStream stream = new MemoryStream(dllData, 0, dllData.Length)) {
				BinaryReader reader = new BinaryReader(stream);
				dosHeader = FromBinaryReader<IMAGE_DOS_HEADER>(reader);

				// Add 4 bytes to the offset
				stream.Seek(dosHeader.e_lfanew, SeekOrigin.Begin);
				UInt32 ntHeadersSignature = reader.ReadUInt32();
				fileHeader = FromBinaryReader<IMAGE_FILE_HEADER>(reader);
			}

			return fileHeader;
		}

		public static T FromBinaryReader<T>(BinaryReader reader) {
			// Read in a byte array
			byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

			// Pin the managed memory while, copy it out the data, then unpin it
			GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
			T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
			handle.Free();

			return theStructure;
		}
	}
}
