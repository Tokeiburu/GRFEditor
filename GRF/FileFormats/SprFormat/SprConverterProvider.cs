using ErrorManager;

namespace GRF.FileFormats.SprFormat {
	public static class SprConverterProvider {
		public static ISprConverter GetConverter(SprHeader header) {
			if (header.Is(2, 1)) {
				return new SprConverterV2M1(header);
			}
			if (header.Is(2, 0)) {
				return new SprConverterV2M0(header);
			}
			if (header.Is(1, 0)) {
				return new SprConverterV1M0(header);
			}

			// Haven't seen other versions yet... we'll assume V2M1 is good enough (it's the most stable)!
			ErrorHandler.HandleException("Unsupported format, attempting SprConverterV2M1 : Major = " + header.MajorVersion + " Minor = " + header.MinorVersion, ErrorLevel.Low);
			return new SprConverterV2M1(header);
		}

		public static ISprConverter GetConverter(byte major, byte minor) {
			SprHeader header = new SprHeader();
			header.SetVersion(major, minor);
			return GetConverter(header);
		}
	}
}