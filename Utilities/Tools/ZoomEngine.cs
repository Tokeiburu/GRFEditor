using System;

namespace Utilities.Tools {
	public class ZoomEngine {
		private double _maxScale = 32;
		private double _minScale = 0.03;
		private Func<double> _zoomInMultiplier = new Func<double>(() => 1d);

		public double MaxScale {
			get { return _maxScale; }
			set { _maxScale = value; }
		}

		public double MinScale {
			get { return _minScale; }
			set { _minScale = value; }
		}

		public Func<double> ZoomInMultiplier {
			get { return _zoomInMultiplier; }
			set { _zoomInMultiplier = value; }
		}

		public ZoomEngine() {
			Scale = 1;
			OldScale = 1;
		}

		public double OldScale { get; private set; }
		public double Scale { get; private set; }

		public string ScaleText {
			get { return String.Format("{0:0.00} %", Scale * 100f); }
		}

		public void Zoom(double mouseWheel) {
			double incrementFactor = 0.002 * Scale * ZoomInMultiplier();

			OldScale = Scale;
			Scale = Scale + incrementFactor * mouseWheel;
			_checkScale();
		}

		private void _checkScale() {
			if (Scale > MaxScale) {
				Scale = MaxScale;
			}

			if (Scale < MinScale) {
				Scale = MinScale;
			}
		}

		public void SetZoom(double scale) {
			Scale = scale;
			_checkScale();
		}
	}
}