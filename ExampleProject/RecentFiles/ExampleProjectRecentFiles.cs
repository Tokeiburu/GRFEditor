using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Utilities;

namespace ExampleProject.RecentFiles {
	/// <summary>
	/// This class shows how to create a recent file menu; the base class
	/// handles the logic and this class deals with updating the menu.
	/// </summary>
	public class ExampleProjectRecentFiles : RecentFilesManager {
		private readonly ToolStripMenuItem _menuItem;

		public ExampleProjectRecentFiles(ConfigAsker config, int numberOfFiles, ToolStripMenuItem menuItem) : base(config, numberOfFiles) {
			_menuItem = menuItem;
			RecentFilesChanged += _exampleProjectRecentFiles_RecentFilesChanged;
		}

		private void _exampleProjectRecentFiles_RecentFilesChanged(List<string> cutNames, List<string> fullFileNames) {
			_menuItem.DropDownItems.Clear();
			_menuItem.Enabled = cutNames.Count != 0;

			for (int i = 0; i < cutNames.Count; i++) {
				ToolStripMenuItem item = new ToolStripMenuItem();
				item.Text = (i + 1) + ".  " + cutNames[i];
				int fileIndex = i;
				item.Click += new EventHandler((s, e) => OnFileClicked(fullFileNames[fileIndex]));
				_menuItem.DropDownItems.Add(item);
			}
		}
	}
}
