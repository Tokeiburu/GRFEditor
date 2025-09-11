namespace Utilities {
	public class ConfigAskerSetting {
		public delegate void ConfigAskerSettingEventHandler(object sender, string oldValue, string newValue);

		public event ConfigAskerSettingEventHandler PreviewPropertyChanged;
		public event ConfigAskerSettingEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string oldvalue, string newvalue) {
			if (_isSetting || oldvalue == newvalue)
				return;

			try {
				_isSetting = true;
				ConfigAskerSettingEventHandler handler = PropertyChanged;
				if (handler != null) handler(this, oldvalue, newvalue);
			}
			finally {
				_isSetting = false;
			}
		}

		private bool _isSetting = false;

		public void OnPreviewPropertyChanged(string oldvalue, string newvalue) {
			ConfigAskerSettingEventHandler handler = PreviewPropertyChanged;
			if (handler != null) handler(this, oldvalue, newvalue);
		}

		private readonly ConfigAsker _configAsker;
		public string Default { get; private set; }
		public string Name { get; private set; }

		public ConfigAskerSetting(ConfigAsker configAsker, string name, string defaultValue) {
			_configAsker = configAsker;
			Default = defaultValue;
			Name = name;
		}

		public void Set(string value) {
			_configAsker[Name] = value;
		}

		internal void SetValue(string value) {
			if (_isSetting) return;

			try {
				_isSetting = true;

				if (_configAsker[Name, Default] != value) {
					OnPreviewPropertyChanged(_configAsker[Name, Default], value);
				}
			}
			finally {
				_isSetting = false;
			}
		}

		public string Get() {
			return _configAsker[Name, Default];
		}

		public bool IsDefault {
			get { return Default == _configAsker[Name, Default]; }
		}

		public void Reset() {
			Set(Default);
		}
	}
}
