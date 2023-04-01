using Utilities;

namespace GRF.ContainerFormat.Commands {
	public interface IContainerCommand<TEntry> where TEntry : ContainerEntry {
		string CommandDescription { get; }
		void Execute(ContainerAbstract<TEntry> container);
		void Undo(ContainerAbstract<TEntry> container);
	}
}