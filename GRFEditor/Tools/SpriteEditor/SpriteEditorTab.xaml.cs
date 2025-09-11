using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using GRF.FileFormats;
using GRF.FileFormats.SprFormat;
using GRF.FileFormats.SprFormat.Builder;
using GRF.FileFormats.SprFormat.Commands;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.GrfSystem;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.Paths;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;
using Utilities.Services;
using ICommand = GRF.FileFormats.SprFormat.Commands.ICommand;

namespace GRFEditor.Tools.SpriteEditor {
	/// <summary>
	/// Interaction logic for SpriteEditorTab.xaml
	/// </summary>
	public partial class SpriteEditorTab : TabItem, IDisposable {
		#region Delegates

		public delegate void TabEventHandler(object sender, string spriteName);

		#endregion

		#region InsertMode enum

		public enum InsertMode {
			Before,
			After,
			Replace,
		}

		#endregion

		private readonly string _displayName;

		private readonly SprBuilderInterface _sprBuilder = new SprBuilderInterface();
		public string OpenedSprite = "";
		private ObservableCollection<SprBuilderImageView> _imagesBgra32; // = new ObservableCollection<SprBuilderImageView>();
		private ObservableCollection<SprBuilderImageView> _imagesIndexed8; // = new ObservableCollection<SprBuilderImageView>();

		public SpriteEditorTab(string displayName, string spritePath = "", bool useSmallerVersion = false) {
			if (File.Exists(spritePath))
				spritePath = new FileInfo(spritePath).FullName;

			OpenedSprite = spritePath;
			_displayName = displayName;

			InitializeComponent();

			_loadUI(useSmallerVersion);
			_loadLists();
			_loadTabHeader(displayName);
			_loadCommands();

			Loaded += delegate {
				var border = WpfUtilities.FindChild<Border>(this, "_borderButton");

				if (border != null) {
					border.PreviewMouseLeftButtonDown += (e, a) => { a.Handled = true; };
					border.PreviewMouseLeftButtonUp += (e, a) => { if (Closing()) OnClose(OpenedSprite); };
				}

				var border2 = WpfUtilities.FindChild<Border>(this, "Border");

				if (border2 != null) {
					border2.PreviewMouseDown += delegate {
						if (Mouse.MiddleButton == MouseButtonState.Pressed) {
							if (Closing()) OnClose(OpenedSprite);
						}
					};
					border2.ContextMenu = new ContextMenu();

					var menuItem = new MenuItem { Header = "Close" };
					menuItem.Click += delegate { if (Closing()) OnClose(OpenedSprite); };
					border2.ContextMenu.Items.Add(menuItem);

					menuItem = new MenuItem { Header = "Close all but this" };
					menuItem.Click += delegate {
						var spriteConverter = WpfUtilities.FindParentControl<SpriteConverter>(this);

						if (spriteConverter != null) {
							spriteConverter.Tabs().ForEach(tab => {
								if (tab.OpenedSprite != OpenedSprite) {
									if (tab.Closing()) {
										tab.OnClose(tab.OpenedSprite);
									}
								}
							});
						}
					};
					border2.ContextMenu.Items.Add(menuItem);

					menuItem = new MenuItem { Header = "Close all" };
					menuItem.Click += delegate {
						var spriteConverter = WpfUtilities.FindParentControl<SpriteConverter>(this);

						if (spriteConverter != null) {
							spriteConverter.Tabs().ForEach(tab => {
								if (tab.Closing()) {
									tab.OnClose(tab.OpenedSprite);
								}
							});
						}
					};
					border2.ContextMenu.Items.Add(menuItem);
					border2.ContextMenu.Items.Add(new Separator());

					menuItem = new MenuItem { Header = "Select in explorer" };
					menuItem.Click += delegate {
						try {
							if (File.Exists(OpenedSprite)) {
								OpeningService.FilesOrFolders(OpenedSprite);
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

		public SprBuilderInterface SprBuilder {
			get { return _sprBuilder; }
		}

		public bool FoundErrors { get; set; }

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		public event TabEventHandler Close;
		public event TabEventHandler PaletteUpdated;

		private void _onPaletteUpdated(string spritename) {
			TabEventHandler handler = PaletteUpdated;
			if (handler != null) handler(this, spritename);
		}

		public void Undo() {
			_sprBuilder.Undo();
			if (!_displayName.EndsWith(" *"))
				Header = _displayName + ((_sprBuilder.IsModified) ? " *" : "");
		}

		public void Redo() {
			_sprBuilder.Redo();
			if (!_displayName.EndsWith(" *"))
				Header = _displayName + ((_sprBuilder.IsModified) ? " *" : "");
		}

		private void _spriteConverter_PreviewKeyDown(object sender, KeyEventArgs e) {
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Z) {
				Undo();
				e.Handled = true;
			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.Y) {
				Redo();
				e.Handled = true;
			}

			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.S) {
				SaveAs();
				e.Handled = true;
			}
		}

		private void _loadCommands() {
			_sprBuilder.CommandExecuted += (e, a) => {
				if (!_displayName.EndsWith(" *"))
					Header = _displayName + " *";
			};
			_sprBuilder.ItemFlipped += new SprBuilderInterface.SprBuilderEventHandler(_sprBuilder_ItemFlipped);
		}

		private void _sprBuilder_ItemFlipped(object sender, SprBuilderImageView view) {
			view.DisplayImage = view.Image.Cast<BitmapSource>();
			view.Update();

			if (view.Image.GrfImageType == GrfImageType.Indexed8)
				_imSprList1_SelectionChanged(null, null);
			else
				_imSprList2_SelectionChanged(null, null);
		}

		public bool Closing() {
			if (_sprBuilder.IsModified) {
				MessageBoxResult result = WindowProvider.ShowDialog("The sprite " + _displayName + " has been modified. Do you want to save it? Any modifications will be discarded otherwise.", "Modified sprite", MessageBoxButton.YesNoCancel);

				if (result == MessageBoxResult.Yes) {
					string path = TkPathRequest.SaveFile<SpriteEditorConfiguration>("AppLastPath",
						"filter", FileFormat.MergeFilters(Format.Spr),
						"initialDirectory", Path.GetDirectoryName(_displayName),
						"fileName", Path.GetFileName(_displayName));

					if (path != null) {
						try {
							_sprBuilder.Create(path);
							return true;
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}

					return false;
				}

				if (result == MessageBoxResult.Cancel) {
					return false;
				}
			}

			return true;
		}

		private void _loadLists() {
			try {
				if (OpenedSprite != "") {
					_sprBuilder.Open(OpenedSprite);
					_imSprClear();
				}
			}
			catch (Exception err) {
				FoundErrors = true;
				ErrorHandler.HandleException(err);
			}

			ReloadLists();
			_imSprList1.PreviewMouseLeftButtonDown += _imSprList_PreviewMouseLeftButtonDown;
			_imSprList2.PreviewMouseLeftButtonDown += _imSprList_PreviewMouseLeftButtonDown;
		}

		private void _imSprList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			ListView list = sender == _imSprList1 ? _imSprList1 : _imSprList2;

			if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt && list.Items.Count > 0) {
				var virtualFileDataObject = new VirtualFileDataObject();
				List<VirtualFileDataObject.FileDescriptor> descriptors = new List<VirtualFileDataObject.FileDescriptor>();
				string path = GrfEditorConfiguration.TempPath;

				foreach (SprBuilderImageView file in list.SelectedItems) {
					descriptors.Add(new VirtualFileDataObject.FileDescriptor {
						Name = file.Filename + (file.Image.GrfImageType == GrfImageType.Indexed8 ? ".bmp" : SpriteEditorConfiguration.UseTgaImages ? ".tga" : ".png"),
						GrfData = null,
						Argument = file,
						StreamContents = (grfData, filePath, stream, argument) => {
							SprBuilderImageView image = (SprBuilderImageView) argument;
							string fileName = Path.Combine(path, image.Filename + (image.Image.GrfImageType == GrfImageType.Indexed8 ? ".bmp" : SpriteEditorConfiguration.UseTgaImages ? ".tga" : ".png"));

							image.Image.Save(fileName);
							byte[] data = File.ReadAllBytes(fileName);
							stream.Write(data, 0, data.Length);
						}
					});
				}

				virtualFileDataObject.Source = DragAndDropSource.SpriteEditor;
				virtualFileDataObject.SetData(descriptors);

				VirtualFileDataObject.DoDragDrop(list, virtualFileDataObject, DragDropEffects.Copy);
				e.Handled = true;
			}
			else {
				if (sender == _imSprList1)
					_imSprList1_SelectionChanged(null, null);
				else
					_imSprList2_SelectionChanged(null, null);
			}
		}

		private void _loadUI(bool useSmallerVersion) {
			_sprBuilder.ItemRemoved += new SprBuilderInterface.SprBuilderEventHandler(_sprBuilder_ItemRemoved);
			_sprBuilder.ItemInserted += new SprBuilderInterface.SprBuilderInsertionEventHandler(_sprBuilder_ItemInserted);
			_sprBuilder.Palette.PaletteChanged += _ => {
				_onPaletteUpdated(OpenedSprite);
				ReloadLists();
				_imSprImage.Source = null;
			};

			PreviewKeyDown += new KeyEventHandler(_spriteConverter_PreviewKeyDown);

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_imSprList1, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "Preview", DisplayExpression = "DisplayImage", FixedWidth = 70, MaxHeight = 50 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "ID", DisplayExpression = "DisplayID", FixedWidth = 35, TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "DisplayName", ToolTipBinding = "DisplayName", TextAlignment = TextAlignment.Left, IsFill = true }
			}, null, new string[] { });

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_imSprList2, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "Preview", DisplayExpression = "DisplayImage", FixedWidth = 70, MaxHeight = 50 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "ID", DisplayExpression = "DisplayID", FixedWidth = 35, TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "DisplayName", ToolTipBinding = "DisplayName", TextAlignment = TextAlignment.Left, IsFill = true }
			}, null, new string[] { });

			if (useSmallerVersion) {
				_gridLists.ColumnDefinitions[0] = new ColumnDefinition { Width = new GridLength(200) };
				_gridLists.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(200) };
			}

			Background = new SolidColorBrush(Colors.White);
		}

		private void _loadTabHeader(string displayName) {
			Header = displayName;
		}

		private void _sprBuilder_ItemInserted(object sender, SprBuilderImageView view, int index, GrfImageType type) {
			view.DisplayImage = view.Image.Cast<BitmapSource>();

			if (type == GrfImageType.Indexed8) {
				_imagesIndexed8.Insert(index, view);
			}
			else {
				_imagesBgra32.Insert(index, view);
			}
		}

		public void OnClose(string spritename) {
			TabEventHandler handler = Close;
			if (handler != null) handler(this, spritename);
		}

		private void _sprBuilder_ItemRemoved(object sender, SprBuilderImageView view) {
			_imagesIndexed8.Remove(view);
			_imagesBgra32.Remove(view);
		}

		private void _menuDelete1_Click(object sender, RoutedEventArgs e) {
			List<ICommand> remove = _imSprList1.SelectedItems.Cast<SprBuilderImageView>().OrderByDescending(p => p.DisplayID).Select(p => new RemoveCommand(p)).Cast<ICommand>().ToList();
			_sprBuilder.StoreAndExecute(new GroupCommand(remove));
		}

		private void _menuDelete2_Click(object sender, RoutedEventArgs e) {
			List<ICommand> remove = _imSprList2.SelectedItems.Cast<SprBuilderImageView>().OrderByDescending(p => p.DisplayID).Select(p => new RemoveCommand(p)).Cast<ICommand>().ToList();
			_sprBuilder.StoreAndExecute(new GroupCommand(remove));
		}

		private void _menuInsertAfter1_Click(object sender, RoutedEventArgs e) {
			try {
				int option = -1;
				_addFiles(_imSprList1, _getImageFiles(), InsertMode.After, ref option);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuInsertAfter2_Click(object sender, RoutedEventArgs e) {
			try {
				int option = -1;
				_addFiles(_imSprList2, _getImageFiles(), InsertMode.After, ref option);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuInsertBefore1_Click(object sender, RoutedEventArgs e) {
			try {
				int option = -1;
				_addFiles(_imSprList1, _getImageFiles(), InsertMode.Before, ref option);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuInsertBefore2_Click(object sender, RoutedEventArgs e) {
			try {
				int option = -1;
				_addFiles(_imSprList2, _getImageFiles(), InsertMode.Before, ref option);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _imSprList1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_imSprList1.SelectedItem != null) {
					if (Mouse.LeftButton != MouseButtonState.Pressed || (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
						_imSprImage.Source = SpriteEditorHelper.MakeFirstPaletteColorTransparent(((SprBuilderImageView) _imSprList1.SelectedItem).Image);
						_imSprInfo.Text = String.Format("Format = {2}; Size = {0} x {1}", (int) _imSprImage.Source.Width, (int) _imSprImage.Source.Height, "Indexed8");
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _imSprList2_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			try {
				if (_imSprList2.SelectedItem != null) {
					if (Mouse.LeftButton != MouseButtonState.Pressed || (Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
						_imSprImage.Source = (BitmapSource) ((SprBuilderImageView) _imSprList2.SelectedItem).DisplayImage;
						_imSprInfo.Text = String.Format("Format = {2}; Size = {0} x {1}", (int) _imSprImage.Source.Width, (int) _imSprImage.Source.Height, "Bgra32");
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private IEnumerable<string> _getImageFiles() {
			return TkPathRequest.OpenFiles<SpriteEditorConfiguration>("AppLastPath", "filter", "Image Files|*.bmp;*.png;*.jpg;*.tga|Bitmap Files|*.bmp|PNG Files|*.png|Jpeg Files|*.jpg|Targa Files|*.tga")
			       ?? new string[] { };
		}

		private void _addFiles(ListView listView, IEnumerable<string> fileNames, InsertMode insertMode, ref int optionSource) {
			int option = optionSource;
			this.Dispatch(delegate {
				int currentIndex = listView.SelectedIndex;
				currentIndex = currentIndex < 0 ? insertMode == InsertMode.Before ? 0 : -1 : currentIndex;

				if (ReferenceEquals(listView, _imSprList1)) {
					foreach (string file in fileNames) {
						if (file.GetExtension() == ".bmp") {
							BmpBitmapDecoder dec = new BmpBitmapDecoder(new MemoryStream(File.ReadAllBytes(file)), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);

							if (dec.Frames[0].Format == PixelFormats.Indexed8) {
								if (insertMode == InsertMode.After) currentIndex++;

								try {
									byte[] pixels = WpfImaging.GetData(dec.Frames[0]);
									byte[] pal = Imaging.Get256BytePaletteRGBA(dec.Frames[0].Palette);
									GrfImage image = new GrfImage(ref pixels, dec.Frames[0].PixelWidth, dec.Frames[0].PixelHeight, GrfImageType.Indexed8, ref pal);

									_sprBuilder.StoreAndExecute(new Insert(new SprBuilderImageView { Image = image, DisplayID = currentIndex, OriginalName = Path.GetFileNameWithoutExtension(file) }));

									if (insertMode == InsertMode.Replace) _sprBuilder.StoreAndExecute(new RemoveCommand(_imagesIndexed8[currentIndex + 1]));
								}
								catch (SprException) {
									_indexed8Error(file, ref currentIndex, ref option, insertMode, GrfImageType.Indexed8);
								}
								catch {
									if (insertMode == InsertMode.After) currentIndex--;
								}

								if (insertMode == InsertMode.Before) currentIndex++;
							}
							else {
								if (insertMode == InsertMode.After) currentIndex++;
								_indexed8Error(file, ref currentIndex, ref option, insertMode, GrfImageType.Indexed8);
								if (insertMode == InsertMode.Before) currentIndex++;
							}
						}
						else {
							if (insertMode == InsertMode.After) currentIndex++;
							_indexed8Error(file, ref currentIndex, ref option, insertMode, GrfImageType.Indexed8);
							if (insertMode == InsertMode.Before) currentIndex++;
						}
					}
				}
				else {
					foreach (string file in fileNames) {
						if (insertMode == InsertMode.After) currentIndex++;

						GrfImage imageSource = ImageProvider.GetImage(file, file.GetExtension());
						imageSource.Convert(new Bgra32FormatConverter());

						_sprBuilder.StoreAndExecute(new Insert(new SprBuilderImageView { Image = imageSource, DisplayID = currentIndex, OriginalName = Path.GetFileNameWithoutExtension(file) }));

						if (insertMode == InsertMode.Before) currentIndex++;
					}
				}
			});
			optionSource = option;
		}

		private void _indexed8Error(string file, ref int index, ref int option, InsertMode insertMode, GrfImageType type, List<ICommand> toReturn = null) {
			try {
				if (option == -2) {
					if (insertMode == InsertMode.After) index--;
					if (insertMode == InsertMode.Before) index--;
					return;
				}

				GrfImage imageSource = ImageProvider.GetImage(File.ReadAllBytes(file), file.GetExtension());

				if (imageSource.GrfImageType != GrfImageType.Indexed8) {
					imageSource.Convert(new Bgra32FormatConverter());
				}

				SpriteConverterFormatDialog dialog = new SpriteConverterFormatDialog(file, _sprBuilder.GetPalette(), imageSource, _sprBuilder.GetUsedPaletteIndexes(), option);

				List<ICommand> commands = new List<ICommand>();

				if (WindowProvider.ShowWindow(dialog, WpfUtilities.FindParentControl<Window>(this)) == true) {
					if (dialog.Result.GrfImageType == GrfImageType.Bgra32) {
						commands.Add(new Insert(new SprBuilderImageView { Image = dialog.Result, DisplayID = _imagesBgra32.Count, OriginalName = Path.GetFileNameWithoutExtension(file) }));

						if (insertMode == InsertMode.Replace && type == GrfImageType.Bgra32) {
							commands.Add(new RemoveCommand(_imagesBgra32[index + 1]));
						}
					}
					else {
						commands.Add(new ChangePalette(dialog.Result.Palette));
						commands.Add(new Insert(new SprBuilderImageView { Image = dialog.Result, DisplayID = index, OriginalName = Path.GetFileNameWithoutExtension(file) }));

						if (insertMode == InsertMode.Replace && type == GrfImageType.Indexed8) {
							commands.Add(new RemoveCommand(_imagesIndexed8[index + 1]));
						}
					}

					if (toReturn != null)
						toReturn.AddRange(commands);
					else
						_sprBuilder.StoreAndExecute(new GroupCommand(commands));
				}
				else {
					if (insertMode == InsertMode.After) index--;
					if (insertMode == InsertMode.Before) index--;
				}

				option = dialog.RepeatOption;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ReloadLists() {
			_sprBuilder.ImagesIndexed8.ForEach(p => p.DisplayImage = p.Image.Cast<BitmapSource>());
			_sprBuilder.ImagesBgra32.ForEach(p => p.DisplayImage = p.Image.Cast<BitmapSource>());

			_imagesIndexed8 = new ObservableCollection<SprBuilderImageView>(_sprBuilder.ImagesIndexed8);
			_imagesBgra32 = new ObservableCollection<SprBuilderImageView>(_sprBuilder.ImagesBgra32);
			_imSprList1.ItemsSource = _imagesIndexed8;
			_imSprList2.ItemsSource = _imagesBgra32;
		}

		private void _menuReplace1_Click(object sender, RoutedEventArgs e) {
			try {
				string file = _getFilePath(false, GrfImageType.Indexed8);
				if (file != null) {
					int option = -1;
					_addFiles(_imSprList1, new string[] { file }, InsertMode.Replace, ref option);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuReplace2_Click(object sender, RoutedEventArgs e) {
			try {
				string file = _getFilePath(false, GrfImageType.Bgra32);
				if (file != null) {
					int option = -1;
					_addFiles(_imSprList2, new string[] { file }, InsertMode.Replace, ref option);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private string _getFilePath(bool saveDialog, GrfImageType mode, string fileName = "") {
			fileName = fileName == "" ? "image.bmp" : fileName;

			if (saveDialog) {
				string file = TkPathRequest.SaveFile<SpriteEditorConfiguration>("AppLastPath",
					"filter", mode == GrfImageType.Indexed8 ? "Bitmap Files|*.bmp" :
						"Image Files|*.bmp;*.png;*.jpg;*.tga|Bitmap Files|*.bmp|PNG Files|*.png|Jpeg Files|*.jpg|Targa Files|*.tga",
					"initialDirectory", Path.GetDirectoryName(fileName),
					"fileName", Path.GetFileName(fileName));

				if (file != null) {
					return file;
				}
			}
			else {
				string file = TkPathRequest.OpenFile<SpriteEditorConfiguration>("AppLastPath",
					"filter", mode == GrfImageType.Indexed8 ? "Bitmap Files|*.bmp" :
						"Image Files|*.bmp;*.png;*.jpg;*.tga|Bitmap Files|*.bmp|PNG Files|*.png|Jpeg Files|*.jpg|Targa Files|*.tga",
					"initialDirectory", Path.GetDirectoryName(fileName),
					"fileName", Path.GetFileName(fileName));

				if (file != null) {
					return file;
				}
			}
			return null;
		}

		private void _menuExtract1_Click(object sender, RoutedEventArgs e) {
			_saveImage(_imSprList1.SelectedItems.Cast<SprBuilderImageView>().ToList());
		}

		private void _menuExtract2_Click(object sender, RoutedEventArgs e) {
			_saveImage(_imSprList2.SelectedItems.Cast<SprBuilderImageView>().ToList());
		}

		private void _menuToBgra32_Click(object sender, RoutedEventArgs e) {
			try {
				for (int index = 0; index < _imSprList1.SelectedItems.Count; index++) {
					SprBuilderImageView view = (SprBuilderImageView) _imSprList1.SelectedItems[index];

					List<ICommand> groupCommands = new List<ICommand>();
					groupCommands.Add(new RemoveCommand(view));

					GrfImage image = view.Image.Copy();
					image.Palette[3] = 0;
					image.Convert(new Bgra32FormatConverter());

					groupCommands.Add(new Insert(new SprBuilderImageView { Image = image, DisplayID = _imagesBgra32.Count, OriginalName = Path.GetFileNameWithoutExtension(view.OriginalName) }));
					_sprBuilder.StoreAndExecute(new GroupCommand(groupCommands));
					index--;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuToIndexed8_Click(object sender, RoutedEventArgs e) {
			try {
				int option = -1;
				InsertMode insertMode = InsertMode.After;

				for (int index = 0; index < _imSprList2.SelectedItems.Count; index++) {
					List<ICommand> commands = new List<ICommand>();
					SprBuilderImageView view = (SprBuilderImageView) _imSprList2.SelectedItems[index];
					string path = TemporaryFilesManager.GetTemporaryFilePath("spr_editor_r_{0:000000}.png");
					view.Image.Save(path, PixelFormats.Bgra32);
					//WpfImaging.SaveImage((BitmapSource) view.DisplayImage, path, PixelFormats.Bgra32);

					int currentIndex = _imSprList1.SelectedIndex;
					currentIndex = currentIndex < 0 ? insertMode == InsertMode.Before ? 0 : -1 : currentIndex;

					if (insertMode == InsertMode.After) currentIndex++;
					_indexed8Error(path, ref currentIndex, ref option, insertMode, GrfImageType.Indexed8, commands);

					File.Delete(path);

					// The conversion went well
					if (commands.Count > 0) {
						commands.Add(new RemoveCommand(view));
						_sprBuilder.StoreAndExecute(new GroupCommand(commands));
						index--;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		protected override void OnLostFocus(RoutedEventArgs e) {
		}

		private void _imSprList1_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null) {
						files = files.Where(p => p.GetExtension() != ".spr").ToArray();

						int option = -1;
						new Thread(() => _addFiles(_imSprList1, files, InsertMode.After, ref option)) { Name = "SpriteEditor - Add files thread" }.Start();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _imSprList2_Drop(object sender, DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null) {
						files = files.Where(p => !p.ToLower().EndsWith(".spr")).ToArray();

						int option = -1;
						new Thread(() => _addFiles(_imSprList2, files, InsertMode.After, ref option)) { Name = "SpriteEditor - Add files thread" }.Start();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _imSprClear() {
			_imSprList1.ItemsSource = null;
			_imSprList2.ItemsSource = null;
			_imSprImage.Source = null;
		}

		public bool Save() {
			if (File.Exists(OpenedSprite)) {
				_sprBuilder.Create(OpenedSprite);
				_sprBuilder.ClearCommands();
				Header = _displayName;
				return true;
			}
			return SaveAs();
		}

		public bool SaveAs() {
			try {
				string file = TkPathRequest.SaveFile<SpriteEditorConfiguration>("AppLastPath",
					"filter", FileFormat.MergeFilters(Format.Spr),
					"initialDirectory", Path.GetDirectoryName(_sprBuilder.GetFileName()),
					"fileName", Path.GetFileName(_sprBuilder.GetFileName()));

				if (file != null) {
					_sprBuilder.Create(file);
					OpenedSprite = file;
					Header = Path.GetFileName(OpenedSprite);
				}
				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		private void _saveImage(IList<SprBuilderImageView> images) {
			try {
				if (images.Count == 1) {
					string file = _getFilePath(true, GrfImageType.Indexed8, images[0].Filename);
					if (file == null) return;
					images[0].Image.Save(file);
				}
				else if (images.Count > 1) {
					string path = PathRequest.FolderExtract();
					if (path == null) return;
					foreach (SprBuilderImageView image in images) {
						image.Image.Save(Path.Combine(path, image.Filename + (image.Image.GrfImageType == GrfImageType.Indexed8 ? ".bmp" : SpriteEditorConfiguration.UseTgaImages ? ".tga" : ".png")));
					}
					OpeningService.OpenFolder(path);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ExportAll() {
			try {
				_saveImage(_sprBuilder.ImagesIndexed8.Concat(_sprBuilder.ImagesBgra32).ToList());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuToFlipVert1_Click(object sender, RoutedEventArgs e) {
			_flip(_imSprList1.SelectedItems.Cast<SprBuilderImageView>().ToList(), FlipDirection.Vertical);
		}

		private void _menuToFlipVert2_Click(object sender, RoutedEventArgs e) {
			_flip(_imSprList2.SelectedItems.Cast<SprBuilderImageView>().ToList(), FlipDirection.Vertical);
		}

		private void _menuToFlipHoriz1_Click(object sender, RoutedEventArgs e) {
			_flip(_imSprList1.SelectedItems.Cast<SprBuilderImageView>().ToList(), FlipDirection.Horizontal);
		}

		private void _menuToFlipHoriz2_Click(object sender, RoutedEventArgs e) {
			_flip(_imSprList2.SelectedItems.Cast<SprBuilderImageView>().ToList(), FlipDirection.Horizontal);
		}

		private void _flip(List<SprBuilderImageView> images, FlipDirection dir) {
			try {
				List<ICommand> commands = new List<ICommand>();
				images.ForEach(p => commands.Add(new Flip(p, dir)));
				_sprBuilder.StoreAndExecute(new GroupCommand(commands));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _menuChangeId1_Click(object sender, RoutedEventArgs e) {
			_changeId(_imSprList1.SelectedItems.Cast<SprBuilderImageView>().ToList(), GrfImageType.Indexed8);
		}

		private void _menuChangeId2_Click(object sender, RoutedEventArgs e) {
			_changeId(_imSprList2.SelectedItems.Cast<SprBuilderImageView>().ToList(), GrfImageType.Bgra32);
		}

		private void _changeId(List<SprBuilderImageView> views, GrfImageType type) {
			if (views == null || views.Count == 0) return;

			views = views.OrderBy(p => p.DisplayID).ToList();

			InputDialog dialog = WindowProvider.ShowWindow<InputDialog>
				(new InputDialog("New ID :", "Change ID", views[0].DisplayID.ToString(CultureInfo.InvariantCulture)), WpfUtilities.FindParentControl<Window>(this));

			try {
				if (dialog.DialogResult == true) {
					int targetIdx = Int32.Parse(dialog.Input);

					List<ICommand> commands = new List<ICommand>();

					if (type == GrfImageType.Indexed8)
						targetIdx = targetIdx < 0 ? 0 : targetIdx >= _sprBuilder.ImagesIndexed8.Count - views.Count ? _sprBuilder.ImagesIndexed8.Count - views.Count : targetIdx;
					else
						targetIdx = targetIdx < 0 ? 0 : targetIdx >= _sprBuilder.ImagesBgra32.Count - views.Count ? _sprBuilder.ImagesBgra32.Count - views.Count : targetIdx;

					if (targetIdx > views[0].DisplayID) {
						for (int i = views.Count - 1; i >= 0; i--) {
							commands.Add(new ChangeId(views[i], targetIdx + views[i].DisplayID - views[0].DisplayID));
						}
					}
					else {
						for (int i = 0; i < views.Count; i++) {
							commands.Add(new ChangeId(views[i], targetIdx + views[i].DisplayID - views[0].DisplayID));
						}
					}

					_sprBuilder.StoreAndExecute(new GroupCommand(commands));
					//int removedIdx = views.DisplayID;


					//_sprBuilder.ChangeImageIndex(removedIdx, type, targetIdx, type);

					if (type == GrfImageType.Indexed8)
						_imSprList1.SelectedItems.Clear();
					else
						_imSprList2.SelectedItems.Clear();
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void SetPalette(byte[] pal) {
			_sprBuilder.StoreAndExecute(new ChangePalette(pal));
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_sprBuilder != null)
					_sprBuilder.Dispose();
			}
		}

		#region Key and mouse events

		private void _keyUp(object sender, KeyEventArgs e) {
			if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt) {
				e.Handled = true;
			}
		}

		#endregion
	}
}