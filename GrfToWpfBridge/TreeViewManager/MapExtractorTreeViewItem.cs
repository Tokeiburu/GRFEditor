using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using TokeiLibrary.WPF;
using Utilities;

namespace GrfToWpfBridge.TreeViewManager {
	public class SharedTreeViewEvent {
		public bool EventsEnabled = true;
	}

	public class MapExtractorTreeViewItem : TkTreeViewItem {
		private TkPath _resourcePath;
		private SharedTreeViewEvent _holder;
		public string RelativeGrfPath { get; set; }

		public MapExtractorTreeViewItem(TkView parent, SharedTreeViewEvent holder) : base(parent, false) {
			CanBeDragged = true;
			UseCheckBox = true;

			Style = (Style)FindResource("MapExtractorTreeViewItemStyle");

			_holder = holder;
			this.Checked += _mapExtractorTreeViewItem_IsCheckedChanged;
			this.Unchecked += _mapExtractorTreeViewItem_IsCheckedChanged;
		}

		private void _mapExtractorTreeViewItem_IsCheckedChanged(object sender, RoutedEventArgs e) {
			if (!_holder.EventsEnabled) return;
			
			MapExtractorTreeViewItem node = sender as MapExtractorTreeViewItem;

			if (node == null)
				return;

			bool isChecked = node.IsChecked == true;

			try {
				_holder.EventsEnabled = false;
				_setAllChildren(node, isChecked);
				_checkParent(node.Parent as TkTreeViewItem);
			}
			finally {
				_holder.EventsEnabled = true;
			}
		}

		private void _checkParent(TkTreeViewItem parent) {
			if (parent == null)
				return;

			bool anyChecked = true;
			bool allChecked = true;

			foreach (TkTreeViewItem child in parent.Items) {
				if (!child.CheckBoxHeaderIsEnabled)
					continue;

				if (child.IsChecked != false)
					anyChecked = true;
				else
					allChecked = false;
			}

			if (allChecked)
				parent.IsChecked = true;
			else if (anyChecked)
				parent.IsChecked = null;
			else
				parent.IsChecked = false;

			_checkParent(parent.Parent as TkTreeViewItem);
		}

		private void _setAllChildren(TkTreeViewItem node, bool isChecked) {
			foreach (TkTreeViewItem child in node.Items) {
				child.IsChecked = isChecked;

				_setAllChildren(child, isChecked);
			}
		}

		public TkPath ResourcePath {
			get {
				return _resourcePath;
			}
			set {
				_resourcePath = value;

				if (_resourcePath != null) {
					if (_resourcePath.RelativePath == null)
						ToolTip = "File: " + _resourcePath.FilePath;
					else
						ToolTip = "GRF: " + _resourcePath.FilePath + (String.IsNullOrEmpty(_resourcePath.RelativePath) ? "" : "\r\n" + _resourcePath.RelativePath);
				}
				else {
					ToolTip = "Resource not found.";
				}
			}
		}
	}
}
