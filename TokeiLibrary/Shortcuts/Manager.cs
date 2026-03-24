using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ErrorManager;
using TokeiLibrary.WPF.Styles;
using Utilities.Extension;

namespace TokeiLibrary.Shortcuts {
	public class Shortcut {
		public Key Key { get; set; }
		public ModifierKeys Modifiers { get; set; }
		public string Name { get; set; }

		public Shortcut(Key key, ModifierKeys modifiers) {
			_init(key, modifiers);
		}

		public Shortcut(string shortcut) {
			List<string> gesture = shortcut.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries).ToList();

			ModifierKeys modifiers = 0;
			Key key = Key.A;

			for (int i = 0; i < gesture.Count; i++) {
				if (String.Compare(gesture[i], "shift", StringComparison.OrdinalIgnoreCase) == 0) {
					modifiers |= ModifierKeys.Shift;
				}
				else if (String.Compare(gesture[i], "ctrl", StringComparison.OrdinalIgnoreCase) == 0) {
					modifiers |= ModifierKeys.Control;
				}
				else if (String.Compare(gesture[i], "alt", StringComparison.OrdinalIgnoreCase) == 0) {
					modifiers |= ModifierKeys.Alt;
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

			_init(key, modifiers);
		}

		private void _init(Key key, ModifierKeys modifiers) {
			Key = key;
			Modifiers = modifiers;

			Name = "";

			if (modifiers.HasFlags(ModifierKeys.Control)) {
				Name += "Ctrl-";
			}
			if (modifiers.HasFlags(ModifierKeys.Shift)) {
				Name += "Shift-";
			}
			if (modifiers.HasFlags(ModifierKeys.Alt)) {
				Name += "Alt-";
			}
			if (modifiers.HasFlags(ModifierKeys.Windows)) {
				Name += "Win-";
			}

			Name += key;
		}

		public Shortcut Clone() {
			return new Shortcut(Key, Modifiers);
		}

		public bool IsMatch() {
			return (Keyboard.Modifiers & Modifiers) == Modifiers && Keyboard.IsKeyDown(Key);
		}

		public override int GetHashCode() {
			return Name.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Name.Equals(((Shortcut)obj).Name);
		}

		public override string ToString() {
			return Name;
		}
	}

	public class TkCommand {
		public string CommandName { get; set; }
		public bool IsAssigned => Shortcut != null;
		public string ShortcutName {
			get {
				if (!IsAssigned)
					return "Not assigned";

				return Shortcut.Name;
			}
		}
		public string InputGestureText {
			get {
				if (!IsAssigned)
					return "";
					//return "NA";

				return Shortcut.Name;
			}
		}
		public Shortcut Shortcut { get; set; }
		public Shortcut OriginalShortcut { get; set; }
		public bool CanReset => Shortcut != OriginalShortcut;

		public TkCommand(string commandName, Shortcut gesture) {
			CommandName = commandName;
			Shortcut = gesture;
			OriginalShortcut = gesture;
		}

		public bool IsMatch() {
			if (Shortcut == null)
				return false;
			return Shortcut.IsMatch();
		}

		public void Reset() {
			Shortcut = OriginalShortcut;
		}

		public override int GetHashCode() {
			return CommandName.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return CommandName.Equals(((TkCommand)obj).CommandName);
		}

		public override string ToString() {
			var shortcut = OriginalShortcut ?? Shortcut;

			return "{" + CommandName + (shortcut != null ? "|" + shortcut.ToString() : "") + "}";
		}

		public static implicit operator string(TkCommand command) {
			return command.ToString();
		}
	}

	public class TkBinding {
		public TkCommand Command { get; set; }
		public Shortcut Gesture => Command.Shortcut;
		public UIElement Element { get; set; }
		public Action Action;

		public TkBinding(TkCommand command, UIElement element, Action action) {
			Command = command;
			Element = element;
			Action = action;
		}

		public bool IsMatch(KeyEventArgs args) {
			if (Gesture != null) {
				var key = ApplicationShortcut.RealKey(args);
				return Keyboard.Modifiers == Gesture.Modifiers && (Keyboard.IsKeyDown(Gesture.Key) || Gesture.Key == key);
			}
			
			return false;
		}

		public override int GetHashCode() {
			return Command.CommandName.GetHashCode();
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Command.CommandName.Equals(((TkBinding)obj).Command.CommandName);
		}

		public override string ToString() {
			return Command.CommandName + "|" + Command.ShortcutName;
		}
	}

	public class TKEventArgs {
		public bool Handled { get; set; }
	}

	public class FrameworkElementBinder {
		public FrameworkElement Element { get; set; }
		public Dictionary<TkCommand, TkBinding> Bindings = new Dictionary<TkCommand, TkBinding>();

		public FrameworkElementBinder(FrameworkElement element) {
			Element = element;
			WeakEventManager<UIElement, KeyEventArgs>.AddHandler(element, "KeyDown", KeyDownEvent);
		}

		public void KeyDownEvent(object source, KeyEventArgs args) {
			bool modifiersOnly = Keyboard.FocusedElement is TextBoxBase;

			foreach (var binding in Bindings.Values) {
				if (!binding.Command.IsAssigned)
					continue;
				if (modifiersOnly && binding.Command.Shortcut.Modifiers == ModifierKeys.None)
					continue;
				if (modifiersOnly && binding.Command.Shortcut.Modifiers == ModifierKeys.Shift)
					continue;

				if (binding.IsMatch(args)) {
					TKEventArgs preArgs = new TKEventArgs();
					PreviewKeyDown?.Invoke(source, preArgs, binding);

					if (preArgs.Handled)
						continue;

					binding.Action();
					args.Handled = true;
					return;
				}
			}
		}

		public delegate void PreKeyDownEventHandler(object source, TKEventArgs args, TkBinding binding);
		public event PreKeyDownEventHandler PreviewKeyDown;
	}

	public static class ApplicationShortcut {
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

		public static ConditionalWeakTable<FrameworkElement, FrameworkElementBinder> ElementBinders = new ConditionalWeakTable<FrameworkElement, FrameworkElementBinder>();
		public static Dictionary<string, TkCommand> Commands = new Dictionary<string, TkCommand>();

		private static IDictionary<string, string> _override;

		public static void Unlink(TkCommand command, FrameworkElement element) {
			if (!ElementBinders.TryGetValue(element, out FrameworkElementBinder elementBinder)) {
				return;
			}

			if (elementBinder.Bindings.ContainsKey(command)) {
				elementBinder.Bindings.Remove(command);
			}
		}

		public static void Link(TkCommand command, Action executeAction, FrameworkElement element) {
			if (Commands.ContainsKey(command.CommandName)) {
				command = Commands[command.CommandName];
			}
			else {
				Commands[command.CommandName] = command;
			}

			if (!ElementBinders.TryGetValue(element, out FrameworkElementBinder elementBinder)) {
				elementBinder = new FrameworkElementBinder(element);
				ElementBinders.Add(element, elementBinder);
			}

			AssignOverrideShortcut(command);

			TkBinding binding = new TkBinding(command, element, executeAction);
			elementBinder.Bindings[command] = binding;
		}

		public static void Link(TkCommand command, MenuItem menuItem, FrameworkElement element) {
			Action executeAction = delegate {
				RoutedEventArgs arg = new RoutedEventArgs(MenuItem.ClickEvent, menuItem);
				menuItem.RaiseEvent(arg);
			};

			Link(command, executeAction, element);
		}

		public static void Link(TkCommand command, TkMenuItem menuItem, FrameworkElement element) {
			Action executeAction = delegate {
				RoutedEventArgs arg = new RoutedEventArgs(MenuItem.ClickEvent, menuItem);
				menuItem.RaiseEvent(arg);
			};

			menuItem.ShortcutCmd = command.CommandName;
			Link(command, executeAction, element);
		}

		public static void OverrideBindings(IDictionary<string, string> shortcuts) {
			// Shortcuts may not have been loaded yet, so save the data for later
			_override = shortcuts;
		
			foreach (var command in Commands.Values) {
				AssignOverrideShortcut(command);
			}
		}

		public static void AssignOverrideShortcut(TkCommand command, Shortcut shortcut = null) {
			if (_override == null)
				return;

			if (shortcut != null)
				command.Shortcut = shortcut;

			if (_override.TryGetValue(command.CommandName, out string newShortcut))
				command.Shortcut = new Shortcut(_override[command.CommandName]);
		}

		public static void ResetBindings() {
			foreach (var command in Commands.Values) {
				command.Reset();
			}
		}

		public static bool IsCommandActive() {
			return Commands.Values.Any(shortcut => shortcut.Shortcut == null ? false : shortcut.Shortcut.IsMatch());
		}

		public static TkCommand FromString(string shortcut, string commandName) {
			Shortcut inputGesture = null;

			if (shortcut != null && shortcut != "NULL" && shortcut != "NA") {
				inputGesture = new Shortcut(shortcut);
			}

			var command = new TkCommand(commandName, inputGesture);

			if (Commands.ContainsKey(command.CommandName))
				return Commands[command.CommandName];

			return command;
		}

		public static TkCommand GetGesture(string commandName) {
			Commands.TryGetValue(commandName, out var command);
			return command;
		}

		public static readonly TkCommand Undo = new TkCommand("Application.Undo", new Shortcut(Key.Z, ModifierKeys.Control));
		public static readonly TkCommand NavigationForward = new TkCommand("Application.NavigationForward", new Shortcut(Key.Y, ModifierKeys.Alt));
		public static readonly TkCommand NavigationBackward = new TkCommand("Application.NavigationBackward", new Shortcut(Key.Z, ModifierKeys.Alt));
		public static readonly TkCommand Redo = new TkCommand("Application.Redo", new Shortcut(Key.Y, ModifierKeys.Control));
		public static readonly TkCommand Search = new TkCommand("Application.Search", new Shortcut(Key.F, ModifierKeys.Control));
		public static readonly TkCommand Replace = new TkCommand("Application.Replace", new Shortcut(Key.H, ModifierKeys.Control));
		public static readonly TkCommand Delete = new TkCommand("Application.Delete", new Shortcut(Key.Delete, ModifierKeys.None));
		public static readonly TkCommand Confirm = new TkCommand("Application.Confirm", new Shortcut(Key.Enter, ModifierKeys.None));
		public static readonly TkCommand Copy = new TkCommand("Application.Copy", new Shortcut(Key.C, ModifierKeys.Control));
		public static readonly TkCommand Paste = new TkCommand("Application.Paste", new Shortcut(Key.V, ModifierKeys.Control));
		public static readonly TkCommand Open = new TkCommand("Application.Open", new Shortcut(Key.O, ModifierKeys.Control));
		public static readonly TkCommand Cut = new TkCommand("Application.Cut", new Shortcut(Key.X, ModifierKeys.Control));
		public static readonly TkCommand Rename = new TkCommand("Application.Rename", new Shortcut(Key.F2, ModifierKeys.None));
		public static readonly TkCommand Change = new TkCommand("Application.Change", new Shortcut(Key.D, ModifierKeys.Control));
		public static readonly TkCommand Restrict = new TkCommand("Application.Restrict", new Shortcut(Key.R, ModifierKeys.Control));
		public static readonly TkCommand New = new TkCommand("Application.New", new Shortcut(Key.N, ModifierKeys.Control));
		public static readonly TkCommand Save = new TkCommand("Application.Save", new Shortcut(Key.S, ModifierKeys.Control));
		public static readonly TkCommand SaveAll = new TkCommand("Application.SaveAll", new Shortcut(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
		public static readonly TkCommand SaveSpecial = new TkCommand("Application.SaveSpecial", new Shortcut(Key.S, ModifierKeys.Control | ModifierKeys.Alt));
		public static readonly TkCommand Select = new TkCommand("Application.Select", new Shortcut(Key.E, ModifierKeys.Control));
	}
}
