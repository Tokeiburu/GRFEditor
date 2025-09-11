using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ErrorManager;
using Utilities;
using Utilities.Extension;

namespace TokeiLibrary.Shortcuts {
	public class AdvKeyBinding {
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
			KeyGesture = keyBinding.KeyGesture;
			Holder = keyBinding.Holder;
			Action = keyBinding.Action;
			Next = keyBinding.Next;
		}

		public void Set(Shortcut gesture) {
			if (OriginalBinding == null)
				OriginalBinding = new AdvKeyBinding(this);

			// Ensure the command is the same
			gesture.Command = KeyGesture.Command;
			ApplicationShortcut.RemoveRawGesture(Holder, KeyGesture);
			ApplicationShortcut.SetRawGesture(Holder, gesture);
			KeyGesture = gesture;
		}

		public void Reset() {
			if (OriginalBinding != null) {
				ApplicationShortcut.RemoveRawGesture(Holder, KeyGesture);
				ApplicationShortcut.SetRawGesture(Holder, OriginalBinding.KeyGesture);
				Action = OriginalBinding.Action;
				KeyGesture = OriginalBinding.KeyGesture;
				Holder = OriginalBinding.Holder;
				Next = OriginalBinding.Next;
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

		public KeyBinding KeyBinding { get; private set; }

		public Shortcut(string cmdName) {
			CommandName = cmdName;
			DisplayString = "Not assigned";
			KeyBinding = new KeyBinding { Key = Key.SelectMedia, Modifiers = ModifierKeys.Windows | ModifierKeys.Shift | ModifierKeys.Control | ModifierKeys.Alt };
		}

		public bool IsAssigned {
			get { return DisplayString != "Not assigned"; }
		}

		public Shortcut(Shortcut shortcut, string cmdName) {
			CommandName = cmdName;
			DisplayString = shortcut.DisplayString;
			KeyBinding = new KeyBinding { Key = shortcut.KeyBinding.Key, Modifiers = shortcut.KeyBinding.Modifiers };
		}

		public Shortcut(Key key, ModifierKeys modifiers, string displayString, string cmdName) {
			CommandName = cmdName;
			DisplayString = displayString;
			KeyBinding = new KeyBinding { Key = key, Modifiers = modifiers };
		}

		public override InputGesture Gesture {
			get {
				return KeyBinding.Gesture;
			}
			set {
				base.Gesture = value;
			}
		}

		public Key Key {
			get { return KeyBinding.Key; }
		}

		public ModifierKeys Modifiers {
			get { return KeyBinding.Modifiers; }
		}

		public bool IsMatch(KeyEventArgs args) {
			if (KeyBinding != null) {
				var key = ApplicationShortcut.RealKey(args);
				return Keyboard.Modifiers == KeyBinding.Modifiers && (Keyboard.IsKeyDown(KeyBinding.Key) || KeyBinding.Key == key);
			}

			return false;
		}
	}

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

		public delegate bool PreKeyDownEventFunc(UIElement source, KeyEventArgs args, Shortcut shortcut);

		public static PreKeyDownEventFunc PreKeyDownEvent = null;

		private static void KeyDownEvent(UIElement source, KeyEventArgs args) {
			try {
				var bindings = UIBindings[source];

				bool modifiersOnly = Keyboard.FocusedElement is TextBoxBase;

				foreach (var binding in bindings) {
					if (modifiersOnly && binding.Modifiers == ModifierKeys.None)
						continue;
					if (modifiersOnly && binding.Modifiers == ModifierKeys.Shift)
						continue;

					if (binding.IsMatch(args)) {
						if (PreKeyDownEvent != null && PreKeyDownEvent(source, args, binding)) {
							continue;
						}

						binding.Command.Execute(source);
						args.Handled = true;
						// Don't allow more than 1 shortcut execution at once
						return;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public static Dictionary<UIElement, List<Shortcut>> UIBindings = new Dictionary<UIElement, List<Shortcut>>();

		public static void Link(Shortcut gesture, Action command, string group, UIElement holder) {
			var advGesture = gesture;
			var cmdName = gesture.DisplayString;

			if (advGesture != null && advGesture.CommandName != null) {
				cmdName = advGesture.CommandName;
			}

			gesture.Command = new CustomCommand(command);
			SetRawGesture(holder, gesture);

			AdvKeyBinding binding = new AdvKeyBinding { Holder = holder, KeyGesture = gesture, Action = command };
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
			gesture.Command = new CustomCommand(command);
			SetRawGesture(holder, gesture);

			AdvKeyBinding binding = new AdvKeyBinding { Holder = holder, KeyGesture = gesture, Action = command };

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

		public static string FindDislayNameMenuItem(Shortcut gesture) {
			if (gesture == null)
				return "NA";

			if (_override.ContainsKey(gesture.CommandName))
				return _override[gesture.CommandName];

			if (_keyBindings2.ContainsKey(gesture.CommandName)) {
				var bind = _keyBindings2[gesture.CommandName].KeyGesture;
				return bind.IsAssigned ? bind.DisplayString : "NA";
			}

			return gesture.IsAssigned ? gesture.DisplayString : "NA";
		}

		public static bool Is(Shortcut shortcut) {
			if (shortcut.KeyBinding != null) {
				return (Keyboard.Modifiers & shortcut.KeyBinding.Modifiers) == shortcut.KeyBinding.Modifiers && Keyboard.IsKeyDown(shortcut.KeyBinding.Key);
			}

			//if (shortcut.KeyBinding != null) {
			//	return Keyboard.IsKeyDown(shortcut.KeyBinding.Key);
			//}

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
			if (keyGesture == "NULL" || keyGesture == "NA") {
				return new Shortcut(cmdName);
			}

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

						key = (Key)keyObj;
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			}

			return new Shortcut(key, keys, keyGesture, cmdName);
		}

		public static void RemoveRawGesture(UIElement holder, Shortcut shortcut) {
			if (!UIBindings.ContainsKey(holder)) {
				UIBindings[holder] = new List<Shortcut>();
				holder.KeyDown += (s, e) => KeyDownEvent(holder, e);
			}

			UIBindings[holder].Remove(shortcut);
		}

		public static void SetRawGesture(UIElement holder, Shortcut shortcut) {
			if (!UIBindings.ContainsKey(holder)) {
				UIBindings[holder] = new List<Shortcut>();
				holder.KeyDown += (s, e) => KeyDownEvent(holder, e);
			}

			UIBindings[holder].Add(shortcut);
		}

		public static Key RealKey(KeyEventArgs e) {
			switch (e.Key) {
				case Key.System:
					return e.SystemKey;
				case Key.ImeProcessed:
					return e.ImeProcessedKey;
				//case Key.DeadCharProcessed:
				//	return e.DeadCharProcessedKey;
				default:
					return e.Key;
			}
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
