using System.Collections.Generic;
using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.PalFormat {
	public class GroupCommand : IGroupCommand<IPaletteCommand>, IPaletteCommand {
		private readonly List<IPaletteCommand> _commands = new List<IPaletteCommand>();
		private readonly bool _executeCommandsOnStore;
		private readonly Pal _pal;
		private bool _firstTimeExecuted = true;

		public GroupCommand(Pal pal, bool executeCommandsOnStore = false) {
			_pal = pal;
			_executeCommandsOnStore = executeCommandsOnStore;
		}

		#region IGroupCommand<IPaletteCommand> Members

		public void Add(IPaletteCommand command) {
			_commands.Add(command);
		}

		public void Processing(IPaletteCommand command) {
			if (_executeCommandsOnStore)
				command.Execute(_pal);
		}

		public void AddRange(List<IPaletteCommand> commands) {
			_commands.AddRange(commands);
		}

		public List<IPaletteCommand> Commands {
			get { return _commands; }
		}

		public void Close() {
		}

		#endregion

		#region IPaletteCommand Members

		public void Execute(Pal palette) {
			if (_executeCommandsOnStore) {
				if (_firstTimeExecuted) {
					_firstTimeExecuted = false;
					return;
				}
			}

			foreach (var command in _commands) {
				command.Execute(palette);
			}
		}

		public void Undo(Pal palette) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(palette);
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
	}
}