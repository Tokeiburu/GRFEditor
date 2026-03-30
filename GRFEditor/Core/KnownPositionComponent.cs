using System.Collections.Generic;
using Utilities;

namespace GRFEditor.Core {
	public class KnownPositionComponent {
		#region Delegates

		public delegate void CommandEventHandler(object sender);

		#endregion

		private readonly List<TkPath> _lastPositions = new List<TkPath>();
		private int _currentLastPositionIndex = -1;

		public bool CanUndo =>  _currentLastPositionIndex > 0;
		public bool CanRedo => _currentLastPositionIndex < _lastPositions.Count - 1;

		public event CommandEventHandler CommandExecuted;
		public event CommandEventHandler UndoExecuted;
		public event CommandEventHandler RedoExecuted;

		public void Reset() {
			_currentLastPositionIndex = -1;
			_lastPositions.Clear();
		}

		public void AddPath(TkPath path) {
			if (_currentLastPositionIndex >= 0 && path.RelativePath == _lastPositions[_currentLastPositionIndex].RelativePath) {
				return;
			}

			if (_lastPositions.Count - 1 > _currentLastPositionIndex) {
				_lastPositions.RemoveRange(_currentLastPositionIndex + 1, _lastPositions.Count - 1 - _currentLastPositionIndex);
			}

			_currentLastPositionIndex++;
			_lastPositions.Add(path);

			CommandExecuted?.Invoke(this);
		}

		public void RemovePath() {
			if (CanUndo) {
				_currentLastPositionIndex--;
				_lastPositions.RemoveAt(_lastPositions.Count - 1);
			}
		}

		public TkPath GetCurrentPath() {
			return _lastPositions[_currentLastPositionIndex];
		}

		public void Redo() {
			if (CanRedo) {
				_currentLastPositionIndex++;
				RedoExecuted?.Invoke(this);
			}
		}

		public void Undo() {
			if (CanUndo) {
				_currentLastPositionIndex--;
				UndoExecuted?.Invoke(this);
			}
		}
	}
}