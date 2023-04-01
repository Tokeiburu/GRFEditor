using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using GRF.Image;
using Utilities;

namespace GRF.FileFormats.IcoFormat {
	//public class IconDirEntry {
	//    public int Width { get; set; }
	//    public int Height { get; set; }
	//    public byte ColorPaletteCount { get; set; }
	//    public byte Reserved { get; set; }
	//    public UInt16 ColorPlanes { get; set; }
	//    public UInt16 BitsPerPixel { get; set; }
	//    public int ImageDataSize { get; set; }
	//    public int ImageDataOffset { get; set; }

	//    public IconDirEntry(IBinaryReader reader) {
	//        Width = reader.Byte();
	//        Height = reader.Byte();

	//        if (Width == 0)
	//            Width = 256;

	//        if (Height == 0)
	//            Height = 256;

	//        ColorPaletteCount = reader.Byte();
	//        Reserved = reader.Byte();
	//        ColorPlanes = reader.UInt16();
	//        BitsPerPixel = reader.UInt16();
	//        ImageDataSize = reader.Int32();
	//        ImageDataOffset = reader.Int32();
	//    }
	//}

	public class BitmapFileHeader {
		public string Magic { get; private set; }
		public BitmapInformationHeader DibHeader { get; private set; }
		public uint BmpFileSize { get; private set; }
		public uint BitmapDataOffset { get; private set; }

		public BitmapFileHeader(IBinaryReader reader) {
			Magic = reader.StringANSI(2);
			BmpFileSize = reader.UInt32();
			reader.Forward(4);
			BitmapDataOffset = reader.UInt32();
			DibHeader = new BitmapInformationHeader(reader);
		}
	}

	public class BitmapInformationHeader {
		public uint Size { get; set; }
		public uint Width { get; set; }
		public uint Height { get; set; }
		public UInt16 Planes { get; set; }
		public UInt16 BitCount { get; set; }
		public uint Compression { get; set; }
		public uint SizeImage { get; set; }
		public uint XPelsPerMeter { get; set; }
		public uint YPelsPerMeter { get; set; }
		public uint ColorTableUsed { get; set; }
		public uint ColorTableImportant { get; set; }

		public byte RBitmask { get; set; }
		public byte GBitmask { get; set; }
		public byte BBitmask { get; set; }
		public byte ABitmask { get; set; }

		public BitmapInformationHeader(IBinaryReader reader) {
			Size = reader.UInt32();

			if (Size <= 40) {
				Width = reader.UInt32();
				Height = reader.UInt32();
				Planes = reader.UInt16();
				BitCount = reader.UInt16();
				Compression = reader.UInt32();
				SizeImage = reader.UInt32();
				XPelsPerMeter = reader.UInt32();
				YPelsPerMeter = reader.UInt32();
				ColorTableUsed = reader.UInt32();
				ColorTableImportant = reader.UInt32();
			}

			reader.Forward((int) (Size - 40));
		}

		public BitmapInformationHeader(GrfImage image) {
			Width = (uint)(image.Width % 255);
			Height = (uint)(image.Width % 255);

			if (image.GrfImageType == GrfImageType.Indexed8) {
				ColorTableUsed = (uint)(image.Palette.Length / 8);
				ColorTableImportant = (uint)(image.Palette.Length / 8);
			}
		}
	}

	//public class Dib : IImageable {
	//    public BitmapInformationHeader Header { get; set; }

	//    public Dib(ByteReader reader) {
	//        Header = new BitmapInformationHeader(reader);

	//        int bpp = Header.BitCount >> 3;
	//        long rowIncrement = entry.Width * bpp;
	//        long length = entry.Width * entry.Height * bpp;

	//        byte[] rData = new byte[length];
	//        int padding = 0;

	//        if (entry.Width % 4 != 0) {
	//            padding = (4 - (entry.Width % 4)) * bpp;
	//            padding = (4 - ((3 * entry.Width) % 4)) % 4;
	//        }

	//        for (int y = 0; y < entry.Height; y++) {
	//            int offset = (int)((entry.Height - y - 1) * rowIncrement);
	//            reader.Bytes(rData, offset, entry.Width * bpp);
	//            reader.Forward(padding);
	//        }

	//        GrfImageType type;

	//        if (bpp == 4) {
	//            type = GrfImageType.Bgra32;
	//        }
	//        else if (bpp == 3) {
	//            type = GrfImageType.Bgr24;
	//        }
	//        else {
	//            type = GrfImageType.Indexed8;
	//        }

	//        Image = new GrfImage(rData, entry.Width, entry.Height, type);
	//    }

	//    public GrfImage Image { get; set; }
	//}

	//public class Ico {
	//    private List<GrfImage> _images = new List<GrfImage>();
	//    public string Magic { get; private set; }
	//    public IcoType IconType { get; private set; }

	//    public Ico(MultiType data)
	//        : this(data.GetBinaryReader()) {

	//    }

	//    private Ico(IBinaryReader reader) {
	//        Magic = reader.StringANSI(2);

	//        if (Magic != "\x00\x00")
	//            GrfExceptions.ThrowFileFormatException("ICO");

	//        IconType = (IcoType)reader.UInt16();

	//        int count = reader.UInt16();

	//        List<IconDirEntry> icoImages = new List<IconDirEntry>(count);

	//        for (int i = 0; i < count; i++) {
	//            icoImages.Add(new IconDirEntry(reader));
	//        }

	//        for (int i = 0; i < count; i++) {
	//            IconDirEntry entry = icoImages[i];

	//            reader.Position = entry.ImageDataOffset;

	//            byte[] imData = reader.Bytes(entry.ImageDataSize);

	//            if (Methods.ByteArrayCompare(GrfImage.PngHeader, 0, 4, imData, 0)) {
	//                _images.Add(new GrfImage(imData));
	//            }
	//            else {
	//                // Extract raw information
	//                Dib dib = new Dib(new ByteReader(imData), entry);
	//                _images.Add(dib.Image);
	//            }
	//        }
	//    }

	//    public List<GrfImage> Images {
	//        get { return _images; }
	//        set { _images = value; }
	//    }

	//    public void Save(string path) {
	//        using (var writer = new BinaryWriter(File.Create(path))) {
	//            writer.Write((short)0);
	//            writer.Write((short)IconType);
	//            writer.Write((UInt16)Images.Count);

	//            int dataOffset = Images.Count * 16 + 6;

	//            List<byte[]> dataArrays = new List<byte[]>();

	//            for (int i = 0; i < Images.Count; i++) {
	//                var image = Images[i];

	//                int width = image.Width;

	//                if (width == 256) {
	//                    width = 0;
	//                }
	//                else if (width > 256) {
	//                    throw new Exception("Width too high");
	//                }

	//                int height = image.Height;

	//                if (height == 256) {
	//                    height = 0;
	//                }
	//                else if (height > 256) {
	//                    throw new Exception("Height too high");
	//                }

	//                //writer.Write((byte)width);

	//                if (i == 0) {
	//                    writer.Write((byte)0);
	//                    writer.Write((byte)0);
	//                }
	//                else {
	//                    writer.Write((byte)width);
	//                    writer.Write((byte)height);
	//                }
	//                //if (i > 0)
	//                //	writer.Write((byte)(height * 2));
	//                //else
	//                //	writer.Write((byte)height);

	//                writer.Write((byte)0);
	//                writer.Write((byte)0);
	//                writer.Write((UInt16)1);
	//                writer.Write((UInt16)(image.GetBpp() << 3));

	//                byte[] dataImg;

	//                //if (i == 0) {
	//                image.Save("C:\\test.png");
	//                dataImg = File.ReadAllBytes("C:\\test.png");
	//                //}
	//                //else {
	//                //	// Save the imge as a DIB
	//                //	image.Save("C:\\test.bmp", PixelFormatInfo.BmpBgra32);
	//                //	byte[] temp = File.ReadAllBytes("C:\\test.bmp");
	//                //	dataImg = new byte[temp.Length - 14];
	//                //	Buffer.BlockCopy(temp, 14, dataImg, 0, dataImg.Length);
	//                //	Buffer.BlockCopy(BitConverter.GetBytes(image.Height * 2), 0, dataImg, 4, 4);
	//                //}

	//                dataArrays.Add(dataImg);
	//                writer.Write(dataImg.Length);
	//                writer.Write(dataOffset);

	//                dataOffset += dataImg.Length;
	//            }

	//            foreach (var d in dataArrays) {
	//                writer.Write(d);
	//            }
	//        }
	//    }
	//}
}
