using System;
using GRF.Graphics;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class OffsetCommand : IStrCommand, IAutoReverse {
		private readonly int _layerIdx;
		private readonly int _frameIdx;
		private float _x;
		private float _y;
		private bool _isSet = false;
		private float _oldX;
		private float _oldY;

		public OffsetCommand(int layerIdx, int frameIdx, float x, float y) {
			_layerIdx = layerIdx;
			_frameIdx = frameIdx;
			_x = x;
			_y = y;
		}

		public string CommandDescription {
			get {
				return "[" + _layerIdx + "," + _frameIdx + "] Offset changed to (" + String.Format("{0:0.00}", _x) + ", " + String.Format("{0:0.00}", _y) + ")";
			}
		}

		public void Execute(Str str) {
			if (!_isSet) {
				_oldX = str[_layerIdx, _frameIdx].Offset.X;
				_oldY = str[_layerIdx, _frameIdx].Offset.Y;
				_isSet = true;
			}

			str[_layerIdx, _frameIdx].Offset = new TkVector2(_x, _y);
		}

		public void Undo(Str str) {
			str[_layerIdx, _frameIdx].Offset = new TkVector2(_oldX, _oldY);
		}

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as OffsetCommand;
			if (cmd != null) {
				if (cmd._layerIdx == _layerIdx &&
					cmd._frameIdx == _frameIdx)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as OffsetCommand;
			if (cmd != null) {
				_x = cmd._x;
				_y = cmd._y;
				abstractCommand.ExplicitCommandExecution((T)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldX == _x && _oldY == _y;
		}
	}
}
