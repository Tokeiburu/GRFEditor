using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GRF.Core;
using GRFEditor.WPF.PreviewTabs;
using Utilities;

namespace GRFEditor.Core.Services {
	public partial class PreviewService {
		private readonly List<int> _lastTabTools = new List<int>();
		private readonly Dictionary<string, int> _preferredOptions = new Dictionary<string, int>();
		private readonly Dictionary<string, int> _preferredTools = new Dictionary<string, int>();
		private readonly List<int> _tabTools = new List<int>();
		private TabItem _tabSpritesPreview;
		private TabItem _tabContainerPreview;
		private TabItem _tabFolderPreview;
		private TabItem _tabFolderStructurePreview;
		private TabItem _tabItemActPreview;
		private TabItem _tabItemDbPreview;
		private TabItem _tabItemEditSpritePreview;
		private TabItem _tabItemGndPreview;
		private TabItem _tabItemGrfPropertiesPreview;
		private TabItem _tabItemImagePreview;
		private TabItem _tabItemLubPreview;
		private TabItem _tabItemMapExtractorPreview;
		private TabItem _tabItemMapGatPreview;
		private TabItem _tabItemRawStructurePreview;
		private TabItem _tabItemResourcePreview;
		private TabItem _tabItemRsmPreview;
		private TabItem _tabItemStrPreview;
		private TabItem _tabItemTextPreview;
		private TabItem _tabItemWavPreview;

		private void _readAsFolder(string path) {
			_tabFolderPreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabFolderPreview.Content == null) _tabFolderPreview.Content = new PreviewFolderItems(_treeView, _items, _previewItems);
				((IFolderPreviewTab) _tabFolderPreview.Content).Load(_grfData, new TkPath { FilePath = _grfData.FileName, RelativePath = path });
			}));
		}

		private void _readAsContainer(string path) {
			_tabContainerPreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabContainerPreview.Content == null) _tabContainerPreview.Content = new PreviewContainer(_previewItems, _editor);
				((IFolderPreviewTab) _tabContainerPreview.Content).Load(_grfData, new TkPath { FilePath = _grfData.FileName, RelativePath = path });
			}));
		}

		private void _readAsPreviewSprites(string path) {
			_tabSpritesPreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabSpritesPreview.Content == null) _tabSpritesPreview.Content = new PreviewSprites(_previewItems, _editor);
				((IFolderPreviewTab)_tabSpritesPreview.Content).Load(_grfData, new TkPath { FilePath = _grfData.FileName, RelativePath = path });
			}));
		}
		
		private void _readAsFolderStructure(string path) {
			_tabFolderStructurePreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabFolderStructurePreview.Content == null) _tabFolderStructurePreview.Content = new PreviewFolderStructure(_previewItems);
				((IFolderPreviewTab) _tabFolderStructurePreview.Content).Load(_grfData, new TkPath { FilePath = _grfData.FileName, RelativePath = path });
			}));
		}

		private void _readAsDecompilationSettings(FileEntry node) {
			_tabItemLubPreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabItemLubPreview.Content == null) _tabItemLubPreview.Content = new DecompilerSettings(this);
				((FilePreviewTab) _tabItemLubPreview.Content).Load(_grfData, node);
			}));
		}

		private void _readAsTxt(FileEntry node) {
			_readAs<PreviewText>(node, _tabItemTextPreview);
		}

		private void _readAsRawStructure(FileEntry node) {
			_readAs<PreviewRawStructure>(node, _tabItemRawStructurePreview);
		}

		private void _readAsMapFile(FileEntry node) {
			_readAs<PreviewMapExtractor>(node, _tabItemMapExtractorPreview);
		}

		private void _readAsMapGat(FileEntry node) {
			_tabItemMapGatPreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabItemMapGatPreview.Content == null) _tabItemMapGatPreview.Content = new PreviewMapGat(_editor);
				((FilePreviewTab)_tabItemMapGatPreview.Content).Load(_grfData, node);
			}));
		}

		private void _readAsProperties(FileEntry node) {
			_readAs<PreviewGRFProperties>(node, _tabItemGrfPropertiesPreview);
		}

		private void _readAsResources(FileEntry node) {
			_readAs<PreviewResource>(node, _tabItemResourcePreview);
		}

		private void _readAsImage(FileEntry node) {
			_readAs<PreviewImage>(node, _tabItemImagePreview);
		}

		private void _readAsDb(FileEntry node) {
			_readAs<PreviewThumbDb>(node, _tabItemDbPreview);
		}

		private void _readAsSound(FileEntry node) {
			_readAs<PreviewWav>(node, _tabItemWavPreview);
		}

		private void _readAsRsm(FileEntry node) {
			_readAs<PreviewRsm>(node, _tabItemRsmPreview);
		}

		private void _readAsStr(FileEntry node) {
			_readAs<PreviewStr>(node, _tabItemStrPreview);
		}

		private void _readAsGnd(FileEntry node) {
			_readAs<PreviewGnd>(node, _tabItemGndPreview);
		}

		private void _readAsEditSprite(FileEntry node) {
			_tabItemEditSpritePreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabItemEditSpritePreview.Content == null) _tabItemEditSpritePreview.Content = new PreviewEditSprite(this);
				((FilePreviewTab) _tabItemEditSpritePreview.Content).Load(_grfData, node);
			}));
		}

		private void _readAsAnimation(FileEntry node) {
			_tabItemActPreview.Dispatcher.BeginInvoke(new Action(delegate {
				if (_tabItemActPreview.Content == null) _tabItemActPreview.Content = new PreviewAct(_asyncOperation);
				((FilePreviewTab) _tabItemActPreview.Content).Load(_grfData, node);
			}));
		}

		private void _readAs<T>(FileEntry node, TabItem tab) where T : FilePreviewTab, new() {
			tab.Dispatcher.BeginInvoke(new Action(delegate {
				if (tab.Content == null) tab.Content = new T();
				((FilePreviewTab) tab.Content).Load(_grfData, node);
			}));
		}

		private void _loadTabItems() {
			Style tabStyle = (Style) Application.Current.FindResource("TabItemStyled");

			_tabItemActPreview = new TabItem { Header = "Animation preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemTextPreview = new TabItem { Header = "Text file editor", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemGrfPropertiesPreview = new TabItem { Header = "Grf entry properties", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemRawStructurePreview = new TabItem { Header = "File info", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemMapExtractorPreview = new TabItem { Header = "Extract resources", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemMapGatPreview = new TabItem { Header = "Minimap preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemResourcePreview = new TabItem { Header = "Resource preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemImagePreview = new TabItem { Header = "Image preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemDbPreview = new TabItem { Header = "Thumbnail preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemEditSpritePreview = new TabItem { Header = "Sprite editor", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabContainerPreview = new TabItem { Header = "Container options", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabSpritesPreview = new TabItem { Header = "Sprites preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabFolderPreview = new TabItem { Header = "Directory info", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabFolderStructurePreview = new TabItem { Header = "Directory view", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemRsmPreview = new TabItem { Header = "Model preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemStrPreview = new TabItem { Header = "Str preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemWavPreview = new TabItem { Header = "Sound preview", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemLubPreview = new TabItem { Header = "Decompiler", Style = tabStyle, Visibility = Visibility.Collapsed };
			_tabItemGndPreview = new TabItem { Header = "Map preview", Style = tabStyle, Visibility = Visibility.Collapsed };

			_tabControlPreview.Items.Add(_tabItemWavPreview);
			_tabControlPreview.Items.Add(_tabFolderStructurePreview);
			_tabControlPreview.Items.Add(_tabContainerPreview);
			_tabControlPreview.Items.Add(_tabSpritesPreview);
			_tabControlPreview.Items.Add(_tabFolderPreview);


			_tabControlPreview.Items.Add(_tabItemStrPreview);
			_tabControlPreview.Items.Add(_tabItemActPreview);
			_tabControlPreview.Items.Add(_tabItemImagePreview);
			_tabControlPreview.Items.Add(_tabItemEditSpritePreview);
			_tabControlPreview.Items.Add(_tabItemDbPreview);
			_tabControlPreview.Items.Add(_tabItemResourcePreview);
			_tabControlPreview.Items.Add(_tabItemMapGatPreview);

			_tabControlPreview.Items.Add(_tabItemRsmPreview);
			_tabControlPreview.Items.Add(_tabItemGndPreview);

			_tabControlPreview.Items.Add(_tabItemMapExtractorPreview);
			_tabControlPreview.Items.Add(_tabItemTextPreview);
			_tabControlPreview.Items.Add(_tabItemRawStructurePreview);
			_tabControlPreview.Items.Add(_tabItemLubPreview);
			_tabControlPreview.Items.Add(_tabItemGrfPropertiesPreview);

			//_tabTools.Add(_tabControlPreview.Items.IndexOf(_tabItemRawStructurePreview));
			_tabTools.Add(_tabControlPreview.Items.IndexOf(_tabItemGrfPropertiesPreview));
			_tabTools.Add(_tabControlPreview.Items.IndexOf(_tabItemMapExtractorPreview));
			_tabTools.Add(_tabControlPreview.Items.IndexOf(_tabItemEditSpritePreview));

			_preferredOptions[".spr"] = _tabControlPreview.Items.IndexOf(_tabItemImagePreview);
			_preferredOptions[".act"] = _tabControlPreview.Items.IndexOf(_tabItemActPreview);
			_preferredOptions[".gat"] = _tabControlPreview.Items.IndexOf(_tabItemMapGatPreview);
			_preferredOptions[".gnd"] = _tabControlPreview.Items.IndexOf(_tabItemGndPreview);
			_preferredOptions[".rsw"] = _tabControlPreview.Items.IndexOf(_tabItemRsmPreview);

			_tabControlPreview.SelectionChanged += new SelectionChangedEventHandler(_tabControlPreview_SelectionChanged);
			_hasBeenLoaded = true;
		}

		private void _tabControlPreview_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (e == null)
				return;

			if (e.RemovedItems.Count <= 0 || e.RemovedItems[0] as TabItem == null || (e.AddedItems.Count > 0 && e.AddedItems[0] as TabItem == null))
				return;

			if (!(((TabItem) e.RemovedItems[0]).Content is FilePreviewTab || ((TabItem) e.RemovedItems[0]).Content is IFolderPreviewTab || ((TabItem) e.RemovedItems[0]).Content is FilePreviewTab))
				return;

			if (e.AddedItems.Count > 0 && !(((TabItem) e.AddedItems[0]).Content is FilePreviewTab || ((TabItem) e.AddedItems[0]).Content is IFolderPreviewTab || ((TabItem) e.AddedItems[0]).Content is FilePreviewTab))
				return;

			if (!_previewItem.IsNull()) {
				string extension = _previewItem.Extension ?? "";
				int selectedIndex = _tabControlPreview.SelectedIndex;

				if (_tabTools.Contains(selectedIndex)) {
					_lastTabTools.Remove(selectedIndex);
					_lastTabTools.Add(selectedIndex);

					if (_preferredTools.ContainsKey(extension)) {
						_preferredTools[extension] = selectedIndex;
					}
					else {
						_preferredTools.Add(extension, selectedIndex);
					}
				}
				else {
					if (_preferredTools.ContainsKey(extension)) {
						List<string> extensions = _preferredTools.Where(p => p.Value == _preferredTools[extension]).Select(p => p.Key).ToList();

						_lastTabTools.Remove(_preferredTools[extension]);

						foreach (string ext in extensions) {
							_preferredTools.Remove(ext);
						}
					}

					_preferredTools.Remove(extension);
					_preferredOptions[extension] = _tabControlPreview.SelectedIndex;
				}
			}

			((IPreviewTab) ((TabItem) e.AddedItems[0]).Content).Update();
		}

		public void InvalidateAllVisiblePreviewTabs(GrfHolder grf) {
			foreach (TabItem tabItem in _tabControlPreview.Items) {
				if (tabItem.Content is FilePreviewTab && tabItem.Visibility == Visibility.Visible) {
					((FilePreviewTab) tabItem.Content).InvalidateOnReload(grf);
				}
			}
		}
	}
}