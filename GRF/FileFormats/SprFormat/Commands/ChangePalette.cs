using System;
using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat.Builder;

namespace GRF.FileFormats.SprFormat.Commands {
	public class ChangePalette : ICommand, IActCommand {
		private readonly byte[] _palette = new byte[1024];
		private bool? _isPaletteSet;
		private byte[] _oldPalette;

		public ChangePalette(byte[] palette) {
			Buffer.BlockCopy(palette, 0, _palette, 0, 1024);
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
			}

			if (act.Sprite.Palette == null) {
				act.Sprite.Palette = new Pal();
			}

			byte[] palette = _palette;

			act.Sprite.Palette.SetPalette(palette);

			for (int i = 0; i < act.Sprite.NumberOfIndexed8Images; i++) {
				act.Sprite.Images[i].SetPalette(ref palette);
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
				act.Sprite.Images[i].SetPalette(ref _oldPalette);
			}
		}

		public string CommandDescription {
			get { return "Sprite palette modified"; }
		}

		#endregion

		#region ICommand Members

		public void Execute(SprBuilderInterface sbi) {
			if (_oldPalette == null) {
				_oldPalette = new byte[1024];
				Buffer.BlockCopy(sbi.GetPalette(), 0, _oldPalette, 0, 1024);
			}

			sbi.SetPalette(_palette);
		}

		public void Undo(SprBuilderInterface sbi) {
			sbi.SetPalette(_oldPalette);
		}

		#endregion
	}
}