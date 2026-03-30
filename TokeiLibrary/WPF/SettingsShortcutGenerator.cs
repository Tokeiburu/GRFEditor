using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WPF {
	public static class SettingsShortcutGenerator {
		public class ShortcutVisual {
			public Grid Grid;
			public Label Label;
		}

		public static Dictionary<string, ShortcutVisual> CreateGrid(ObservableDictionary<string, string> remapper, Grid _gridShortcuts) {
			int row = 0;
			Dictionary<string, ShortcutVisual> _shortcuts = new Dictionary<string, ShortcutVisual>();

			foreach (var command in ApplicationShortcut.Commands.Values) {
				string actionName = command.CommandName;

				if (actionName == "#auto_generated")
					continue;

				Label l = new Label { Content = actionName };

				l.Content = actionName;

				WpfUtilities.SetGridPosition(l, row, 0);
				_gridShortcuts.Children.Add(l);

				Border b = new Border();
				b.Margin = new Thickness(3);
				b.BorderThickness = new Thickness(1);
				b.BorderBrush = WpfUtilities.LostFocusBrush;

				Grid grid = new Grid();
				_shortcuts[actionName] = new ShortcutVisual { Grid = grid, Label = l };
				grid.ColumnDefinitions.Add(new ColumnDefinition());
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(-1, GridUnitType.Auto) });

				TextBox tb = new TextBox { Text = command.ShortcutName };
				tb.BorderThickness = new Thickness(0);
				tb.Padding = new Thickness(0);
				tb.IsReadOnly = true;
				b.Child = tb;

				grid.Children.Add(b);

				FancyButton button = new FancyButton();
				button.ImagePath = "reset.png";
				button.Width = 20;
				button.Height = 20;
				button.Visibility = Visibility.Collapsed;
				button.Margin = new Thickness(0, 0, 3, 0);
				button.Click += delegate {
					button.Visibility = Visibility.Collapsed;
					command.Reset();
					tb.Text = command.ShortcutName;
					remapper.Remove(actionName);
					b.BorderBrush = WpfUtilities.LostFocusBrush;
				};

				if (command.CanReset) {
					button.Visibility = Visibility.Visible;
				}

				WpfUtilities.SetGridPosition(button, 0, 1);
				grid.Children.Add(button);

				WpfUtilities.SetGridPosition(grid, row, 1);
				_gridShortcuts.Children.Add(grid);
				_gridShortcuts.RowDefinitions.Add(new RowDefinition { Height = new GridLength(-1, GridUnitType.Auto) });

				tb.GotFocus += delegate {
					b.BorderThickness = new Thickness(2);

					if (b.BorderBrush == Brushes.Red) {
						return;
					}

					b.BorderBrush = WpfUtilities.GotFocusBrush;
				};

				tb.LostFocus += delegate {
					b.BorderThickness = new Thickness(1);

					if (b.BorderBrush == Brushes.Red) {
						return;
					}

					b.BorderBrush = WpfUtilities.LostFocusBrush;
				};

				tb.PreviewKeyDown += delegate (object sender, KeyEventArgs e) {
					if (e.Key == Key.Escape || e.Key == Key.Tab) {
						return;
					}

					bool valid;
					tb.Text = _make(e.Key, Keyboard.Modifiers, out valid);

					try {
						if (!valid)
							throw new Exception();

						var shortcut = new Shortcut(e.Key, Keyboard.Modifiers);
						command.Shortcut = shortcut;

						if (command.CanReset) {
							button.Visibility = Visibility.Visible;
						}
						else {
							button.Visibility = Visibility.Collapsed;
						}

						remapper[actionName] = tb.Text;
						//ApplicationShortcut.OverrideBindings(remapper);

						b.BorderThickness = new Thickness(2);
						b.BorderBrush = WpfUtilities.GotFocusBrush;
					}
					catch {
						b.BorderThickness = new Thickness(2);
						b.BorderBrush = Brushes.Red;
						button.Visibility = Visibility.Visible;
					}
					e.Handled = true;
				};

				row++;
			}

			return _shortcuts;
		}

		private static string _make(Key key, ModifierKeys modifiers, out bool valid) {
			string display = "";

			if (modifiers.HasFlag(ModifierKeys.Control)) {
				display += "Ctrl-";
			}
			if (modifiers.HasFlag(ModifierKeys.Shift)) {
				display += "Shift-";
			}
			if (modifiers.HasFlag(ModifierKeys.Alt)) {
				display += "Alt-";
			}
			if (modifiers.HasFlag(ModifierKeys.Windows)) {
				display += "Win-";
			}

			if (key == Key.LeftAlt ||
				key == Key.RightAlt ||
				key == Key.LeftCtrl ||
				key == Key.RightCtrl ||
				key == Key.LeftShift ||
				key == Key.RightShift ||
				key == Key.System ||
				key == Key.LWin ||
				key == Key.RWin) {
				valid = false;
				return display;
			}

			valid = true;
			display += key;
			return display;
		}
	}
}
