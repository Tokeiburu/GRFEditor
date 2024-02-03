using System;
using System.Collections.Generic;
using System.Globalization;
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
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GRFEditor.Tools.GrfValidation;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewContainer.xaml
	/// </summary>
	public partial class PreviewSprites : UserControl, IFolderPreviewTab {
		private readonly EditorMainWindow _editor;
		private readonly Dictionary<string, Grid> _grids = new Dictionary<string, Grid>();
		private readonly object _lock = new object();
		private readonly Queue<PreviewItem> _previewItems;
		private readonly RangeObservableCollection<SpritePreviewView> _previews = new RangeObservableCollection<SpritePreviewView>();
		private readonly RangeObservableCollection<LargeSpritePreviewView> _previews2 = new RangeObservableCollection<LargeSpritePreviewView>();
		private TkPath _currentPath;
		private GrfHolder _grfData;
		private string[] _previewFiles = new string[] { ".spr", ".bmp", ".png", ".gat" };

		public PreviewSprites(Queue<PreviewItem> previewItems, EditorMainWindow editor) {
			_previewItems = previewItems;
			_editor = editor;

			InitializeComponent();

			_items.ItemsSource = _previews;

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_items, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", SearchGetAccessor = "RelativePath", MaxHeight = 50, TextAlignment = TextAlignment.Left, NoResize = true, ToolTipBinding = "RelativePath", FixedWidth = -1, IsFill = true },
			}, null, new string[] { "Default", "Black" }, "generateHeader", "false");

			_itemsLarge.ItemsSource = _previews2;

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_itemsLarge, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", MaxHeight = 50, TextAlignment = TextAlignment.Left, NoResize = true, FixedWidth = -1, IsFill = true },
			}, null, new string[] { "Default", "Black" }, "generateHeader", "false");

			Binder.Bind(_cbWrapImages, () => GrfEditorConfiguration.PreviewSpritesWrap, delegate {
				_itemsLarge.Dispatch(p => p.Visibility = GrfEditorConfiguration.PreviewSpritesWrap ? Visibility.Visible : Visibility.Hidden);

				if (_currentPath != null)
					_load(_currentPath);
			}, true);

			Binder.Bind(_cbAuto, () => GrfEditorConfiguration.PreviewSpritesAuto, delegate {
				_cbWrapImages.IsEnabled = _cbAuto.IsChecked == false;
				_tbPerLine.IsEnabled = _cbAuto.IsChecked == false;

				if (_currentPath != null)
					_load(_currentPath);
			}, true);

			_primaryGrid.SizeChanged += new SizeChangedEventHandler(_primaryGrid_SizeChanged);
			WpfUtilities.AddFocus(_tbPerLine);

			Binder.Bind(_tbPerLine, () => GrfEditorConfiguration.PreviewSpritesPerLine, _refreshList);

			WpfUtils.AddMouseInOutEffectsBox(_cbWrapImages, _cbAuto);
		}

		private void _primaryGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
			_refreshList();
		}

		public class LargeSpritePreviewView {
			public bool Default {
				get { return true; }
			}

			public List<SpritePreviewView> Previews = new List<SpritePreviewView>();

			public int SuggestedSize { get; set; }

			public object DataImage {
				get {
					DrawingGroup dGroup = new DrawingGroup();
					List<GrfImage> images = new List<GrfImage>();
					List<int> heights = new List<int>();

					foreach (var item in Previews) {
						GrfImage image = ImageProvider.GetFirstImage(item.Entry);

						if (image.Width > SuggestedSize) {
							heights.Add((int)(image.Height * (float)SuggestedSize / image.Width));
						}
						else {
							heights.Add(image.Height);
						}

						images.Add(image);
					}

					int maxHeight = heights.Max(p => p);

					double left;
					double top;

					using (DrawingContext dc = dGroup.Open()) {
						for (int i = 0; i < Previews.Count; i++) {
							GrfImage image = images[i];

							double width = image.Width;
							double resize = 1;

							if (width > SuggestedSize) {
								resize = (double) SuggestedSize / image.Width;
							}

							left = i * SuggestedSize + SuggestedSize / 2;
							top = maxHeight / 2d;

							if (i == 0) {
								byte[] data = new byte[1 * (int) left * 4];
								GrfImage edge = new GrfImage(data, (int) left, 1, GrfImageType.Bgra32);
								dc.DrawImage(edge.Cast<BitmapSource>(), new Rect(0, 0, edge.Width, edge.Height));
							}

							TransformGroup group = new TransformGroup();
							TranslateTransform translate2 = new TranslateTransform(image.Width / -2d, image.Height / -2d);
							ScaleTransform scale = new ScaleTransform(resize, resize);
							TranslateTransform translate = new TranslateTransform(left, top);

							group.Children.Add(translate2);
							group.Children.Add(scale);
							group.Children.Add(translate);

							dc.PushTransform(group);
							
							dc.DrawImage(image.Cast<BitmapSource>(), new Rect(0, 0, image.Width, image.Height));
							dc.Pop();
						}
					}

					DrawingImage dImage = new DrawingImage(dGroup);
					dImage.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.Linear);
					return dImage;

					//return img.Cast<BitmapSource>();
				}
			}
		}

		public class SpritePreviewView {
			public bool Default {
				get { return true; }
			}

			public FileEntry Entry { get; set; }

			public string RelativePath {
				get { return Entry == null ? "" : Entry.RelativePath; }
			}

			public object DataImage {
				get {
					if (Entry != null) {
						GrfImage image = ImageProvider.GetImage(Entry);
						return image.Cast<BitmapSource>();
					}

					return null;
				}
			}
		}

		private void _refreshList() {
			if (!GrfEditorConfiguration.PreviewSpritesWrap)
				return;

			List<LargeSpritePreviewView> previews2 = new List<LargeSpritePreviewView>();

			int perLine = GrfEditorConfiguration.PreviewSpritesPerLine;

			if (GrfEditorConfiguration.PreviewSpritesAuto) {
				bool imageFolder = _grfData.FileTable.EntriesInDirectory(_currentPath.RelativePath, SearchOption.TopDirectoryOnly).Any(entry => entry.RelativePath.IsExtension(".jpg", ".bmp", ".png"));

				if (imageFolder) {
					perLine = 3;
				}
				else {
					perLine = GrfEditorConfiguration.PreviewSpritesPerLine;
				}
			}

			int suggestedSize = (int)(_primaryGrid.Dispatch(p => p.ActualWidth - 15) / perLine);
			int current = 0;

			if (suggestedSize < 32) {
				suggestedSize = 32;
			}

			foreach (var entry in _grfData.FileTable.EntriesInDirectory(_currentPath.RelativePath, SearchOption.TopDirectoryOnly)) {
				if (entry.RelativePath.IsExtension(_previewFiles)) {
					if (current % perLine == 0) {
						previews2.Add(new LargeSpritePreviewView { SuggestedSize = suggestedSize });
					}

					previews2[current / perLine].Previews.Add(new SpritePreviewView { Entry = entry });
					current++;
				}
			}

			_editor.Dispatch(p => {
				_previews2.Clear();
				_previews2.Disable();
				_previews2.AddRange(previews2);
				_previews2.UpdateAndEnable();
			});
		}

		#region IFolderPreviewTab Members

		public void Update() {
			Thread thread = new Thread(() => _load(_currentPath)) { Name = "GrfEditor - Preview container thread" };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		public void Load(GrfHolder grfData, TkPath currentPath) {
			_currentPath = currentPath;
			_grfData = grfData;

			if (IsVisible) {
				Update();
			}
		}

		#endregion

		private void _load(TkPath currentSearch) {
			try {
				lock (_lock) {
					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					if (!GrfEditorConfiguration.PreviewSpritesWrap) {
						List<SpritePreviewView> previews = new List<SpritePreviewView>();

						foreach (var entry in _grfData.FileTable.EntriesInDirectory(_currentPath.RelativePath, SearchOption.TopDirectoryOnly)) {
							if (entry.RelativePath.IsExtension(_previewFiles)) {
								previews.Add(new SpritePreviewView { Entry = entry });
							}
						}

						_editor.Dispatch(p => {
							_previews.Clear();
							_previews.Disable();
							_previews.AddRange(previews);
							_previews.UpdateAndEnable();
						});
					}

					_refreshList();
					_setVisible();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _setVisible() {
			this.Dispatch(delegate {
			});
		}

		private void _select(SpritePreviewView preview) {
			try {
				PreviewService.Select(_editor._treeView, _editor._items, preview.Entry.RelativePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _miSelect_Click(object sender, RoutedEventArgs e) {
			try {
				var spv = _items.SelectedItem as SpritePreviewView;

				if (spv != null)
					PreviewService.Select(_editor._treeView, _editor._items, spv.Entry.RelativePath);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _items_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			_miSelect_Click(null, null);
		}

		private void _miSelect2_Click(object sender, RoutedEventArgs e) {
			try {
				var lspv = _itemsLarge.SelectedItem as LargeSpritePreviewView;

				if (lspv == null)
					return;

				var pos =  Mouse.GetPosition(_primaryGrid);

				int index = (int)(pos.X / lspv.SuggestedSize);
				if (index >= lspv.Previews.Count)
					index = lspv.Previews.Count - 1;
				_select(lspv.Previews[index]);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _items2_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			try {
				var lspv = _itemsLarge.SelectedItem as LargeSpritePreviewView;

				if (lspv == null)
					return;

				var pos = e.GetPosition(_primaryGrid);

				int index = (int) (pos.X / lspv.SuggestedSize);
				if (index >= lspv.Previews.Count) {
					return;
				}
				_select(lspv.Previews[index]);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}