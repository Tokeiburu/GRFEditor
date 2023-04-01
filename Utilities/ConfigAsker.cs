using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using Utilities.Extension;

namespace Utilities {
	/// <summary>
	/// ROUtilityTool library class
	/// Used to write simple and basic configuration files
	/// They are very tolerant to errors and easy to use.
	/// </summary>
	public class ConfigAsker {
		public string ConfigFile = "";
		public bool IsAutomaticSaveEnabled {
			get { return _isAutomaticSaveEnabled; }
			set {
				_isAutomaticSaveEnabled = value;

				if (value)
					_save();
			}
		}

		protected readonly Dictionary<string, string> _properties = new Dictionary<string, string>();
		protected readonly Dictionary<string, ConfigAskerSetting> _propertySettings = new Dictionary<string, ConfigAskerSetting>();
		private bool _hasBeenClosed;
		private readonly object _lock = new object();
		private bool _isAutomaticSaveEnabled = true;

		public bool AdvancedSettingEnabled { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigAsker" /> class.
		/// </summary>
		/// <param name="fileName">Name of the file to load and save the data from.</param>
		public ConfigAsker(string fileName = "config.txt") {
			ConfigFile = fileName;
			_load();
		}

		public ConfigAsker(bool dummy) {
		}

		public void OpenNew(string fileName) {
			this.Close();
			_hasBeenClosed = false;
			_properties.Clear();
			ConfigFile = fileName;
			_load();
		}

		public bool IsEmpty {
			get {
				return (_properties.Count == 0);
			}
		}

		private const string _lineBreakIdentifier = "__%LineBreak%";
		private string _expand(string val) {
			if (val != null)
				val = val.Replace(_lineBreakIdentifier, "\r\n");

			return val;
		}

		/// <summary>
		/// Gets or sets a configuration variable.
		/// </summary>
		/// <param name="toFind">The configuration variable to find.</param>
		/// <returns>The content of the varible found (null if nothing has been found).</returns>
		public string this[string toFind] {
			get {
				lock (_lock) {
					return _hasBeenClosed ? null : _expand((from prop in _properties where prop.Key == toFind select prop.Value).FirstOrDefault());
				}
			}
			set {
				if (_hasBeenClosed) return;

				value = value.ReplaceAll("\r\n", _lineBreakIdentifier).ReplaceAll("\n", _lineBreakIdentifier);

				lock (_lock) {
					if (AdvancedSettingEnabled) {
						if (this[toFind] != null && _propertySettings.ContainsKey(toFind)) {
							_propertySettings[toFind].SetValue(value);
						}
					}

					_properties[toFind] = value;
					_save();
				}
			}
		}

		public ConfigAskerSetting RetrieveSetting(Func<object> action) {
			return RetrieveSetting(() => { action(); });
		}

		public ConfigAskerSetting RetrieveSetting(Action action) {
			bool state = AdvancedSettingEnabled;

			_latestAccessed = null;

			AdvancedSettingEnabled = true;
			action();
			AdvancedSettingEnabled = state;

			ConfigAskerSetting setting = null;

			if (_latestAccessed != null) {
				setting = _propertySettings[_latestAccessed];
			}

			return setting;
		}

		private string _latestAccessed;

		/// <summary>
		/// Same as this[string], except it returns a default value and assigns it if
		/// there is nothing assigned.
		/// </summary>
		/// <param name="toFind">The configuration variable to find.</param>
		/// <param name="def">The default value to return and set.</param>
		/// <returns>The content of the varible found (null if nothing has been found).</returns>
		public string this[string toFind, string def] {
			get {
				lock (_lock) {
					if (AdvancedSettingEnabled) {
						if (!_propertySettings.ContainsKey(toFind)) {
							_propertySettings[toFind] = new ConfigAskerSetting(this, toFind, def);
						}

						_latestAccessed = toFind;
					}

					string obj = this[toFind];
					if (String.IsNullOrEmpty(obj) && obj != def) {
						this[toFind] = def;
						_save();
						return _expand(def);
					}

					obj = obj ?? "";

					return _expand(obj);
				}
			}
		}

		/// <summary>
		/// Deletes the key.
		/// </summary>
		/// <param name="key">The key.</param>
		public void DeleteKey(string key) {
			try {
				_properties.Remove(key);
			}
			catch {
			}
		}

		/// <summary>
		/// Determines whether the specified key contains key.
		/// </summary>
		/// <param name="key">The key.</param>
		/// <returns>
		///   <c>true</c> if the specified key contains key; otherwise, <c>false</c>.
		/// </returns>
		public bool ContainsKey(string key) {
			return _properties.ContainsKey(key);
		}

		/// <summary>
		/// Deletes this instance by clearing the loaded values and by deleting the actual configuration save file.
		/// </summary>
		public void Delete() {
			try {
				File.Delete(ConfigFile);
				_properties.Clear();
			}
			catch {
			}
		}

		/// <summary>
		/// Closes and saves the current configuration to the configuration file.
		/// The current configuration cannot be used after this operation.
		/// </summary>
		public void Close() {
			if (_hasBeenClosed) return;

			_hasBeenClosed = true;
			_save();
		}

		protected virtual void _save() {
			if (!IsAutomaticSaveEnabled) return;

			try {
				using (StreamWriter writer = new StreamWriter(ConfigFile)) {
					foreach (KeyValuePair<string, string> property in _properties.OrderBy(p => p.Key)) {
						writer.WriteLine(property.Key + "=" + property.Value);
					}

					writer.Close();
				}
			}
			catch {
			}
		}

		protected virtual void _load() {
			if (!File.Exists(ConfigFile)) {
				try {
					Directory.CreateDirectory(Path.GetDirectoryName(ConfigFile));
					File.Create(ConfigFile).Close();
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
			else {
				using (StreamReader configStream = new StreamReader(ConfigFile)) {
					while (!configStream.EndOfStream) {
						string buffer = configStream.ReadLine();

						if (buffer != null) {
							string[] values = buffer.Split(new char[] { '=' }, 2);

							try {
								_properties[values[0]] = values[1];
							}
							catch { }
						}
					}
				}
			}
		}

		public void Merge(ConfigAsker configAsker) {
			foreach (var tuple in configAsker._properties) {
				this[tuple.Key] = tuple.Value;
			}
		}

		public void DeleteKeys(string toFind) {
			try {
				IsAutomaticSaveEnabled = false;

				List<string> keys = _properties.Keys.ToList();

				foreach (string key in keys) {
					if (key.Contains(toFind)) {
						_properties.Remove(key);
					}
				}
			}
			finally {
				IsAutomaticSaveEnabled = true;
			}
		}
	}
}
