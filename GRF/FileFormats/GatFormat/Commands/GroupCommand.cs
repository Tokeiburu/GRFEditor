using System.Collections.Generic;
using Utilities.Commands;

namespace GRF.FileFormats.GatFormat.Commands {
	public class GroupCommand : IGroupCommand<IGatCommand>, IGatCommand {
		private readonly List<IGatCommand> _commands = new List<IGatCommand>();
		private readonly bool _executeCommandsOnStore;
		private readonly Gat _gat;
		private bool _firstTimeExecuted = true;

		public GroupCommand(Gat gat, bool executeCommandsOnStore = false) {
			_gat = gat;
			_executeCommandsOnStore = executeCommandsOnStore;
		}

		#region IGatCommand Members

		public void Execute(Gat gat) {
			if (_executeCommandsOnStore) {
				if (_firstTimeExecuted) {
					_firstTimeExecuted = false;
					return;
				}
			}

			foreach (var command in _commands) {
				command.Execute(gat);
			}
		}

		public void Undo(Gat gat) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(gat);
			}
		}

		public string CommandDescription {
			get {
				if (_commands.Count == 1)
					return _commands[0].CommandDescription;

				const int DisplayLimit = 2;

				string result = string.Format("Group command ({0}) :\r\n", _commands.Count);

				for (int i = 0; i < DisplayLimit && i < _commands.Count; i++) {
					result += "    " + _commands[i].CommandDescription.Replace("\r\n", "\\r\\n").Replace("\n", "\\n") + "\r\n";
				}

				result = result.Trim(new char[] { '\r', '\n' });

				if (_commands.Count > DisplayLimit) {
					result += "...";
				}

				return result;
			}
		}

		#endregion

		#region IGroupCommand<IGatCommand> Members

		public void Add(IGatCommand command) {
			_commands.Add(command);
		}

		public void Processing(IGatCommand command) {
			if (_executeCommandsOnStore)
				command.Execute(_gat);
		}

		public void AddRange(List<IGatCommand> commands) {
			_commands.AddRange(commands);
		}

		public List<IGatCommand> Commands {
			get { return _commands; }
		}

		public void Close() {
		}

		#endregion
	}
}