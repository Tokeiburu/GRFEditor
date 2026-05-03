using System;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class BackupCommand : IActCommand {
		private readonly Action<Act> _command;
		public CopyStructureMode CopyMode = CopyStructureMode.Full;
		private string _commandName;
		private CopyStructureAct _copy;

		public BackupCommand(Action<Act> command) {
			_command = command;
		}

		public BackupCommand(Action<Act> command, string commandName) {
			_command = command;
			_commandName = commandName;
		}

		public string CommandName {
			get { return _commandName; }
			set { _commandName = value; }
		}

		#region IActCommand Members

		public void Execute(Act act) {
			int oldActionCount = act.NumberOfActions;

			if (_copy == null)
				_copy = new CopyStructureAct(act, CopyMode);

			if (_command != null) {
				try {
					_command(act);
				}
				catch {
					_copy.Undo(act);
					throw;
				}
			}

			_copy.RemovedUnusedChanges(act);

			if (!_copy.HasChanged(act)) {
				throw new CancelAbstractCommand(new AbstractCommandArg { Cancel = true });
			}

			act.InvalidateSpriteVisual();

			if (oldActionCount != act.NumberOfActions)
				act.OnActionCountChanged();
		}

		public void Undo(Act act) {
			int oldActionCount = act.NumberOfActions;

			_copy.Undo(act);

			act.InvalidateSpriteVisual();

			if (oldActionCount != act.NumberOfActions)
				act.OnActionCountChanged();
		}

		public string CommandDescription {
			get { return _commandName ?? "Generic command"; }
		}

		#endregion
	}
}