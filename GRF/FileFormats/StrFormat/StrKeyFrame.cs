using System.IO;
using GRF.Graphics;
using GRF.IO;

namespace GRF.FileFormats.StrFormat {
	/// <summary>
	/// STR key frame
	/// </summary>
	public class StrKeyFrame : IWriteableObject {
		public StrKeyFrame() {
			UVs = new float[8];
			Positions = new float[8];
			Color = new float[4];
			BezierPositions = new float[4];

			Offset = new TkVector2(Str.OffsetX, Str.OffsetY);

			UVs[0] = 0;
			UVs[1] = 0;
			UVs[2] = 1;
			UVs[3] = 1;
			UVs[4] = 0;
			UVs[5] = 0;
			UVs[6] = 1;
			UVs[7] = 1;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrKeyFrame" /> class.
		/// </summary>
		/// <param name="reader">The reader.</param>
		public StrKeyFrame(IBinaryReader reader) {
			FrameIndex = reader.Int32();
			Type = reader.Int32();
			Offset = reader.Vector2();
			UVs = reader.ArrayFloat(8);
			Positions = reader.ArrayFloat(8);
			TextureIndex = reader.Float();
			AnimationType = (AnimationType)reader.Int32();
			Delay = reader.Float();

			if (float.IsInfinity(Delay) || float.IsNaN(Delay)) {
				Delay = 0;
			}

			Angle = reader.Float() / (1024 / 360f);
			Color = reader.ArrayFloat(4);
			BlendSrc = reader.Int32();
			BlendDst = reader.Int32();
			MtPresent = reader.Int32();
			BezierPositions = new float[4];
		}

		public StrKeyFrame(StrKeyFrame frame) {
			UVs = new float[8];
			Positions = new float[8];
			Color = new float[4];
			BezierPositions = new float[4];

			FrameIndex = frame.FrameIndex;
			Type = frame.Type;
			Offset = frame.Offset;

			for (int i = 0; i < 8; i++) {
				UVs[i] = frame.UVs[i];
				Positions[i] = frame.Positions[i];
			}

			for (int i = 0; i < 4; i++) {
				Color[i] = frame.Color[i];
				BezierPositions[i] = frame.BezierPositions[i];
			}

			TextureIndex = frame.TextureIndex;
			AnimationType = frame.AnimationType;
			Delay = frame.Delay;
			Angle = frame.Angle;
			BlendSrc = frame.BlendSrc;
			BlendDst = frame.BlendDst;
			MtPresent = frame.MtPresent;
			IsInterpolated = frame.IsInterpolated;
			ScaleBias = frame.ScaleBias;
			AngleBias = frame.AngleBias;
			OffsetBias = frame.OffsetBias;
		}

		public int FrameIndex;
		public int Type;
		public TkVector2 Offset;
		public float[] UVs;
		public float[] Positions;
		public float TextureIndex;
		public AnimationType AnimationType;
		public float Delay;
		public float Angle;
		public float[] Color;
		public int BlendSrc;
		public int BlendDst;
		public int MtPresent;
		public float[] BezierPositions;
		public bool IsInterpolated;

		public float AngleBias;
		public float OffsetBias;
		public float ScaleBias;

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
			keyFrame.Positions[0] = -64;
			keyFrame.Positions[4] = -64;
			keyFrame.Positions[1] = 64;
			keyFrame.Positions[5] = -64;
			keyFrame.Positions[2] = 64;
			keyFrame.Positions[6] = 64;
			keyFrame.Positions[3] = -64;
			keyFrame.Positions[7] = 64;

			keyFrame.UVs[0] = 0;
			keyFrame.UVs[1] = 0;
			keyFrame.UVs[2] = 1;
			keyFrame.UVs[3] = 1;
			keyFrame.UVs[4] = 0;
			keyFrame.UVs[5] = 0;
			keyFrame.UVs[6] = 1;
			keyFrame.UVs[7] = 1;

			keyFrame.Color[0] = 255;
			keyFrame.Color[1] = 255;
			keyFrame.Color[2] = 255;
			keyFrame.Color[3] = 255;

			keyFrame.Offset = new TkVector2(Str.OffsetX, Str.OffsetY);
			keyFrame.BlendSrc = 5;
			keyFrame.BlendDst = 7;
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
			for (int i = 0; i < 8; i++) writer.Write(UVs[i]);
			for (int i = 0; i < 8; i++) writer.Write(Positions[i]);
			writer.Write(TextureIndex);
			writer.Write((int)AnimationType);

			if (float.IsInfinity(Delay) || float.IsNaN(Delay)) {
				Delay = 0;
			}

			writer.Write(Delay);
			writer.Write(Angle * (1024 / 360f));
			for (int i = 0; i < 4; i++) writer.Write(Color[i]);
			writer.Write(BlendSrc);
			writer.Write(BlendDst);
			writer.Write(MtPresent);
		}

		#endregion

		public override string ToString() {
			return "Frame = " + FrameIndex + "; Type = " + Type + "; (" + Offset + ")";
		}

		public void Translate(float x, float y) {
			Offset = new TkVector2(Offset.X + x, Offset.Y + y);
		}

		public void Scale(float scale) {
			for (int i = 0; i < 8; i++) {
				Positions[i] *= scale;
			}
		}

		public void Scale(float x, float y) {
			for (int i = 0; i < 4; i++) {
				Positions[i] *= x;
			}

			for (int i = 4; i < 8; i++) {
				Positions[i] *= y;
			}
		}

		public void Rotate(float angle) {
			Angle += angle;
		}
	}
}