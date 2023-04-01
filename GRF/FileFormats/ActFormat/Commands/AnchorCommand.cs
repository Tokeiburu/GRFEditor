namespace GRF.FileFormats.ActFormat.Commands {
	public class AnchorCommand : IActCommand {
		private readonly int _actionIndex;
		private readonly int _anchorIndex;
		private readonly int _frameIndex;
		private readonly int _offsetX;
		private readonly int _offsetY;

		private int _addedNew;
		private Anchor _oldAnchor;

		public AnchorCommand(int actionIndex, int frameIndex, int offsetX, int offsetY, int anchorIndex) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_offsetX = offsetX;
			_offsetY = offsetY;
			_anchorIndex = anchorIndex;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_anchorIndex >= act[_actionIndex, _frameIndex].Anchors.Count) {
				while (_anchorIndex >= act[_actionIndex, _frameIndex].Anchors.Count) {
					act[_actionIndex, _frameIndex].Anchors.Add(new Anchor(new byte[4], 0, 0, 0));
					_addedNew++;
				}
			}

			Anchor anchor = act[_actionIndex, _frameIndex].Anchors[_anchorIndex];

			if (_oldAnchor == null) {
				_oldAnchor = new Anchor(anchor);
			}

			anchor.OffsetX = _offsetX;
			anchor.OffsetY = _offsetY;
		}

		public void Undo(Act act) {
			if (_addedNew > 0) {
				act[_actionIndex, _frameIndex].Anchors.RemoveRange(act[_actionIndex, _frameIndex].Anchors.Count - _addedNew, _addedNew);
			}
			else {
				act[_actionIndex, _frameIndex].Anchors[_anchorIndex] = new Anchor(_oldAnchor);
			}

			_addedNew = 0;
		}

		public string CommandDescription {
			get { return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Anchor position changed to (" + _offsetX + ", " + _offsetY + ")."; }
		}

		#endregion
	}
}