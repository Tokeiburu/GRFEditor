using System;

namespace GRF.FileFormats.GatFormat {
	public enum GatPreviewFormat {
		GrayBlock,
		Heightmap,
		LightAndShadow,
		Light,
		Shadow,
		Texture,
	}

	[Flags]
	public enum GatPreviewOptions {
		Rescale = 1 << 0,
		HideBorders = 1 << 1,
		Transparent = 1 << 2,
	}
}