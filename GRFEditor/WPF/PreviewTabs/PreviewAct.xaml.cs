using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.ActFormat;
using GRF.Image;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using GrfToWpfBridge.ActRenderer;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.DrawingComponents;
using GrfToWpfBridge.PreviewTabs;
using TokeiLibrary;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewAct.xaml
	/// </summary>
	public partial class PreviewAct : FilePreviewTab {
		public class FrameRendererEditor : IFrameRendererEditor {
			public Act Act { get; set; }
			public int SelectedAction => IndexSelector.SelectedAction;
			public int SelectedFrame => IndexSelector.SelectedFrame;
			public IActIndexSelector IndexSelector { get; set; }
			public FrameRenderer FrameRenderer { get; set; }
			public Grid GridPrimary { get; set; }
			public event FrameRendererEventDelegates.ActEditorEventDelegate ActLoaded;
			public void OnActLoaded() => ActLoaded?.Invoke(Act);
			public bool IsLoading { get; set; }
			public int PreferedLoadingAction { get; set; }
			public Canvas Canvas => FrameRenderer.Canvas;
		}

		private FrameRendererEditor _editor = new FrameRendererEditor();
		private FrameRendererConfiguration _config;

		public class DefaultDrawModule : IDrawingModule {
			private readonly Func<List<DrawingComponent>> _getComponents;
			public int DrawingPriority => 0;
			public List<DrawingComponent> GetComponents() => _getComponents() ?? new List<DrawingComponent>();
			public bool Permanent => false;

			public DefaultDrawModule(Func<List<DrawingComponent>> getComponents) {
				_getComponents = getComponents;
			}
		}

		public PreviewAct(AsyncOperation asyncOperation) {
			InitializeComponent();
			SettingsDialog.UIPanelPreviewBackgroundPick(_indexSelector._qcsBackground);
			_renderer.Canvas.Background = GrfEditorConfiguration.UIPanelPreviewBackground;

			_indexSelector._buttonExportAsGif.Click += _buttonExportAsGif_Click;
			
			IsVisibleChanged += delegate {
				if (IsVisible)
					_indexSelector.Play();
				else
					_indexSelector.Stop();
			};

			_initializeEditor();
			_bindSettings();

			WpfUtilities.AddMouseInOutUnderline(_cbGrid);
			ErrorPanel = _errorPanel;
		}

		private void _bindSettings() {
			Binder.Bind(_indexSelector._buttonScale, () => GrfEditorConfiguration.PreviewActScaleType, v => GrfEditorConfiguration.PreviewActScaleType = v, delegate {
				_indexSelector._buttonScale.IsPressed = GrfEditorConfiguration.PreviewActScaleType;
				_config.ActEditorScalingMode = GrfEditorConfiguration.PreviewActScaleType ? BitmapScalingMode.HighQuality : BitmapScalingMode.NearestNeighbor;
				_renderer.DrawSlotManager?.ImagesDirty();
			}, true);
			Binder.Bind(_cbGrid, () => GrfEditorConfiguration.PreviewActShowGrid, v => GrfEditorConfiguration.PreviewActShowGrid = v, delegate {
				var color = GrfEditorConfiguration.PreviewActShowGrid ? GrfColors.Black : GrfColors.Transparent;
				_config.ActEditorGridLineHorizontal.Set(color);
				_config.ActEditorGridLineVertical.Set(color);
				_renderer.Update();
			}, true);
		}

		private void _initializeEditor() {
			_editor.IndexSelector = _indexSelector;
			_editor.FrameRenderer = _renderer;
			_editor.GridPrimary = _gridPrimary;
			
			_indexSelector.Init(_editor, 0, 0);

			_config = new FrameRendererConfiguration(GrfEditorConfiguration.ConfigAsker);
			_renderer.RelativeCenter = new Point(0.5d, 0.6d);
			_renderer.Init(_editor, _config);

			_renderer.DrawingModules.Add(new DefaultDrawModule(delegate {
				if (_editor.Act != null) {
					return new List<DrawingComponent> { new ActDraw(_editor.Act) };
				}

				return new List<DrawingComponent>();
			}));
		}

		public Action<Brush> BackgroundBrushFunction => v => this.Dispatch(() => _renderer.Canvas.Background = v);

		protected override void _load(FileEntry entry) {
			try {
				_setupUI(entry);

				bool success = _tryLoadAct(entry, out byte[] actData, out byte[] sprData);
				if (!success) {
					_setAnimation(null);
					return;
				}

				if (_isCancelRequired()) return;

				_setAnimation(new Act(actData, sprData));
			}
			catch {
				_setAnimation(null);
				throw;
			}
		}

		private void _setAnimation(Act act) {
			act?.Safe();

			this.Dispatch(delegate {
				_editor.IsLoading = true;
				_editor.Act = act;
				_editor.OnActLoaded();
				_editor.IndexSelector.Init(_editor, _editor.PreferedLoadingAction, 0);
				
				if (act != null)
					_editor.IndexSelector.Play();

				_editor.FrameRenderer.Update();
				_editor.IsLoading = false;
			});
		}

		private bool _tryLoadAct(FileEntry entry, out byte[] actData, out byte[] sprData) {
			actData = entry.GetDecompressedData();
			sprData = null;

			var sprPath = entry.RelativePath.ReplaceExtension(".spr");
			var sprEntry = _grfData.FileTable.TryGet(sprPath);
			
			if (sprEntry == null) {
				// If a garment/wing, attempt to load the SPR from the parent directory
				if (entry.RelativePath.StartsWith(EncodingService.FromAnyToDisplayEncoding(@"data\sprite\·Îºê\"))) {
					var dirs = GrfPath.SplitDirectories(entry.RelativePath).ToList();
					dirs.RemoveAt(dirs.Count - 1);
					dirs.RemoveAt(dirs.Count - 1);
					dirs.Add(dirs.Last() + ".spr");

					sprEntry = _grfData.FileTable.TryGet(GrfPath.Combine(dirs.ToArray()));
				}
			}

			if (sprEntry == null) {
				throw new Exception("Could not find the corresponding SPR file: '" + sprPath + "'.");
			}

			sprData = sprEntry.GetDecompressedData();
			return true;
		}

		private void _setupUI(FileEntry entry) {
			this.Dispatch(delegate {
				_labelHeader.Dispatch(p => p.Text = "Animation: " + entry.DisplayRelativePath);
			});
		}

		private void _buttonExportAsGif_Click(object sender, RoutedEventArgs e) {
			try {
				var file = GrfToWpfBridge.Imaging.SaveTo(_editor.Act, _indexSelector.SelectedAction, _entry.RelativePath, PathRequest.ExtractSetting);

				if (file != null)
					Utilities.Debug.Ignore(() => Utilities.Services.OpeningService.FileOrFolder(file));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}