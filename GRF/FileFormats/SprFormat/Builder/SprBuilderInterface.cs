using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat.Commands;
using GRF.Image;
using GRF.Threading;
using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.SprFormat.Builder {
	/// <summary>
	/// Used to generate sprite files
	/// </summary>
	public class SprBuilderInterface : AbstractCommand<ICommand>, IProgress {
		#region Delegates

		public delegate void SprBuilderEventHandler(object sender, SprBuilderImageView view);

		public delegate void SprBuilderInsertionEventHandler(object sender, SprBuilderImageView view, int index, GrfImageType type);

		#endregion

		//public delegate void SaveImageDelegate(string pathToExtract, string filenameWithoutExtension, int imageIndex, GRFImage image, bool useTga);

		public static ISprConverter[] _converters = new ISprConverter[] { SprConverterProvider.GetConverter(2, 1), SprConverterProvider.GetConverter(2, 0) };
		private readonly List<SprBuilderImageView> _imagesBgra32 = new List<SprBuilderImageView>();
		private readonly List<SprBuilderImageView> _imagesIndexed8 = new List<SprBuilderImageView>();
		//private readonly PaletteValidator _palValidator = new PaletteValidator();
		private readonly Pal _palette = new Pal();
		private readonly GrfImageType[] _validImageTypes = new GrfImageType[] { GrfImageType.Indexed8, GrfImageType.Bgra32 };
		private string _internalSpriteFullPath;
		private bool _paletteIsSet;

		public SprBuilderInterface() {
			Converter = Converters[0];
		}

		public Pal Palette {
			get { return _palette; }
		}

		public static ISprConverter[] Converters {
			get { return _converters; }
		}

		public static SprBuilderInterface Instance {
			get { return new SprBuilderInterface(); }
		}

		public ISprConverter Converter { get; set; }

		private string _spriteFullPath {
			get { return _internalSpriteFullPath ?? (_internalSpriteFullPath = ""); }
			set { _internalSpriteFullPath = value; }
		}

		private string _spriteName {
			get { return Path.GetFileNameWithoutExtension(_spriteFullPath); }
		}

		public List<SprBuilderImageView> ImagesIndexed8 {
			get { return _imagesIndexed8; }
		}

		public List<SprBuilderImageView> ImagesBgra32 {
			get { return _imagesBgra32; }
		}

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		public event SprBuilderEventHandler ItemRemoved;
		public event SprBuilderEventHandler ItemFlipped;
		public event SprBuilderInsertionEventHandler ItemInserted;

		private void _onItemFlipped(SprBuilderImageView view) {
			SprBuilderEventHandler handler = ItemFlipped;
			if (handler != null) handler(this, view);
		}

		private void _onItemInserted(SprBuilderImageView view, int index, GrfImageType type) {
			SprBuilderInsertionEventHandler handler = ItemInserted;
			if (handler != null) handler(this, view, index, type);
		}

		private void _onItemRemoved(SprBuilderImageView view) {
			SprBuilderEventHandler handler = ItemRemoved;
			if (handler != null) handler(this, view);
		}

		public void Create(string filename) {
			if (Converter == null)
				throw new SprException("No converter has been chosen yet.");

			Spr spr = new Spr();
			foreach (SprBuilderImageView view in _imagesIndexed8) {
				spr.AddImage(view.Image);
			}

			foreach (SprBuilderImageView view in _imagesBgra32) {
				spr.AddImage(view.Image);
			}
			Converter.Save(spr, filename);
		}

		internal SprBuilderImageView Insert(GrfImage image, int index, string imageOriginalName = null) {
			if (!_validImageTypes.Contains(image.GrfImageType))
				throw new SprInvalidImageFormatException();

			if (index < 0)
				throw new SprException("Invalid insertion index.");

			if (image.GrfImageType == GrfImageType.Indexed8 && index > _imagesIndexed8.Count)
				throw new SprException("Invalid insertion index (index is greater than the number of images).");

			if (image.GrfImageType == GrfImageType.Bgra32 && index > _imagesBgra32.Count)
				throw new SprException("Invalid insertion index (index is greater than the number of images).");

			SprBuilderImageView view;

			if (image.GrfImageType == GrfImageType.Indexed8) {
				byte[] palette = image.Palette;

				if (!_paletteIsSet) {
					SetPalette(image.Palette);
				}

				if (!Methods.ByteArrayCompare(palette, _palette.BytePalette)) {
					throw new SprException("Invalid palette");
				}

				view = new SprBuilderImageView { Image = image, OriginalName = imageOriginalName };
				_imagesIndexed8.Insert(index, view);
			}
			else {
				view = new SprBuilderImageView { Image = image, OriginalName = imageOriginalName };
				_imagesBgra32.Insert(index, view);
			}

			_refreshLists();
			_onItemInserted(view, index, image.GrfImageType);
			return image.GrfImageType == GrfImageType.Indexed8 ? _imagesIndexed8[index] : _imagesBgra32[index];
		}

		internal SprBuilderImageView Insert(SprBuilderImageView view) {
			return Insert(view.Image, view.DisplayID, view.OriginalName);
		}

		private void _refreshLists() {
			for (int i = 0; i < _imagesIndexed8.Count; i++) {
				_imagesIndexed8[i].DisplayID = i;
				_imagesIndexed8[i].Filename = _spriteName + String.Format("{0:0000}", i);
			}

			for (int i = 0; i < _imagesBgra32.Count; i++) {
				_imagesBgra32[i].DisplayID = i;
				_imagesBgra32[i].Filename = _spriteName + String.Format("{0:0000}", i + _imagesIndexed8.Count);
			}
		}

		private void _reloadLists(Spr spr) {
			_imagesIndexed8.Clear();
			_imagesBgra32.Clear();

			for (int i = 0; i < spr.NumberOfIndexed8Images; i++) {
				_imagesIndexed8.Add(new SprBuilderImageView { DisplayID = i, Filename = _spriteName + String.Format("{0:0000}", i), Image = spr.Images[i] });
			}

			for (int i = 0; i < spr.NumberOfBgra32Images; i++) {
				_imagesBgra32.Add(new SprBuilderImageView { DisplayID = i, Filename = _spriteName + String.Format("{0:0000}", i + spr.NumberOfIndexed8Images), Image = spr.Images[spr.NumberOfIndexed8Images + i] });
			}

			if (spr.NumberOfIndexed8Images > 0) {
				_palette.SetPalette(spr.Palette.BytePalette);
			}
			else {
				_palette.SetPalette(new byte[1024]);
				_paletteIsSet = false;
			}
		}

		internal void Delete(int index, GrfImageType type) {
			_validateIndex(index, type);

			SprBuilderImageView view;

			if (type == GrfImageType.Indexed8) {
				view = _imagesIndexed8[index];
				_imagesIndexed8.RemoveAt(index);
			}
			else {
				view = _imagesBgra32[index];
				_imagesBgra32.RemoveAt(index);
			}

			if (_imagesIndexed8.Count == 0) {
				_paletteIsSet = false;
				_palette.SetPalette(new byte[1024]);
			}

			_onItemRemoved(view);
			_refreshLists();
		}

		internal void Delete(SprBuilderImageView image) {
			GrfImageType type = image.Image.GrfImageType;
			int index = type == GrfImageType.Indexed8 ? _imagesIndexed8.IndexOf(image) : _imagesBgra32.IndexOf(image);
			index = index < 0 ? image.DisplayID : index;

			Delete(index, type);
		}

		private void _validateIndex(int index, GrfImageType type) {
			if (!_validImageTypes.Contains(type))
				throw new SprInvalidImageFormatException();

			if (index < 0)
				throw new SprException("Invalid index.");

			if (type == GrfImageType.Indexed8 && index >= _imagesIndexed8.Count)
				throw new SprException("Invalid index (index is greater than the number of images).");

			if (type == GrfImageType.Bgra32 && index >= _imagesBgra32.Count)
				throw new SprException("Invalid index (index is greater than the number of images).");
		}

		public GrfImage GetImage(int index, GrfImageType type) {
			_validateIndex(index, type);
			return ((type == GrfImageType.Indexed8) ? _imagesIndexed8[index] : _imagesBgra32[index + _imagesIndexed8.Count]).Image;
		}

		public void Open(string path) {
			Spr spr = new Spr(File.ReadAllBytes(path));
			Converter = spr.Converter;
			_spriteFullPath = path;
			_reloadLists(spr);
		}

		public void Open(Spr sprite) {
			Converter = sprite.Converter;
			_reloadLists(sprite);
		}

		public string GetFileName() {
			return _spriteName == "" ? "sprite" : _spriteName;
		}

		public byte[] GetPalette() {
			return _palette.BytePalette;
		}

		public List<byte> GetUsedPaletteIndexes() {
			List<byte> used = new List<byte>();

			for (int i = 0; i < _imagesIndexed8.Count; i++) {
				GrfImage im = _imagesIndexed8[i].Image;
				used.AddRange(im.Pixels.Distinct());
			}

			return used.Distinct().ToList();
		}

		public List<byte> GetUnusedPaletteIndexes() {
			List<byte> used = GetUsedPaletteIndexes();
			List<byte> unused = new List<byte>();

			for (int i = 0; i < 256; i++) {
				if (!used.Contains((byte) i))
					unused.Add((byte) i);
			}
			return unused;
		}

		internal void SetPalette(byte[] palette) {
			for (int i = 0; i < _imagesIndexed8.Count; i++) {
				_imagesIndexed8[i].Image.SetPalette(ref palette);
				_imagesIndexed8[i].Update();
			}

			_palette.SetPalette(palette);
			_paletteIsSet = true;
		}

		internal void ChangeImageIndex(int indexSource, GrfImageType typeSource, int indexDestination, GrfImageType typeDestination) {
			if (typeSource == GrfImageType.Indexed8 && typeDestination == GrfImageType.Indexed8) {
				if (indexSource < indexDestination) {
					Insert(_imagesIndexed8[indexSource].Image, indexDestination + 1, _imagesIndexed8[indexSource].OriginalName);
					Delete(indexSource, typeSource);
				}
				else {
					Insert(_imagesIndexed8[indexSource].Image, indexDestination, _imagesIndexed8[indexSource].OriginalName);
					Delete(indexSource + 1, typeSource);
				}
			}
			else if (typeSource == GrfImageType.Bgra32 && typeDestination == GrfImageType.Bgra32) {
				if (indexSource < indexDestination) {
					Insert(_imagesBgra32[indexSource].Image, indexDestination + 1, _imagesBgra32[indexSource].OriginalName);
					Delete(indexSource, typeSource);
				}
				else {
					Insert(_imagesBgra32[indexSource].Image, indexDestination, _imagesBgra32[indexSource].OriginalName);
					Delete(indexSource + 1, typeSource);
				}
			}
		}

		protected override void _execute(ICommand command) {
			command.Execute(this);
		}

		protected override void _undo(ICommand command) {
			command.Undo(this);
		}

		protected override void _redo(ICommand command) {
			command.Execute(this);
		}

		internal void Flip(SprBuilderImageView view, FlipDirection dir) {
			view.Image.Flip(dir);
			_onItemFlipped(view);
		}
	}
}