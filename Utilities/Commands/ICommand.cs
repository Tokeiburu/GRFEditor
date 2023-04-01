namespace Utilities.Commands {
	public interface ICommand<T> {
		string CommandDescription { get; }
		void Execute(T act);
		void Undo(T act);
	}
}