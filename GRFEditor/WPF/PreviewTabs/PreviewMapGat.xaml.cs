using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.FileFormats.GatFormat;
using GRF.IO;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GrfToWpfBridge;
using TokeiLibrary;
using Utilities.Extension;
using Utilities.Services;
using GrfToWpfBridge.PreviewTabs;
using GRFEditor.WPF.PreviewTabs.Controls;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewMapGat.xaml
	/// </summary>
	public partial class PreviewMapGat : FilePreviewTab {
		private readonly EditorMainWindow _editor;
		//private readonly List<FancyButton> _buttons = new List<FancyButton>();
		private readonly GrfImageWrapper _primaryImage = new GrfImageWrapper();

		public PreviewMapGat(EditorMainWindow editor) {
			_editor = editor;
			InitializeComponent();

			Binder.Bind(_cbTransparent, () => GrfEditorConfiguration.GatPreviewTransparent, () => Update(true));
			Binder.Bind(_cbRescale, () => GrfEditorConfiguration.GatPreviewRescale, () => Update(true));
			Binder.Bind(_cbHideBorders, () => GrfEditorConfiguration.GatPreviewHideBorders, () => Update(true));

			_cbPreviewMode.SelectedIndex = GrfEditorConfiguration.GatPreviewMode;
			_cbPreviewMode.SelectionChanged += (s, e) => Update(true);
			VirtualFileDataObject.SetDraggable(_imagePreview, _primaryImage);
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			WpfUtilities.AddMouseInOutUnderline(_cbHideBorders, _cbRescale, _cbTransparent);
			ErrorPanel = _errorPanel;
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		protected override void _load(FileEntry entry) {
			_setupUI(entry);

			_primaryImage.Image = _tryLoadImage(entry);
			_primaryImage.ExportFileName = Path.GetFileNameWithoutExtension(entry.RelativePath);

			if (_isCancelRequired()) return;

			_displayImage();
		}

		private void _displayImage() {
			ImageSource source = null;

			if (_primaryImage.Image != null)
				source = _primaryImage.Image.Cast<BitmapSource>();

			_imagePreview.Dispatch(p => p.Source = source);
		}

		private GrfImage _tryLoadImage(FileEntry entry) {
			Gat gat = new Gat(entry);
			GatPreviewFormat previewFormat = (GatPreviewFormat)_cbPreviewMode.Dispatch(p => p.SelectedIndex);

			GatPreviewOptions options = GatPreviewOptions.None;
			if (GrfEditorConfiguration.GatPreviewRescale) options |= GatPreviewOptions.Rescale;
			if (GrfEditorConfiguration.GatPreviewHideBorders) options |= GatPreviewOptions.HideBorders;
			if (GrfEditorConfiguration.GatPreviewTransparent) options |= GatPreviewOptions.Transparent;

			gat.LoadImage(previewFormat, options, entry.RelativePath, _grfData);
			return gat.Image;
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Text = "Map preview: " + entry.DisplayRelativePath;
			});
		}

		private void _buttonSaveInGrf_Click(object sender, RoutedEventArgs e) {
			try {
				if (_primaryImage.Image == null)
					throw new Exception("No image loaded. This usually happens if the file is corrupted or encrypted.");

				var image = _primaryImage.Image.Clone();
				image.Convert(GrfImageType.Indexed8);

				using (MemoryStream stream = new MemoryStream()) {
					image.Save(stream);
					_grfData.Commands.AddFile(_getTargetPath(), stream.ToArray(), _editor.ReplaceFilesCallback);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private string _getTargetPath() {
			string root = _grfData.FileName.IsExtension(".thor") ? GrfStrings.RgzRoot : "";
			string basePath = root + @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\map\";

			return GrfPath.Combine(
				EncodingService.FromAnyToDisplayEncoding(basePath), 
				_entry.DisplayRelativePath.ReplaceExtension(".bmp")
			);
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) => ImageHelper.ExportAs(_primaryImage, _entry.RelativePath);
	}
}