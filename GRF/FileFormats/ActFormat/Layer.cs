using System;
using System.IO;
using GRF.FileFormats.SprFormat;
using GRF.Graphics;
using GRF.Image;

namespace GRF.FileFormats.ActFormat {
	public enum SpriteTypes {
		Indexed8 = 0,
		Bgra32 = 1,
	}

	[Serializable]
	public class Layer {
		/// <summary>
		/// Gets or sets the color mask
		/// </summary>
		public GrfColor Color = new GrfColor();

		public int Height;
		public bool Mirror;
		public int OffsetX;
		public int OffsetY;

		public int SpriteIndex;
		public SpriteTypes SpriteType;
		public int Width;
		private int _rotation;
		private float _scaleX = 1f;
		private float _scaleY = 1f;

		internal Layer() {
		}

		public Layer(int relativeIndex, GrfImage image) {
			Color = new GrfColor(255, 255, 255, 255);
			ScaleX = 1;
			ScaleY = 1;
			Width = image.Width;
			Height = image.Height;
			SpriteIndex = relativeIndex;
			SpriteType = image.GrfImageType == GrfImageType.Indexed8 ? SpriteTypes.Indexed8 : SpriteTypes.Bgra32;
		}

		public Layer(int absoluteIndex, Spr spr) : this(spr.AbsoluteToRelative(absoluteIndex, spr.Images[absoluteIndex].GrfImageType), spr.Images[absoluteIndex]) {
		}

		public Layer(Layer layer) {
			OffsetY = layer.OffsetY;
			OffsetX = layer.OffsetX;
			SpriteType = layer.SpriteType;
			SpriteIndex = layer.SpriteIndex;
			Mirror = layer.Mirror;
			Rotation = layer.Rotation;
			Width = layer.Width;
			Height = layer.Height;
			ScaleX = layer.ScaleX;
			ScaleY = layer.ScaleY;
			Color = new GrfColor(layer.Color);
		}

		public int Rotation {
			get { return _rotation; }
			set {
				value %= 360;

				if (value >= 360) {
					value -= 360;
				}

				if (value < 0) {
					value += 360;
				}

				_rotation = value;
			}
		}

		public double RotationRadian {
			get { return _rotation * Math.PI / 180d; }
			set { Rotation = (int) (value * 180d / Math.PI); }
		}

		public float ScaleX {
			get { return _scaleX; }
			set {
				if (value > float.MaxValue)
					value = float.MaxValue;

				if (value < float.MinValue)
					value = float.MinValue;

				_scaleX = value;
			}
		}

		public float ScaleY {
			get { return _scaleY; }
			set {
				if (value > float.MaxValue)
					value = float.MaxValue;

				if (value < float.MinValue)
					value = float.MinValue;

				_scaleY = value;
			}
		}

		public int SpriteTypeInt {
			get { return (int) SpriteType; }
			set { SpriteType = (SpriteTypes) value; }
		}

		protected bool Equals(Layer other) {
			return Equals(Color, other.Color) && Mirror.Equals(other.Mirror) && OffsetX == other.OffsetX && OffsetY == other.OffsetY && SpriteIndex == other.SpriteIndex && SpriteType.Equals(other.SpriteType) && _scaleX.Equals(other._scaleX) && _scaleY.Equals(other._scaleY) && _rotation == other._rotation && Width == other.Width && Height == other.Height;
		}

		public override int GetHashCode() {
			unchecked {
				int hashCode = (Color != null ? Color.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ Mirror.GetHashCode();
				hashCode = (hashCode * 397) ^ OffsetX;
				hashCode = (hashCode * 397) ^ OffsetY;
				hashCode = (hashCode * 397) ^ SpriteIndex;
				hashCode = (hashCode * 397) ^ SpriteType.GetHashCode();
				hashCode = (hashCode * 397) ^ _scaleX.GetHashCode();
				hashCode = (hashCode * 397) ^ _scaleY.GetHashCode();
				hashCode = (hashCode * 397) ^ _rotation;
				hashCode = (hashCode * 397) ^ Width;
				hashCode = (hashCode * 397) ^ Height;
				return hashCode;
			}
		}

		public void Scale(float scale) {
			Scale(scale, scale);
		}

		public void Scale(float scaleX, float scaleY) {
			ScaleX = ScaleX * scaleX;
			ScaleY = ScaleY * scaleY;
		}

		public void Translate(int x, int y) {
			OffsetX += x;
			OffsetY += y;
		}

		public void TranslateX(int x) {
			OffsetX += x;
		}

		public void TranslateY(int y) {
			OffsetY += y;
		}

		public void Rotate(int amount) {
			Rotation += amount;
		}

		public void Rotate(int amount, TkVector2 center) {
			TkVector2 vertex = new TkVector2(OffsetX, OffsetY);
			vertex.RotateZ(-amount, center);
			OffsetX = (int) Math.Round(vertex.X, MidpointRounding.AwayFromZero);
			OffsetY = (int) Math.Round(vertex.Y, MidpointRounding.AwayFromZero);
			Rotation += amount;
		}

		public void ApplyMirror(bool mirror) {
			Mirror = mirror;
		}

		public void ApplyMirror() {
			Mirror = !Mirror;
		}

		public void SetColor(string color) {
			SetColor(new GrfColor(color));
		}

		public void SetColor(GrfColor color) {
			Color = color;
		}

		public void Write(BinaryWriter writer, Spr sprite) {
			writer.Write(OffsetX);
			writer.Write(OffsetY);
			writer.Write(SpriteIndex);
			writer.Write(Mirror ? 1 : 0);
			Color.Write(writer);

			writer.Write(ScaleX);
			writer.Write(ScaleY);
			writer.Write(Rotation);
			writer.Write((int) SpriteType);

			if (sprite != null) {
				GrfImage image = sprite.GetImage(this);

				if (SpriteIndex < 0) {
					writer.Write(0);
					writer.Write(0);
				}
				else if (image != null) {
					writer.Write(image.Width);
					writer.Write(image.Height);
				}
				else {
					writer.Write(Width);
					writer.Write(Height);
				}
			}
			else {
				writer.Write(Width);
				writer.Write(Height);
			}
		}

		public bool IsIndexed8() {
			return SpriteType == SpriteTypes.Indexed8;
		}

		public bool IsBgra32() {
			return SpriteType == SpriteTypes.Bgra32;
		}

		/// <summary>
		/// Gets the image used by the layer
		/// </summary>
		/// <param name="sprite">The sprite.</param>
		/// <returns>The image used by the layer; null if failed to retrieve</returns>
		public GrfImage GetImage(Spr sprite) {
			return sprite.GetImage(this);
		}

		/// <summary>
		/// Gets the absolute sprite index.
		/// </summary>
		/// <param name="sprite">The sprite.</param>
		/// <returns>The absolute sprite index</returns>
		public int GetAbsoluteSpriteId(Spr sprite) {
			return IsIndexed8() ? SpriteIndex : SpriteIndex + sprite.NumberOfIndexed8Images;
		}

		public void SetAbsoluteSpriteId(int index, Spr sprite) {
			if (index < sprite.NumberOfIndexed8Images) {
				SpriteType = SpriteTypes.Indexed8;
				SpriteIndex = index;
			}
			else {
				SpriteType = SpriteTypes.Bgra32;
				SpriteIndex = index - sprite.NumberOfIndexed8Images;
			}
		}

		public bool IsImageTypeValid(GrfImage image) {
			switch (image.GrfImageType) {
				case GrfImageType.Indexed8:
					if (IsIndexed8()) {
						return true;
					}
					break;
				case GrfImageType.Bgra32:
					if (IsBgra32())
						return true;
					break;
			}

			return false;
		}

		public override bool Equals(object obj) {
			var layer = obj as Layer;

			if (layer != null) {
				return Equals(layer);
			}

			return false;
		}

		public override string ToString() {
			return "Offsets (" + OffsetX + ", " + OffsetY + "), Scale (" + ScaleX + ", " + ScaleY + ")";
		}

		public void Magnify(float value) {
			Scale(value);
			OffsetX = (int) (value * OffsetX);
			OffsetY = (int) (value * OffsetY);
		}
	}
}