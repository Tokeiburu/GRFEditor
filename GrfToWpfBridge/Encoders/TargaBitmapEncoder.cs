using System;
using System.IO;
using GRF.Image;

namespace GrfToWpfBridge.Encoders {
	public sealed class TargaBitmapEncoder : IWpfEncoder {
		#region IWpfEncoder Members

		public GrfImage Frame { get; set; }

		public void Save(Stream stream) {
			byte[] header = new byte[] { 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, (byte) (Frame.GetBpp() * 8), 0x08 };
			Buffer.BlockCopy(BitConverter.GetBytes((ushort) Frame.Width), 0, header, 12, 2);
			Buffer.BlockCopy(BitConverter.GetBytes((ushort) Frame.Height), 0, header, 14, 2);

			stream.Write(header, 0, 18);

			byte[] image = Frame.Pixels;
			byte[] realImage = new byte[image.Length];

			int stride = Frame.Width * Frame.GetBpp();

			for (int i = 0; i < Frame.Height; i++) {
				Buffer.BlockCopy(image, (Frame.Height - i - 1) * stride, realImage, i * stride, stride);
			}

			stream.Write(realImage, 0, realImage.Length);
		}

		#endregion
	}
}