using System.Collections.Generic;
using Utilities;
using Utilities.Commands;

namespace GRF.ContainerFormat.Commands {
	public class GroupCommand<TEntry> : IGroupCommand<IContainerCommand<TEntry>>, IContainerCommand<TEntry> where TEntry : ContainerEntry {
		#region Delegates

		public delegate void MoveFileCallback(string oldFileName, string newFileName, bool isExecuted);

		#endregion

		private readonly List<IContainerCommand<TEntry>> _commands = new List<IContainerCommand<TEntry>>();
		private readonly ContainerAbstract<TEntry> _container;
		private readonly bool _executeCommandsOnStore;
		private bool _firstTimeExecuted = true;

		/// <summary>
		/// Execute a group of commands as a whole
		/// </summary>
		/// <param name="commands"> </param>
		public GroupCommand(List<IContainerCommand<TEntry>> commands) {
			_commands = commands;
		}

		public GroupCommand() {
		}

		public GroupCommand(ContainerAbstract<TEntry> container, bool executeCommandsOnStore) {
			_container = container;
			_executeCommandsOnStore = executeCommandsOnStore;
		}

		#region IContainerCommand<TEntry> Members

		public void Execute(ContainerAbstract<TEntry> container) {
			if (_executeCommandsOnStore) {
				if (_firstTimeExecuted) {
					_firstTimeExecuted = false;
					return;
				}
			}

			foreach (var command in _commands) {
				command.Execute(container);
			}
		}

		public void Undo(ContainerAbstract<TEntry> container) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(container);
			}
		}

		public string CommandDescription {
			get {
				if (_commands.Count == 1)
					return _commands[0].CommandDescription;

				const int DisplayLimit = 2;

				string result = string.Format(GrfStrings.GroupCommand, _commands.Count);

				for (int i = 0; i < DisplayLimit && i < _commands.Count; i++) {
					result += "    " + _commands[i].CommandDescription.Replace("\r\n", "\\r\\n").Replace("\n", "\\n") + "\r\n";
				}

				result = result.Trim(new char[] {'\r', '\n'});

				if (_commands.Count > DisplayLimit) {
					result += "...";
				}

				return result;
			}
		}

		#endregion

		#region IGroupCommand<IContainerCommand<TEntry>> Members

		public void Add(IContainerCommand<TEntry> command) {
		}

		public void AddRange(List<IContainerCommand<TEntry>> commands) {
			_commands.AddRange(commands);
		}

		public List<IContainerCommand<TEntry>> Commands {
			get { return _commands; }
		}

		public void Close() {
		}

		public void Processing(IContainerCommand<TEntry> command) {
			if (_executeCommandsOnStore)
				command.Execute(_container);
		}

		#endregion
	}
}