using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.ContainerFormat;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.PalFormat;
using GRF.IO;
using GRF.Image;

namespace GRF.FileFormats.SprFormat {
	/// <summary>
	/// If the Spr file is loaded from an Act file, it 
	/// </summary>
	public class Spr : IImageable, IEnumerable<GrfImage> {
		private static bool _enableImageSizeCheck = true;
		private GrfImage _image;
		private Pal _pal;

		/// <summary>
		/// Initializes a new instance of the <see cref="Spr" /> class.
		/// </summary>
		public Spr() {
			Header = new SprHeader();
			RleImages = new List<Rle>();
			Images = new List<GrfImage>();
			Converter = SprConverterProvider.GetConverter(Header);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Spr" /> class.
		/// </summary>
		/// <param name="sprData">The sprite data.</param>
		public Spr(MultiType sprData)
			: this(sprData.Data, false) {
			LoadedPath = sprData.Path;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Spr" /> class.
		/// </summary>
		/// <param name="spr">The sprite.</param>
		public Spr(Spr spr) {
			Header = new SprHeader(spr.Header);
			NumberOfIndexed8Images = spr.NumberOfIndexed8Images;
			NumberOfBgra32Images = spr.NumberOfBgra32Images;
			Converter = spr.Converter;
			Images = new List<GrfImage>();

			if (spr.Palette != null)
				Palette = new Pal(spr.Palette.BytePalette, false);

			foreach (var image in spr.Images) {
				Images.Add(image.Copy());
			}
		}

		/// <summary>
		/// Prevents a default instance of the <see cref="Spr" /> class from being created.
		/// </summary>
		/// <param name="dataDecompressed">The data decompressed.</param>
		/// <param name="loadFirstImageOnly">if set to <c>true</c> [load first image only].</param>
		private Spr(byte[] dataDecompressed, bool loadFirstImageOnly) {
			ByteReader reader = new ByteReader(dataDecompressed);
			Header = new SprHeader(reader);
			RleImages = new List<Rle>();
			NumberOfIndexed8Images = reader.UInt16();
			NumberOfBgra32Images = reader.UInt16();
			Converter = SprConverterProvider.GetConverter(Header);
			List<GrfImage> imageSources = Converter.GetImages(this, reader, loadFirstImageOnly);
			Images = imageSources;
		}

		public static bool EnableImageSizeCheck {
			get { return _enableImageSizeCheck; }
			set { _enableImageSizeCheck = value; }
		}

		public string LoadedPath { get; set; }

		internal List<Rle> RleImages { get; set; }

		public Pal Palette {
			get { return _pal; }
			set {
				bool refresh = _pal != null;

				_pal = value;

				if (_pal != null) {
					_pal.PaletteChanged += new Pal.PalEventHandler(_pal_PaletteChanged);

					if (refresh)
						_pal_PaletteChanged(null);
				}
			}
		}

		public SprHeader Header { get; private set; }
		public ISprConverter Converter { get; set; }

		public int NumberOfImagesLoaded {
			get { return Images.Count; }
		}

		// The real number of images, regardless of the version
		public int NumberOfIndexed8Images { get; internal set; }
		public int NumberOfBgra32Images { get; internal set; }

		public List<GrfImage> Images { get; set; }

		#region IEnumerable<GrfImage> Members

		public IEnumerator<GrfImage> GetEnumerator() {
			return Images.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region IImageable Members

		/// <summary>
		/// Gets or sets the preview image.
		/// </summary>
		public GrfImage Image {
			get {
				if (_image == null)
					_generateOneImage(Images);

				return _image;
			}
			set { _image = value; }
		}

		#endregion

		public static GrfImage GetFirstImage(byte[] dataDecompressed) {
			Spr spr = new Spr(dataDecompressed, true);
			if (spr.Images.Count > 0)
				return spr.Images[0];
			return null;
		}

		private void _pal_PaletteChanged(object sender) {
			for (int i = 0; i < NumberOfIndexed8Images; i++) {
				GrfImage image = Images[i];
				byte[] paletteData = _pal.BytePalette;
				image.SetPalette(ref paletteData);
			}
		}

		private void _generateOneImage(List<GrfImage> imageSources) {
			if (imageSources == null || imageSources.Count <= 0) {
				byte[] pixels = new byte[4];
				Image = new GrfImage(ref pixels, 1, 1, GrfImageType.Bgra32);
				return;
			}

			if (imageSources.All(p => p.GrfImageType == GrfImageType.Indexed8)) {
				int bitWidth = imageSources.Sum(p => p.Width);
				int bitHeight = imageSources.Max(p => p.Height);
				int bitStride = bitWidth;

				int currentXPosition = 0;
				int colorsLength = bitWidth * bitHeight;
				byte[] realColors = new byte[colorsLength];

				foreach (GrfImage so in imageSources) {
					int width = so.Width;
					int height = so.Height;
					int stride = width;

					byte[] pixels1 = so.Pixels;

					for (int j = 0; j < height; j++) {
						Buffer.BlockCopy(pixels1, j * stride, realColors, j * bitStride + currentXPosition, stride);
					}
					currentXPosition += stride;
				}

				byte[] palette = imageSources[0].Palette;
				Image = new GrfImage(ref realColors, bitWidth, bitHeight, GrfImageType.Indexed8, ref palette);
			}
			else if (imageSources.All(p => p.GrfImageType == GrfImageType.Bgra32)) {
				int bitWidth = imageSources.Sum(p => p.Width);
				int bitHeight = imageSources.Max(p => p.Height);
				int bitStride = bitWidth * 4;

				int currentXPosition = 0;
				int colorsLength = bitWidth * bitHeight * 4;
				byte[] realColors = new byte[colorsLength];

				foreach (GrfImage so in imageSources) {
					int width = so.Width;
					int height = so.Height;
					int stride = width * 4;

					byte[] pixels1 = so.Pixels;

					for (int j = 0; j < height; j++) {
						Buffer.BlockCopy(pixels1, j * stride, realColors, j * bitStride + currentXPosition, stride);
					}
					currentXPosition += stride;
				}

				Image = new GrfImage(ref realColors, bitWidth, bitHeight, GrfImageType.Bgra32);
			}
			else {
				// Converting all images to Bgra32
				int bitWidth = imageSources.Sum(p => p.Width);
				int bitHeight = imageSources.Max(p => p.Height);
				int bitStride = bitWidth * 4;

				int currentXPosition = 0;
				int colorsLength = bitWidth * bitHeight * 4;
				byte[] realColors = new byte[colorsLength];
				byte[] palette = _toBgraPalette(imageSources.First(p => p.GrfImageType == GrfImageType.Indexed8).Palette);

				int t;
				int t2;
				byte[] pixels1;

				foreach (GrfImage so in imageSources) {
					int width = so.Width;
					int height = so.Height;
					int stride = width * 4;

					if (so.GrfImageType == GrfImageType.Indexed8) {
						pixels1 = so.Pixels;

						for (int j = 0; j < height; j++) {
							t = j * width;
							t2 = j * bitStride + currentXPosition;

							for (int x = 0; x < width; x++) {
								Buffer.BlockCopy(palette, pixels1[t + x] * 4, realColors, t2 + 4 * x, 4);
							}
						}
					}
					else {
						pixels1 = so.Pixels;

						for (int j = 0; j < height; j++) {
							Buffer.BlockCopy(pixels1, j * stride, realColors, j * bitStride + currentXPosition, stride);
						}
					}

					currentXPosition += stride;
				}

				Image = new GrfImage(ref realColors, bitWidth, bitHeight, GrfImageType.Bgra32);
			}
		}

		protected byte[] _toBgraPalette(byte[] palette) {
			byte[] pal = new byte[palette.Length];

			for (int i = 0, size = palette.Length; i < size; i += 4) {
				pal[i + 0] = palette[i + 2];
				pal[i + 1] = palette[i + 1];
				pal[i + 2] = palette[i + 0];
				pal[i + 3] = palette[i + 3];
			}

			return pal;
		}

		public HashSet<byte> GetUnusedPaletteIndexes() {
			HashSet<byte> used = GetUsedPaletteIndexes();
			HashSet<byte> unused = new HashSet<byte>();

			for (int i = 0; i < 256; i++) {
				if (!used.Contains((byte) i))
					unused.Add((byte) i);
			}
			return unused;
		}

		public HashSet<byte> GetUsedPaletteIndexes() {
			HashSet<byte> used = new HashSet<byte>();

			for (int i = 0; i < NumberOfIndexed8Images; i++) {
				GrfImage im = Images[i];

				for (int p = 0; p < im.Pixels.Length; p++) {
					used.Add(im.Pixels[p]);
				}
			}

			return used;
		}

		internal int AddImage(GrfImage image) {
			if (image.GrfImageType == GrfImageType.Indexed8) {
				Images.Insert(NumberOfIndexed8Images, image);
				NumberOfIndexed8Images++;
				return NumberOfIndexed8Images - 1;
			}

			Images.Insert(NumberOfIndexed8Images + NumberOfBgra32Images, image);
			NumberOfBgra32Images++;
			return NumberOfBgra32Images - 1;
		}

		internal int AddImage(GrfImage image, int index) {
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", "The index is out of range, it must be above or equal to 0 (make sure the image format corresponds to the proper container).");

			if (image.GrfImageType == GrfImageType.Indexed8) {
				if (index > NumberOfIndexed8Images)
					throw new ArgumentOutOfRangeException("index", "The index is out of range, it must be below or equal to " + NumberOfIndexed8Images + ".");

				Images.Insert(index, image);
				NumberOfIndexed8Images++;
				return index;
			}

			if (NumberOfIndexed8Images + index > NumberOfImagesLoaded)
				throw new ArgumentOutOfRangeException("index", "The index is out of range, it must be below or equal to " + NumberOfImagesLoaded + ".");

			Images.Insert(NumberOfIndexed8Images + index, image);
			NumberOfBgra32Images++;
			return index;
		}

		public void SetToNullIndexes(Act act, GrfImageType type, int relativeIndex) {
			if (type == GrfImageType.Indexed8) {
				act.AllLayers(layer => {
					if (layer.IsIndexed8()) {
						if (layer.SpriteIndex == relativeIndex) {
							layer.SpriteIndex = -1;
						}
					}
				});
			}
			else if (type == GrfImageType.Bgra32) {
				act.AllLayers(layer => {
					if (layer.IsBgra32()) {
						if (layer.SpriteIndex == relativeIndex) {
							layer.SpriteIndex = -1;
						}
					}
				});
			}
		}

		public void ShiftIndexesAbove(Act act, GrfImageType type, int diff, int relativeIndex) {
			if (type == GrfImageType.Indexed8) {
				act.AllLayers(layer => {
					if (layer.IsIndexed8()) {
						if (layer.SpriteIndex > relativeIndex) {
							layer.SpriteIndex += diff;
						}
					}
				});
			}
			else if (type == GrfImageType.Bgra32) {
				act.AllLayers(layer => {
					if (layer.IsBgra32()) {
						if (layer.SpriteIndex > relativeIndex) {
							layer.SpriteIndex += diff;
						}
					}
				});
			}
		}

		public void ShiftIndexesBelow(Act act, GrfImageType type, int diff, int relativeIndex) {
			if (type == GrfImageType.Indexed8) {
				act.AllLayers(layer => {
					if (layer.IsIndexed8()) {
						if (layer.SpriteIndex < relativeIndex) {
							layer.SpriteIndex += diff;
						}
					}
				});
			}
			else if (type == GrfImageType.Bgra32) {
				act.AllLayers(layer => {
					if (layer.IsBgra32()) {
						if (layer.SpriteIndex < relativeIndex) {
							layer.SpriteIndex += diff;
						}
					}
				});
			}
		}

		public void Remove(int relativeIndex, GrfImageType type) {
			if (type == GrfImageType.Indexed8) {
				if (relativeIndex < NumberOfIndexed8Images && relativeIndex > -1) {
					Images.RemoveAt(relativeIndex);
					NumberOfIndexed8Images--;

					if (NumberOfIndexed8Images == 0) {
						Palette = null;
					}
				}
			}
			else {
				if (relativeIndex < NumberOfBgra32Images && relativeIndex > -1) {
					Images.RemoveAt(NumberOfIndexed8Images + relativeIndex);
					NumberOfBgra32Images--;
				}
			}
		}

		public void Remove(int relativeIndex, GrfImageType type, Act act, EditOption edit) {
			if (edit == EditOption.KeepCurrentIndexes) {
				Remove(relativeIndex, type);
			}
			else if (edit == EditOption.AdjustIndexes) {
				Remove(relativeIndex, type);
				SetToNullIndexes(act, type, relativeIndex);
				ShiftIndexesAbove(act, type, -1, relativeIndex);
			}
		}

		public void Remove(int absoluteIndex) {
			if (absoluteIndex < NumberOfIndexed8Images) {
				Remove(absoluteIndex, GrfImageType.Indexed8);
			}
			else {
				Remove(absoluteIndex - NumberOfIndexed8Images, GrfImageType.Bgra32);
			}
		}

		public void Remove(int absoluteIndex, Act act, EditOption edit) {
			if (absoluteIndex < NumberOfIndexed8Images) {
				Remove(absoluteIndex, GrfImageType.Indexed8, act, edit);
			}
			else {
				Remove(absoluteIndex - NumberOfIndexed8Images, GrfImageType.Bgra32, act, edit);
			}
		}

		public void Remove(Layer layer) {
			Remove(layer.GetAbsoluteSpriteId(this));
		}

		public void Remove(Layer layer, Act act, EditOption edit) {
			Remove(layer.GetAbsoluteSpriteId(this), act, edit);
		}

		public void Insert(int absoluteIndex, GrfImage image) {
			if (image.GrfImageType == GrfImageType.Indexed8) {
				AddImage(image, absoluteIndex);

				if (NumberOfIndexed8Images == 1) {
					// Added the image successfully
					Palette = new Pal(image.Palette);
				}
				else {
					byte[] palette = new byte[1024];
					Buffer.BlockCopy(Palette.BytePalette, 0, palette, 0, 1024);
					image.SetPalette(ref palette);
				}
			}
			else if (image.GrfImageType == GrfImageType.Bgra32) {
				AddImage(image, absoluteIndex - NumberOfIndexed8Images);
			}
			else {
				throw new Exception("Invalid image format. Found : " + image.GrfImageType + ", expected Indexed8 or Bgra32.");
			}
		}

		public void Insert(int absoluteIndex, GrfImage image, Act act, EditOption edit) {
			Insert(absoluteIndex, image);
			ShiftIndexesAbove(act, image.GrfImageType, 1, absoluteIndex);
		}

		public void Insert(int relativeIndex, GrfImageType type, GrfImage image) {
			Insert(relativeIndex + (type == GrfImageType.Bgr32 ? NumberOfIndexed8Images : 0), image);
		}

		public void Insert(int relativeIndex, GrfImageType type, GrfImage image, Act act, EditOption edit) {
			Insert(relativeIndex + (type == GrfImageType.Bgr32 ? NumberOfIndexed8Images : 0), image, act, edit);
		}

		public void Replace(int absoluteIndex, GrfImage image, Act act) {
			if (absoluteIndex >= Images.Count)
				return;

			var oldImage = Images[absoluteIndex];

			if (oldImage.GrfImageType == image.GrfImageType) {
				Images[absoluteIndex] = image;
			}
			else if (oldImage.GrfImageType == GrfImageType.Indexed8) {	// New image is Bgra32
				var img2 = image.Copy();

				if (image.GrfImageType != GrfImageType.Bgra32) {
					img2.Convert(GrfImageType.Bgra32);
				}

				SetToNullIndexes(act, GrfImageType.Indexed8, absoluteIndex);
				ShiftIndexesAbove(act, GrfImageType.Indexed8, -1, absoluteIndex);

				Remove(absoluteIndex);
				
				foreach (var layer in act.GetAllLayers().Where(p => p.SpriteIndex == -1)) {
					layer.SpriteIndex = NumberOfBgra32Images;
					layer.SpriteType = SpriteTypes.Bgra32;
				}

				InsertAny(img2);
			}
			else if (oldImage.GrfImageType == GrfImageType.Bgra32) {
				var img2 = image.Copy();

				if (image.GrfImageType != GrfImageType.Indexed8) {
					img2.Convert(GrfImageType.Bgra32);
				}

				SetToNullIndexes(act, GrfImageType.Bgra32, absoluteIndex - NumberOfIndexed8Images);
				ShiftIndexesAbove(act, GrfImageType.Bgra32, -1, absoluteIndex - NumberOfIndexed8Images);
				
				Remove(absoluteIndex);

				foreach (var layer in act.GetAllLayers().Where(p => p.SpriteIndex == -1)) {
					layer.SpriteIndex = NumberOfIndexed8Images;
					layer.SpriteType = SpriteTypes.Indexed8;
				}

				InsertAny(img2);
			}
		}

		public int InsertAny(GrfImage image) {
			if (image.GrfImageType == GrfImageType.Indexed8 ||
			    image.GrfImageType == GrfImageType.Bgra32) {
				return AddImage(image);
			}

			throw new Exception("Invalid image format. Found : " + image.GrfImageType + ", expected Indexed8 or Bgra32.");
		}

		public GrfImage GetImage(int index, int type) {
			return GetImage(index, type == 0 ? GrfImageType.Indexed8 : GrfImageType.Bgra32);
		}

		public GrfImage GetImage(int index, SpriteTypes type) {
			return GetImage(index, type == 0 ? GrfImageType.Indexed8 : GrfImageType.Bgra32);
		}

		public GrfImage GetImage(Layer layer) {
			if (layer == null) return null;
			return GetImage(layer.SpriteIndex, layer.SpriteType);
		}

		public GrfImage GetImage(int absoluteIndex) {
			if (absoluteIndex < NumberOfIndexed8Images) {
				return GetImage(absoluteIndex, GrfImageType.Indexed8);
			}

			return GetImage(absoluteIndex - NumberOfIndexed8Images, GrfImageType.Indexed8);
		}

		public GrfImage GetImage(int index, GrfImageType type) {
			if (index < 0 || index >= NumberOfImagesLoaded)
				return null;

			if (type == GrfImageType.Indexed8) {
				if (index < NumberOfIndexed8Images && index < Images.Count)
					return Images[index];
			}
			else {
				if (index < NumberOfBgra32Images && NumberOfIndexed8Images + index < Images.Count)
					return Images[NumberOfIndexed8Images + index];
			}

			return null;
		}

		public void ReloadCount() {
			NumberOfBgra32Images = Images.Count(p => p.GrfImageType == GrfImageType.Bgra32);
			NumberOfIndexed8Images = Images.Count(p => p.GrfImageType == GrfImageType.Indexed8);
		}

		public int RelativeToAbsolute(int relativeIndex) {
			return relativeIndex < NumberOfIndexed8Images ? relativeIndex : relativeIndex + NumberOfIndexed8Images;
		}

		public int AbsoluteToRelative(int absoluteIndex, int type) {
			return type == 0 ? absoluteIndex : absoluteIndex - NumberOfIndexed8Images;
		}

		public int AbsoluteToRelative(int absoluteIndex, SpriteTypes type) {
			return type == SpriteTypes.Indexed8 ? absoluteIndex : absoluteIndex - NumberOfIndexed8Images;
		}

		public int AbsoluteToRelative(int absoluteIndex, GrfImageType type) {
			return type == GrfImageType.Indexed8 ? absoluteIndex : absoluteIndex - NumberOfIndexed8Images;
		}

		public void Save(Stream stream) {
			(Converter ?? new SprConverterV2M1(Header)).Save(this, stream, false);
		}

		public void Save(string path) {
			(Converter ?? new SprConverterV2M1(Header)).Save(this, path);
		}

		public void Save() {
			(Converter ?? new SprConverterV2M1(Header)).Save(this, LoadedPath);
		}

		public void RemoveUnusedImages(Act act) {
			RemoveUnusedImages(act, EditOption.AdjustIndexes);
		}

		public void RemoveUnusedImages(Act act, EditOption edit) {
			for (int i = Images.Count - 1; i >= 0; i--) {
				if (act.FindUsageOf(i).Count == 0) {
					Remove(i, act, edit);
				}
			}
		}

		public void RemoveSimilarImages(GrfImage image, float tolerance, Act act) {
			RemoveSimilarImages(image, tolerance, act, EditOption.AdjustIndexes);
		}

		public void RemoveSimilarImages(GrfImage image, float tolerance, Act act, EditOption edit) {
			GrfExceptions.IfOutOfRangeThrow(tolerance, "tolerance", 0, 1);
			GrfImage.ClearBufferedData();

			for (int i = Images.Count - 1; i >= 0; i--) {
				if (image.SimilarityWith(Images[i]) >= tolerance) {
					Remove(i, act, edit);
				}
			}
		}

		public IEnumerable GetEarlyEndingEncoding() {
			for (int index = 0; index < RleImages.Count; index++) {
				var rle = RleImages[index];
				if (rle.EarlyEndingEncoding == true)
					yield return index;
			}
		}
	}

	public enum EditOption {
		KeepCurrentIndexes,
		AdjustIndexes,
	}
}