using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.TgaFormat;
using Utilities.Extension;

namespace GRF.Image {
	public static class ImageProvider {
		public static GrfImage GetImage(FileEntry entry) {
			return GetImage(entry.GetDecompressedData(), entry.RelativePath.GetExtension());
		}

		public static GrfImage GetFirstImage(FileEntry entry) {
			return GetImage(entry.GetDecompressedData(), entry.RelativePath.GetExtension(), true);
		}

		public static GrfImage GetImage(MultiType data, string extension, bool firstImageOnly = false) {
			var dataDecompressed = data.Data;
			extension = extension.ToLower();

			switch (extension) {
				case ".bmp":
					return new CommonImageFormat(dataDecompressed).Image;
				case ".jpg":
					return new CommonImageFormat(dataDecompressed).Image;
				case ".png":
					return new CommonImageFormat(dataDecompressed).Image;
				case ".gat":
					return new Gat(dataDecompressed).Image;
				case ".tga":
					return new Tga(dataDecompressed).Image;
				case ".pal":
					return new Pal(dataDecompressed).Image;
				case ".spr":
					if (firstImageOnly)
						return Spr.GetFirstImage(dataDecompressed);
					return new Spr(dataDecompressed).Image;
				case ".ebm":
					return new Ebm(dataDecompressed).Image;
			}
			return null;
		}
	}
}
