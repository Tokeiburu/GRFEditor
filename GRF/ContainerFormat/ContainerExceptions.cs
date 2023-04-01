using System;
using GRF.Core;
using GRF.IO;

namespace GRF.ContainerFormat {
	public static class GrfExceptions {
// ReSharper disable InconsistentNaming
		public static readonly FormattedExceptionMessage __NullPathException = "Path cannot be null or empty.\r\n{0}";
		public static readonly FormattedExceptionMessage __InvalidTextEncoding = "Text encoding invalid.\r\n{0}";
		public static readonly FormattedExceptionMessage __PathNotFound = "Path not found in the container.\r\n{0}";
		public static readonly FormattedExceptionMessage __PathsIdentical = "The paths are identical.\r\n{0}\r\n{1}";
		public static readonly FormattedExceptionMessage __InvalidCharactersInPath = "The path contains invalid characters.\r\n{0}";
		public static readonly FormattedExceptionMessage __DestIsSubfolder = "The destination folder is a subfolder of the source folder.";
		public static readonly FormattedExceptionMessage __FolderNameAlreadyExists = "Folder name already exists.\r\n{0}";
		public static readonly FormattedExceptionMessage __DestMustBeFolder = "You are trying to move '{0}' to a file ('{1}'); the destination must be a folder path.";
		public static readonly FormattedExceptionMessage __DestMustBeFile = "You are trying to move '{0}' to a folder ('{1}'); the destination must be a grf file path.";
		public static readonly FormattedExceptionMessage __HiddenFolderConflict = "Folder name not allowed (this happens if you had a folder with this name before; you'll have to undo the operations or save the GRF to edit the name).\r\n{0}";
		public static readonly FormattedExceptionMessage __FileNameAlreadyExists = "File name already exists.\r\nConflict with : {0}";
		public static readonly FormattedExceptionMessage __DoubleSlashPath = "The path contains double slashes (this is not allowed).\r\n{0}";
		public static readonly FormattedExceptionMessage __ArgumentNullValue = "The value cannot be null for the argument '{0}'.";
		public static readonly FormattedExceptionMessage __InvalidContainerFormat = "The container format is invalid for this operation.\r\nCurrent : {0}\r\nExpected format : {1}";
		public static readonly FormattedExceptionMessage __FileNotFound = "File not found.\r\n{0}";
		public static readonly FormattedExceptionMessage __HeaderLengthInvalid = "The length of the stream is too small for this container. Unable to parse the header.\r\nActual size : {0}\r\nExpected minimal size : {1}";
		public static readonly FormattedExceptionMessage __ReadContainerNotOpened = "Attempted to read the container before opening it.";
		public static readonly FormattedExceptionMessage __AccessContainerNotOpened = "Attempted to access a container before opening it.";
		public static readonly FormattedExceptionMessage __ReadContainerNotProperlyLoaded = "Attempted to read a container that wasn't loaded properly. NullReferenceException expected.";
		public static readonly FormattedExceptionMessage __NonInstiatedContainer = "Attempted to perform an operation on a non instantiated container.";
		public static readonly FormattedExceptionMessage __FileLocked = "Cannot access the file '{0}'. It is locked by another process.";
		public static readonly FormattedExceptionMessage __PatcherRequiresUpdate = "The patcher requires to be updated.";
		public static readonly FormattedExceptionMessage __ContainerBusy = "The container is currently busy and operations are disabled.";
		public static readonly FormattedExceptionMessage __ContainerSaving = "The GRF is already saving... wait or cancel the operation first.";
		public static readonly FormattedExceptionMessage __UnsupportedFileFormat = "The file format '{0}' is unknown or not supported.";
		public static readonly FormattedExceptionMessage __UnsupportedAction = "The method is not supported for this container.";
		public static readonly FormattedExceptionMessage __UnsupportedCompression = "The compression method does not support compressing files. It is meant for extracting the GRF's file content from their offset directly.";
		public static readonly FormattedExceptionMessage __ArgumentOutOfRangeMin = "The argument {0} is below the minimum value {1}.";
		public static readonly FormattedExceptionMessage __ArgumentOutOfRangeMax = "The argument {0} is above the maximum value {1}.";
		public static readonly FormattedExceptionMessage __ClosedImage = "The image component has been closed.";
		public static readonly FormattedExceptionMessage __NonLoadedImage = "Pixel data hasn't been set (invalid image format?).";
		public static readonly FormattedExceptionMessage __UnsupportedImageFormat = "The image format doesn't support this operation.";
		public static readonly FormattedExceptionMessage __NoFilesSelected = "At least one entry must be selected.";
		public static readonly FormattedExceptionMessage __ChecksumFailed = "The zlib checksum for the compressed data has failed.";
		public static readonly FormattedExceptionMessage __InvalidImagePosition = "The value '{0}' must be greater or equal to {1}.";
		public static readonly FormattedExceptionMessage __NoImageConverter = "No appropriate converter was found for the image. An image converter needs to be registered at the beginning of the application. The GrfImageObject file shows an example on how to create such a class.";
		public static readonly FormattedExceptionMessage __NoKeyFileSet = "No key file was set to decrypt this file.";
		public static readonly FormattedExceptionMessage __WrongKeyFile = "The key or key file is invalid.";
		public static readonly FormattedExceptionMessage __InvalidRepairArguments = "When opening a GRF in the repair mode, only the repair flag can be used.";
		public static readonly FormattedExceptionMessage __InvalidFileHeaderLength = "The minimum length of the header ({0}) is invalid, it must be greater than {1}.";
		public static readonly FormattedExceptionMessage __EncryptionCheckFlagInProgress = "You cannot extract files or save while the GRF is being inspected for encrypted content. This operation should be quick, please wait.";
		public static readonly FormattedExceptionMessage __LzmaCompression = "The compression used for this entry is LZMA and it is not supported by the current compression method. Please change the compression by going in Settings > General > Compression method > Custom...";
		public static readonly FormattedExceptionMessage __CompressionDllGuard = "GRF Editor has detected the custom compression DLL cannot be loaded and will most likely crash the application. The compression method has been resetted to the original file.";
		public static readonly FormattedExceptionMessage __RepackInstead = "The container should be repacked instead.";
		public static readonly FormattedExceptionMessage __EntryDataInvalid = "The file entry for '{0}' contains invalid information (the offset is invalid).";
		public static readonly FormattedExceptionMessage __CannotConvertWithErrors = "Cannot convert to a GRF container while the current container contains errors.";
		public static readonly FormattedExceptionMessage __FileFormatException = "Unrecognized file format ({0}).";
		public static readonly FormattedExceptionMessage __FileFormatException2 = "Unrecognized file format ({0}). {1}";
		public static readonly FormattedExceptionMessage __UnsupportedPixelFormat = "Unsupported pixel format : {0}.";
		public static readonly FormattedExceptionMessage __UnknownHashAlgorithm = "Unknown hash algorithm.";
		public static readonly FormattedExceptionMessage __UnsupportedFileVersion = "Unsupported file version.";
		public static readonly FormattedExceptionMessage __CompressionDllFailed = "Failed to load the decompression library ({0}).";
		public static readonly FormattedExceptionMessage __FailedToCompressData = "Failed to compress data.";
		public static readonly FormattedExceptionMessage __FailedToDecompressData = "Failed to decompress data.";
		public static readonly FormattedExceptionMessage __MergeVersionEncryptionException = "The destination GRF must have a version higher than 0x100 when merging with an ecrypted GRF.";
		public static readonly FormattedExceptionMessage __MergeNotSupported = "Attempted to merge a GRF by using a method that doesn't support this feature.";
		public static readonly FormattedExceptionMessage __CouldNotLoadGrf = "Couldn't load the GRF.";
		public static readonly FormattedExceptionMessage __ContainerNotSavedForRepack = "The container must be saved before repacking its content.";
		public static readonly FormattedExceptionMessage __OperationNotAllowed = "The container cannot execute the operation requested.";
		public static readonly FormattedExceptionMessage __ContainerNotSavedForCompact = "The container must be saved before redirecting its content.";
		public static readonly FormattedExceptionMessage __AddedGrfModified = "The GRF to add has been modified, save it first.";
		public static readonly FormattedExceptionMessage __UnsupportedEncryption = "The encryption feature is incompatible with the version used on this GRF. Decrypt your GRF from an older version and encrypt it again.";
		public static readonly FormattedExceptionMessage __ChangeVersionNotAllowed = "Operation not allowed: you cannot change the GRF version manually, use the ChangeVersion() command instead.";
		public static readonly FormattedExceptionMessage __GrfAccessViolationOpened = "Attempted to perform an operation on an opened GRF.";
		public static readonly FormattedExceptionMessage __GrfAccessViolationClosed = "Attempted to perform an operation on an closed GRF.";
		public static readonly FormattedExceptionMessage __InvalidImageFormat = "Unable to parse the image data. The content is either corrupted or not supported.";
		public static readonly FormattedExceptionMessage __CorruptedOrEncryptedEntry = "Failed to decompress data. The following entry is either corrupted or encrypted: \r\n{0}";
		public static readonly FormattedExceptionMessage __GrfSizeLimitReached = "Failed to save the GRF, size limit reached (4,294,967,295 bytes).";

		internal static GrfException Create(FormattedExceptionMessage exception, params object[] items) {
			return new GrfException(exception, String.Format(exception.Message, items));
		}

		internal static Exception ThrowNullPathException(string path) {
			throw Create(__NullPathException, path);
		}

		internal static Exception ThrowInvalidTextEncoding(string path) {
			throw Create(__InvalidTextEncoding, path);
		}

		internal static Exception ThrowPathNotFound(string path) {
			throw Create(__PathNotFound, path);
		}

		internal static Exception ThrowPathsIdentical(string path1, string path2) {
			throw Create(__PathsIdentical, path1, path2);
		}

		internal static void ThrowInvalidCharactersInPath(string path) {
			throw Create(__InvalidCharactersInPath, path);
		}

		internal static void ThrowDestIsSubfolder() {
			throw Create(__DestIsSubfolder);
		}

		internal static void ThrowDestMustBeFolder(string path1, string path2) {
			throw Create(__DestMustBeFolder, path1, path2);
		}

		internal static void ThrowDestMustBeFile(string path1, string path2) {
			throw Create(__DestMustBeFile, path1, path2);
		}

		internal static void ThrowFolderNameAlreadyExists(string path) {
			throw Create(__FolderNameAlreadyExists, path);
		}

		internal static void ThrowHiddenFolderConflict(string path) {
			throw Create(__HiddenFolderConflict, path);
		}

		internal static void ThrowFileNameAlreadyExists(string path) {
			throw Create(__FileNameAlreadyExists, path);
		}

		internal static void ThrowDoubleSlashPath(string path) {
			throw Create(__DoubleSlashPath, path);
		}

		internal static void IfNullThrow(object value, string name) {
			if (value == null)
				throw Create(__ArgumentNullValue, name);
		}

		internal static void ThrowInvalidContainerFormat(string path, string expectedFormat) {
			throw Create(__InvalidContainerFormat, path, expectedFormat);
		}

		internal static void ThrowFileNotFound(string path) {
			throw Create(__FileNotFound, path);
		}

		internal static void ThrowHeaderLengthInvalid(int length, int expected) {
			throw Create(__HeaderLengthInvalid, length, expected);
		}

		internal static void ThrowReadContainerNotOpened() {
			throw Create(__ReadContainerNotOpened);
		}

		internal static void ThrowReadContainerNotProperlyLoaded() {
			throw Create(__ReadContainerNotProperlyLoaded);
		}

		internal static void ThrowNonInstiatedContainer() {
			throw Create(__NonInstiatedContainer);
		}

		internal static void Throw(string message) {
			throw Create(message);
		}

		internal static void ThrowFileLocked(string path) {
			throw Create(__FileLocked, path);
		}

		internal static void ThrowAccessContainerNotOpened() {
			throw Create(__AccessContainerNotOpened);
		}

		internal static void IfSavingThrow<T>(ContainerAbstract<T> container) where T : ContainerEntry {
			if (container.IsBusy || container.Commands.IsLocked)
				throw Create(__ContainerBusy);
		}

		internal static void IfTrueThrowContainerSaving(bool value) {
			if (value)
				throw Create(__ContainerSaving);
		}

		internal static void ThrowUnsupportedFileFormat(string value) {
			throw Create(__UnsupportedFileFormat);
		}

		internal static void IfTrueThrowClosedImage(bool value) {
			if (value)
				throw Create(__ClosedImage);
		}

		internal static void IfNullThrowNonLoadedImage(object value) {
			if (value == null)
				throw Create(__NonLoadedImage);
		}

		internal static void IfLtZeroThrowUnsupportedImageFormat(int value) {
			if (value < 0)
				throw Create(__UnsupportedImageFormat);
		}

		internal static void IfLtZeroThrowInvalidImagePosition(string name, int value) {
			if (value < 0)
				throw Create(__InvalidImagePosition, name, value);
		}

		public static void IfOutOfRangeThrow(float tolerance, string name, float minInclude, float maxInclude) {
			if (tolerance < minInclude)
				throw Create(__ArgumentOutOfRangeMin, name, minInclude);

			if (tolerance > maxInclude)
				throw Create(__ArgumentOutOfRangeMax, name, maxInclude);
		}

		public static void IfOutOfRangeThrow(double tolerance, string name, double minInclude, double maxInclude) {
			if (tolerance < minInclude)
				throw Create(__ArgumentOutOfRangeMin, name, minInclude);

			if (tolerance > maxInclude)
				throw Create(__ArgumentOutOfRangeMax, name, maxInclude);
		}

		public static void IfOutOfRangeThrow(int tolerance, string name, int minInclude, int maxInclude) {
			if (tolerance < minInclude)
				throw Create(__ArgumentOutOfRangeMin, name, minInclude);

			if (tolerance > maxInclude)
				throw Create(__ArgumentOutOfRangeMax, name, maxInclude);
		}

		internal static void ThrowNoKeyFileSet() {
			throw Create(__NoKeyFileSet);
		}

		internal static void ThrowLzmaCompression() {
			throw Create(__LzmaCompression);
		}

		internal static void ThrowCompressionDllGuard() {
			throw Create(__CompressionDllGuard);
		}

		internal static void ThrowCannotConvertWithErrors() {
			throw Create(__CannotConvertWithErrors);
		}

		public static void IfEncryptionCheckFlagThrow(GrfHolder grf) {
			if (grf != null && grf.IsOpened && grf.Header.EncryptionCheckFlag) {
				throw Create(__EncryptionCheckFlagInProgress);
			}
		}

		internal static void IfEncryptionCheckFlagThrow(Container grf) {
			if (grf != null && grf.InternalHeader.EncryptionCheckFlag) {
				throw Create(__EncryptionCheckFlagInProgress);
			}
		}

		internal static void ThrowRepackInstead() {
			throw Create(__RepackInstead);
		}

		internal static void ThrowContainerNotSavedForRepack() {
			throw Create(__ContainerNotSavedForRepack);
		}

		internal static void ThrowOperationNotAllowed() {
			throw Create(__OperationNotAllowed);
		}

		internal static void ThrowContainerNotSavedForCompact() {
			throw Create(__ContainerNotSavedForCompact);
		}

		internal static void ThrowEntryDataInvalid(string path) {
			throw Create(__EntryDataInvalid, path);
		}

		public static void ThrowFileFormatException(string ext) {
			throw Create(__FileFormatException, ext);
		}

		public static void ThrowFileFormatException(string ext, string extra) {
			throw Create(__FileFormatException2, ext, extra);
		}

		internal static void ThrowUnsupportedPixelFormat(byte bits) {
			throw Create(__UnsupportedPixelFormat, bits);
		}

		internal static void ThrowUnknownHashAlgorithm() {
			throw Create(__UnknownHashAlgorithm);
		}

		public static void ThrowUnsupportedFileVersion() {
			throw Create(__UnsupportedFileVersion);
		}

		internal static void ThrowFailedToCompressData() {
			throw Create(__FailedToCompressData);
		}

		internal static void ThrowFailedToDecompressData() {
			throw Create(__FailedToDecompressData);
		}

		internal static void ThrowCompressionDllFailed(string dllName) {
			throw Create(__CompressionDllFailed, dllName ?? "NULL");
		}

		internal static void ThrowMergeVersionEncryptionException() {
			throw Create(__MergeVersionEncryptionException);
		}

		internal static void ThrowAddedGrfModified() {
			throw new InvalidOperationException(__AddedGrfModified);
		}

		internal static void ThrowMergeNotSupported() {
			throw Create(__MergeNotSupported);
		}

		internal static void ThrowUnsupportedEncryption() {
			throw Create(__UnsupportedEncryption);
		}

		internal static void ThrowWrongKeyFile() {
			throw Create(__WrongKeyFile);
		}

		internal static void ThrowInvalidRepairArguments() {
			throw Create(__InvalidRepairArguments);
		}

		internal static void ValidateHeaderLength(IBinaryReader reader, string type, int length) {
			if (reader.Length < length)
				throw Create(__InvalidFileHeaderLength, type, length);
		}

		internal static void ThrowUnsupportedImageFormat() {
			throw Create(__UnsupportedImageFormat);
		}

		internal static void ThrowNoFilesSelected() {
			throw Create(__NoFilesSelected);
		}

		internal static void ThrowChecksumFailed() {
			throw Create(__ChecksumFailed);
		}
	}

	public class FormattedExceptionMessage {
		private static int _errorId;

		private readonly int _id;
		private readonly string _message;

		public FormattedExceptionMessage(string message) {
			_message = message;
			_errorId++;
			_id = _errorId;
		}

		public string Message {
			get { return _message; }
		}

		protected bool Equals(FormattedExceptionMessage other) {
			return _id == other._id;
		}

		public override int GetHashCode() {
			return _id;
		}

		public string Display(params object[] args) {
			return String.Format(Message, args);
		}

		public static implicit operator FormattedExceptionMessage(string message) {
			return new FormattedExceptionMessage(message);
		}

		public static implicit operator string(FormattedExceptionMessage message) {
			return message.Message;
		}

		public override bool Equals(object obj) {
			var formattedExceptionMessage = obj as FormattedExceptionMessage;
			if (formattedExceptionMessage != null)
				return Equals(formattedExceptionMessage);
			return false;
		}

		public GrfException Create(params object[] items) {
			return GrfExceptions.Create(this, items);
		}
	}

	public class GrfException : Exception {
		private readonly FormattedExceptionMessage _format;

		public GrfException(FormattedExceptionMessage format, string message) : base(message) {
			_format = format;
		}

		public FormattedExceptionMessage Format {
			get { return _format; }
		}

		protected bool Equals(FormattedExceptionMessage other) {
			return Equals(_format, other);
		}

		public override int GetHashCode() {
			return (_format != null ? _format.GetHashCode() : 0);
		}

		public override bool Equals(object obj) {
			if (obj is GrfException) {
				return Equals((obj as GrfException).Format);
			}

			if (obj is FormattedExceptionMessage) {
				return Equals(obj as FormattedExceptionMessage);
			}

			return false;
		}

		public static bool operator ==(GrfException exp1, GrfException exp2) {
			if (ReferenceEquals(exp1, null) && ReferenceEquals(exp2, null)) return true;
			if (ReferenceEquals(exp1, null) || ReferenceEquals(exp2, null)) return false;

			return exp1.Equals(exp2);
		}

		public static bool operator !=(GrfException exp1, GrfException exp2) {
			return !(exp1 == exp2);
		}

		public static bool operator ==(GrfException exp1, FormattedExceptionMessage exp2) {
			if (ReferenceEquals(exp1, null) && ReferenceEquals(exp2, null)) return true;
			if (ReferenceEquals(exp1, null) || ReferenceEquals(exp2, null)) return false;

			return exp1.Equals(exp2);
		}

		public static bool operator !=(GrfException exp1, FormattedExceptionMessage exp2) {
			return !(exp1 == exp2);
		}
	}
}