using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRFEditor.Tools.MapExtractor;
using TokeiLibrary;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewMapExtractor.xaml
	/// </summary>
	public partial class PreviewMapExtractor : FilePreviewTab {
		private MapExtractor _mapExtractor;

		public PreviewMapExtractor() : base(true) {
			InitializeComponent();

			_isInvisibleResult = () => _primary.Dispatch(p => p.Content = null);
		}

		public Action<Brush> BackgroundBrushFunction {
			get {
				return v => this.Dispatch(delegate {
					if (_mapExtractor == null)
						return;
					if (_mapExtractor._quickPreview == null)
						return;
					if (_mapExtractor._quickPreview._scrollViewer == null)
						return;
					_mapExtractor._quickPreview._scrollViewer.Background = v;
				});
			}
		}

		protected override void _load(FileEntry entry) {
			Dispatcher.Invoke(new Action(delegate {
				try {
					if (_isCancelRequired()) return;

					if (_mapExtractor == null) {
						_mapExtractor = new MapExtractor(_grfData, entry.RelativePath);
						_mapExtractor.AsyncOperation.DoNotShowExtraDialogs = true;
						_primary.Content = _mapExtractor;
					}
					else if (_primary.Content == null) {
						_primary.Content = _mapExtractor;
						_mapExtractor.Reload(_grfData, entry.RelativePath, _isCancelRequired);
					}
					else
						_mapExtractor.Reload(_grfData, entry.RelativePath, _isCancelRequired);

					if (_isCancelRequired()) return;

					_labelHeader.Content = "Extract resources : " + Path.GetFileNameWithoutExtension(entry.RelativePath);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}));
		}

		private void _buttonExport_Click(object sender, RoutedEventArgs e) {
			if (_mapExtractor != null) {
				_mapExtractor.Export();
			}
		}

		private void _buttonExportAt_Click(object sender, RoutedEventArgs e) {
			if (_mapExtractor != null) {
				_mapExtractor.ExportAt();
			}
		}
	}
}