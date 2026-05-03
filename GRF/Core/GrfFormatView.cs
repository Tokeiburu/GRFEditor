using GRF.ContainerFormat;
using System;
using System.Collections.Generic;

namespace GRF.Core {
	public static class GrfFormatViews {
		public static List<GrfFormatView> Formats = new List<GrfFormatView>();

		public static GrfFormatView Grf102 = new GrfFormatView(1, 2);
		public static GrfFormatView Grf103 = new GrfFormatView(1, 3);
		public static GrfFormatView Grf200 = new GrfFormatView(2, 0);
		public static GrfFormatView Grf300 = new GrfFormatView(3, 0);

		public static GrfFormatView Get(byte majorVersion, byte minorVersion) {
			switch (majorVersion) {
				case 1:
					switch (minorVersion) {
						case 2:
							return Grf102;
						case 3:
							return Grf103;
					}
					break;
				case 2:
					return Grf200;
				case 3:
					return Grf300;
			}

			throw GrfExceptions.__UnsupportedFileVersion.Create();
		}
	}

	public class GrfFormatView {
		public byte Major;
		public byte Minor;

		public GrfFormatView(byte major, byte minor) {
			Major = major;
			Minor = minor;

			GrfFormatViews.Formats.Add(this);
		}

		public override string ToString() {
			return String.Format("0x{0}{1:00}", Major, Minor);
		}
	}
}
