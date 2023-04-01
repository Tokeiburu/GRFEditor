using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace TokeiLibrary.WpfBugFix {
	public class RangeListView : ListView {
		private bool _supressEvents;

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
