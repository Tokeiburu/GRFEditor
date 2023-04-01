using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TokeiLibrary.WpfBugFix {
	public class UnclickableBorder : Border {
		private ComboBox _source;

		public void Init(ComboBox source) {
			_source = source;
		}

		protected override void OnPreviewMouseDown(MouseButtonEventArgs e) {
			ComboBox[] children = WpfUtilities.FindChildren<ComboBox>(this);
			List<Grid> grids = new List<Grid>();

			foreach (Grid grid in WpfUtilities.FindChildren<Grid>(this)) {
				grids.AddRange(WpfUtilities.FindChildren<Grid>(grid));
			}

			if (grids.Count > 0) {
				List<ComboBox> boxes = new List<ComboBox>();

				foreach (Grid grid in grids) {
					boxes.AddRange(grid.Children.OfType<ComboBox>());
				}

				if (children == null)
					children = new ComboBox[] { };

				children = children.Concat(boxes).ToArray();
			}

			if (this.Child is Grid) {
				if (children == null)
					children = new ComboBox[] { };

				children = children.Concat(((Grid) this.Child).Children.OfType<ComboBox>()).ToArray();
			}

			if (children != null) {
				if (children.Any(box => box.IsDropDownOpen)) {
					base.OnPreviewMouseDown(e);
					return;
				}
			}

			//if (WpfUtilities.FindParentControl<ComboBoxItem>((Mouse.DirectlyOver as DependencyObject)) != null) {
			//    base.OnPreviewMouseDown(e);
			//    return;
			//}

			Point position = e.GetPosition(this);

			if (position.X < 0 || position.Y < 0 ||
				position.Y > this.ActualHeight ||
				position.X > this.ActualWidth) {
				_source.IsDropDownOpen = false;
				base.OnPreviewMouseDown(e);
				base.OnPreviewMouseDown(e);
				return;
			}

			base.OnPreviewMouseDown(e);
		}

		protected override void OnPreviewMouseUp(MouseButtonEventArgs e) {
			IInputElement element = InputHitTest(e.GetPosition(this));

			ComboBox[] children = WpfUtilities.FindChildren<ComboBox>(this);
			List<Grid> grids = new List<Grid>();

			foreach (Grid grid in WpfUtilities.FindChildren<Grid>(this)) {
				grids.AddRange(WpfUtilities.FindChildren<Grid>(grid));
			}

			if (grids.Count > 0) {
				List<ComboBox> boxes = new List<ComboBox>();

				foreach (Grid grid in grids) {
					boxes.AddRange(grid.Children.OfType<ComboBox>());
				}

				if (children == null)
					children = new ComboBox[] { };

				children = children.Concat(boxes).ToArray();
			}

			if (this.Child is Grid) {
				if (children == null)
					children = new ComboBox[] { };

				children = children.Concat(((Grid)this.Child).Children.OfType<ComboBox>()).ToArray();
			}

			if (children != null) {
				if (children.Any(box => box.IsDropDownOpen)) {
					base.OnPreviewMouseUp(e);
					return;
				}
			}

			//if (WpfUtilities.FindParentControl<ComboBoxItem>((Mouse.DirectlyOver as DependencyObject)) != null) {
			//    base.OnPreviewMouseUp(e);
			//    return;
			//}

			Point position = e.GetPosition(this);

			if (position.X < 0 || position.Y < 0 ||
				position.Y > this.ActualHeight ||
				position.X > this.ActualWidth) {
				base.OnPreviewMouseUp(e);
				return;
			}

			if (WpfUtilities.FindParentControl<ComboBox>(element as DependencyObject) != null ||
				WpfUtilities.FindParentControl<System.Windows.Controls.ListView>(element as DependencyObject) != null ||
				WpfUtilities.FindParentControl<CheckBox>(element as DependencyObject) != null) {
				base.OnPreviewMouseUp(e);
				return;
			}

			e.Handled = true;
		}
	}
}
