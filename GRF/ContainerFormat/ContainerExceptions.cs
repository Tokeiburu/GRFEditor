using System;
using GRF.Core;

namespace GRF.ContainerFormat {
	public static class GrfExceptions {
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
		public static readonly FormattedExceptionMessage __ContainerClosed = "Attempted to read a container that has been closed.";
		public static readonly FormattedExceptionMessage __UnsupportedFileFormat = "The file format '{0}' is unknown or not supported.";
		public static readonly FormattedExceptionMessage __UnsupportedAction = "The method is not supported for this container.";
		public static readonly FormattedExceptionMessage __UnsupportedCompression = "The compression method does not support compressing files. It is meant for extracting the GRF's file content from their offset directly.";
		public static readonly FormattedExceptionMessage __ArgumentOutOfRangeMin = "The argument {0} is below the minimum value {1}.";
		public static readonly FormattedExceptionMessage __ArgumentOutOfRangeMax = "The argument {0} is above the maximum value {1}.";
		public static readonly FormattedExceptionMessage __NonLoadedImage = "Pixel data hasn't been set (invalid image format?).";
		public static readonly FormattedExceptionMessage __UnsupportedImageFormat = "The image format doesn't support this operation.";
		public static readonly FormattedExceptionMessage __UnsupportedImageFormatMethod = "The image format doesn't support this operation '{1}'.";
		public static readonly FormattedExceptionMessage __NoFilesSelected = "At least one entry must be selected.";
		public static readonly FormattedExceptionMessage __ChecksumFailed = "The zlib checksum for the compressed data has failed.";
		public static readonly FormattedExceptionMessage __InvalidImagePosition = "The value '{0}' must be greater or equal to {1}.";
		public static readonly FormattedExceptionMessage __NoImageConverter = "No appropriate converter was found for the image. An image converter needs to be registered at the beginning of the application with ImageConverterManager.AddConverter(AbstractImageConverter). You can use DefaultImageConverter in GrfToWpfBridge.dll (WPF) or ImageConverter1 in ExampleProject (WinForms).";
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
		public static readonly FormattedExceptionMessage __ResourceNotFound = "Failed to load the decompression library ({0}). Resource not found.";
		public static readonly FormattedExceptionMessage __LoadLibraryFailed = "Failed to load the decompression library ({0}).\r\nLast LoadLibrary error: {1} | 0x{2:X8}.";
		public static readonly FormattedExceptionMessage __CompressionDllFailed2 = "Failed to load the decompression library ({0}).\r\n\r\nYou are most likely missing the following VC++ Redistributable: {1}.";
		public static readonly FormattedExceptionMessage __CompressionDllFailed3 = "Failed to load the decompression library ({0}).\r\n\r\nThe function '{1}' couldn't be loaded. Expected function declaration '{2}'.";
		public static readonly FormattedExceptionMessage __EncryptionDllFailed = "Last LoadLibrary error: {1}.";
		public static readonly FormattedExceptionMessage __EncryptionDllFailed2 = "Last LoadLibrary error: {1}.\nInvalid compilation target, expected {2}-bit DLL, found {3}-bit DLL.";
		public static readonly FormattedExceptionMessage __DllMissingFunction = "Failed to load the library, missing function ({0}).";
		public static readonly FormattedExceptionMessage __FailedToCompressData = "Failed to compress data.";
		public static readonly FormattedExceptionMessage __FailedToDecompressData = "Failed to decompress data.";
		public static readonly FormattedExceptionMessage __MergeVersionEncryptionException = "The destination GRF must have a version higher than 0x100 when merging with an ecrypted GRF.";
		public static readonly FormattedExceptionMessage __MergeNotSupported = "Attempted to merge a GRF on a container format that doesn't support this feature.";
		public static readonly FormattedExceptionMessage __CouldNotLoadGrf = "Couldn't load the GRF.";
		public static readonly FormattedExceptionMessage __ContainerNotSavedForRepack = "The container must be saved before repacking its content.";
		public static readonly FormattedExceptionMessage __OperationNotAllowed = "The container cannot execute the operation requested.";
		public static readonly FormattedExceptionMessage __ContainerNotSavedForCompact = "The container must be saved before redirecting its content.";
		public static readonly FormattedExceptionMessage __AddedGrfModified = "The GRF to add has been modified, save it first.";
		public static readonly FormattedExceptionMessage __ChangeVersionNotAllowed = "Operation not allowed: you cannot change the GRF version manually, use the ChangeVersion() command instead.";
		public static readonly FormattedExceptionMessage __GrfAccessViolationOpened = "Attempted to perform an operation on an opened GRF.";
		public static readonly FormattedExceptionMessage __GrfAccessViolationClosed = "Attempted to perform an operation on an closed GRF.";
		public static readonly FormattedExceptionMessage __InvalidImageFormat = "Unable to parse the image data. The content is either corrupted or not supported.";
		public static readonly FormattedExceptionMessage __CorruptedOrEncryptedEntry = "Failed to decompress data. The following entry is either corrupted or encrypted: \r\n{0}";
		public static readonly FormattedExceptionMessage __GrfSizeLimitReached = "Failed to save the GRF, size limit reached (4,294,967,295 bytes).";
		public static readonly FormattedExceptionMessage __InvalidSprConvertMode = "Unexpected SprConvertMode for this function.";
		public static readonly FormattedExceptionMessage __GravityEncryptedFile = "The file '{0}' is using a new encryption method by Gravity which is not supported.";
		public static readonly FormattedExceptionMessage __ThorFileDeleted = "The file '{0}' is a special GRF entry used by patchers to signal its deletion. It has no content.";
		public static readonly FormattedExceptionMessage __SprSizeLimitReached = "Image [index: {2}, {0}x{1}] has too many pixels (max: {3}), the limit is 65536. Consider converting the image to Bgra32 (no size limit).";
		public static readonly FormattedExceptionMessage __SprRleBufferOverflowException = "Buffer overflow while executing the Rle compression or decompressing.";
		public static readonly FormattedExceptionMessage __UnsupportedEncryptionOperation = "The encryption operation is incompatible with the version used on this GRF.";
		public static readonly FormattedExceptionMessage __UnsupportedEncryptionVersion = "GRF encryption can only be used for THOR, GRF and GPF files which use the 0x200 or 0x300 format.";

		internal static void IfNullThrow(object value, string name) {
			if (value == null)
				throw __ArgumentNullValue.Create(name);
		}

		internal static void IfSavingThrow<T>(ContainerAbstract<T> container) where T : ContainerEntry {
			try {
				var t = container.Commands;
			}
			catch {
				throw __ContainerClosed.Create();
			}

			if (container.IsBusy || container.Commands.IsLocked)
				throw __ContainerBusy.Create();
		}

		internal static void IfTrueThrowContainerSaving(bool value) {
			if (value)
				throw __ContainerSaving.Create();
		}

		internal static void ThrowUnsupportedFileFormat(string value) {
			throw __UnsupportedFileFormat.Create();
		}

		internal static void IfNullThrowNonLoadedImage(object value) {
			if (value == null)
				throw __NonLoadedImage.Create();
		}

		internal static void IfLtZeroThrowUnsupportedImageFormat(int value) {
			if (value < 0)
				throw __UnsupportedImageFormat.Create();
		}

		internal static void IfLtZeroThrowInvalidImagePosition(string name, int value) {
			if (value < 0)
				throw __InvalidImagePosition.Create(name, value);
		}

		public static void IfOutOfRangeThrow(float tolerance, string name, float minInclude, float maxInclude) {
			if (tolerance < minInclude)
				throw __ArgumentOutOfRangeMin.Create(name, minInclude);

			if (tolerance > maxInclude)
				throw __ArgumentOutOfRangeMax.Create(name, maxInclude);
		}

		public static void IfEncryptionCheckFlagThrow(GrfHolder grf) {
			if (grf != null && grf.IsOpened && grf.Header.EncryptionCheckFlag) {
				throw __EncryptionCheckFlagInProgress.Create();
			}
		}

		internal static void IfEncryptionCheckFlagThrow(Container grf) {
			if (grf != null && grf.InternalHeader.EncryptionCheckFlag) {
				throw __EncryptionCheckFlagInProgress.Create();
			}
		}
	}

	public class FormattedExceptionMessage {
		private static int _errorId;

		private readonly int _id;
		private readonly string _message;

		internal FormattedExceptionMessage(string message) {
			_message = message;
			_errorId++;
			_id = _errorId;
		}

		public string Message => _message;
		protected bool Equals(FormattedExceptionMessage other) => _id == other._id;
		public override int GetHashCode() => _id;

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
			return new GrfException(this, String.Format(Message, items));
		}
	}

	public class GrfException : Exception {
		private readonly FormattedExceptionMessage _format;

		public GrfException(FormattedExceptionMessage format, string message) : base(message) {
			_format = format;
		}

		public FormattedExceptionMessage Format => _format;

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

		public static bool operator ==(GrfException exp1, FormattedExceptionMessage exp2) {
			if (ReferenceEquals(exp1, null) && ReferenceEquals(exp2, null)) return true;
			if (ReferenceEquals(exp1, null) || ReferenceEquals(exp2, null)) return false;

			return exp1.Equals(exp2);
		}

		public static bool operator ==(FormattedExceptionMessage exp1, GrfException exp2) {
			if (ReferenceEquals(exp1, null) && ReferenceEquals(exp2, null)) return true;
			if (ReferenceEquals(exp1, null) || ReferenceEquals(exp2, null)) return false;

			return exp1.Equals(exp2);
		}

		public static bool operator !=(GrfException exp1, FormattedExceptionMessage exp2) => !(exp1 == exp2);
		public static bool operator !=(GrfException exp1, GrfException exp2) => !(exp1 == exp2);
		public static bool operator !=(FormattedExceptionMessage exp1, GrfException exp2) => !(exp1 == exp2);
	}
}