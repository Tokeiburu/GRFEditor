using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Commands;

namespace TokeiLibrary.WpfBugFix {
	/// <summary>
	/// Interaction logic for ToggleMemoryButton.xaml
	/// </summary>
	public partial class ToggleMemoryButton : UserControl {
		public static DependencyProperty DisplayFormatProperty = DependencyProperty.Register("DisplayFormat", typeof(string), typeof(ToggleMemoryButton), new PropertyMetadata("{0} action"));
		public static DependencyProperty PrimaryButtonImagePathProperty = DependencyProperty.Register("PrimaryButtonImagePath", typeof(string), typeof(ToggleMemoryButton), new PropertyMetadata("empty.png", new PropertyChangedCallback(OnPrimaryButtonImagePathChanged)));
		public static DependencyProperty CurrentIndexProperty = DependencyProperty.Register("CurrentIndex", typeof(int), typeof(ToggleMemoryButton), new PropertyMetadata(-1, new PropertyChangedCallback(OnCurrentIndexChanged)));

		public delegate void ToggleMemoryButtonEventHandler(object sender, int index);

		public event ToggleMemoryButtonEventHandler Click;

		public void OnClick(int index) {
			ToggleMemoryButtonEventHandler handler = Click;
			if (handler != null) handler(this, index);
		}

		private readonly RangeListView _listView = new RangeListView();

		public ToggleMemoryButton() {
			InitializeComponent();
			IsEnabledChanged += delegate {
				_button.Dispatch(p => p.IsButtonEnabled = IsEnabled);
				_buttonOpenSubMenuDrop.Dispatch(p => p.IsButtonEnabled = IsEnabled);
			};

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_listView, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
			    new ListViewDataTemplateHelper.GeneralColumnInfo {Header = "Commands", DisplayExpression = "CommandDescription", FixedWidth = 230, TextAlignment = TextAlignment.Left, ToolTipBinding = "CommandDescription" }
			}, null, new string[] { }, "generateHeader", "false");

			_gridSearchContent.Children.Add(_listView);
			_listView.MaxHeight = 225;
			_listView.FocusVisualStyle = null;
			_listView.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
			_listView.BorderThickness = new Thickness(0);
			_listView.Padding = new Thickness(0);
			_listView.SetValue(ScrollViewer.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
			_listView.Background = Brushes.Transparent;

			_listView.PreviewMouseMove += new MouseEventHandler(_undoListView_PreviewMouseMove);
			_cbSubMenu.DropDownOpened += new EventHandler(_cbSubMenu_DropDownOpened);
			_listView.SelectionChanged += new SelectionChangedEventHandler(_listView_SelectionChanged);
			_label.Content = String.Format(DisplayFormat, CurrentIndex + 1);

			_button.MouseEnter += delegate {
				_buttonOpenSubMenuDrop.ShowMouseOver = true;
			};
			_buttonOpenSubMenuDrop.MouseEnter += delegate {
				_button.ShowMouseOver = true;
			};
			_button.MouseLeave += delegate {
				_buttonOpenSubMenuDrop.ShowMouseOver = false;
			};
			_buttonOpenSubMenuDrop.MouseLeave += delegate {
				_button.ShowMouseOver = false;
			};

			_button.IsButtonEnabled = IsEnabled;
		}

		public string DisplayFormat {
			get { return (string)GetValue(DisplayFormatProperty); }
			set { SetValue(DisplayFormatProperty, value); }
		}

		public string PrimaryButtonImagePath {
			get { return (string)GetValue(PrimaryButtonImagePathProperty); }
			set { SetValue(PrimaryButtonImagePathProperty, value); }
		}

		public int CurrentIndex {
			get { return (int)GetValue(CurrentIndexProperty); }
			set { SetValue(CurrentIndexProperty, value); }
		}

		public static void OnCurrentIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			ToggleMemoryButton button = d as ToggleMemoryButton;

			if (button != null) {
				button._label.Content = String.Format(button.DisplayFormat, button.CurrentIndex + 1) + (button.CurrentIndex < 1 ? "" : "s");
				button._selectAll((int)e.NewValue);
			}
		}

		public static void OnPrimaryButtonImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			ToggleMemoryButton button = d as ToggleMemoryButton;

			if (button != null) {
				button._button.ImagePath = (string)e.NewValue;
			}
		}

		private void _listView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (CurrentIndex >= 0)
				OnClick(CurrentIndex);
		}

		private void _selectAll(int index) {
			_listView.SelectionChanged -= new SelectionChangedEventHandler(_listView_SelectionChanged);

			int upTo = index;
			ListViewItem item;

			for (int i = 0; i < _listView.Items.Count; i++) {
				item = _listView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

				if (item != null) {
					item.IsSelected = i <= upTo;
				}
			}

			_listView.SelectionChanged += new SelectionChangedEventHandler(_listView_SelectionChanged);
		}

		private void _undoListView_PreviewMouseMove(object sender, MouseEventArgs e) {
			var item = _listView.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listView));

			if (item != null) {
				CurrentIndex = _listView.Items.IndexOf(((ListViewItem)item).Content);
				((ListViewItem)item).Focus();
			}
			else {
				CurrentIndex = -1;
			}
		}

		private object _command;
		private delegate void ClearEventHandler(object sender);
		private event ClearEventHandler _clear;

		public void SetUndo<T>(AbstractCommand<T> bCommand) {
			if (_clear != null) {
				_clear(_command);
				_clear = null;
			}

			this.IsEnabled = bCommand.CanUndo;
			_button.IsButtonEnabled = bCommand.CanUndo;
			_buttonOpenSubMenuDrop.IsButtonEnabled = bCommand.CanUndo;

			_command = bCommand;

			_button.Click += _button_Click2<T>;
			_cbSubMenu.DropDownOpened += _cbSubMenu_DropDownOpened2<T>;
			_listView.PreviewMouseDown += _listView_PreviewMouseDown<T>;
			bCommand.CommandIndexChanged += _bCommand_CommandIndexChanged;

			_clear += s => _toggleMemoryButton_Clear<T>(bCommand);
		}

		private void _toggleMemoryButton_Clear<T>(object sender) {
			_clearHandlers((AbstractCommand<T>)sender);
		}

		private void _listView_PreviewMouseDown<T>(object sender, MouseButtonEventArgs e) {
			var item = _listView.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listView));

			if (item != null) {
				int cur = CurrentIndex;

				while (cur >= 0) {
					((AbstractCommand<T>)_command).Undo();
					cur--;
				}
			}
		}
		private void _cbSubMenu_DropDownOpened2<T>(object sender, EventArgs e) {
			ListViewItem item;
			for (int i = 0; i < _listView.Items.Count; i++) {
				item = _listView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

				if (item != null) {
					item.IsSelected = false;
				}
			}

			List<T> commands = ((AbstractCommand<T>)_command).GetUndoCommands();

			if (commands != null) {
				commands.Reverse();
				_listView.ItemsSource = new ObservableCollection<T>(commands);
			}
			else {
				_listView.ItemsSource = null;
			}
		}
		private void _button_Click2<T>(object sender, RoutedEventArgs e) {
			((AbstractCommand<T>)_command).Undo();
		}
		private void _bCommand_CommandIndexChanged<T>(object sender, T command) {
			if (sender != _command) {
				return;
			}

			this.Dispatch(p => p.IsEnabled = ((AbstractCommand<T>)_command).CanUndo);
			_button.Dispatch(p => p.IsButtonEnabled = ((AbstractCommand<T>)_command).CanUndo);
			_buttonOpenSubMenuDrop.Dispatch(p => p.IsButtonEnabled = ((AbstractCommand<T>)_command).CanUndo);
		}

		private void _listView_PreviewMouseDown2<T>(object sender, MouseButtonEventArgs e) {
			var item = _listView.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listView));

			if (item != null) {
				int cur = CurrentIndex;

				while (cur >= 0) {
					((AbstractCommand<T>)_command).Redo();
					cur--;
				}
			}
		}
		private void _cbSubMenu_DropDownOpened3<T>(object sender, EventArgs e) {
			ListViewItem item;
			for (int i = 0; i < _listView.Items.Count; i++) {
				item = _listView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

				if (item != null) {
					item.IsSelected = false;
				}
			}

			List<T> commands = ((AbstractCommand<T>)_command).GetRedoCommands();

			if (commands != null) {
				_listView.ItemsSource = new ObservableCollection<T>(commands);
			}
			else {
				_listView.ItemsSource = null;
			}
		}
		private void _button_Click3<T>(object sender, RoutedEventArgs e) {
			((AbstractCommand<T>)_command).Redo();
		}
		private void _bCommand_CommandIndexChanged2<T>(object sender, T command) {
			if (sender != _command) {
				return;
			}

			this.Dispatch(p => p.IsEnabled = ((AbstractCommand<T>)_command).CanRedo);
			_button.Dispatch(p => p.IsButtonEnabled = ((AbstractCommand<T>)_command).CanRedo);
			_buttonOpenSubMenuDrop.Dispatch(p => p.IsButtonEnabled = ((AbstractCommand<T>)_command).CanRedo);
		}

		private void _clearHandlers<T>(AbstractCommand<T> bcommand) {
			bcommand.CommandExecuted -= _bCommand_CommandIndexChanged;
			bcommand.CommandExecuted -= _bCommand_CommandIndexChanged2;
			_button.Click -= _button_Click2<T>;
			_button.Click -= _button_Click3<T>;
			_cbSubMenu.DropDownOpened -= _cbSubMenu_DropDownOpened2<T>;
			_cbSubMenu.DropDownOpened -= _cbSubMenu_DropDownOpened3<T>;
			_listView.PreviewMouseDown -= _listView_PreviewMouseDown<T>;
			_listView.PreviewMouseDown -= _listView_PreviewMouseDown2<T>;
		}

		public void SetRedo<T>(AbstractCommand<T> bCommand) {
			if (_clear != null) {
				_clear(_command);
				_clear = null;
			}

			this.IsEnabled = bCommand.CanRedo;
			_button.IsButtonEnabled = bCommand.CanRedo;
			_buttonOpenSubMenuDrop.IsButtonEnabled = bCommand.CanRedo;

			_command = bCommand;

			_button.Click += _button_Click3<T>;
			_cbSubMenu.DropDownOpened += _cbSubMenu_DropDownOpened3<T>;
			_listView.PreviewMouseDown += _listView_PreviewMouseDown2<T>;
			bCommand.CommandIndexChanged += _bCommand_CommandIndexChanged2;

			_clear += s => _toggleMemoryButton_Clear<T>(bCommand);
		}

		public void LinkUndo<T>(AbstractCommand<T> bCommand) {
			SetUndo(bCommand);
		}

		private bool _eventsUndoSet;
		private Action _undo;
		private object _acUndo;

		public void LinkUndo<T>(AbstractCommand<T> aCommand, Action undo) {
			_acUndo = aCommand;
			_undo = undo;
			_button.IsButtonEnabled = aCommand.CanUndo;
			_buttonOpenSubMenuDrop.IsButtonEnabled = aCommand.CanUndo;
			this.IsEnabled = aCommand.CanUndo;

			aCommand.CommandIndexChanged += delegate {
				this.Dispatch(p => p.IsEnabled = aCommand.CanUndo);
				_button.Dispatch(p => p.IsButtonEnabled = aCommand.CanUndo);
				_buttonOpenSubMenuDrop.Dispatch(p => p.IsButtonEnabled = aCommand.CanUndo);
			};

			if (!_eventsUndoSet) {
				_button.Click += delegate {
					_undo();
				};

				_cbSubMenu.DropDownOpened += delegate {
					ListViewItem item;
					for (int i = 0; i < _listView.Items.Count; i++) {
						item = _listView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

						if (item != null) {
							item.IsSelected = false;
						}
					}

					List<T> commands = ((AbstractCommand<T>) _acUndo).GetUndoCommands();

					if (commands != null) {
						commands.Reverse();
						_listView.ItemsSource = new ObservableCollection<T>(commands);
					}
					else {
						_listView.ItemsSource = null;
					}
				};

				_listView.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e) {
					var item = _listView.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listView));

					if (item != null) {
						int cur = CurrentIndex;

						while (cur >= 0) {
							_undo();
							cur--;
						}
					}
				};

				_eventsUndoSet = true;
			}
		}

		public void LinkRedo<T>(AbstractCommand<T> aCommand) {
			SetRedo(aCommand);
		}

		private bool _eventsRedoSet;
		private Action _redo;
		private object _acRedo;

		public void LinkRedo<T>(AbstractCommand<T> aCommand, Action redo) {
			_acRedo = aCommand;
			_redo = redo;
			this.IsEnabled = aCommand.CanRedo;
			_button.IsButtonEnabled = aCommand.CanRedo;
			_buttonOpenSubMenuDrop.IsButtonEnabled = aCommand.CanRedo;

			aCommand.CommandIndexChanged += delegate {
				this.Dispatch(p => p.IsEnabled = aCommand.CanRedo);
				_button.Dispatch(p => p.IsButtonEnabled = aCommand.CanRedo);
				_buttonOpenSubMenuDrop.Dispatch(p => p.IsButtonEnabled = aCommand.CanRedo);
			};

			if (!_eventsRedoSet) {
				_button.Click += delegate {
					_redo();
				};

				_cbSubMenu.DropDownOpened += delegate {
					ListViewItem item;
					for (int i = 0; i < _listView.Items.Count; i++) {
						item = _listView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

						if (item != null) {
							item.IsSelected = false;
						}
					}

					List<T> commands = ((AbstractCommand<T>) _acRedo).GetRedoCommands();

					if (commands != null) {
						_listView.ItemsSource = new ObservableCollection<T>(commands);
					}
					else {
						_listView.ItemsSource = null;
					}
				};

				_listView.PreviewMouseDown += delegate(object sender, MouseButtonEventArgs e) {
					var item = _listView.GetObjectAtPoint<ListViewItem>(e.GetPosition(_listView));

					if (item != null) {
						int cur = CurrentIndex;

						while (cur >= 0) {
							_redo();
							cur--;
						}
					}
				};

				_eventsRedoSet = true;
			}
		}

		private void _cbSubMenu_DropDownOpened(object sender, EventArgs e) {
			_cbSubMenu.SelectedIndex = 0;
		}

		private void _button_Click(object sender, RoutedEventArgs e) {
			if (_listView.Items.Count > 0)
				OnClick(0);
		}

		private void _buttonOpenSubMenuDrop_Click(object sender, RoutedEventArgs e) {
			_cbSubMenu.IsDropDownOpen = true;
		}
	}
}
