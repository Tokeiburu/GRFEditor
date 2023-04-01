using System;
using System.IO;
using Utilities.Extension;

namespace Utilities {
	public class TkPath {
		private bool _isModified;
		private string _relativePath;
		private string _filePath;
		private string _fullPath;

		public string FileName {
			get { return Path.GetFileName(GetMostRelative()); }
		}

		public TkPath() { }

		public TkPath(TkPath path) {
			RelativePath = path.RelativePath;
			FilePath = path.FilePath;
		}

		public TkPath(string filePath, string relativePath) {
			FilePath = filePath;
			RelativePath = relativePath;
		}

		public TkPath(string path) {
			if (path.Contains("?")) {
				FilePath = path.Split('?')[0];
				RelativePath = path.Split('?')[1];

				if (FilePath == "") {
					FilePath = RelativePath;
					RelativePath = "";
				}
			}
			else {
				FilePath = path;
			}
		}

		/// <summary>
		/// Gets or sets the relative path. The relative path is the path inside the file
		/// container. If null, this represents a file.
		/// Ex: ...?path\to\relative.path
		/// </summary>
		/// <value>
		/// The relative path.
		/// </value>
		public string RelativePath {
			get { return _relativePath; }
			set {
				if (_relativePath != value) {
					_isModified = true;
				}

				_relativePath = value;
			}
		}

		/// <summary>
		/// Gets or sets the file path.
		/// Ex: path\to\file.path?...
		/// </summary>
		/// <value>
		/// The file path.
		/// </value>
		public string FilePath {
			get { return _filePath; }
			set {
				if (_filePath != value) {
					_isModified = true;
				}

				_filePath = value;
			}
		}

		public bool IsFolder {
			get {
				return String.IsNullOrEmpty(RelativePath) && Directory.Exists(FilePath);
			}
		}

		public bool IsFile {
			get {
				return String.IsNullOrEmpty(RelativePath) && File.Exists(FilePath);
			}
		}

		public bool IsContainer {
			get {
				return FilePath.IsExtension(".grf", ".gpf", ".thor", ".rgz");
			}
		}

		public static implicit operator string(TkPath path) {
			return path == null ? null : path.GetFullPath();
		}

		public static implicit operator TkPath(string path) {
			return path == null ? null : new TkPath(path);
		}

		/// <summary>
		/// Gets the full path.
		/// Ex: my\file.path or my\server.grf?path\to\relative.path
		/// </summary>
		/// <returns>Full path</returns>
		public string GetFullPath() {
			if (_isModified) {
				_fullPath = FilePath + (String.IsNullOrEmpty(RelativePath) ? "" : "?" + RelativePath);
				_isModified = false;
			}

			return _fullPath;
		}

		/// <summary>
		/// Gets the most relative path.
		/// </summary>
		/// <returns>Most relative path</returns>
		public string GetMostRelative() {
			return RelativePath ?? FilePath;
		}

		public override string ToString() {
			return GetFullPath();
		}

		protected bool Equals(TkPath other) {
			return string.Equals(RelativePath, other.RelativePath) && string.Equals(FilePath, other.FilePath);
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((TkPath) obj);
		}

		public override int GetHashCode() {
			unchecked {
				return GetFullPath().GetHashCode();
			}
		}

		public void CreateDirectory() {
			if (FilePath != null) {
				var dir = Path.GetDirectoryName(FilePath);

				if (dir != null && !Directory.Exists(dir)) 
					Directory.CreateDirectory(dir);
			}
		}
	}
}
