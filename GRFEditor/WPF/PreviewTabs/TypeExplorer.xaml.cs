using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using GRF.FileFormats;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Graphics;
using GRF.Image;
using GrfToWpfBridge.TreeViewManager;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for TypeExplorer.xaml
	/// </summary>
	public partial class TypeExplorer : UserControl {
		private const int MaxChildrenToExpand = 200;

		#region Delegates

		public delegate bool CancelTokenDelegate();

		#endregion

		private static readonly HashSet<Type> _doNotExpandOnInitTypes = new HashSet<Type>();
		private ObjectParserConfig _parserConfig = new ObjectParserConfig();
		private HashSet<ObjectParser> _processed;

		static TypeExplorer() {
			_doNotExpandOnInitTypes.Add(typeof (float));
			_doNotExpandOnInitTypes.Add(typeof (Boolean));
			_doNotExpandOnInitTypes.Add(typeof (byte));
			_doNotExpandOnInitTypes.Add(typeof (UInt32));
			_doNotExpandOnInitTypes.Add(typeof (UInt16));
			_doNotExpandOnInitTypes.Add(typeof (TkVector2));
			_doNotExpandOnInitTypes.Add(typeof (TkVector3));
			_doNotExpandOnInitTypes.Add(typeof (TkVector4));
			_doNotExpandOnInitTypes.Add(typeof (TextureVertex));
			_doNotExpandOnInitTypes.Add(typeof (Face));
			_doNotExpandOnInitTypes.Add(typeof (Mesh));
			_doNotExpandOnInitTypes.Add(typeof (GrfImage));
			_doNotExpandOnInitTypes.Add(typeof (FileHeader));
			_doNotExpandOnInitTypes.Add(typeof (RswObject));
			_doNotExpandOnInitTypes.Add(typeof (byte[]));
		}

		public TypeExplorer() {
			InitializeComponent();
			_initParserObjectConfig();
		}

		private void _initParserObjectConfig() {
			_parserConfig.ExploreAllLists = true;
			_parserConfig.ExploreAllClasses = true;
			_parserConfig.TypeValueOverrides[typeof(Enum)] = o => "{" + o + "}";
			_parserConfig.TypeValueOverrides[typeof(GrfColor)] = o => "{" + o + "}";
			_parserConfig.TypeValueOverrides[typeof(Stream)] = o => "{Stream}";
			_parserConfig.TypeValueOverrides[typeof(String)] = o => "\"" + ((String)o) + "\"";
			_parserConfig.TypeValueOverrides[typeof(Boolean)] = o => "{" + ((Boolean)o).ToString(CultureInfo.InvariantCulture) + "}";
		}

		public void LoadObject(object item, CancelTokenDelegate cancelToken, int expandLevel = 1) {
			if (cancelToken())
				return;

			_view.Items.Clear();

			if (expandLevel < 0)
				expandLevel = 0;

			_processed = new HashSet<ObjectParser>();
			ObjectParser parser = new ObjectParser(item, _parserConfig);
			_addNode(null, parser, cancelToken);
			_initExpand((TypeTreeViewItem)_view.Items[0], parser, 2, cancelToken);
			
			if (cancelToken())
				return;
		}

		private void _initExpand(TypeTreeViewItem tvi, ObjectParser parser, int level, CancelTokenDelegate cancelToken) {
			if (parser.Children.Count == 0 || level <= 0 || cancelToken())
				return;

			var type = parser.Type;

			if (parser.Object is IList) {
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
					Type itemType = type.GetGenericArguments()[0];

					if (_isTypeExpandableOnInit(itemType)) {
						tvi.IsExpanded = true;
					}
				}
			}
			else if (parser.Children.Count > 0) {
				if (_isTypeExpandableOnInit(type)) {
					tvi.IsExpanded = true;
				}
			}

			if (tvi.IsExpanded && parser.Children.Count > 0) {
				for (int i = 0; i < parser.Children.Count; i++) {
					_initExpand((TypeTreeViewItem)tvi.Items[i], parser.Children[i], level - 1, cancelToken);
				}
			}
		}

		private bool _isTypeExpandableOnInit(Type itemType) {
			return !_doNotExpandOnInitTypes.Contains(itemType) && (itemType.BaseType == null || !_doNotExpandOnInitTypes.Contains(itemType.BaseType));
		}

		private void _addNode(TypeTreeViewItem parentTvi, ObjectParser parser, CancelTokenDelegate cancelToken, int index = -1) {
			if (cancelToken())
				return;

			parser.Evaluate();

			TypeTreeViewItem tvi = new TypeTreeViewItem(_view, _getDisplayType(parser));
			tvi.HeaderText = _getDisplayValue(parser, index);

			if (parser.Children.Count > 0) {
				tvi.Expanded += (s, e) => _tvi_Expanded(tvi, parser, cancelToken);
				tvi.Items.Add(new TypeTreeViewItem(_view, TypeTreeViewItemClass.TooManyType) { HeaderText = "..." });
			}

			if (parentTvi != null)
				parentTvi.Items.Add(tvi);
			else
				_view.Items.Add(tvi);
		}

		private void _tvi_Expanded(TypeTreeViewItem tvi, ObjectParser parser, CancelTokenDelegate cancelToken) {
			if (!_processed.Add(parser))
				return;

			if (parser.Children.Count > MaxChildrenToExpand)
				return;

			tvi.Items.Clear();

			foreach (var child in parser.Children)
				child.Evaluate();

			parser.Children = parser.Children.OrderBy(p => p.Priority).ToList();

			for (int i = 0; i < parser.Children.Count; i++) {
				var child = parser.Children[i];

				_addNode(tvi, child, cancelToken, i);
			}
		}

		private string _getDisplayValue(ObjectParser parser, int index = -1) {
			if (parser.Parent != null && parser.Parent.Object is IList) {
				if (parser.Object is string)
					return $"{parser.Parent.FriendlyName}[{index}] | {parser.Value}";

				return $"{parser.Parent.FriendlyName}[{index}] | {parser.Object}";
			}

			if (parser.Object is IList) {
				return $"{parser.FriendlyName} | {{Count = {parser.Children.Count}}}";
			}

			if (parser.Children.Count > 0) {
				return $"{parser.FriendlyName} | {{{parser.Object}}}";
			}

			return $"{parser.FriendlyName} | {parser.Value ?? parser.Object.ToString()}";
		}

		private TypeTreeViewItemClass _getDisplayType(ObjectParser parser) {
			if (parser.Object is IList) {
				return TypeTreeViewItemClass.ListType;
			}

			if (parser.Children.Count > 0)
				return TypeTreeViewItemClass.ClassType;

			return TypeTreeViewItemClass.MemberType;
		}
	}
}