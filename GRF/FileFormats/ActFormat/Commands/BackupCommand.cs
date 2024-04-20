using System;
using Utilities.Commands;

namespace GRF.FileFormats.ActFormat.Commands {
	public class BackupCommand : IActCommand {
		private readonly Action<Act> _command;
		public CopyStructureMode CopyMode = CopyStructureMode.Full;
		private string _commandName;

		private CopyStructureAct _copy;
		private bool _forceUpdateOnUndoRedoCommands;

		public BackupCommand(Action<Act> command) {
			_command = command;
		}

		public BackupCommand(Action<Act> command, string commandName) {
			_command = command;
			_commandName = commandName;
		}

		public BackupCommand(Action<Act> command, string commandName, bool forceUpdateOnUndoRedoCommands) {
			_command = command;
			_commandName = commandName;
			_forceUpdateOnUndoRedoCommands = forceUpdateOnUndoRedoCommands;
		}

		public bool ForceUpdateOnUndoRedoCommands {
			get { return _forceUpdateOnUndoRedoCommands; }
			set { _forceUpdateOnUndoRedoCommands = value; }
		}

		public string CommandName {
			get { return _commandName; }
			set { _commandName = value; }
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_copy == null)
				_copy = new CopyStructureAct(act, CopyMode);

			if (_command != null) {
				try {
					_command(act);
				}
				catch {
					_copy.Apply(act);
					throw;
				}
			}

			_copy.Clean(act);

			//if (_forceUpdateOnUndoRedoCommands) {
			if (!_copy.HasChanged(act)) {
				throw new CancelAbstractCommand(new AbstractCommandArg { Cancel = true });
			}
			//}

			if (_forceUpdateOnUndoRedoCommands) {
				act.InvalidateSpriteVisual();
			}
		}

		public void Undo(Act act) {
			_copy.Apply(act);

			if (_forceUpdateOnUndoRedoCommands) {
				act.InvalidateSpriteVisual();
			}
		}

		public string CommandDescription {
			get { return _commandName ?? "Generic command"; }
		}

		#endregion
	}
}