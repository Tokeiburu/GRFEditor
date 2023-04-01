using GRF.Image;
using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class ColorCommand : IActCommand, IAutoReverse {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly int _mode;
		private GrfColor _color;
		private CopyStructureAct _copy;
		private GrfColor _oldColor;

		public ColorCommand(GrfColor color) {
			_color = color;
			_mode = 1;
		}

		public ColorCommand(int actionIndex, int frameIndex, int layerIndex, GrfColor color) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_color = color;
			_mode = 0;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_mode) {
				case 0:
					if (_oldColor == null)
						_oldColor = act[_actionIndex, _frameIndex, _layerIndex].Color;

					act[_actionIndex, _frameIndex, _layerIndex].Color = _color;
					break;
				case 1:
					if (_copy == null) {
						_copy = new CopyStructureAct(act, CopyStructureMode.Actions);
					}

					act.AllLayers(p => p.Color = _color);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_mode) {
				case 0:
					act[_actionIndex, _frameIndex, _layerIndex].Color = _oldColor;
					break;
				case 1:
					_copy.Apply(act);
					break;
			}
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Color changed to " + _color.ToHexString(); }
		}

		#endregion

		#region IAutoReverse Members

		public bool CanCombine(ICombinableCommand command) {
			if (_mode != 0) return false;

			var cmd = command as ColorCommand;
			if (cmd != null) {
				if (cmd._actionIndex == _actionIndex &&
				    cmd._frameIndex == _frameIndex &&
				    cmd._layerIndex == _layerIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ColorCommand;
			if (cmd != null) {
				_color = cmd._color;
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return _color.Equals(_oldColor);
		}

		#endregion
	}
}