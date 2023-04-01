using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RsmFormat.MeshStructure;
using GRF.Graphics;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.WPF.PreviewTabs;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using Utilities.Extension;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for QuickPreview.xaml
	/// </summary>
	public partial class QuickPreview : UserControl, IDisposable {
		private string _fileName;
		private MultiGrfReader _metaGrf;
		private GrfImageWrapper _wrapper = new GrfImageWrapper();

		public QuickPreview() {
			InitializeComponent();
		}

		#region IDisposable Members

		public void Dispose() {
			if (_metaGrf != null) {
				_metaGrf.Dispose();
				_metaGrf = null;
			}

			if (_wrapper != null) {
				if (_wrapper.Image != null) {
					_wrapper.Image.Close();
					_wrapper.Image = null;
				}

				_wrapper = null;
			}

			if (_imagePreview != null) {
				_imagePreview.Source = null;
				_imagePreview = null;
			}

			if (_meshesDrawer != null) {
				_meshesDrawer.Dispose();
				_meshesDrawer = null;
			}
		}

		#endregion

		public void Set(AsyncOperation asyncOperation, MultiGrfReader metaGrf) {
			_metaGrf = metaGrf;
			VirtualFileDataObject.SetDraggable(_imagePreview, _wrapper);
		}

		public void Update(string file) {
			_fileName = file;

			byte[] data = _metaGrf.GetData(file);

			if (data != null) {
				string ext = file.GetExtension();

				if (ext == ".bmp" || ext == ".tga" || ext == ".png" || ext == ".spr" || ext == ".ebm" || ext == ".gat" || ext == ".pal") {
					_imagePreview.Tag = Path.GetFileNameWithoutExtension(file);
					_wrapper.Image = ImageProvider.GetImage(data, ext);
					_imagePreview.Source = _wrapper.Image.Cast<BitmapSource>();
					_meshesDrawer.Dispatch(p => p.Clear());
					_meshesDrawer.Visibility = Visibility.Hidden;
					_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
				}
				else if (ext == ".rsm" || ext == ".rsm2") {
					_meshesDrawer.Visibility = Visibility.Visible;
					_scrollViewer.Dispatch(p => p.Visibility = Visibility.Hidden);

					_meshesDrawer.Dispatch(p => p.Clear());

					Rsm rsm = new Rsm(data);

					if (rsm.Header.IsCompatibleWith(2, 0)) {
						_meshesDrawer.Dispatch(p => p.Update(_metaGrf, () => false, 200));
						_meshesDrawer.Dispatch(p => p.AddRsm2(rsm, 0, true));
						_meshesDrawer.Dispatch(p => p.Visibility = Visibility.Visible);
					}
					else {
						rsm.CalculateBoundingBox();

						_meshesDrawer.Dispatch(p => p.Update(_metaGrf, () => false, Math.Max(Math.Max(rsm.Box.Range[0], rsm.Box.Range[1]), rsm.Box.Range[2]) * 4));

						Matrix4 modelRotation = Matrix4.Identity;
						modelRotation = Matrix4.RotateX(modelRotation, ModelViewerHelper.ToRad(180));
						modelRotation = Matrix4.RotateY(modelRotation, ModelViewerHelper.ToRad(180));

						Dictionary<string, MeshRawData> allMeshData = rsm.Compile(modelRotation, 2);

						// Render meshes
						_meshesDrawer.Dispatch(p => p.AddObject(allMeshData.Values.ToList(), Matrix4.Identity));

						_meshesDrawer.Dispatch(p => p.UpdateCamera());
						_meshesDrawer.Dispatch(p => p.Visibility = Visibility.Visible);
					}
				}
				else {
					ClearPreview();
				}
			}
			else {
				ClearPreview();
			}
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) {
			if (_fileName != null) {
				if (_wrapper.Image != null)
					_wrapper.Image.SaveTo(_fileName, PathRequest.ExtractSetting);
			}
		}

		public void ClearPreview() {
			_meshesDrawer.Dispatch(p => p.Clear());
			_meshesDrawer.Dispatch(p => p.Visibility = Visibility.Hidden);
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
			_imagePreview.Dispatch(p => p.Source = null);
		}
	}
}