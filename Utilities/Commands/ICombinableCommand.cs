namespace Utilities.Commands {
	public interface ICombinableCommand {
		bool CanCombine(ICombinableCommand command);
		void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand);
	}
}