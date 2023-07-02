using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokeiLibrary.WPF.Styles;
using TokeiLibrary.WPF.Styles.ListView;

namespace TokeiLibrary.WpfBugFix {
	/// <summary>
	/// Interaction logic for ToggleMemoryPopup.xaml
	/// </summary>
	public partial class ToggleMemoryPopup : TkWindow {
		internal readonly RangeListView _listView = new RangeListView();

		public ToggleMemoryPopup() {
			InitializeComponent();

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
		}
	}
}
