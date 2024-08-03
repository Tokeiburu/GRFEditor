using System.Collections.Generic;
using Utilities.Commands;

namespace Database.Commands {
	public class GroupCommand<TKey, TValue> : IGroupCommand<ITableCommand<TKey, TValue>>, ITableCommand<TKey, TValue>
		where TValue : Tuple {
		private readonly Table<TKey, TValue> _table;
		private readonly List<ITableCommand<TKey, TValue>> _commands;
		private readonly CCallbacks.GroupTupleCallback _callback;
		private readonly bool _executeCommandsOnStore;
		private bool _firstTimeExecuted = true;

		public List<ITableCommand<TKey, TValue>> Commands {
			get { return _commands; }
		}

		public void Close() {
			if (_callback != null)
				_callback(true);
		}

		internal GroupCommand() : this(new List<ITableCommand<TKey,TValue>>(), null, false) {
		}

		internal GroupCommand(bool executeCommandsOnStore, Table<TKey, TValue> table)
			: this(new List<ITableCommand<TKey, TValue>>(), null, executeCommandsOnStore) {
			_table = table;
		}

		internal GroupCommand(bool executeCommandsOnStore, Table<TKey, TValue> table, CCallbacks.GroupTupleCallback callback)
			: this(new List<ITableCommand<TKey, TValue>>(), callback, executeCommandsOnStore) {
			_table = table;
		}

		internal GroupCommand(List<ITableCommand<TKey, TValue>> commands, CCallbacks.GroupTupleCallback callback) 
			: this(commands, callback, false) {
		}

		internal GroupCommand(List<ITableCommand<TKey, TValue>> commands, CCallbacks.GroupTupleCallback callback, bool executeCommandsOnStore) {
			_commands = commands;
			_callback = callback;
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

			if (_callback != null)
				_callback(true);
		}

		public void Undo(Table<TKey, TValue> table) {
			for (int index = _commands.Count - 1; index >= 0; index--) {
				_commands[index].Undo(table);
			}

			if (_callback != null)
				_callback(false);
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

		public static GroupCommand<TKey, TValue> Make() {
			return new GroupCommand<TKey, TValue>();
		}
	}
}
