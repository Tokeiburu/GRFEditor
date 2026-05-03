using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace GRF.FileFormats {
	public sealed class FileFormat {
		public static List<FileFormat> AllFileFormats = new List<FileFormat>();

		public static FileFormat AllContainers = new FileFormat(".grf;.rgz;.gpf;.thor", "Container");
		public static FileFormat Act = new FileFormat(".act", "Animation");
		public static FileFormat Pal = new FileFormat(".pal", "Palette");
		public static FileFormat Ebm = new FileFormat(".ebm", "Emblem");
		public static FileFormat Grf = new FileFormat(".grf", "Gravity Resource");
		public static FileFormat Gpf = new FileFormat(".gpf", "Gravity Patch");
		public static FileFormat Rgz = new FileFormat(".rgz", "Ragnarok Gzip");
		public static FileFormat Cde = new FileFormat(".cde", "Client Database Editor");
		public static FileFormat Sde = new FileFormat(".sde", "Server Database Editor");
		public static FileFormat Spr = new FileFormat(".spr", "Sprite");
		public static FileFormat Exe = new FileFormat(".exe", "Executable");
		public static FileFormat Lua = new FileFormat(".lua", "Lua");
		public static FileFormat Txt = new FileFormat(".txt", "Txt");
		public static FileFormat Log = new FileFormat(".log", "Log");
		public static FileFormat Xml = new FileFormat(".xml", "Xml");
		public static FileFormat Ezv = new FileFormat(".ezv", "Ezv");
		public static FileFormat Lub = new FileFormat(".lub", "Lub");
		public static FileFormat Gif = new FileFormat(".gif", "Gif");
		public static FileFormat All = new FileFormat(".*", "All");
		public static FileFormat GrfKey = new FileFormat(".grfkey", "GRF Key");
		public static FileFormat Gat = new FileFormat(".gat", "Altitude");
		public static FileFormat PalAndSpr = new FileFormat(".spr;.pal", "Sprite and Palette");
		public static FileFormat Image = new FileFormat(".bmp;.png;.jpg;.tga", "Image");
		public static FileFormat Thor = new FileFormat(".thor", "Thor");
		public static FileFormat PaletteContainers = new FileFormat(".pal;.spr;.bmp", "Palette");
		public static FileFormat Str = new FileFormat(".str", "Animation");
		public static FileFormat Json = new FileFormat(".json", "Json");
		public static FileFormat Bson = new FileFormat(".bson", "Bson");
		public static FileFormat Rsm = new FileFormat(".rsm", "Rsm");
		public static FileFormat Rsm2 = new FileFormat(".rsm2", "Rsm2");
		public static FileFormat Csv = new FileFormat(".csv", "Csv");
		public static FileFormat Bmp = new FileFormat(".bmp", "Bitmap");
		public static FileFormat Png = new FileFormat(".png", "PNG");
		public static FileFormat Tga = new FileFormat(".tga", "Targa");
		public static FileFormat Jpeg = new FileFormat(".jpg", "Jpeg");

		private FileFormat(string extension, string name) {
			Extension = extension;
			Name = name;
			Filter = Extension.Replace(".", "*.");
			AllFileFormats.Add(this);
			Extensions = extension.Split(';').ToArray();
		}

		public string Extension { get; private set; }
		public string[] Extensions { get; private set; }
		public string Filter { get; private set; }
		public string Name { get; private set; }

		private static string _mergeFilters(List<FileFormat> formats) {
			return Methods.Aggregate(formats.Select(p => p.ToFilter()).ToList(), "|");
		}

		public static string MergeFilters(params FileFormat[] formats) {
			return _mergeFilters(formats.ToList());
		}

		public static string MergeFilters(List<FileFormat> formats) {
			return _mergeFilters(formats);
		}

		public string ToFilter() {
			return Name + " Files|" + Filter;
		}

		public override string ToString() {
			return ToFilter();
		}
	}

	//[Flags]
	//public enum Format : long {
	//	Ebm = 1 << 1,
	//	Pal = 1 << 2,
	//	AllContainers = 1 << 3,
	//	Grf = 1 << 4,
	//	Gpf = 1 << 5,
	//	Rgz = 1 << 6,
	//	Cde = 1 << 8,
	//	PalAndSpr = 1 << 9,
	//	Spr = 1 << 10,
	//	Exe = 1 << 11,
	//	Image = 1 << 12,
	//	Txt = 1 << 13,
	//	Lua = 1 << 14,
	//	Log = 1 << 15,
	//	Xml = 1 << 16,
	//	Ezv = 1 << 17,
	//	All = 1 << 18,
	//	GrfKey = 1 << 19,
	//	Gat = 1 << 20,
	//	Gif = 1 << 21,
	//	Thor = 1 << 22,
	//	Sde = 1 << 23,
	//	Act = 1 << 24,
	//	PaletteContainers = 1 << 25,
	//	Lub = 1 << 26,
	//	Str = 1 << 27,
	//	Json = 1 << 28,
	//	Bson = 1 << 29,
	//	Rsm = 1 << 30,
	//	Rsm2 = 1 << 31,
	//	Csv = 1 << 32,
	//}
}