using GRF.Image;
using System;
using System.Windows.Media;
using Utilities;

namespace GrfToWpfBridge.ActRenderer {
	public class FrameRendererConfiguration {
		private ConfigAsker _configAsker;
		public BufferedBrushes BufferedBrushes = new BufferedBrushes();

		public FrameRendererConfiguration(ConfigAsker configAsker) {
			_configAsker = configAsker;

			ActEditorSpriteSelectionBorder = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Selected sprite border color]", GrfColor.ToHex(255, 255, 0, 0), v => new GrfColor(v), v => v.ToHexString());
			ActEditorSpriteSelectionBorderOverlay = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Selected sprite overlay color]", GrfColor.ToHex(0, 255, 255, 255), v => new GrfColor(v), v => v.ToHexString());
			ActEditorSelectionBorder = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Selection border color]", GrfColor.ToHex(255, 0, 0, 255), v => new GrfColor(v), v => v.ToHexString());
			ActEditorSelectionBorderOverlay = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Selection overlay color]", GrfColor.ToHex(50, 128, 128, 255), v => new GrfColor(v), v => v.ToHexString());
			ActEditorAnchorColor = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Anchor color]", GrfColor.ToHex(200, 255, 255, 0), v => new GrfColor(v), v => v.ToHexString());
			ActEditorGridLineHorizontal = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Grid line horizontal color]", GrfColor.ToHex(255, 0, 0, 0), v => new GrfColor(v), v => v.ToHexString());
			ActEditorGridLineVertical = new QuickSetting<GrfColor>(_configAsker, "[ActEditor - Grid line vertical color]", GrfColor.ToHex(255, 0, 0, 0), v => new GrfColor(v), v => v.ToHexString());
		}

		public QuickSetting<GrfColor> ActEditorSpriteSelectionBorder;
		public QuickSetting<GrfColor> ActEditorSpriteSelectionBorderOverlay;
		public QuickSetting<GrfColor> ActEditorSelectionBorder;
		public QuickSetting<GrfColor> ActEditorSelectionBorderOverlay;
		public QuickSetting<GrfColor> ActEditorAnchorColor;
		public QuickSetting<GrfColor> ActEditorGridLineHorizontal;
		public QuickSetting<GrfColor> ActEditorGridLineVertical;

		public static int FrameInterval => 24;

		private static bool? _useAliasing;
		private static BitmapScalingMode? _mode;

		public bool UseAliasing {
			get {
				if (_useAliasing == null)
					_useAliasing = Boolean.Parse(_configAsker["[ActEditor - Use aliasing]", false.ToString()]);

				return _useAliasing.Value;
			}
			set {
				_configAsker["[ActEditor - Use aliasing]"] = value.ToString();
				_useAliasing = value;
			}
		}

		public BitmapScalingMode ActEditorScalingMode {
			get {
				if (_mode != null) {
					return _mode.Value;
				}

				var value = (BitmapScalingMode)Enum.Parse(typeof(BitmapScalingMode), _configAsker["[ActEditor - Scale mode]", BitmapScalingMode.NearestNeighbor.ToString()], true);
				_mode = value;
				return value;
			}
			set {
				_configAsker["[ActEditor - Scale mode]"] = value.ToString();
				_mode = value;
			}
		}

		public Color ActEditorBackgroundColor {
			get { return new GrfColor((_configAsker["[ActEditor - Background preview color]", GrfColor.ToHex(150, 0, 0, 0)])).ToColor(); }
			set { _configAsker["[ActEditor - Background preview color]"] = GrfColor.ToHex(value.A, value.R, value.G, value.B); }
		}

		public class QuickSetting<T> {
			private ConfigAsker _configAsker;
			private string _propertyName;
			private string _defaultValue;
			public Func<string, T> ConverterTo;
			public Func<T, string> ConverterFrom;
			private T _cached;
			private bool _isCached = false;

			public delegate void PropertyChangedEventHandler();

			public event PropertyChangedEventHandler PropertyChanged;

			public QuickSetting(ConfigAsker configAsker, string propertyName, string defaultValue, Func<string, T> getter, Func<T, string> setter) {
				_configAsker = configAsker;
				_propertyName = propertyName;
				_defaultValue = defaultValue;
				ConverterTo = getter;
				ConverterFrom = setter;
			}

			public T Get() {
				if (!_isCached) {
					_cached = ConverterTo(_configAsker[_propertyName, _defaultValue]);
					_isCached = true;
				}

				return _cached;
			}

			public void Set(T value) {
				_configAsker[_propertyName] = ConverterFrom(value);
				_cached = value;
				_isCached = true;
				PropertyChanged?.Invoke();
			}

			public string GetDefaultString() {
				return _defaultValue;
			}
		}
	}
}
