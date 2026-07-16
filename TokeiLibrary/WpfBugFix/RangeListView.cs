using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokeiLibrary.WPF.Styles;

namespace TokeiLibrary.WpfBugFix {
	public class RangeListView : ListView {
		private bool _supressEvents;
		private bool _hasItems = true;

		public RangeListView() : base() {
			PreviewMouseDown += _rangeListView_PreviewMouseDown;
		}

		private void _rangeListView_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			Focus();
		}

		protected override void OnPreviewMouseRightButtonUp(MouseButtonEventArgs e) {
			_hasItems = this.GetObjectAtPoint<ListViewItem>(e.GetPosition(this)) != null;
		}

		protected override void OnContextMenuOpening(ContextMenuEventArgs e) {
			base.OnContextMenuOpening(e);

			if (ContextMenu == null)
				return;

			foreach (var menuItem in ContextMenu.Items.OfType<TkMenuItem>()) {
				if (menuItem.RequiresItem) {
					menuItem.IsEnabled = _hasItems;
				}
			}

			_hasItems = true;
		}

		public void Disable() {
			_supressEvents = true;
		}

		public void Enable() {
			_supressEvents = false;
		}

		public void UpdateAndEnable() {
			_supressEvents = false;
			SelectionChangedEventArgs newEventArgs = new SelectionChangedEventArgs(SelectionChangedEvent, new List<object>(), new List<object> { this.SelectedItem });
			RaiseEvent(newEventArgs);
		}

		public void OnResize(SizeChangedInfo size) {
			OnRenderSizeChanged(size);
		}

		protected override void OnRenderSizeChanged(System.Windows.SizeChangedInfo sizeInfo) {
			base.OnRenderSizeChanged(sizeInfo);
		}

		public void SelectItems(IEnumerable items) {
			SetSelectedItems(items);
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
			if (!_supressEvents)
				base.OnSelectionChanged(e);
		}
	}
}
