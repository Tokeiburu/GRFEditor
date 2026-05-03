using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using GRF.Core.GroupedGrf;
using GRF.Image;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core;
using GRFEditor.OpenGL.MapComponents;
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
		private GrfImageWrapper _primaryImage = new GrfImageWrapper();

		public QuickPreview() {
			InitializeComponent();

			_viewport.Camera.Mode = CameraMode.PerspectiveDirectX;
			_viewport.RenderOptions.FpsCap = 30;
		}

		#region IDisposable Members

		public void Dispose() {
			if (_metaGrf != null) {
				_metaGrf.Dispose();
				_metaGrf = null;
			}

			if (_primaryImage != null) {
				if (_primaryImage.Image != null) {
					_primaryImage.Image.Close();
					_primaryImage.Image = null;
				}

				_primaryImage = null;
			}

			if (_imagePreview != null) {
				_imagePreview.Source = null;
				_imagePreview = null;
			}
		}

		#endregion

		public void Set(AsyncOperation asyncOperation) {
			_metaGrf = GrfEditorConfiguration.Resources.MultiGrf;
			VirtualFileDataObject.SetDraggable(_imagePreview, _primaryImage);
		}

		public void Update(string file) {
			if (_fileName == file)
				return;

			_fileName = file;

			var _isCancelRequired = new Func<bool>(() => file != _fileName);

			byte[] data = _metaGrf.GetData(file);

			if (data != null) {
				string ext = file.GetExtension();

				if (ext == ".bmp" || ext == ".tga" || ext == ".png" || ext == ".spr" || ext == ".ebm" || ext == ".gat" || ext == ".pal") {
					_primaryImage.Image = ImageProvider.GetImage(data, ext);
					_primaryImage.ExportFileName = Path.GetFileNameWithoutExtension(file);
					_imagePreview.Source = _primaryImage.Image.Cast<BitmapSource>();
					_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
					_viewport.Visibility = Visibility.Hidden;
				}
				else if (ext == ".rsm" || ext == ".rsm2") {
					_scrollViewer.Dispatch(p => p.Visibility = Visibility.Hidden);
					_viewport.Visibility = Visibility.Visible;

					Rsm.ForceShadeType = 2;
					_viewport.RotateCamera = true;
					_viewport.Camera.Mode = CameraMode.PerspectiveDirectX;
					_viewport.Loader.AddRequest(new RendererLoadRequest { IsMap = false, Rsm = new Rsm(data), CancelRequired = _isCancelRequired, Resource = file, Context = _viewport });
				}
				else if (ext == ".gnd" || ext == ".rsw") {
					_scrollViewer.Dispatch(p => p.Visibility = Visibility.Hidden);
					_viewport.Visibility = Visibility.Visible;

					Rsm.ForceShadeType = -1;
					_viewport.RotateCamera = true;
					_viewport.Camera.Mode = CameraMode.PerspectiveOpenGL;
					_viewport.Loader.AddRequest(new RendererLoadRequest { IsMap = true, Resource = file.ReplaceExtension(""), CancelRequired = _isCancelRequired, Context = _viewport });
				}
			}
			else {
				ClearPreview();
			}
		}

		private void _menuItemImageExport_Click(object sender, RoutedEventArgs e) {
			if (_fileName != null) {
				if (_primaryImage.Image != null)
					_primaryImage.Image.SaveTo(_fileName, PathRequest.ExtractSetting);
			}
		}

		public void ClearPreview() {
			_scrollViewer.Dispatch(p => p.Visibility = Visibility.Visible);
			_viewport.Dispatch(p => p.Unload());
			_viewport.Dispatch(p => p.Visibility = Visibility.Hidden);
			_imagePreview.Dispatch(p => p.Source = null);
			_fileName = null;
		}
	}
}