using ErrorManager;
using GRF.FileFormats;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using TokeiLibrary;
using TokeiLibrary.Paths;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.Extension;

namespace GRFEditor.Tools.SpriteEditor {
	public class PaletteOperations {
		private TkWindow _spriteEditorPalette;
		private SpriteConverter _editor;

		public PaletteOperations(SpriteConverter editor) {
			_editor = editor;
		}

		public void ViewPalette(SpriteEditorControl control) {
			if (_spriteEditorPalette == null || _spriteEditorPalette.IsVisible == false) {
				_spriteEditorPalette = new TkWindow("Palette", "help.ico");
				_spriteEditorPalette.ShowInTaskbar = true;
				_spriteEditorPalette.Show();
				UpdatePalette(control);
				_spriteEditorPalette.Owner = _editor;
			}
		}

		public void UpdatePalette(SpriteEditorControl control) {
			if (control == null)
				return;

			if (_spriteEditorPalette != null && _spriteEditorPalette.IsVisible) {
				Image image = new Image();
				image.Width = 256;
				image.Height = 256;

				var spr = control.Spr;

				if (spr.Palette == null || spr.Palette.BytePalette == null) {
					image.Source = null;
				}
				else {
					image.Source = new Pal(Methods.Copy(spr.Palette.BytePalette), Pal.FormatMode.NoTransparency).Image.Cast<BitmapSource>();
				}

				_spriteEditorPalette.Content = image;
			}
		}

		public void ReplacePalette(SpriteEditorControl control) {
			string file = TkPathRequest.OpenFile(SpriteEditorConfiguration.AppLastPath_Config,
				"fileName", SpriteEditorConfiguration.AppLastPath,
				"filter", FileFormat.MergeFilters(FileFormat.PalAndSpr, FileFormat.Pal, FileFormat.Spr));

			if (file != null) {
				if (file.GetExtension() == ".pal") {
					byte[] pal = File.ReadAllBytes(file);

					if (pal.Length != 1024) {
						ErrorHandler.HandleException("Invalid palette file.");
						return;
					}

					control.SetPalette(pal);
				}
				else if (file.IsExtension(".spr")) {
					byte[] pal = new byte[1024];
					Spr spr = new Spr(file);

					if (spr.NumberOfIndexed8Images <= 0) {
						ErrorHandler.HandleException("This palette file doesn't contain any Indexed8 images; no palette found.");
						return;
					}

					Buffer.BlockCopy(spr.Images[0].Palette, 0, pal, 0, 1024);
					control.SetPalette(pal);
				}
				else {
					ErrorHandler.HandleException("Invalid file extension.");
					return;
				}

				UpdatePalette(control);
			}
		}

		public void ReplaceWithDefault(SpriteEditorControl control) {
			byte[] pal = ApplicationManager.GetResource("default.pal");

			control.SetPalette(pal);
			UpdatePalette(control);
		}

		public void ClearPalette(SpriteEditorControl control) {
			byte[] pal = new byte[1024];
			pal[0] = 255;
			pal[2] = 255;

			for (int i = 0; i < 256; i++) {
				pal[4 * i + 3] = 255;
			}

			control.SetPalette(pal);
			UpdatePalette(control);
		}
	}
}
