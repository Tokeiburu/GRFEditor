using System;
using GRF.Graphics;

namespace GRF.FileFormats.StrFormat {
	public class InterpolatedKeyFrame {
		public float Angle;
		public float Delay;
		public float DelayStrKeyFrame;
		public float[] Color = new float[4];
		public TkVector2 Offset;
		public TkVector2 Scale;
		public int TextureIndex;
		public int TextureIndexStrKeyFrame;
		public int AnimationType;
		public int SourceAlpha;
		public int DestinationAlpha;
		public int FrameIndex;
		public float[] Vertices = new float[8];
		public float[] TextCoords = new float[8];
		public float[] Bezier = new float[4];
		public float[] BezierAdjacent = new float[4];
		public float OffsetBias;
		public float ScaleBias;
		public float AngleBias;

		public bool Interpolated;
		public StrKeyFrame KeyFrame;
		public StrKeyFrame InterpolateBaseKeyFrame;
		public StrKeyFrame InterpolateMidKeyFrame;
		public StrKeyFrame InterpolateNextKeyFrame;
		public int LayerIdx;
		public int KeyIndex;

		public InterpolatedKeyFrame() {
		}

		public InterpolatedKeyFrame(InterpolatedKeyFrame keyFrame) {
			Offset = keyFrame.Offset;
			Angle = keyFrame.Angle;
			Scale = keyFrame.Scale;
		}

		public bool Dirty { get; set; }

		public StrKeyFrame ToKeyFrame(int type = 0) {
			StrKeyFrame frame = new StrKeyFrame();
			frame.Angle = Angle;
			frame.TextureIndex = TextureIndexStrKeyFrame;
			//frame.AnimationType

			for (int i = 0; i < 4; i++) {
				frame.Color[i] = Color[i];
				frame.Bezier[i] = Bezier[i];
			}

			for (int i = 0; i < 8; i++) {
				frame.Xy[i] = Vertices[i];
				frame.Uv[i] = TextCoords[i];
			}

			frame.AnimationType = AnimationType;
			frame.FrameIndex = FrameIndex;
			frame.Offset = new TkVector2(Offset.X, Offset.Y);
			frame.OffsetBias = OffsetBias;
			frame.ScaleBias = ScaleBias;
			frame.AngleBias = AngleBias;
			frame.TextureIndex = TextureIndex;

			if (type == 0 || type == 2) {
				frame.SourceAlpha = SourceAlpha;
				frame.DestinationAlpha = DestinationAlpha;
			}

			frame.Type = 0;
			frame.Delay = DelayStrKeyFrame;

			return frame;
		}

		private static float _ease(float v0, float v1, float t, float bias, float subMult) {
			if (bias > 0) {
				return (v1 - v0) * (float)Math.Pow(t, 1 + bias / 5) + v0 * subMult;
			}
			else if (bias < 0) {
				return (v1 - v0) * (1 - (float)Math.Pow(1 - t, -bias / 5 + 1)) + v0 * subMult;
			}
			else {
				return (v1 - v0) * t + v0 * subMult;
			}
		}

		public static float EaseTime(float t, float bias) {
			if (bias > 0) {
				return (float)Math.Pow(t, 1 + bias / 5);
			}
			else if (bias < 0) {
				return (1 - (float)Math.Pow(1 - t, -bias / 5 + 1));
			}
			else {
				return t;
			}
		}

		public static InterpolatedKeyFrame InterpolateSubOffsetsOnly(Str str, int layerIdx, int frameIdx, StrKeyFrame frame0, StrKeyFrame frame1, bool interpolationOnly = false) {
			StrKeyFrame[] frames = { frame0, frame1 };
			InterpolatedKeyFrame inter = new InterpolatedKeyFrame();

			inter.LayerIdx = layerIdx;
			inter.KeyIndex = -1;
			inter.FrameIndex = interpolationOnly ? frame0.FrameIndex : frameIdx;

			if (frame1 == null) {
				inter.Offset = new TkVector2(frames[0].Offset.X, frames[0].Offset.Y);
				inter.Interpolated = true;
				return inter;
			}

			var time = 1d / (frames[1].FrameIndex - frames[0].FrameIndex) * (frameIdx - frames[0].FrameIndex);
			float subMult = 1f;

			if (interpolationOnly) {
				time = 1d / (frames[1].FrameIndex - frames[0].FrameIndex);
				subMult = 0;
			}

			time = EaseTime((float)time, frames[0].OffsetBias);

			TkVector2 p1 = frames[0].Offset + new TkVector2(frames[0].Bezier[2], frames[0].Bezier[3]);
			TkVector2 p2 = frames[1].Offset + new TkVector2(frames[1].Bezier[0], frames[1].Bezier[1]);

			if (frames[0].Bezier[2] != 0 || frames[0].Bezier[3] != 0 || frames[1].Bezier[0] != 0 || frames[1].Bezier[1] != 0) {
				inter.Offset = _getBezier((float)time, frames[0].Offset, p1, p2, frames[1].Offset);
			}
			else {
				inter.Offset = new TkVector2((frames[1].Offset.X - frames[0].Offset.X) * time + frames[0].Offset.X * subMult, (frames[1].Offset.Y - frames[0].Offset.Y) * time + frames[0].Offset.Y * subMult);
			}

			inter.Interpolated = true;

			return inter;
		}

		public static InterpolatedKeyFrame InterpolateSub(Str str, int layerIdx, int frameIdx, StrKeyFrame frame0, StrKeyFrame frame1, bool interpolationOnly = false) {
			StrKeyFrame[] frames = { frame0, frame1 };
			InterpolatedKeyFrame inter = new InterpolatedKeyFrame();

			inter.LayerIdx = layerIdx;
			inter.KeyIndex = -1;
			inter.FrameIndex = interpolationOnly ? frame0.FrameIndex : frameIdx;

			if (frame1 == null) {
				inter.Angle = frames[0].Angle;
				inter.Color[0] = frames[0].Color[0];
				inter.Color[1] = frames[0].Color[1];
				inter.Color[2] = frames[0].Color[2];
				inter.Color[3] = frames[0].Color[3];

				for (int i = 0; i < 8; i++) {
					inter.Vertices[i] = frames[0].Xy[i];
					inter.TextCoords[i] = frames[0].Uv[i];
				}

				for (int i = 0; i < 4; i++) {
					inter.Bezier[i] = frames[0].Bezier[i];
				}

				inter.AngleBias = frames[0].AngleBias;
				inter.OffsetBias = frames[0].OffsetBias;
				inter.ScaleBias = frames[0].ScaleBias;
				inter.Delay = frames[0].Delay;
				inter.AnimationType = frames[0].AnimationType;
				inter.TextureIndex = (int)frames[0].TextureIndex;
				inter.SourceAlpha = frames[0].SourceAlpha;
				inter.DestinationAlpha = frames[0].DestinationAlpha;
				inter.Offset = new TkVector2(frames[0].Offset.X, frames[0].Offset.Y);
				inter.KeyIndex = str.Layers[layerIdx].FrameIndex2KeyIndex[frameIdx];
				inter.KeyFrame = frames[0];
				inter.Interpolated = true;
				return inter;
			}

			var time = 1d / (frames[1].FrameIndex - frames[0].FrameIndex) * (frameIdx - frames[0].FrameIndex);
			float subMult = 1f;

			if (interpolationOnly) {
				time = 1d / (frames[1].FrameIndex - frames[0].FrameIndex);
				subMult = 0;
			}

			inter.Angle = _ease(frames[0].Angle, frames[1].Angle, (float)time, frames[0].AngleBias, subMult);
			inter.Color[0] = (float)((frames[1].Color[0] - frames[0].Color[0]) * time + frames[0].Color[0] * subMult);
			inter.Color[1] = (float)((frames[1].Color[1] - frames[0].Color[1]) * time + frames[0].Color[1] * subMult);
			inter.Color[2] = (float)((frames[1].Color[2] - frames[0].Color[2]) * time + frames[0].Color[2] * subMult);
			inter.Color[3] = (float)((frames[1].Color[3] - frames[0].Color[3]) * time + frames[0].Color[3] * subMult);

			for (int i = 0; i < 8; i++) {
				inter.Vertices[i] = _ease(frames[0].Xy[i], frames[1].Xy[i], (float)time, frames[0].ScaleBias, subMult);
				inter.TextCoords[i] = (float)((frames[1].Uv[i] - frames[0].Uv[i]) * time + frames[0].Uv[i] * subMult);
			}

			inter.TextureIndex = (int)frames[0].TextureIndex;

			if (frames[0].AnimationType == 3 || frames[0].AnimationType == 2) {
				var rate = frames[0].Delay * (frameIdx - frames[0].FrameIndex) + frames[0].TextureIndex;
				inter.TextureIndex = (int)rate;

				if (frames[0].AnimationType == 2 && inter.TextureIndex >= str[layerIdx].TextureNames.Count) {
					inter.TextureIndex = str[layerIdx].TextureNames.Count - 1;
				}
				else {
					inter.TextureIndex = ((int)rate) % str[layerIdx].TextureNames.Count;
				}
			}

			inter.Delay = frames[0].Delay;
			inter.DelayStrKeyFrame = frames[0].Delay;
			inter.AnimationType = frames[0].AnimationType;
			inter.TextureIndexStrKeyFrame = 0;
			inter.SourceAlpha = frames[0].SourceAlpha;
			inter.DestinationAlpha = frames[0].DestinationAlpha;

			time = EaseTime((float)time, frames[0].OffsetBias);

			TkVector2 p1 = frames[0].Offset + new TkVector2(frames[0].Bezier[2], frames[0].Bezier[3]);
			TkVector2 p2 = frames[1].Offset + new TkVector2(frames[1].Bezier[0], frames[1].Bezier[1]);

			if (frames[0].Bezier[2] != 0 || frames[0].Bezier[3] != 0 || frames[1].Bezier[0] != 0 || frames[1].Bezier[1] != 0) {
				inter.Offset = _getBezier((float)time, frames[0].Offset, p1, p2, frames[1].Offset);
				
				// Bezier property fix
				TkVector2 p0 = frames[0].Offset;
				TkVector2 p3 = frames[1].Offset;
			
				TkVector2 p0_i = (p1 - p0) * (float)time + p0 * subMult;
				TkVector2 p1_i = (p2 - p1) * (float)time + p1 * subMult;
				TkVector2 p2_i = (p3 - p2) * (float)time + p2 * subMult;
			
				TkVector2 p0_ii = (p1_i - p0_i) * (float)time + p0_i * subMult - inter.Offset;
				TkVector2 p1_ii = (p2_i - p1_i) * (float)time + p1_i * subMult - inter.Offset;
			
				inter.Bezier[0] = p0_ii.X;
				inter.Bezier[1] = p0_ii.Y;
				inter.Bezier[2] = p1_ii.X;
				inter.Bezier[3] = p1_ii.Y;

				inter.BezierAdjacent[0] = p0_i.X - p0.X;
				inter.BezierAdjacent[1] = p0_i.Y - p0.Y;
				inter.BezierAdjacent[2] = p2_i.X - p3.X;
				inter.BezierAdjacent[3] = p2_i.Y - p3.Y;
			}
			else {
				inter.Offset = new TkVector2((frames[1].Offset.X - frames[0].Offset.X) * time + frames[0].Offset.X * subMult, (frames[1].Offset.Y - frames[0].Offset.Y) * time + frames[0].Offset.Y * subMult);
			}

			inter.Interpolated = true;
			return inter;
		}

		private static TkVector2 _getBezier(float t, TkVector2 p0, TkVector2 p1, TkVector2 p2, TkVector2 p3) {
			float cx = 3 * (p1.X - p0.X);
			float cy = 3 * (p1.Y - p0.Y);
			float bx = 3 * (p2.X - p1.X) - cx;
			float by = 3 * (p2.Y - p1.Y) - cy;
			float ax = p3.X - p0.X - cx - bx;
			float ay = p3.Y - p0.Y - cy - by;
			float Cube = t * t * t;
			float Square = t * t;
			
			float resX = (ax * Cube) + (bx * Square) + (cx * t) + p0.X;
			float resY = (ay * Cube) + (by * Square) + (cy * t) + p0.Y;

			return new TkVector2(resX, resY);
		}

		public static InterpolatedKeyFrame InterpolateOffsetsOnly(Str str, int layerIdx, int frameIdx, StrKeyFrame temporaryKeyFrame, bool interpolationOnly = false) {
			var strFrames = str.Layers[layerIdx].KeyFrames;
			StrKeyFrame[] frames = new StrKeyFrame[2];
			int keyFrameIdx = str.Layers[layerIdx].FrameIndex2KeyIndex[frameIdx];

			if (keyFrameIdx < 0)
				return null;

			frames[0] = strFrames[keyFrameIdx];

			if (interpolationOnly) {
				//if (keyFrameIdx + 1 < strFrames.Count && strFrames[keyFrameIdx + 1].Type == 0) {
				//	frames[1] = strFrames[keyFrameIdx + 1];
				//}
				//else if (keyFrameIdx + 1 < strFrames.Count && strFrames[keyFrameIdx + 1].Type == 1) {
				//	frames[1] = strFrames[keyFrameIdx];
				//
				//	if (keyFrameIdx + 2 < strFrames.Count && strFrames[keyFrameIdx + 2].Type == 0) {
				//		frames[1] = strFrames[keyFrameIdx + 2];
				//	}
				//}
			}
			else {
				if (frames[0].IsInterpolated) {
					frames[1] = frames[0];

					if (keyFrameIdx + 1 < strFrames.Count) {
						frames[1] = strFrames[keyFrameIdx + 1];
					}
					else {
						frames[1] = new StrKeyFrame(frames[0]);
						frames[1].FrameIndex = str.MaxKeyFrame;
					}
				}
				else {
					frames[1] = null;
				}
			}

			if (frames[0] == null) {
				return null;
			}

			InterpolatedKeyFrame inter = new InterpolatedKeyFrame();
			inter.LayerIdx = layerIdx;
			inter.KeyIndex = -1;
			inter.FrameIndex = frameIdx;

			if (temporaryKeyFrame != null) {
				if (temporaryKeyFrame.FrameIndex == frames[0].FrameIndex || temporaryKeyFrame.FrameIndex == frameIdx) {
					frames[0] = temporaryKeyFrame;
				}
				else if (frames[1] != null && frames[0].FrameIndex < temporaryKeyFrame.FrameIndex && temporaryKeyFrame.FrameIndex <= frames[1].FrameIndex) {
					if (frameIdx >= frames[0].FrameIndex && frameIdx < temporaryKeyFrame.FrameIndex) {
						frames[1] = temporaryKeyFrame;
					}
					else if (frameIdx > temporaryKeyFrame.FrameIndex && frameIdx < frames[1].FrameIndex) {
						frames[0] = temporaryKeyFrame;
					}
				}
			}

			if (frames[1] == null || frames[0].FrameIndex == frameIdx) {
				inter.Offset = new TkVector2(frames[0].Offset.X, frames[0].Offset.Y);
			}
			else {
				inter = InterpolateSubOffsetsOnly(str, layerIdx, frameIdx, frames[0], frames[1]);
			}

			return inter;
		}

		public static InterpolatedKeyFrame Interpolate(Str str, int layerIdx, int frameIdx, bool interpolationOnly = false) {
			var strFrames = str.Layers[layerIdx].KeyFrames;
			StrKeyFrame[] frames = new StrKeyFrame[2];
			int keyFrameIdx = str.Layers[layerIdx].FrameIndex2KeyIndex[frameIdx];

			if (keyFrameIdx < 0)
				return null;

			frames[0] = strFrames[keyFrameIdx];

			if (interpolationOnly) {
				//if (keyFrameIdx + 1 < strFrames.Count && strFrames[keyFrameIdx + 1].Type == 0) {
				//	frames[1] = strFrames[keyFrameIdx + 1];
				//}
				//else if (keyFrameIdx + 1 < strFrames.Count && strFrames[keyFrameIdx + 1].Type == 1) {
				//	frames[1] = strFrames[keyFrameIdx];
				//
				//	if (keyFrameIdx + 2 < strFrames.Count && strFrames[keyFrameIdx + 2].Type == 0) {
				//		frames[1] = strFrames[keyFrameIdx + 2];
				//	}
				//}
			}
			else {
				if (frames[0].IsInterpolated) {
					frames[1] = frames[0];

					if (keyFrameIdx + 1 < strFrames.Count) {
						frames[1] = strFrames[keyFrameIdx + 1];
					}
					else {
						frames[1] = new StrKeyFrame(frames[0]);
						frames[1].FrameIndex = str.MaxKeyFrame;
					}
				}
				else {
					frames[1] = null;
				}
			}

			if (frames[0] == null) {
				return null;
			}

			InterpolatedKeyFrame inter = new InterpolatedKeyFrame();
			inter.LayerIdx = layerIdx;
			inter.KeyIndex = -1;
			inter.FrameIndex = frameIdx;

			if (frames[1] == null || frames[0].FrameIndex == frameIdx) {
				inter.Angle = frames[0].Angle;
				inter.Color[0] = frames[0].Color[0];
				inter.Color[1] = frames[0].Color[1];
				inter.Color[2] = frames[0].Color[2];
				inter.Color[3] = frames[0].Color[3];

				for (int i = 0; i < 8; i++) {
					inter.Vertices[i] = frames[0].Xy[i];
					inter.TextCoords[i] = frames[0].Uv[i];
				}

				for (int i = 0; i < 4; i++) {
					inter.Bezier[i] = frames[0].Bezier[i];
				}

				inter.AngleBias = frames[0].AngleBias;
				inter.OffsetBias = frames[0].OffsetBias;
				inter.ScaleBias = frames[0].ScaleBias;
				inter.Delay = frames[0].Delay;
				inter.AnimationType = frames[0].AnimationType;
				inter.TextureIndex = (int)frames[0].TextureIndex;
				inter.SourceAlpha = frames[0].SourceAlpha;
				inter.DestinationAlpha = frames[0].DestinationAlpha;
				inter.Offset = new TkVector2(frames[0].Offset.X, frames[0].Offset.Y);
				inter.KeyIndex = keyFrameIdx;
				inter.KeyFrame = frames[0];
				inter.Interpolated = false;
			}
			else {
				inter = InterpolateSub(str, layerIdx, frameIdx, frames[0], frames[1]);
				inter.KeyIndex = keyFrameIdx;
			}

			return inter;
		}

		public static void ConvertToFrame(InterpolatedKeyFrame currentFrame, Str str, bool adjustBezier = true) {
			if (currentFrame.Interpolated) {
				StrKeyFrame frame = currentFrame.ToKeyFrame();

				int frameIndex = currentFrame.FrameIndex;

				var layer = str[currentFrame.LayerIdx];
				var baseKeyIndex = layer.FrameIndex2KeyIndex[frameIndex];

				if (baseKeyIndex < 0)
					return;

				str.Commands.Begin();

				if (layer[baseKeyIndex].FrameIndex == frameIndex - 1)
					str.Commands.SetInterpolated(currentFrame.LayerIdx, baseKeyIndex, false);

				if (layer[baseKeyIndex + 1] != null && layer[baseKeyIndex + 1].FrameIndex != frameIndex + 1)
					frame.IsInterpolated = true;

				if (adjustBezier && (Math.Abs(currentFrame.BezierAdjacent[0]) > 0.05 ||
				    Math.Abs(currentFrame.BezierAdjacent[1]) > 0.05 ||
				    Math.Abs(currentFrame.BezierAdjacent[2]) > 0.05 ||
				    Math.Abs(currentFrame.BezierAdjacent[3]) > 0.05) &&
				    baseKeyIndex + 1 < str[currentFrame.LayerIdx].KeyFrames.Count) {
					str.Commands.SetBezier(currentFrame.LayerIdx, baseKeyIndex, new float[] {
						str[currentFrame.LayerIdx, baseKeyIndex].Bezier[0],
						str[currentFrame.LayerIdx, baseKeyIndex].Bezier[1],
						currentFrame.BezierAdjacent[0],
						currentFrame.BezierAdjacent[1],
					});

					str.Commands.SetBezier(currentFrame.LayerIdx, baseKeyIndex + 1, new float[] {
						currentFrame.BezierAdjacent[2],
						currentFrame.BezierAdjacent[3],
						str[currentFrame.LayerIdx, baseKeyIndex + 1].Bezier[2],
						str[currentFrame.LayerIdx, baseKeyIndex + 1].Bezier[3],
					});
				}

				str.Commands.AddKey(currentFrame.LayerIdx, baseKeyIndex + 1, frame);
				currentFrame.KeyIndex = baseKeyIndex + 1;
				currentFrame.Interpolated = false;
			}
		}
	}
}
