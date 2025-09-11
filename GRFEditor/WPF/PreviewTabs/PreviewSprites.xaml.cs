using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.Image;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewContainer.xaml
	/// </summary>
	public partial class PreviewSprites : UserControl, IFolderPreviewTab {
		private readonly EditorMainWindow _editor;
		private int _previewElementWidth;
		private int _previewElementHeight;
		private int _elementPerLine;
		public bool ThreadEnabled { get; set; }
		private Panel _panel;

		private TkPath _currentPath;
		private GrfHolder _grfData;
		private string[] _previewFileExtensions = new string[] { ".spr", ".bmp", ".png", ".gat" };

		public PreviewSprites(EditorMainWindow editor) {
			_editor = editor;

			_previewElementWidth = 100;
			_previewElementHeight = 100 + (GrfEditorConfiguration.PreviewSpritesShowNames ? 20 : 0);

			InitializeComponent();

			Binder.Bind(_cbWrapImages, () => GrfEditorConfiguration.PreviewSpritesWrap, delegate {
				if (_currentPath != null)
					Update(true);
			}, true);

			if (GrfEditorConfiguration.PreviewSpritesWrap) {
				_stackPanel.Visibility = Visibility.Collapsed;
				_wrapPanel.Visibility = Visibility.Visible;
			}
			else {
				_stackPanel.Visibility = Visibility.Visible;
				_wrapPanel.Visibility = Visibility.Collapsed;
			}

			Binder.Bind(_cbShowNames, () => GrfEditorConfiguration.PreviewSpritesShowNames, delegate {
				if (_currentPath != null) {
					var prev = _sv.ContentVerticalOffset / _sv.ScrollableHeight;
					Update(false);

					if (double.IsNaN(prev))
						return;

					_sv.ScrollToVerticalOffset(prev * (_gridBackground.Height - _sv.ActualHeight));
				}
			}, true);

			WpfUtils.AddMouseInOutEffectsBox(_cbWrapImages, _cbShowNames);

			_gpEase.SetPosition(0.2d, false);

			_gpEase.ValueChanged += delegate {
				var value = Math.Round(_gpEase.Position * 10);
				_gpEase.SetPosition(value / 10d, true);

				int size = (int)value * 20 + 60;

				if (_previewElementWidth == size)
					return;

				var prev = _sv.ContentVerticalOffset / _sv.ScrollableHeight;
				Update(false);

				if (double.IsNaN(prev))
					return;

				_sv.ScrollToVerticalOffset(prev * (_gridBackground.Height - _sv.ActualHeight));
			};

			_sv.ScrollChanged += new ScrollChangedEventHandler(_sv_ScrollChanged);
			_sv.SizeChanged += new SizeChangedEventHandler(_sv_SizeChanged);
			GrfThread.Start(_loadPreviewImages, "GRF - Preview images");

			this.Dispatcher.ShutdownStarted += delegate {
				ThreadEnabled = false;
			};
		}

		private void _refreshPanelFrames() {
			var start = (int)((_wrapPanel.Margin.Top / _previewElementHeight) * _elementPerLine);

			for (int i = 0; i < _items.Count; i++) {
				int nI = start + i;

				_setImage(_items[i], nI);
			}
		}

		#region IFolderPreviewTab Members

		public void Update() {
			Update(false);
		}

		public void Update(bool clearImages) {
			int size = (int)(_gpEase.Position * 10) * 20 + 60;
			_previewElementWidth = size;
			_previewElementHeight = size + (GrfEditorConfiguration.PreviewSpritesShowNames ? 20 : 0);

			if (!clearImages && _pendingVisible) {
				clearImages = true;
			}

			_pendingVisible = false;

			if (clearImages) {
				this.Dispatch(delegate {
					for (int i = 0; i < _items.Count; i++) {
						_items[i]._tbName.Text = "";
						_items[i]._image.Source = null;
						_stretchCheck(_items[i]);
					}
				});

				List<FileEntry> previews = new List<FileEntry>();

				foreach (var entry in _grfData.FileTable.EntriesInDirectory(_currentPath.RelativePath, SearchOption.TopDirectoryOnly, GrfEditorConfiguration.GrfFileTableIgnoreCase)) {
					if (entry.RelativePath.IsExtension(_previewFileExtensions)) {
						previews.Add(entry);
					}
				}

				_images.Clear();
				_previewFiles.Clear();
				_previewFiles.AddRange(previews);
			}

			if (GrfEditorConfiguration.PreviewSpritesWrap) {
				_imageProvider = ImageProvider.GetFirstImage;
			}
			else {
				_imageProvider = ImageProvider.GetImage;
			}

			this.Dispatch(delegate {
				if (GrfEditorConfiguration.PreviewSpritesWrap) {
					_stackPanel.Visibility = Visibility.Hidden;
					_wrapPanel.Visibility = Visibility.Visible;
					_panel = _wrapPanel;
				}
				else {
					_stackPanel.Visibility = Visibility.Visible;
					_wrapPanel.Visibility = Visibility.Hidden;
					_panel = _stackPanel;
				}

				_sv_SizeChanged(null, null);
				_refreshPanelFrames();
			});

			Resume();

			_sv.MouseDoubleClick += new MouseButtonEventHandler(_previewSprites_MouseDoubleClick);
		}

		public void Load(GrfHolder grfData, TkPath currentPath) {
			_grfData = grfData;

			if (_currentPath != null && currentPath != null && _currentPath.GetFullPath() == currentPath.GetFullPath())
				return;

			_currentPath = currentPath;
			_pendingVisible = true;

			if (IsVisible) {
				Update(true);
			}
		}

		#endregion

		private HashSet<PreviewImageItem> _previewImagesToLoad = new HashSet<PreviewImageItem>();
		private object _loadPreviewLock = new object();
		private readonly AutoResetEvent _are = new AutoResetEvent(false);
		private Dictionary<int, BitmapSource> _images = new Dictionary<int, BitmapSource>();
		private List<PreviewImageItem> _items = new List<PreviewImageItem>();
		private List<FileEntry> _previewFiles = new List<FileEntry>();
		private Func<FileEntry, GrfImage> _imageProvider;
		private bool _pendingVisible;

		public void Resume() {
			_are.Set();
		}

		private void _pushPreview(PreviewImageItem item) {
			item._image.Source = ApplicationManager.PreloadResourceImage("mapEditor.png");

			lock (_loadPreviewLock) {
				_previewImagesToLoad.Add(item);
			}

			_are.Set();
		}

		private void _loadPreviewImages() {
			ThreadEnabled = true;
			TkPath previousHash = null;

			while (ThreadEnabled) {
				try {
					int count;

					lock (_loadPreviewLock) {
						count = _previewImagesToLoad.Count;
					}

					if (count == 0)
						_are.WaitOne();

					List<PreviewImageItem> previewItemsToLoad;
					List<int> previewItemIndexes;

					lock (_loadPreviewLock) {
						if (previousHash != null && previousHash.GetFullPath() != _currentPath.GetFullPath()) {
							//_images.Clear();
						}

						previousHash = _currentPath;
						previewItemsToLoad = _previewImagesToLoad.ToList();
						previewItemIndexes = previewItemsToLoad.Select(p => p.PreviewIndex).ToList();
						_previewImagesToLoad.Clear();
					}

					if (previousHash == null)
						continue;

					for (int i = 0; i < previewItemsToLoad.Count; i++) {
						var previewItem = previewItemsToLoad[i];
						var previewItemIndex = previewItemIndexes[i];

						if (_currentPath.GetFullPath() != previousHash.GetFullPath())
							break;

						if (previewItem.PreviewIndex >= _previewFiles.Count) {
							this.Dispatch(delegate {
								try {
									previewItem._image.Source = null;
								}
								catch {
								}
							});
						}

						if (previewItem.PreviewIndex >= _previewFiles.Count)
							continue;

						if (_currentPath.GetFullPath() != previousHash.GetFullPath())
							continue;

						if (previewItem.PreviewIndex != previewItemIndex)
							continue;

						if (previewItem.Parent == null)
							continue;

						BitmapSource bitmap = null;

						if (_images.ContainsKey(previewItem.PreviewIndex)) {
							bitmap = _images[previewItem.PreviewIndex];
						}
						else {
							var entry = _previewFiles[previewItemIndex];
							GrfImage image = _imageProvider(entry);

							if (image != null)
								bitmap = image.Cast<BitmapSource>();

							_images[previewItemIndex] = bitmap;
						}

						if (_currentPath.GetFullPath() != previousHash.GetFullPath())
							continue;

						this.Dispatch(delegate {
							try {
								if (previewItem.PreviewIndex != previewItemIndex)
									return;

								previewItem._image.Source = bitmap;
							}
							catch {
								Z.F();
							}
						});
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		private void _sv_SizeChanged(object sender, SizeChangedEventArgs e) {
			var minimumHeight = _sv.ActualHeight;
			var targetWidth = _wrapPanel.ActualWidth;

			double width = _previewElementWidth;

			if (GrfEditorConfiguration.PreviewSpritesWrap) {
				_elementPerLine = Math.Max(1, (int)(targetWidth / _previewElementWidth));
			}
			else {
				width = _gridBackground.ActualWidth;
				_elementPerLine = 1;
			}

			var lineCount = Math.Ceiling((double)_previewFiles.Count / _elementPerLine);

			var targetHeight = lineCount * _previewElementHeight;
			targetHeight = Math.Max(minimumHeight, targetHeight);

			_gridBackground.Height = targetHeight;
			_wrapPanel.Height = (int)(Math.Ceiling(minimumHeight / _previewElementHeight) + 1) * _previewElementHeight;
			_stackPanel.Height = (int)(Math.Ceiling(minimumHeight / _previewElementHeight) + 1) * _previewElementHeight;
			
			int elementCount = (int)(Math.Ceiling(minimumHeight / _previewElementHeight) + 1) * _elementPerLine;

			if (_items.Count != elementCount) {
				_wrapPanel.Children.Clear();
				_stackPanel.Children.Clear();
				_items.Clear();

				for (int i = 0; i < elementCount; i++) {
					var previewItem = new PreviewImageItem();
					previewItem.Width = width;
					previewItem.Height = _previewElementHeight;
					_items.Add(previewItem);
					_stretchCheck(_items[i]);
					_panel.Children.Add(previewItem);
				}

				_refreshPanelFrames();
			}

			for (int i = 0; i < _items.Count; i++) {
				_items[i].Width = width;
				_items[i].Height = _previewElementHeight;
				_stretchCheck(_items[i]);
			}
		}

		private void _stretchCheck(PreviewImageItem item) {
			if (GrfEditorConfiguration.PreviewSpritesWrap) {
				item._image.VerticalAlignment = VerticalAlignment.Stretch;
				item._image.HorizontalAlignment = HorizontalAlignment.Stretch;
				item._image.Stretch = Stretch.Uniform;
			}
			else {
				item._image.VerticalAlignment = VerticalAlignment.Top;
				item._image.HorizontalAlignment = HorizontalAlignment.Left;
				item._image.Stretch = Stretch.None;
			}

			item._tbName.Visibility = GrfEditorConfiguration.PreviewSpritesShowNames ? Visibility.Visible : Visibility.Collapsed;
		}

		private void _sv_ScrollChanged(object sender, ScrollChangedEventArgs e) {
			var point = _wrapPanel.TranslatePoint(new Point(0, 0), _gridBackground);

			if ((point.Y + _previewElementHeight < _sv.ContentVerticalOffset) ||
				(point.Y > _sv.ContentVerticalOffset)) {
				_wrapPanel.Margin = new Thickness(0, (int)(_sv.ContentVerticalOffset / _previewElementHeight) * _previewElementHeight, 0, 0);
				_stackPanel.Margin = new Thickness(0, (int)(_sv.ContentVerticalOffset / _previewElementHeight) * _previewElementHeight, 0, 0);

				// Get new display index range
				_refreshPanelFrames();
			}
		}

		private void _setImage(PreviewImageItem previewItem, int nI) {
			previewItem.PreviewIndex = nI;

			if (nI >= _previewFiles.Count) {
				previewItem._image.Source = null;
				previewItem._tbName.Text = "";
			}
			else if (_images.ContainsKey(nI)) {
				previewItem._image.Source = _images[nI];
				previewItem._tbName.Text = Path.GetFileName(_previewFiles[nI].RelativePath);
			}
			else {
				_pushPreview(previewItem);
				previewItem._tbName.Text = Path.GetFileName(_previewFiles[nI].RelativePath);
			}
		}

		private void _select(FileEntry entry) {
			try {
				PreviewService.Select(_editor._treeView, _editor._items, entry.RelativePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _previewSprites_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			try {
				var item = _panel.GetObjectAtPoint<PreviewImageItem>(e.GetPosition(_panel));

				if (item != null && item.PreviewIndex < this._previewFiles.Count)
					_select(this._previewFiles[item.PreviewIndex]);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}