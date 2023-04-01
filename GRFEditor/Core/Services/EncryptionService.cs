using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ErrorManager;
using GRF.Core;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.WPF;

namespace GRFEditor.Core.Services {
	internal class EncryptionService {
		public static bool Encrypt(GrfHolder grfHolder, ListView list) {
			try {
				return Encrypt(grfHolder, list.SelectedItems.Cast<FileEntry>().ToList());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		public static bool Encrypt(GrfHolder grfHolder, List<FileEntry> entries) {
			try {
				entries = entries.Where(p => !p.Encrypted).ToList();

				if (entries.Count == 0) {
					ErrorHandler.HandleException("Found no files to encrypt.", ErrorLevel.Low);
				}
				else {
					if (grfHolder.Header.EncryptionKey == null) {
						_askAndSetEncryptionKey(grfHolder);
					}
					if (grfHolder.Header.EncryptionKey != null) {
						grfHolder.Commands.EncryptFiles(entries.Select(p => p.RelativePath));
						return true;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
			return false;
		}

		public static bool Decrypt(GrfHolder grfHolder, ListView list) {
			try {
				return Decrypt(grfHolder, list.SelectedItems.Cast<FileEntry>().ToList());
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
		}

		public static bool Decrypt(GrfHolder grfHolder, List<FileEntry> entries) {
			try {
				entries = entries.Where(p => p.Encrypted).ToList();

				if (entries.Count == 0) {
					ErrorHandler.HandleException("No encrypted files are selected.", ErrorLevel.Low);
				}
				else {
					if (grfHolder.Header.EncryptionKey == null) {
						_askAndSetEncryptionKey(grfHolder);
					}
					if (grfHolder.Header.EncryptionKey != null) {
						grfHolder.Commands.DecryptFiles(entries.Select(p => p.RelativePath));
						return true;
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
			return false;
		}

		public static bool RequestDecryptionKey(GrfHolder grfHolder) {
			if (grfHolder.Header.EncryptionKey == null) {
				return (bool) Application.Current.Dispatcher.Invoke((Func<bool>) (() => {
					try {
						_askAndSetEncryptionKey(grfHolder);
						return grfHolder.Header.EncryptionKey != null;
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
						return false;
					}
				}));
			}

			return false;
		}

		private static void _askAndSetEncryptionKey(GrfHolder grfHolder) {
			EncryptorInputKeyDialog dialog = new EncryptorInputKeyDialog("No encryption key has been set.");
			dialog.Owner = Application.Current.MainWindow;
			dialog.ShowDialog();

			if (dialog.Result == MessageBoxResult.OK) {
				GrfEditorConfiguration.EncryptorPassword = dialog.Key;
				grfHolder.Header.SetKey(dialog.Key, grfHolder);
			}
		}
	}
}