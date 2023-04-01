namespace GRF {
	/// <summary>
	/// This class contains all the strings used by the GRF library.
	/// </summary>
	public static class GrfStrings {
		#region Commands
		public const string AddFilesToRemove = "Add file to remove '{0}'";
		public const string AddFileToRemove = "Add files to remove...";
		public const string AddFile = "Add file '{0}' in '{1}'";
		public const string AddFiles = "Add files in '{0}'...";
		public const string AddFolder = "Folder added '{0}'";
		public const string ChangeVersion = "Change format from '{0}' to '{1}'";
		public const string DecryptFiles = "Decrypt files...";
		public const string DeleteFile = "Delete file '{0}'";
		public const string DeleteFiles = "Delete files...";
		public const string DeleteFolder = "Delete folder '{0}'";
		public const string DeleteFolders = "Delete folders...";
		public const string EncryptFiles = "Encrypt files...";
		public const string GroupCommand = "Group command ({0}) :\r\n";
		public const string MoveFile = "Move file '{0}' to '{1}'";
		public const string MoveFiles = "Move files '{0}' to '{1}'";
		public const string RenameFile =  "Rename file '{0}' with '{1}'";
		public const string RenameFolder =  "Rename folder '{0}' with '{1}'";
		public const string ReplaceFile = "Replace '{0}' with '{1}'";
		#endregion

		#region File format header
		public const string UnrecognizedFileFormat = "Unrecognized file format.";
		public const string TgaBitsExpected = "Expected 24 or 32 bits per pixel, found {0} instead.";
		public const string TgaImageTypeExpected = " Image type = {0}";
		#endregion

		#region Compression DLL
		public const string DisplayGravityDll =  "Gravity Official Zlib Library";
		public const string DisplayGrfEditorDll = "GRF Editor Custom Zlib Library";
		public const string DisplayLzmaDll = "LZMA Library (by Curiosity)";
		public const string DisplayNullCompression = "No compression (debug mode)";
		public const string RecoveryCompression = "Force decompression (Recovery)";
		public const string CustomCompression = "Custom compression...";
		#endregion

		#region Encryption
		public const string FailedHeaderEncrypted = "GRF encryption can only be used for RGZ, GRF and GPF files which use the 0x200 version.";
		public const string EncryptionNotSet = "Couldn't set the encryption key. The key must be the same as the one used for the encryption. If you're trying to encrypt a GRF again with a different key, then you must decrypt the entire content first.";
		public const string DecryptionNotSet = "Couldn't set the decryption key. The key must be the same as the one used for the encryption.";
		#endregion

		#region Errors
		public const string FailedNullString =  "Failed to recognize the null-terminated string: {0}.";
		public const string FailedReadContainer = "Failed to read the container: {0}";
		public const string FailedEncodingString = "File name is wrongly encoded: {0}.";
		public const string FailedGrfHeader = "File header is invalid, excpected 'Master of Magic', found '{0}'.";
		public const string GrfContainsErrors = "There were errors detected while loading or while modifying the GRF. It's strongly advised to save the GRF with another name and check if the content has been correctly updated.\n\nWould you like to save it anyway?";
		public const string CouldNotSaveContainer = "Couldn't save the container file.";
		public const string CouldNotSaveContainerForceReload = "Couldn't save the container file. The size limit has been reached and the GRF must be forcibly reloaded.";
		#endregion

		#region Others
		public const string GrfDataIntegrity = "Grf data integrity";
		public const string GrfIntegrityFile = "data.integrity";
		public const string DisplayNoFileType =  "None";
		public const string EncryptionFilename = "__encryption.info";
		public const string RgzRoot = "root\\";
		public const string CurrentlyOpenedGrf = "Currently opened GRF: ";	// Used by MultiGrfReader
		public const string NoExceptions = "No exceptions.";
		public const string DataStreamId = "[data stream]";
		public const string EncryptionDbFile = "_encryptiondb.grf";
		#endregion

	}
}
