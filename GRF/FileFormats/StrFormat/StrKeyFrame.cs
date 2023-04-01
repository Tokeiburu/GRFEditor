using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.StrFormat {
	/// <summary>
	/// STR key frame
	/// </summary>
	public class StrKeyFrame : IWriteableObject {
		public StrKeyFrame() {
			Uv = new float[8];
			Xy = new float[8];
			Color = new float[4];
			Bezier = new float[4];

			Offset = new Point(319, 291);

			Uv[0] = 0;
			Uv[1] = 0;
			Uv[2] = 1;
			Uv[3] = 1;
			Uv[4] = 0;
			Uv[5] = 0;
			Uv[6] = 1;
			Uv[7] = 1;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrKeyFrame" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public StrKeyFrame(IBinaryReader reader) {
			FrameIndex = reader.Int32();
			Type = reader.Int32();
			Offset = reader.Point();
			Uv = reader.ArrayFloat(8);
			Xy = reader.ArrayFloat(8);
			TextureIndex = reader.Float();
			AnimationType = reader.Int32();
			Delay = reader.Float();

			if (float.IsInfinity(Delay) || float.IsNaN(Delay)) {
				Delay = 0;
			}

			Angle = reader.Float() / (1024 / 360f);
			Color = reader.ArrayFloat(4);
			SourceAlpha = reader.Int32();
			DestinationAlpha = reader.Int32();
			MtPresent = reader.Int32();
			Bezier = new float[4];
		}

		public StrKeyFrame(StrKeyFrame frame) {
			Uv = new float[8];
			Xy = new float[8];
			Color = new float[4];
			Bezier = new float[4];

			FrameIndex = frame.FrameIndex;
			Type = frame.Type;
			Offset = frame.Offset;

			for (int i = 0; i < 8; i++) {
				Uv[i] = frame.Uv[i];
				Xy[i] = frame.Xy[i];
			}

			for (int i = 0; i < 4; i++) {
				Color[i] = frame.Color[i];
				Bezier[i] = frame.Bezier[i];
			}

			TextureIndex = frame.TextureIndex;
			AnimationType = frame.AnimationType;
			Delay = frame.Delay;
			Angle = frame.Angle;
			SourceAlpha = frame.SourceAlpha;
			DestinationAlpha = frame.DestinationAlpha;
			MtPresent = frame.MtPresent;
			IsInterpolated = frame.IsInterpolated;
			ScaleBias = frame.ScaleBias;
			AngleBias = frame.AngleBias;
			OffsetBias = frame.OffsetBias;
		}

		public int FrameIndex { get; set; }
		public int Type { get; set; }
		public Point Offset { get; set; }
		public float[] Uv { get; private set; }
		public float[] Xy { get; private set; }
		public float TextureIndex { get; set; }
		public int AnimationType { get; set; }
		public float Delay { get; set; }
		public float Angle { get; set; }
		public float[] Color { get; private set; }
		public int SourceAlpha { get; set; }
		public int DestinationAlpha { get; set; }
		public int MtPresent { get; set; }
		public float[] Bezier { get; private set; }
		public bool IsInterpolated { get; set; }

		public float AngleBias { get; set; }
		public float OffsetBias { get; set; }
		public float ScaleBias { get; set; }

		internal bool PendingDelete { get; set; }

		public bool Compare(StrLayer keyFrame) {
			return true;
		}

		public static StrKeyFrame CreateInterKeyFrame(StrKeyFrame keyFrameBase) {
			StrKeyFrame keyFrame = new StrKeyFrame();
			keyFrame.Type = 1;
			keyFrame.FrameIndex = keyFrameBase.FrameIndex;
			return keyFrame;
		}

		public static StrKeyFrame CreateInterKeyFrame(int frameIndex) {
			StrKeyFrame keyFrame = new StrKeyFrame();
			keyFrame.Type = 1;
			keyFrame.FrameIndex = frameIndex;
			return keyFrame;
		}

		public static StrKeyFrame CreateDefaultFrame(int frameIndex) {
			StrKeyFrame keyFrame = new StrKeyFrame();
			keyFrame.Type = 0;
			keyFrame.FrameIndex = frameIndex;
			keyFrame.Xy[0] = -64;
			keyFrame.Xy[4] = -64;
			keyFrame.Xy[1] = 64;
			keyFrame.Xy[5] = -64;
			keyFrame.Xy[2] = 64;
			keyFrame.Xy[6] = 64;
			keyFrame.Xy[3] = -64;
			keyFrame.Xy[7] = 64;

			keyFrame.Uv[0] = 0;
			keyFrame.Uv[1] = 0;
			keyFrame.Uv[2] = 1;
			keyFrame.Uv[3] = 1;
			keyFrame.Uv[4] = 0;
			keyFrame.Uv[5] = 0;
			keyFrame.Uv[6] = 1;
			keyFrame.Uv[7] = 1;

			keyFrame.Color[0] = 255;
			keyFrame.Color[1] = 255;
			keyFrame.Color[2] = 255;
			keyFrame.Color[3] = 255;

			keyFrame.Offset = new Point(319, 291);
			keyFrame.SourceAlpha = 5;
			keyFrame.DestinationAlpha = 7;
			return keyFrame;
		}

		#region IWriteableObject Members

		/// <summary>
		/// Writes the specified object to the stream.
		/// </summary>
		/// <param name="writer">The writer.</param>
		public void Write(BinaryWriter writer) {
			writer.Write(FrameIndex);
			writer.Write(Type);
			writer.Write(Offset.X);
			writer.Write(Offset.Y);
			for (int i = 0; i < 8; i++) writer.Write(Uv[i]);
			for (int i = 0; i < 8; i++) writer.Write(Xy[i]);
			writer.Write(TextureIndex);
			writer.Write(AnimationType);

			if (float.IsInfinity(Delay) || float.IsNaN(Delay)) {
				Delay = 0;
			}

			writer.Write(Delay);
			writer.Write(Angle * (1024 / 360f));
			for (int i = 0; i < 4; i++) writer.Write(Color[i]);
			writer.Write(SourceAlpha);
			writer.Write(DestinationAlpha);
			writer.Write(MtPresent);
		}

		#endregion

		public override string ToString() {
			return "Frame = " + FrameIndex + "; Type = " + Type + "; (" + Offset + ")";
		}

		public void Translate(float x, float y) {
			Offset = new Point(Offset.X + x, Offset.Y + y);
		}

		public void Scale(float scale) {
			for (int i = 0; i < 8; i++) {
				Xy[i] *= scale;
			}
		}

		public void Scale(float x, float y) {
			for (int i = 0; i < 4; i++) {
				Xy[i] *= x;
			}

			for (int i = 4; i < 8; i++) {
				Xy[i] *= y;
			}
		}

		public void Rotate(float angle) {
			Angle += angle;
		}
	}
}