using System;
using System.Collections;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.IO;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.Application;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary.WPF;
using Utilities.Extension;

namespace GRFEditor.Core.Services {
	public class ExtractingService {
		private readonly AsyncOperation _asyncOperation;

		public ExtractingService(AsyncOperation asyncOperation) {
			_asyncOperation = asyncOperation;

			if (String.IsNullOrEmpty(GrfEditorConfiguration.ExtractingServiceLastPath)) {
				GrfEditorConfiguration.ExtractingServiceLastPath = GrfEditorConfiguration.DefaultExtractingPath;
			}
		}

		#region Internal methods

		private string _getCurrentPath(object selectedItem) {
			if (selectedItem == null) return null;

			TkTreeViewItem item = selectedItem as TkTreeViewItem;

			if (item != null) {
				return TreeViewPathManager.GetTkPath(selectedItem).RelativePath;
			}

			return null;
		}

		private bool _isValid(bool showErrors, object selectedItem) {
			if (selectedItem == null) {
				if (showErrors) ErrorHandler.HandleException("Please select a folder first.", ErrorLevel.Low);
				return false;
			}

			if (selectedItem is IList && (selectedItem as IList).Count == 0) {
				if (showErrors) ErrorHandler.HandleException("Please select a file first.", ErrorLevel.Low);
				return false;
			}

			return true;
		}

		#endregion

		#region Public methods
		private ExtractOptions _getOptions() {
			return 
				(GrfEditorConfiguration.AlwaysOpenAfterExtraction ? ExtractOptions.OpenAfterExtraction : ExtractOptions.Normal) |
				(GrfEditorConfiguration.OverrideExtractionPath ? ExtractOptions.UseAppDataPathToExtract : ExtractOptions.Normal);
		}

		// TreeView - Extract folder
		public void FolderExtract(GrfHolder grfData, object selectedItem, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItem))
					return;

				_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(null, _getCurrentPath(selectedItem), SearchOption.AllDirectories, null, GrfEditorConfiguration.DefaultExtractingPath, _getOptions(), SyncMode.Synchronous), grfData, 300, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		// TreeView - Extract folder at...
		public void FolderExtractTo(GrfHolder grfData, object selectedItem, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItem))
					return;

				string path = PathRequest.FolderExtract();

				if (path != null) {
					_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(path, _getCurrentPath(selectedItem), SearchOption.AllDirectories, null, GrfEditorConfiguration.DefaultExtractingPath, _getOptions(), SyncMode.Synchronous), grfData, 300, null, true));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		// TreeView - Extract root files
		public void RootFilesExtract(GrfHolder grfData, object selectedItem, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItem))
					return;

				_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(null, _getCurrentPath(selectedItem), SearchOption.TopDirectoryOnly, null, GrfEditorConfiguration.DefaultExtractingPath, _getOptions(), SyncMode.Synchronous), grfData, 300, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		// TreeView - Extract root files at...
		public void RootFilesExtractTo(GrfHolder grfData, object selectedItem, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItem))
					return;

				string path = PathRequest.FolderExtract();

				if (path != null) {
					_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(path, _getCurrentPath(selectedItem), SearchOption.TopDirectoryOnly, null, GrfEditorConfiguration.DefaultExtractingPath, _getOptions(), SyncMode.Synchronous), grfData, 300, null, true));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		// ListBox - Extract files
		public void FileExtract(GrfHolder grfData, IList selectedItems, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItems))
					return;

				_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(null, Path.GetDirectoryName(((FileEntry)selectedItems[0]).RelativePath), SearchOption.TopDirectoryOnly, selectedItems.OfType<FileEntry>(), GrfEditorConfiguration.DefaultExtractingPath, _getOptions(), SyncMode.Synchronous), grfData, 300, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		// ListBox - Extract files at...
		public void FileExtractTo(GrfHolder grfData, IList selectedItems, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItems) || selectedItems.Count == 0)
					return;

				string path = PathRequest.FolderExtract();

				if (path != null) {
					_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(path, Path.GetDirectoryName(((FileEntry)selectedItems[0]).RelativePath), SearchOption.TopDirectoryOnly, selectedItems.OfType<FileEntry>(), GrfEditorConfiguration.DefaultExtractingPath, _getOptions(), SyncMode.Synchronous), grfData, 300, null, true));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		public void ExportAsThor(GrfHolder grfData, IList selectedItems, bool showErrors = true) {
			try {
				if (!_isValid(showErrors, selectedItems) || selectedItems.Count == 0)
					return;

				string path = PathRequest.SaveFileEditor(
					"filter", FileFormat.MergeFilters(Format.Grf | Format.Gpf | Format.Thor), 
					"fileName", Path.GetFileName(grfData.FileName));

				if (path != null) {
					var grf = new GrfHolder(path, GrfLoadOptions.New);

					_asyncOperation.SetAndRunOperation(new GrfThread(() => {
						try {
							using (grf) {
								grf.Attached["Thor.UseGrfMerging"] = true;
								bool isThor = path.IsExtension(".thor");

								foreach (var entry in selectedItems.Cast<FileEntry>()) {
									string outputPath = entry.RelativePath;

									if (isThor) {
										outputPath = outputPath.StartsWith("root\\") ? outputPath : "root\\" + outputPath;
									}
									else {
										if (outputPath.StartsWith("root\\")) {
											outputPath = outputPath.ReplaceFirst("root\\", "");
										}
									}

									grf.Commands.AddFile(outputPath, entry);
								}

								grf.Save(path, SyncMode.Synchronous);

								if (!grf.CancelReload)
									Utilities.Services.OpeningService.FileOrFolder(path);
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}, grf, 300, null, true));
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}
		
		#endregion

		public void ExtractAll(GrfHolder grfData, string extractionPath) {
			try {
				_asyncOperation.SetAndRunOperation(new GrfThread(() => grfData.Extract(extractionPath, "", SearchOption.AllDirectories, grfData.FileTable.Entries,
 					null,
					ExtractOptions.Normal | ExtractOptions.OverrideCpuPerf, SyncMode.Synchronous), grfData, 200, null, true));
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.NotSpecified);
			}
		}
	}
}