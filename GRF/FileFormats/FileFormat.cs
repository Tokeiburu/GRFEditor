using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;
using Utilities.Extension;

namespace GRF.FileFormats {
	public sealed class FileFormat {
		public static List<FileFormat> AllFileFormats = new List<FileFormat>();

		public static FileFormat AllGrfs = new FileFormat(".grf;.rgz;.gpf;.thor", "Container", Format.AllContainers);
		public static FileFormat Act = new FileFormat(".act", "Animation", Format.Act);
		public static FileFormat Pal = new FileFormat(".pal", "Palette", Format.Pal);
		public static FileFormat Ebm = new FileFormat(".ebm", "Emblem", Format.Ebm);
		public static FileFormat Grf = new FileFormat(".grf", "Gravity Resource", Format.Grf);
		public static FileFormat Gpf = new FileFormat(".gpf", "Gravity Patch", Format.Gpf);
		public static FileFormat Rgz = new FileFormat(".rgz", "Ragnarok Gzip", Format.Rgz);
		public static FileFormat Cde = new FileFormat(".cde", "Client Database Editor", Format.Cde);
		public static FileFormat Sde = new FileFormat(".sde", "Server Database Editor", Format.Sde);
		public static FileFormat Spr = new FileFormat(".spr", "Sprite", Format.Spr);
		public static FileFormat Exe = new FileFormat(".exe", "Executable", Format.Exe);
		public static FileFormat Lua = new FileFormat(".lua", "Lua", Format.Lua);
		public static FileFormat Txt = new FileFormat(".txt", "Txt", Format.Txt);
		public static FileFormat Log = new FileFormat(".log", "Log", Format.Log);
		public static FileFormat Xml = new FileFormat(".xml", "Xml", Format.Xml);
		public static FileFormat Ezv = new FileFormat(".ezv", "Ezv", Format.Ezv);
		public static FileFormat Lub = new FileFormat(".lub", "Lub", Format.Lub);
		public static FileFormat Gif = new FileFormat(".gif", "Gif", Format.Gif);
		public static FileFormat All = new FileFormat(".*", "All", Format.All);
		public static FileFormat GrfKey = new FileFormat(".grfkey", "GRF Key", Format.GrfKey);
		public static FileFormat Gat = new FileFormat(".gat", "Altitude", Format.Gat);
		public static FileFormat PalAndSpr = new FileFormat(".spr;.pal", "Sprite and Palette", Format.PalAndSpr);
		public static FileFormat Image = new FileFormat(".bmp;.png;.jpg;.tga", "Image", Format.Image);
		public static FileFormat Thor = new FileFormat(".thor", "Thor", Format.Thor);
		public static FileFormat PaletteContainers = new FileFormat(".pal;.spr;.bmp", "Palette", Format.PaletteContainers);
		public static FileFormat Str = new FileFormat(".str", "Animation", Format.Str);
		public static FileFormat Json = new FileFormat(".json", "Json", Format.Json);
		public static FileFormat Bson = new FileFormat(".bson", "Bson", Format.Bson);

		private FileFormat(string extension, string name, Format flag) {
			Extension = extension;
			Name = name;
			Filter = Extension.Replace(".", "*.");
			Flag = flag;
			AllFileFormats.Add(this);
			Extensions = extension.Split(';').ToArray();
		}

		//"GRF Files|*.grf;*.rgz;*.gpf|GRF|*.grf|GPF|*.gpf|RGZ|*.rgz|Synchronized Files|*.syn";
		public string Extension { get; private set; }
		public string[] Extensions { get; private set; }
		public string Filter { get; private set; }
		public string Name { get; private set; }
		public Format Flag { get; private set; }

		private static string _mergeFilters(params FileFormat[] formats) {
			return Methods.Aggregate(formats.Select(p => p.ToFilter()).ToList(), "|");
		}

		public static string MergeFilters(params Format[] formats) {
			return _mergeFilters(formats.Select(p => AllFileFormats.FirstOrDefault(format => format.Flag == p)).ToArray());
		}

		public static string MergeFilters(Format formats) {
			return _mergeFilters(AllFileFormats.Where(format => formats.HasFlags(format.Flag)).ToArray());
		}

		public string ToFilter() {
			return Name + " Files|" + Filter;
		}

		public override string ToString() {
			return ToFilter();
		}
	}

	[Flags]
	public enum Format : uint {
		Ebm = 1 << 1,
		Pal = 1 << 2,
		AllContainers = 1 << 3,
		Grf = 1 << 4,
		Gpf = 1 << 5,
		Rgz = 1 << 6,
		Cde = 1 << 8,
		PalAndSpr = 1 << 9,
		Spr = 1 << 10,
		Exe = 1 << 11,
		Image = 1 << 12,
		Txt = 1 << 13,
		Lua = 1 << 14,
		Log = 1 << 15,
		Xml = 1 << 16,
		Ezv = 1 << 17,
		All = 1 << 18,
		GrfKey = 1 << 19,
		Gat = 1 << 20,
		Gif = 1 << 21,
		Thor = 1 << 22,
		Sde = 1 << 23,
		Act = 1 << 24,
		PaletteContainers = 1 << 25,
		Lub = 1 << 26,
		Str = 1 << 27,
		Json = 1 << 28,
		Bson = 1 << 29,
	}
}