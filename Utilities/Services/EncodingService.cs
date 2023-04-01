using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using ErrorManager;
using Utilities.Extension;

namespace Utilities.Services {
	public class EncodingView : INotifyPropertyChanged {
		private string _friendlyName;
		public string FriendlyName {
			get { return _friendlyName; }
			set {
				_friendlyName = value;
				OnPropertyChanged("FriendlyName");
			}
		}

		public Encoding Encoding { get; set; }

		public override string ToString() {
			return FriendlyName;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// This service manages the encoding, it has some basic
	/// detection features to know if a string is in the correct
	/// format or not.
	/// </summary>
	public class EncodingService {
		public static Encoding Korean {
			get {
				try {
					return Encoding.GetEncoding(949);
				}
				catch {
					return null;
				}
			}
		}
		public static Encoding Ansi {
			get {
				try {
					return Encoding.GetEncoding(1252);
				}
				catch {
					return null;
				}
			}
		}
		public static Encoding ANSI {		// Compatibility with the Encryption file
			get {
				try {
					return Encoding.GetEncoding(1252);
				}
				catch {
					return null;
				}
			}
		}
		public static Encoding Utf8 {
			get {
				try {
					return Encoding.GetEncoding(65001);
				}
				catch {
					return null;
				}
			}
		}
		public static Encoding Chinese {
			get {
				try {
					return Encoding.GetEncoding(936);
				}
				catch {
					return null;
				}
			}
		}
		public static Encoding Japanese {
			get {
				try {
					return Encoding.GetEncoding(932);
				}
				catch {
					return null;
				}
			}
		}
		public static Encoding Cyrillic {
			get {
				try {
					return Encoding.GetEncoding(1251);
				}
				catch {
					return null;
				}
			}
		}

		public static List<EncodingView> GetKnownEncodings() {
			try {
				return new List<EncodingView> {
					new EncodingView { Encoding = Ansi, FriendlyName = "Default (1252 - Western European)" },
					new EncodingView { Encoding = Korean, FriendlyName = "Korean (949)" },
					new EncodingView { Encoding = Encoding.GetEncoding(1251), FriendlyName = "Cyrillic (1251)" },
					new EncodingView { Encoding = Encoding.GetEncoding(932), FriendlyName = "Japanese (932)" },
					new EncodingView { Encoding = Encoding.GetEncoding(936), FriendlyName = "Chinese Simplified (936 - GB2312)" },
					new EncodingView { Encoding = null, FriendlyName = "Other..." }
				};
			}
			catch {
				return new List<EncodingView> {
					new EncodingView { Encoding = Ansi, FriendlyName = "Default (1252 - Western European)" },
					new EncodingView { Encoding = Korean, FriendlyName = "Korean (949)" },
					new EncodingView { Encoding = null, FriendlyName = "Other..." }
				};
			}
		}

		private static bool _hasBeenAdvised;
		private static Encoding _displayEncoding;
		private static Encoding _oldDisplayEncoding;

		static EncodingService() {
			DisplayEncoding = Encoding.Default;
		}

		public static Encoding DisplayEncoding {
			get { return _displayEncoding; }
			set {
				Encoding old = _oldDisplayEncoding;

				_oldDisplayEncoding = _displayEncoding;
				_displayEncoding = value;

				if (old != null && _oldDisplayEncoding.CodePage == _displayEncoding.CodePage) {
					_oldDisplayEncoding = old;
				}
			}
		}

		public static Encoding GetOldDisplayEncoding() {
			return _oldDisplayEncoding;
		}

		private static bool _validateEncoding(string file, Encoding encoding) {
			return encoding.GetString(Ansi.GetBytes(GetAnsiString(file, encoding))) == file;
		}

		/// <summary>
		/// Corrects the file name ONLY, the directory name will not be modified.
		/// This should be used only when the folder doesn't matter, such
		/// as when you're adding a single file in the GRF.
		/// 
		/// This method guesses the encoding and it should be
		/// used carefully (if you know the encoding, use
		/// the appropriate method).
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <param name="quiet">if set to <c>true</c> [quiet].</param>
		/// <returns></returns>
		public static string CorrectFileName(string fileName, bool quiet = false) {
			if (fileName == null) return null;

			string path = Path.GetDirectoryName(fileName);
			string file = Path.GetFileName(fileName);

			if (_validateEncoding(file, DisplayEncoding)) {
				return fileName;
			}

			string newFile = _fromAnyToDisplayEncoding(file.ExpandString());
			if (!_hasBeenAdvised && !quiet) {
				_hasBeenAdvised = true;
				ErrorHandler.HandleException(
					"The file name is in the wrong encoding format. Here's the automatic translation result." +
					"\nOriginal : " + file +
					"\nNew : " + newFile +
					"\n\nYou can undo this operation if you think it's wrong (you will not be notified about " +
					"similar issues again).", ErrorLevel.Low);
			}

			return Path.Combine(path ?? "", newFile);
		}

		public static List<string> CorrectFileNames(List<string> fileNames) {
			return fileNames.Select(p => CorrectFileName(p)).ToList();
		}

		/// <summary>
		/// Corrects the full path (so the entire string will be
		/// taken into account). This method is equivalent to
		/// ConvertStringToCurrentEncoding(), except this one
		/// can trigger a pop up.
		/// 
		/// This method guesses the encoding and it should be
		/// used carefully (if you know the encoding, use
		/// the appropriate method).
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="quiet">if set to <c>true</c> [quiet].</param>
		/// <returns></returns>
		public static string CorrectPathExplode(string path, bool quiet = false) {
			if (path == null) return null;

			string newPath = FromAnyToDisplayEncoding(path);

			if (newPath == path)
				return path;

			if (!_hasBeenAdvised && !quiet) {
				ErrorHandler.HandleException(
					"The path name is in the wrong encoding format. Here's the automatic translation result." +
					"\nOriginal : " + path +
					"\nNew : " + newPath +
					"\n\nYou can undo this operation if you think it's wrong (you will not be notified about " +
					"similar issues again).", ErrorLevel.Low);
				_hasBeenAdvised = true;
			}

			return newPath;
		}

		/// <summary>
		/// Gets the ANSI string. If non-ANSI characters are
		/// detected, the encoding used to decode the chars
		/// will be the DisplayEncoding.
		/// 
		/// This method should NOT be used for dropped files or
		/// file names with unknown encoding.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		public static string GetAnsiString(string fileName) {
			return _isAnsi(fileName) ? fileName : Ansi.GetString(DisplayEncoding.GetBytes(fileName));
		}

		public static string GetAnsiString(string fileName, Encoding encoding) {
			return _isAnsi(fileName) ? fileName : Ansi.GetString(encoding.GetBytes(fileName));
		}

		public static string GetKoreanString(string fileName) {
			return _isKorean(fileName) ? fileName : Korean.GetString(DisplayEncoding.GetBytes(fileName));
		}

		/// <summary>
		/// Gets the current string. This method is similar to
		/// GetANSIString except that it returns the string
		/// based upon the DisplayEncoding.
		/// 
		/// This method should NOT be used for dropped files or
		/// file names with unknown encoding.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		public static string GetCurrentString(string fileName) {
			return _isAnsi(fileName) ? DisplayEncoding.GetString(Ansi.GetBytes(fileName)) : fileName;
		}

		public static string FromAnyToDisplayEncoding(string fullPath) {
			return FromAnyToDisplayEncoding(fullPath, false);
		}

		public static string FromAnyToDisplayEncoding(string fullPath, bool expand) {
			if (expand)
				fullPath = fullPath.ExpandString();
			return string.Join("\\", fullPath.Split('\\').Select(_fromAnyToDisplayEncoding).ToArray());
		}

		public static string FromAnyTo(string fullPath, Encoding destination) {
			return string.Join("\\", fullPath.Split('\\').Select(p => _fromAnyTo(p, destination)).ToArray());
		}

		public static string FromAnsiToDisplayEncoding(string file) {
			return DisplayEncoding.CodePage == Ansi.CodePage ? GetAnsiString(file) : GetCurrentString(file);
		}

		/// <summary>
		/// Converts the string to the current encoding.
		/// 
		/// This method guesses the encoding. Be careful.
		/// 
		/// This method SHOULD be used for dropped files or
		/// file names with unknown encoding.
		/// </summary>
		/// <param name="file">The file.</param>
		/// <returns></returns>
		private static string _fromAnyToDisplayEncoding(string file) {
			// We try to detect the current encoding of the file name!
			Encoding encoding = _getEncoding(file);

			if (DisplayEncoding == Utf8 && encoding == Ansi) {
				return file;
			}

			if (encoding != null) {
				return DisplayEncoding.GetString(encoding.GetBytes(file));
			}

			return GetCurrentString(file);
		}

		private static string _fromAnyTo(string file, Encoding destination) {
			// We try to detect the current encoding of the file name!
			Encoding encoding = _getEncoding(file);

			if (DisplayEncoding == Utf8 && encoding == Ansi) {
				return file;
			}

			if (encoding == null) {
				if (_validateEncoding(file, destination)) {
					return file;
				}
			}

			if (encoding != null) {
				return destination.GetString(encoding.GetBytes(file));
			}

			return GetCurrentString(file);
		}

		private static Encoding _getEncoding(string file) {
			if (_validateEncoding(file, DisplayEncoding)) {
				return DisplayEncoding;
			}

			if (_validateEncoding(file, Ansi)) {
				return Ansi;
			}

			if (_validateEncoding(file, Korean)) {
				return Korean;
			}

			if (_oldDisplayEncoding != null && _validateEncoding(file, _oldDisplayEncoding)) {
				return _oldDisplayEncoding;
			}

			if (_validateEncoding(file, Chinese)) {
				return Chinese;
			}

			if (_validateEncoding(file, Japanese)) {
				return Japanese;
			}

			if (_validateEncoding(file, Cyrillic)) {
				return Cyrillic;
			}

			return null;// DisplayEncoding.CodePage == Ansi.CodePage ? GetAnsiString(file) : GetCurrentString(file);
		}

		public static string ConvertStringToAnsi(string file) {
			// We try to detect the current encoding of the file name!
			Encoding encoding = _getEncoding(file);

			if (encoding != null) {
				return Ansi.GetString(encoding.GetBytes(file));
			}

			return GetAnsiString(file);
		}

		public static string ConvertStringToKorean(string file) {
			// We try to detect the current encoding of the file name!
			Encoding encoding = _getEncoding(file);

			if (encoding != null) {
				return Korean.GetString(encoding.GetBytes(file));
			}

			return GetKoreanString(file);
		}

		/// <summary>
		/// Checks if the string contains only ANSI characters
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		/// <returns></returns>
		private static bool _isAnsi(string fileName) {
			return fileName == Ansi.GetString(Ansi.GetBytes(fileName));
		}

		private static bool _isKorean(string fileName) {
			return fileName == Korean.GetString(Korean.GetBytes(fileName));
		}

		public static bool EncodingExists(string input) {
			try {
				Encoding.GetEncoding(Int32.Parse(input));
				return true;
			}
			catch {
				return false;
			}
		}
		public static bool EncodingExists(int input) {
			try {
				Encoding.GetEncoding(input);
				return true;
			}
			catch {
				return false;
			}
		}

		public static bool SetDisplayEncoding(int input) {
			try {
				DisplayEncoding = Encoding.GetEncoding(input);
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Low);
				DisplayEncoding = Encoding.Default;
				return false;
			}
		}

		public static bool IsGrfCompatible(string input) {
			return !CorrectPathExplode(input).Contains("?");
		}

		public static bool IsCompatible(string input, Encoding encoding) {
			return DisplayEncoding.GetString(encoding.GetBytes(encoding.GetString(DisplayEncoding.GetBytes(input)))) == input;
		}

		public static Encoding DetectEncoding(string outputPath) {
			if (_isEncoding(outputPath, Ansi))
				return Ansi;

			if (_isEncoding(outputPath, Korean))
				return Korean;

			if (_isEncoding(outputPath, Utf8))
				return Utf8;

			if (_isEncoding(outputPath, DisplayEncoding))
				return DisplayEncoding;

			return null;
		}

		public static Encoding DetectEncoding(byte[] data) {
			if (_isEncoding(data, Ansi))
				return Ansi;

			if (_isEncoding(data, Korean))
				return Korean;

			if (_isEncoding(data, Utf8))
				return Utf8;

			if (_isEncoding(data, DisplayEncoding))
				return DisplayEncoding;

			return null;
		}

		private static bool _isEncoding(string outputPath, Encoding encoding) {
			bool valid = true;
			int count = 0;
			using (StreamReader reader = new StreamReader(outputPath, encoding)) {
				while (!reader.EndOfStream && count < 100) {
					string line = reader.ReadLine();
					if (line == null)
						break;

					if (line != encoding.GetString(encoding.GetBytes(line))) {
						valid = false;
						break;
					}

					count++;
				}
			}

			return valid;
		}

		private static bool _isEncoding(byte[] data, Encoding encoding) {
			bool valid = true;
			int count = 0;
			using (StreamReader reader = new StreamReader(new MemoryStream(data), encoding)) {
				while (!reader.EndOfStream && count < 100) {
					string line = reader.ReadLine();
					if (line == null)
						break;

					if (line != encoding.GetString(encoding.GetBytes(line))) {
						valid = false;
						break;
					}

					count++;
				}
			}

			return valid;
		}
	}
}
