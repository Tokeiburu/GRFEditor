using System.Collections.Generic;
using System.Linq;
using Utilities.Extension;

namespace Utilities {
	public abstract class RecentFilesManager {
		#region Delegates

		public delegate void RFMFileClickedEventHandler(string file);
		public delegate void RFMFilesChangedEventHandler(List<string> cutNames, List<string> fullFileNames);

		#endregion

		private readonly ConfigAsker _config;
		private readonly string _configName;
		private readonly int _numberOfFiles;
		private readonly bool _useAdvancedSeperator;
		private readonly List<string> _files = new List<string>();

		public List<string> Files {
			get { return _files; }
		}

		protected RecentFilesManager(ConfigAsker config, int numberOfFiles, string name, bool useAdvancedSeperator) {
			_config = config;
			_numberOfFiles = numberOfFiles;
			_useAdvancedSeperator = useAdvancedSeperator;
			_configName = "[" + (name == null ? "" : name + " - ") + "Recently opened files]";
		}

		protected RecentFilesManager(ConfigAsker config, int numberOfFiles, string groupName)
			: this(config, numberOfFiles, groupName, false) {
		}

		protected RecentFilesManager(ConfigAsker config, int numberOfFiles)
			: this(config, numberOfFiles, null, false) {
		}

		public event RFMFilesChangedEventHandler RecentFilesChanged;
		public event RFMFileClickedEventHandler FileClicked;

		public void Reload() {
			_loadRecentFiles();
		}

		protected void _moveTopIfExists(string file) {
			if (_files.Contains(file))
				AddRecentFile(file);
		}

		private int _count(string value, char c) {
			int count = 0;
			for (int i = 0; i < value.Length; i++) {
				if (value[i] == c)
					count++;
			}

			return count;
		}

		private const string _emptyStringIdentifier = "__%EmptyString%";
		private const string _nullStringIdentifier = "__%NullString%";

		private void _loadRecentFiles() {
			_files.Clear();

			var content = _config[_configName, ""].Replace(_emptyStringIdentifier, "").Replace(_nullStringIdentifier, "");

			List<string> files;
			if (_useAdvancedSeperator) {
				files = content.Split(new string[] { Methods.AdvSeperator }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
			}
			else {
				files = content.Split('>').ToList();
			}

			foreach (string file in files) {
				if (file.Length < 4) continue;
				if (_useAdvancedSeperator) {
					_files.Add(file);
					continue;
				}
				if (_count(file, ':') > 1) {
					_files.AddRange(file.Split(','));
				}
				else {
					_files.Add(file);
				}
			}

			List<string> cutNames = new List<string>();
			List<string> fullFileNames = new List<string>();

			foreach (string file in _files) {
				fullFileNames.Add(file);
				cutNames.Add(Methods.CutFileName(file, 40));
			}

			if (RecentFilesChanged != null)
				RecentFilesChanged(cutNames, fullFileNames);
		}

		public void AddRecentFile(string file) {
			if (_files.Contains(file)) {
				_files.Remove(file);
			}

			_files.Insert(0, file);
			while (_files.Count > _numberOfFiles) {
				_files.RemoveAt(_files.Count - 1);
			}

			if (_useAdvancedSeperator) {
				_config[_configName] = Methods.ListToString(_files, Methods.AdvSeperator);
			}
			else {
				_config[_configName] = Methods.ListToString(_files, '>');
			}
			_loadRecentFiles();
		}

		public void RemoveRecentFile(string file) {
			if (_files.Contains(file)) {
				_files.Remove(file);
			}
			if (_useAdvancedSeperator) {
				_config[_configName] = Methods.ListToString(_files, Methods.AdvSeperator);
			}
			else {
				_config[_configName] = Methods.ListToString(_files, '>');
			}
			_loadRecentFiles();
		}

		public virtual void OnFileClicked(string file) {
			if (FileClicked != null)
				FileClicked(file);
			_moveTopIfExists(file);
		}
	}
}