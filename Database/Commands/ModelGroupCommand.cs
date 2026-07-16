using System.Collections.Generic;
using Utilities.Commands;

namespace Database.Commands {
	public class ModelGroupCommand<TKey, TValue> : IGroupCommand<ITableCommand<TKey, TValue>>, ITableCommand<TKey, TValue>, IAutoReverse, ICombinableCommand
		where TValue : Tuple {
		private readonly List<ITableCommand<TKey, TValue>> _commands = new List<ITableCommand<TKey, TValue>>();
		private readonly Table<TKey, TValue> _table;
		private readonly string _editUniqueId;
		private readonly bool _executeCommandsOnStore;
		private bool _firstTimeExecuted = true;

		public List<ITableCommand<TKey, TValue>> Commands => _commands;

		public void Close() {
		}

		public ModelGroupCommand(Table<TKey, TValue> table, string editUniqueId, bool executeCommandsOnStore) {
			_table = table;
			_editUniqueId = editUniqueId;
			_executeCommandsOnStore = executeCommandsOnStore;
			Key = default(TKey);
		}

		public void Execute(Table<TKey, TValue> table) {
			if (_executeCommandsOnStore) {
				if (_firstTimeExecuted) {
					_firstTimeExecuted = false;
					return;
				}
			}

			for (int index = 0; index < _commands.Count; index++) {
				_commands[index].Execute(table);
			}
		}

		public void Undo(Table<TKey, TValue> table) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(table);
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

				result = result.Trim(new char[] {'\r', '\n'});

				if (_commands.Count > DisplayLimit) {
					result += "...";
				}

				return result;
			}
		}

		public TKey Key { get; private set; }

		public void Add(ITableCommand<TKey, TValue> command) {
			_commands.Add(command);
		}

		public void Processing(ITableCommand<TKey, TValue> command) {
			if (_executeCommandsOnStore)
				command.Execute(_table);
		}

		public void AddRange(List<ITableCommand<TKey, TValue>> commands) {
			_commands.AddRange(commands);
		}

		public bool CanDelete(IAutoReverse command) {
			return false;
		}

		public bool CanCombine(ICombinableCommand command) {
			return command is ModelGroupCommand<TKey, TValue> modelGroupCmd && _editUniqueId == modelGroupCmd._editUniqueId;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			if (command is ModelGroupCommand<TKey, TValue> modelGroupCmd) {
				abstractCommand.ExplicitCommandUndo((T)(object)this);

			}
		}
	}
}
