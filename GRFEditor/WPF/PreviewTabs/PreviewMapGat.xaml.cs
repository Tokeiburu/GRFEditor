using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.FileFormats.GatFormat;
using GRF.IO;
using GRF.Image;
using GRF.GrfSystem;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewMapGat.xaml
	/// </summary>
	public partial class PreviewMapGat : FilePreviewTab {
		private readonly EditorMainWindow _editor;
		//private readonly List<FancyButton> _buttons = new List<FancyButton>();
		private readonly GrfImageWrapper _wrapper = new GrfImageWrapper();

		public PreviewMapGat(EditorMainWindow editor) {
			_editor = editor;
			InitializeComponent();

			Binder.Bind(_cbTransparent, () => GrfEditorConfiguration.GatPreviewTransparent, () => Update(true));
			Binder.Bind(_cbRescale, () => GrfEditorConfiguration.GatPreviewRescale, () => Update(true));
			Binder.Bind(_cbHideBorders, () => GrfEditorConfiguration.GatPreviewHideBorders, () => Update(true));

			_cbPreviewMode.SelectedIndex = GrfEditorConfiguration.GatPreviewMode;
			_cbPreviewMode.SelectionChanged += new SelectionChangedEventHandler(_cbPreviewMode_SelectionChanged);
			_isInvisibleResult = () => _imagePreview.Dispatch(p => p.Visibility = Visibility.Hidden);
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper);
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			WpfUtils.AddMouseInOutEffectsBox(_cbHideBorders, _cbRescale, _cbTransparent);
		}

		public Action<Brush> BackgroundBrushFunction {
			get { return v => this.Dispatch(p => _scrollViewer.Background = v); }
		}

		private void _cbPreviewMode_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			Update(true);
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) {
			if (_wrapper.Image != null)
				_wrapper.Image.SaveTo(_entry.RelativePath, PathRequest.ExtractSetting);
		}

		protected override void _load(FileEntry entry) {
			ImageSource source = null;
			string fileName = entry.RelativePath;
			_labelHeader.Dispatch(p => p.Text = "Map preview : " + Path.GetFileName(fileName));
			_imagePreview.Dispatch(p => p.Tag = Path.GetFileNameWithoutExtension(fileName));

			Gat gat = new Gat(entry.GetDecompressedData());
			GatPreviewFormat preview = (GatPreviewFormat) _cbPreviewMode.Dispatch(p => p.SelectedIndex);

			GatPreviewOptions options = 0;

			if (GrfEditorConfiguration.GatPreviewRescale) options |= GatPreviewOptions.Rescale;
			if (GrfEditorConfiguration.GatPreviewHideBorders) options |= GatPreviewOptions.HideBorders;
			if (GrfEditorConfiguration.GatPreviewTransparent) options |= GatPreviewOptions.Transparent;

			gat.LoadImage(preview, options, fileName, _grfData);
			_wrapper.Image = gat.Image;

			if (_wrapper.Image != null)
				source = _wrapper.Image.Cast<BitmapSource>();

			if (_isCancelRequired()) return;

			_imagePreview.Dispatch(p => p.Source = source);
			_imagePreview.Dispatch(p => p.Visibility = Visibility.Visible);
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
		}

		private void _buttonSaveInGrf_Click(object sender, RoutedEventArgs e) {
			try {
				string tempImage = TemporaryFilesManager.GetTemporaryFilePath("img_{0:0000}.bmp");
				string root = (_grfData.FileName.IsExtension(".thor") ? GrfStrings.RgzRoot : "");
				_wrapper.Image.Convert(GrfImageType.Indexed8);
				_wrapper.Image.Save(tempImage);
				_grfData.Commands.AddFile(GrfPath.Combine(EncodingService.FromAnyToDisplayEncoding(root + @"data\texture\À¯ÀúÀÎÅÍÆäÀÌ½º\map\"), _entry.DisplayRelativePath.ReplaceExtension(".bmp")), File.ReadAllBytes(tempImage), _replaceFileCallback);
				//WindowProvider.ShowDialog("Map added.");
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _replaceFileCallback(string grfpath, string filename, string filepath, bool isExecuted) {
			if (isExecuted) {
				_editor._treeViewPathManager.AddFolders(_grfData.FileName, new List<string> { grfpath });
			}
			else {
				_editor._treeViewPathManager.AddFoldersUndo(_grfData.FileName);
			}
		}
	}
}