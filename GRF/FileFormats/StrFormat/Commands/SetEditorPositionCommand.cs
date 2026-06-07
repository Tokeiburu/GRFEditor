using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class SetEditorPositionCommand : IStrCommand, IFrameCommand, INullCommand {
		private readonly int _layerIndex;
		private readonly int _frameIndex;
		private readonly int _layerCount;
		private readonly int _frameCount;

		public int LayerIndex => _layerIndex;
		public int FrameIndex => _frameIndex;

		public int LayerCount => _layerCount;
		public int FrameCount => _frameCount;

		public SetEditorPositionCommand(int frameIndex, int frameCount, int layerIndex, int layerCount) {
			_layerIndex = layerIndex;
			_frameIndex = frameIndex;
			_frameCount = frameCount;
			_layerCount = layerCount;
		}

		public string CommandDescription => null;

		public void Execute(Str str) {
		}

		public void Undo(Str str) {
		}
	}
}
