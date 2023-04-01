using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.FileFormats;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using TheCodeKing.Net.Messaging;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;
using Configuration = GRFEditor.ApplicationConfiguration.GrfEditorConfiguration;

namespace GRFEditor.Tools.SpriteEditor {
	/// <summary>
	/// Interaction logic for SpriteConverter.xaml
	/// </summary>
	public partial class SpriteConverter : TkWindow {
		private readonly XDListener _listener = new XDListener();
		private readonly WpfRecentFiles _recentFiles;
		private readonly Style _tabStyle;
		private TkWindow _spriteEditorPalette;

		public SpriteConverter(IEnumerable<string> filenames = null)
			: base("Sprite editor", "spritemaker.ico", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();

			ShowInTaskbar = true;

			_recentFiles = new WpfRecentFiles(SpriteEditorConfiguration.ConfigAsker, 6, _menuItemRecent, "Sprite Editor");
			_recentFiles.FileClicked += new RecentFilesManager.RFMFileClickedEventHandler(_recentFiles_FileClicked);
			_recentFiles.Reload();

			//_presenter.MainGrid.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);

			Binder.Bind(_cbUseTga, () => SpriteEditorConfiguration.UseTgaImages);

			_mainTabControl.SelectionChanged += new SelectionChangedEventHandler(_mainTabControl_SelectionChanged);

			try {
				var style = TryFindResource("TabItemSprite") as Style;

				if (style != null) {
					_tabStyle = style;
				}
			}
			catch {
			}

			if (filenames != null) {
				foreach (string filename in filenames) {
					_openSprite(filename);
				}
			}

			_cbAssocSpr.Checked -= new RoutedEventHandler(_cbAssocSpr_Checked);
			_cbAssocSpr.IsChecked = (Configuration.FileShellAssociated & FileAssociation.Spr) == FileAssociation.Spr;
			_cbAssocSpr.Checked += new RoutedEventHandler(_cbAssocSpr_Checked);

			_listener.RegisterChannel("openSprite");
			_listener.MessageReceived += new XDListener.XDMessageHandler(_listener_MessageReceived);

			PreviewKeyDown += new KeyEventHandler(_spriteConverter_PreviewKeyDown);
		}

		private void _spriteConverter_PreviewKeyDown(object sender, KeyEventArgs e) {
			var tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;

			if (tab != null) {
				if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S) {
					tab.Save();
					e.Handled = true;
				}
			}
		}

		private void _presenter_DragEnter(object sender, DragEventArgs e) {
			e.Effects = DragDropEffects.Copy;
		}

		private void _presenter_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
				string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

				if (files != null && files.Length > 0) {
					foreach (string file in files.Where(p => p.ToLower().EndsWith(".spr"))) {
						_openSprite(file);
					}
				}
			}
		}

		private void _openSprite(string file) {
			_recentFiles.AddRecentFile(file);
			if (Tabs().Any(p => p.OpenedSprite == file)) {
				_mainTabControl.SelectedIndex = Tabs().IndexOf(Tabs().First(p => p.OpenedSprite == file));
			}
			else {
				if (!File.Exists(file))
					return;

				SpriteEditorTab tab = new SpriteEditorTab(Path.GetFileName(file), file);
				tab.Style = _tabStyle;

				if (!tab.FoundErrors) {
					tab.Close += (o, a) => _mainTabControl.Items.RemoveAt(Tabs().IndexOf(Tabs().First(p => ReferenceEquals(p, tab))));
					tab.PaletteUpdated += (o, a) => _updatePalette();
					_mainTabControl.Items.Insert(_mainTabControl.Items.Count - 1, tab);
					_mainTabControl.SelectedIndex = _mainTabControl.Items.Count - 2;
				}
			}
		}

		private void _menuItemPalView_Click(object sender, RoutedEventArgs e) {
			var tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
			if (tab != null) {
				if (_spriteEditorPalette == null || _spriteEditorPalette.IsVisible == false) {
					_spriteEditorPalette = new TkWindow("Palette", "help.ico");
					_spriteEditorPalette.ShowInTaskbar = true;
					Image image = new Image();
					image.Width = 256;
					image.Height = 256;
					image.Source = ImageProvider.GetImage(tab.SprBuilder.GetPalette(), ".pal").Cast<BitmapSource>();
					_spriteEditorPalette.Content = image;
					_spriteEditorPalette.Show();
					_spriteEditorPalette.Owner = this;
				}
			}
		}

		private void _updatePalette() {
			if (_mainTabControl.SelectedIndex < 0)
				return;

			var tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
			if (tab != null) {
				if (_spriteEditorPalette != null && _spriteEditorPalette.IsVisible) {
					Image image = new Image();
					image.Width = 256;
					image.Height = 256;
					image.Source = ImageProvider.GetImage(tab.SprBuilder.GetPalette(), ".pal").Cast<BitmapSource>();
					_spriteEditorPalette.Content = image;
					_spriteEditorPalette.Owner = this;
				}
			}
		}

		private void _menuItemPalReplaceWith_Click(object sender, RoutedEventArgs e) {
			try {
				var tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
				if (tab != null) {
					if (tab.SprBuilder.ImagesIndexed8.Count > 0) {
						if (WindowProvider.ShowDialog("This sprite already has a palette, are you sure you want to replace it?", "Information", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes) {
							return;
						}
					}

					string file = PathRequest.OpenFileSprite(
						"fileName", Configuration.AppLastPath,
						"filter", FileFormat.MergeFilters(Format.PalAndSpr | Format.Pal | Format.Spr));

					if (file != null) {
						if (file.GetExtension() == ".pal") {
							byte[] pal = File.ReadAllBytes(file);

							if (pal.Length != 1024) {
								ErrorHandler.HandleException("Invalid palette file.");
								return;
							}

							tab.SetPalette(pal);
						}
						else if (file.IsExtension(".spr")) {
							byte[] pal = new byte[1024];
							Spr spr = new Spr(file);

							if (spr.NumberOfIndexed8Images <= 0) {
								ErrorHandler.HandleException("This palette file doesn't contain any Indexed8 images; no palette found.");
								return;
							}

							Buffer.BlockCopy(spr.Images[0].Palette, 0, pal, 0, 1024);

							tab.SetPalette(pal);
						}
						else {
							ErrorHandler.HandleException("Invalid file extension.");
							return;
						}

						_updatePalette();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemPalReplaceDefault_Click(object sender, RoutedEventArgs e) {
			try {
				var tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
				if (tab != null) {
					if (tab.SprBuilder.ImagesIndexed8.Count > 0) {
						if (WindowProvider.ShowDialog("This sprite already has a palette, are you sure you want to replace it?", "Information", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes) {
							return;
						}
					}

					byte[] pal = ApplicationManager.GetResource("default.pal");

					tab.SetPalette(pal);
					_updatePalette();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemPalClear_Click(object sender, RoutedEventArgs e) {
			try {
				var tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
				if (tab != null) {
					if (tab.SprBuilder.ImagesIndexed8.Count > 0) {
						if (WindowProvider.ShowDialog("This sprite already has a palette, are you sure you want to replace it?", "Information", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes) {
							return;
						}
					}

					byte[] pal = new byte[1024];
					pal[0] = 255;
					pal[2] = 255;

					for (int i = 0; i < 256; i++) {
						pal[4 * i + 3] = 255;
					}

					tab.SetPalette(pal);
					_updatePalette();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _cbAssocSpr_Checked(object sender, RoutedEventArgs e) {
			Configuration.FileShellAssociated |= FileAssociation.Spr;
			ApplicationManager.AddExtension(Methods.ApplicationFullPath, "Sprite", ".spr", true);
		}

		private void _cbAssocSpr_Unchecked(object sender, RoutedEventArgs e) {
			Configuration.FileShellAssociated &= ~FileAssociation.Spr;
			ApplicationManager.RemoveExtension(Methods.ApplicationFullPath, ".spr");
		}

		#region Menu

		private void _recentFiles_FileClicked(string file) {
			try {
				if (File.Exists(file)) {
					if (Tabs().Any(p => p.OpenedSprite == file)) {
						_mainTabControl.SelectedIndex = Tabs().IndexOf(Tabs().First(p => p.OpenedSprite == file));
					}
					else {
						SpriteEditorTab tab = new SpriteEditorTab(Path.GetFileName(file), file);
						tab.Style = _tabStyle;

						if (!tab.FoundErrors) {
							tab.Close += (o, a) => _mainTabControl.Items.RemoveAt(Tabs().IndexOf(Tabs().First(p => ReferenceEquals(p, tab))));
							tab.PaletteUpdated += (o, a) => _updatePalette();
							_mainTabControl.Items.Insert(_mainTabControl.Items.Count - 1, tab);
							_mainTabControl.SelectedIndex = _mainTabControl.Items.Count - 2;
						}
					}
				}
				else {
					_recentFiles.RemoveRecentFile(file);
					ErrorHandler.HandleException("File not found : " + file, ErrorLevel.Low);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemNew_Click(object sender, RoutedEventArgs e) {
			try {
				SpriteEditorTab tab = new SpriteEditorTab("new *");
				tab.Style = _tabStyle;

				tab.PaletteUpdated += (o, a) => _updatePalette();
				tab.Close += (o, a) => _mainTabControl.Items.RemoveAt(Tabs().IndexOf(Tabs().First(p => ReferenceEquals(p, tab))));
				_mainTabControl.Items.Insert(_mainTabControl.Items.Count - 1, tab);
				_mainTabControl.SelectedIndex = _mainTabControl.Items.Count - 2;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemAbout_Click(object sender, RoutedEventArgs e) {
			WindowProvider.ShowWindow(new AboutDialog(SpriteEditorConfiguration.PublicVersion, SpriteEditorConfiguration.RealVersion, SpriteEditorConfiguration.Author, SpriteEditorConfiguration.ProgramName), this);
		}

		private void _menuItemClose_Click(object sender, RoutedEventArgs e) {
			try {
				Close();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSave_Click(object sender, RoutedEventArgs e) {
			try {
				SpriteEditorTab tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
				if (tab != null) {
					if (tab.Save()) {
						_recentFiles.AddRecentFile(tab.OpenedSprite);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemSaveAs_Click(object sender, RoutedEventArgs e) {
			try {
				SpriteEditorTab tab = _mainTabControl.Items[_mainTabControl.SelectedIndex] as SpriteEditorTab;
				if (tab != null) {
					if (tab.SaveAs()) {
						_recentFiles.AddRecentFile(tab.OpenedSprite);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemOpen_Click(object sender, RoutedEventArgs e) {
			string[] files = PathRequest.OpenFilesSprite("filter", FileFormat.MergeFilters(Format.Spr),
			                                             "fileName", SpriteEditorConfiguration.AppLastPath);

			if (files != null) {
				try {
					foreach (string file in files) {
						_openSprite(file);
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		private void _menuItemExportAll_Click(object sender, RoutedEventArgs e) {
			try {
				if (_mainTabControl.SelectedItem is SpriteEditorTab) {
					((SpriteEditorTab) _mainTabControl.SelectedItem).ExportAll();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuItemCloseSprite_Click(object sender, RoutedEventArgs e) {
			try {
				_mainTabControl.Items.Remove(_mainTabControl.SelectedItem);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		#endregion

		#region XDMessagin

		private void _listener_MessageReceived(object sender, XDMessageEventArgs e) {
			if (e.DataGram.Channel == "openSprite") {
				this.Activate();
				//NativeMethods.SetForegroundWindow(this.HandlesScrolling);
				XDBroadcast.SendToChannel("openSpriteResponse", "grabbed");
				_openSprite(e.DataGram.Message);
			}
		}

		protected override void OnClosing(CancelEventArgs e) {
			if (_spriteEditorPalette != null) {
				_spriteEditorPalette.Close();
			}
			_listener.UnRegisterChannel("openSprite");
			base.OnClosing(e);
		}

		#endregion
	}
}