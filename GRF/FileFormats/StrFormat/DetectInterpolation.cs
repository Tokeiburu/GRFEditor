using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GRF.Graphics;
using GRF.IO;
using Utilities;

namespace GRF.FileFormats.StrFormat {
	public partial class Str {

		public void ConvertInterpolatedFrames() {
			foreach (var layer in Layers) {
				for (int keyIndex = 0; keyIndex < layer.KeyFrames.Count; keyIndex++) {
					if (keyIndex > 0 && layer[keyIndex].Type == 1) {
						layer[keyIndex - 1].IsInterpolated = true;

						if (layer.IsType(keyIndex + 1, 0) && layer[keyIndex + 1].FrameIndex == layer[keyIndex].FrameIndex)
							layer[keyIndex - 1].IsInterpolated = false;
					}
				}

				for (int keyIndex = layer.KeyFrames.Count - 1; keyIndex >= 0; keyIndex--) {
					if (layer[keyIndex].Type == 1) {
						layer.KeyFrames.RemoveAt(keyIndex);
					}
				}
			}
		}

		private float _calcBias(float r, float diff, float t) {
			return (float)((Math.Log(r / diff) / Math.Log(t) - 1) * 5);
		}

		private float _applyBias(float bias, float diff, float v0, float t) {
			if (bias > 0) {
				return (float)Math.Pow(t, 1 + bias / 5) * diff + v0;
			}
			else {
				return diff - (float)Math.Pow(1 - t, -bias / 5 + 1) * diff + v0;
			}
		}

		private float _calcBiasReverse(float r, float diff, float t) {
			return -5 * (float)(Math.Log(1 - r / diff) / Math.Log(1 - t) - 1);
		}

		private delegate float CalcBiasMethod(float r, float diff, float t);

		private bool _detectScaleBias(StrLayer layer, int keyIndex, int startIndex, int distance, ref float bias) {
			bool compare = true;
			bool noMove = true;

			// Check offset
			float[] previousBias = new float[8];

			float[] diff = new float[8];
			bool ignoreX = false;
			bool ignoreY = false;

			var biasRecord = new List<List<float>>();
			int startComp = 0;
			int endComp = 8;

			for (int i = 0; i < 8; i++) {
				diff[i] = layer[keyIndex].Xy[i] - layer[startIndex].Xy[i];
				previousBias[i] = 0;

				if (diff[i] != 0)
					noMove = false;

				biasRecord.Add(new List<float>());
			}

			if (diff[0] == 0 &&
			    diff[1] == 0 &&
			    diff[2] == 0 &&
			    diff[3] == 0) {
				ignoreX = true;
				startComp = 4;
			}

			if (diff[4] == 0 &&
				diff[5] == 0 &&
				diff[6] == 0 &&
				diff[7] == 0) {
				ignoreY = true;
				endComp = 4;
			}

			Z.F(ignoreX);
			Z.F(ignoreY);

			CalcBiasMethod method = _calcBias;

			if (noMove) {
				// Nothing to do
				for (int i = startIndex + 1; i < keyIndex; i++) {
					for (int j = 0; j < 8; j++) {
						if (Math.Abs(layer[i].Xy[j] - layer[startIndex].Xy[j]) > ToleranceBias ||
							Math.Abs(layer[i].Xy[j] - layer[startIndex].Xy[j]) > ToleranceBias)
							return false;
					}
				}
			}
			else {
				// Find base bias, middle of the distance
				for (int i = startIndex + 1; i < keyIndex; i++) {
					if (i == startIndex + 1) {
						int middle = startIndex + (int)(0.5 * distance);

						for (int j = startComp; j < endComp; j++) {
							previousBias[j] = method(layer[middle].Xy[j] - layer[startIndex].Xy[j], diff[j], (float)(middle - startIndex) / distance);

							if (double.IsNaN(previousBias[j])) {
								previousBias[j] = 0;
							}

							biasRecord[j].Add(previousBias[j]);
						}

						continue;
					}

					var t = (float)(i - startIndex) / distance;
					float[] biasA = new float[8];

					for (int j = startComp; j < endComp; j++) {
						biasA[j] = method(layer[i].Xy[j] - layer[startIndex].Xy[j], diff[j], t);
					}

					bool noChange = true;

					for (int j = startComp; j < endComp; j++) {
						if (double.IsNaN(biasA[j]) || Math.Abs(biasA[j] - previousBias[j]) >= Tolerance) {
							noChange = false;
							break;
						}
					}

					if (!noChange) {
						noChange = true;

						for (int j = 0; j < 8; j++) {
							if (Math.Abs(_applyBias(previousBias[j], diff[j], layer[startIndex].Xy[j], t) - layer[i].Xy[j]) >= ToleranceBias) {
								noChange = false;
								break;
							}
						}

						if (noChange) {
							for (int j = startComp; j < endComp; j++) {
								biasRecord[j].Add(previousBias[j]);
							}

							continue;
						}
					}

					if (noChange && !biasA.Any(p => double.IsNaN(p))) {
						for (int j = startComp; j < endComp; j++) {
							previousBias[j] = biasA[j];
							biasRecord[j].Add(previousBias[j]);
						}

						continue;
					}

					if (method == _calcBias) {
						method = _calcBiasReverse;
						i = startIndex;

						for (int j = 0; j < 8; j++) {
							biasRecord[j].Clear();
						}

						continue;
					}

					compare = false;
					break;
				}
			}

			for (int j = startComp + 1; j < endComp; j++) {
				if (biasRecord[j].Count > 0) {
					previousBias[j] = biasRecord[j].Average();
				}
			}

			for (int j = startComp + 1; j < endComp; j++) {
				if (Math.Abs(previousBias[startComp] - previousBias[j]) >= Tolerance) {
					compare = false;
					break;
				}
			}

			if (!compare) {
				return false;
			}

			bias = (float)Math.Round(previousBias[startComp], 4);

			if (float.IsNaN(bias)) {
				bias = 0;
				return true;
			}

			if (Math.Abs(bias - Math.Round(bias)) > ToleranceBiasAverage) {
				bias = 0;
				return true;
			}

			return true;
		}

		private bool _detectOffsetBias(StrLayer layer, int keyIndex, int startIndex, int distance, ref float bias) {
			bool compare = true;

			// Check offset
			float previousBiasX = 0;
			float previousBiasY = 0;

			float diffX = layer[keyIndex].Offset.X - layer[startIndex].Offset.X;
			float diffY = layer[keyIndex].Offset.Y - layer[startIndex].Offset.Y;
			CalcBiasMethod method = _calcBias;
			var biasXRecord = new List<float>();
			var biasYRecord = new List<float>();
			
			bool ignoreY = false;
			bool ignoreX = false;

			if (diffX == 0) {
				ignoreX = true;
			}

			if (diffY == 0) {
				ignoreY = true;
			}

			if (diffX == 0 && diffY == 0) {
				// Nothing to do
				for (int i = startIndex + 1; i < keyIndex; i++) {
					if (Math.Abs(layer[i].Offset.X - layer[startIndex].Offset.X) > ToleranceBias ||
						Math.Abs(layer[i].Offset.Y - layer[startIndex].Offset.Y) > ToleranceBias)
						return false;
				}
			}
			else {
				for (int i = startIndex + 1; i < keyIndex; i++) {
					var t = (float)(i - startIndex) / distance;
					var biasX = method(layer[i].Offset.X - layer[startIndex].Offset.X, diffX, t);
					var biasY = method(layer[i].Offset.Y - layer[startIndex].Offset.Y, diffY, t);
					
					if (i == startIndex + 1) {
						int middle = startIndex + (int)(0.5 * distance);

						previousBiasX = method(layer[middle].Offset.X - layer[startIndex].Offset.X, diffX, (float)(middle - startIndex) / distance);
						previousBiasY = method(layer[middle].Offset.Y - layer[startIndex].Offset.Y, diffY, (float)(middle - startIndex) / distance);
						biasXRecord.Add(previousBiasX);
						biasYRecord.Add(previousBiasY);

						if ((!ignoreX && float.IsInfinity(previousBiasX)) || (!ignoreY && float.IsInfinity(previousBiasY))) {
							return false;
						}

						continue;
					}

					if ((!ignoreX && float.IsInfinity(biasX)) || (!ignoreY && float.IsInfinity(biasY))) {
						// Before rejecting it, check if applying matches
						if (diffX == 0) {
							if (Math.Abs(_applyBias(previousBiasY, diffY, layer[startIndex].Offset.Y, t) - layer[i].Offset.Y) < ToleranceBias) {
								continue;
							}
						}
						else if (diffY == 0) {
							if (Math.Abs(_applyBias(previousBiasX, diffX, layer[startIndex].Offset.X, t) - layer[i].Offset.X) < ToleranceBias) {
								continue;
							}
						}
						else {
							if (Math.Abs(_applyBias(previousBiasY, diffY, layer[startIndex].Offset.Y, t) - layer[i].Offset.Y) < ToleranceBias &&
							    Math.Abs(_applyBias(previousBiasX, diffX, layer[startIndex].Offset.X, t) - layer[i].Offset.X) < ToleranceBias) {
								continue;
							}
						}

						return false;
					}

					if (diffX == 0) {
						if (Math.Abs(biasY - previousBiasY) < Tolerance) {
							previousBiasY = biasY;
							biasXRecord.Add(previousBiasX);
							biasYRecord.Add(previousBiasY);
							continue;
						}

						if (Math.Abs(_applyBias(previousBiasY, diffY, layer[startIndex].Offset.Y, t) - layer[i].Offset.Y) < ToleranceBias) {
							biasXRecord.Add(biasX);
							biasYRecord.Add(biasY);
							continue;
						}
					}
					else if (diffY == 0) {
						if (Math.Abs(biasX - previousBiasX) < Tolerance) {
							previousBiasX = biasX;
							biasXRecord.Add(previousBiasX);
							biasYRecord.Add(previousBiasY);
							continue;
						}

						if (Math.Abs(_applyBias(previousBiasX, diffX, layer[startIndex].Offset.X, t) - layer[i].Offset.X) < ToleranceBias) {
							biasXRecord.Add(previousBiasX);
							biasYRecord.Add(previousBiasY);
							continue;
						}
					}
					else {
						if (Math.Abs(biasX - previousBiasX) < Tolerance &&
							Math.Abs(biasY - previousBiasY) < Tolerance) {
							previousBiasX = biasX;
							previousBiasY = biasY;
							biasXRecord.Add(previousBiasX);
							biasYRecord.Add(previousBiasY);
							continue;
						}

						if (Math.Abs(_applyBias(previousBiasY, diffY, layer[startIndex].Offset.Y, t) - layer[i].Offset.Y) < ToleranceBias &&
							Math.Abs(_applyBias(previousBiasX, diffX, layer[startIndex].Offset.X, t) - layer[i].Offset.X) < ToleranceBias) {
							biasXRecord.Add(previousBiasX);
							biasYRecord.Add(previousBiasY);
							continue;
						}
					}

					if (method == _calcBias) {
						method = _calcBiasReverse;
						i = startIndex;
						biasXRecord.Clear();
						biasYRecord.Clear();
						continue;
					}

					compare = false;
					break;
				}
			}

			if (biasXRecord.Count > 0) {
				previousBiasX = biasXRecord.Average();
			}

			if (biasYRecord.Count > 0) {
				previousBiasY = biasYRecord.Average();
			}

			if (diffX != 0 && diffY != 0 && Math.Abs(previousBiasX - previousBiasY) >= Tolerance) {
				compare = false;
			}

			if (diffX == 0) {
				previousBiasX = previousBiasY;
			}

			if (!compare) {
				return false;
			}

			bias = (float)Math.Round(previousBiasX, 4);

			if (float.IsNaN(bias)) {
				bias = 0;
				return true;
			}

			if (Math.Abs(bias - Math.Round(bias)) > ToleranceBiasAverage) {
				bias = 0;
				return true;
			}

			if (Math.Abs(bias - Math.Round(bias)) < ToleranceBiasAverage) {
				bias = (float)Math.Round(bias);
			}

			return true;
		}

		private bool _detectAngleBias(StrLayer layer, int keyIndex, int startIndex, int distance, ref float bias) {
			bool compare = true;

			// Check offset
			float previousBiasX = 0;

			float diffX = layer[keyIndex].Angle - layer[startIndex].Angle;
			CalcBiasMethod method = _calcBias;
			var biasXRecord = new List<float>();
			previousBiasX = 0;

			if (diffX == 0) {
				// Verify if there was a change in Angle during the timeframe
				for (int i = startIndex + 1; i < keyIndex; i++) {
					if (Math.Abs(layer[i].Angle - layer[startIndex].Angle) > ToleranceBias)
						return false;
				}
			}
			else {
				for (int i = startIndex + 1; i < keyIndex; i++) {
					var t = (float)(i - startIndex) / distance;
					var biasX = method(layer[i].Angle - layer[startIndex].Angle, diffX, t);

					if (double.IsInfinity(biasX))
						return false;

					if (i == startIndex + 1) {
						biasXRecord.Add(biasX);
						previousBiasX = biasX;
						continue;
					}

					if (Math.Abs(biasX - previousBiasX) < Tolerance) {
						previousBiasX = biasX;
						biasXRecord.Add(previousBiasX);
						continue;
					}

					if (Math.Abs(_applyBias(previousBiasX, diffX, layer[startIndex].Angle, t) - layer[i].Angle) < ToleranceBias) {
						biasXRecord.Add(biasX);
						continue;
					}

					if (method == _calcBias) {
						method = _calcBiasReverse;
						i = startIndex;
						biasXRecord.Clear();
						continue;
					}

					compare = false;
					break;
				}
			}

			if (!compare) {
				return false;
			}

			if (biasXRecord.Count > 0) {
				previousBiasX = biasXRecord.Average();
			}

			bias = (float)Math.Round(previousBiasX, 4);

			if (float.IsNaN(bias)) {
				bias = 0;
				return true;
			}

			if (Math.Abs(bias - Math.Round(bias)) < ToleranceBiasAverage) {
				bias = (float)Math.Round(bias);
			}

			return true;
		}

		private bool _detectBezier(StrLayer layer, int keyIndex, int startIndex, int distance, ref float[] bezier, ref float offsetBiasOut) {
			// Test possible offset biases as well
			for (int j = 0; j <= 40; j++) {
				var offsetBias = ((j + 1) / 2) * ((j % 2) == 1 ? -1 : 1);
				
				var p0 = new Point(layer[startIndex].Offset.X, layer[startIndex].Offset.Y);
				var p3 = new Point(layer[keyIndex].Offset.X, layer[keyIndex].Offset.Y);
				
				var keyIndexF = (int)(0.33 * distance) + 0;
				var keyIndexG = (int)(0.66 * distance) + 0;
				//var f = new Point(layer[startIndex + keyIndexF].Offset.X, layer[startIndex + keyIndexF].Offset.Y);
				//var g = new Point(layer[startIndex + keyIndexG].Offset.X, layer[startIndex + keyIndexG].Offset.Y);

				var u = (float)keyIndexF / distance;
				var v = (float)keyIndexG / distance;

				u = InterpolatedKeyFrame.EaseTime(u, offsetBias);
				v = InterpolatedKeyFrame.EaseTime(v, offsetBias);

				var mu = (1 - u);
				var mv = (1 - v);

				var mu3 = mu * mu * mu;
				var mv3 = mv * mv * mv;

				var cx = layer[startIndex + keyIndexF].Offset.X - mu3 * p0.X - u * u * u * p3.X;
				var cy = layer[startIndex + keyIndexF].Offset.Y - mu3 * p0.Y - u * u * u * p3.Y;

				//var c = f - (1 - u) * (1 - u) * (1 - u) * p0 - u * u * u * p3;
				//var d = g - (1 - v) * (1 - v) * (1 - v) * p0 - v * v * v * p3;

				var a1 = 3 * mu * mu * u;
				var a2 = 3 * mu * u * u;

				var b1 = 3 * mv * mv * v;
				var b2 = 3 * mv * v * v;

				var p2 = new Point(
					((layer[startIndex + keyIndexG].Offset.X - mv3 * p0.X - v * v * v * p3.X) * a1 - cx * b1) / (b2 * a1 - a2 * b1),
					((layer[startIndex + keyIndexG].Offset.Y - mv3 * p0.Y - v * v * v * p3.Y) * a1 - cy * b1) / (b2 * a1 - a2 * b1)
					);
				var p1 = new Point(
					(cx - a2 * p2.X) / a1,
					(cy - a2 * p2.Y) / a1
					);
				
				//var p2 = (d * a1 - c * b1) / (b2 * a1 - a2 * b1);
				//var p1 = (c - a2 * p2) / a1;

				p1.X = (float)Math.Round(p1.X, 4);
				p1.Y = (float)Math.Round(p1.Y, 4);

				p2.X = (float)Math.Round(p2.X, 4);
				p2.Y = (float)Math.Round(p2.Y, 4);

				if (Math.Abs(p1.X - Math.Round(p1.X)) < ToleranceBiasAverage) {
					p1.X = (float)Math.Round(p1.X);
				}

				if (Math.Abs(p1.Y - Math.Round(p1.Y)) < ToleranceBiasAverage) {
					p1.Y = (float)Math.Round(p1.Y);
				}

				if (Math.Abs(p2.X - Math.Round(p2.X)) < ToleranceBiasAverage) {
					p2.X = (float)Math.Round(p2.X);
				}

				if (Math.Abs(p1.Y - Math.Round(p2.Y)) < ToleranceBiasAverage) {
					p2.Y = (float)Math.Round(p2.Y);
				}

				// The angle should not be used, it's not a good reference value at all
				var ind = layer[keyIndex - 1].Offset - p0;
				
				// Check if the bezier points are on the line itself; if yes, return directly
				var temp1 = Math.Abs(Point.CalculateAngle(ind, p1 - p0));
				var temp2 = Math.Abs(Point.CalculateAngle(ind, p2 - p0));
				
				if (double.IsNaN(temp1))
					temp1 = 0;
				if (double.IsNaN(temp2))
					temp2 = 0;
				
				//if (temp1 < 0.01 && temp2 < 0.01) {
				if (temp1 < 0.05 && temp2 < 0.05) {
					return false;
				}

				// Too small to determine anything honestly
				if ((p1 - p0).Length < 2 && (p2 - p3).Length < 2)
					continue;

				bool fail = false;
				//bool bezierImpact = false;

				// Verify if other nodes match
				for (int i = startIndex + 1; i < keyIndex; i++) {
					var t = (float)(i - startIndex) / distance;
					
					t = InterpolatedKeyFrame.EaseTime(t, offsetBias);

					var mt = (1 - t);
					var p = new Point(
						mt * mt * mt * p0.X + 3 * mt * mt * t * p1.X + 3 * mt * t * t * p2.X + t * t * t * p3.X,
						mt * mt * mt * p0.Y + 3 * mt * mt * t * p1.Y + 3 * mt * t * t * p2.Y + t * t * t * p3.Y);

					if (Math.Abs(p.X - layer[i].Offset.X) > ToleranceBiasAverage ||
						Math.Abs(p.Y - layer[i].Offset.Y) > ToleranceBiasAverage) {
						fail = true;
						break;
					}

					//p = new Point(
					//	(layer[keyIndex].Offset.X - layer[startIndex].Offset.X) * t + layer[startIndex].Offset.X,
					//	(layer[keyIndex].Offset.Y - layer[startIndex].Offset.Y) * t + layer[startIndex].Offset.Y);
					//
					//if (Math.Abs(p.X - layer[i].Offset.X) > ToleranceBiasAverage ||
					//	Math.Abs(p.Y - layer[i].Offset.Y) > ToleranceBiasAverage) {
					//	bezierImpact = true;
					//	break;
					//}
				}

				if (fail)
					continue;

				//if (!bezierImpact)
				//	continue;

				//if (angle < 0.2 || double.IsNaN(angle))
				//	continue;

				offsetBiasOut = offsetBias;
				bezier[0] = p1.X;
				bezier[1] = p1.Y;
				bezier[2] = p2.X;
				bezier[3] = p2.Y;
				return true;
			}

			return false;
		}

		private bool _detectOtherBias(int layerIndex, StrLayer layer, int keyIndex, int startIndex, int distance, ref float bias, float biasOffset) {
			var start = layer[startIndex];
			var end = layer[keyIndex];
			bool checkOffsets = !(Math.Abs(biasOffset) > 0.001);

			for (int frameIndex = start.FrameIndex + 1; frameIndex < start.FrameIndex + distance; frameIndex++) {
				var inter = InterpolatedKeyFrame.InterpolateSub(this, layerIndex, frameIndex, start, end);
				int targetKeyFrame = startIndex + (frameIndex - start.FrameIndex);

				for (int i = 0; i < 4; i++) {
					if (Math.Abs(inter.Color[i] - layer[targetKeyFrame].Color[i]) > 0.001) {
						bias = 0;
						return false;
					}
				}

				if (checkOffsets) {
					if (Math.Abs(inter.Offset.X - layer[targetKeyFrame].Offset.X) > 0.001 ||
						Math.Abs(inter.Offset.Y - layer[targetKeyFrame].Offset.Y) > 0.001) {
						bias = 0;
						return false;
					}
				}
			}

			bias = 1;
			return true;
		}

		const float Tolerance = 0.06f;
		const float ToleranceDetection = 0.001f;
		const float ToleranceBias = 0.001f;
		const float ToleranceBiasAverage = 0.01f;

		public void DetectInterpolatedFrames() {
			//GrfPath.Delete(@"C:\Games\debug.log");

			// Check for potential candidates
			for (int index = 0; index < Layers.Count; index++) {
				var layer = Layers[index];

				// Check the one with the least amount of frames
				var layer1 = new StrLayer(layer);
				var layer2 = new StrLayer(layer);

				_detectInterpolatedOthers(1, index, layer1);
				_detectInterpolatedBezier(1, index, layer1);

				if (!_detectInterpolationHasMoreFrames(index, layer1)) {
					Layers[index] = layer1;
					continue;
				}

				_detectInterpolatedBezier(2, index, layer2);
				_detectInterpolatedOthers(2, index, layer2);

				if (layer1.KeyFrames.Count < layer2.KeyFrames.Count) {
					Layers[index] = layer1;
				}
				else {
					Layers[index] = layer2;
				}
			}
			
			Z.F();
		}

		private bool _detectInterpolationHasMoreFrames(int layerIndex, StrLayer layer) {
			for (int keyIndex = 0; keyIndex < layer.KeyFrames.Count; keyIndex++) {
				// How many consecutive frames are there?
				if (layer[keyIndex].IsInterpolated)
					continue;

				int startKeyIndex = keyIndex;
				int endKeyIndex = keyIndex;

				for (int kIndex = keyIndex; kIndex < layer.KeyFrames.Count - 1; kIndex++) {
					if (layer[kIndex].FrameIndex != layer[kIndex + 1].FrameIndex - 1) {
						endKeyIndex = kIndex;
						break;
					}
					else if (kIndex + 1 == layer.KeyFrames.Count - 1) {
						endKeyIndex = kIndex + 1;
						break;
					}
				}

				// Check how many are matching
				int distance = endKeyIndex - startKeyIndex;

				if (distance >= 1)
					return true;
			}

			return false;
		}

		private void _detectInterpolatedOthers(int pass, int layerIndex, StrLayer layer) {
			//
			// Do a second pass for other transforms
			//_____________________________
			for (int keyIndex = 0; keyIndex < layer.KeyFrames.Count; keyIndex++) {
				// How many consecutive frames are there?
				if (layer[keyIndex].IsInterpolated)
					continue;

				int startKeyIndex = keyIndex;
				int endKeyIndex = keyIndex;

				for (int kIndex = keyIndex; kIndex < layer.KeyFrames.Count - 1; kIndex++) {
					if (layer[kIndex].FrameIndex != layer[kIndex + 1].FrameIndex - 1) {
						endKeyIndex = kIndex;
						break;
					}
					else if (kIndex + 1 == layer.KeyFrames.Count - 1) {
						endKeyIndex = kIndex + 1;
						break;
					}
				}

				// Check how many are matching
				int distance = endKeyIndex - startKeyIndex;

				if (distance <= 1)
					continue;

				for (; distance >= 3; distance--) {
					float biasOffset = 0;
					float biasAngle = 0;
					float biasScale = 0;
					float isInterpolated = 0;

					// Check offset
					if (!_detectOffsetBias(layer, startKeyIndex + distance, startKeyIndex, distance, ref biasOffset)) {
						continue;
					}

					if (!_detectAngleBias(layer, startKeyIndex + distance, startKeyIndex, distance, ref biasAngle)) {
						continue;
					}

					if (!_detectScaleBias(layer, startKeyIndex + distance, startKeyIndex, distance, ref biasScale)) {
						continue;
					}

					if (!_detectOtherBias(layerIndex, layer, startKeyIndex + distance, startKeyIndex, distance, ref isInterpolated, biasOffset)) {
						continue;
					}

					if (Math.Abs(biasOffset) < ToleranceBias && Math.Abs(biasAngle) < ToleranceBias && Math.Abs(biasScale) < ToleranceBias && Math.Abs(isInterpolated) < ToleranceBias) {
						continue;
					}

					layer[keyIndex].AngleBias = biasAngle;
					layer[keyIndex].OffsetBias = biasOffset;
					layer[keyIndex].ScaleBias = biasScale;
					layer[keyIndex].IsInterpolated = true;

					for (int i = keyIndex + 1; i < startKeyIndex + distance; i++) {
						layer[i].PendingDelete = true;
					}

					keyIndex += distance - 1;
					break;
				}
			}

			for (int keyIndex = layer.KeyFrames.Count - 1; keyIndex >= 0; keyIndex--) {
				if (layer[keyIndex].PendingDelete) {
					layer.KeyFrames.RemoveAt(keyIndex);
				}
			}
		}

		private void _detectInterpolatedBezier(int pass, int layerIndex, StrLayer layer) {
			//
			// Do a first pass for Bezier
			//_____________________________
			for (int keyIndex = 0; keyIndex < layer.KeyFrames.Count; keyIndex++) {
				// How many consecutive frames are there?
				if (layer[keyIndex].IsInterpolated)
					continue;

				int startKeyIndex = keyIndex;
				int endKeyIndex = keyIndex;

				for (int kIndex = keyIndex; kIndex < layer.KeyFrames.Count - 1; kIndex++) {
					if (layer[kIndex].FrameIndex != layer[kIndex + 1].FrameIndex - 1) {
						endKeyIndex = kIndex;
						break;
					}
					else if (kIndex + 1 == layer.KeyFrames.Count - 1) {
						endKeyIndex = kIndex + 1;
						break;
					}
				}

				// Check how many are matching
				int distance = endKeyIndex - startKeyIndex;

				if (distance <= 4)
					continue;

				for (; distance >= 4; distance--) {
					float[] bezier = new float[4];
					float biasOffset = 0;
					float biasAngle = 0;
					float biasScale = 0;
					
					//int uid = layerIndex * 100000000 + startKeyIndex * 10000 + startKeyIndex + distance;

					if (!_detectBezier(layer, startKeyIndex + distance, startKeyIndex, distance, ref bezier, ref biasOffset)) {
						continue;
					}

					// Bezier was detected, now look for angle and scale
					if (!_detectAngleBias(layer, startKeyIndex + distance, startKeyIndex, distance, ref biasAngle)) {
						continue;
					}

					if (!_detectScaleBias(layer, startKeyIndex + distance, startKeyIndex, distance, ref biasScale)) {
						continue;
					}

					layer[keyIndex].Bezier[2] = bezier[0] - layer[keyIndex].Offset.X;
					layer[keyIndex].Bezier[3] = bezier[1] - layer[keyIndex].Offset.Y;
					layer[keyIndex].OffsetBias = biasOffset;
					layer[keyIndex].AngleBias = biasAngle;
					layer[keyIndex].ScaleBias = biasScale;

					layer[startKeyIndex + distance].Bezier[0] = bezier[2] - layer[startKeyIndex + distance].Offset.X;
					layer[startKeyIndex + distance].Bezier[1] = bezier[3] - layer[startKeyIndex + distance].Offset.Y;

					layer[keyIndex].IsInterpolated = true;

					for (int i = keyIndex + 1; i < startKeyIndex + distance; i++) {
						layer[i].PendingDelete = true;
					}

					keyIndex += distance - 1;
					break;
				}
			}

			for (int keyIndex = layer.KeyFrames.Count - 1; keyIndex >= 0; keyIndex--) {
				if (layer[keyIndex].PendingDelete) {
					layer.KeyFrames.RemoveAt(keyIndex);
				}
			}
		}
	}
}
