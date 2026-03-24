using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Image;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Tools.SpriteEditor;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.Paths;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for ImageConverter.xaml
	/// </summary>
	public partial class ImageConverter : TkWindow {
		private List<string> _paths = new List<string>();
		private readonly List<PixelFormatInfo> _formats = new List<PixelFormatInfo>();
		private int _previewElementWidth;
		private int _previewElementHeight;
		private int _elementPerLine;
		public bool ThreadEnabled { get; set; }
		private Panel _panel;

		private HashSet<PreviewImageItem> _previewImagesToLoad = new HashSet<PreviewImageItem>();
		private object _loadPreviewLock = new object();
		private readonly AutoResetEvent _are = new AutoResetEvent(false);
		private Dictionary<int, BitmapSource> _images = new Dictionary<int, BitmapSource>();
		private List<PreviewImageItem> _items = new List<PreviewImageItem>();
		private List<string> _previewFiles = new List<string>();
		private Func<string, GrfImage> _imageProvider;
		private bool _pendingVisible;
		private int _uid;

		public ImageConverter() : base("Image converter", "imconvert.ico") {
			InitializeComponent();

			_previewElementWidth = 100;
			_previewElementHeight = 100;

			_formats.AddRange(PixelFormatInfo.Formats);

			_cbFormats.ItemsSource = _formats.Select(p => p.DisplayName + " (*" + p.Extension + ")").ToList();
			_cbFormats.SelectedIndex = 5;
			ShowInTaskbar = true;

			_sv.ScrollChanged += new ScrollChangedEventHandler(_sv_ScrollChanged);
			_sv.SizeChanged += new SizeChangedEventHandler(_sv_SizeChanged);
			GrfThread.Start(_loadPreviewImages, "GRF - Preview images");

			WpfUtilities.AddMouseInOutUnderline(_cbPink);

			this.Dispatcher.ShutdownStarted += delegate {
				ThreadEnabled = false;
			};

			Binder.Bind(_cbPink, () => GrfEditorConfiguration.ImConverterMakePinkTransparent, v => GrfEditorConfiguration.ImConverterMakePinkTransparent = v);
		}

		private void _sv_SizeChanged(object sender, SizeChangedEventArgs e) {
			var minimumHeight = _sv.ActualHeight;
			var targetWidth = _wrapPanel.ActualWidth;

			double width = _previewElementWidth;

			_elementPerLine = Math.Max(1, (int)(targetWidth / _previewElementWidth));
			
			var lineCount = Math.Ceiling((double)_previewFiles.Count / _elementPerLine);

			var targetHeight = lineCount * _previewElementHeight;
			targetHeight = Math.Max(minimumHeight, targetHeight);

			_gridBackground.Height = targetHeight;
			_wrapPanel.Height = (int)(Math.Ceiling(minimumHeight / _previewElementHeight) + 1) * _previewElementHeight;
			//_stackPanel.Height = (int)(Math.Ceiling(minimumHeight / _previewElementHeight) + 1) * _previewElementHeight;

			int elementCount = (int)(Math.Ceiling(minimumHeight / _previewElementHeight) + 1) * _elementPerLine;

			if (_items.Count != elementCount) {
				_wrapPanel.Children.Clear();
				//_stackPanel.Children.Clear();
				_items.Clear();

				for (int i = 0; i < elementCount; i++) {
					var previewItem = new PreviewImageItem();
					previewItem.Width = width;
					previewItem.Height = _previewElementHeight;
					_items.Add(previewItem);
					_stretchCheck(_items[i]);
					_wrapPanel.Children.Add(previewItem);
				}

				_refreshPanelFrames();
			}

			for (int i = 0; i < _items.Count; i++) {
				_items[i].Width = width;
				_items[i].Height = _previewElementHeight;
				_stretchCheck(_items[i]);
			}
		}

		private void _sv_ScrollChanged(object sender, ScrollChangedEventArgs e) {
			var point = _wrapPanel.TranslatePoint(new Point(0, 0), _gridBackground);

			if ((point.Y + _previewElementHeight < _sv.ContentVerticalOffset) ||
				(point.Y > _sv.ContentVerticalOffset)) {
				_wrapPanel.Margin = new Thickness(0, (int)(_sv.ContentVerticalOffset / _previewElementHeight) * _previewElementHeight, 0, 0);
				//_stackPanel.Margin = new Thickness(0, (int)(_sv.ContentVerticalOffset / _previewElementHeight) * _previewElementHeight, 0, 0);

				// Get new display index range
				_refreshPanelFrames();
			}
		}

		private void _stretchCheck(PreviewImageItem item) {
			item._image.VerticalAlignment = VerticalAlignment.Stretch;
			item._image.HorizontalAlignment = HorizontalAlignment.Stretch;
			item._image.Stretch = Stretch.Uniform;
			item._tbName.Visibility = GrfEditorConfiguration.PreviewSpritesShowNames ? Visibility.Visible : Visibility.Collapsed;
		}

		protected override void OnClosed(EventArgs e) {
			ThreadEnabled = false;
			base.OnClosed(e);
		}

		private void _refreshPanelFrames() {
			var start = (int)((_wrapPanel.Margin.Top / _previewElementHeight) * _elementPerLine);

			for (int i = 0; i < _items.Count; i++) {
				int nI = start + i;

				_setImage(_items[i], nI);
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
				previewItem._tbName.Text = Path.GetFileName(_previewFiles[nI]);
			}
			else {
				_pushPreview(previewItem);
				previewItem._tbName.Text = Path.GetFileName(_previewFiles[nI]);
			}
		}

		private void _pushPreview(PreviewImageItem item) {
			item._image.Source = ApplicationManager.PreloadResourceImage("mapEditor.png");

			lock (_loadPreviewLock) {
				_previewImagesToLoad.Add(item);
			}

			_are.Set();
		}

		private void _buttonSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				if (_cbFormats.SelectedIndex < 0)
					throw new Exception("No output format selected.");

				if (_paths.Count == 0) {
					return;
				}

				int selectedFormat = _cbFormats.SelectedIndex;
				var format = _formats[selectedFormat];
				var path = PathRequest.FolderExtract();

				if (path != null) {
					bool pinkTransparent = GrfEditorConfiguration.ImConverterMakePinkTransparent;
					float progress = 0;
					int total = 0;

					if (_paths.Count > 20) {
						TaskManager.DisplayTaskC("Image converter", "Converting...", () => progress, (dialog, cancelToken) => {
							try {
								foreach (var file in _paths) {
									total++;

									if (cancelToken())
										return;

									var image = new GrfImage(file);

									if (pinkTransparent) {
										image.MakePinkShadeTransparent();
									}

									image.Save(GrfPath.Combine(path, Path.GetFileNameWithoutExtension(file) + format.Extension), format);
									progress = (float)total / _paths.Count * 100f;

									dialog.SetUpdate(Path.GetFileName(file));
								}
							}
							catch (OperationCanceledException) {

							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
							}
						});
					}
					else {
						foreach (var file in _paths) {
							var image = new GrfImage(file);

							if (pinkTransparent) {
								image.MakePinkShadeTransparent();
							}

							image.Save(GrfPath.Combine(path, Path.GetFileNameWithoutExtension(file) + format.Extension), format);
						}

						progress = 100.0f;
					}

					try {
						if (progress >= 100.0f)
							Utilities.Services.OpeningService.OpenFolder(path);
					}
					catch { }
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Update() {
			Update(false);
		}

		public void Update(bool clearImages) {
			_labelDrop.Visibility = Visibility.Collapsed;

			if (_paths.Count == 0) {
				_labelDrop.Visibility = Visibility.Visible;
			}
			else {
				_buttonSaveAs.IsEnabled = true;
			}

			_previewElementWidth = 100;
			_previewElementHeight = 100;
			_uid++;

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
				
				_images.Clear();
				_previewFiles.Clear();
				_previewFiles.AddRange(_paths);
			}

			_imageProvider = ImageProvider.GetFirstImage;

			this.Dispatch(delegate {
				_wrapPanel.Visibility = Visibility.Visible;
				_panel = _wrapPanel;

				_sv_SizeChanged(null, null);
				_refreshPanelFrames();
			});

			Resume();
		}

		public void Resume() {
			_are.Set();
		}

		private void _buttonBrowse_Click(object sender, RoutedEventArgs e) {
			try {
				var paths = TkPathRequest.OpenFiles<SpriteEditorConfiguration>("AppLastPath", "filter", "Image Files|*.bmp;*.png;*.jpg;*.tga|Bitmap Files|*.bmp|PNG Files|*.png|Jpeg Files|*.jpg|Targa Files|*.tga") ?? new string[] { };

				_paths.Clear();
				_paths.AddRange(paths);

				Update(true);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _loadPreviewImages() {
			ThreadEnabled = true;
			int previousHash = -1;

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
						previousHash = _uid;
						previewItemsToLoad = _previewImagesToLoad.ToList();
						previewItemIndexes = previewItemsToLoad.Select(p => p.PreviewIndex).ToList();
						_previewImagesToLoad.Clear();
					}

					if (previousHash == -1)
						continue;

					for (int i = 0; i < previewItemsToLoad.Count; i++) {
						var previewItem = previewItemsToLoad[i];
						var previewItemIndex = previewItemIndexes[i];

						if (_uid != previousHash)
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

						if (_uid != previousHash)
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

						if (_uid != previousHash)
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

		private void _scrollViewer_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null) {
						_paths = files.Where(p => p.IsExtension(".jpg", ".bmp", ".png", ".tga")).ToList();
						Update(true);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}