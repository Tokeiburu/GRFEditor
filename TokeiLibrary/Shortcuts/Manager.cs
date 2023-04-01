using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ErrorManager;
using Utilities.Extension;

namespace TokeiLibrary.Shortcuts {
	public class AdvKeyBinding {
		public InputBinding InputBinding { get; set; }
		public CommandBinding CommandBinding { get; set; }
		public Shortcut KeyGesture { get; set; }
		public UIElement Holder { get; set; }
		public Action Action { get; set; }
		public AdvKeyBinding Next { get; set; }

		public bool CanReset {
			get { return OriginalBinding != null; }
		}

		internal AdvKeyBinding OriginalBinding { get; set; }

		internal AdvKeyBinding() {
		}

		internal AdvKeyBinding(AdvKeyBinding keyBinding) {
			InputBinding = keyBinding.InputBinding;
			CommandBinding = keyBinding.CommandBinding;
			KeyGesture = keyBinding.KeyGesture;
			Holder = keyBinding.Holder;
			Action = keyBinding.Action;
			Next = keyBinding.Next;
		}

		public void Set(Shortcut gesture) {
			if (OriginalBinding == null)
				OriginalBinding = new AdvKeyBinding(this);

			Holder.InputBindings.Remove(InputBinding);
			Holder.CommandBindings.Remove(CommandBinding);

			KeyGesture = gesture;

			ICommand iCommand = new CustomCommand(Action);
			InputBinding ib = new InputBinding(iCommand, gesture.Gesture);

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
				InputBinding ib = new InputBinding(iCommand, KeyGesture.Gesture);

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

		public Shortcut(Shortcut shortcut, string cmdName) {
			CommandName = cmdName;
			DisplayString = shortcut.DisplayString;

			if (shortcut.KeyGesture != null) {
				KeyGesture = new KeyGesture(shortcut.KeyGesture.Key, shortcut.KeyGesture.Modifiers, shortcut.KeyGesture.DisplayString);
			}
			else {
				KeyBinding = new KeyBinding { Key = shortcut.KeyBinding.Key };
			}
		}

		public Shortcut(Key key, ModifierKeys modifiers, string displayString, string cmdName) {
			CommandName = cmdName;
			DisplayString = displayString;

			try {
				KeyGesture = new KeyGesture(key, modifiers, displayString);
			}
			catch {
				KeyBinding = new System.Windows.Input.KeyBinding { Key = key };
			}
		}

		public override InputGesture Gesture {
			get {
				return KeyGesture ?? KeyBinding.Gesture;
			}
			set {
				base.Gesture = value;
			}
		}
	}

	//public class AdvKeyGesture : KeyGesture {
	//	public AdvKeyGesture(Key key) : base(key) {
	//	}
	//
	//	public AdvKeyGesture(Key key, ModifierKeys modifiers) : base(key, modifiers) {
	//	}
	//
	//	public AdvKeyGesture(Key key, ModifierKeys modifiers, string displayString) : base(key, modifiers, displayString) {
	//	}
	//
	//	public AdvKeyGesture(Key key, ModifierKeys modifiers, string displayString, string cmdName)
	//		: base(key, modifiers, displayString) {
	//			CommandName = cmdName;
	//	}
	//
	//	public string CommandName { get; set; }
	//}

	public sealed class ApplicationShortcut {
		private static Shortcut _make(string cmdName, Key key, ModifierKeys modifiers = ModifierKeys.None) {
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

			return new Shortcut(key, modifiers, display, cmdName);
		}

		public static readonly Shortcut Undo = _make("Undo", Key.Z, ModifierKeys.Control);
		public static readonly Shortcut UndoGlobal = _make("Undo global", Key.Z, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly Shortcut UndoLocal = _make("Undo local", Key.Z, ModifierKeys.Alt);
		public static readonly Shortcut NavigationForward = _make("Navigation forward", Key.Y, ModifierKeys.Alt);
		public static readonly Shortcut NavigationBackward = _make("Navigation backward", Key.Z, ModifierKeys.Alt);
		public static readonly Shortcut NavigationBackward2 = _make("Navigation backward2", Key.X, ModifierKeys.Alt);
		public static readonly Shortcut Redo = _make("Redo", Key.Y, ModifierKeys.Control);
		public static readonly Shortcut RedoGlobal = _make("Redo global", Key.Y, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly Shortcut RedoLocal = _make("Redo local", Key.Y, ModifierKeys.Alt);
		public static readonly Shortcut Search = _make("Search", Key.F, ModifierKeys.Control);
		public static readonly Shortcut Replace = _make("Replace", Key.H, ModifierKeys.Control);
		public static readonly Shortcut Delete = _make("Delete", Key.Delete);
		public static readonly Shortcut Confirm = _make("Confirm", Key.Enter);
		public static readonly Shortcut Copy = _make("Copy", Key.C, ModifierKeys.Control);
		public static readonly Shortcut Paste = _make("Paste", Key.V, ModifierKeys.Control);
		public static readonly Shortcut AdvancedPaste = _make("Paste2", Key.V, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly Shortcut AdvancedPaste2 = _make("Paste3", Key.B, ModifierKeys.Control);
		public static readonly Shortcut Open = _make("Open", Key.O, ModifierKeys.Control);
		public static readonly Shortcut Copy2 = _make("Copy2", Key.C, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly Shortcut Cut = _make("Cut", Key.X, ModifierKeys.Control);
		public static readonly Shortcut Rename = _make("Rename", Key.F2);
		public static readonly Shortcut Change = _make("Change", Key.D, ModifierKeys.Control);
		public static readonly Shortcut Restrict = _make("Restrict", Key.R, ModifierKeys.Control);
		public static readonly Shortcut New = _make("New", Key.N, ModifierKeys.Control);
		public static readonly Shortcut Save = _make("Save", Key.S, ModifierKeys.Control);
		public static readonly Shortcut SaveAll = _make("Save all", Key.S, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly Shortcut SaveSpecial = _make("Save special", Key.S, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly Shortcut CopyTo = _make("Copy to", Key.D, ModifierKeys.Control | ModifierKeys.Shift);
		public static readonly Shortcut CopyTo2 = _make("Copy to2", Key.D, ModifierKeys.Control | ModifierKeys.Alt);
		public static readonly Shortcut MoveLineUp = _make("Move line up", Key.Up, ModifierKeys.Control);
		public static readonly Shortcut MoveLineDown = _make("Move line up", Key.Down, ModifierKeys.Control);
		public static readonly Shortcut Select = _make("Select", Key.E, ModifierKeys.Control);

		private static readonly TwoKeyDictionary<string, string, AdvKeyBinding> _keyBindings = new TwoKeyDictionary<string, string, AdvKeyBinding>();
		private static readonly Dictionary<string, AdvKeyBinding> _keyBindings2 = new Dictionary<string, AdvKeyBinding>();
		private static IDictionary<string, string> _override = new Dictionary<string, string>();

		public static TwoKeyDictionary<string, string, AdvKeyBinding> KeyBindings {
			get { return _keyBindings; }
		}

		public static Dictionary<string, AdvKeyBinding> KeyBindings2 {
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

		public static void Link(Shortcut shortcut, Action command, FrameworkElement holder) {
			Link(shortcut, command, holder.Name, holder);
		}

		public static void Link(string gesture, string cmdName, Action command, FrameworkElement holder) {
			Link(FromString(gesture, cmdName), command, holder.Name, holder);
		}

		public static void Link(string gesture, string cmdName, Action command, string group, UIElement holder) {
			Link(FromString(gesture, cmdName), command, group, holder);
		}

		public static void Link(Shortcut shortcut, string cmdName, Action command, FrameworkElement holder) {
			Link(new Shortcut(shortcut, cmdName), command, holder.Name, holder);
		}

		//ApplicationShortcut.Link(ApplicationShortcut.Delete, "MultiGrf.Delete", () => _menuItemsDelete_Click(null, null), _itemsResources);

		//public static void Link(KeyGesture keyGesture, Action command, FrameworkElement holder) {
		//	Link(new Shortcut(keyGesture.Key, keyGesture.Modifiers, keyGesture.DisplayString, ""), command, holder.Name, holder);
		//}

		public static void Link(Shortcut gesture, Action command, string group, UIElement holder) {
			var advGesture = gesture as Shortcut;

			var cmdName = gesture.DisplayString;

			if (advGesture != null && advGesture.CommandName != null) {
				cmdName = advGesture.CommandName;
			}

			ICommand iCommand = new CustomCommand(command);
			InputBinding ib = new InputBinding(iCommand, gesture.Gesture);

			holder.InputBindings.Add(ib);
			CommandBinding cb = new CommandBinding(iCommand);
			holder.CommandBindings.Add(cb);

			AdvKeyBinding binding = new AdvKeyBinding { Action = command, Holder = holder, InputBinding = ib, KeyGesture = gesture, CommandBinding = cb };
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

		public static void Link(Shortcut gesture, Action command, UIElement holder, string commandName) {
			ICommand iCommand = new CustomCommand(command);
			InputBinding ib = new InputBinding(iCommand, gesture.Gesture);

			holder.InputBindings.Add(ib);
			CommandBinding cb = new CommandBinding(iCommand);
			holder.CommandBindings.Add(cb);

			AdvKeyBinding binding = new AdvKeyBinding { Action = command, Holder = holder, InputBinding = ib, KeyGesture = gesture, CommandBinding = cb };

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

		public static Shortcut GetGesture(string commandName) {
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

		public static string FindDislayName(Shortcut gesture) {
			if (_override.ContainsKey(gesture.CommandName))
				return _override[gesture.CommandName];

			if (_keyBindings2.ContainsKey(gesture.CommandName))
				return _keyBindings2[gesture.CommandName].KeyGesture.DisplayString;

			return gesture.DisplayString;
		}

		public static bool Is(Shortcut shortcut) {
			if (shortcut.KeyGesture != null) {
				return (Keyboard.Modifiers & shortcut.KeyGesture.Modifiers) == shortcut.KeyGesture.Modifiers && Keyboard.IsKeyDown(shortcut.KeyGesture.Key);
			}

			if (shortcut.KeyBinding != null) {
				return Keyboard.IsKeyDown(shortcut.KeyBinding.Key);
			}

			return false;
		}

		//public static AdvKeyGesture FromString(string keyGesture) {
		//	return FromString(keyGesture, null);
		//}

		public static Shortcut Make(string cmdName, Key key, ModifierKeys modifiers) {
			return _make(cmdName, key, modifiers);
		}

		public static Shortcut Make(string cmdName, Key key) {
			return _make(cmdName, key);
		}

		public static Shortcut FromString(string keyGesture, string cmdName) {
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

			return new Shortcut(key, keys, keyGesture, cmdName);
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
