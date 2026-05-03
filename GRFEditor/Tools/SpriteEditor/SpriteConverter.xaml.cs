using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using ErrorManager;
using GRF.FileFormats;
using GrfToWpfBridge.Application;
using TokeiLibrary.Paths;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;

namespace GRFEditor.Tools.SpriteEditor {
	/// <summary>
	/// Interaction logic for SpriteConverter.xaml
	/// </summary>
	public partial class SpriteConverter : TkWindow {
		private WpfRecentFiles _recentFiles;
		private TabEngine _tabEngine;
		public WpfRecentFiles RecentFiles => _recentFiles;

		public SpriteConverter(IEnumerable<string> fileNames = null)
			: base("Sprite editor", "spritemaker.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();

			_tabEngine = new TabEngine(_mainTabControl, this);

			_initializeRecentFiles(fileNames);

			ApplicationShortcut.Link(ApplicationShortcut.Save, () => _tabEngine.Save(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Undo, () => _tabEngine.Undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Redo, () => _tabEngine.Redo(), this);
		}

		private void _initializeRecentFiles(IEnumerable<string> fileNames) {
			_recentFiles = new WpfRecentFiles(SpriteEditorConfiguration.ConfigAsker, 6, _menuItemRecent, "Sprite Editor");
			_recentFiles.FileClicked += _recentFiles_FileClicked;
			_recentFiles.Reload();

			_tabEngine.OpenFiles(fileNames);
		}

		private void _presenter_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _presenter_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
				_tabEngine.OpenFiles(e.Data.GetData(DataFormats.FileDrop, true) as string[]);
			}
		}

		private void _menuItemPalView_Click(object sender, RoutedEventArgs e) => _tabEngine.ViewPalette();
		private void _menuItemPalReplaceWith_Click(object sender, RoutedEventArgs e) => _tabEngine.ReplacePalette();
		private void _menuItemPalReplaceDefault_Click(object sender, RoutedEventArgs e) => _tabEngine.ReplaceWithDefault();
		private void _menuItemPalClear_Click(object sender, RoutedEventArgs e) => _tabEngine.ClearPalette();

		#region Menu

		private void _recentFiles_FileClicked(string file) => _tabEngine.Open(file);
		private void _menuItemNew_Click(object sender, RoutedEventArgs e) => _tabEngine.New();
		private void _menuItemSave_Click(object sender, RoutedEventArgs e) => _tabEngine.Save();
		private void _menuItemSaveAs_Click(object sender, RoutedEventArgs e) => _tabEngine.SaveAs();

		private void _menuItemAbout_Click(object sender, RoutedEventArgs e) {
			var dialog = new AboutDialog(SpriteEditorConfiguration.PublicVersion, SpriteEditorConfiguration.RealVersion, SpriteEditorConfiguration.Author, SpriteEditorConfiguration.ProgramName);
			dialog.AboutTextBox.Background = FindResource("UIThemeAboutDialogBrush") as Brush;
			WindowProvider.ShowWindow(dialog, this);
		}

		protected override void OnClosing(CancelEventArgs e) {
			try {
				foreach (var entry in _tabEngine.GetTabs()) {
					if (!entry.Control.Close()) {
						e.Cancel = true;
						return;
					}
				}

				base.OnClosing(e);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemClose_Click(object sender, RoutedEventArgs e) => Close();

		private void _menuItemOpen_Click(object sender, RoutedEventArgs e) {
			string[] files = TkPathRequest.OpenFiles(SpriteEditorConfiguration.AppLastPath_Config,
				"filter", FileFormat.Spr.ToFilter(),
				"fileName", SpriteEditorConfiguration.AppLastPath);

			_tabEngine.OpenFiles(files);
		}

		private void _menuItemExportAll_Click(object sender, RoutedEventArgs e) => _tabEngine.ExportAll();
		private void _menuItemCloseSprite_Click(object sender, RoutedEventArgs e) => _tabEngine.CloseTab();

		#endregion
	}
}