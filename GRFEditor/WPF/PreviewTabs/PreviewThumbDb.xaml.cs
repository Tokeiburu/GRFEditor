using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.DbFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GrfToWpfBridge;
using TokeiLibrary;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewThumbDb.xaml
	/// </summary>
	public partial class PreviewThumbDb : FilePreviewTab {
		private readonly GrfImageWrapper _wrapper = new GrfImageWrapper();
		private ThumbDB _db;

		public PreviewThumbDb() {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			_isInvisibleResult = new Action(() => _scrollViewer.Visibility = Visibility.Hidden);
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper);
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null)
				_wrapper.Image.SaveTo(_entry.RelativePath, PathRequest.ExtractSetting);
		}

		protected override void _load(FileEntry entry) {
			string fileName = entry.RelativePath;
			_labelHeader.Dispatch(p => p.Content = "Image preview : " + Path.GetFileName(fileName));

			_comboBoxActionIndex.Dispatch(p => p.Items.Clear());

			string rFileName = Path.Combine(Configuration.TempPath, Path.GetRandomFileName());
			File.WriteAllBytes(rFileName, entry.GetDecompressedData());

			if (_isCancelRequired()) return;

			_db = new ThumbDB(rFileName);
			string[] files = _db.GetThumbfiles();

			if (_isCancelRequired()) return;

			for (int i = 0; i < files.Length; i++) {
				int i1 = i;
				_comboBoxActionIndex.Dispatcher.Invoke((Action) (() => _comboBoxActionIndex.Items.Add(files[i1])));
			}

			if (_isCancelRequired()) return;

			_imagePreview.Dispatch(p => p.VerticalAlignment = VerticalAlignment.Top);
			_imagePreview.Dispatch(p => p.HorizontalAlignment = HorizontalAlignment.Left);
			_comboBoxActionIndex.Dispatch(p => p.SelectedIndex = 0);
			_comboBoxActionIndex.Dispatch(p => p.Visibility = Visibility.Visible);
			_imagePreview.Dispatch(p => p.Visibility = Visibility.Visible);
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _comboBoxActionIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxActionIndex.SelectedItem == null) return;

			try {
				_wrapper.Image = _db.GetThumbnailImage(_comboBoxActionIndex.SelectedItem.ToString());
				_imagePreview.Dispatch(p => p.Tag = Path.GetFileNameWithoutExtension(_comboBoxActionIndex.SelectedItem.ToString()));
				_imagePreview.Source = _wrapper.Image.Cast<BitmapSource>();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null)
				_wrapper.Image.SaveTo(_entry.RelativePath, PathRequest.ExtractSetting);
		}
	}
}