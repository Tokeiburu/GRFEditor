using System.Collections.Generic;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class ActGroupCommand : IGroupCommand<IActCommand>, IActCommand {
		private readonly Act _act;
		private readonly List<IActCommand> _commands = new List<IActCommand>();
		private readonly bool _executeCommandsOnStore;
		private bool _firstTimeExecuted = true;

		public ActGroupCommand(Act act, bool executeCommandsOnStore = false) {
			_act = act;
			_executeCommandsOnStore = executeCommandsOnStore;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_executeCommandsOnStore) {
				if (_firstTimeExecuted) {
					_firstTimeExecuted = false;
					return;
				}
			}

			for (int index = 0; index < _commands.Count; index++) {
				var command = _commands[index];
				try {
					command.Execute(act);
				}
				catch (AbstractCommandException) {
					_commands.RemoveAt(index);
					index--;
				}
			}
		}

		public void Undo(Act act) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(act);
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

		#region IGroupCommand<IActCommand> Members

		public List<IActCommand> Commands {
			get { return _commands; }
		}

		public void Close() {
		}

		public void Add(IActCommand command) {
			_commands.Add(command);
		}

		public void Processing(IActCommand command) {
			if (_executeCommandsOnStore)
				command.Execute(_act);
		}

		public void AddRange(List<IActCommand> commands) {
			_commands.AddRange(commands);
		}

		#endregion
	}
}