using System.Collections.Generic;

namespace GRF.FileFormats.ActFormat.Commands {
	public class LayerCommand : IActCommand {
		#region LayerEdit enum

		public enum LayerEdit {
			RemoveRange,
		}

		#endregion

		private readonly int _actionIndex;
		private readonly int _count;
		private readonly LayerEdit _edit;
		private readonly int _frameIndex;
		private readonly int _layerIndex;
		private List<Layer> _conflicts;

		public LayerCommand(int actionIndex, int frameIndex, int layerIndex, int count, LayerEdit edit) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerIndex = layerIndex;
			_count = count;
			_edit = edit;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_edit) {
				case LayerEdit.RemoveRange:
					_conflicts = act.RemoveLayers(_actionIndex, _frameIndex, _layerIndex, _count);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_edit) {
				case LayerEdit.RemoveRange:
					act[_actionIndex, _frameIndex].Layers.InsertRange(_layerIndex, _conflicts);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_edit) {
					case LayerEdit.RemoveRange:
						return CommandsHolder.GetId(_actionIndex, _frameIndex, _layerIndex) + " Remove range (" + _count + ")";
				}

				return "";
			}
		}

		#endregion
	}
}