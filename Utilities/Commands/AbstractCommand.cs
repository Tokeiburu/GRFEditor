﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Utilities.Commands {
	/// <summary>
	/// Defines an object with Undo and Redo operations.
	/// </summary>
	/// <typeparam name="T">The object type on which the commands are being executed</typeparam>
	public abstract class AbstractCommand<T> : IDisposable {
		/// <summary>
		/// Gets a value indicating whether a command is being executed (locked).
		/// </summary>
		public bool IsLocked { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the command stack is being grouped.
		/// </summary>
		public bool IsDelayed { get; private set; }

		public int CommandIndexNonModified { get { return _commandIndexNonModified; } }

		protected readonly List<T> _commands = new List<T>();
		protected readonly object _thisLock = new object();
		protected int _commandIndexModified = -1;
		protected int _commandIndexCurrent = -1;			// Index to the latest command used
		protected int _commandIndexNonModified = -1;
		private readonly List<T> _delayedCommands = new List<T>();
		private IGroupCommand<T> _delayedCommandsCommand;

		/// <summary>
		/// Gets the delayed commands.
		/// </summary>
		public ReadOnlyCollection<T> DelayedCommands {
			get { return _delayedCommands.AsReadOnly(); }
		}

		/// <summary>
		/// Gets the commands.
		/// </summary>
		public ReadOnlyCollection<T> Commands {
			get { return _commands.AsReadOnly(); }
		}

		/// <summary>
		/// Gets the latest command executed.
		/// </summary>
		public T Current {
			get {
				if (_commandIndexCurrent >= 0)
					return _commands[_commandIndexCurrent];
				return default(T);
			}
		}

		/// <summary>
		/// Gets the current index of the command.
		/// </summary>
		public int CommandIndex {
			get { return _commandIndexCurrent; }
		}

		/// <summary>
		/// Gets the index of the command that has been last modified.
		/// </summary>
		public int ModifiedCommandIndex {
			get { return _commandIndexModified; }
		}

		public event AbstractCommandsEventHandler PreviewCommandExecuted;
		public event AbstractCommandsEventHandler CommandExecuted;
		public event AbstractCommandsEventHandler CommandIndexChanged;
		public event AbstractCommandsEventHandler PreviewCommandUndo;
		public event AbstractCommandsEventHandler CommandUndo;
		public event AbstractCommandsEventHandler PreviewCommandRedo;
		public event AbstractCommandsEventHandler CommandRedo;
		public event AbstractCommandsEventHandler ModifiedStateChanged;

		~AbstractCommand() {
			Dispose(false);
		}

		protected virtual void _onCommandIndexChanged(T command) {
			AbstractCommandsEventHandler handler = CommandIndexChanged;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onPreviewCommandExecuted(T command) {
			AbstractCommandsEventHandler handler = PreviewCommandExecuted;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onPreviewCommandUndo(T command) {
			AbstractCommandsEventHandler handler = PreviewCommandUndo;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onPreviewCommandRedo(T command) {
			AbstractCommandsEventHandler handler = PreviewCommandRedo;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onCommandUndo(T command) {
			AbstractCommandsEventHandler handler = CommandUndo;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onCommandRedo(T command) {
			AbstractCommandsEventHandler handler = CommandRedo;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onCommandExecuted(T command) {
			AbstractCommandsEventHandler handler = CommandExecuted;
			if (handler != null) handler(this, command);
		}

		protected virtual void _onModifiedStateChanged(T command) {
			AbstractCommandsEventHandler handler = ModifiedStateChanged;
			if (handler != null) handler(this, command);
		}

		public delegate void AbstractCommandsEventHandler(object sender, T command);

		/// <summary>
		/// Gets a value indicating whether this instance is modified.
		/// </summary>
		public virtual bool IsModified {
			get { return _commandIndexCurrent != _commandIndexNonModified; }
		}

		/// <summary>
		/// Gets a value indicating whether this instance can undo.
		/// </summary>
		public virtual bool CanUndo {
			get {
				return (_commandIndexCurrent > -1);
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance can redo.
		/// </summary>
		public virtual bool CanRedo {
			get {
				return (_commandIndexCurrent < _commands.Count - 1);
			}
		}

		/// <summary>
		/// Saves this instance.
		/// </summary>
		public void SaveCommandIndex() {
			_commandIndexNonModified = _commandIndexCurrent;
			_commandIndexModified = _commandIndexCurrent;
			_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
			_onModifiedStateChanged(default(T));
		}

		/// <summary>
		/// Store a command.
		/// </summary>
		/// <param name="command">The command.</param>
		public virtual void Store(T command) {
			lock (_thisLock) {
				IsLocked = true;

				try {
					if (_mergeDown(command)) {
						if (_commandIndexNonModified == _commandIndexCurrent) {
							if (_commandIndexNonModified >= 0) {
								_commandIndexNonModified = -2;
							}
						}

						_commandIndexModified = _commandIndexCurrent;
						_onModifiedStateChanged(default(T));
						return;
					}

					if (IsDelayed) {
						_delayedCommands.Add(command);
						IGroupCommand<T> commandGroup = _delayedCommandsCommand;
						commandGroup.Processing(command);
						return;
					}

					while (_commandIndexCurrent < _commands.Count - 1) {
						_commands.RemoveAt(_commands.Count - 1);
					}

					if (_commandIndexNonModified >= 0) {
						if (_commandIndexCurrent < _commandIndexNonModified) {
							_commandIndexNonModified = -2;
						}
					}

					_commands.Add(command);
					_commandIndexCurrent = _commands.Count - 1;
					_commandIndexModified = _commandIndexCurrent;
					_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
					_onModifiedStateChanged(default(T));
				}
				finally {
					IsLocked = false;
				}
			}
		}

		/// <summary>
		/// Store and execute a command.
		/// </summary>
		/// <param name="command">The command.</param>
		public virtual void StoreAndExecute(T command) {
			lock (_thisLock) {
				IsLocked = true;

				BackupCommandStack stack = new BackupCommandStack(this);

				try {
					if (_mergeDown(command)) {
						if (_commandIndexNonModified == _commandIndexCurrent) {
							if (_commandIndexNonModified >= 0) {
								_commandIndexNonModified = -2;
							}
						}

						_commandIndexModified = _commandIndexCurrent;
						_onModifiedStateChanged(default(T));
						return;
					}

					if (IsDelayed) {
						_delayedCommands.Add(command);
						_delayedCommandsCommand.Processing(command);
						return;
					}

					while (_commandIndexCurrent < _commands.Count - 1) {
						_commands.RemoveAt(_commands.Count - 1);
					}

					if (_commandIndexNonModified >= 0) {
						if (_commandIndexCurrent < _commandIndexNonModified) {
							_commandIndexNonModified = -2;
						}
					}

					_onPreviewCommandExecuted(command);
					_execute(command);
					_commands.Add(command);
					_commandIndexCurrent = _commands.Count - 1;
					_commandIndexModified = _commandIndexCurrent;
					_onCommandExecuted(command);
					_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
					_onModifiedStateChanged(default(T));
				}
				catch (CancelAbstractCommand) {
					stack.Restore(this);
					_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
				}
				finally {
					IsLocked = false;
				}
			}
		}

		private bool _mergeDown(T command) {
			var commandAdded = command as ICombinableCommand;

			if (commandAdded != null) {
				if (IsDelayed && _delayedCommands.Count > 0 ||
					!IsDelayed && _commands.Count > 0 && _commandIndexCurrent > -1) {
					T lastCommand = IsDelayed ? _delayedCommands.Last() : Current;

					var combinableCommand = lastCommand as ICombinableCommand;

					if (combinableCommand != null) {
						var deleteCommandFrom = lastCommand as IAutoReverse;
						var deleteCommandTo = command as IAutoReverse;

						if (combinableCommand.CanCombine(commandAdded)) {
							combinableCommand.Combine(commandAdded, this);

							if (deleteCommandFrom != null && deleteCommandTo != null) {
								if (deleteCommandFrom.CanDelete(deleteCommandTo)) {
									Undo();
									RemoveLastCommand();
									return true;
								}
							}

							return true;
						}
					}
				}
			}

			return false;
		}

		public void ExplicitCommandExecution(T command) {
			_execute(command);
		}

		public class BackupCommandStack {
			public List<T> Commands;
			public List<T> DelayedCommands;
			public int CommandIndexNonModified;
			public int CommandIndexCurrent;

			public BackupCommandStack(AbstractCommand<T> ac) {
				Commands = new List<T>(ac._commands);
				CommandIndexNonModified = ac._commandIndexNonModified;
				CommandIndexCurrent = ac._commandIndexCurrent;
				DelayedCommands = new List<T>(ac._delayedCommands);
			}

			public void Restore(AbstractCommand<T> ac) {
				ac._commands.Clear();
				ac._commands.AddRange(Commands);
				ac._delayedCommands.Clear();
				ac._delayedCommands.AddRange(DelayedCommands);
				ac._commandIndexNonModified = CommandIndexNonModified;
				ac._commandIndexCurrent = CommandIndexCurrent;
			}
		}

		public void StoreAndExecute(List<T> commands) {
			lock (_thisLock) {
				IsLocked = true;

				BackupCommandStack stack = new BackupCommandStack(this);

				try {
					if (commands.Count > 0) {
						while (_commandIndexCurrent < _commands.Count - 1) {
							_commands.RemoveAt(_commands.Count - 1);
						}

						IGroupCommand<T> commandGroup = _delayedCommandsCommand;
						commandGroup.AddRange(commands);
						T command = (T)commandGroup;

						if (_commandIndexNonModified >= 0) {
							if (_commandIndexCurrent < _commandIndexNonModified) {
								_setNonModifiedIndex(-2);
							}
						}

						_onPreviewCommandExecuted(command);
						_execute(command);

						if (commandGroup.Commands.Count > 0) {
							_commands.Add(command);
						}
						else {
							throw new CancelAbstractCommand(new AbstractCommandArg {Cancel = true});
						}

						_commandIndexCurrent = _commands.Count - 1;
						_commandIndexModified = _commandIndexCurrent;
						_onCommandExecuted(command);
						_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
						_onModifiedStateChanged(default(T));
					}
				}
				catch (CancelAbstractCommand) {
					stack.Restore(this);
					_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
				}
				finally {
					IsLocked = false;
				}
			}
		}

		protected abstract void _execute(T command);
		protected abstract void _undo(T command);
		protected abstract void _redo(T command);

		/// <summary>
		/// Undo the latest operation.
		/// </summary>
		/// <returns>True on sucess, false otherwise.</returns>
		public virtual bool Undo() {
			lock (_thisLock) {
				IsLocked = true;

				try {
					if (_commandIndexCurrent <= -1) return false;

					_commandIndexModified = _commandIndexCurrent;
					_onPreviewCommandUndo(_commands[_commandIndexCurrent]);
					_undo(_commands[_commandIndexCurrent]);
					_onCommandUndo(_commands[_commandIndexCurrent]);
					_commandIndexCurrent = _commandIndexCurrent <= -1 ? -1 : _commandIndexCurrent - 1;
					_onCommandIndexChanged(_commands[_commandIndexCurrent + 1]);
					_onModifiedStateChanged(default(T));
					return true;
				}
				finally {
					IsLocked = false;
				}
			}
		}

		/// <summary>
		/// Redo the latest operation.
		/// </summary>
		/// <returns>True on sucess, false otherwise.</returns>
		public virtual bool Redo() {
			lock (_thisLock) {
				IsLocked = true;

				try {
					if (_commandIndexCurrent >= _commands.Count - 1) return false;

					_commandIndexCurrent++;
					_commandIndexModified = _commandIndexCurrent;
					_onPreviewCommandRedo(_commands[_commandIndexCurrent]);
					_redo(_commands[_commandIndexCurrent]);
					_onCommandRedo(_commands[_commandIndexCurrent]);
					_onCommandIndexChanged(_commands[_commandIndexCurrent]);
					_onModifiedStateChanged(default(T));
					return true;
				}
				finally {
					IsLocked = false;
				}
			}
		}

		/// <summary>
		/// Clears all commands stored, you can't use Undo after this operation.
		/// </summary>
		public virtual void ClearCommands() {
			int oldIndex = _commandIndexCurrent;
			var count = _commands.Count;

			foreach (var command in _commands) {
				if (command is IGroupCommand<T>) {
					var groupCommand = (IGroupCommand<T>)command;
					groupCommand.Commands.Clear();
				}
			}

			_commandIndexCurrent = -1;
			_setNonModifiedIndex(-1);
			_commands.Clear();

			if (oldIndex != _commandIndexCurrent || count > 0)
				_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
		}

		/// <summary>
		/// Removes a number of commands which cannot be undone.
		/// </summary>
		/// <param name="count">The number of commands to remove silently.</param>
		public void RemoveCommands(int count) {
			int oldIndex = _commandIndexCurrent;

			_commandIndexCurrent -= count;
			if (_commandIndexCurrent < -1)
				_commandIndexCurrent = -1;

			int numberRemoved = 0;

			while (_commandIndexCurrent < _commands.Count - 1) {
				_commands.RemoveAt(_commands.Count - 1);
				numberRemoved++;
			}

			if (oldIndex != _commandIndexCurrent || numberRemoved > 0) {
				_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
				_commandIndexModified = _commandIndexCurrent;
				_onModifiedStateChanged(default(T));
			}
		}

		internal void RemoveLastCommand() {
			if (_commands.Count > 0) {
				_commands.RemoveAt(_commands.Count - 1);
				_onCommandIndexChanged(_commandIndexCurrent <= -1 ? default(T) : _commands[_commandIndexCurrent]);
				_commandIndexModified = _commandIndexCurrent;
				_onModifiedStateChanged(default(T));
			}
		}

		public virtual List<T> GetUndoCommands() {
			if (CanUndo)
				return _commands.Take(_commandIndexCurrent + 1).ToList();

			return null;
		}

		public virtual List<T> GetRedoCommands() {
			if (CanRedo)
				return _commands.Skip(_commandIndexCurrent + 1).ToList();

			return null;
		}

		/// <summary>
		/// Determines whether this instance has the command specified.
		/// </summary>
		/// <typeparam name="TFind">The type of the command to find.</typeparam>
		/// <returns>
		///   <c>true</c> if this instance has the command specified; otherwise, <c>false</c>.
		/// </returns>
		public bool HasCommand<TFind>() {
			var list = GetUndoCommands();

			if (list == null) return false;

			foreach (T command in list) {
				var group = command as IGroupCommand<T>;

				if (group != null) {
					if (@group.Commands.OfType<TFind>().Any()) {
						return true;
					}
				}

				if (command is TFind)
					return true;
			}

			return false;
		}

		public virtual void SetCommands(List<T> commands) {
			_commands.Clear();
			_commands.AddRange(commands);
			_commandIndexModified = _commandIndexCurrent;
			_onModifiedStateChanged(default(T));
		}

		public virtual void SetCommandIndex(int commandIndex) {
			_commandIndexCurrent = commandIndex;
			_commandIndexModified = _commandIndexCurrent;
			_onCommandIndexChanged(_commandIndexCurrent <= -1 || _commandIndexCurrent < _commands.Count ? default(T) : _commands[_commandIndexCurrent]);
			_onModifiedStateChanged(default(T));
		}

		public virtual void BeginEdit(IGroupCommand<T> command) {
			lock (_thisLock) {
				IsLocked = true;

				try {
					_delayedCommandsCommand = command;
					IsDelayed = true;
				}
				finally {
					IsLocked = false;
				}
			}
		}

		public virtual void EndEdit() {
			lock (_thisLock) {
				IsLocked = true;

				try {
					if (_delayedCommands.Count != 0) {
						StoreAndExecute(_delayedCommands);
						try {
							_delayedCommandsCommand.Close();
						}
						catch { }
					}
				}
				finally {
					IsLocked = false;
					_delayedCommands.Clear();
					IsDelayed = false;
				}
			}
		}

		public virtual void CancelEdit() {
			try {
				lock (_thisLock) {
					IsLocked = true;

					try {
						_delayedCommandsCommand.AddRange(_delayedCommands);
						T command = (T)_delayedCommandsCommand;
						_undo(command);
						_delayedCommands.Clear();
					}
					finally {
						IsLocked = false;
					}
				}
			}
			catch { }
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				Debug.Ignore(ClearCommands);
			}
		}

		public virtual void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Undoes all commands.
		/// </summary>
		public void UndoAll() {
			while (CanUndo) {
				Undo();
			}
		}

		private void _setNonModifiedIndex(int index) {
			bool isModified = IsModified;
			_commandIndexNonModified = index;
			if (isModified != IsModified)
				_onModifiedStateChanged(default(T));
		}
	}
}
