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

				allMeshData = _gnd.Compile(rsw.Water.Level / 5f, rsw.Water.WaveHeight / 5f, true);
				_meshesDrawer.AddObjectSub(allMeshData.MeshRawData.Values, _modelRotation);

				if (GrfEditorConfiguration.ShowRswObjects) {
					Dictionary<string, Rsm> models = new Dictionary<string, Rsm>();

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

					Dictionary<string, MeshRawData> merged = new Dictionary<string, MeshRawData>();

					foreach (var model in rsw.Objects.OfType<Model>().OrderBy(p => p.ModelName)) {
						var rsm = models[model.ModelName];

						if (rsm != null) {
							Matrix4 m = Matrix4.Identity;

							if (rsm.Header.IsCompatibleWith(2, 2)) {
								// Do nothing
							}
							else {
								m.Offset = new Vertex(0, -rsm.Box.Range[1], 0);
							}

							m = Matrix4.Scale(m, new Vertex(-.2 * model.Scale.X, -.2 * model.Scale.Y, .2 * model.Scale.Z));

							var stuff = rsm.Compile(m, 0);

							m = Matrix4.Identity;

							m = Matrix4.RotateZ(m, ModelViewerHelper.ToRad(model.Rotation.Z));
							m = Matrix4.RotateX(m, -ModelViewerHelper.ToRad(model.Rotation.X));
							m = Matrix4.RotateY(m, -ModelViewerHelper.ToRad(model.Rotation.Y));
							
							Vertex trans = new Vertex(model.Position.X * -.2f, model.Position.Y * -.2f, model.Position.Z * .2f);

							if (trans.X < -_gnd.Header.Width ||
							    trans.X > _gnd.Header.Width ||
							    trans.Z < -_gnd.Header.Height ||
							    trans.Z > _gnd.Header.Height) {
								continue;
							}

							m.Offset += trans;

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
						}
					}

					_meshesDrawer.AddObjectSub(merged.Values, Matrix4.Identity);

					Z.Stop(700, true);
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