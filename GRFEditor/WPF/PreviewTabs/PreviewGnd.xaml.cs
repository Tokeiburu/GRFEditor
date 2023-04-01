using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Graphics;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using GrfToWpfBridge;
using TokeiLibrary;
using TokeiLibrary.WPF.Styles.ListView;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for PreviewGnd.xaml
	/// </summary>
	public partial class PreviewGnd : FilePreviewTab {
		//private int _shader = -1;
		private Gnd _gnd;
		private Matrix4 _modelRotation = Matrix4.Identity;

		public PreviewGnd() {
			InitializeComponent();

			Binder.Bind(_checkBoxObjects, () => GrfEditorConfiguration.ShowRswObjects, () => new Thread(() => _baseLoad(_entry)) { Name = "GrfEditor - IPreview base loading thread" }.Start());

			_isInvisibleResult = () => _meshesDrawer.Dispatch(p => p.Visibility = Visibility.Hidden);

			_checkBoxRotateCamera.Checked += (e, a) => _meshesDrawer.SetCameraState(true, true, null);
			_checkBoxRotateCamera.Unchecked += (e, a) => _meshesDrawer.SetCameraState(false, null, null);
			_meshesDrawer.SetCameraState(false, null, true);
			SettingsDialog.UIPanelPreviewBackgroundPick(_qcsBackground);
			WpfUtils.AddMouseInOutEffectsBox(_checkBoxRotateCamera, _checkBoxObjects);
		}

		public Action<Brush> BackgroundBrushFunction {
			get {
				return v => _grid.Dispatch(p => _grid.Background = v);
			}
		}

		protected override void _load(FileEntry entry) {
			//CLHelper.CReset(-99);

			_meshesDrawer.Dispatch(p => p.Clear());

			string gndFileName = GrfPath.Combine(Path.GetDirectoryName(entry.RelativePath), Path.GetFileNameWithoutExtension(entry.RelativePath)) + ".gnd";
			var tEntry = _grfData.FileTable.TryGet(gndFileName);

			if (tEntry == null) {
				_isInvisibleResult();
				return;
			}

			_gnd = new Gnd(tEntry);

			if (_isCancelRequired()) return;

			_labelHeader.Dispatch(p => p.Content = "Map preview : " + Path.GetFileName(entry.RelativePath));

			_meshesDrawer.Dispatch(p => p.Update(_grfData, _isCancelRequired, Math.Max(_gnd.Header.Height, _gnd.Header.Width) * 2f));

			if (_isCancelRequired()) return;

			_modelRotation = Matrix4.Identity;
			_modelRotation = Matrix4.RotateX(_modelRotation, ModelViewerHelper.ToRad(180));
			_modelRotation = Matrix4.RotateY(_modelRotation, ModelViewerHelper.ToRad(180));
			_modelRotation.Offset += new Vertex(_gnd.Header.Width, 0, -_gnd.Header.Height);

			// Find Rsw file!
			GndMesh allMeshData;

			try {
				string rswName = GrfPath.Combine(GrfPath.GetDirectoryName(entry.RelativePath), Path.GetFileNameWithoutExtension(entry.RelativePath)) + ".rsw";
				Rsw rsw = new Rsw(_grfData.FileTable[rswName].GetDecompressedData());

				//CLHelper.CStart(-11);
				allMeshData = _gnd.Compile(rsw.Water.Level / 5f, rsw.Water.WaveHeight / 5f, true);
				//CLHelper.CStop(-11);

				//CLHelper.CStart(-12);
				_meshesDrawer.AddObjectSub(allMeshData.MeshRawData.Values, _modelRotation);
				//CLHelper.CStop(-12);

				if (GrfEditorConfiguration.ShowRswObjects) {
					Dictionary<string, Rsm> models = new Dictionary<string, Rsm>();

					//CLHelper.CStart(-5);
					foreach (var model in rsw.Objects.OfType<Model>().OrderBy(p => p.ModelName)) {
						var name = model.ModelName;

						if (models.ContainsKey(name))
							continue;

						var rsmEntry = _grfData.FileTable.TryGet("data\\model\\" + name);
						if (rsmEntry != null) {
							var rsm = new Rsm(rsmEntry);
							rsm.CalculateBoundingBox();
							models[name] = rsm;
						}
						else
							models[name] = null;
					}

					//CLHelper.CStop(-5);
					//CLHelper.CReset(-6);
					//CLHelper.CReset(-7);
					//CLHelper.CReset(-8);
					//CLHelper.CReset(-9);

					Dictionary<string, MeshRawData> merged = new Dictionary<string, MeshRawData>();

					foreach (var model in rsw.Objects.OfType<Model>().OrderBy(p => p.ModelName)) {
						var rsm = models[model.ModelName];

						if (rsm != null) {
							//var rsm = new Rsm(rsmEntry);
							//CLHelper.CResume(-6);
							Matrix4 m = Matrix4.Identity;

							if (model.Name.Contains("bathwall_s_08.0") || model.Name.Contains("bathwall_s_08.1")) {
								Z.F();
								//rsm.Header.SetVersion(2, 3);
							}

							if (rsm.Header.IsCompatibleWith(2, 2)) {
								// Do nothing
							}
							else {
								m.Offset = new Vertex(0, -rsm.Box.Range[1], 0);
							}

							m = Matrix4.Scale(m, new Vertex(-.2 * model.Scale.X, -.2 * model.Scale.Y, .2 * model.Scale.Z));

							var stuff = rsm.Compile(m, 0);
							//CLHelper.CStop(-6);

							//CLHelper.CResume(-7);
							m = Matrix4.Identity;

							m = Matrix4.RotateZ(m, ModelViewerHelper.ToRad(model.Rotation.Z));
							m = Matrix4.RotateX(m, -ModelViewerHelper.ToRad(model.Rotation.X));
							m = Matrix4.RotateY(m, -ModelViewerHelper.ToRad(model.Rotation.Y));
							
							Vertex trans = new Vertex(model.Position.X * -.2f, model.Position.Y * -.2f, model.Position.Z * .2f);

							if (trans.X < -_gnd.Header.Width ||
							    trans.X > _gnd.Header.Width ||
							    trans.Z < -_gnd.Header.Height ||
							    trans.Z > _gnd.Header.Height) {
								Z.F();
								continue;
							}

							m.Offset += trans;
							//CLHelper.CStop(-7);

							//CLHelper.CResume(-9);
							//Matrix4 

							Vertex v;
							Vertex v2 = new Vertex();

							foreach (var keyPair in stuff) {
								var item = keyPair.Value;

								for (int mT = 0; mT < item.MeshTriangles.Length; mT++) {
									var mt = item.MeshTriangles[mT];

									for (int j = 0; j < mt.Positions.Length; j++) {
										v = mt.Positions[j];
										v2.X = m[0] * v.X + m[4] * v.Y + m[8] * v.Z + m[12];
										v2.Y = m[1] * v.X + m[5] * v.Y + m[9] * v.Z + m[13];
										v2.Z = m[2] * v.X + m[6] * v.Y + m[10] * v.Z + m[14];
										mt.Positions[j] = v2;
									}
								}

								if (merged.ContainsKey(keyPair.Key)) {
									var newArray = new MeshTriangle[merged[item.Texture].MeshTriangles.Length + item.MeshTriangles.Length];
									Array.Copy(merged[item.Texture].MeshTriangles, newArray, merged[item.Texture].MeshTriangles.Length);
									Array.Copy(item.MeshTriangles, 0, newArray, merged[item.Texture].MeshTriangles.Length, item.MeshTriangles.Length);
									merged[item.Texture].MeshTriangles = newArray;
								}
								else {
									merged[keyPair.Key] = item;
								}
							}

							//CLHelper.CStop(-9);
						}
					}

					//CLHelper.CResume(-8);
					//_meshesDrawer.Dispatch(() => _meshesDrawer.AddObject(items, Matrix4.Identity));
					_meshesDrawer.AddObjectSub(merged.Values, Matrix4.Identity);
					//CLHelper.CStop(-8);

					//CLHelper.CStopAndDisplay("Loading models", -5);
					//CLHelper.CStopAndDisplay("Compiling ground", -11);
					//CLHelper.CStopAndDisplay("Add ground to the world", -12);
					//CLHelper.CStopAndDisplay("Compiling models", -6);
					//CLHelper.CStopAndDisplay("Calculate transform matrix", -7);
					//CLHelper.CStopAndDisplay("Apply transform matrix", -9);
					//CLHelper.CStopAndDisplay("Add models to the world", -8);
					//CLHelper.WL = "";

					//var t = new[] {
					//	"Loading models",
					//	"Compiling ground",
					//	"Add ground to the world",
					//	"Compiling models",
					//	"Calculate transform matrix",
					//	"Apply transform matrix",
					//	"Add models to the world",
					//	"Library conversions"
					//};
					//var n = new[] { -5, -11, -12, -6, -7, -9, -8, -99 };
					//
					//string text = "";
					//for (int i = 0; i < t.Length; i++) {
					//	text += t[i] + " : " + //CLHelper.CDisplay(n[i]) + " ms; \r\n";
					//}
					//
					//ErrorHandler.HandleException(text);
				}
			}
			catch (Exception err) {
				Console.WriteLine(err.ToString());
				ErrorHandler.HandleException(err);
				allMeshData = _gnd.Compile(0, 0, true);
				_meshesDrawer.AddObject(allMeshData.MeshRawData.Values.ToList(), _modelRotation);
			}


			if (_isCancelRequired()) return;

			_meshesDrawer.Dispatch(p => p.UpdateCamera());
			_meshesDrawer.Dispatch(p => p.Visibility = Visibility.Visible);
		}
	}
}