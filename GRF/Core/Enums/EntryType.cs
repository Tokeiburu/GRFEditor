using System;

// ReSharper disable CheckNamespace
namespace GRF.Core {
	/// <summary>
	/// Quick note : 
	/// You'll usually see flags 3 or 5, meaning
	/// 3 = 1 + 2 -> File + HeaderCrypted
	/// 5 = 1 + 4 -> File + DataCrypted
	/// </summary>
	[Flags]
	public enum EntryType {
		Directory = 0,
		File = 1 << 0,
		HeaderCrypted = 1 << 1,
		FileAndHeaderCrypted = File | HeaderCrypted,
		DataCrypted = 1 << 2,
		FileAndDataCrypted = File | DataCrypted,
		RemoveFile = 1 << 4,
		GrfEditorCrypted = 1 << 5,
		Encrypt = 1 << 6,
		Decrypt = 1 << 7,
		FileNameRenamed = 1 << 8,
		CustomCompressed = 1 << 9,
		RawDataFile = 1 << 10,
		LZSS = 1 << 11,
	}
}