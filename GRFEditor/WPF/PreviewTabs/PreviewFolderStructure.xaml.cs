using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
		private readonly PreviewService _previewService;
		private TkPath _currentPath;
		private GrfHolder _grfData;
		private Func<bool> _isCancelRequired;

		private static readonly Color ColorEntriesAll = Color.FromArgb(255, 207, 218, 238);
		private static readonly Color ColorEntriesTopDirectory = Color.FromArgb(255, 239, 239, 239);
		private static readonly Color ColorGrfHeader = Color.FromArgb(255, 100, 100, 100);
		private static readonly Color ColorGrfFileTable = Color.FromArgb(255, 127, 138, 207);

		public PreviewFolderStructure(PreviewService previewService) {
			_previewService = previewService;
			InitializeComponent();
		}

		#region IFolderPreviewTab Members

		public void Load(GrfHolder grfData, TkPath currentPath) {
			if (_currentPath != null && currentPath.GetFullPath() == _currentPath.GetFullPath())
				return;

			_currentPath = currentPath;
			_grfData = grfData;

			if (IsVisible) {
				Update();
			}
		}

		public void Update(bool forceUpdate) {
			Update();
		}

		public void Update() {
			var currentPath = _currentPath;
			_isCancelRequired = () => _previewService.QueueCount != 0 || _currentPath.GetFullPath() != currentPath.GetFullPath();

			Task.Run(delegate {
				try {
					_load(currentPath);
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			});
		}

		#endregion

		private void _load(TkPath currentSearch) {
			try {
				if (_isCancelRequired()) return;

				_setupUI(currentSearch);

				var data = _loadFolderClusterData(currentSearch);

				_displayClusterStats(data);
				_renderClusterView(data);
			}
			catch (InvalidOperationException) {
				// ??
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		private void _setupUI(TkPath currentPath) {
			this.Dispatch(delegate {
				_labelHeader.Text = currentPath.RelativePath;
			});
		}

		private void _renderClusterView(FolderClusterData data) {
			_clusterView.DrawBackground();
			_clusterView.Draw(data.PhysicalFileSize, data.OffsetsAll, data.LengthsAll, ColorEntriesAll);
			_clusterView.Draw(data.PhysicalFileSize, data.OffsetsTopDirectory, data.LengthsTopDirectory, ColorEntriesTopDirectory);
			_clusterView.Draw(data.PhysicalFileSize, new List<long> { 0 }, new List<int> { GrfHeader.DataByteSize }, ColorGrfHeader);
			_clusterView.Draw(data.PhysicalFileSize, new List<long> { _grfData.Header.FileTableOffset }, new List<int> { _grfData.FileTable.TableSizeCompressed }, ColorGrfFileTable);
			_clusterView.Dispatch(p => p.DrawImage());
		}

		private void _displayClusterStats(FolderClusterData data) {
			this.Dispatch(delegate {
				_tbNumOfFilesAll.Text = $"{data.EntriesAll.Count}";
				_tbCompRatioAll.Text = String.Format("{0:0.00}", (1 - (float)data.GrfSizeCompressedAll / Math.Max(1, data.GrfSizeDecompressedAll)) * 100f);
				_tbSizeCompressedAll.Text = Methods.FileSizeToString(data.GrfSizeCompressedAll);
				_tbSizeDecompressedAll.Text = Methods.FileSizeToString(data.GrfSizeDecompressedAll);

				_tbNumOfFilesTopDirectory.Text = $"{data.EntriesTopDirectory.Count}";
				_tbCompRatioTopDirectory.Text = String.Format("{0:0.00}", (1 - (float)data.GrfSizeCompressedTopDirectory / Math.Max(1, data.GrfSizeDecompressedTopDirectory)) * 100f);
				_tbSizeCompressedTopDirectory.Text = Methods.FileSizeToString(data.GrfSizeCompressedTopDirectory);
				_tbSizeDecompressedTopDirectory.Text = Methods.FileSizeToString(data.GrfSizeDecompressedTopDirectory);
			});
		}

		public class FolderClusterData {
			public List<FileEntry> EntriesAll;
			public List<FileEntry> EntriesTopDirectory;
			public List<long> OffsetsAll;
			public List<int> LengthsAll;
			public List<long> OffsetsTopDirectory;
			public List<int> LengthsTopDirectory;
			public long PhysicalFileSize;
			public long GrfSizeCompressedAll;
			public long GrfSizeDecompressedAll;
			public long GrfSizeCompressedTopDirectory;
			public long GrfSizeDecompressedTopDirectory;
		};

		private FolderClusterData _loadFolderClusterData(TkPath currentSearch) {
			FolderClusterData data = new FolderClusterData();
			data.EntriesAll = _grfData.FileTable.EntriesInDirectory(currentSearch.RelativePath, SearchOption.AllDirectories, GrfEditorConfiguration.GrfFileTableIgnoreCase);
			data.EntriesTopDirectory = _grfData.FileTable.EntriesInDirectory(currentSearch.RelativePath, SearchOption.TopDirectoryOnly, GrfEditorConfiguration.GrfFileTableIgnoreCase);

			data.OffsetsAll = data.EntriesAll.Select(p => p.FileExactOffset).ToList();
			data.LengthsAll = data.EntriesAll.Select(p => p.SizeCompressedAlignment).ToList();

			data.OffsetsTopDirectory = data.EntriesTopDirectory.Select(p => p.FileExactOffset).ToList();
			data.LengthsTopDirectory = data.EntriesTopDirectory.Select(p => p.SizeCompressedAlignment).ToList();

			data.PhysicalFileSize = !_grfData.IsNewGrf ? _grfData.GetFileSize() : 0;

			data.GrfSizeCompressedAll = 0;
			data.GrfSizeDecompressedAll = 0;
			data.GrfSizeCompressedTopDirectory = 0;
			data.GrfSizeDecompressedTopDirectory = 0;

			long lastOffset = -1;

			foreach (FileEntry entry in data.EntriesAll) {
				if (lastOffset != entry.FileExactOffset)
					data.GrfSizeCompressedAll += entry.NewSizeCompressed;
				data.GrfSizeDecompressedAll += entry.NewSizeDecompressed;
				lastOffset = entry.FileExactOffset;
			}

			lastOffset = -1;

			foreach (FileEntry entry in data.EntriesTopDirectory) {
				if (lastOffset != entry.FileExactOffset)
					data.GrfSizeCompressedTopDirectory += entry.NewSizeCompressed;
				data.GrfSizeDecompressedTopDirectory += entry.NewSizeDecompressed;
				lastOffset = entry.FileExactOffset;
			}

			return data;
		}
	}
}