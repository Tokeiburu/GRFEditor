using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using TokeiLibrary.WPF;

namespace GrfToWpfBridge.TreeViewManager {
	public class TypeTreeViewItem : TkTreeViewItem {
		private TypeTreeViewItemClass _objectType;

		public TypeTreeViewItem(TkView parent)
			: base(parent) {
			CanBeDragged = false;
			CanBeDropped = false;

			HorizontalContentAlignment = HorizontalAlignment.Left;
			VerticalContentAlignment = VerticalAlignment.Center;
		}

		public bool? HasBeenLoaded { get; set; }

		public bool DontAutomaticallyExpand { get; set; }

		public TypeTreeViewItemClass ObjectType {
			get { return _objectType; }
			set {
				_objectType = value;

				if (!IsLoaded)
					Loaded += new RoutedEventHandler(_projectTreeViewItem_Loaded);
			}
		}

		private void _projectTreeViewItem_Loaded(object sender, RoutedEventArgs e) {
			//this.TVIHeaderBrush.ToolTip = _objectType.FilePath;

			switch (_objectType) {
				case TypeTreeViewItemClass.ClassType:
					PathIconClosed = "treeClass.png";
					PathIconOpened = "treeClass.png";
					break;
				case TypeTreeViewItemClass.MemberType:
					PathIconClosed = "properties.png";
					PathIconOpened = "properties.png";
					break;
				case TypeTreeViewItemClass.ListType:
					PathIconClosed = "treeList.png";
					PathIconOpened = "treeList.png";
					break;
				default:
					break;
			}
		}
	}

	public enum TypeTreeViewItemClass {
		ClassType,
		MemberType,
		ListType,
	}
}
