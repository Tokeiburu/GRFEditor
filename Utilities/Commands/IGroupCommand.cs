using System.Collections.Generic;

namespace Utilities.Commands {
	public interface IGroupCommand<T> {
		void Add(T command);
		void Processing(T command);
		void AddRange(List<T> commands);
		List<T> Commands { get; }
		void Close();
	}
}