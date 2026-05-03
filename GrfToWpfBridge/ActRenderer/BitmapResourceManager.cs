using GRF.FileFormats.ActFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GrfToWpfBridge.ActRenderer {
	public class BitmapResourceManager {
		public class BitmapHandle {
			public WriteableBitmap Bitmap;
			public bool InUse;
			public bool New;
			public DateTime CreationTime;
			public DateTime LastUse;
		}

		public class CachedHandle {
			public int GrfImageHash;
			public int Width;
			public int Height;
			public Dictionary<uint, BitmapHandle> Bitmaps = new Dictionary<uint, BitmapHandle>();
		}

		private List<CachedHandle> _cachedBitmaps = new List<CachedHandle>();
		//private Dictionary<(int w, int h, uint color, GrfImageType fmt), Stack<BitmapHandle>> _bitmapPool = new Dictionary<(int w, int h, uint color, GrfImageType fmt), Stack<BitmapHandle>>();

		public BitmapHandle GetBitmapHandle(SpriteIndex index, Act act, GrfImage image, GrfColor color) {
			ValidateCache(act);

			var handle = _getBitmapHandle(index, act, image, color);

			return handle;
		}

		public void ClearCache() {
			foreach (var cacheBitmap in _cachedBitmaps) {
				cacheBitmap.Bitmaps.Clear();
			}

			_cachedBitmaps.Clear();
		}

		public void ValidateCache(Act act) {
			// Add missing cache
			while (_cachedBitmaps.Count < act.Sprite.NumberOfImagesLoaded) {
				_cachedBitmaps.Add(new CachedHandle());
			}

			// Remove extra cached images
			var max = act.Sprite.NumberOfImagesLoaded;

			for (int i = max; i < _cachedBitmaps.Count; i++) {
				_cachedBitmaps.RemoveAt(max);
			}

			// Ensure the cache has valid entries
			var images = act.Sprite.Images;
			var now = DateTime.Now;

			for (int i = 0; i < _cachedBitmaps.Count && i < max; i++) {
				var cacheBitmap = _cachedBitmaps[i];
				var image = images[i];

				{
					var toDeleteKeys = new List<uint>();

					foreach (var bitmapHandle in cacheBitmap.Bitmaps) {
						var elapsed = now - bitmapHandle.Value.LastUse;
						
						if (elapsed.TotalSeconds > 10) {
							toDeleteKeys.Add(bitmapHandle.Key);
						}
					}

					foreach (var key in toDeleteKeys) {
						cacheBitmap.Bitmaps.Remove(key);
					}
				}

				if (cacheBitmap.GrfImageHash == image.GetHashCode() &&
					cacheBitmap.Width == image.Width &&
					cacheBitmap.Height == image.Height) {
					continue;
				}

				cacheBitmap.GrfImageHash = image.GetHashCode();
				cacheBitmap.Width = image.Width;
				cacheBitmap.Height = image.Height;
				cacheBitmap.Bitmaps.Clear();
			}
		}

		private BitmapHandle _getBitmapHandle(SpriteIndex index, Act act, GrfImage image, GrfColor color) {
			int absoluteIndex = index.GetAbsoluteIndex(act.Sprite);
			uint colorKey = color.ToArgbInt32();

			CachedHandle cache = _cachedBitmaps[absoluteIndex];

			if (cache.Bitmaps.TryGetValue(colorKey, out BitmapHandle handle)) {
				handle.LastUse = DateTime.Now;
				return handle;
			}

			int width = image.Width;
			int height = image.Height;
			GrfImageType type = image.GrfImageType;
			BitmapPalette palette = null;

			handle = new BitmapHandle();
			cache.Bitmaps[colorKey] = handle;

			if (image.GrfImageType == GrfImageType.Indexed8) {
				palette = new BitmapPalette(_loadColors(image.Palette, color));
			}
			else {
				image = image.Copy();
				image.Multiply(color);
			}

			int srcStride = width;
			int dstStride = (width * 8 + 31 & ~31) / 8;

			handle.Bitmap = new WriteableBitmap(width, height, 96, 96, type == GrfImageType.Indexed8 ? PixelFormats.Indexed8 : PixelFormats.Bgra32, palette);

			try {
				handle.Bitmap.Lock();

				unsafe {
					fixed (byte* src = image.Pixels) {
						if (image.GrfImageType == GrfImageType.Indexed8 && srcStride != dstStride) {
							byte* pBackBuffer = (byte*)handle.Bitmap.BackBuffer;

							for (int y = 0; y < height; y++) {
								Buffer.MemoryCopy(
									src + y * srcStride,
									pBackBuffer + y * dstStride,
									srcStride,
									srcStride);
							}
						}
						else {
							Buffer.MemoryCopy(
								src,
								(void*)handle.Bitmap.BackBuffer,
								image.Pixels.Length,
								image.Pixels.Length);
						}
					}
				}

				handle.Bitmap.AddDirtyRect(new Int32Rect(0, 0, image.Width, image.Height));
			}
			finally {
				handle.Bitmap.Unlock();
			}

			handle.LastUse = DateTime.Now;
			handle.CreationTime = DateTime.Now;
			return handle;
		}

		private List<Color> _loadColors(byte[] palette, GrfColor multColor) {
			if (palette == null)
				throw new Exception("Palette not loaded.");

			var colors = new List<Color>(256);

			int fA = (multColor.A << 16) / 255;
			int fR = (multColor.R << 16) / 255;
			int fG = (multColor.G << 16) / 255;
			int fB = (multColor.B << 16) / 255;

			for (int i = 0, count = palette.Length; i < count; i += 4) {
				colors.Add(Color.FromArgb(
					(byte)(palette[i + 3] * fA >> 16), 
					(byte)(palette[i + 0] * fR >> 16), 
					(byte)(palette[i + 1] * fG >> 16), 
					(byte)(palette[i + 2] * fB >> 16)));
			}

			return colors;
		}
	}
}
