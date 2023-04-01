using System.Collections.Generic;
using System.Linq;
using Utilities.Extension;

namespace GRF.FileFormats.ActFormat.Commands {
	public class FrameCommand : IActCommand {
		#region FrameEdit enum

		public enum FrameEdit {
			RemoveRange,
			CopyTo,
			ReplaceTo,
			Switch,
			MoveTo,
			AddTo,
			AddRange,
			InsertTo,
			MoveRange,
			SwitchRange
		}

		#endregion

		private readonly int _actionIndex;
		private readonly int _actionIndexTo;
		private readonly int _count;
		private readonly FrameEdit _frameEdit;
		private readonly int _frameIndex;
		private readonly int _frameIndexTo;
		private readonly int _layerFrom;
		private readonly int _layerFromLength;
		private readonly int _layerTo;
		private readonly Layer[] _layers;
		private readonly Layer _newLayer;
		private readonly int _range = 1;
		private Frame _conflict;
		private List<Frame> _conflicts;
		private CopyStructureAct _copy;

		public FrameCommand(int actionIndex, int frameIndex, int layerFrom, int layerFromLength, int layerTo, FrameEdit edit) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_layerFrom = layerFrom;
			_layerFromLength = layerFromLength;
			_layerTo = layerTo;
			_frameEdit = edit;
		}

		public FrameCommand(int actionIndex, int frameIndex, int count = 1) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_count = count;
			_frameEdit = FrameEdit.RemoveRange;
		}

		public FrameCommand(int actionIndex, int frameIndex, FrameEdit edit) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_frameEdit = edit;
		}

		public FrameCommand(int actionIndex, int frameIndex, int actionIndexTo, int frameIndexTo, FrameEdit edit) {
			if (edit == FrameEdit.MoveRange || edit == FrameEdit.SwitchRange) {
				_actionIndex = actionIndex;
				_frameIndex = frameIndex;
				_range = actionIndexTo;
				_frameIndexTo = frameIndexTo;
				_frameEdit = edit;
			}
			else {
				_actionIndex = actionIndex;
				_frameIndex = frameIndex;
				_actionIndexTo = actionIndexTo;
				_frameIndexTo = frameIndexTo;
				_frameEdit = edit;
			}
		}

		public FrameCommand(int actionIndex, int frameIndex, int frameIndexTo, Layer newLayer, FrameEdit edit) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_frameIndexTo = frameIndexTo;
			_newLayer = newLayer;
			_frameEdit = edit;
		}

		public FrameCommand(int actionIndex, int frameIndex, int frameIndexTo, Layer[] layers, FrameEdit edit) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_frameIndexTo = frameIndexTo;
			_layers = layers;
			_frameEdit = edit;
		}

		public int FrameIndex {
			get { return _frameIndex; }
		}

		public int FrameIndexTo {
			get { return _frameIndexTo; }
		}

		public int ActionIndexTo {
			get { return _actionIndexTo; }
		}

		public int ActionIndex {
			get { return _actionIndex; }
		}

		public FrameEdit Edit {
			get { return _frameEdit; }
		}

		public bool Executed { get; set; }

		#region IActCommand Members

		public void Execute(Act act) {
			Executed = true;

			switch (_frameEdit) {
				case FrameEdit.RemoveRange:
					_conflicts = act.RemoveFrames(_actionIndex, _frameIndex, _count);
					break;
				case FrameEdit.CopyTo:
					act[_actionIndexTo].Frames.Insert(_frameIndexTo, new Frame(act[_actionIndex].Frames[_frameIndex]));
					break;
				case FrameEdit.Switch:
					_conflict = act[_actionIndex, _frameIndex];
					act[_actionIndex, _frameIndex] = act[_actionIndexTo, _frameIndexTo];
					act[_actionIndexTo, _frameIndexTo] = _conflict;
					break;
				case FrameEdit.MoveTo:
					List<Layer> layers = act[_actionIndex, _frameIndex].Layers.Skip(_layerFrom).Take(_layerFromLength).ToList();
					act[_actionIndex, _frameIndex].Layers.InsertRange(_layerTo, layers);

					if (_layerTo < _layerFrom) {
						act[_actionIndex, _frameIndex].Layers.RemoveRange(_layerFrom + _layerFromLength, _layerFromLength);
					}
					else {
						act[_actionIndex, _frameIndex].Layers.RemoveRange(_layerFrom, _layerFromLength);
					}
					break;
				case FrameEdit.AddTo:
					act[_actionIndex, _frameIndex].Layers.Insert(_frameIndexTo, _newLayer);
					break;
				case FrameEdit.InsertTo:
					act[_actionIndex].Frames.Insert(_frameIndex, new Frame());
					break;
				case FrameEdit.ReplaceTo:
					if (_conflict == null)
						_conflict = act[_actionIndexTo, _frameIndexTo];

					act[_actionIndexTo, _frameIndexTo] = new Frame(act[_actionIndex, _frameIndex]);
					break;
				case FrameEdit.AddRange:
					if (_copy == null)
						_copy = new CopyStructureAct(act, CopyStructureMode.Actions);

					List<Layer> layersToAdd = new List<Layer>();

					for (int i = 0; i < _layers.Length; i++) {
						if (_layers[i].IsIndexed8() && _layers[i].SpriteIndex < act.Sprite.NumberOfIndexed8Images)
							layersToAdd.Add(new Layer(_layers[i]));
						else if (_layers[i].IsBgra32() && _layers[i].SpriteIndex < act.Sprite.NumberOfBgra32Images)
							layersToAdd.Add(new Layer(_layers[i]));
					}

					act[_actionIndex, _frameIndex].Layers.InsertRange(_frameIndexTo, layersToAdd);
					_copy.Clean(act);
					break;
				case FrameEdit.MoveRange:
					act[_actionIndex].Frames.Move(_frameIndex, _range, _frameIndexTo);
					break;
				case FrameEdit.SwitchRange:
					if (_conflicts == null)
						_conflicts = new List<Frame>(act[_actionIndex].Frames);

					act[_actionIndex].Frames.Switch(_frameIndex, _range, _frameIndexTo, 1);
					break;
			}
		}

		public void Undo(Act act) {
			Executed = false;

			switch (_frameEdit) {
				case FrameEdit.RemoveRange:
					act[_actionIndex].Frames.InsertRange(_frameIndex, _conflicts);
					break;
				case FrameEdit.CopyTo:
					act[_actionIndexTo].Frames.RemoveAt(_frameIndexTo);
					break;
				case FrameEdit.Switch:
					_conflict = act[_actionIndex, _frameIndex];
					act[_actionIndex, _frameIndex] = act[_actionIndexTo, _frameIndexTo];
					act[_actionIndexTo, _frameIndexTo] = _conflict;
					break;
				case FrameEdit.MoveTo:
					List<Layer> layers;

					if (_layerTo < _layerFrom) {
						layers = act[_actionIndex, _frameIndex].Layers.Skip(_layerTo).Take(_layerFromLength).ToList();
						act[_actionIndex, _frameIndex].Layers.RemoveRange(_layerTo, _layerFromLength);
					}
					else {
						layers = act[_actionIndex, _frameIndex].Layers.Skip(_layerTo - _layerFromLength).Take(_layerFromLength).ToList();
						act[_actionIndex, _frameIndex].Layers.RemoveRange(_layerTo - _layerFromLength, _layerFromLength);
					}

					act[_actionIndex, _frameIndex].Layers.InsertRange(_layerFrom, layers);
					break;
				case FrameEdit.AddTo:
					act[_actionIndex, _frameIndex].Layers.RemoveAt(_frameIndexTo);
					break;
				case FrameEdit.InsertTo:
					act[_actionIndex].Frames.RemoveAt(_frameIndex);
					break;
				case FrameEdit.ReplaceTo:
					act[_actionIndexTo, _frameIndexTo] = new Frame(_conflict);
					break;
				case FrameEdit.AddRange:
					_copy.Apply(act);
					break;
				case FrameEdit.MoveRange:
					act[_actionIndex].Frames.ReverseMove(_frameIndex, _range, _frameIndexTo);
					break;
				case FrameEdit.SwitchRange:
					act[_actionIndex].Frames.Clear();
					act[_actionIndex].Frames.AddRange(_conflicts);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_frameEdit) {
					case FrameEdit.RemoveRange:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Remove range (" + _count + ")";
					case FrameEdit.CopyTo:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Copy to " + CommandsHolder.GetId(_actionIndexTo, _frameIndexTo);
					case FrameEdit.Switch:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Switched with " + CommandsHolder.GetId(_actionIndexTo, _frameIndexTo);
					case FrameEdit.MoveTo:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Moved layers";
					case FrameEdit.AddTo:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Added layer at " + _frameIndexTo;
					case FrameEdit.ReplaceTo:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Copy and replace to " + CommandsHolder.GetId(_actionIndexTo, _frameIndexTo);
					case FrameEdit.AddRange:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Added layers at " + _frameIndexTo;
					case FrameEdit.MoveRange:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Moved range layers to " + _frameIndexTo;
					case FrameEdit.SwitchRange:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Switch range layers to " + _frameIndexTo;
				}

				return "Unknown";
			}
		}

		#endregion
	}
}