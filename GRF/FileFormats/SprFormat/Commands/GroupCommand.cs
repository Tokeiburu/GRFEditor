using System.Collections.Generic;
using GRF.FileFormats.SprFormat.Builder;

namespace GRF.FileFormats.SprFormat.Commands {
	public class GroupCommand : ICommand {
		private readonly List<ICommand> _commands;

		public GroupCommand(List<ICommand> commands) {
			_commands = commands;
		}

		#region ICommand Members

		public void Execute(SprBuilderInterface sbi) {
			for (int index = 0; index < _commands.Count; index++) {
				_commands[index].Execute(sbi);
			}
		}

		public void Undo(SprBuilderInterface sbi) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(sbi);
			}
		}

		#endregion
	}
}