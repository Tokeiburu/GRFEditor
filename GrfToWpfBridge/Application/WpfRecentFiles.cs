using System;
using System.Collections.Generic;
using System.Windows.Controls;
using Utilities;

namespace GrfToWpfBridge.Application {
	public class WpfRecentFiles : RecentFilesManager {
		private readonly MenuItem _menuItem;

		public WpfRecentFiles(ConfigAsker config, int numberOfFiles, MenuItem menuItem, string group, bool useAdvanced)
			: base(config, numberOfFiles, group, useAdvanced) {
			_menuItem = menuItem;
			_menuItem.IsEnabled = false;
			RecentFilesChanged += _exampleProjectRecentFiles_RecentFilesChanged;
			Reload();
		}

		public WpfRecentFiles(ConfigAsker config, int numberOfFiles, MenuItem menuItem, string group)
			: this(config, numberOfFiles, menuItem, group, false) {
		}

		private void _exampleProjectRecentFiles_RecentFilesChanged(List<string> cutNames, List<string> fullFileNames) {
			_menuItem.Dispatcher.Invoke(new Action(delegate {
				_menuItem.Items.Clear();
				_menuItem.IsEnabled = cutNames.Count != 0;

				for (int i = 0; i < cutNames.Count; i++) {
					MenuItem item = new MenuItem();
					item.Header = new TextBlock { Text = cutNames[i] };
					item.Icon = "  " + (i + 1);
					int fileIndex = i;
					item.Click += (s, e) => OnFileClicked(fullFileNames[fileIndex]);
					_menuItem.Items.Add(item);
				}
			}));
		}
	}
}