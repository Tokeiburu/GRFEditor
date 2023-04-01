using System;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using Utilities.Commands;

namespace GRF.FileFormats.StrFormat.Commands {
	public class BackupCommand : IStrCommand {
		private readonly Action<Str> _command;
		private string _commandName;

		private CopyStructureStr _copy;
		private bool _forceUpdateOnUndoRedoCommands;

		public BackupCommand(Action<Str> command) {
			_command = command;
		}

		public BackupCommand(Action<Str> command, string commandName) {
			_command = command;
			_commandName = commandName;
		}

		public BackupCommand(Action<Str> command, string commandName, bool forceUpdateOnUndoRedoCommands) {
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

		public void Execute(Str str) {
			if (_copy == null)
				_copy = new CopyStructureStr(str);

			if (_command != null) {
				try {
					_command(str);
				}
				catch {
					_copy.Apply(str);
					throw;
				}
			}

			_copy.Clean(str);

			//if (_forceUpdateOnUndoRedoCommands) {
			if (!_copy.HasChanged(str)) {
				throw new CancelAbstractCommand(new AbstractCommandArg { Cancel = true });
			}
			//}

			if (_forceUpdateOnUndoRedoCommands) {
				str.InvalidateVisualRedraw();
			}
		}

		public void Undo(Str str) {
			_copy.Apply(str);

			if (_forceUpdateOnUndoRedoCommands) {
				str.InvalidateVisualRedraw();
			}
		}

		public string CommandDescription {
			get { return _commandName ?? "Generic command"; }
		}

		#endregion
	}
}