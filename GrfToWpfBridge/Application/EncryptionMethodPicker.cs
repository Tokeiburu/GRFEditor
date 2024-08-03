using System;
using System.Linq;
using System.Windows.Controls;
using ErrorManager;
using GRF;
using GRF.Core;
using TokeiLibrary;
using TokeiLibrary.Paths;
using Utilities;

namespace GrfToWpfBridge.Application {
	public class EncryptionMethodPicker : ComboBox {
		public EncryptionMethodPicker() {
			SetEncryption = v => { Encryption.Encryptor = v; };

			SettingEncryptionIndex = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Encryption method index]"] = v.ToString(); }, () => Int32.Parse(Configuration.ConfigAsker["[GRFEditor - Encryption method index]", "0"]));

			Setting = new Setting(v => { Configuration.ConfigAsker["[Application - Latest encryption path]"] = v.ToString(); }, () => Configuration.ConfigAsker["[Application - Latest encryption path]", Configuration.ApplicationPath]);

			SettingGuard = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Loading custom encryption DLL state]"] = v.ToString(); }, () => Boolean.Parse(Configuration.ConfigAsker["[GRFEditor - Loading custom encryption DLL state]", false.ToString()]));

			SettingCustomPath = new Setting(v => { Configuration.ConfigAsker["[GRFEditor - Custom encryption library]"] = v.ToString(); }, () => Configuration.ConfigAsker["[GRFEditor - Custom encryption library]", ""]);
		}

		public Action<IEncryption> SetEncryption { get; set; }
		public Setting SettingEncryptionIndex { get; set; }
		public Setting Setting { get; set; }
		public Setting SettingGuard { get; set; }
		public Setting SettingCustomPath { get; set; }

		public static void Load() {
			new EncryptionMethodPicker().Init();
		}

		public void Init() {
			ItemsSource = new string[] { "Default", GrfStrings.CustomEncryption }.ToList();

			SelectedIndex = (int) SettingEncryptionIndex.Get();
			_updateCompression(false);

			SelectionChanged += (s, e) => _updateCompression(true);
		}

		private void _updateCompression(bool userInput) {
			SettingEncryptionIndex.Set(SelectedIndex >= Items.Count - 1 ? -1 : SelectedIndex);

			if (SelectedIndex < 0 || SelectedIndex >= Items.Count - 1) {
				try {
					if (SelectedIndex < 0 && !userInput) {
						SelectedIndex = Items.Count - 1;
					}

					string file = userInput ? TkPathRequest.OpenFile(Setting, "filter", "Dll Files|*.dll|All Files|*.*") : (string) SettingCustomPath.Get();

					if (file != null) {
						Encryption.Encryptor = new CustomEncryption(file, SettingGuard);

						if (!Encryption.Encryptor.Success) {
							throw new Exception("Failed to load the encryption library (" + file + ").", Encryption.Encryptor.LastException);
						}

						SettingCustomPath.Set(file);
					}
					else {
						SetEncryption(Encryption.DefaultEncryption);
						SettingEncryptionIndex.Set(0);
						SelectedIndex = 0;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
					SettingEncryptionIndex.Set(0);
					SelectedIndex = 0;
				}
			}
			else {
				// Direct assign
				try {
					SetEncryption(Encryption.DefaultEncryption);

					if (!Encryption.Encryptor.Success)
						throw new Exception();
				}
				catch {
					Encryption.Encryptor = Encryption.DefaultEncryption;
					SettingEncryptionIndex.Set(0);
					ErrorHandler.HandleException("Couldn't load the encryption library. The application has been set to the default encryption method.");
				}
			}
		}
	}
}