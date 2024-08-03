using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Utilities {
	public class IniSection : IEnumerable<KeyValuePair<string, string>> {
		private string _section;

		internal string Section {
			set {
				_section = value;
			}
		}

		public IniSection() {
			_section = "";
		}

		internal IniSection(string section) {
			_section = section;
		}

		public IEnumerable<string> Keys {
			get { return _properties.Keys; }
		}

		public IEnumerable<string> Values {
			get { return _properties.Values; }
		}

		private readonly Dictionary<string, string> _properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		public event IniConfigAsker.PropertyChangedDelegate PropertyChanged;

		public virtual void OnPropertyChanged(string section, string key, string value) {
			IniConfigAsker.PropertyChangedDelegate handler = PropertyChanged;
			if (handler != null) handler(this, section, key, value);
		}

		public string this[string key] {
			get { return _properties[key]; }
			set {
				_properties[key] = value;
				OnPropertyChanged(_section, key, value);
			}
		}

		public string this[string key, string def] {
			get {
				if (!_properties.ContainsKey(key)) {
					_properties[key] = def;
					OnPropertyChanged(_section, key, def);
				}
				return _properties[key];
			}
			set {
				_properties[key] = value;
				OnPropertyChanged(_section, key, value);
			}
		}

		public IEnumerator<KeyValuePair<string, string>> GetEnumerator() {
			return _properties.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}

	public class IniConfigAsker {
		private readonly string _filePath;

		public delegate void PropertyChangedDelegate(object sender, string section, string key, string value);
		public delegate void SectionChangedDelegate(object sender, string section, string key);

		private readonly Dictionary<string, IniSection> _sections = new Dictionary<string, IniSection>(StringComparer.OrdinalIgnoreCase);

		public event SectionChangedDelegate SectionChanged;

		public virtual void OnSectionChanged(string section, string key) {
			SectionChangedDelegate handler = SectionChanged;
			if (handler != null) handler(this, section, key);
		}

		public string ConfigFile {
			get { return _filePath; }
		}

		public IniConfigAsker(string filePath) {
			_filePath = filePath;
			_load();
		}

		private void _load() {
			using (StreamReader reader = new StreamReader(_filePath)) {
				string currentSection = null;
				string[] property;
				while (!reader.EndOfStream) {
					string line = reader.ReadLine();

					if (String.IsNullOrEmpty(line))
						continue;

					if (line.Length >= 2 && line[0] == '[' && line[line.Length - 1] == ']') {
						currentSection = line.Substring(1, line.Length - 2);
						continue;
					}

					if (currentSection == null)
						continue;

					if (line.Length >= 2 && line[0] == '/' || line[1] == '/')
						continue;

					property = line.Split(new char[] { '=' }, 2);

					if (property.Length < 2)
						continue;

					this[currentSection, property[0]] = property[1];
				}
			}
		}

		public IniSection this[string section] {
			get { return _sections[section]; }
			set {
				if (!_sections.ContainsKey(section))
					_sections[section] = new IniSection(section);
				value.Section = section;
				_sections[section] = value;
			}
		}

		public string this[string section, string key] {
			get { return _sections[section][key]; }
			set {
				if (!_sections.ContainsKey(section))
					_sections[section] = new IniSection(section);
				_sections[section][key] = value;
			}
		}

		public string this[string section, string key, string def] {
			get { return _sections[section][key, def]; }
			set {
				if (!_sections.ContainsKey(section))
					_sections[section] = new IniSection(section);
				_sections[section][key] = value;
			}
		}

		public IEnumerable<IniSection> Sections {
			get { return _sections.Values; }
		}

		public void Delete(string section) {
			
		}

		private void _sectionSave(object sender) {
			//_save();
		}
	}
}
