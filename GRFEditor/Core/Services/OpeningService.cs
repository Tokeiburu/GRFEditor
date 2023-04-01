using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary.WPF;

namespace GRFEditor.Core.Services {
	public enum OpeningAction {
		Run,
		None,
		SelectFileInExplorer,
		SelectFolderInExplorer,
		OpenFolder
	}

	public class OpeningService {
		public static bool Enabled;
		private string _fileToOpen = "";

		public void OpenSelectedFile(FileEntry entry, OpeningAction action) {
			try {
				if (entry == null) {
					ErrorHandler.HandleException("Please select a file.", ErrorLevel.Low);
					return;
				}

				switch (action) {
					case OpeningAction.Run:
						if (File.Exists(_fileToOpen)) {
							Process.Start(_fileToOpen);
						}
						break;
					case OpeningAction.SelectFileInExplorer:
						Utilities.Services.OpeningService.FileOrFolder(_fileToOpen);
						break;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Low);
			}
		}

		public void RunSelected(GrfHolder grfData, ExtractingService extractingService, FileEntry entry, OpeningAction action = OpeningAction.Run) {
			try {
				if (entry == null) {
					ErrorHandler.HandleException("Please select a file.", ErrorLevel.Low);
					return;
				}

				_fileToOpen = GrfPath.Combine(GrfEditorConfiguration.OverrideExtractionPath ?  GrfEditorConfiguration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(grfData.FileName).FullName), Rgz.GetRealFilename(entry.RelativePath));

				if (!File.Exists(_fileToOpen)) {
					if (WindowProvider.ShowDialog("The file hasn't been extracted yet, would you like to do that now?", "File not extracted",
					                              MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
						extractingService.FileExtract(grfData, new FileEntry[] { entry }, false);
					}
				}
				else {
					OpenSelectedFile(entry, action);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err, ErrorLevel.Warning);
			}
		}

		public void OpenSelectedInExplorer(GrfHolder grfData, ExtractingService extractingService, FileEntry entry) {
			RunSelected(grfData, extractingService, entry, OpeningAction.SelectFileInExplorer);
		}

		public void OpenSelectedFolder(GrfHolder grfData, TreeView tree, ExtractingService extractingService) {
			if (tree.SelectedItem == null) {
				ErrorHandler.HandleException("Please select a folder.", ErrorLevel.Low);
				return;
			}

			_fileToOpen = GrfPath.Combine(GrfEditorConfiguration.OverrideExtractionPath ? GrfEditorConfiguration.DefaultExtractingPath : Path.GetDirectoryName(new FileInfo(grfData.FileName).FullName), Rgz.GetRealFilename(TreeViewPathManager.GetTkPath(tree.SelectedItem).RelativePath));

			if (!Directory.Exists(_fileToOpen)) {
				if (WindowProvider.ShowDialog("The folder hasn't been extracted yet, would you like to do that now?", "Folder not extracted",
				                              MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
					extractingService.RootFilesExtract(grfData, tree.SelectedItem);
				}
			}
			else {
				Utilities.Services.OpeningService.FileOrFolder(_fileToOpen);
			}
		}
	}
}