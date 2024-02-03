using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using GRF.Core;
using TokeiLibrary.WPF.Styles;

namespace GRFEditor.Tools.MapExtractor {
	/// <summary>
	/// Interaction logic for MapExtractorDialog.xaml
	/// </summary>
	public partial class MapExtractorDialog : TkWindow {
		private readonly MapExtractor _mapExtractor;
		private readonly Dictionary<string, GrfHolder> _openedGrfs = new Dictionary<string, GrfHolder>();

		public MapExtractorDialog(GrfHolder grf, string fileName) : base("Export map files", "mapEditor.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();

			_mapExtractor = new MapExtractor(grf, fileName);
			_gridMapExtractor.Children.Add(_mapExtractor);
			_mapExtractor.Reload(grf, fileName, () => false);
		}

		protected override void OnClosing(CancelEventArgs e) {
			_mapExtractor.AsyncOperation.Cancel();

			foreach (GrfHolder grf in _openedGrfs.Values) {
				grf.Close();
			}

			base.OnClosing(e);
		}

		private void _buttonExport_Click(object sender, RoutedEventArgs e) {
			_mapExtractor.Export();
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) {
			_mapExtractor.ExportAt();
		}
	}
}