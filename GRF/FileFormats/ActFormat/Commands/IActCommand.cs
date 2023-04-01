namespace GRF.FileFormats.ActFormat.Commands {
	public interface IActCommand {
		string CommandDescription { get; }
		void Execute(Act act);
		void Undo(Act act);
	}
}