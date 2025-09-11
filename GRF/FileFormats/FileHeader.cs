using System;
using Utilities;

namespace GRF.FileFormats {
	public abstract class FileHeader {
		/// <summary>
		/// Gets or sets the magic (the file unique identifier).
		/// </summary>
		public string Magic { get; internal set; }

		/// <summary>
		/// Gets or sets the major version.
		/// </summary>
		public byte MajorVersion {
			get { return _majorVersion; }
			protected set {
				_majorVersion = value;
				_version = FormatConverters.DoubleConverter(MajorVersion + "." + MinorVersion);
			}
		}

		/// <summary>
		/// Gets or sets the minor version.
		/// </summary>
		public byte MinorVersion {
			get { return _minorVersion; }
			protected set {
				_minorVersion = value;
				_version = FormatConverters.DoubleConverter(MajorVersion + "." + MinorVersion);
			}
		}

		public double Version {
			get {
				if (_version == null) {
					_version = FormatConverters.DoubleConverter(MajorVersion + "." + MinorVersion);
				}
				
				return _version.Value;
			}
		}

		private byte _minorVersion;
		private byte _majorVersion;
		private double? _version = null;
		public int BuildNumber { get; set; }
		public byte UnknownData { get; protected set; }

		/// <summary>
		/// Gets the version in the hex format.
		/// </summary>
		public string HexVersionFormat {
			get { return String.Format("0x{0:X}", (MajorVersion << 8) + MinorVersion); }
		}

		/// <summary>
		/// Determines whether the object is compatible with the version specified.
		/// </summary>
		/// <param name="major">The major id.</param>
		/// <param name="minor">The minor id.</param>
		/// <returns>
		///   <c>true</c> if is compatible with the version specified; otherwise, <c>false</c>.
		/// </returns>
		public bool IsCompatibleWith(int major, int minor) {
			return ((major == MajorVersion && MinorVersion >= minor) ||
			        MajorVersion > major);
		}

		/// <summary>
		/// Determines whether the object is the major version specified.
		/// </summary>
		/// <param name="major">The major id.</param>
		/// <returns>
		///   <c>true</c> if is compatible with the version specified; otherwise, <c>false</c>.
		/// </returns>
		public bool IsMajorVersion(int major) {
			return MajorVersion == major;
		}

		/// <summary>
		/// Determines whether the object is the version specified.
		/// </summary>
		/// <param name="major">The major id.</param>
		/// <param name="minor">The minor id.</param>
		/// <returns>
		///   <c>true</c> if is compatible with the version specified; otherwise, <c>false</c>.
		/// </returns>
		public bool Is(int major, int minor) {
			return (major == MajorVersion && minor == MinorVersion);
		}

		/// <summary>
		/// Determines whether the object is not compatible with version specified.
		/// </summary>
		/// <param name="major">The major id.</param>
		/// <param name="minor">The minor id.</param>
		/// <returns>
		///   <c>true</c> if is not compatible with the version specified; otherwise, <c>false</c>.
		/// </returns>
		public bool IsNotCompatibleWith(int major, int minor) {
			return !IsCompatibleWith(major, minor);
		}

		/// <summary>
		/// Sets the version.
		/// </summary>
		/// <param name="major">The major id.</param>
		/// <param name="minor">The minor id.</param>
		public virtual void SetVersion(byte major, byte minor) {
			MajorVersion = major;
			MinorVersion = minor;
		}

		public override string ToString() {
			return String.Format("Magic = {0}; Version = {1}", Magic, HexVersionFormat);
		}
	}
}