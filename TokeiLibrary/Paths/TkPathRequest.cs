using System;
using System.IO;
using System.Windows.Forms;
using System.Windows.Interop;
using TokeiLibrary.Paths.FolderSelect;
using Utilities;
using Utilities.Extension;

namespace TokeiLibrary.Paths {
	public static class TkPathRequest {
		public static SaveFileDialog LatestSaveFileDialog { get; set; }

		private static void _setCommonDialog(FileDialog dialog, params string[] extra) {
			dialog.Filter = "All files (*.*)|*.*";
			dialog.AddExtension = true;

			for (int i = 0; i < extra.Length; i++) {
				if (extra[i] == "defaultExt") {
					dialog.DefaultExt = extra[++i];
				}
				else if (extra[i] == "fileName") {
					dialog.FileName = Path.GetFileName(extra[++i]);
				}
				else if (extra[i] == "initialDirectory") {
					dialog.InitialDirectory = extra[++i];
				}
				else if (extra[i] == "filter") {
					dialog.Filter = extra[++i];
				}
				else if (extra[i] == "filterIndex") {
					dialog.FilterIndex = Int32.Parse(extra[++i]);
				}
				else if (extra[i] == "title") {
					dialog.Title = extra[++i];
				}
				else if (extra[i] == "addExtension") {
					dialog.AddExtension = Boolean.Parse(extra[++i]);
				}
				else {
					throw new Exception("Unrecognized attribute = " + extra[i] + "; value = " + extra[++i]);
				}
			}

			if (!dialog.Filter.Contains("*.*") && !dialog.Filter.Contains(dialog.FileName.GetExtension() ?? "")) {
				try {
					var index = dialog.Filter.IndexOf("*.", StringComparison.Ordinal);

					if (index > -1) {
						// Find the end index
						int endIndex = dialog.Filter.Length >= index + 5 ? index + 5 : dialog.Filter.Length;
						
						for (int i = index + 2; i < index + 10 && i < dialog.Filter.Length; i++) {
							if (!char.IsLetterOrDigit(dialog.Filter[i])) {
								endIndex = i;
								break;
							}

							if (i == dialog.Filter.Length - 1) {
								endIndex = i + 1;
								break;
							}
						}
						
						var newExt = dialog.Filter.Substring(index + 1, endIndex - index - 1);
						dialog.FileName = dialog.FileName.ReplaceExtension(newExt);
					}
				}
				catch { }
			}
		}
		public static string Folder(Setting setting, params string[] extra) {
			FolderSelectDialog dialog = new FolderSelectDialog();

			string description = "Select a folder";
			string selectedPath = (string)setting.Get();
			string fileName = null;

			if (selectedPath.GetExtension() != null) {
				fileName = Path.GetFileName(selectedPath);
				selectedPath = Path.GetDirectoryName(selectedPath);
			}

			for (int i = 0; i < extra.Length; i++) {
				if (extra[i] == "description") {
					description = extra[++i];
				}
				else if (extra[i] == "selectedPath") {
					selectedPath = extra[++i];
				}
			}

			dialog.Title = description;
			dialog.InitialDirectory = selectedPath;

			var parent = WpfUtilities.TopWindow;
			bool res = false;

			if (parent == null) {
				res = dialog.ShowDialog();
			}
			else {
				res = dialog.ShowDialog(new WindowInteropHelper(parent).Handle);
			}

			if (res) {
				if (fileName != null) {
					setting.Set(Path.Combine(dialog.FileName, fileName));
				}
				else {
					setting.Set(dialog.FileName);
				}

				return dialog.FileName;
			}

			return null;
		}

		public static string Folder<T>(string setting, params string[] extra) {
			return Folder(new Setting(null, typeof(T).GetProperty(setting)), extra);
		}

		public static string Folder(params string[] extra) {
			FolderSelectDialog dialog = new FolderSelectDialog();

			dialog.Title = "Select a folder";
			
			for (int i = 0; i < extra.Length; i++) {
				if (extra[i] == "description") {
					dialog.Title = extra[++i];
				}
				else if (extra[i] == "selectedPath") {
					string selectedPath = extra[++i];

					if (selectedPath.GetExtension() != null) {
						dialog.InitialDirectory = Path.GetDirectoryName(selectedPath);
					}
					else {
						dialog.InitialDirectory = selectedPath;
					}
				}
			}

			if (dialog.ShowDialog(new WindowInteropHelper(WpfUtilities.TopWindow).Handle)) {
				return dialog.FileName;
			}

			return null;
		}

		public static string OpenFile<T>(string setting, params string[] extra) {
			return OpenFile(new Setting(null, typeof(T).GetProperty(setting)), extra);
		}

		public static string OpenFile(Setting setting, params string[] extra) {
			OpenFileDialog dialog = new OpenFileDialog();
			string currentPath = (string)setting.Get();

			dialog.Title = "Select a file";
			dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
			dialog.FileName = Path.GetFileName(currentPath);

			_setCommonDialog(dialog, extra);

			dialog.Multiselect = false;
			dialog.CheckFileExists = true;
			dialog.CheckPathExists = true;

			if (dialog.ShowDialog() == DialogResult.OK) {
				setting.Set(dialog.FileName);
				return dialog.FileName;
			}

			return null;
		}

		public static string SaveFile<T>(string setting, params string[] extra) {
			return SaveFile(new Setting(null, typeof(T).GetProperty(setting)), extra);
		}

		public static string SaveFile(Setting setting, params string[] extra) {
			SaveFileDialog dialog = new SaveFileDialog();
			LatestSaveFileDialog = dialog;
			string currentPath = (string)setting.Get();

			dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
			dialog.FileName = Path.GetFileName(currentPath);
			dialog.Title = "Save file";

			_setCommonDialog(dialog, extra);

			if (dialog.ShowDialog() == DialogResult.OK) {
				setting.Set(dialog.FileName);
				return dialog.FileName;
			}

			return null;
		}

		public static string[] OpenFiles<T>(string setting, params string[] extra) {
			return OpenFiles(new Setting(null, typeof(T).GetProperty(setting)), extra);
		}

		public static string[] OpenFiles(Setting setting, params string[] extra) {
			OpenFileDialog dialog = new OpenFileDialog();
			string currentPath = (string)setting.Get();

			dialog.Title = "Select a file";
			dialog.InitialDirectory = Path.GetDirectoryName(currentPath);
			dialog.FileName = Path.GetFileName(currentPath);

			_setCommonDialog(dialog, extra);

			dialog.Multiselect = true;
			dialog.CheckFileExists = true;
			dialog.CheckPathExists = true;

			if (dialog.ShowDialog() == DialogResult.OK) {
				setting.Set(dialog.FileName);
				return dialog.FileNames;
			}

			return null;
		}
	}
}
