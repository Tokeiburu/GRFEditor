using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.ContainerFormat.Commands;
using GRF.Core;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.IO;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge.TreeViewManager;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities.Extension;
using System.Linq;

namespace GRFEditor.Core.Services {
	public class RenamingService {
		public void RenameFolder(object selectedItem, TreeView _treeView, GrfHolder grfData, Window owner, CCallbacks.RenameCallback callback, CCallbacks.DeleteCallback callbackDelete, CCallbacks.AddFilesCallback callbackAddFiles) {
			if (_treeView.SelectedItem == null) {
				ErrorHandler.HandleException("Please select a folder.", ErrorLevel.Low);
			}
			else if (_treeView.SelectedItem is ProjectTreeViewItem) {
				ErrorHandler.HandleException("Only folders can be renamed.", ErrorLevel.Low);
			}
			else if (_treeView.SelectedItem is TkTreeViewItem) {
				var tkTvi = ((TkTreeViewItem)_treeView.SelectedItem);

				tkTvi.IsEditMode = true;

				TextBox tb = null;

				var isFirst = tkTvi.Get("_tbEdit", out tb);

				tkTvi.CurrentPath = TreeViewPathManager.GetTkPath(_treeView.SelectedItem).RelativePath;
				tkTvi.OldHeaderText = tkTvi.HeaderText;
				tb.Text = tkTvi.HeaderText;
				Keyboard.Focus(tb);
				tb.Focus();
				tb.SelectAll();

				if (isFirst) {
					tb.LostKeyboardFocus += delegate {
						if (!tkTvi.IsEditMode) {
							tkTvi.HeaderText = tkTvi.OldHeaderText;
							return;
						}

						tkTvi.IsEditMode = false;

						try {
							foreach (char c in Path.GetInvalidFileNameChars()) {
								if (tb.Text.Contains(c)) {
									WindowProvider.ShowDialog("Invalid characters.");
									return;
								}
							}

							try {
								grfData.Commands.Rename(tkTvi.CurrentPath, Path.Combine(GrfPath.GetDirectoryName(tkTvi.CurrentPath), tb.Text), callback);
							}
							catch (GrfException grfErr) {
								if (grfErr == GrfExceptions.__FolderNameAlreadyExists) {
									if (WindowProvider.ShowDialog("A folder with this name already exists, do you want to merge them?", "Folder already exists", MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes) {
										grfData.Commands.MergeFolders(tkTvi.CurrentPath, Path.Combine(GrfPath.GetDirectoryName(tkTvi.CurrentPath), tb.Text), callbackDelete, callbackAddFiles);
									}
								}
								else throw;
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
							}
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err, ErrorLevel.Warning);
						}

						//RenameFolder(_treeView.SelectedItem, tkTvi.CurrentPath, tb.Text, grfData, this, _renameFolderCallback, _deleteFolderCallback, _addFilesCallback);
					};

					tb.PreviewTextInput += (s, g) => {
						foreach (char c in Path.GetInvalidFileNameChars()) {
							if (g.Text.Contains(c)) {
								g.Handled = true;
								ToolTip tooltip = new ToolTip { Content = "Invalid character: " + c };
								tb.ToolTip = tooltip;
								tooltip.PlacementTarget = tb;
								tooltip.Placement = PlacementMode.Bottom;
								tooltip.Opened += delegate {
									GrfThread.Start(delegate {
										Thread.Sleep(1500);
										tb.Dispatch(delegate {
											tooltip.IsOpen = false;
											tb.ToolTip = null;
										});
									});
								};
								tooltip.IsOpen = true;
								break;
							}
						}
					};

					tb.KeyDown += (s, g) => {
						if (!tkTvi.IsEditMode)
							return;

						if (g.Key == Key.Enter) {
							tkTvi.Focus();
						}
						else if (g.Key == Key.Escape) {
							tkTvi.IsEditMode = false;
							tkTvi.Focus();
						}
					};
				}
			}
		}

		public void RenameFile(FileEntry entry, GrfHolder grfData, Window owner, CCallbacks.RenameCallback callback) {
			try {
				if (entry == null) {
					ErrorHandler.HandleException("Please select a file.", ErrorLevel.Low);
					return;
				}

				InputDialog input = WindowProvider.ShowWindow<InputDialog>(new InputDialog("The current file name is : \n" + Path.GetFileName(entry.RelativePath), "Rename", Path.GetFileName(entry.RelativePath), true), owner);

				if (input.DialogResult == true) {
					grfData.Commands.Rename(entry.RelativePath, GrfPath.Combine(GrfPath.GetDirectoryName(entry.RelativePath), input.Input), callback);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public bool SaveMap(FileEntry entry, GrfHolder grfData, Window owner) {
			try {
				if (entry == null) {
					ErrorHandler.HandleException("Please select a file.", ErrorLevel.Low);
					return false;
				}

				string extension = entry.RelativePath.GetExtension();

				if (!(extension == ".rsw" || extension == ".gnd" || extension == ".gat")) {
					ErrorHandler.HandleException("The selected entry doesn't appear to be a map file (invalid extension).");
					return false;
				}

				string fileMapName = Path.GetFileNameWithoutExtension(entry.RelativePath);

				if (!grfData.FileTable.ContainsFile(entry.RelativePath.ReplaceExtension(".rsw"))) {
					ErrorHandler.HandleException("Couldn't find the associated RSW map file.");
					return false;
				}

				Rsw rsw = new Rsw(grfData.FileTable[entry.RelativePath.ReplaceExtension(".rsw")].GetDecompressedData());

				InputDialog input = WindowProvider.ShowWindow<InputDialog>(new InputDialog("The current file map name is : \n" + fileMapName, "Save map as...", fileMapName, true), owner);

				if (input.DialogResult == true) {
					if (String.IsNullOrEmpty(input.Input)) {
						ErrorHandler.HandleException("Invalid file name.", ErrorLevel.Low);
						return false;
					}

					string newFileMapName = input.Input;
					rsw.Header.GroundFile = newFileMapName + ".gnd";
					rsw.Header.AltitudeFile = newFileMapName + ".gat";

					string rswTempFile = TemporaryFilesManager.GetTemporaryFilePath("map_{0:000000}.rsw");
					string gatTempFile = TemporaryFilesManager.GetTemporaryFilePath("map_{0:000000}.gat");
					string gndTempFile = TemporaryFilesManager.GetTemporaryFilePath("map_{0:000000}.gnd");

					rsw.Save(rswTempFile);
					File.WriteAllBytes(gatTempFile, grfData.FileTable[entry.RelativePath.ReplaceExtension(".gat")].GetDecompressedData());
					File.WriteAllBytes(gndTempFile, grfData.FileTable[entry.RelativePath.ReplaceExtension(".gnd")].GetDecompressedData());

					try {
						grfData.Commands.Begin();
						grfData.Commands.AddFile(String.Format(@"data\{0}{1}", newFileMapName, ".rsw"), rswTempFile);
						grfData.Commands.AddFile(String.Format(@"data\{0}{1}", newFileMapName, ".gat"), gatTempFile);
						grfData.Commands.AddFile(String.Format(@"data\{0}{1}", newFileMapName, ".gnd"), gndTempFile);
					}
					catch {
						grfData.Commands.CancelEdit();
					}
					finally {
						grfData.Commands.End();
					}

					return true;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return false;
		}

		public bool DowngradeModel(FileEntry entry, GrfHolder grfData, Window owner) {
			try {
				if (entry == null) {
					ErrorHandler.HandleException("Please select a file.", ErrorLevel.Low);
					return false;
				}

				string extension = entry.RelativePath.GetExtension();

				if (extension != ".rsm2") {
					ErrorHandler.HandleException("The selected entry doesn't appear to be a valid RSM2 model (invalid extension).");
					return false;
				}

				Rsm rsm = new Rsm(entry);
				rsm.Downgrade();

				string rsmTempFile = TemporaryFilesManager.GetTemporaryFilePath("rsm_{0:000000}.rsm");

				rsm.Save(rsmTempFile);

				try {
					grfData.Commands.Begin();
					grfData.Commands.AddFile(entry.RelativePath.ReplaceExtension(".rsm"), rsmTempFile);
				}
				catch {
					grfData.Commands.CancelEdit();
				}
				finally {
					grfData.Commands.End();
				}

				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return false;
		}
	}
}