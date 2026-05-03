using System;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.FileFormats.PalFormat;

namespace GRF.FileFormats.SprFormat.Commands {
	public class ChangePalette : IActCommand {
		private readonly byte[] _palette = new byte[1024];
		private bool? _isPaletteSet;
		private byte[] _oldPalette;

		public ChangePalette(byte[] palette) {
			Buffer.BlockCopy(palette, 0, _palette, 0, 1024);

			// The palette transparency is always set to 0 for ActEditor
			_palette[3] = 0;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_isPaletteSet == null) {
				_isPaletteSet = act.Sprite.Palette != null;

				if (!_isPaletteSet.Value) {
					act.Sprite.Palette = new Pal();
				}

				_oldPalette = new byte[1024];
				Buffer.BlockCopy(act.Sprite.Palette.BytePalette, 0, _oldPalette, 0, 1024);
				_oldPalette[3] = 0;
			}

			if (act.Sprite.Palette == null) {
				act.Sprite.Palette = new Pal();
			}

			byte[] palette = _palette;

			act.Sprite.Palette.SetPalette(palette);

			for (int i = 0; i < act.Sprite.NumberOfIndexed8Images; i++) {
				act.Sprite.Images[i].SetPalette(palette);
			}
		}

		public void Undo(Act act) {
			if (_isPaletteSet != null && !_isPaletteSet.Value) {
				act.Sprite.Palette = null;
			}

			if (act.Sprite.Palette != null && _oldPalette != null) {
				act.Sprite.Palette.SetPalette(_oldPalette);
			}

			for (int i = 0; i < act.Sprite.NumberOfIndexed8Images; i++) {
				act.Sprite.Images[i].SetPalette(_oldPalette);
			}
		}

		public string CommandDescription {
			get { return "Sprite palette modified"; }
		}

		#endregion
	}
}