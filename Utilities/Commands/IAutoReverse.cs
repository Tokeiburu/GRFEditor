namespace Utilities.Commands {
	public interface IAutoReverse : ICombinableCommand {
		bool CanDelete(IAutoReverse command);
	}
}