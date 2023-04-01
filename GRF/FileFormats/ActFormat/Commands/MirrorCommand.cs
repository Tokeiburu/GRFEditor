using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class MirrorCommand : IActCommand, IAutoReverse {
		private readonly int _actionIndex;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private readonly bool _mirror;
		private readonly int _mode;
		private CopyStructureAct _copy;

		public MirrorCommand(bool mirror) {
			_mirror = mirror;
			_mode = 1;
		}

		public MirrorCommand(int actionIndex, int frameIndex, int layerIndex) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_mode = 0;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_mode) {
				case 0:
					act[_actionIndex, _frameIndex, _layerIndex].Mirror = !act[_actionIndex, _frameIndex, _layerIndex].Mirror;
					break;
				case 1:
					if (_copy == null) {
						_copy = new CopyStructureAct(act, CopyStructureMode.Actions);
					}

					act.AllLayers(p => p.Mirror = !p.Mirror);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_mode) {
				case 0:
					act[_actionIndex, _frameIndex, _layerIndex].Mirror = !act[_actionIndex, _frameIndex, _layerIndex].Mirror;
					break;
				case 1:
					_copy.Apply(act);
					break;
			}
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Mirror value changed"; }
		}

		#endregion

		#region IAutoReverse Members

		public bool CanCombine(ICombinableCommand command) {
			if (_mode != 0) return false;

			var cmd = command as MirrorCommand;
			if (cmd != null) {
				if (cmd._actionIndex == _actionIndex &&
				    cmd._frameIndex == _frameIndex &&
				    cmd._layerIndex == _layerIndex)
					return true;
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as AnimationSpeedCommand;
			if (cmd != null) {
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		public bool CanDelete(IAutoReverse command) {
			return true;
		}

		#endregion
	}
}