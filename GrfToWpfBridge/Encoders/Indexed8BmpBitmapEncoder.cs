using System;
using System.IO;
using GRF.Image;

namespace GrfToWpfBridge.Encoders {
	public class Indexed8BmpBitmapEncoder : IWpfEncoder {
		#region IWpfEncoder Members

		public GrfImage Frame { get; set; }

		public void Save(Stream stream) {
			Save(new BinaryWriter(stream), Frame.Pixels, Frame.Palette, Frame.Width, Frame.Height);
		}

		#endregion

		public void Save(BinaryWriter writer, byte[] pixels, byte[] palette, int width, int height) {
			Imaging.RgbaToBgra(palette);

			BmpHeader header = new BmpHeader();
			header.DibHeader.Size = 40;
			header.DibHeader.Planes = 1;
			header.DibHeader.Compression = 0;
			header.DibHeader.Vres = 2834;
			header.DibHeader.Hres = 2834;

			int fakeWidth = width;

			while (fakeWidth % 4 != 0)
				fakeWidth++;

			header.DibHeader.Width = width;
			header.DibHeader.Height = height;
			header.DibHeader.Bpp = 8;
			header.DibHeader.ColorCount = 256;
			header.DibHeader.ImportantColors = 0;
			header.DibHeader.DataSize = (uint) (fakeWidth * height);
			header.FileHeader.Offset = 54 + 256 * 4;
			header.FileHeader.Size = header.FileHeader.Offset + header.DibHeader.DataSize;

			writer.Write(header.FileHeader.Magic);
			writer.Write(header.FileHeader.Size);
			writer.Write(header.FileHeader.Reserved1);
			writer.Write(header.FileHeader.Reserved2);
			writer.Write(header.FileHeader.Offset);
			writer.Write(header.DibHeader.Size);
			writer.Write(header.DibHeader.Width);
			writer.Write(header.DibHeader.Height);
			writer.Write(header.DibHeader.Planes);
			writer.Write(header.DibHeader.Bpp);
			writer.Write(header.DibHeader.Compression);
			writer.Write(header.DibHeader.DataSize);
			writer.Write(header.DibHeader.Hres);
			writer.Write(header.DibHeader.Vres);
			writer.Write(header.DibHeader.ColorCount);
			writer.Write(header.DibHeader.ImportantColors);

			writer.Write(palette);

			byte[] data = new byte[header.DibHeader.DataSize];

			for (uint y = 0; y < height; y++) {
				Buffer.BlockCopy(pixels, (int) ((height - y - 1) * width), data, (int) (y * fakeWidth), width);
			}

			writer.Write(data);
		}

		#region Nested type: BmpFileHeader

		public class BmpFileHeader {
			public byte[] Magic = new byte[] { 0x42, 0x4d };
			public uint Offset;
			public ushort Reserved1;
			public ushort Reserved2;
			public uint Size;
		}

		#endregion

		#region Nested type: BmpHeader

		public class BmpHeader {
			public DibHeader DibHeader = new DibHeader();
			public BmpFileHeader FileHeader = new BmpFileHeader();
		}

		#endregion

		#region Nested type: DibHeader

		public class DibHeader {
			public ushort Bpp;
			public uint ColorCount;
			public uint Compression;
			public uint DataSize;
			public int Height;
			public uint Hres;
			public uint ImportantColors;
			public ushort Planes;
			public uint Size;
			public uint Vres;
			public int Width;
		}

		#endregion
	}
}