using ErrorManager;
using GRF.FileFormats;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GrfToWpfBridge;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TokeiLibrary;
using TokeiLibrary.Paths;
using TokeiLibrary.Shortcuts;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities.Extension;

namespace GRFEditor.Tools.SpriteEditor {
	/// <summary>
	/// Interaction logic for SpriteEditorControl.xaml
	/// </summary>
	public partial class SpriteEditorControl : UserControl {
		private SpriteEditorLogic _spriteEditorLogic;
		private string _sprFilePath;
		private GrfImageWrapper _primaryImage = new GrfImageWrapper();
		private RangeObservableCollection<SpriteView> _list1Items;
		private RangeObservableCollection<SpriteView> _list2Items;
		private string _spriteName;
		private Spr _spr;
		private Act _act;
		private SpriteView _lastDisplayItem;
		public Spr Spr => _spr;
		public Act Act => _act;
		private bool _isNew;

		public bool IsNew {
			get => _isNew;
			set {
				if (_isNew != value)
					NewStateChanged?.Invoke(this);

				_isNew = value;
			}
		}

		public delegate void SprModifiedEventHandler(Spr spr);
		public delegate void NewStateChangedEventHandler(object sender);

		public event SprModifiedEventHandler Modified;
		public event SprModifiedEventHandler Saved;
		public event NewStateChangedEventHandler NewStateChanged;

		public SpriteEditorControl() {
			InitializeComponent();

			_initializeLists();
			_setupShortcuts();
			VirtualFileDataObject.SetDraggable(_imSprImage, _primaryImage);
		}

		private void _setupShortcuts() {
			ApplicationShortcut.Link(ApplicationShortcut.Undo, () => _act?.Commands.Undo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Redo, () => _act?.Commands.Redo(), this);
			ApplicationShortcut.Link(ApplicationShortcut.Save, () => SaveAs(), this);
		}

		public void Load(Spr spr, string sprFilePath) {
			_spr = spr;

			if (_act != null) {
				_act.Commands.CommandIndexChanged -= _commands_CommandIndexChanged;
				_act.Commands.SaveCommandChanged -= _commands_SaveCommandChanged;
			}

			_act = new Act(_spr);
			_spriteEditorLogic = new SpriteEditorLogic(spr, _act);
			_sprFilePath = sprFilePath;
			
			_list1Items = new RangeObservableCollection<SpriteView>();
			_list2Items = new RangeObservableCollection<SpriteView>();
			_spriteName = Path.GetFileNameWithoutExtension(sprFilePath);

			for (int i = 0; i < _spr.NumberOfIndexed8Images; i++)
				_list1Items.Add(new SpriteView(_spr, i, GrfImageType.Indexed8, _spriteName));

			for (int i = 0; i < _spr.NumberOfBgra32Images; i++)
				_list2Items.Add(new SpriteView(_spr, i + _spr.NumberOfIndexed8Images, GrfImageType.Bgra32, _spriteName));

			_imSprList1.ItemsSource = _list1Items;
			_imSprList2.ItemsSource = _list2Items;

			_act.Commands.CommandIndexChanged += _commands_CommandIndexChanged;
			_act.Commands.SaveCommandChanged += _commands_SaveCommandChanged;
			_imSprImage.Source = null;
		}

		private void _commands_SaveCommandChanged(object sender, GRF.FileFormats.ActFormat.Commands.IActCommand command) {
			 Modified?.Invoke(_spr);
		}

		private void _commands_CommandIndexChanged(object sender, GRF.FileFormats.ActFormat.Commands.IActCommand command) {
			_updateList(_imSprList1, _list1Items, GrfImageType.Indexed8, _spr.NumberOfIndexed8Images);
			_updateList(_imSprList2, _list2Items, GrfImageType.Bgra32, _spr.NumberOfBgra32Images);
			_display(_lastDisplayItem);
			Modified?.Invoke(_spr);
		}

		private void _updateList(ListView lv, RangeObservableCollection<SpriteView> list, GrfImageType type, int count) {
			var oldSelection = _getSelection(type);
			var previousCount = list.Count;

			list.Disable();
			list.Clear();

			for (int i = 0; i < count; i++)
				list.Add(new SpriteView(_spr, i + (type == GrfImageType.Bgra32 ? _spr.NumberOfIndexed8Images : 0), type, _spriteName));

			list.UpdateAndEnable();

			if (count >= previousCount) {
				foreach (var id in oldSelection)
					lv.SelectedItems.Add(list[id + (type == GrfImageType.Bgra32 ? -_spr.NumberOfIndexed8Images : 0)]);
			}
		}

		private void _initializeLists() {
			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_imSprList1, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "Preview", DisplayExpression = "DisplayImage", FixedWidth = 70, MaxHeight = 50 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "ID", DisplayExpression = "AbsoluteId", FixedWidth = 35, TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "DisplayName", ToolTipBinding = "DisplayName", TextAlignment = TextAlignment.Left, IsFill = true }
			}, null, new string[] { });

			ListViewDataTemplateHelper.GenerateListViewTemplateNew(_imSprList2, new ListViewDataTemplateHelper.GeneralColumnInfo[] {
				new ListViewDataTemplateHelper.ImageColumnInfo { Header = "Preview", DisplayExpression = "DisplayImage", FixedWidth = 70, MaxHeight = 50 },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "ID", DisplayExpression = "AbsoluteId", FixedWidth = 35, TextAlignment = TextAlignment.Right },
				new ListViewDataTemplateHelper.GeneralColumnInfo { Header = "File name", DisplayExpression = "DisplayName", ToolTipBinding = "DisplayName", TextAlignment = TextAlignment.Left, IsFill = true }
			}, null, new string[] { });
		}

		private void _menuDelete1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Delete(GrfImageType.Indexed8, _getSelection(GrfImageType.Indexed8));
		private void _menuDelete2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Delete(GrfImageType.Bgra32, _getSelection(GrfImageType.Bgra32));
		private void _menuInsertBefore1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.InsertBefore(GrfImageType.Indexed8, _getListView(GrfImageType.Indexed8).SelectedIndex, _getFilesList());
		private void _menuInsertBefore2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.InsertBefore(GrfImageType.Bgra32, _getListView(GrfImageType.Bgra32).SelectedIndex, _getFilesList());
		private void _menuInsertAfter1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.InsertAfter(GrfImageType.Indexed8, _getListView(GrfImageType.Indexed8).SelectedIndex, _getFilesList());
		private void _menuInsertAfter2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.InsertAfter(GrfImageType.Bgra32, _getListView(GrfImageType.Bgra32).SelectedIndex, _getFilesList());
		private void _menuReplace1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Replace(GrfImageType.Indexed8, _getListView(GrfImageType.Indexed8).SelectedIndex, _getFilesList());
		private void _menuReplace2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Replace(GrfImageType.Bgra32, _getListView(GrfImageType.Bgra32).SelectedIndex, _getFilesList());
		private void _menuChangeId1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.ChangeId(GrfImageType.Indexed8, _getListView(GrfImageType.Indexed8).SelectedIndex, _getNewId(GrfImageType.Indexed8));
		private void _menuChangeId2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.ChangeId(GrfImageType.Bgra32, _getListView(GrfImageType.Bgra32).SelectedIndex + _spr.NumberOfIndexed8Images, _getNewId(GrfImageType.Bgra32));
		private void _menuToFlipVert1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Flip(GrfImageType.Indexed8, _getSelection(GrfImageType.Indexed8), FlipDirection.Vertical);
		private void _menuToFlipVert2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Flip(GrfImageType.Bgra32, _getSelection(GrfImageType.Bgra32), FlipDirection.Vertical);
		private void _menuToFlipHoriz1_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Flip(GrfImageType.Indexed8, _getSelection(GrfImageType.Indexed8), FlipDirection.Horizontal);
		private void _menuToFlipHoriz2_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Flip(GrfImageType.Bgra32, _getSelection(GrfImageType.Bgra32), FlipDirection.Horizontal);
		private void _menuToBgra32_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Convert(GrfImageType.Indexed8, _getSelection(GrfImageType.Indexed8));
		private void _menuToIndexed8_Click(object sender, RoutedEventArgs e) => _spriteEditorLogic.Convert(GrfImageType.Bgra32, _getSelection(GrfImageType.Bgra32));
		private void _menuExtract1_Click(object sender, RoutedEventArgs e) => _extract(GrfImageType.Indexed8);
		private void _menuExtract2_Click(object sender, RoutedEventArgs e) => _extract(GrfImageType.Bgra32);
		private void _imSprList1_SelectionChanged(object sender, SelectionChangedEventArgs e) => _display(_imSprList1.SelectedItem);
		private void _imSprList2_SelectionChanged(object sender, SelectionChangedEventArgs e) => _display(_imSprList2.SelectedItem);
		private void _imSprList1_Drop(object sender, DragEventArgs e) => _spriteEditorLogic.InsertAfter(GrfImageType.Indexed8, _spr.NumberOfIndexed8Images - 1, _getDropFilesList(e));
		private void _imSprList2_Drop(object sender, DragEventArgs e) => _spriteEditorLogic.InsertAfter(GrfImageType.Bgra32, _spr.NumberOfBgra32Images - 1, _getDropFilesList(e));

		private List<string> _getDropFilesList(DragEventArgs e) {
			try {
				if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
					string[] files = e.Data.GetData(DataFormats.FileDrop, true) as string[];

					if (files != null) {
						return files.Where(p => !p.IsExtension(".spr")).ToList();
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return null;
		}

		private void _display(object item) {
			SpriteView spriteView = item as SpriteView;

			if (spriteView == null) {
				_lastDisplayItem = null;
				return;
			}

			try {
				var image = _spr.GetImage(spriteView.AbsoluteId);

				if (image == null) {
					_imSprImage.Source = null;
					_imSprInfo.Text = "";
					return;
				}

				_primaryImage.ExportFileName = $"{_spriteName}{spriteView.AbsoluteId:0000}" + _getFileExtension(image.GrfImageType);
				_primaryImage.Image = image;
				_imSprImage.Source = image.Cast<BitmapSource>();
				_imSprInfo.Text = $"Format = {image.GrfImageType}; Size = {image.Width} x {image.Height}";
				_lastDisplayItem = spriteView;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private int _getNewId(GrfImageType type) {
			var id = _getListView(type).SelectedIndex;

			if (id < 0)
				return -1;

			try {
				InputDialog dialog = WindowProvider.ShowWindow<InputDialog>(new InputDialog("New ID:", "Change ID", id.ToString(CultureInfo.InvariantCulture)), WpfUtilities.FindParentControl<Window>(this));

				if (dialog.DialogResult == true) {
					return Int32.Parse(dialog.Input);
				}
			}
			catch {
				return -1;
			}

			return -1;
		}

		private void _extract(List<SpriteView> sprites) {
			try {
				if (sprites.Count == 1) {
					var sprite = sprites[0];
					string fileName = $"{_spriteName}{sprite.AbsoluteId:0000}";

					string file = TkPathRequest.SaveFile(SpriteEditorConfiguration.AppLastPath_Config,
						"filter", _getFilter(sprite.ImageType),
						"initialDirectory", Path.GetDirectoryName(fileName),
						"fileName", Path.GetFileName(fileName));

					if (file == null) return;
					_spr.GetImage(sprite.AbsoluteId).Save(file);
				}
				else if (sprites.Count > 1) {
					string path = PathRequest.FolderExtract();
					if (path == null) return;
					List<string> spritePaths = new List<string>();
					foreach (var sprite in sprites) {
						string spritePath = GrfPath.Combine(path, $"{_spriteName}{sprite.AbsoluteId:0000}" + _getFileExtension(sprite.ImageType));
						spritePaths.Add(spritePath);
						_spr.GetImage(sprite.AbsoluteId).Save(spritePath);
					}
					Utilities.Services.OpeningService.FilesOrFolders(spritePaths);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _extract(GrfImageType type) {
			_extract(_getListView(type).SelectedItems.OfType<SpriteView>().ToList());
		}

		private string _getFilter(GrfImageType type) {
			if (type == GrfImageType.Indexed8)
				return FileFormat.MergeFilters(FileFormat.Bmp);
			else
				return FileFormat.MergeFilters(FileFormat.Image, FileFormat.Bmp, FileFormat.Png, FileFormat.Jpeg, FileFormat.Tga);
		}

		private List<string> _getFilesList() {
			return (TkPathRequest.OpenFiles(SpriteEditorConfiguration.AppLastPath_Config, "filter", "Image Files|*.bmp;*.png;*.jpg;*.tga|Bitmap Files|*.bmp|PNG Files|*.png|Jpeg Files|*.jpg|Targa Files|*.tga") ?? new string[] { }).ToList();
		}

		private string _getFileExtension(GrfImageType type) {
			return type == GrfImageType.Indexed8 ? ".bmp" : SpriteEditorConfiguration.UseTgaImages ? ".tga" : ".png";
		}

		private List<int> _getSelection(GrfImageType type) {
			return _getListView(type).SelectedItems.Cast<SpriteView>().Select(p => p.AbsoluteId).ToList();
		}

		private ListView _getListView(GrfImageType type) {
			return type == GrfImageType.Indexed8 ? _imSprList1 : _imSprList2;
		}

		public bool SaveAs() {
			try {
				string file = TkPathRequest.SaveFile(SpriteEditorConfiguration.AppLastPath_Config,
					"filter", FileFormat.Spr.ToFilter(),
					"initialDirectory", Path.GetDirectoryName(_sprFilePath),
					"fileName", Path.GetFileName(_sprFilePath));

				if (file != null) {
					_spr.Save(file);
					_sprFilePath = file;
					_spr.LoadedPath = _sprFilePath;
					_act.Commands.SaveCommandIndex();
					Saved?.Invoke(_spr);
					return true;
				}

				return false;

			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		private void _imSprList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			ListView list = sender as ListView;

			if (list == null || list.SelectedItems.Count <= 0)
				return;

			if ((Keyboard.Modifiers & ModifierKeys.Alt) != ModifierKeys.Alt) {
				if (sender == _imSprList1)
					_imSprList1_SelectionChanged(null, null);
				else
					_imSprList2_SelectionChanged(null, null);
				return;
			}

			try {
				// Drag and drop the images out of the editor
				var virtualFileDataObject = new VirtualFileDataObject();
				List<VirtualFileDataObject.FileDescriptor> descriptors = new List<VirtualFileDataObject.FileDescriptor>();
				string path = GrfEditorConfiguration.TempPath;

				foreach (SpriteView file in list.SelectedItems) {
					descriptors.Add(new VirtualFileDataObject.FileDescriptor {
						Name = file.DisplayName + _getFileExtension(file.GrfImage.GrfImageType),
						GrfData = null,
						Argument = file,
						StreamContents = (grfData, filePath, stream, argument) => {
							using (MemoryStream mem = new MemoryStream()) {
								// IStream doesn't support Length, need to use a MemoryStream first
								file.GrfImage.Save(mem);
								var data = mem.ToArray();
								stream.Write(data, 0, data.Length);
							}
						}
					});
				}

				virtualFileDataObject.Source = DragAndDropSource.SpriteEditor;
				virtualFileDataObject.SetData(descriptors);

				VirtualFileDataObject.DoDragDrop(list, virtualFileDataObject, DragDropEffects.Copy);
				e.Handled = true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void ExportAll() => _extract(_list1Items.Concat(_list2Items).ToList());

		public void SetPalette(byte[] pal) {
			try {
				_act.Commands.SpriteSetPalette(pal);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _keyUp(object sender, KeyEventArgs e) {
			if (e.SystemKey == Key.LeftAlt || e.SystemKey == Key.RightAlt) {
				e.Handled = true;
			}
		}

		public bool Close() {
			if (_act == null) return true;

			if (_act.Commands.IsModified) {
				MessageBoxResult result = WindowProvider.ShowDialog("The sprite " + _spriteName + " has been modified. Do you want to save it? Any modifications will be discarded otherwise.", "Modified sprite", MessageBoxButton.YesNoCancel);

				if (result == MessageBoxResult.Yes)
					return SaveAs();

				if (result == MessageBoxResult.Cancel)
					return false;
			}

			_act?.Commands.SaveCommandIndex();
			return true;
		}

		public void SaveCommandIndex() {
			_act?.Commands.SaveCommandIndex();
		}

		#region Dependency Properties
		public static readonly DependencyProperty ListWidthProperty = DependencyProperty.Register("ListWidth", typeof(double), typeof(SpriteEditorControl), new PropertyMetadata(300.0, OnListWidthChanged));

		public double ListWidth {
			get => (double)GetValue(ListWidthProperty);
			set => SetValue(ListWidthProperty, value);
		}

		private static void OnListWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var control = (SpriteEditorControl)d;
			double newValue = (double)e.NewValue;

			control.UpdateListWidth();
		}

		private void UpdateListWidth() {
			double newSize = ListWidth;

			_gridLists.ColumnDefinitions[0] = new ColumnDefinition { Width = new GridLength(newSize) };
			_gridLists.ColumnDefinitions[1] = new ColumnDefinition { Width = new GridLength(newSize) };
		}
		#endregion
	}
}
