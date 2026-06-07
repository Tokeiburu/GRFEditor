using System;
using GRF.Graphics;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class SetOffsetCommand : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private float _x;
		private float _y;
		private bool _isSet = false;
		private float _oldX;
		private float _oldY;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public SetOffsetCommand(int layerIndex, int frameIndex, float x, float y) {
			_layerIndex = layerIndex;
			_keyIndex = frameIndex;
			_x = x;
			_y = y;
		}

		public string CommandDescription => $"[{_layerIndex},{_keyIndex}] Offset changed to ({_x:0.00}, {_y:0.00})";

		public void Execute(Str str) {
			if (!_isSet) {
				_oldX = str[_layerIndex, _keyIndex].Offset.X;
				_oldY = str[_layerIndex, _keyIndex].Offset.Y;
				_isSet = true;
			}

			str[_layerIndex, _keyIndex].Offset = new TkVector2(_x, _y);
		}

		public void Undo(Str str) {
			str[_layerIndex, _keyIndex].Offset = new TkVector2(_oldX, _oldY);
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is SetOffsetCommand cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is SetOffsetCommand cmd) {
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
