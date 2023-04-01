using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using ErrorManager;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.Graphics;
using GRF.Image;
using GRF.Image.Decoders;
using GrfToWpfBridge;
using TokeiLibrary;
using Utilities;
using Utilities.CommandLine;
using Point = System.Windows.Point;

namespace GRFEditor.WPF.PreviewTabs {
	/// <summary>
	/// Interaction logic for MeshesDrawer.xaml
	/// </summary>
	public partial class MeshesDrawer : UserControl, IDisposable {
		private double _angleXRad;
		private double _angleYDegree = 20;
		private Vector3D _cameraPosition = new Vector3D(0, 0, 5);
		private double _distance = 5;
		private MultiGrfReader _grfData;
		private Func<bool> _isCancelRequired = new Func<bool>(() => false);
		private bool _isRotatingCameraActivated;
		private bool _isRotatingCameraChecked2 = true;
		private long _lastElapsedMs;
		private Vector3D _lookAt = new Vector3D(2.5, 0.5, 0.5);
		private Point _oldPosition = new Point(0, 0);
		private bool _reactivateRotatingCamera = true;
		private bool _resetCameraDistance = true;
		private bool _timerThreadRun = true;

		public MeshesDrawer() {
			InitializeComponent();

			_primaryGrid.MouseMove += new MouseEventHandler(_3DTests_MouseMove);
			_primaryGrid.MouseUp += new MouseButtonEventHandler(_primaryGrid_MouseUp);
			_primaryGrid.KeyDown += new KeyEventHandler(_3DTests_KeyDown);
			_primaryGrid.MouseWheel += new MouseWheelEventHandler(_3DTests_MouseWheel);

			Dispatcher.ShutdownStarted += (e, a) => _timerThreadRun = false;

			_modelLight2.Content = new AmbientLight(Color.FromArgb(127, 127, 127, 127));
			new Thread(_timerThread) { Name = "GrfEditor - Camera rotation update thread" }.Start();
		}

		public GrfColor BackgroundColor {
			set { _backgroundGrid.Background = new SolidColorBrush(value.ToColor()); }
		}

		#region IDisposable Members

		public void Dispose() {
			if (_viewport3D1 != null) {
				_viewport3D1.Children.Clear();
				_viewport3D1 = null;
			}

			_timerThreadRun = false;
		}

		#endregion

		private void _primaryGrid_MouseUp(object sender, MouseButtonEventArgs e) {
			_primaryGrid.ReleaseMouseCapture();
		}

		private void _timerThread() {
			Stopwatch watch = new Stopwatch();

			while (_timerThreadRun) {
				Thread.Sleep(50);

				if (_isRotatingCameraChecked2 && _isRotatingCameraActivated) {
					_angleXRad += 0.03 * (50.0 + _lastElapsedMs) / 50.0;

					watch.Reset();
					watch.Start();
					UpdateCamera();
					watch.Stop();
					_lastElapsedMs = watch.ElapsedMilliseconds;
				}
			}
		}

		public void Update(MultiGrfReader grfData, Func<bool> cancelRequired, float distance) {
			ClearBufferedTextures();

			_isCancelRequired = cancelRequired;
			_grfData = grfData;

			if (_isCancelRequired()) return;

			_lookAt = new Vector3D(0, 0, 0);

			if (_resetCameraDistance)
				_distance = distance;
			else
				_resetCameraDistance = true;

			if (_reactivateRotatingCamera)
				_isRotatingCameraActivated = true;
			else
				_reactivateRotatingCamera = true;

			UpdateCamera();
		}

		private void _3DTests_MouseWheel(object sender, MouseWheelEventArgs e) {
			_isRotatingCameraActivated = false;
			float delta = e.Delta / 20f;
			_distance -= delta;
			UpdateCamera();
		}

		private void _3DTests_KeyDown(object sender, KeyEventArgs e) {
			_isRotatingCameraActivated = false;
			if (e.Key == Key.Right) {
				_angleXRad += 0.04;
			}
			if (e.Key == Key.Left) {
				_angleXRad -= 0.04;
			}
			if (e.Key == Key.Up) {
				_angleYDegree += 0.04;
			}
			if (e.Key == Key.Down) {
				_angleYDegree -= 0.04;
			}

			UpdateCamera();
		}

		private void _3DTests_MouseMove(object sender, MouseEventArgs e) {
			Point newMousePosition = e.GetPosition(_viewport3D1);

			if (e.LeftButton == MouseButtonState.Pressed) {
				if (!_primaryGrid.IsMouseCaptured)
					_primaryGrid.CaptureMouse();

				_isRotatingCameraActivated = false;
				double deltaX = ModelViewerHelper.ToRad(newMousePosition.X - _oldPosition.X);
				_angleXRad -= deltaX;

				double deltaY = newMousePosition.Y - _oldPosition.Y;
				_angleYDegree += deltaY;

				UpdateCamera();
			}
			else if (e.RightButton == MouseButtonState.Pressed) {
				if (!_primaryGrid.IsMouseCaptured)
					_primaryGrid.CaptureMouse();

				_isRotatingCameraActivated = false;

				double deltaX = newMousePosition.X - _oldPosition.X;
				double deltaZ = newMousePosition.Y - _oldPosition.Y;

				double distX = _distance * 0.0013 * deltaX;
				double distZ = _distance * 0.0013 * deltaZ;

				_lookAt.X += -distX * Math.Cos(_angleXRad) - distZ * Math.Sin(_angleXRad); // * Math.Sin(yRad);
				_lookAt.Z += distX * Math.Sin(_angleXRad) - distZ * Math.Cos(_angleXRad); //*Math.Sin(yRad);

				UpdateCamera();
			}

			_oldPosition = newMousePosition;
		}

		public void UpdateCamera() {
			Dispatcher.Invoke(new Action(delegate {
				FilePreviewTab parent = WpfUtilities.FindParentControl<FilePreviewTab>(this);

				if (parent != null && !parent.IsVisible) {
					_isRotatingCameraActivated = false;
				}

				if (_isCancelRequired()) {
					_isRotatingCameraActivated = false;
					return;
				}

				_angleYDegree = _angleYDegree > 89 ? 89 : _angleYDegree;
				_angleYDegree = _angleYDegree < -89 ? -89 : _angleYDegree;

				//_angleX = _angleX > 360f ? _angleX - 360f * (int) (_angleX / 360) : _angleX;
				//_primaryCamera.Width = _distance;

				double subDistance = _distance * Math.Cos(ModelViewerHelper.ToRad(_angleYDegree));
				_cameraPosition.Y = _distance * Math.Sin(ModelViewerHelper.ToRad(_angleYDegree));

				_cameraPosition.X = subDistance * Math.Sin(_angleXRad);
				_cameraPosition.Z = subDistance * Math.Cos(_angleXRad);
				_cameraPosition += _lookAt;

				_primaryCamera.Position = new Point3D(_cameraPosition.X, _cameraPosition.Y, _cameraPosition.Z);
				_primaryCamera.LookDirection = _lookAt - _cameraPosition;

				if (_modelLight.Content is DirectionalLight) {
					((DirectionalLight) _modelLight.Content).Direction = _lookAt - _cameraPosition;
				}
			}));
		}

		public void AddObject(List<MeshRawData> meshData, Matrix4 matrix) {
			meshData.Sort(new TextureMeshComparer());
			AddObjectSub(meshData, matrix);
		}

		public void AddObjectSub(IEnumerable<MeshRawData> meshData, Matrix4 matrix) {
			if (_isCancelRequired()) return;

			MeshTriangle triangle;

			foreach (MeshRawData mesh in meshData) {
				if (_isCancelRequired()) return;

				MeshTriangle[] meshTriangles = mesh.MeshTriangles.Where(p => p != null).ToArray();
				Point3D[] tPositions = new Point3D[3 * meshTriangles.Length];
				Vector3D[] tNormals = new Vector3D[3 * meshTriangles.Length];
				Point[] tTextureCoordinates = new Point[3 * meshTriangles.Length];

				for (int i = 0, count = meshTriangles.Length; i < count; i++) {
					triangle = meshTriangles[i];

					//for (int j = 0; j < 3; j++) {
					//	tPositions[3 * i + j] = new Point3D(triangle.Positions[j].X, triangle.Positions[j].Y, triangle.Positions[j].Z);
					//	tNormals[3 * i + j] = new Vector3D(triangle.Normals[j].X, triangle.Normals[j].Y, triangle.Normals[j].Z);
					//	tTextureCoordinates[3 * i + j] = new Point(triangle.TextureCoords[j].X, triangle.TextureCoords[j].Y);
					//}

					tPositions[3 * i] = triangle.Positions[0].ToPoint3D();
					tPositions[3 * i + 1] = triangle.Positions[1].ToPoint3D();
					tPositions[3 * i + 2] = triangle.Positions[2].ToPoint3D();
					
					tNormals[3 * i] = triangle.Normals[0].ToVector3D();
					tNormals[3 * i + 1] = triangle.Normals[1].ToVector3D();
					tNormals[3 * i + 2] = triangle.Normals[2].ToVector3D();
					
					tTextureCoordinates[3 * i] = triangle.TextureCoords[0].ToPoint();
					tTextureCoordinates[3 * i + 1] = triangle.TextureCoords[1].ToPoint();
					tTextureCoordinates[3 * i + 2] = triangle.TextureCoords[2].ToPoint();
				}

				mesh.Attached = new object[] { tPositions, tNormals, tTextureCoordinates };
			}

			_viewport3D1.Dispatcher.Invoke(new Action(delegate {
				try {
					foreach (MeshRawData mesh in meshData) {
						if (_isCancelRequired()) return;

						ModelVisual3D model = new ModelVisual3D();
						GeometryModel3D geoModel = new GeometryModel3D();
						model.Content = geoModel;

						MeshGeometry3D meshGeo = new MeshGeometry3D();
						object[] arrays = (object[]) mesh.Attached;

						meshGeo.Positions = new Point3DCollection((Point3D[]) arrays[0]);
						meshGeo.Normals = new Vector3DCollection((Vector3D[]) arrays[1]);
						meshGeo.TextureCoordinates = new PointCollection((Point[]) arrays[2]);
						mesh.Attached = null;

						geoModel.Geometry = meshGeo;

						//bool isTransparent = false;

						Material material = _generateMaterial(mesh.Texture, false);
						
						if (_isCancelRequired()) return;

						geoModel.Material = material;
						geoModel.BackMaterial = material;
						
						if (_isCancelRequired()) return;
						
						_viewport3D1.Children.Add(model);
						
						Matrix3D matrix3D = matrix.ToMatrix3D();
						MatrixTransform3D mt = new MatrixTransform3D(matrix3D);
						model.Transform = mt;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}));
		}

		private readonly Dictionary<string, DiffuseMaterial> _bufferedTextures = new Dictionary<string, DiffuseMaterial>();
		private readonly Dictionary<string, Material> _bufferedTexturesNoTiles = new Dictionary<string, Material>();
		private Model3DGroup _mainModelGroup = new Model3DGroup();

		public void ClearBufferedTextures() {
			_bufferedTextures.Clear();
		}

		private Material _generateMaterial(string texture, bool meterialNoTile) {
			if (meterialNoTile) {
				if (_bufferedTexturesNoTiles.ContainsKey(texture)) {
					return _bufferedTexturesNoTiles[texture];
				}
			}
			else {
				if (_bufferedTextures.ContainsKey(texture)) {
					return _bufferedTextures[texture];
				}
			}

			var material = new DiffuseMaterial();
			Brush materialBrush;
			FileEntry entry = _grfData.FileTable.TryGet(Rsm.RsmTexturePath + "\\" + texture);

			if (entry != null) {
				ImageBrush imageBrush = new ImageBrush();

				if (meterialNoTile) {
					imageBrush.TileMode = TileMode.None;
				}
				else {
					imageBrush.TileMode = TileMode.Tile;
				}

				materialBrush = imageBrush;

				byte[] fileData = entry.GetDecompressedData();

				try {
					GrfImage image = new GrfImage(fileData);

					if (image.GrfImageType == GrfImageType.Indexed8) {
						image.MakePinkTransparent();
						imageBrush.ImageSource = image.Cast<BitmapSource>();

						bool[] trans = new bool[256];

						for (int i = 0; i < image.Palette.Length; i += 4) {
							if (image.Palette[i + 3] == 0) {
								trans[i / 4] = true;
							}
						}
					}
					else if (image.GrfImageType == GrfImageType.Bgr24) {
						image.Convert(new Bgra32FormatConverter());
						image.MakePinkTransparent();
						imageBrush.ImageSource = image.Cast<BitmapSource>();
					}
					else {
						imageBrush.ImageSource = image.Cast<BitmapSource>();
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}

				imageBrush.ViewportUnits = BrushMappingMode.Absolute;
			}
			else {
				materialBrush = new SolidColorBrush(Color.FromArgb(255, 174, 0, 0));
			}

			material.Brush = materialBrush;

			if (meterialNoTile) {
				_bufferedTexturesNoTiles[texture] = material;
			}
			else {
				_bufferedTextures[texture] = material;
			}

			return material;
		}

		public void Clear() {
			_lastElapsedMs = 0;
			_viewport3D1.Dispatcher.Invoke(new Action(delegate {
				while (_viewport3D1.Children.Count > 2) {
					_viewport3D1.Children.RemoveAt(_viewport3D1.Children.Count - 1);
				}
			}));
		}

		public void ResetCameraDistance(bool value) {
			_resetCameraDistance = value;
		}

		public void SetCameraState(bool? isCameraRotatingChecked, bool? isCameraRotatingActivated, bool? useGlobalLighting) {
			if (isCameraRotatingChecked != null) {
				_isRotatingCameraChecked2 = isCameraRotatingChecked == true;
			}

			if (isCameraRotatingActivated != null) {
				_isRotatingCameraActivated = isCameraRotatingActivated == true;
			}

			if (useGlobalLighting == true) {
				_modelLight.Content = new AmbientLight(Color.FromArgb(255, 255, 255, 255));
			}
			else if (useGlobalLighting == false) {
				_modelLight.Content = new DirectionalLight(Color.FromArgb(255, 255, 255, 255), _lookAt - _cameraPosition);
			}
		}

		public void ReactivateRotatingCamera(bool value) {
			_reactivateRotatingCamera = value;
		}

		#region Nested type: TextureMeshComparer

		public class TextureMeshComparer : IComparer<MeshRawData> {
			#region IComparer<MeshRawData> Members

			public int Compare(MeshRawData x, MeshRawData y) {
				if (x.Texture.ToLower().EndsWith(".tga"))
					return 1;

				if (y.Texture.ToLower().EndsWith(".tga"))
					return -1;

				if (x.Alpha != y.Alpha) {
					return x.Alpha < y.Alpha ? 1 : 0;
				}

				return 0;
			}

			#endregion
		}

		public class TextureMeshComparer2 : IComparer<MeshRawData2> {
			private readonly Rsm _rsm;
			private readonly Vertex _origin;

			#region IComparer<MeshRawData> Members

			public TextureMeshComparer2(Rsm rsm, Vertex origin) {
				_rsm = rsm;
				_origin = origin;
			}

			public int Compare(MeshRawData2 x, MeshRawData2 y) {
				if (x.Texture.ToLower().EndsWith(".tga") && !y.Texture.ToLower().EndsWith(".tga")) {
					return 1;
				}

				if (y.Texture.ToLower().EndsWith(".tga") && !x.Texture.ToLower().EndsWith(".tga")) {
					return -1;
				}
				
				var lenghtX = (x.Position - _origin).Length + x.BoundingBox.Range.Length;
				var lenghtY = (y.Position - _origin).Length + y.BoundingBox.Range.Length;

				if (Math.Abs(lenghtX - lenghtY) < 0.00001) {
					if (x.Mesh.Parent != null && x.Mesh.Parent == y.Mesh) {
						return 1;
					}

					if (y.Mesh.Parent != null && y.Mesh.Parent == x.Mesh) {
						return -1;
					}

					return 0;
				}

				if (Math.Abs(lenghtX - lenghtY) < 5) {	// Both models are too close to tell, use model's hierarchy
					int i1 = _rsm.Meshes.IndexOf(x.Mesh);
					int i2 = _rsm.Meshes.IndexOf(y.Mesh);

					return i1 - i2 < 0 ? -1 : 1;
				}

				return lenghtX > lenghtY ? -1 : 1;
			}

			#endregion
		}

		#endregion

		public void AddRsm2(Rsm rsm, int animationFrame, bool updateCamera = false) {
			_mainModelGroup.Children.Clear();

			_loadRsm2(rsm, animationFrame, _mainModelGroup);

			if (updateCamera) {	// Only called once
				_viewport3D1.Children.Add(new ModelVisual3D { Content = _mainModelGroup });
			
				var Box = new BoundingBox();
			
				for (int i = 0; i < 3; i++) {
					for (int j = 0; j < rsm.Meshes.Count; j++) {
						Box.Max[i] = Math.Max(Box.Max[i], rsm.Meshes[j].BoundingBox.Max[i]);
						Box.Min[i] = Math.Min(Box.Min[i], rsm.Meshes[j].BoundingBox.Min[i]);
					}
			
					Box.Offset[i] = (Box.Max[i] + Box.Min[i]) / 2.0f;
					Box.Range[i] = (Box.Max[i] - Box.Min[i]) / 2.0f;
					Box.Center[i] = Box.Min[i] + Box.Range[i];
				}
			
				_lookAt = Box.Center.ToVector3D();//new Vector3D()
			
				if (_resetCameraDistance)
					_distance = Math.Max(Math.Max(Box.Range[0], Box.Range[1]), Box.Range[2]) * 4;
				else
					_resetCameraDistance = true;
			
				if (_reactivateRotatingCamera)
					_isRotatingCameraActivated = true;
				else
					_reactivateRotatingCamera = true;
			
				UpdateCamera();
			}
		}

		private void _applyMatrix(List<Vertex> vert, Matrix4 mat, Mesh mesh) {
			if (mesh != null) {
				mesh.BoundingBox = new BoundingBox();
			}

			for (int i = 0; i < vert.Count; i++) {
				vert[i] = Matrix4.Multiply(mat, vert[i]);

				if (mesh != null) {
					for (int j = 0; j < 3; j++) {
						mesh.BoundingBox.Min[j] = Math.Min(vert[i][j], mesh.BoundingBox.Min[j]);
						mesh.BoundingBox.Max[j] = Math.Max(vert[i][j], mesh.BoundingBox.Max[j]);
					}
				}
			}

			if (mesh != null) {
				for (int i = 0; i < 3; i++) {
					mesh.BoundingBox.Offset[i] = (mesh.BoundingBox.Max[i] + mesh.BoundingBox.Min[i]) / 2.0f;
					mesh.BoundingBox.Range[i] = (mesh.BoundingBox.Max[i] - mesh.BoundingBox.Min[i]) / 2.0f;
					mesh.BoundingBox.Center[i] = mesh.BoundingBox.Min[i] + mesh.BoundingBox.Range[i];
				}
			}
		}

		private List<Vertex> _compile(Mesh mesh) {
			List<Vertex> vertices = mesh.Vertices.ToList();
			_applyMatrix(vertices, mesh.MeshMatrixSelf, mesh);
			return vertices;
		}

		//private Vertex[] _compile(Mesh mesh) {
		//	Vertex[] vert = mesh.Vertices.ToArray();
		//	Matrix4 offsetMt = new Matrix4(mesh.OffsetMatrix);
		//
		//	mesh.Matrix = Matrix4.Identity;
		//
		//	Matrix4 modelViewMat = Matrix4.Identity;
		//
		//	modelViewMat = Matrix4.Multiply2(modelViewMat, mesh.MeshMatrixSelf);
		//	_applyMatrix(vert, modelViewMat, null);
		//	modelViewMat = Matrix4.Identity;
		//
		//	Matrix4 translation = Matrix4.Identity;
		//	translation.Offset = mesh.Position_ + mesh.Position_V;
		//
		//	modelViewMat = Matrix4.Multiply(modelViewMat, translation);
		//
		//	if (mesh.RotFrames.Count == 0) {
		//		modelViewMat = Matrix4.Multiply(modelViewMat, offsetMt);
		//	}
		//
		//	_applyMatrix(vert, modelViewMat, mesh);
		//	return vert;
		//}

		private void _loadRsm2(Rsm rsm, int animationFrame, Model3DGroup mainModel3DGroup) {
			rsm.MainMesh.Calc(rsm, animationFrame);
			rsm.ClearBuffers();

			if (true) {
				List<MeshRawData2> meshData = new List<MeshRawData2>();
				List<Vertex> position = new List<Vertex>();
			
				foreach (var mesh in rsm.Meshes) {
					Dictionary<string, MeshRawData2> allRawData = new Dictionary<string, MeshRawData2>();
					var vertices = _compile(mesh);
			
					List<Vector3D> normals = new List<Vector3D>(vertices.Count);
					List<Vector3D> vertices3d = new List<Vector3D>(vertices.Count);
					List<Point3D> verticesPoints3D = new List<Point3D>(vertices.Count);
					List<Point> text3D = new List<Point>(mesh.TextureVertices.Count);

					for (int i = 0; i < vertices.Count; i++) {
						normals.Add(new Vector3D(0, 0, 0));
						vertices3d.Add(vertices[i].ToVector3D());
						verticesPoints3D.Add(vertices[i].ToPoint3D());
					}

					for (int i = 0; i < mesh.TextureVertices.Count; i++) {
						text3D.Add(new Point(mesh.TextureVertices[i].U, mesh.TextureVertices[i].V));
					}

					if (mesh.AttachedNormals == null) {
						for (int i = 0; i < mesh.Faces.Count; i++) {
							Vector3D p = Vector3D.CrossProduct(vertices3d[mesh.Faces[i].VertexIds[1]] - vertices3d[mesh.Faces[i].VertexIds[0]], vertices3d[mesh.Faces[i].VertexIds[2]] - vertices3d[mesh.Faces[i].VertexIds[0]]);
							normals[mesh.Faces[i].VertexIds[0]] += p;
							normals[mesh.Faces[i].VertexIds[1]] += p;
							normals[mesh.Faces[i].VertexIds[2]] += p;
						}

						for (int i = 0; i < normals.Count; i++) {
							normals[i].Normalize();
						}

						mesh.AttachedNormals = normals;
					}
					else {
						normals = (List<Vector3D>)mesh.AttachedNormals;
					}

					for (int i = 0; i < mesh.Faces.Count; i++) {
						var face = mesh.Faces[i];
						string texture;
					
						if (mesh.Textures.Count > 0) {
							texture = mesh.Textures[mesh.TextureIndexes[face.TextureId]];
						}
						else {
							texture = rsm.Textures[mesh.TextureIndexes[face.TextureId]];
						}
					
						if (!allRawData.ContainsKey(texture)) {
							allRawData[texture] = new MeshRawData2 { Texture = texture, Alpha = 0, Position = mesh.BoundingBox.Center, Mesh = mesh, BoundingBox = mesh.BoundingBox };
						}
					
						var rawData = allRawData[texture];
						rawData.Positions.Add(verticesPoints3D[face.VertexIds[0]]);
						rawData.Positions.Add(verticesPoints3D[face.VertexIds[1]]);
						rawData.Positions.Add(verticesPoints3D[face.VertexIds[2]]);
					
						rawData.Normals.Add(normals[face.VertexIds[0]]);
						rawData.Normals.Add(normals[face.VertexIds[1]]);
						rawData.Normals.Add(normals[face.VertexIds[2]]);

						Point v0 = text3D[face.TextureVertexIds[0]];
						Point v1 = text3D[face.TextureVertexIds[1]];
						Point v2 = text3D[face.TextureVertexIds[2]];
					
						if (mesh.TextureKeyFrameGroup.Count > 0) {
							foreach (var type in mesh.TextureKeyFrameGroup.Types) {
								if (mesh.TextureKeyFrameGroup.HasTextureAnimation(face.TextureId, type)) {
									float offset = mesh.GetTexture(animationFrame, face.TextureId, type);
									Matrix4 mat = Matrix4.Identity;
					
									switch(type) {
										case 0:
											v0.X += offset;
											v1.X += offset;
											v2.X += offset;
											break;
										case 1:
											v0.Y += offset;
											v1.Y += offset;
											v2.Y += offset;
											break;
										case 2:
											v0.X *= offset;
											v1.X *= offset;
											v2.X *= offset;
											break;
										case 3:
											v0.Y *= offset;
											v1.Y *= offset;
											v2.Y *= offset;
											break;
										case 4:
											mat = Matrix4.Rotate3(mat, new Vertex(0, 0, 1), offset);
					
											Vertex n0 = Matrix4.Multiply(mat, new Vertex(v0.X, v0.Y, 0));
											Vertex n1 = Matrix4.Multiply(mat, new Vertex(v1.X, v1.Y, 0));
											Vertex n2 = Matrix4.Multiply(mat, new Vertex(v2.X, v2.Y, 0));
					
											v0.X = n0.X;
											v0.Y = n0.Y;
					
											v1.X = n1.X;
											v1.Y = n1.Y;
					
											v2.X = n2.X;
											v2.Y = n2.Y;
											rawData.MaterialNoTile = true;
											break;
									}
								}
							}
						}
					
						rawData.TextureCoordinates.Add(v0);
						rawData.TextureCoordinates.Add(v1);
						rawData.TextureCoordinates.Add(v2);
					}
					
					foreach (var meshRawData in allRawData) {
						meshData.Add(meshRawData.Value);
						position.Add(mesh.Position_);
					}
				}
			
				for (int i = 0; i < meshData.Count; i++) {
					meshData[i].Material = _generateMaterial(meshData[i].Texture, meshData[i].MaterialNoTile);
				}
			
				meshData.Sort(new TextureMeshComparer2(rsm, new Vertex(_cameraPosition.X, _cameraPosition.Y, _cameraPosition.Z)));
			
				foreach (var meshRawData in meshData) {
					MeshGeometry3D mesh3 = new MeshGeometry3D();
				
					mesh3.Positions = new Point3DCollection(meshRawData.Positions);
					mesh3.TextureCoordinates = new PointCollection(meshRawData.TextureCoordinates);
					mesh3.Normals = new Vector3DCollection(meshRawData.Normals);
				
					var material3 = meshRawData.Material;
					GeometryModel3D model3 = new GeometryModel3D(mesh3, material3);
					model3.BackMaterial = material3;
					mainModel3DGroup.Children.Add(model3);
				}

				//Matrix3D mat3d = Matrix3D.Identity;
				//mat3d.Scale(new Vector3D(-1, 1, 1));
				//MatrixTransform3D mt = new MatrixTransform3D(mat3d);
				//mainModel3DGroup.Transform = mt;
			}
		}
	}
}