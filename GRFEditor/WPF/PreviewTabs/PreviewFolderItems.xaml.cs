using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewFolderItems.xaml
	/// </summary>
	public partial class PreviewFolderItems : UserControl, IFolderPreviewTab {
		private readonly ListView _items;
		private readonly object _lock = new object();
		private readonly Queue<PreviewItem> _previewItems;
		private readonly TreeView _treeView;
		private TkPath _currentPath;
		private GrfHolder _grfData;
		private TkPath _oldPath;

		public PreviewFolderItems(TreeView treeView, ListView items, Queue<PreviewItem> previewItems) {
			_treeView = treeView;
			_items = items;
			_previewItems = previewItems;
			InitializeComponent();
		}

		#region IFolderPreviewTab Members

		public void Load(GrfHolder grfData, TkPath currentPath) {
			_currentPath = currentPath;
			_grfData = grfData;

			if (IsVisible) {
				Update();
			}
		}

		public void Update() {
			if (_oldPath != null && _oldPath.GetFullPath() == _currentPath.GetFullPath())
				return;

			if (_oldPath == null)
				_oldPath = _currentPath;

			Dispatcher.Invoke(new Action(delegate {
				if (!IsVisible)
					_wrapPanel.Dispatch(p => p.Children.Clear());
			}));

			Thread thread = new Thread(() => _load(_currentPath)) { Name = "GrfEditor - Preview folder items thread" };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		#endregion

		private void _load(TkPath currentSearch) {
			try {
				lock (_lock) {
					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;
					_oldPath = _currentPath;
					_wrapPanel.Dispatch(p => p.Children.Clear());

					List<FileEntry> entries = _grfData.FileTable.EntriesInDirectory(currentSearch.RelativePath, SearchOption.TopDirectoryOnly);

					if (entries.Count > 200) {
						entries = entries.Take(200).ToList();
					}

					_labelHeader.Dispatch(p => p.Content = _currentPath.RelativePath);

					foreach (FileEntry entry in entries) {
						GrfImage image = ImageProvider.GetImage(entry.GetDecompressedData(), entry.RelativePath.GetExtension());

						if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

						_wrapPanel.Dispatcher.Invoke(new Action(delegate {
							string filename = entry.RelativePath;
							FancyButton button = new FancyButton();
							button.TextSubDescription = Path.GetFileName(entry.RelativePath);
							button.Height = 125;
							button.Width = 125;
							button.Click += delegate { PreviewService.Select(_treeView, _items, filename); };

							button.ImageIcon.Source = image != null ? image.Cast<BitmapSource>() : IconProvider.GetLargeIcon(entry.RelativePath.GetExtension());
							button.ImageIcon.Height = 100;
							button.ImageIcon.Width = 100;

							_wrapPanel.Children.Add(button);
						}));
					}

					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_wrapPanel.Dispatch(p => p.Visibility = Visibility.Visible);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}