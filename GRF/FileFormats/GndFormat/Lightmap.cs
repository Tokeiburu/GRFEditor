using System;
using GRF.Image;
using Utilities;

namespace GRF.FileFormats.GndFormat {
	/// <summary>
	/// Represents a lightmap for a tile
	/// </summary>
	public static class Lightmap {
		/// <summary>
		/// Gets the <see cref="GrfColor" /> at the specified index of the lightmap.
		/// </summary>
		/// <param name="data">The lightmap data.</param>
		/// <param name="index">The index.</param>
		/// <returns>The color at the specified index.</returns>
		public static GrfColor GetColor(byte[] data, int index) {
			int b = data.Length >> 2;
			b = 3 * index + b;
			return new GrfColor(index, data[b], data[b + 1], data[b + 2]);
		}

		public static int GetColorCount(byte[] data) {
			return data.Length >> 2;
		}

		public static int GetHash(byte[] data, Gnd gnd) {
			const uint poly = 0x82f63b78;

			long crc = ~0;
			int size = gnd.LightmapWidth * gnd.LightmapHeight * 4;
			if (size < 4)
				return 0;
			for (int i = 0; i < size; i++) {
				crc ^= data[i];
				crc = (crc & 1) == 1 ? (crc >> 1) ^ poly : crc >> 1;
			}
			return (int)~crc;
		}

		public static bool IsEqual(byte[] a, byte[] b) {
			return NativeMethods.memcmp(a, b, Math.Max(a.Length, b.Length)) == 0;
		}
	}
}