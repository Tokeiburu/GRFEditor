using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using GRFEditor.OpenGL.MapComponents;
using GRFEditor.OpenGL.MapRenderers;
using TokeiLibrary.WPF.Styles;
using Utilities;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for CloudEditDialog.xaml
	/// </summary>
	public partial class CameraEditDialog : TkWindow {
		private Camera _camera;
		private DispatcherTimer _refreahTimer = new DispatcherTimer();
		public static bool Opened = false;

		public CameraEditDialog()
			: base("Camera edit...", "settings.png", SizeToContent.Manual, ResizeMode.CanResize) {
			InitializeComponent();
			Opened = true;
		}

		protected override void GRFEditorWindowKeyDown(object sender, KeyEventArgs e) {
			
		}

		protected override void OnClosed(EventArgs e) {
			base.OnClosed(e);

			_refreahTimer.Stop();
			Opened = false;
		}

		public void Init(OpenGLViewport viewport) {
			_refreahTimer.Tick += delegate {
				_updateFields();
			};

			_refreahTimer.Interval = TimeSpan.FromSeconds(0.1);

			var modesA = Enum.GetValues(typeof(CameraMode));
			List<string> modes = new List<string>();

			foreach (var mode in modesA) {
				modes.Add(mode.ToString());
			}

			_camera = viewport.Camera;

			int index = (int)_camera.Mode;

			_cbCameraMode.ItemsSource = modes;
			_cbCameraMode.SelectedIndex = index;

			//_camera.LookAt = new Vector3(gnd.Width * 5f, 0, gnd.Height * 5f + 10f);

			_update(_tbDistance, v => {
				_camera.Distance = v;
				_camera.MaxDistance = (float)Math.Max(_camera.MaxDistance, v);
			}, v => v.Text = _camera.Distance + "");
			_update(_tbLookAtX, 
				v => _camera.LookAt.X = (float)(v + (viewport._request.IsMap ? viewport._request.Gnd.Width * 5f : 0)), 
				v => Debug.Ignore(() => v.Text = (_camera.LookAt.X - (viewport._request.IsMap ? viewport._request.Gnd.Width * 5f : 0)) + ""));
			_update(_tbLookAtY, 
				v => _camera.LookAt.Y = (float)v, 
				v => v.Text = _camera.LookAt.Y + "");
			_update(_tbLookAtZ, 
				v => _camera.LookAt.Z = (float)(v + (viewport._request.IsMap ? viewport._request.Gnd.Height * 5f + 10f : 0)),
				v => Debug.Ignore(() => v.Text = (_camera.LookAt.Z - (viewport._request.IsMap ? viewport._request.Gnd.Height * 5f + 10f : 0)) + ""));

			_update(_tbAngleX, v => _camera.AngleX_Degree = (float)v, v => v.Text = (float)_camera.AngleX_Degree + "");
			_update(_tbAngleY, v => _camera.AngleY_Degree = (float)v, v => v.Text = (float)_camera.AngleY_Degree + "");

			_update(_tbPositionX, v => _camera.Position.X = (float)v, v => v.Text = (float)_camera.Position.X + "");
			_update(_tbPositionY, v => _camera.Position.Y = (float)v, v => v.Text = (float)_camera.Position.Y + "");
			_update(_tbPositionZ, v => _camera.Position.Z = (float)v, v => v.Text = (float)_camera.Position.Z + "");

			_update(_tbZNear, v => _camera.ZNear = (float)v, v => v.Text = (float)_camera.ZNear + "");
			_update(_tbZFar, v => _camera.ZFar = (float)v, v => v.Text = (float)_camera.ZFar + "");

			_tbAngleX.DisplayFormat = "{0:0.00}°";
			_tbAngleY.DisplayFormat = "{0:0.00}°";

			_updateFunctions.Add(delegate {
				if (_cbCameraMode.IsDropDownOpen)
					return;

				_cbCameraMode.SelectedIndex = (int)_camera.Mode;
			});

			_cbCameraMode.SelectionChanged += delegate {
				try {
					_fieldEditing = true;

					_camera.Mode = (CameraMode)_cbCameraMode.SelectedIndex;
				}
				finally {
					_fieldEditing = false;
				}
			};

			_updateFields();
			_refreahTimer.Start();
		}

		private void _updateFields() {
			if (_fieldEditing)
				return;

			foreach (var function in _updateFunctions) {
				function();
			}
		}

		private void _update(FloatTextBoxEdit tb, Action<double> setValueFunc, Action<FloatTextBoxEdit> loadValueFunc) {
			_setupTb(tb);

			tb.TextChanged += delegate {
				if (_fieldEditing)
					return;

				_fieldEditing = true;
				setValueFunc(tb.GetFloat());
				_fieldEditing = false;
			};

			_updateFunctions.Add(
				delegate {
					if (tb._tb.Opacity > 0)
						return;

					loadValueFunc(tb);
				});
		}

		private List<Action> _updateFunctions = new List<Action>();
		private bool _fieldEditing;

		private void _setupTb(FloatTextBoxEdit tb) {
			tb._outerBorder.BorderThickness = new Thickness(1, 1, 1, 1);
			tb._outerBorder.CornerRadius = new CornerRadius(4, 4, 4, 4);

			tb._previewLeft.Visibility = Visibility.Collapsed;
			tb._previewRight.Visibility = Visibility.Collapsed;
		}

		private void _bind(TextBox tb, Func<string> get, Func<string, float> set) {
			tb.Text = get();

			tb.TextChanged += delegate {
				set(tb.Text);
				MapRenderer.SkyMap.IsChanged = true;
			};
		}

		private void _buttonCopy_Click(object sender, RoutedEventArgs e) {
			_camera.Copy();
		}

		private void _buttonPaste_Click(object sender, RoutedEventArgs e) {
			_camera.Paste();
		}
	}
}
