using Utilities;

namespace GRF.FileFormats.LubFormat {
	public class LubSettings {
		private bool _groupIfAllKeyValues;
		private bool? _oldGroupIfAllKeyValues;
		public bool AppendFunctionId { get; set; }
		public bool UseCodeReconstructor { get; set; }
		public bool DecodeInstructions { get; set; }
		public bool GroupIfAllValues { get; set; }

		public bool GroupIfAllKeyValues {
			get {
				bool val = _groupIfAllKeyValues;

				if (_oldGroupIfAllKeyValues != null) {
					_groupIfAllKeyValues = _oldGroupIfAllKeyValues.Value;
					_oldGroupIfAllKeyValues = null;
				}

				return val;
			}
			set { _groupIfAllKeyValues = value; }
		}

		public int TextLengthLimit { get; set; }

		public void OneTimeOverrideGroupIfAllKeyValues(bool value) {
			_oldGroupIfAllKeyValues = _groupIfAllKeyValues;
			_groupIfAllKeyValues = value;
		}

		public void RemoveOverrides() {
		}
	}
}