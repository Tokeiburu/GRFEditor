using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ErrorManager;
using Utilities.Extension;

namespace TokeiLibrary.Shortcuts {
	public class KeyBinding {
		public InputBinding InputBinding { get; set; }
		public CommandBinding CommandBinding { get; set; }
		public KeyGesture KeyGesture { get; set; }
		public UIElement Holder { get; set; }
		public Action Action { get; set; }
		public KeyBinding Next { get; set; }

		public bool CanReset {
			get { return OriginalBinding != null; }
		}

		internal KeyBinding OriginalBinding { get; set; }

		internal KeyBinding() {
		}

		internal KeyBinding(KeyBinding keyBinding) {
			InputBinding = keyBinding.InputBinding;
			CommandBinding = keyBinding.CommandBinding;
			KeyGesture = keyBinding.KeyGesture;
			Holder = keyBinding.Holder;
			Action = keyBinding.Action;
			Next = keyBinding.Next;
		}

		public void Set(AdvKeyGesture gesture) {
			if (OriginalBinding == null)
				OriginalBinding = new KeyBinding(this);

			Holder.InputBindings.Remove(InputBinding);
			Holder.CommandBindings.Remove(CommandBinding);

			KeyGesture = gesture;

			ICommand iCommand = new CustomCommand(Action);
			InputBinding ib = new InputBinding(iCommand, gesture);

			Holder.InputBindings.Add(ib);
			CommandBinding cb = new CommandBinding(iCommand);

			CommandBinding = cb;
			InputBinding = ib;

			Holder.CommandBindings.Add(cb);
		}

		public void Reset() {
			if (OriginalBinding != null) {
				Holder.InputBindings.Remove(InputBinding);
				Holder.CommandBindings.Remove(CommandBinding);

				InputBinding = OriginalBinding.InputBinding;
				CommandBinding = OriginalBinding.CommandBinding;
				KeyGesture = OriginalBinding.KeyGesture;
				Holder = OriginalBinding.Holder;
				Action = OriginalBinding.Action;
				Next = OriginalBinding.Next;

				ICommand iCommand = new CustomCommand(Action);
				InputBinding ib = new InputBinding(iCommand, KeyGesture);

				Holder.InputBindings.Add(ib);
				CommandBinding cb = new CommandBinding(iCommand);
				Holder.CommandBindings.Add(cb);

				OriginalBinding = null;
			}
		}
	}

	public class TwoKeyDictionary<TK1, TK2, T> : Dictionary<TK1, Dictionary<TK2, T>> {
		public T this[TK1 index1, TK2 index2] {
			get { return this[index1][index2]; }
			set {
				if (!this.ContainsKey(index1))
					this[index1] = new Dictionary<TK2, T>();

				this[index1][index2] = value;
			}
		}
	}

	public class Shortcut : InputBinding {
		public string CommandName { get; set; }
		public string DisplayString { get; set; }

		public KeyGesture KeyGesture { get; private set; }
		public System.Windows.Input.KeyBinding KeyBinding { get; private set; }

		public Shortcut(Key key, ModifierKeys modifiers, string displayString, string cmdName) {
			CommandName = cmdName;
			DisplayString = displayString;

			try {
				KeyGesture = new KeyGesture(key, modifiers, displayString);
				var binding = (InputBinding)KeyGesture;

			}
			catch {
				KeyBinding = new System.Windows.Input.KeyBinding { Key = key };
			}
		}
	}

	public class AdvKeyGesture : KeyGesture {
		public AdvKeyGesture(Key key) : base(key) {
		}

		public AdvKeyGesture(Key key, ModifierKeys modifiers) : base(key, modifiers) {
		}

		public AdvKeyGesture(Key key, ModifierKeys modifiers, string displayString) : base(key, modifiers, displayString) {
		}

		public AdvKeyGesture(Key key, ModifierKeys modifiers, string displayString, string cmdName)
			: base(key, modifiers, displayString) {
				CommandName = cmdName;
		}

		public string CommandName { get; set; }
	}

	public sealed class ApplicationShortcut {
		private static AdvKeyGesture _make(string cmdName, Key key, ModifierKeys modifiers = ModifierKeys.None) {
			string display = "";

			if (modifiers.HasFlags(ModifierKeys.Control)) {
				display += "Ctrl-";
			}
			if (modifiers.HasFlags(ModifierKeys.Shift)) {
				display += "Shift-";
			}
			if (modifiers.HasFlags(ModifierKeys.Alt)) {
				display += "Alt-";
			}
			if (modifiers.HasFlags(ModifierKeys.Windows)) {
				display += "Win-";
			}

			display += key;

			return new AdvKeyGesture(key, modifiers, display, cmdName);
		}

		public static readonly InputBinding Undo = _make("Undo", Key.Z, ModifierKeys.Control);
		public static readonly InputBinding UndoGlobal = _make("Undo global", Key.Z, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly InputBinding UndoLocal = _make("Undo local", Key.Z, ModifierKeys.Alt);
		public static readonly KeyGesture NavigationForward = _make("Navigation forward", Key.Y, ModifierKeys.Alt);
		public static readonly KeyGesture NavigationBackward = _make("Navigation backward", Key.Z, ModifierKeys.Alt);
		public static readonly KeyGesture NavigationBackward2 = _make("Navigation backward2", Key.X, ModifierKeys.Alt);
		public static readonly KeyGesture Redo = _make("Redo", Key.Y, ModifierKeys.Control);
		public static readonly KeyGesture RedoGlobal = _make("Redo global", Key.Y, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly KeyGesture RedoLocal = _make("Redo local", Key.Y, ModifierKeys.Alt);
		public static readonly KeyGesture Search = _make("Search", Key.F, ModifierKeys.Control);
		public static readonly KeyGesture Replace = _make("Replace", Key.H, ModifierKeys.Control);
		public static readonly KeyGesture Delete = _make("Delete", Key.Delete);
		public static readonly KeyGesture Confirm = _make("Confirm", Key.Enter);
		public static readonly KeyGesture Copy = _make("Copy", Key.C, ModifierKeys.Control);
		public static readonly KeyGesture Paste = _make("Paste", Key.V, ModifierKeys.Control);
		public static readonly KeyGesture AdvancedPaste = _make("Paste2", Key.V, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly KeyGesture AdvancedPaste2 = _make("Paste3", Key.B, ModifierKeys.Control);
		public static readonly KeyGesture Open = _make("Open", Key.O, ModifierKeys.Control);
		public static readonly KeyGesture Copy2 = _make("Copy2", Key.C, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly KeyGesture Cut = _make("Cut", Key.X, ModifierKeys.Control);
		public static readonly KeyGesture Rename = _make("Rename", Key.F2);
		public static readonly KeyGesture Change = _make("Change", Key.D, ModifierKeys.Control);
		public static readonly KeyGesture Restrict = _make("Restrict", Key.R, ModifierKeys.Control);
		public static readonly KeyGesture New = _make("New", Key.N, ModifierKeys.Control);
		public static readonly KeyGesture Save = _make("Save", Key.S, ModifierKeys.Control);
		public static readonly KeyGesture SaveAll = _make("Save all", Key.S, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly KeyGesture SaveSpecial = _make("Save special", Key.S, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly KeyGesture CopyTo = _make("Copy to", Key.D, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly KeyGesture CopyTo2 = _make("Copy to2", Key.D, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly KeyGesture MoveLineUp = _make("Move line up", Key.Up, ModifierKeys.Control);
		public static readonly KeyGesture MoveLineDown = _make("Move line up", Key.Down, ModifierKeys.Control);
		public static readonly KeyGesture Select = _make("Select", Key.E, ModifierKeys.Control);

		private static readonly TwoKeyDictionary<string, string, KeyBinding> _keyBindings = new TwoKeyDictionary<string, string, KeyBinding>();
		private static readonly Dictionary<string, KeyBinding> _keyBindings2 = new Dictionary<string, KeyBinding>();
		private static IDictionary<string, string> _override = new Dictionary<string, string>();

		public static TwoKeyDictionary<string, string, KeyBinding> KeyBindings {
			get { return _keyBindings; }
		}

		public static Dictionary<string, KeyBinding> KeyBindings2 {
			get { return _keyBindings2; }
		}

		public static void ResetBindings() {
			foreach (var shortcut in _keyBindings2) {
				shortcut.Value.Reset();
			}
		}

		public static bool IsCommandActive() {
			return _keyBindings2.Any(shortcut => Is(shortcut.Value.KeyGesture));
		}

		public static void OverrideBindings(IDictionary<string, string> shortcuts) {
			_override = shortcuts;

			foreach (var shortcut in _keyBindings2) {
				if (_override.ContainsKey(shortcut.Key)) {
					var binding = shortcut.Value;

					try {
						binding.Set(FromString(_override[shortcut.Key], shortcut.Key));
					}
					catch {
					}
				}
			}
		}

		public static void Link(KeyGesture keyGesture, string cmdName, Action command, FrameworkElement holder) {
			Link(new AdvKeyGesture(keyGesture.Key, keyGesture.Modifiers, keyGesture.DisplayString, cmdName), command, holder.Name, holder);
		}

		public static void Link(string gesture, string cmdName, Action command, FrameworkElement holder) {
			Link(FromString(gesture, cmdName), command, holder.Name, holder);
		}

		public static void Link(string gesture, string cmdName, Action command, string group, UIElement holder) {
			Link(FromString(gesture, cmdName), command, group, holder);
		}

		public static void Link(KeyGesture gesture, Action command, FrameworkElement holder) {
			Link(gesture, command, holder.Name, holder);
		}

		public static void Link(KeyGesture gesture, Action command, string group, UIElement holder) {
			var advGesture = gesture as AdvKeyGesture;

			var cmdName = gesture.DisplayString;

			if (advGesture != null && advGesture.CommandName != null) {
				cmdName = advGesture.CommandName;
			}

			ICommand iCommand = new CustomCommand(command);
			InputBinding ib = new InputBinding(iCommand, gesture);

			holder.InputBindings.Add(ib);
			CommandBinding cb = new CommandBinding(iCommand);
			holder.CommandBindings.Add(cb);

			KeyBinding binding = new KeyBinding { Action = command, Holder = holder, InputBinding = ib, KeyGesture = gesture, CommandBinding = cb };
			_keyBindings[group, gesture.DisplayString] = binding;

			if (_keyBindings2.ContainsKey(cmdName)) {
				var t = _keyBindings2[cmdName];
				t.Next = binding;
			}
			else {
				_keyBindings2[cmdName] = binding;
			}

			if (_override.ContainsKey(cmdName)) {
				var t = _keyBindings2[cmdName];
				try {
					var newGesture = FromString(_override[cmdName], cmdName);
					t.Set(newGesture);
					//gesture.DisplayString = newGesture;
				}
				catch { }
			}
		}

		public static void Link(KeyGesture gesture, Action command, UIElement holder, string commandName) {
			ICommand iCommand = new CustomCommand(command);
			InputBinding ib = new InputBinding(iCommand, gesture);

			holder.InputBindings.Add(ib);
			CommandBinding cb = new CommandBinding(iCommand);
			holder.CommandBindings.Add(cb);

			KeyBinding binding = new KeyBinding { Action = command, Holder = holder, InputBinding = ib, KeyGesture = gesture, CommandBinding = cb };

			if (_keyBindings2.ContainsKey(commandName)) {
				var t = _keyBindings2[commandName];
				t.Next = binding;
			}
			else {
				_keyBindings2[commandName] = binding;
			}
		}

		public static void Execute(KeyGesture gesture, string group) {
			_keyBindings[group, gesture.DisplayString].Action();
		}

		public static KeyGesture GetGesture(string commandName) {
			if (_keyBindings2.ContainsKey(commandName))
				return _keyBindings2[commandName].KeyGesture;

			return null;
		}

		public static void Execute(string commandName) {
			_keyBindings2[commandName].Action();
		}

		public static void Execute(KeyGesture gesture, FrameworkElement element) {
			_keyBindings[element.Name, gesture.DisplayString].Action();
		}

		public static void DeleteLink(string commandName) {
			if (_keyBindings.ContainsKey(commandName)) {
				_keyBindings.Remove(commandName);
			}

			if (_keyBindings2.ContainsKey(commandName)) {
				_keyBindings2.Remove(commandName);
			}
		}

		public static string FindDislayName(KeyGesture gesture) {
			var advGesture = gesture as AdvKeyGesture;

			if (advGesture != null) {
				if (_override.ContainsKey(advGesture.CommandName))
					return _override[advGesture.CommandName];

				if (_keyBindings2.ContainsKey(advGesture.CommandName))
					return _keyBindings2[advGesture.CommandName].KeyGesture.DisplayString;
			}

			return gesture.DisplayString;
		}

		public static bool Is(object shortcut) {
			var keyGesture = shortcut as KeyGesture;

			if (keyGesture != null) {
				return (Keyboard.Modifiers & keyGesture.Modifiers) == keyGesture.Modifiers && Keyboard.IsKeyDown(keyGesture.Key);
			}

			var keyBinding = shortcut as KeyBinding;

			if (keyBinding != null) {
				return Keyboard.IsKeyDown(keyBinding.KeyGesture.Key);
			}

			return false;
		}

		//public static AdvKeyGesture FromString(string keyGesture) {
		//	return FromString(keyGesture, null);
		//}

		public static AdvKeyGesture Make(string cmdName, Key key, ModifierKeys modifiers) {
			return _make(cmdName, key, modifiers);
		}

		public static AdvKeyGesture Make(string cmdName, Key key) {
			return _make(cmdName, key);
		}

		public static AdvKeyGesture FromString(string keyGesture, string cmdName) {
			List<string> gesture = keyGesture.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			ModifierKeys keys = 0;
			Key key = Key.A;

			for (int i = 0; i < gesture.Count; i++) {
				if (String.Compare(gesture[i], "shift", StringComparison.OrdinalIgnoreCase) == 0) {
					keys |= ModifierKeys.Shift;
				}

				else if (String.Compare(gesture[i], "ctrl", StringComparison.OrdinalIgnoreCase) == 0) {
					keys |= ModifierKeys.Control;
				}

				else if (String.Compare(gesture[i], "alt", StringComparison.OrdinalIgnoreCase) == 0) {
					keys |= ModifierKeys.Alt;
				}
				else {
					try {
						KeyConverter k = new KeyConverter();
						object keyObj = k.ConvertFromString(gesture[i]);

						if (keyObj == null)
							continue;

						key = (Key) keyObj;
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			}

			return new AdvKeyGesture(key, keys, keyGesture, cmdName);
		}
	}

	internal class CustomCommand : ICommand {
		private readonly Action _command;

		public CustomCommand(Action command) {
			_command = command;
		}

		public void Execute(object parameter) {
			_command();
		}

		public bool CanExecute(object parameter) {
			return true;
		}

		public event EventHandler CanExecuteChanged;

		public void OnCanExecuteChanged(EventArgs e) {
			EventHandler handler = CanExecuteChanged;
			if (handler != null) handler(this, e);
		}
	}
}
