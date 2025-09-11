using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using TokeiLibrary;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewFolderStructure.xaml
	/// </summary>
	public partial class PreviewFolderStructure : UserControl, IFolderPreviewTab {
		private readonly object _lock = new object();
		private readonly Queue<PreviewItem> _previewItems;
		private TkPath _currentPath;
		private GrfHolder _grfData;
		private TkPath _oldPath;

		public PreviewFolderStructure(Queue<PreviewItem> previewItems) {
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

		public void Update(bool forceUpdate) {
			if (forceUpdate)
				_oldPath = null;

			Update();
		}

		public void Update() {
			if (_oldPath != null && _oldPath.GetFullPath() == _currentPath.GetFullPath())
				return;

			if (_oldPath == null)
				_oldPath = _currentPath;

			Thread thread = new Thread(() => _load(_currentPath)) { Name = "GrfEditor - Preview folder structure thread" };
			thread.SetApartmentState(ApartmentState.STA);
			thread.Start();
		}

		#endregion

		private void _load(TkPath currentSearch) {
			try {
				lock (_lock) {
					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;
					_oldPath = _currentPath;

					_labelHeader.Dispatch(p => p.Content = _currentPath.RelativePath);

					List<FileEntry> entries = _grfData.FileTable.EntriesInDirectory(currentSearch.RelativePath, SearchOption.AllDirectories, GrfEditorConfiguration.GrfFileTableIgnoreCase);
					List<FileEntry> entriesRoot = _grfData.FileTable.EntriesInDirectory(currentSearch.RelativePath, SearchOption.TopDirectoryOnly, GrfEditorConfiguration.GrfFileTableIgnoreCase);

					List<long> offsets = entries.Select(p => p.FileExactOffset).ToList();
					List<int> lengths = entries.Select(p => p.SizeCompressedAlignment).ToList();

					List<long> offsetsRoot = entriesRoot.Select(p => p.FileExactOffset).ToList();
					List<int> lengthsRoot = entriesRoot.Select(p => p.SizeCompressedAlignment).ToList();

					long fileSize = 0;

					if (!_grfData.IsNewGrf)
						fileSize = _grfData.GetFileSize();

					if ((int) fileSize == -1)
						return;

					long compSize = 0;
					long decompSize = 0;

					foreach (FileEntry entry in entries) {
						compSize += entry.NewSizeCompressed;
						decompSize += entry.NewSizeDecompressed;
					}

					compSize = decompSize == 0 ? 1 : compSize;
					decompSize = decompSize == 0 ? 1 : decompSize;

					_tbNumOfFiles.Dispatch(p => p.Text = entries.Count + "");
					_tbCompRatio.Dispatch(p => p.Text = String.Format("{0:0.00}", ((float) (decompSize - compSize) / decompSize) * 100f));

					_tbSizeCompressed.Dispatch(p => p.Text = Methods.FileSizeToString(compSize));
					_tbSizeDecompressed.Dispatch(p => p.Text = Methods.FileSizeToString(decompSize));

					_clusterView.DrawBackground();

					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_clusterView.Draw(fileSize, offsets.ToArray(), lengths.ToArray(), Color.FromArgb(255, 207, 218, 238));

					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_clusterView.Draw(fileSize, offsetsRoot.ToArray(), lengthsRoot.ToArray(), Color.FromArgb(255, 239, 239, 239));

					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_clusterView.Draw(fileSize, new long[] { 0 }, new int[] { GrfHeader.DataByteSize }, Color.FromArgb(255, 100, 100, 100));

					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_clusterView.Draw(fileSize, new long[] { _grfData.Header.FileTableOffset }, new int[] { _grfData.FileTable.TableSizeCompressed }, Color.FromArgb(255, 127, 138, 207));

					if (_previewItems.Count != 0 || currentSearch.GetFullPath() != _currentPath.GetFullPath()) return;

					_clusterView.Dispatch(p => p.DrawImage());
				}
			}
			catch (InvalidOperationException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}
	}
}