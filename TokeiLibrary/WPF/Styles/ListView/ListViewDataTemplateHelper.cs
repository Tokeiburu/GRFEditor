using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using ErrorManager;
using Application = System.Windows.Application;
using Binding = System.Windows.Data.Binding;
using Control = System.Windows.Controls.Control;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using ListViewItem = System.Windows.Controls.ListViewItem;
using Path = System.Windows.Shapes.Path;

namespace TokeiLibrary.WPF.Styles.ListView {
	public static class ListViewDataTemplateHelper {
		[DllImport("dwmapi.dll", PreserveSig = false)]
		public static extern bool DwmIsCompositionEnabled();

		public static void GenerateListViewTemplateNew(System.Windows.Controls.ListView list, 
			GeneralColumnInfo[] columnInfos, ListViewCustomComparer sorter, IList<string> triggers, 
			params string[] extraCommands) {
			try {
				bool generateHeader = true;
				bool generateStyle = true;
				bool overrideSizeRedraw = false;
				DataTemplate template = null;

				for (int i = 0; i < extraCommands.Length; i++) {
					if (extraCommands[i] == "generateHeader") {
						generateHeader = Boolean.Parse(extraCommands[i + 1]);
					}
					if (extraCommands[i] == "generateStyle") {
						generateStyle = Boolean.Parse(extraCommands[i + 1]);
					}
					if (extraCommands[i] == "overrideSizeRedraw") {
						overrideSizeRedraw = Boolean.Parse(extraCommands[i + 1]);
					}
					if (extraCommands[i] == "dataTemplate") {
						template = list.TryFindResource(extraCommands[i + 1]) as DataTemplate;
					}
					i++;
				}

				if (generateStyle) {
					_getStyle(list);
				}

				list.SetValue(ListViewLayoutManager.EnabledProperty, true);

				GridView grid = new GridView();
				grid.AllowsColumnReorder = false;

				if (!generateHeader) {
					grid.ColumnHeaderContainerStyle = list.TryFindResource("gridViewColumHeaderEmpty") as Style;
				}

				for (int index = 0; index < columnInfos.Length; index++) {
					GeneralColumnInfo columnInfo = columnInfos[index];
					GridViewColumn gridColumn = new GridViewColumn();

					gridColumn.Header = _generateHeader(columnInfo);

					if (columnInfo.FixedWidth > 0) {
						gridColumn.SetValue(FixedColumn.WidthProperty, columnInfo.FixedWidth);
						gridColumn.Width = columnInfo.FixedWidth;
					}

					if (columnInfo is ProportinalColumnInfo) {
						gridColumn.SetValue(ProportionalColumn.WidthProperty, ((ProportinalColumnInfo) columnInfo).Ratio);
					}

					if (columnInfo is RangeColumnInfo) {
						RangeColumnInfo rangeColumnInfo = (RangeColumnInfo) columnInfo;

						gridColumn.SetValue(RangeColumn.MinWidthProperty, rangeColumnInfo.MinWidth);
						gridColumn.SetValue(RangeColumn.IsFillColumnProperty, rangeColumnInfo.IsFill);

						if (rangeColumnInfo.MaxWidth > 0) {
							gridColumn.SetValue(RangeColumn.MaxWidthProperty, rangeColumnInfo.MaxWidth);
						}

						if (rangeColumnInfo.Width > 0) {
							gridColumn.Width = columnInfo.Width;
						}
					}

					if (columnInfo.IsFill) {
						gridColumn.SetValue(RangeColumn.IsFillColumnProperty, true);
					}

					if (columnInfo is DataColumnInfo) {
						if (template != null) {
							gridColumn.CellTemplate = template;
						}
						else {
							gridColumn.CellTemplate = _generateDataTemplate(columnInfo);
						}
					}
					else if (columnInfo is ImageColumnInfo) {
						ImageColumnInfo imageColumnInfo = (ImageColumnInfo) columnInfo;
						gridColumn.CellTemplate = _generateImageTemplate(imageColumnInfo);
					}
					else {
						gridColumn.CellTemplate = _generateTemplate(columnInfo, triggers, index == columnInfos.Length - 1);
					}

					if (Environment.OSVersion.Version.Major < 6)
						gridColumn.CellTemplate.Triggers.Add(_generateTriggers());
					else if (Environment.OSVersion.Version.Major >= 6) {
						try {
							if (!DwmIsCompositionEnabled()) {
								gridColumn.CellTemplate.Triggers.Add(_generateTriggers());
							}
						}
						catch {
							gridColumn.CellTemplate.Triggers.Add(_generateTriggers());
						}
					}

					grid.Columns.Add(gridColumn);
				}

				WpfUtils.SetCustomSorter(list, sorter);

				list.View = grid;

				if ((grid.Columns.Count > 0 && generateHeader && columnInfos.Any(p => p.IsFill)) || overrideSizeRedraw) {
					Style style = new Style(typeof(ListViewItem), list.ItemContainerStyle);
					GridViewColumn lastColumn = null;
					
					for (int i = 0; i < columnInfos.Length; i++) {
						if (columnInfos[i].IsFill) {
							lastColumn = grid.Columns[i];
							break;
						}
					}

					if (lastColumn == null) {
						return;
					}

					style.Setters.Add(new Setter(
						FrameworkElement.WidthProperty,
						new Binding("ActualWidth") {
							Source = lastColumn,
							Converter = new ListWidthConverter(list.BorderThickness),
							ConverterParameter = list
						}));

					list.ItemContainerStyle = style;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private static DataTemplate _generateTemplate(GeneralColumnInfo columnInfo, IList<string> triggers, bool isLast) {
			return _getDataTemplate("{Binding Path=" + columnInfo.DisplayExpression + "}", columnInfo.TextAlignment, triggers, 
				columnInfo.ToolTipBinding == null ? null : "{Binding Path=" + columnInfo.ToolTipBinding + "}", isLast, columnInfo);
		}

		private static DataTemplate _generateImageTemplate(ImageColumnInfo columnInfo) {
			return _getImageDataTemplate("{Binding Path=" + columnInfo.DisplayExpression + "}", TextAlignment.Center, columnInfo.MaxHeight, columnInfo.NoResize);
		}

		private static DataTemplate _generateDataTemplate(GeneralColumnInfo columnInfo) {
			return _getDataTemplate();
		}

		private static DataTrigger _generateTriggers() {
			DataTrigger trigger = new DataTrigger();
			trigger.Value = "True";
			Binding binding = new Binding();
			binding.Path = new PropertyPath("IsSelected");
			RelativeSource rS = new RelativeSource();
			rS.Mode = RelativeSourceMode.FindAncestor;
			rS.AncestorType = typeof(ListViewItem);
			binding.RelativeSource = rS;
			trigger.Binding = binding;
			trigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.White));
			return trigger;
		}

		private static GridViewColumnHeader _generateHeader(GeneralColumnInfo columnInfo) {
			GridViewColumnHeader header = new GridViewColumnHeader { HorizontalContentAlignment = HorizontalAlignment.Stretch };
			Grid headerGrid = new Grid();

			headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
			headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

			TextBlock txtBlock = new TextBlock { Text = columnInfo.Header ?? "", TextAlignment = TextAlignment.Center };
			Canvas canvas = new Canvas { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(-5, 0, 0, 0) };
			canvas.Width = 8;
			canvas.Visibility = Visibility.Collapsed;
			Path path = new Path();
			path.Fill = new SolidColorBrush(Colors.Black);
			path.StrokeThickness = 0.5f;
			canvas.Children.Add(path);
			canvas.SetValue(Grid.ColumnProperty, 1);

			headerGrid.Children.Add(txtBlock);
			headerGrid.Children.Add(canvas);

			header.Content = headerGrid;
			header.SetValue(LayoutColumn.GetAccessorBindingProperty, columnInfo.SearchGetAccessor ?? columnInfo.DisplayExpression);
			header.SetValue(LayoutColumn.IsClickableColumnProperty, true);
			return header;
		}

		public static void GenerateListViewTemplate(System.Windows.Controls.ListView list, ColumnInfo[] columns, ListViewCustomComparer sorter, IList<string> triggers) {
			try {
				_getStyle(list);
				//((INotifyCollectionChanged) list.Items).CollectionChanged += (_, a) => CollectionChanged(list, a);

				GridView grid = new GridView();
				grid.AllowsColumnReorder = false;

				for (int index = 0; index < columns.Length; index++) {
					ColumnInfo column = columns[index];
					FixedWidthColumn col = new FixedWidthColumn();
					//col.Header = column.Header;

					GridViewColumnHeader header = new GridViewColumnHeader {HorizontalContentAlignment = HorizontalAlignment.Stretch};
					header.SetValue(LayoutColumn.IsClickableColumnProperty, true);

					Grid headerGrid = new Grid();

					headerGrid.ColumnDefinitions.Add(new ColumnDefinition());
					headerGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Auto)});

					TextBlock txtBlock = new TextBlock {Text = column.Header, TextAlignment = TextAlignment.Center};
					Canvas canvas = new Canvas {VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(-5, 0, 0, 0)};
					canvas.Width = 8;
					canvas.Visibility = Visibility.Collapsed;
					Path path = new Path();
					//path.Data = Geometry.Parse("M -3,0 3,0 0,4 -3,0 3,0");
					path.Fill = new SolidColorBrush(Colors.Black);
					path.StrokeThickness = 0.5f;
					canvas.Children.Add(path);
					canvas.SetValue(Grid.ColumnProperty, 1);

					headerGrid.Children.Add(txtBlock);
					headerGrid.Children.Add(canvas);

					header.Content = headerGrid;
					col.Header = header;

					if (column.Width <= 0) {
						col.IsFill = true;
						col.FixedWidth = 0;
					}
					else {
						col.FixedWidth = column.Width;
					}

					col.BindingExpression = column.SearchGetAccessor;
					DataTrigger trigger = new DataTrigger();
					trigger.Value = "True";
					Binding binding = new Binding();
					binding.Path = new PropertyPath("IsSelected");
					binding.RelativeSource = new RelativeSource { Mode = RelativeSourceMode.FindAncestor, AncestorType = typeof (ListViewItem) };
					trigger.Binding = binding;
					trigger.Setters.Add(new Setter(Control.ForegroundProperty, new SolidColorBrush(Colors.White)));
					col.CellTemplate = column.IsImage ? _getImageDataTemplate(column.DisplayExpression, column.Alignment, column.MaxHeight, column.NoResize) :
						_getDataTemplate(column.DisplayExpression, column.Alignment, triggers, column.ToolTipBinding, index == columns.Length - 1, null);
					if (Environment.OSVersion.Version.Major < 6)
						col.CellTemplate.Triggers.Add(trigger);
					grid.Columns.Add(col);
					WpfUtils.SetSortBindingMember(col, new Binding(column.SearchGetAccessor));
				}

				WpfUtils.SetCustomSorter(list, sorter);

				list.SizeChanged += (sender, e) => {
					double width = list.ActualWidth;
					double totalWidth;

					GridViewColumnCollection collection = ((GridView)list.View).Columns;

					totalWidth = collection.OfType<FixedWidthColumn>().Where(p => !p.IsFill).Sum(p => p.ActualWidth);

					List<FixedWidthColumn> toFillColumns = collection.OfType<FixedWidthColumn>().Where(p => p.IsFill).ToList();

					int numOfColumns = toFillColumns.Count;

					if (numOfColumns > 0) {
						double remainingWidth = width - totalWidth - SystemInformation.VerticalScrollBarWidth;
						double widthPerColumn = remainingWidth / numOfColumns;

						if (widthPerColumn < 0)
							return;

						toFillColumns.ForEach(p => p.FixedWidth = widthPerColumn);
					}
				};

				list.View = grid;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private static void _getStyle(System.Windows.Controls.ListView list) {
			list.ItemContainerStyle = Application.Current.Resources["DefaultListViewItemStyle"] as Style;
		}

		private static DataTemplate _getDataTemplate() {
			XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

			XElement xDataTemplate;

			xDataTemplate =
				new XElement(ns + "DataTemplate",
								new XElement(ns + "GridViewRowPresenter",
									new XAttribute("Content", "{TemplateBinding Content}")));

			StringReader sr = new StringReader(xDataTemplate.ToString());
			XmlReader xr = XmlReader.Create(sr);
			return XamlReader.Load(xr) as DataTemplate;
		}

		private static DataTemplate _getImageDataTemplate(string bindingExpression, TextAlignment alignment, double imHeight, bool noResize) {
			XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

			XElement xDataTemplate;

			if (noResize) {
				xDataTemplate =
					new XElement(ns + "DataTemplate",
					             new XElement(ns + "Image",
					                          new XAttribute("Source", bindingExpression),
					                          //new XAttribute("Width", imWidth),
					                          //new XAttribute("MaxHeight", imHeight),
					                          new XAttribute("Stretch", "None"),
											  new XAttribute("HorizontalAlignment", "Left"),
					                          new XAttribute("Margin", "-4 0 -4 0")));
				//new XAttribute("VerticalAlignment", alignment == TextAlignment.Center ? VerticalAlignment.Center : alignment == TextAlignment.Left ? VerticalAlignment.Top : VerticalAlignment.Bottom),
				//new XAttribute("HorizontalAlignment", alignment == TextAlignment.Center ? HorizontalAlignment.Center : alignment == TextAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Right)));
			}
			else {
				xDataTemplate =
				new XElement(ns + "DataTemplate",
							 new XElement(ns + "Image",
										  new XAttribute("Source", bindingExpression),
					//new XAttribute("Width", imWidth),
										  new XAttribute("MaxHeight", imHeight),
										  new XAttribute("Stretch", "None"),
										  new XAttribute("Margin", "-4 0 -4 0"),
										  new XAttribute("VerticalAlignment", alignment == TextAlignment.Center ? VerticalAlignment.Center : alignment == TextAlignment.Left ? VerticalAlignment.Top : VerticalAlignment.Bottom),
										  new XAttribute("HorizontalAlignment", alignment == TextAlignment.Center ? HorizontalAlignment.Center : alignment == TextAlignment.Left ? HorizontalAlignment.Left : HorizontalAlignment.Right)));
			}

			StringReader sr = new StringReader(xDataTemplate.ToString());
			XmlReader xr = XmlReader.Create(sr);
			return XamlReader.Load(xr) as DataTemplate;
		}

		private static DataTemplate _getDataTemplate(string bindingExpression, TextAlignment alignment, IList<string> triggers, string toolTip, bool isLast, GeneralColumnInfo column) {
			XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

			List<XElement> bindings = new List<XElement>();

			for (int i = 0; i < triggers.Count; i++) {
				bindings.Add(new XElement(ns + "DataTrigger",
				                          new XAttribute("Binding", "{Binding " + triggers[i] + "}"),
				                          new XAttribute("Value", "True"),
				                          new XElement(ns + "Setter",
				                                       new XAttribute("Property", "Control.Foreground"),
				                                       new XAttribute("Value", triggers[i + 1]))
					             ));
				i++;
			}

			if (triggers.Count == 0) {
				bindings.Add(new XElement(ns + "DataTrigger",
					new XAttribute("Binding", "{Binding Null}"),
					new XAttribute("Value", "False"),
					new XElement(ns + "Setter",
						new XAttribute("Property", "Control.Foreground"),
						new XAttribute("Value", "{DynamicResource TextForeground}"))
					));
			}
			else {
				List<XElement> revTriggers = new List<XElement>();

				for (int i = 0; i < triggers.Count; i += 2) {
					revTriggers.Add(
						new XElement(ns + "Condition",
							new XAttribute("Binding", "{Binding " + triggers[i] + "}"),
							new XAttribute("Value", "False"))
						);
				}

				bindings.Add(new XElement(ns + "MultiDataTrigger",
					new XElement(ns + "MultiDataTrigger.Conditions",
							revTriggers
						),
					new XElement(ns + "Setter",
						new XAttribute("Property", "Control.Foreground"),
						new XAttribute("Value", "{DynamicResource TextForeground}"))));
			}

			XElement xDataTemplate;

			List<XAttribute> textBlock = new List<XAttribute>();

			textBlock.Add(new XAttribute("Text", bindingExpression));
			textBlock.Add(new XAttribute("TextAlignment", alignment));
			textBlock.Add(new XAttribute("TextWrapping", column == null ? TextWrapping.NoWrap : column.TextWrapping));
			textBlock.Add(new XAttribute("Margin", isLast ? "-4 0 0 0" : "-4 0 -4 0"));

			if (toolTip != null) {
				textBlock.Add(new XAttribute("ToolTip", toolTip));
			}

			if (triggers.Count == 0)
				xDataTemplate =
				new XElement(ns + "DataTemplate",
				             new XElement(ns + "TextBlock",
										  textBlock,
										  new XAttribute("Foreground", "{DynamicResource TextForeground}")));
			else
				xDataTemplate =
				new XElement(ns + "DataTemplate",
				             new XElement(ns + "TextBlock",
										  textBlock),
				             new XElement(ns + "DataTemplate.Triggers",
										 bindings));
												
			StringReader sr = new StringReader(xDataTemplate.ToString());
			XmlReader xr = XmlReader.Create(sr);
			return XamlReader.Load(xr) as DataTemplate;
		}

		#region Nested type: ColumnInfo

		public class ColumnInfo {
			public string Header { get; set; }
			public double Width { get; set; }
			public string SearchGetAccessor { get; set; }
			public string DisplayExpression { get; set; }
			public string Margin { get; set; }
			public int ImWidth { get; set; }
			public int MaxHeight { get; set; }
			public bool IsImage { get; set; }
			public bool NoResize { get; set; }
			public bool UseNewSorter { get; set; }
			public TextAlignment Alignment { get; set; }
			public string ToolTipBinding { get; set; }
		}

		#endregion

		#region Nested type: GeneralColumnInfo

		public class GeneralColumnInfo {
			private TextWrapping _textWrapping = TextWrapping.NoWrap;

			public string SearchGetAccessor { get; set; }
			public string DisplayExpression { get; set; }
			public string ToolTipBinding { get; set; }
			public string Header { get; set; }
			public bool IsFill { get; set; }
			public TextAlignment TextAlignment { get; set; }
			public double FixedWidth { get; set; }
			public double Width { get; set; }

			public TextWrapping TextWrapping {
				get { return _textWrapping; }
				set { _textWrapping = value; }
			}
		}

		#endregion

		#region Nested type: ImageColumnInfo

		public class ImageColumnInfo : GeneralColumnInfo {
			public double MaxHeight { get; set; }
			public bool NoResize { get; set; }
			public bool IsDrawingGroup { get; set; }
		}

		#endregion

		#region Nested type: DataColumnInfo

		public class DataColumnInfo : GeneralColumnInfo {
		}

		#endregion

		#region Nested type: ProportinalColumnInfo

		public class ProportinalColumnInfo : GeneralColumnInfo {
			public double Ratio { get; set; }
		}

		#endregion

		#region Nested type: RangeColumnInfo

		public class RangeColumnInfo : GeneralColumnInfo {
			public double MinWidth { get; set; }
			public double MaxWidth { get; set; }
		}

		#endregion
	}
}
