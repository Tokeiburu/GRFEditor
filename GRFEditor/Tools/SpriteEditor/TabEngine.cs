using ErrorManager;
using GRF.FileFormats.SprFormat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Utilities;
using TokeiLibrary;
using GRF.GrfSystem;
using System.Windows.Input;
using Utilities.Services;
using GRF.FileFormats.ActFormat.Commands;

namespace GRFEditor.Tools.SpriteEditor {
	public class TabIndex {
		public TabItem Tab;
		public SpriteEditorControl Control;

		public TabIndex(TabItem tab, SpriteEditorControl control) {
			Tab = tab;
			Control = control;
		}

		public TabIndex(TabItem tab) {
			Tab = tab;
			Control = tab.Content as SpriteEditorControl;
		}
	};

	public class TabEngine {
		private TabControl _tabControl;
		private SpriteConverter _editor;
		private SprLoaderService _sprLoadService = new SprLoaderService();
		private PaletteOperations _paletteOperations;
		private int _lastTabSelected = -1;

		public TabEngine(TabControl tabControl, SpriteConverter editor) {
			_tabControl = tabControl;
			_editor = editor;
			_paletteOperations = new PaletteOperations(editor);
			_tabControl.SelectionChanged += _tabControl_SelectionChanged; ;
		}

		public void CloseTab(TabItem tab = null) {
			try {
				SpriteEditorControl control = null;

				if (tab == null) {
					var index = GetCurrentTabIndex();
					
					// Tab is already closed?
					if (index == null)
						return;

					control = index.Control;
					tab = index.Tab;
				}
				else {
					control = tab.Content as SpriteEditorControl;
				}

				if (!control.Close())
					return;

				_tabControl.Items.Remove(tab);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ExportAll() => SafeExecute(control => control.ExportAll());

		public void New() {
			try {
				// A bit silly to use a temporary file for this, but a physical file is needed.
				var fileName = TemporaryFilesManager.GetTemporaryFilePath("new_{0:0000}");
				Spr spr = new Spr();
				spr.Save(fileName);
				Open(fileName, focusTab: true, isNew: true);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void Open(TkPath file, bool focusTab = true, bool isNew = false) {
			try {
				var index = TabExists(file);

				if (index != null) {
					_tabControl.SelectedItem = index.Tab;
					return;
				}

				var result = _sprLoadService.Load(file);

				if (result.AddToRecentFiles)
					_editor.RecentFiles.AddRecentFile(result.FilePath);
				if (result.RemoveToRecentFiles)
					_editor.RecentFiles.RemoveRecentFile(result.FilePath);
				if (result.ErrorMessage != null)
					ErrorHandler.HandleException(result.ErrorMessage);
				if (!result.Success)
					return;

				index = CreateTab(result.LoadedSpr, isNew);

				if (focusTab)
					Focus(index.Tab);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void OpenFiles(IEnumerable<string> files) {
			if (files == null)
				return;

			foreach (string file in files.Where(p => p.EndsWith(".spr", StringComparison.OrdinalIgnoreCase))) {
				Open(file);
			}
		}

		public void Focus(TabItem tab) {
			tab.IsSelected = true;
		}

		public TabIndex CreateTab(Spr spr, bool isNew = false) {
			TabItem tabItem = new TabItem();

			SpriteEditorControl control = new SpriteEditorControl();
			control.IsNew = isNew;
			control.Load(spr, spr.LoadedPath);

			tabItem.Content = control;

			tabItem.Style = _editor.FindResource("TabItemSprite") as Style;
			tabItem.Header = Path.GetFileNameWithoutExtension(new TkPath(spr.LoadedPath).FileName);

			_tabControl.Items.Add(tabItem);
			_tabControl.Visibility = Visibility.Visible;

			_addEvents(tabItem, control);
			SetTabHeaderTitle(tabItem, control);
			return new TabIndex(tabItem, control);
		}

		private void _addEvents(TabItem tab, SpriteEditorControl control) {
			Utilities.Commands.AbstractCommand<IActCommand>.AbstractCommandsEventHandler handlerIndexChanged = delegate {
				_paletteOperations.UpdatePalette(control);
				SetTabHeaderTitle(tab, control);
			};

			control.Act.Commands.CommandIndexChanged += handlerIndexChanged;

			tab.Unloaded += delegate {
				control.Act.Commands.CommandIndexChanged -= handlerIndexChanged;
			};

			control.NewStateChanged += delegate {
				SetTabHeaderTitle(tab, control);
			};

			_loadDigustingButtonHandling(tab, control);
		}

		public void SetTabHeaderTitle(TabItem tab, SpriteEditorControl control) {
			string name = Path.GetFileNameWithoutExtension(control.Spr.LoadedPath);

			tab.BeginDispatch(delegate {
				bool isNew = control.IsNew;
				tab.Header = (!control.Act.Commands.IsModified && !isNew) ? name : name + " *";
			});
		}

		public void Save() {
			SafeExecute(control => {
				if (control.IsNew) {
					SaveAs();
					return;
				}

				control.Spr.Save();
				control.IsNew = false;
				_editor.RecentFiles.AddRecentFile(control.Spr.LoadedPath);
			});
		}

		public void SaveAs() {
			SafeExecute(control => {
				if (control.SaveAs()) {
					control.IsNew = false;
					_editor.RecentFiles.AddRecentFile(control.Spr.LoadedPath);
				}
			});
		}

		public TabIndex GetCurrentTabIndex() {
			return GetTabIndex(_tabControl.SelectedIndex);
		}

		public TabIndex GetTabIndex(int index) {
			if (index < 0)
				return null;

			var tab = _tabControl.Items[index] as TabItem;
			return new TabIndex(tab);
		}

		public TabIndex TabExists(TkPath file) {
			var fullPath = file.GetFullPath();

			foreach (var entry in GetTabs()) {
				if (entry?.Control?.Spr?.LoadedPath == fullPath) {
					return entry;
				}
			}

			return null;
		}

		public List<TabIndex> GetTabs() {
			return _tabControl.Items.OfType<TabItem>().Select(p => new TabIndex(p)).ToList();
		}

		private void _tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				// Prevent triggering the selection changed event multiple times from controls within the tab
				if (_lastTabSelected == _tabControl.SelectedIndex)
					return;

				_lastTabSelected = _tabControl.SelectedIndex;
				_paletteOperations.UpdatePalette(GetCurrentTabIndex()?.Control);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ViewPalette() => SafeExecute(control => _paletteOperations.ViewPalette(control));
		public void ReplacePalette() => SafeExecute(control => _paletteOperations.ReplacePalette(control));
		public void ReplaceWithDefault() => SafeExecute(control => _paletteOperations.ReplaceWithDefault(control));
		public void ClearPalette() => SafeExecute(control => _paletteOperations.ClearPalette(control));

		public void Undo() => SafeExecute(control => control.Act?.Commands.Undo());
		public void Redo() => SafeExecute(control => control.Act?.Commands.Redo());

		/// <summary>
		/// Executes an action for a SpriteEditorControl and wraps it in a try-catch friendly block with error display.
		/// </summary>
		/// <param name="action">The action.</param>
		public void SafeExecute(Action<SpriteEditorControl> action) {
			try {
				var index = GetCurrentTabIndex();

				if (index == null)
					return;

				action(index.Control);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _loadDigustingButtonHandling(TabItem tab, SpriteEditorControl control) {
			tab.Loaded += delegate {
				var border = WpfUtilities.FindChild<Border>(tab, "_borderButton");

				if (border != null) {
					border.PreviewMouseLeftButtonDown += (e, a) => { a.Handled = true; };
					border.PreviewMouseLeftButtonUp += (e, a) => { if (control.Close()) CloseTab(tab); };
				}

				var presenter = WpfUtilities.FindChild<ContentPresenter>(tab, "ContentSite");

				var border2 = WpfUtilities.FindChild<Border>(tab, "Border");

				if (border2 != null) {
					border2.PreviewMouseDown += delegate {
						if (Mouse.MiddleButton == MouseButtonState.Pressed) {
							if (control.Close()) CloseTab(tab);
						}
					};
					border2.ContextMenu = new ContextMenu();

					var menuItem = new MenuItem { Header = "Close" };
					menuItem.Click += delegate { if (control.Close()) CloseTab(tab); };
					border2.ContextMenu.Items.Add(menuItem);

					menuItem = new MenuItem { Header = "Close all but this" };
					menuItem.Click += delegate {
						GetTabs().ForEach(entry => {
							if (entry.Tab != tab) {
								if (entry.Control.Close()) {
									CloseTab(entry.Tab);
								}
							}
						});
					};
					border2.ContextMenu.Items.Add(menuItem);

					menuItem = new MenuItem { Header = "Close all" };
					menuItem.Click += delegate {
						GetTabs().ForEach(entry => {
							if (entry.Control.Close()) {
								CloseTab(entry.Tab);
							}
						});
					};
					border2.ContextMenu.Items.Add(menuItem);
					border2.ContextMenu.Items.Add(new Separator());

					menuItem = new MenuItem { Header = "Select in explorer" };
					menuItem.Click += delegate {
						try {
							if (File.Exists(control.Spr.LoadedPath)) {
								OpeningService.FilesOrFolders(control.Spr.LoadedPath);
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					};
					border2.ContextMenu.Items.Add(menuItem);
				}
			};
		}
	}
}
