namespace GRF.FileFormats.StrFormat.Commands {
	public interface IStrCommand {
		string CommandDescription { get; }
		void Execute(Str str);
		void Undo(Str str);
	}

	public interface IPosCommand {
		int LayerIndex { get; }
		int KeyIndex { get; }
	}

	public interface IFrameCommand {
		int LayerIndex { get; }
		int FrameIndex { get; }
	}
}
