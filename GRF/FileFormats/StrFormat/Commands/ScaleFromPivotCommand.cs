using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GRF.Graphics;

namespace GRF.FileFormats.StrFormat.Commands {
	public class ScaleFromPivotCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private float _sx;
		private float _sy;
		private TkVector2 _pivot;
		private PivotMode _pivotMode;
		private bool _isSet = false;

		private float[] _vertices;
		private float[] _newVertices;
		private TkVector2[] _oldOffsets;
		private TkVector2[] _newOffsets;
		private List<StrKeyFrame> _keyFrames = new List<StrKeyFrame>();

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;
		private ScaleMode _mode;

		public enum ScaleMode {
			KeyFrame,
			Layer,
			Str
		}

		public enum PivotMode {
			Defined,
			Center,
			Origin
		}

		public ScaleFromPivotCommand(int layerIndex, int keyIndex, float sx, float sy, TkVector2 pivot, PivotMode pivotMode) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_sx = sx;
			_sy = sy;
			_pivot = pivot;
			_pivotMode = pivotMode;
			_mode = ScaleMode.KeyFrame;
		}

		public ScaleFromPivotCommand(int layerIndex, float sx, float sy, TkVector2 pivot, PivotMode pivotMode) {
			_layerIndex = layerIndex;
			_sx = sx;
			_sy = sy;
			_pivot = pivot;
			_pivotMode = pivotMode;
			_mode = ScaleMode.Layer;
		}

		public ScaleFromPivotCommand(float sx, float sy, TkVector2 pivot, PivotMode pivotMode) {
			_sx = sx;
			_sy = sy;
			_pivot = pivot;
			_pivotMode = pivotMode;
			_mode = ScaleMode.Str;
		}

		public string CommandDescription {
			get {
				switch (_mode) {
					case ScaleMode.KeyFrame:
						return $"[{_layerIndex},{_keyIndex}] Scale changed by ({_sx:0.00}, {_sy:0.00})";
					case ScaleMode.Layer:
						return $"[{_layerIndex}] Scale changed by ({_sx:0.00}, {_sy:0.00})";
					case ScaleMode.Str:
					default:
						return $"Scale changed by ({_sx:0.00}, {_sy:0.00})";
				}
			}
		}

		public void Execute(Str str) {
			TkVector2 scale = new TkVector2(_sx, _sy);

			if (!_isSet) {
				switch (_mode) {
					case ScaleMode.KeyFrame:
						_keyFrames.Add(str[_layerIndex, _keyIndex]);
						break;
					case ScaleMode.Layer:
						_keyFrames.AddRange(str[_layerIndex].KeyFrames);
						break;
					case ScaleMode.Str:
						for (int lidx = 0; lidx < str.Layers.Count; lidx++) {
							_keyFrames.AddRange(str[lidx].KeyFrames);
						}
						break;
				}

				_vertices = new float[_keyFrames.Count * 8];
				_newVertices = new float[_vertices.Length];
				_oldOffsets = new TkVector2[_keyFrames.Count];
				_newOffsets = new TkVector2[_keyFrames.Count];

				for (int i = 0; i < _keyFrames.Count; i++) {
					var keyFrame = _keyFrames[i];

					Buffer.BlockCopy(keyFrame.Xy, 0, _vertices, 8 * i * sizeof(float), 8 * sizeof(float));
					_oldOffsets[i] = keyFrame.Offset;
					_newOffsets[i] = _oldOffsets[i];
				}

				switch (_pivotMode) {
					case PivotMode.Defined:
					case PivotMode.Origin:
						for (int i = 0; i < _vertices.Length; i++) {
							if ((i % 8) < 4) {
								_newVertices[i] = _vertices[i] * _sx;
							}
							else {
								_newVertices[i] = _vertices[i] * _sy;
							}
						}

						if (_pivotMode == PivotMode.Defined) {
							for (int k = 0; k < _keyFrames.Count; k++) {
								var keyFrame = _keyFrames[k];

								// The offset shown is not the real XY values. The center is rebased by the centerOffset, so some math is needed
								var center = _oldOffsets[k] - _pivot;
								center = scale * center;
								_newOffsets[k] = center + _pivot;
							}
						}
						break;
					case PivotMode.Center:
						for (int k = 0; k < _keyFrames.Count; k++) {
							var keyFrame = _keyFrames[k];
							TkVector2 center = default;

							for (int j = 0; j < 4; j++) {
								center.X += keyFrame.Xy[j];
								center.Y += keyFrame.Xy[j + 4];
							}

							center /= 4f;

							for (int j = 0; j < 4; j++) {
								_newVertices[8 * k + j + 0] = (keyFrame.Xy[j] - center.X) * _sx + center.X;
								_newVertices[8 * k + j + 4] = (keyFrame.Xy[j + 4] - center.Y) * _sy + center.Y;
							}
						}
						break;
				}

				_isSet = true;
			}

			for (int k = 0; k < _keyFrames.Count; k++) {
				var keyFrame = _keyFrames[k];

				Buffer.BlockCopy(_newVertices, k * 8 * sizeof(float), keyFrame.Xy, 0, 8 * sizeof(float));
				keyFrame.Offset = _newOffsets[k];
			}
		}

		public void Undo(Str str) {
			for (int k = 0; k < _keyFrames.Count; k++) {
				var keyFrame = _keyFrames[k];

				Buffer.BlockCopy(_vertices, k * 8 * sizeof(float), keyFrame.Xy, 0, 8 * sizeof(float));
				keyFrame.Offset = _oldOffsets[k];
			}
		}
	}
}
