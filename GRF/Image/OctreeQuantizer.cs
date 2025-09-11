using System;
using System.Collections.Generic;

namespace GRF.Image {
	public class OctreeQuantizer {
		private class OctreeNode {
			public bool IsLeaf;
			public bool IsPendingDelete;
			public long PixelCount;
			public _GrfColorRgbLong Rgb;
			public _GrfColorXyz Xyz;
			public OctreeNode[] Children;
			public bool IsReserved;
			public bool IsRgb = true;
		}

		private OctreeNode _root;
		private List<OctreeNode>[] _levels;
		private int _leafCount;
		private bool _addFixedColor;
		private HashSet<int> _reservedColors;

		public const int MaxDepth = 8;
		public const int ChildrenSize = 8;
		public GrfColorMode ColorMode = GrfColorMode.Rgb;

		public OctreeQuantizer() {
			_root = new OctreeNode();
			_levels = new List<OctreeNode>[MaxDepth];

			for (int i = 0; i < _levels.Length; i++)
				_levels[i] = new List<OctreeNode>();
		}

		public void AddColor(GrfColor color, long count) {
			if (color.A == 0)
				return;

			_addColor(_root, color.R << 16 | color.G << 8 | color.B, count, 0);
		}

		public void AddColor(int color, long count) {
			_addColor(_root, color, count, 0);
		}

		public void AddColor(GrfColor color) {
			AddColor(color, 1);
		}

		public void AddColor(int color) {
			AddColor(color, 1);
		}

		public void AddImage(GrfImage image) {
			if (image.GetBpp() != 4) {
				image = image.Copy();
				image.Convert(GrfImageType.Bgra32);
			}

			Dictionary<int, long> colors = new Dictionary<int, long>();

			unsafe {
				fixed (byte* pBase = image.Pixels) {
					byte* p = pBase;
					byte* pEnd = pBase + image.Pixels.Length;

					while (p < pEnd) {
						byte a = p[3];
						int key = 0;

						if (a == 0) {
							p += 4;
							continue;
						}

						if (a != 255) {
							int m = (255 - a) * 255;
							p[0] = (byte)((m + a * p[0]) / 255);
							p[1] = (byte)((m + a * p[1]) / 255);
							p[2] = (byte)((m + a * p[2]) / 255);
							p[3] = 255;
						}

						key = (p[2] << 16) | (p[1] << 8) | p[0];
						long count;

						if (colors.TryGetValue(key, out count))
							colors[key] = count + 1;
						else
							colors[key] = 1;

						p += 4;
					}
				}
			}

			foreach (var c in colors)
				AddColor(c.Key, c.Value);
		}

		private void _addColor(OctreeNode node, int color, long count, int level) {
			if (node.IsLeaf) {
				if (_addFixedColor)
					node.IsReserved = true;

				node.PixelCount += count;
				node.Rgb.R += ((color & 0xFF0000) >> 16) * count;
				node.Rgb.G += ((color & 0x00FF00) >> 8) * count;
				node.Rgb.B += (color & 0x0000FF) * count;
			}
			else {
				int index = _getIndex(color, level);

				if (node.Children == null)
					node.Children = new OctreeNode[ChildrenSize];

				if (node.Children[index] == null) {
					var child = new OctreeNode();
					node.Children[index] = child;

					int childLevel = level + 1;

					if (childLevel == MaxDepth) {
						child.IsLeaf = true;
						_leafCount++;
					}
					else {
						_levels[childLevel].Add(child);
					}
				}

				_addColor(node.Children[index], color, count, level + 1);
			}
		}

		private void _removeColor(OctreeNode node, int color, int level) {
			if (node.IsLeaf) {
				node.IsPendingDelete = true;
				return;
			}
			else {
				int index = _getIndex(color, level);

				if (node.Children == null || node.Children[index] == null) {
					// Shouldn't happen
					return;
				}

				if (node.Children[index].IsLeaf) {
					_leafCount--;
				}

				_removeColor(node.Children[index], color, level + 1);

				int childrenCount = 0;
				for (int i = 0; i < ChildrenSize; i++) {
					if (node.Children[i] != null) {
						if (node.Children[i].IsPendingDelete) {
							node.Children[i] = null;
						}
						else {
							childrenCount++;
						}
					}
				}

				// Cannot be reduced anymore
				if (childrenCount == 0) {
					if (level < MaxDepth)
						_levels[level].Remove(node);

					node.IsPendingDelete = true;
				}
			}
		}

		private int _getIndex(int color, int level) {
			int shift = 7 - level;
			int r = (((color & 0xFF0000) >> 16) >> shift) & 1;
			int g = (((color & 0x00FF00) >> 8) >> shift) & 1;
			int b = ((color & 0x0000FF) >> shift) & 1;
			return (r << 2) | (g << 1) | b;
		}

		public void ReduceTree(int maxColors) {
			_reduceTree(maxColors);
		}

		private void _reduceTree(int maxColors) {
			if (_reservedColors != null) {
				foreach (var color in _reservedColors)
					_removeColor(_root, color, 0);
			}

			if (_leafCount <= maxColors)
				return;

			for (int level = MaxDepth - 1; level >= 0; level--) {
				var list = _levels[level];

				for (int i = list.Count - 1; i >= 0; i--) {
					var node = list[i];

					if (node.Children == null) {
						list.RemoveAt(i);
						continue;
					}

					_GrfColorRgbLong rgb = new _GrfColorRgbLong();
					_GrfColorXyz xyz = new _GrfColorXyz();
					long count = 0;
					int leavesUnder = 0;

					for (int c = 0; c < ChildrenSize; c++) {
						var child = node.Children[c];
						if (child == null)
							continue;

						switch (ColorMode) {
							case GrfColorMode.Rgb:
								rgb.Add(child.Rgb);
								break;
							case GrfColorMode.Lab:
								if (child.IsRgb) {
									var tXyz = _GrfColorXyz.From(child.Rgb.R / child.PixelCount, child.Rgb.G / child.PixelCount, child.Rgb.B / child.PixelCount);
									tXyz.X *= child.PixelCount;
									tXyz.Y *= child.PixelCount;
									tXyz.Z *= child.PixelCount;
									xyz.Add(tXyz);
									break;
								}

								xyz.Add(child.Xyz);
								break;
						}
						count += child.PixelCount;

						leavesUnder += _countLeaves(child);

						if (!child.IsLeaf) {
							int childLevel = level + 1;
							_levels[childLevel].Remove(child);
						}

						node.Children[c] = null;
					}

					node.Children = null;
					node.IsLeaf = true;

					switch (ColorMode) {
						case GrfColorMode.Rgb:
							node.Rgb = rgb;
							break;
						case GrfColorMode.Lab:
							node.Xyz = xyz;
							node.IsRgb = false;
							break;
					}
					node.PixelCount = count;

					_leafCount -= (leavesUnder - 1);

					list.RemoveAt(i);

					if (_leafCount <= maxColors)
						return;
				}
			}
		}

		private int _countLeaves(OctreeNode node) {
			if (node == null)
				return 0;
			if (node.IsLeaf)
				return 1;

			int sum = 0;
			if (node.Children != null) {
				for (int i = 0; i < ChildrenSize; i++)
					if (node.Children[i] != null)
						sum += _countLeaves(node.Children[i]);
			}

			return sum;
		}

		public List<int> GeneratePaletteRgbInt(int maxColors) {
			maxColors = _reservedColors != null ? maxColors - _reservedColors.Count : maxColors;

			if (_leafCount > maxColors)
				_reduceTree(maxColors);

			List<int> palette = new List<int>();
			palette.Add(255 << 16 | 255);
			_assignPaletteIndex(_root, palette);

			if (_reservedColors != null)
				palette.AddRange(_reservedColors);
			return palette;
		}

		private void _assignPaletteIndex(OctreeNode node, List<int> palette) {
			if (node == null)
				return;

			if (node.IsLeaf && !node.IsReserved) {
				if (node.PixelCount > 0) {
					if (ColorMode == GrfColorMode.Lab) {
						var rgb = _GrfColorRgb.From(new _GrfColorXyz(node.Xyz.X / node.PixelCount, node.Xyz.Y / node.PixelCount, node.Xyz.Z / node.PixelCount));
						palette.Add(rgb.R << 16 | rgb.G << 8 | rgb.B);
						return;
					}

					palette.Add((byte)(node.Rgb.R / node.PixelCount) << 16 | (byte)(node.Rgb.G / node.PixelCount) << 8 | (byte)(node.Rgb.B / node.PixelCount));
				}
			}
			else if (node.Children != null) {
				for (int i = 0; i < ChildrenSize; i++)
					if (node.Children[i] != null)
						_assignPaletteIndex(node.Children[i], palette);
			}
		}

		/// <summary>
		/// Sets colors from a palette that cannot be added to the octree.
		/// </summary>
		/// <param name="colors">The colors.</param>
		public void SetReservedColors(HashSet<int> colors) {
			_reservedColors = colors;

			try {
				_addFixedColor = true;
				foreach (var color in colors)
					AddColor(color, 1);
			}
			finally {
				_addFixedColor = false;
			}
		}

		public List<int> RefinePalette(int maxColors, List<int> palette, HashSet<int> fixedColors) {
			if (palette.Count <= maxColors)
				return palette;

			// Make sure fixed colors are added to the start of the palette
			for (int i = 0; i < palette.Count; i++) {
				if (fixedColors.Contains(palette[i])) {
					palette.RemoveAt(i);
					i--;
				}
			}

			foreach (var color in fixedColors)
				palette.Insert(0, color);

			// Merge close colors
			int collapseThreshold = 1;

			while (palette.Count > maxColors) {
				for (int i = fixedColors.Count; i < palette.Count; i++) {
					for (int j = 0; j < i; j++) {
						int dr = (palette[i] & 0xFF0000) >> 16 - (palette[j] & 0xFF0000) >> 16;
						int dg = (palette[i] & 0x00FF00) >> 8 - (palette[j] & 0x00FF00) >> 8;
						int db = (palette[i] & 0x0000FF) - (palette[j] & 0x0000FF);

						if (Math.Abs(dr) + Math.Abs(dg) + Math.Abs(db) <= collapseThreshold) {
							palette.RemoveAt(i);
							i--;
							break;
						}
					}

					if (palette.Count <= maxColors)
						return palette;
				}

				collapseThreshold++;
			}

			return palette;
		}
	}
}
