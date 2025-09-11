using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ErrorManager;
using GRF;
using GRF.Core.GroupedGrf;
using TokeiLibrary;
using TokeiLibrary.Paths;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;
using Utilities.Services;
using DataFormats = System.Windows.DataFormats;
using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;
using ListViewItem = System.Windows.Controls.ListViewItem;
using UserControl = System.Windows.Controls.UserControl;

namespace GrfToWpfBridge.MultiGrf {
	/// <summary>
	/// Interaction logic for MetaGrfResourcesViewer.xaml
	/// </summary>
	public partial class MetaGrfResourcesViewer : UserControl {
		#region Delegates

		public delegate void MgrvEventHandler(object sender);

		#endregion

		private readonly ObservableCollection<MultiGrfPathView> _itemsResourcesSource = new ObservableCollection<MultiGrfPathView>();
		private bool _canDeleteMainGrf = true;

		public MetaGrfResourcesViewer() {
			InitializeComponent();

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_itemsResources, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "", DisplayExpression = "DataImage", FixedWidth = 30, MaxHeight = 60 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "Path", DisplayExpression = "DisplayFileName", FixedWidth = 100, ToolTipBinding = "DisplayFileName", TextAlignment = TextAlignment.Left, IsFill = true }
			}, null, new string[] { "FileNotFound", "Red" });

			_itemsResources.ItemsSource = _itemsResourcesSource;
			_loadResourcesInfo();
			WpfUtils.AddDragDropEffects(_itemsResources);

			ApplicationShortcut.Link(ApplicationShortcut.Delete, "MultiGrf.Delete", () => _menuItemsDelete_Click(null, null), _itemsResources);
			ApplicationShortcut.Link(ApplicationShortcut.Confirm, "MultiGrf.Select in explorer", () => _menuItemsSelectInExplorer_Click(null, null), _itemsResources);

			Setting = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Application latest file name]"] = v.ToString(); }, () => Configuration.ConfigAsker["[GRFEditor - Application latest file name]", Configuration.ApplicationPath]);
		}

		public Setting Setting { get; set; }

		public List<MultiGrfPath> Paths {
			get { return _itemsResources.Items.Cast<MultiGrfPathView>().Select(p => p.Resource).ToList(); }
		}

		public Action<string> SaveResourceMethod { get; set; }
		public Func<List<MultiGrfPath>> LoadResourceMethod { get; set; }

		public bool CanDeleteMainGrf {
			get { return _canDeleteMainGrf; }
			set { _canDeleteMainGrf = value; }
		}

		public event MgrvEventHandler Modified;

		public void OnModified() {
			MgrvEventHandler handler = Modified;
			if (handler != null) handler(this);
		}

		private void _itemsResources_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e) {
			try {
				ListViewItem lvi = _itemsResources.GetObjectAtPoint<ListViewItem>(e.GetPosition(_itemsResources));

				if (lvi != null) {
					_menuItemsDelete.IsEnabled = true;
					_menuItemsMoveUp.IsEnabled = true;
					_menuItemsMoveDown.IsEnabled = true;
					_menuItemsSelectInExplorer.IsEnabled = true;
					_menuItemsAdd.IsEnabled = true;

					var view = lvi.Content as MultiGrfPathView;

					if (view != null) {
						if (!CanDeleteMainGrf && view.Resource.IsCurrentlyLoadedGrf) {
							_menuItemsDelete.IsEnabled = false;
						}
					}
				}
				else {
					_menuItemsDelete.IsEnabled = false;
					_menuItemsMoveUp.IsEnabled = false;
					_menuItemsMoveDown.IsEnabled = false;
					_menuItemsSelectInExplorer.IsEnabled = false;
					_menuItemsAdd.IsEnabled = true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _itemsResources_DragEnter(object sender, DragEventArgs e) {
			if (e.Is(DataFormats.FileDrop)) {
				e.Effects = DragDropEffects.Copy;
			}
		}

		private void _itemsResources_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Is(DataFormats.FileDrop)) {
					string[] files = e.Get<string[]>(DataFormats.FileDrop);

					foreach (string file in files) {
						_itemsResourcesSource.Add(new MultiGrfPathView(new MultiGrfPath(file)));
					}

					_saveResourcesInfo();
					OnModified();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				e.Handled = true;
			}
		}

		public void _menuItemsMoveDown_Click(object sender, RoutedEventArgs e) {
			try {
				if (_itemsResources.SelectedItem != null) {
					MultiGrfPathView rme = (MultiGrfPathView) _itemsResources.SelectedItem;

					if (_itemsResourcesSource.Count <= 1)
						return;

					int index = _getIndex(rme);

					if (index < _itemsResourcesSource.Count - 1) {
						MultiGrfPathView old = _itemsResourcesSource[index + 1];
						_itemsResourcesSource.RemoveAt(index + 1);
						_itemsResourcesSource.Insert(index, old);

						_saveResourcesInfo();
						OnModified();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void _menuItemsMoveUp_Click(object sender, RoutedEventArgs e) {
			try {
				if (_itemsResources.SelectedItem != null) {
					MultiGrfPathView rme = (MultiGrfPathView) _itemsResources.SelectedItem;

					if (_itemsResourcesSource.Count <= 1)
						return;

					int index = _getIndex(rme);

					if (index > 0) {
						MultiGrfPathView old = _itemsResourcesSource[index - 1];
						_itemsResourcesSource.RemoveAt(index - 1);
						_itemsResourcesSource.Insert(index, old);

						_saveResourcesInfo();
						OnModified();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void _menuItemsAdd_Click(object sender, RoutedEventArgs e) {
			try {
				string[] paths = TkPathRequest.OpenFiles(Setting, "filter", "Container Files|*.grf;*.gpf;*.thor");

				if (paths != null && paths.Length > 0) {
					foreach (string file in paths) {
						_itemsResourcesSource.Add(new MultiGrfPathView(new MultiGrfPath(file)));
					}

					_saveResourcesInfo();
					OnModified();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void _menuItemsSelectInExplorer_Click(object sender, RoutedEventArgs e) {
			try {
				if (_itemsResources.SelectedItem != null) {
					MultiGrfPathView rme = (MultiGrfPathView) _itemsResources.SelectedItem;

					OpeningService.FilesOrFolders(new string[] { rme.Resource.Path });
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void _menuItemsDelete_Click(object sender, RoutedEventArgs e) {
			try {
				int deletedCount = 0;

				for (int index = 0; index < _itemsResources.SelectedItems.Count; index++) {
					MultiGrfPathView rme = (MultiGrfPathView) _itemsResources.SelectedItems[index];

					if (!CanDeleteMainGrf && rme.Resource.IsCurrentlyLoadedGrf) {
						continue;
					}

					_itemsResourcesSource.Remove(rme);
					index--;
					deletedCount++;
				}

				if (deletedCount > 0) {
					_saveResourcesInfo();
					OnModified();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _saveResourcesInfo() {
			if (SaveResourceMethod != null) {
				// From configuration is always saved
				// If not from either, then it was manually added
				var t = _itemsResourcesSource.Where(p => p.Resource.FromConfiguration || (!p.Resource.FromConfiguration && !p.Resource.IsCurrentlyLoadedGrf)).Select(p => p.Resource.Path).ToList();
				SaveResourceMethod(Methods.ListToString(t));
				LoadResourcesInfo();
			}
		}

		private int _getIndex(MultiGrfPathView rme) {
			for (int i = 0; i < _itemsResourcesSource.Count; i++) {
				if (_itemsResourcesSource[i] == rme)
					return i;
			}

			return -1;
		}

		public void LoadResourcesInfo() {
			_loadResourcesInfo();
		}

		private void _loadResourcesInfo() {
			this.Dispatch(delegate {
				try {
					if (LoadResourceMethod == null) return;

					bool needsVisualReload = false;

					var resources = LoadResourceMethod();

					if (resources.Count == _itemsResourcesSource.Count) {
						for (int index = 0; index < resources.Count; index++) {
							string resourcePath = resources[index].Path;
							if (_itemsResourcesSource[index].Resource.Path != resourcePath) {
								needsVisualReload = true;
								break;
							}
						}
					}
					else {
						needsVisualReload = true;
					}

					if (needsVisualReload) {
						_itemsResourcesSource.Clear();

						foreach (var resourcePath in LoadResourceMethod()) {
							_itemsResourcesSource.Add(new MultiGrfPathView(resourcePath));
						}

						OnModified();
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
		}
	}
}