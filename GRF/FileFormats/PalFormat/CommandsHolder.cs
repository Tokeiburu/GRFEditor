using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.PalFormat {
	public class CommandsHolder : AbstractCommand<IPaletteCommand> {
		private readonly Pal _palette;

		public CommandsHolder(Pal palette) {
			_palette = palette;
		}

		protected override void _execute(IPaletteCommand command) {
			command.Execute(_palette);
		}

		protected override void _undo(IPaletteCommand command) {
			command.Undo(_palette);
		}

		protected override void _redo(IPaletteCommand command) {
			command.Execute(_palette);
		}

		public void SetPalette(byte[] newPalette, bool ignoreFirstPixel = true) {
			if (ignoreFirstPixel) {
				if (Methods.ByteArrayCompare(newPalette, 4, 1020, _palette.BytePalette, 4)) {
					return;
				}
			}
			else {
				if (Methods.ByteArrayCompare(newPalette, _palette.BytePalette)) {
					return;
				}
			}

			_palette.Commands.StoreAndExecute(new PaletteChange(0, newPalette));
		}

		public void SetRawBytesInPalette(int offset1024, byte[] data) {
			if (Methods.ByteArrayCompare(data, 0, data.Length, _palette.BytePalette, offset1024)) {
				return;
			}

			_palette.Commands.StoreAndExecute(new PaletteChange(offset1024, data));
		}

		public void SetRawBytesInPalette(int offset1024, byte[] oldData, byte[] newData) {
			if (Methods.ByteArrayCompare(oldData, newData)) {
				return;
			}

			_palette.Commands.StoreAndExecute(new PaletteChange(offset1024, oldData, newData));
		}

		public void ChangeColor(int offset256, byte[] newBytes) {
			if (Methods.ByteArrayCompare(newBytes, offset256 * 4, newBytes.Length, _palette.BytePalette, offset256 * 4)) {
				return;
			}

			_palette.Commands.StoreAndExecute(new ColorChange(offset256, newBytes));
		}

		public void ChangeColor(int offset256, byte[] oldBytes, byte[] newBytes) {
			if (Methods.ByteArrayCompare(newBytes, oldBytes)) {
				return;
			}

			_palette.Commands.StoreAndExecute(new ColorChange(offset256, oldBytes, newBytes));
		}

		public void Begin() {
			_palette.Commands.BeginEdit(new GroupCommand(_palette, false));
		}

		public void BeginNoDelay() {
			_palette.Commands.BeginEdit(new GroupCommand(_palette, true));
		}

		public void End() {
			_palette.Commands.EndEdit();
		}
	}
}