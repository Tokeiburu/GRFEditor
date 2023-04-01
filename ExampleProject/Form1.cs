using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ErrorManager;
using ExampleProject.ErrorHandlers;
using ExampleProject.ImageConverters;
using ExampleProject.RecentFiles;
using GRF.ContainerFormat;
using GRF.ContainerFormat.Commands;
using GRF.Core;
using GRF.FileFormats;
using GRF.FileFormats.GatFormat;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.SprFormat;
using GRF.Image;
using Utilities;
using Utilities.Services;

namespace ExampleProject {
	public partial class Form1 : Form {
		// GRFHolder is a higher level interface of the GRFData class, it's safer and easier to use.
		// You should not ignore exceptions from GRFHolder; they mean you did something wrong and
		// that the GRFHolder object is not usable anymore (usually because you forgot to close the
		// the grf before opening a new one).
		private readonly GrfHolder _grf = new GrfHolder();
		private readonly ExampleProjectRecentFiles _recentFilesManager;

		// A class for extracting GRF files is required if you want a progress update
		private readonly GrfExtractor _extractor = new GrfExtractor();

		public Form1() {
			Gat gat = new Gat(@"C:\Users\Tokei\AppData\Roaming\GRF Editor\FlatMapsMaker\mhisle01_copy.gat");

			int[] coords = new[] {
				94, 150, 30,
				159,76,	 30,
				359,48,	 30,
				334,170, 30,
				72,279,	 30,
				283,305, 30,
				131,306, 30,
				297,191, 30,
				333,103, 30,
				295,49,	 30,
				191,37,	 30,
				166,109, 30,
				85,98,	 30,
				334,248, 30,
				344,322, 30,
				223,311, 30,
				177,298, 30,
				141,117, 30,
				214,231, 30,
				237,131, 30,
				182,365, 30,
				104,296, 30,
				87,214,	 30,
				153,224, 30,
				184,134, 30,
				128,80,	 30,
				219,59,	 30,
				366,88,	 30,
				22,317,	 30,
				25,191,	 30,
				366,381, 30,
				221,196, 30,
				22,286,	 30,
				31,160,	 30,
				197,176, 30,
				379,336, 30,
				338,370, 30,
				279,356, 30,
				126,338, 30,
				288,233, 30,
				159,367, 31,
				41,337,	 31,
				387,378, 31,
				309,248, 31,
				19,226,	 31,
				161,175, 31,
				245,359, 31,
				194,239, 31,
				78,167,	 31,
				135,149, 31,
				291,102, 31,
				207,85,	 31,
				236,282, 31,
				347,193, 31,
				167,139, 31,
				376,71,	 31,
				150,54,	 31,
				54,119,	 31,
			};

			for (int i = 0; i < coords.Length; i += 3) {
				int x = coords[i];
				int y = coords[i + 1];
				int type = coords[i + 2];

				int x_start = x - 2;
				int y_start = y - 2;

				int x_end = x + 2;
				int y_end = y + 2;

				for (int xx = x_start; xx <= x_end; xx++) {
					for (int yy = y_start; yy <= y_end; yy++) {
						int xy = xx + yy * gat.Header.Width;

						if (gat.Cells[xy].Type == GatType.NoWalkable)
							continue;

						gat.Cells[xy].Type = (GatType)type;
					}
				}
			}

			gat.Save(@"C:\Users\Tokei\AppData\Roaming\GRF Editor\FlatMapsMaker\InputMaps\mhisle01.gat");

			Z.F();


			_initializeApplication();

			InitializeComponent();

			_grf.ContainerOpened += delegate {
				// Adding the events for the undo/redo command, this is purely for visual
				_grf.Commands.CommandExecuted += _commandExecuted;
				_grf.Commands.CommandRedo += _commandExecuted;
				_grf.Commands.CommandUndo += _commandExecuted;
				_grf.Commands.CommandIndexChanged += _commandExecuted;
			};

			// This line isn't necesarry, but it creates a new GRF once opening this application
			_grf.New();

			// From the Utilities library, you will find a ConfigAsker class which provides you with
			// all you need to make a configuration file; see the Configuration class from GRF Editor.
			// The RecentFilesManager class requires a configuration file to save its information,
			// and the configuration files can be shared in such a way that only one configuration file
			// is created. But since this application isn't saving any information, we create a new one.
			// (Properties from the config file are loaded if the file already exists)
			_recentFilesManager = new ExampleProjectRecentFiles(new ConfigAsker("config.txt"), 6, openRecentToolStripMenuItem);
			_recentFilesManager.FileClicked += _recentFilesManager_FileClicked;
			_recentFilesManager.Reload();
		}

		private void _initializeApplication() {
			// Sets the custom image converter from GRFImage to Bitmap image, see ImageConverter1 class implementation
			// for more details.
			ImageConverterManager.AddConverter(new ImageConverter1());

			// Sets the error handler to show exceptions in your own way, the default one uses the console.
			// You could create one that rethrows the exceptions if you want to handle them yourself, but
			// creating a custom error handler is a lot easier. See BasicErrorHandler's class implementation for
			// more details.
			ErrorHandler.SetErrorHandler(new BasicErrorHandler());
		}

		#region Events
		private void _listBox1_SelectedIndexChanged(object sender, EventArgs e) {
			string item = listBox1.SelectedItem as string;

			if (item != null && !_grf.IsBusy) {
				string extension = Path.GetExtension(item);
				try {
					if (extension == ".txt") {
						pictureBox1.Visible = false; textBox2.Visible = true;
						textBox2.Text = EncodingService.DisplayEncoding.GetString(_grf.FileTable[item].GetDecompressedData());
					}
					else if (extension == ".rsw") {
						pictureBox1.Visible = false; textBox2.Visible = true;
						textBox2.Text = FileFormatParser.DisplayObjectProperties(new Rsw(_grf.FileTable[item].GetDecompressedData()));
					}
					else if (extension == ".gnd") {
						pictureBox1.Visible = false; textBox2.Visible = true;
						textBox2.Text = FileFormatParser.DisplayObjectProperties(new Gnd(_grf.FileTable[item].GetDecompressedData()));
					}
					else if (extension == ".rsm" || extension == ".rsm2") {
						pictureBox1.Visible = false; textBox2.Visible = true;
						textBox2.Text = FileFormatParser.DisplayObjectProperties(new Rsm(_grf.FileTable[item].GetDecompressedData()));
					}
					else if (
						extension == ".bmp" || extension == ".tga" || extension == ".png" || extension == ".jpg" ||
						extension == ".pal" || extension == ".gat") {
						pictureBox1.Visible = true; textBox2.Visible = false;
						pictureBox1.Image = ImageProvider.GetImage(_grf.FileTable[item].GetDecompressedData(), extension).Cast<Bitmap>();
					}
					else if (extension == ".spr") {
						pictureBox1.Visible = true; textBox2.Visible = false;
						pictureBox1.Image = new Spr(_grf.FileTable[item].GetDecompressedData()).Images[0].Cast<Bitmap>();
					}
				}
				catch (GrfException err) {
					if (err == GrfExceptions.__ContainerBusy)
						return;

					throw;
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}
		private void _buttonSearch_Click(object sender, EventArgs e) {
			if (_grf.IsOpened) {
				string search = textBox1.Text;
				listBox1.Items.Clear();

				// This is a very basic search
				// Using IndexOf is a lot faster than using file.Contains(...)
				// Also, since I'm using Linq queries a lot and you might not be familar with it, 
				// here's a quick example of the equivalent two lines
				// (You should hopefully see why I'm always going for short Linq queries)
				//
				//foreach (string file in _grf.FileTable.Files) {
				//    if (file.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1)
				//        listBox1.Items.Add(file);
				//}
				//
				// This is very slow because :
				// - You have to evaluate the values one after the other
				// - You add the items one by one, which requires a ton of calls on the object as well
				//   as multiple control redraws.
				//
				// This next one is an equivalent to the Linq query used, which should have the same speed
				//IEnumerable<object> files = from p in _grf.FileTable.Files
				//                            where p.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1
				//                            select (object) p;
				//listBox1.Items.AddRange(files.ToArray());

				listBox1.Items.AddRange(_grf.FileTable.Files.Where(p => p.IndexOf(search, StringComparison.InvariantCultureIgnoreCase) != -1).Cast<object>().ToArray());
			}
		}
		#endregion

		#region Menu
		private void _openToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog {Filter = "GRF Files|*.grf;*.rgz;*.gpf|GRF|*.grf|GPF|*.gpf", CheckFileExists = true};

			if (dialog.ShowDialog() == DialogResult.OK) {
				if (_closeGrf()) {
					_openGrf(dialog.FileName);
					_recentFilesManager.AddRecentFile(dialog.FileName);
				}
			}
		}
		private void _closeToolStripMenuItem_Click(object sender, EventArgs e) {
			this.Close();
		}
		private void _saveToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFileDialog dialog = new SaveFileDialog {Filter = "GRF|*.grf|GPF|*.gpf"};

			if (dialog.ShowDialog() == DialogResult.OK) {
				WaitingWindow wDialog = new WaitingWindow(() => _grf.Save(dialog.FileName), _grf) { StartPosition = FormStartPosition.CenterParent };
				if (wDialog.ShowDialog() == DialogResult.OK) {
					_grf.Close();
					_openGrf(dialog.FileName);
				}
				else {
					ErrorHandler.HandleException("Couldn't save the GRF because the user aborted the operation.", ErrorLevel.NotSpecified);
				}
			}
		}
		private void _addToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFileDialog dialog = new OpenFileDialog {CheckFileExists = true, Multiselect = true};

			if (dialog.ShowDialog() == DialogResult.OK) {
				// This will add new files directly to the data folder, see GRF.GRF.Commands for a
				// list of all available commands.
				// See ExampleGrf's project for an usage of each common commands.
				// You don't have to use a callback, but this is really useful for redo/undo
				_grf.Commands.AddFilesInDirectory("data", dialog.FileNames, _addFilesCallback);
			}
		}
		private void _deleteToolStripMenuItem_Click(object sender, EventArgs e) {
			if (listBox1.SelectedItem != null) {
				// This will delete the selected files, see GRF.GRF.Commands for a list of all available commands.
				// See ExampleGrf's project for an usage of each common commands.
				// You don't have to use a callback, but this is really useful for redo/undo
				_grf.Commands.RemoveFiles(listBox1.SelectedItems.Cast<string>(), _removeFilesCallback);
			}
		}
		private void _newToolStripMenuItem_Click(object sender, EventArgs e) {
			if (_closeGrf()) {
				_grf.New();
				listBox1.Items.Clear();
			}
		}
		private void _recentFilesManager_FileClicked(string file) {
			if (_closeGrf()) {
				if (!File.Exists(file)) {
					MessageBox.Show("File not found.");
					_recentFilesManager.RemoveRecentFile(file);
				}
				else
					_openGrf(file);
			}
		}
		private void _extractToolStripMenuItem_Click(object sender, EventArgs e) {
			if (_grf.IsOpened) {
				FolderBrowserDialog dialog = new FolderBrowserDialog { ShowNewFolderButton = true };
				dialog.Description = "Select a folder to extract the file(s)";

				if (dialog.ShowDialog() == DialogResult.OK) {
					new WaitingWindow(() => _extractor.Extract(_grf, dialog.SelectedPath, listBox1.SelectedItems.Cast<string>()
						.ToList()), _extractor) { StartPosition = FormStartPosition.CenterParent }.ShowDialog();
				}
			}
		}
		#endregion

		#region Undo/redo
		private void _addFilesCallback(List<string> files, List<string> grfFullFilePath, List<string> grfPaths, bool isExecuted) {
			if (isExecuted) {
				listBox1.Items.AddRange(grfFullFilePath.Cast<object>().ToArray());
			}
			else {
				foreach (string file in grfFullFilePath) {
					listBox1.Items.Remove(file);
				}
			}
		}
		private void _removeFilesCallback(List<string> files, bool isExecuted) {
			if (isExecuted)
				files.ForEach(p => listBox1.Items.Remove(p));
			else
				listBox1.Items.AddRange(files.Cast<object>().ToArray());
		}
		private void _undoToolStripMenuItem_Click(object sender, EventArgs e) {
			if (_grf.IsOpened && _grf.Commands.CanUndo)
				_grf.Commands.Undo();
		}
		private void _redoToolStripMenuItem_Click(object sender, EventArgs e) {
			if (_grf.IsOpened && _grf.Commands.CanRedo)
				_grf.Commands.Redo();
		}
		private void _commandExecuted(object sender, IContainerCommand<FileEntry> command) {
			redoToolStripMenuItem.Enabled = _grf.Commands.CanRedo;
			undoToolStripMenuItem.Enabled = _grf.Commands.CanUndo;
		}
		#endregion

		/// <summary>
		/// Simple method to check if the GRF has modifications before closing it.
		/// </summary>
		/// <returns>True if it's ok to close the GRF, false otherwise</returns>
		private bool _closeGrf() {
			if (_grf.IsOpened && _grf.IsModified) {
				if (MessageBox.Show("The current GRF has been modified, are you sure you want to continue?", "Modified GRF", MessageBoxButtons.YesNoCancel) != DialogResult.Yes)
					return false;
			}
			_grf.Close();
			return true;
		}
		private void _showGrfContent() {
			listBox1.Items.Clear();
			listBox1.Items.AddRange(_grf.FileTable.Files.Cast<object>().ToArray());
		}
		private void _openGrf(string fileName) {
			WaitingWindow wDialog = new WaitingWindow(() => _grf.Open(fileName), _grf) { StartPosition = FormStartPosition.CenterParent };
			if (wDialog.ShowDialog() == DialogResult.OK) {
				_showGrfContent();
			}
			else {
				ErrorHandler.HandleException("Couldn't load the GRF because the user aborted the operation.", ErrorLevel.NotSpecified);
			}
		}
		protected override void OnClosing(CancelEventArgs e) {
			if (!_closeGrf()) {
				e.Cancel = true;
				return;
			}

			// Necessary if the debugger thread is running
			Process.GetCurrentProcess().Kill();
			base.OnClosing(e);
		}
		private void _listBox1_MouseDown(object sender, MouseEventArgs e) {
			// Sorry but... ctrl is for multi selection, not the other way around >_>'
			if ((ModifierKeys & Keys.Control) != Keys.Control && listBox1.SelectedItems.Count > 1) {
			    object file = listBox1.Items[listBox1.IndexFromPoint(e.Location)];
			    if (file != null) {
			        listBox1.ClearSelected();
			        listBox1.SelectedItem = file;
			    }
			}
		}
	}
}
