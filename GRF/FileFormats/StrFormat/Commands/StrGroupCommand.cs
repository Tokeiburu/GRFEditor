using System.Collections.Generic;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class StrGroupCommand : IGroupCommand<IStrCommand>, IStrCommand {
		private readonly Str _str;
		private readonly List<IStrCommand> _commands = new List<IStrCommand>();
		private readonly bool _executeCommandsOnStore;
		private bool _firstTimeExecuted = true;

		public StrGroupCommand(Str str, bool executeCommandsOnStore = false) {
			_str = str;
			_executeCommandsOnStore = executeCommandsOnStore;
		}

		#region IStrCommand Members

		public void Execute(Str str) {
			if (_executeCommandsOnStore) {
				if (_firstTimeExecuted) {
					_firstTimeExecuted = false;
					return;
				}
			}

			for (int index = 0; index < _commands.Count; index++) {
				var command = _commands[index];
				try {
					command.Execute(str);
				}
				catch (AbstractCommandException) {
					_commands.RemoveAt(index);
					index--;
				}
			}
		}

		public void Undo(Str str) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(str);
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

		#region IGroupCommand<IStrCommand> Members

		public List<IStrCommand> Commands {
			get { return _commands; }
		}

		public void Close() {
		}

		public void Add(IStrCommand command) {
			_commands.Add(command);
		}

		public void Processing(IStrCommand command) {
			if (_executeCommandsOnStore)
				command.Execute(_str);
		}

		public void AddRange(List<IStrCommand> commands) {
			_commands.AddRange(commands);
		}

		#endregion
	}
}