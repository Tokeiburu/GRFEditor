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
using GRFEditor.Core;
using GRFEditor.WPF.PreviewTabs.Controls;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewThumbDb.xaml
	/// </summary>
	public partial class PreviewThumbDb : FilePreviewTab {
		private readonly GrfImageWrapper _primaryImage = new GrfImageWrapper();
		private ThumbDB _db;

		public PreviewThumbDb() {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			VirtualFileDataObject.SetDraggable(_imagePreview, _primaryImage);
			ErrorPanel = _errorPanel;
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			_db = _tryLoadThumbnailDb(entry);

			if (_isCancelRequired()) return;

			_displayThumbnailDb();
		}

		private void _displayThumbnailDb() {
			this.Dispatch(delegate {
				string[] files = _db.GetThumbfiles();

				_comboBoxActionIndex.ItemsSource = files;
				_comboBoxActionIndex.SelectedIndex = 0;

				// Clear previous displayed image
				if (files.Length == 0) {
					_imagePreview.Source = null;
				}
			});
		}

		private ThumbDB _tryLoadThumbnailDb(FileEntry entry) {
			// A physical file is required for ThumbDB to read its content.
			string rFileName = Path.Combine(Configuration.TempPath, Path.GetRandomFileName());
			File.WriteAllBytes(rFileName, entry.GetDecompressedData());
			return new ThumbDB(rFileName);
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "Image preview: " + entry.DisplayRelativePath;

				_comboBoxActionIndex.ItemsSource = null;
			});
		}

		private void _comboBoxActionIndex_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (_comboBoxActionIndex.SelectedItem == null) return;

			try {
				string fileName = _comboBoxActionIndex.SelectedItem.ToString();
				_primaryImage.Image = _db.GetThumbnailImage(fileName);
				_primaryImage.ExportFileName = Path.GetFileNameWithoutExtension(fileName);
				_imagePreview.Source = _primaryImage.Image.Cast<BitmapSource>();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
	}
}