using System;
using Utilities;
using Utilities.Commands;

namespace GRF.FileFormats.PalFormat {
	internal class PaletteChange : IPaletteCommand, ICombinableCommand {
		private readonly int _absoluteOffset;
		private byte[] _newBytes;
		private byte[] _oldBytes;

		public PaletteChange(int offset1024, byte[] newBytes) {
			_absoluteOffset = offset1024;
			_newBytes = newBytes;
		}

		public PaletteChange(int offset1024, byte[] oldBytes, byte[] newBytes) {
			_absoluteOffset = offset1024;
			_newBytes = newBytes;
			_oldBytes = oldBytes;
		}

		#region ICombinableCommand Members

		public bool CanCombine(ICombinableCommand command) {
			PaletteChange cmd = command as PaletteChange;

			if (cmd != null) {
				if (cmd._oldBytes == null || _oldBytes == null || _absoluteOffset != cmd._absoluteOffset)
					return false;

				if (Methods.ByteArrayCompare(cmd._oldBytes, _oldBytes)) {
					return true;
				}
			}

			return false;
		}

		public void Combine<T>(ICombinableCommand command, AbstractCommand<T> abstractCommand) {
			PaletteChange cmd = command as PaletteChange;

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
				Buffer.BlockCopy(palette.BytePalette, _absoluteOffset, _oldBytes, 0, _oldBytes.Length);
			}

			palette.SetBytes(_absoluteOffset, _newBytes);
		}

		public void Undo(Pal palette) {
			palette.SetBytes(_absoluteOffset, _oldBytes);
		}

		public string CommandDescription {
			get { return "Palette changed (changed at " + Pal.Offset1024ToOffset32(_absoluteOffset) + ")"; }
		}

		#endregion
	}
}