using System;
using System.Linq;
using System.Windows.Controls;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.Core.GrfCompression;
using TokeiLibrary;
using TokeiLibrary.Paths;
using Utilities;

namespace GrfToWpfBridge.Application {
	public class CompressionMethodPicker : ComboBox {
		public CompressionMethodPicker() {
			SetCompression = v => { Compression.CompressionAlgorithm = v; };

			SettingCompressionIndex = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Compression method index]"] = v.ToString(); }, () => Int32.Parse(Configuration.ConfigAsker["[GRFEditor - Compression method index]", "0"]));

			Setting = new Setting(v => { Configuration.ConfigAsker["[Application - Latest compression path]"] = v.ToString(); }, () => Configuration.ConfigAsker["[Application - Latest compression path]", Configuration.ApplicationPath]);

			SettingGuard = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Loading custom DLL state]"] = v.ToString(); }, () => Boolean.Parse(Configuration.ConfigAsker["[GRFEditor - Loading custom DLL state]", false.ToString()]));

			SettingCustomPath = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Custom compression library"] = v.ToString(); }, () => Configuration.ConfigAsker["[GRFEditor - Custom compression library]", ""]);
		}

		public Action<ICompression> SetCompression { get; set; }
		public Setting SettingCompressionIndex { get; set; }
		public Setting Setting { get; set; }
		public Setting SettingGuard { get; set; }
		public Setting SettingCustomPath { get; set; }

		public static void Load() {
			new CompressionMethodPicker().Init();
		}

		public void Init() {
			ItemsSource = Compression.CompressionMethods.Select(p => p.ToString()).Concat(new string[] { GrfStrings.CustomCompression });

			SelectedIndex = (int) SettingCompressionIndex.Get();
			_updateCompression(false);

			SelectionChanged += (s, e) => _updateCompression(true);
		}

		private void _updateCompression(bool userInput) {
			SettingCompressionIndex.Set(SelectedIndex >= Items.Count - 1 ? -1 : SelectedIndex);

			if (SelectedIndex < 0 || SelectedIndex >= Items.Count - 1) {
				try {
					if (SelectedIndex < 0 && !userInput) {
						SelectedIndex = Items.Count - 1;
					}

					string file = userInput ? TkPathRequest.OpenFile(Setting, "filter", "Dll Files|*.dll|All Files|*.*") : (string) SettingCustomPath.Get();

					if (file != null) {
						Compression.CompressionAlgorithm = new CustomCompression(file, SettingGuard);

						if (!Compression.CompressionAlgorithm.Success)
							throw new Exception("Failed to load the decompression library (" + file + ").");

						SettingCustomPath.Set(file);
					}
					else {
						SetCompression(Compression.CompressionMethods[0]);
						SettingCompressionIndex.Set(0);
						SelectedIndex = 0;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
					SettingCompressionIndex.Set(0);
					SelectedIndex = 0;
				}
			}
			else {
				// Direct assign
				try {
					SetCompression(Compression.CompressionMethods[SelectedIndex]);

					if (!Compression.CompressionAlgorithm.Success)
						throw new Exception();
				}
				catch {
					Compression.CompressionAlgorithm = new DotNetCompression();
					SettingCompressionIndex.Set(1);
					ErrorHandler.HandleException("Couldn't load the compression library. The application has been set to the default compression method.");
				}
			}
		}
	}
}