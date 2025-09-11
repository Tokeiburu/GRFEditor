using System;
using System.Collections.Generic;
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
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Graphics;
using GRF.IO;
using GRF.GrfSystem;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.OpenGL;
using GrfToWpfBridge.TreeViewManager;
using OpenTK;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities.Extension;
using System.Linq;
using Gnd = GRF.FileFormats.GndFormat.Gnd;
using Matrix4 = OpenTK.Matrix4;
using Rsm = GRF.FileFormats.RsmFormat.Rsm;

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

				InputDialog input = WindowProvider.ShowWindow<InputDialog>(new InputDialog("The current file name is: \n" + Path.GetFileName(entry.RelativePath), "Rename", Path.GetFileName(entry.RelativePath), true), owner);

				if (input.DialogResult == true) {
					grfData.Commands.Rename(entry.RelativePath, GrfPath.Combine(GrfPath.GetDirectoryName(entry.RelativePath), input.Input), callback);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public bool DowngradeMap(FileEntry entry, GrfHolder grfData, Window owner, string outputPath = null) {
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

				if (!grfData.FileTable.ContainsFile(entry.RelativePath.ReplaceExtension(".rsw"))) {
					ErrorHandler.HandleException("Couldn't find the associated RSW map file.");
					return false;
				}

				try {
					if (outputPath == null) {
						grfData.Commands.Begin();
					}

					Rsw rsw = new Rsw(grfData.FileTable[entry.RelativePath.ReplaceExtension(".rsw")].GetDecompressedData());
					Gnd gnd = new Gnd(grfData.FileTable[entry.RelativePath.ReplaceExtension(".gnd")].GetDecompressedData());

					string newFileMapName = Path.GetFileNameWithoutExtension(entry.RelativePath);
					rsw.Header.SetVersion(2, 1);
					rsw.Header.GroundFile = newFileMapName + ".gnd";
					rsw.Header.AltitudeFile = newFileMapName + ".gat";

					gnd.Header.SetVersion(1, 7);

					if (gnd.Water.Zones.Count > 0) {
						rsw.Water = gnd.Water.Zones[0];
					}

					string rswTempFile = TemporaryFilesManager.GetTemporaryFilePath("map_{0:000000}.rsw");
					string gndTempFile = TemporaryFilesManager.GetTemporaryFilePath("map_{0:000000}.gnd");
					string gatTempFile = TemporaryFilesManager.GetTemporaryFilePath("map_{0:000000}.gat");

					HashSet<string> convertedModels = new HashSet<string>();
					Dictionary<string, OpenGL.MapComponents.Rsm> rsm2 = new Dictionary<string, OpenGL.MapComponents.Rsm>(StringComparer.OrdinalIgnoreCase);
					Dictionary<string, OpenGL.MapComponents.Rsm> rsm1 = new Dictionary<string, OpenGL.MapComponents.Rsm>(StringComparer.OrdinalIgnoreCase);

					foreach (var model in rsw.Objects.OfType<Model>()) {
						if (model.ModelName.IsExtension(".rsm2")) {
							var entryModel = GrfEditorConfiguration.Resources.MultiGrf.GetData(Rsm.RsmModelPath + "\\" + model.ModelName);

							if (entryModel != null) {
								if (convertedModels.Add(model.ModelName)) {
									Rsm rsm = new Rsm(entryModel);
									OpenGL.MapComponents.Rsm rsm_2 = new OpenGL.MapComponents.Rsm(entryModel);

									if (rsm_2.Meshes.Any(p => p.RotationKeyFrames.Count > 0)) {
										rsm.Downgrade();
									}
									else {
										rsm_2.MainMesh.FlattenModel(Matrix4.Identity);

										for (int i = 0; i < rsm.Meshes.Count; i++) {
											rsm.Meshes[i].Vertices.Clear();
											rsm.Meshes[i].Vertices.AddRange(rsm_2.Meshes[i].Vertices.Select(p => new TkVector3(p.X, p.Y, p.Z)).ToList());
										}

										rsm.Flatten();
									}

									string rsmTempFile = TemporaryFilesManager.GetTemporaryFilePath("rsm_{0:000000}.rsm");
									rsm.Save(rsmTempFile);
									rsm2[model.ModelName] = rsm_2;
									OpenGL.MapComponents.Rsm rsm_1 = new OpenGL.MapComponents.Rsm(rsmTempFile);
									rsm1[model.ModelName] = rsm_1;

									if (outputPath == null) {
										grfData.Commands.AddFile(Rsm.RsmModelPath + "\\" + model.ModelName.ReplaceExtension(".rsm"), rsmTempFile);
									}
									else {
										GrfPath.Copy(rsmTempFile, GrfPath.Combine(outputPath, Rsm.RsmModelPath + "\\" + model.ModelName.ReplaceExtension(".rsm")));
									}
								}

								{
									var rsm_2 = rsm2[model.ModelName];
									//var rsm_1 = rsm1[model.ModelName];
									var m2 = _calcRenderMatrix(model, gnd, rsm_2);
									//var m1 = _calcRenderMatrix(model, gnd, rsm_1);
									var b2 = rsm_2.RealBox;
									//var b1 = rsm_1.RealBox;

									model.ModelName = model.ModelName.ReplaceExtension(".rsm");
									model.Position = new TkVector3(
										m2.Row3.X + b2.Center.X * (model.Scale.X < 0 ? -1 : 1), 
										-m2.Row3.Y - b2.Min.Y, 
										-m2.Row3.Z + b2.Center.Z);

									var MatrixCache = Matrix4.Identity;
									MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(1, 1, -1));
									MatrixCache = GLHelper.Rotate(MatrixCache, GLHelper.ToRad(model.Rotation.Z), new Vector3(0, 0, 1));
									MatrixCache = GLHelper.Rotate(MatrixCache, -GLHelper.ToRad(model.Rotation.X), new Vector3(1, 0, 0));
									MatrixCache = GLHelper.Rotate(MatrixCache, -GLHelper.ToRad(model.Rotation.Y), new Vector3(0, 1, 0));
									MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(model.Scale.X, model.Scale.Y, model.Scale.Z));

									model.Position = new TkVector3(
										m2.Row3.X,
										-m2.Row3.Y,
										-m2.Row3.Z);

									var offset = new Vector4(b2.Center.X, -b2.Min.Y, -b2.Center.Z, 1);
									offset = offset * MatrixCache;

									var result = new Vector3(offset.X, offset.Y, offset.Z);

									model.Position += new TkVector3(result.X, result.Y, result.Z);

									model.Scale *= new TkVector3(-1, 1, 1);
								}
							}
						}
					}

					rsw.Save(rswTempFile);
					gnd.Save(gndTempFile);
					File.WriteAllBytes(gatTempFile, grfData.FileTable[entry.RelativePath.ReplaceExtension(".gat")].GetDecompressedData());

					if (outputPath == null) {
						grfData.Commands.AddFile(String.Format(@"data\{0}{1}", newFileMapName, ".rsw"), rswTempFile);
						grfData.Commands.AddFile(String.Format(@"data\{0}{1}", newFileMapName, ".gnd"), gndTempFile);
						grfData.Commands.AddFile(String.Format(@"data\{0}{1}", newFileMapName, ".gat"), gatTempFile);
					}
					else {
						GrfPath.Copy(rswTempFile, GrfPath.Combine(outputPath, Path.GetFileNameWithoutExtension(entry.RelativePath) + ".rsw"));
						GrfPath.Copy(gndTempFile, GrfPath.Combine(outputPath, Path.GetFileNameWithoutExtension(entry.RelativePath) + ".gnd"));
						GrfPath.Copy(gatTempFile, GrfPath.Combine(outputPath, Path.GetFileNameWithoutExtension(entry.RelativePath) + ".gat"));
					}
				}
				catch {
					if (outputPath == null) {
						grfData.Commands.CancelEdit();
					}
					throw;
				}
				finally {
					if (outputPath == null) {
						grfData.Commands.End();
					}
				}

				return true;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}

			return false;
		}

		private Matrix4 _calcRenderMatrix(Model model, Gnd gnd, OpenGL.MapComponents.Rsm rsm) {
			var MatrixCache = Matrix4.Identity;
			MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(1, 1, -1));

			MatrixCache = GLHelper.Translate(MatrixCache, new Vector3(model.Position.X, -model.Position.Y, model.Position.Z));
			MatrixCache = GLHelper.Rotate(MatrixCache, -GLHelper.ToRad(model.Rotation.Z), new Vector3(0, 0, 1));
			MatrixCache = GLHelper.Rotate(MatrixCache, -GLHelper.ToRad(model.Rotation.X), new Vector3(1, 0, 0));
			MatrixCache = GLHelper.Rotate(MatrixCache, GLHelper.ToRad(model.Rotation.Y), new Vector3(0, 1, 0));
			MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(model.Scale.X, -model.Scale.Y, model.Scale.Z));

			if (rsm.Version < 2.2) {
				MatrixCache = GLHelper.Translate(MatrixCache, new Vector3(-rsm.RealBox.Center.X, rsm.RealBox.Min.Y, -rsm.RealBox.Center.Z));
			}
			else {
				MatrixCache = GLHelper.Scale(MatrixCache, new Vector3(1, -1, 1));
			}

			return MatrixCache;
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

				InputDialog input = WindowProvider.ShowWindow<InputDialog>(new InputDialog("The current file map name is: \n" + fileMapName, "Save map as...", fileMapName, true), owner);

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

		public bool DowngradeModel(List<FileEntry> entries, GrfHolder grfData, Window owner, bool flatten) {
			try {
				int converted = 0;

				if (entries.Count == 0 || (entries.Count ==  1 && entries[0] == null)) {
					ErrorHandler.HandleException("Please select a file.", ErrorLevel.Low);
					return false;
				}

				grfData.Commands.Begin();

				foreach (var entry in entries) {
					string extension = entry.RelativePath.GetExtension();

					if (extension != ".rsm2") {
						continue;
					}

					Rsm rsm_n = new Rsm(entry);

					if (flatten) {
						OpenGL.MapComponents.Rsm rsm = new OpenGL.MapComponents.Rsm(entry);
						rsm.MainMesh.FlattenModel(Matrix4.Identity);

						for (int i = 0; i < rsm.Meshes.Count; i++) {
							rsm_n.Meshes[i].Vertices.Clear();
							rsm_n.Meshes[i].Vertices.AddRange(rsm.Meshes[i].Vertices.Select(p => new TkVector3(p.X, p.Y, p.Z)).ToList());
						}

						rsm_n.Flatten();
					}
					else {
						rsm_n.Downgrade();
					}

					string rsmTempFile = TemporaryFilesManager.GetTemporaryFilePath("rsm_{0:000000}.rsm");
					rsm_n.Save(rsmTempFile);
					grfData.Commands.AddFile(entry.RelativePath.ReplaceExtension(".rsm"), rsmTempFile);
					converted++;
				}

				if (converted == 0) {
					ErrorHandler.HandleException("The selected entry doesn't appear to be a valid RSM2 model (invalid extension).");
				}

				return converted != 0;
			}
			catch (Exception err) {
				grfData.Commands.CancelEdit();
				ErrorHandler.HandleException(err);
			}
			finally {
				grfData.Commands.End();
			}

			return false;
		}
	}
}