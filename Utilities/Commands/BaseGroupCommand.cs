using System.Collections.Generic;

namespace Utilities.Commands {
	public class BaseGroupCommand<T> : IGroupCommand<T> where T : ICommand<T> {
		private readonly T _obj;
		private readonly List<T> _commands = new List<T>();
		private readonly bool _executeCommandsOnStore;
		private bool _firstTimeExecuted = true;

		public BaseGroupCommand(T obj)
			: this(obj, false) {
		}

		public BaseGroupCommand(T obj, bool executeCommandsOnStore) {
			_obj = obj;
			_executeCommandsOnStore = executeCommandsOnStore;
		}

		#region IActCommand Members

		public void Execute(T act) {
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

		public void Undo(T act) {
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

		public List<T> Commands {
			get { return _commands; }
		}

		public void Close() {
		}

		public void Add(T command) {
			_commands.Add(command);
		}

		public void Processing(T command) {
			if (_executeCommandsOnStore)
				command.Execute(_obj);
		}

		public void AddRange(List<T> commands) {
			_commands.AddRange(commands);
		}

		#endregion
	}
}