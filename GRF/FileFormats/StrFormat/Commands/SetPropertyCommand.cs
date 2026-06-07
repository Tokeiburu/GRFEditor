using System;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class SetPropertyCommand<T> : IStrCommand, IPosCommand {
		private readonly int _layerIndex;
		private readonly int _keyIndex;
		private T _newValue;
		private T _oldValue;
		private Action<T> _setter;
		private string _name;
		private int _mode;

		public int LayerIndex => _layerIndex;
		public int KeyIndex => _keyIndex;

		public SetPropertyCommand(int layerIndex, int keyIndex, T oldValue, T newValue, Action<T> setter, string name) {
			_layerIndex = layerIndex;
			_keyIndex = keyIndex;
			_oldValue = oldValue;
			_newValue = newValue;
			_setter = setter;
			_name = name;
			_mode = 0;
		}

		public SetPropertyCommand(int layerIndex, T oldValue, T newValue, Action<T> setter, string name) {
			_layerIndex = layerIndex;
			_oldValue = oldValue;
			_newValue = newValue;
			_setter = setter;
			_name = name;
			_mode = 1;
		}

		public SetPropertyCommand(T oldValue, T newValue, Action<T> setter, string name) {
			_oldValue = oldValue;
			_newValue = newValue;
			_setter = setter;
			_name = name;
			_mode = 2;
		}

		public string CommandDescription {
			get {
				switch (_mode) {
					case 0:
						if (_newValue is float)
							return $"[{_layerIndex},{_keyIndex}] {_name} changed to {_newValue:0.##}";

						return $"[{_layerIndex},{_keyIndex}] {_name} changed to {_newValue}";
					case 1:
						if (_newValue is float)
							return $"[{_layerIndex}] {_name} changed to {_newValue:0.##}";

						return $"[{_layerIndex}] {_name} changed to {_newValue}";
					default:
					case 2:
						if (_newValue is float)
							return $"{_name} changed to {_newValue:0.##}";

						return $"{_name} changed to {_newValue}";
				}
			}
		}

		public void Execute(Str str) {
			_setter(_newValue);
		}

		public void Undo(Str str) {
			_setter(_oldValue);
		}

		public bool CanCombine(ICombinableCommand command) {
			if (command is SetPropertyCommand<T> cmd) {
				if (cmd._layerIndex == _layerIndex &&
					cmd._keyIndex == _keyIndex)
					return true;
			}

			return false;
		}

		public void Combine<T2>(ICombinableCommand command, AbstractCommand<T2> abstractCommand) {
			if (command is SetPropertyCommand<T> cmd) {
				_newValue = cmd._newValue;
				abstractCommand.ExplicitCommandExecution((T2)(object)this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _oldValue.GetHashCode() == _newValue.GetHashCode();
		}
	}
}
