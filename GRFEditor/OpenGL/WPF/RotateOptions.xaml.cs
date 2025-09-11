using System;
using System.Windows;
using System.Windows.Media;
using GRFEditor.OpenGL.MapComponents;

namespace GRFEditor.OpenGL.WPF {
	/// <summary>
	/// Interaction logic for LightningOptions.xaml
	/// </summary>
	public partial class RotateOptions : Window {
		private bool _enableEvents = true;
		private RendererLoadRequest _currentRequest;
		private OpenGLViewport _viewport;
		private const double SliderWidth = 200d;
		private const double SliderWidthStep = 3;

		private double _totalLength;
		private double _halfTotalLength;
		
		public RotateOptions() {
			InitializeComponent();

			_totalLength = SliderWidth / SliderWidthStep;
			_totalLength *= 0.5d;
			_halfTotalLength = _totalLength * 0.5d;

			_sliderLatitude.ValueChanged += _sliderLatitude_ValueChanged;
			_reset.Click += delegate {
				_viewport.RenderOptions.RotateSpeed = 6;
				_sliderLatitude.SetPosition((_viewport.RenderOptions.RotateSpeed + _halfTotalLength) / _totalLength, true);
				_tbLatitude.Text = String.Format("{0:0.0}", _viewport.RenderOptions.RotateSpeed);
			};
		}

		public void Load(RendererLoadRequest request) {
			_currentRequest = request;
			_enableEvents = false;

			_viewport = _currentRequest.Context as OpenGLViewport;

			if (_viewport != null) {
				_sliderLatitude.SetPosition((_viewport.RenderOptions.RotateSpeed + _halfTotalLength) / _totalLength, true);
				_tbLatitude.Text = String.Format("{0:0.0}", _viewport.RenderOptions.RotateSpeed);
			}

			_enableEvents = true;
		}

		private void _sliderLatitude_ValueChanged(object sender, double value) {
			if (!_enableEvents)
				return;

			_viewport.RenderOptions.RotateSpeed = Math.Round((value * _totalLength - _halfTotalLength) * 2d, MidpointRounding.AwayFromZero) / 2d;
			_sliderLatitude.SetPosition((_viewport.RenderOptions.RotateSpeed + _halfTotalLength) / _totalLength, true);
			_tbLatitude.Text = String.Format("{0:0.0}", _viewport.RenderOptions.RotateSpeed);
		}
	}
}
