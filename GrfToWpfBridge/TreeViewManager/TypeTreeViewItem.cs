using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using ErrorManager;
using TokeiLibrary.WPF;
using Utilities;

namespace GrfToWpfBridge.TreeViewManager {
	public class TypeTreeViewItem : TkTreeViewItem {
		public static DependencyProperty ObjectTypeProperty = DependencyProperty.Register("ObjectType", typeof(TypeTreeViewItemClass), typeof(ProjectTreeViewItem), new PropertyMetadata(default(TypeTreeViewItemClass)));

		public TypeTreeViewItemClass ObjectType {
			get { return (TypeTreeViewItemClass)GetValue(ObjectTypeProperty); }
			set { SetValue(ObjectTypeProperty, value); }
		}

		public TypeTreeViewItem(TkView parent, TypeTreeViewItemClass objectType)
			: base(parent) {
			CanBeDragged = false;
			CanBeDropped = false;

			ObjectType = objectType;

			Style = (Style)this.FindResource("TypeTreeViewItemStyle");
		}

		public bool? HasBeenLoaded { get; set; }

		public bool DontAutomaticallyExpand { get; set; }
	}

	public enum TypeTreeViewItemClass {
		ClassType,
		MemberType,
		ListType,
		TooManyType,
	}
}
