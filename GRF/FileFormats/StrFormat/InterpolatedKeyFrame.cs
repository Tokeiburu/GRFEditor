using System;
using GRF.Graphics;

namespace GRF.FileFormats.StrFormat {
	public enum PointId {
		TopLeft,
		TopRight,
		BottomRight,
		BottomLeft,
	}

	public enum UvPointId {
		TopLeft,
		TopRight
	}

	public enum KeyFrameValueType {
		OffsetX,
		OffsetY,
		OffsetXY,
		P1X,
		P1Y,
		P2X,
		P2Y,
		P3X,
		P3Y,
		P4X,
		P4Y,
		P1,
		P2,
		P3,
		P4,
		Points,
		Angle,
		Scale,
		UV1X,
		UV1Y,
		UV2X,
		UV2Y,
		UV1,
		UV2,
		UVs,
		BezierP1,
		BezierP2,
		Bezier,
		OffsetBias,
		ScaleBias,
		AngleBias,
	}

	public enum AnimationType {
		Stop,
		Interpolation,
		Once,
		Loop,
		ReverseLoop,
		BiLoop
	}

	public class InterpolatedKeyFrame {
		public float Angle;
		public float Delay;
		public float DelayStrKeyFrame;
		public float[] Color = new float[4];
		public TkVector2 Offset;
		public TkVector2 Scale;
		public int TextureIndex;
		public int TextureIndexStrKeyFrame;
		public AnimationType AnimationType;
		public int BlendSrc;
		public int BlendDst;
		public int FrameIndex;
		public float[] Positions = new float[8];
		public float[] UVs = new float[8];
		public float[] BezierPositions = new float[4];
		public float[] BezierAdjacentPositions = new float[4];
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

		public delegate void ValueChangedEventHandler(KeyFrameValueType arg);
		public event ValueChangedEventHandler ValueChanged;
		public void OnValueChanged(KeyFrameValueType arg) => ValueChanged?.Invoke(arg);

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

			for (int i = 0; i < 4; i++) {
				frame.Color[i] = Color[i];
				frame.BezierPositions[i] = BezierPositions[i];
			}

			for (int i = 0; i < 8; i++) {
				frame.Positions[i] = Positions[i];
				frame.UVs[i] = UVs[i];
			}

			frame.AnimationType = AnimationType;
			frame.FrameIndex = FrameIndex;
			frame.Offset = new TkVector2(Offset.X, Offset.Y);
			frame.OffsetBias = OffsetBias;
			frame.ScaleBias = ScaleBias;
			frame.AngleBias = AngleBias;
			frame.TextureIndex = TextureIndex;

			if (type == 0 || type == 2) {
				frame.BlendSrc = BlendSrc;
				frame.BlendDst = BlendDst;
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

			TkVector2 p1 = frames[0].Offset + new TkVector2(frames[0].BezierPositions[2], frames[0].BezierPositions[3]);
			TkVector2 p2 = frames[1].Offset + new TkVector2(frames[1].BezierPositions[0], frames[1].BezierPositions[1]);

			if (frames[0].BezierPositions[2] != 0 || frames[0].BezierPositions[3] != 0 || frames[1].BezierPositions[0] != 0 || frames[1].BezierPositions[1] != 0) {
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
					inter.Positions[i] = frames[0].Positions[i];
					inter.UVs[i] = frames[0].UVs[i];
				}

				for (int i = 0; i < 4; i++) {
					inter.BezierPositions[i] = frames[0].BezierPositions[i];
				}

				inter.AngleBias = frames[0].AngleBias;
				inter.OffsetBias = frames[0].OffsetBias;
				inter.ScaleBias = frames[0].ScaleBias;
				inter.Delay = frames[0].Delay;
				inter.AnimationType = frames[0].AnimationType;
				inter.TextureIndex = (int)frames[0].TextureIndex;
				inter.BlendSrc = frames[0].BlendSrc;
				inter.BlendDst = frames[0].BlendDst;
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
				inter.Positions[i] = _ease(frames[0].Positions[i], frames[1].Positions[i], (float)time, frames[0].ScaleBias, subMult);
				inter.UVs[i] = (float)((frames[1].UVs[i] - frames[0].UVs[i]) * time + frames[0].UVs[i] * subMult);
			}

			int textureCount = str[layerIdx].TextureNames.Count;

			switch (frames[0].AnimationType) {
				default:
				case AnimationType.Stop:
					// [KeyFrame.TextureIndex]
					// Uses the keyframe texture continuously.
					inter.TextureIndex = (int)frames[0].TextureIndex;
					break;
				case AnimationType.Interpolation:
					// Unsure, requires more testing.
					// There is no bias setting saved in the STR structure, so this mode is most likely never going to appear.
					// It needs to be rasterized when saving the STR file, and then detected back when loading it. Quite a nightmare,
					// let's just ignore for now.
					inter.TextureIndex = (int)((frames[1].TextureIndex - frames[0].TextureIndex) * time + frames[0].TextureIndex);
					break;
				case AnimationType.Once:
					// [KeyFrame.TextureIndex..Layer.TextureCount]
					// Goes from the keyframe texture to the last texture in the layer's list.
					// Note: It does not care about the next keyframe's texture index at all.
					inter.TextureIndex = (int)(frames[0].Delay * (frameIdx - frames[0].FrameIndex) + frames[0].TextureIndex);

					if (inter.TextureIndex >= str[layerIdx].TextureNames.Count)
						inter.TextureIndex = str[layerIdx].TextureNames.Count - 1;
					break;
				case AnimationType.Loop:
					// [KeyFrame.TextureIndex..Layer.TextureCount] -> REPEAT [0..Layer.TextureCount]
					// Goes from the keyframe texture to the last texture in the layer's list, then repeat at frame 0.
					// Note: It does not care about the next keyframe's texture index at all.
					inter.TextureIndex = (int)(frames[0].Delay * (frameIdx - frames[0].FrameIndex) + frames[0].TextureIndex);
					inter.TextureIndex = inter.TextureIndex % str[layerIdx].TextureNames.Count;
					break;
				case AnimationType.ReverseLoop:
					// [KeyFrame.TextureIndex..0] -> REPEAT [Layer.TextureCount..0]
					// Goes from the keyframe texture to minus infinity's index.
					// The first frame is changed instantly with this mode, and that's intended. It's how the client does it...
					inter.TextureIndex = (int)(frames[0].TextureIndex - frames[0].Delay * (frameIdx - frames[0].FrameIndex));
					inter.TextureIndex = ((inter.TextureIndex % textureCount) + textureCount) % textureCount;
					break;
				case AnimationType.BiLoop:
					// This does Loop + ReverseLoop, on repeat.
					if (textureCount <= 1) {
						inter.TextureIndex = 0;
						break;
					}

					int rawIndex = (int)(frames[0].Delay * (frameIdx - frames[0].FrameIndex) + frames[0].TextureIndex);

					int cycleLength = (textureCount - 1) * 2;

					int pingPong = rawIndex % cycleLength;

					if (pingPong >= textureCount)
						pingPong = cycleLength - pingPong;

					inter.TextureIndex = pingPong;
					break;
			}

			inter.Delay = frames[0].Delay;
			inter.DelayStrKeyFrame = frames[0].Delay;
			inter.AnimationType = frames[0].AnimationType;
			inter.TextureIndexStrKeyFrame = 0;
			inter.BlendSrc = frames[0].BlendSrc;
			inter.BlendDst = frames[0].BlendDst;

			time = EaseTime((float)time, frames[0].OffsetBias);

			TkVector2 p1 = frames[0].Offset + new TkVector2(frames[0].BezierPositions[2], frames[0].BezierPositions[3]);
			TkVector2 p2 = frames[1].Offset + new TkVector2(frames[1].BezierPositions[0], frames[1].BezierPositions[1]);

			if (frames[0].BezierPositions[2] != 0 || frames[0].BezierPositions[3] != 0 || frames[1].BezierPositions[0] != 0 || frames[1].BezierPositions[1] != 0) {
				inter.Offset = _getBezier((float)time, frames[0].Offset, p1, p2, frames[1].Offset);
				
				// Bezier property fix
				TkVector2 p0 = frames[0].Offset;
				TkVector2 p3 = frames[1].Offset;
			
				TkVector2 p0_i = (p1 - p0) * (float)time + p0 * subMult;
				TkVector2 p1_i = (p2 - p1) * (float)time + p1 * subMult;
				TkVector2 p2_i = (p3 - p2) * (float)time + p2 * subMult;
			
				TkVector2 p0_ii = (p1_i - p0_i) * (float)time + p0_i * subMult - inter.Offset;
				TkVector2 p1_ii = (p2_i - p1_i) * (float)time + p1_i * subMult - inter.Offset;
			
				inter.BezierPositions[0] = p0_ii.X;
				inter.BezierPositions[1] = p0_ii.Y;
				inter.BezierPositions[2] = p1_ii.X;
				inter.BezierPositions[3] = p1_ii.Y;

				inter.BezierAdjacentPositions[0] = p0_i.X - p0.X;
				inter.BezierAdjacentPositions[1] = p0_i.Y - p0.Y;
				inter.BezierAdjacentPositions[2] = p2_i.X - p3.X;
				inter.BezierAdjacentPositions[3] = p2_i.Y - p3.Y;
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
					inter.Positions[i] = frames[0].Positions[i];
					inter.UVs[i] = frames[0].UVs[i];
				}

				for (int i = 0; i < 4; i++) {
					inter.BezierPositions[i] = frames[0].BezierPositions[i];
				}

				inter.AngleBias = frames[0].AngleBias;
				inter.OffsetBias = frames[0].OffsetBias;
				inter.ScaleBias = frames[0].ScaleBias;
				inter.Delay = frames[0].Delay;
				inter.AnimationType = frames[0].AnimationType;
				inter.TextureIndex = (int)frames[0].TextureIndex;
				inter.BlendSrc = frames[0].BlendSrc;
				inter.BlendDst = frames[0].BlendDst;
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

				if (adjustBezier && (Math.Abs(currentFrame.BezierAdjacentPositions[0]) > 0.05 ||
				    Math.Abs(currentFrame.BezierAdjacentPositions[1]) > 0.05 ||
				    Math.Abs(currentFrame.BezierAdjacentPositions[2]) > 0.05 ||
				    Math.Abs(currentFrame.BezierAdjacentPositions[3]) > 0.05) &&
				    baseKeyIndex + 1 < str[currentFrame.LayerIdx].KeyFrames.Count) {
					str.Commands.SetBezierPositions(currentFrame.LayerIdx, baseKeyIndex, new float[] {
						str[currentFrame.LayerIdx, baseKeyIndex].BezierPositions[0],
						str[currentFrame.LayerIdx, baseKeyIndex].BezierPositions[1],
						currentFrame.BezierAdjacentPositions[0],
						currentFrame.BezierAdjacentPositions[1],
					});

					str.Commands.SetBezierPositions(currentFrame.LayerIdx, baseKeyIndex + 1, new float[] {
						currentFrame.BezierAdjacentPositions[2],
						currentFrame.BezierAdjacentPositions[3],
						str[currentFrame.LayerIdx, baseKeyIndex + 1].BezierPositions[2],
						str[currentFrame.LayerIdx, baseKeyIndex + 1].BezierPositions[3],
					});
				}

				str.Commands.AddKey(currentFrame.LayerIdx, baseKeyIndex + 1, frame);
				currentFrame.KeyIndex = baseKeyIndex + 1;
				currentFrame.Interpolated = false;
			}
		}

		public TkVector2 GetXYVector(PointId point) {
			int id = (int)point;
			return new TkVector2(Positions[id], Positions[4 + id]);
		}

		public void SetXYVector(PointId point, in TkVector2 vector) {
			int id = (int)point;
			Positions[id] = vector.X;
			Positions[id + 4] = vector.Y;
		}

		public TkVector2 GetUVVector(UvPointId point) {
			int id = 2 * (int)point;
			return new TkVector2(UVs[id + 0], UVs[id + 1]);
		}

		public void SetUVVector(UvPointId point, in TkVector2 vector) {
			int id = 2 * (int)point;
			UVs[id + 0] = vector.X;
			UVs[id + 1] = vector.Y;
		}
	}
}
