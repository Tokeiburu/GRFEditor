using System;
using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.PalFormat {
	internal class ColorChange : IPaletteCommand, ICombinableCommand {
		private readonly int _offset256;
		private byte[] _newBytes;
		private byte[] _oldBytes;

		public ColorChange(int offset256, byte[] newBytes) {
			_offset256 = offset256;
			_newBytes = newBytes;
		}

		public ColorChange(int offset256, byte[] oldBytes, byte[] newBytes) {
			_offset256 = offset256;
			_newBytes = newBytes;
			_oldBytes = oldBytes;
		}

		#region ICombinableCommand Members

		public bool CanCombine(ICombinableCommand command) {
			var cmd = command as ColorChange;

			if (cmd != null) {
				if (_oldBytes == null || cmd._oldBytes == null || _offset256 != cmd._offset256)
					return false;

				if (Methods.ByteArrayCompare(_oldBytes, cmd._oldBytes)) {
					return true;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			var cmd = command as ColorChange;

			if (cmd != null) {
				_newBytes = cmd._newBytes;
				abstractCommand.ExplicitCommandExecution((T) (object) this);
			}
		}

		#endregion

		#region IPaletteCommand Members

		public void Execute(Pal palette) {
			if (_oldBytes == null) {
				_oldBytes = new byte[_newBytes.Length];
				Buffer.BlockCopy(palette.BytePalette, _offset256 * 4, _oldBytes, 0, _oldBytes.Length);
			}

			palette.SetBytes(_offset256 * 4, _newBytes);
		}

		public void Undo(Pal palette) {
			palette.SetBytes(_offset256 * 4, _oldBytes);
		}

		public string CommandDescription {
			get { return "Color changed at " + _offset256; }
		}

		#endregion
	}
}