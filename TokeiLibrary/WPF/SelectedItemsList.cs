using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ErrorManager;

namespace TokeiLibrary.WPF {
	public class SelectedItemsList {
		private readonly List<object> _selectedItems = new List<object>();
		public bool MultiSelection { get; set; }
		public bool SelectNoEvent { get; set; }

		public bool AltPressed {
			get { return (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt; }
		}

		public bool ControlPressed {
			get { return (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control; }
		}

		public List<TkTreeViewItem> Items {
			get { return _selectedItems.OfType<TkTreeViewItem>().ToList(); }
		}

		public void Add(object item, TkView parent) {
			try {
				if (SelectNoEvent) {
					if (_selectedItems.Contains(item))
						_selectedItems.Remove(item);

					_selectedItems.Add(item);
					return;
				}

				if (MultiSelection) {
					if (_selectedItems.Contains(item)) {
						if (_selectedItems[0] != item && !AltPressed)
							_selectedItems.Remove(item);
					}

					if (!ControlPressed && (Keyboard.Modifiers & ModifierKeys.Shift) != ModifierKeys.Shift && !AltPressed) {
						Clear();
					}
					else if (AltPressed) {
						if (_selectedItems.Contains(item))
							return;

						Clear();
						_selectedItems.Insert(0, item);
						parent.OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<object>(null, item));
						return;
					}
					else if (ControlPressed) {
						_selectedItems.Insert(0, item);
						return;
					}
					else if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) {
						//if (_selectedItems.Count > 0) {
							_selectedItems.Skip(1).OfType<TkTreeViewItem>().ToList().ForEach(p => p.IsSelected = false);

							int position1 = parent.FindPosition(_selectedItems[0] as TkTreeViewItem);
							int position2 = parent.FindPosition(item as TkTreeViewItem);

							List<object> selected = position1 < position2 ? parent.GetItems(position1, position2) : parent.GetItems(position2, position1);

							selected.Remove(_selectedItems[0]);

							SelectNoEvent = true;
							selected.OfType<TkTreeViewItem>().ToList().ForEach(p => p.IsSelected = true);
							SelectNoEvent = false;
						//}

						return;
					}
				}
				else {
					_selectedItems.Remove(item);
					Clear();
				}

				_selectedItems.Insert(0, item);
				parent.OnSelectedItemChanged(new RoutedPropertyChangedEventArgs<object>(null, item));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public object Last() {
			if (_selectedItems.Count == 0)
				return null;

			return _selectedItems[0];
		}

		public void Clear() {
			_selectedItems.OfType<TkTreeViewItem>().ToList().ForEach(p => p.IsSelected = false);
			_selectedItems.Clear();
		}

		public void Set(object item) {
			Clear();

			if (item == null)
				return;

			TkTreeViewItem tvi = (TkTreeViewItem)item;
			tvi.IsSelected = true;
		}

		public void Remove(object item) {
			_selectedItems.Remove(item);
		}
	}
}