namespace GRF.FileFormats.StrFormat.Commands {
	public interface IStrCommand {
		string CommandDescription { get; }
		void Execute(Str str);
		void Undo(Str str);
	}
}
