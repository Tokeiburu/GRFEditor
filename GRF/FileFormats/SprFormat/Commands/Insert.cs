using GRF.FileFormats.ActFormat;
using GRF.FileFormats.ActFormat.Commands;
using GRF.FileFormats.PalFormat;
using GRF.FileFormats.SprFormat.Builder;
using GRF.Image;
using Utilities;

namespace GRF.FileFormats.SprFormat.Commands {
	public class Insert : ICommand, IActCommand {
		private readonly int _absoluteIndex;
		private readonly GrfImage _image;
		private readonly SprBuilderImageView _view;
		private bool? _isPaletteSet;
		private int _relativeIndex = -1;

		public Insert(int absoluteIndex, GrfImage image) {
			_absoluteIndex = absoluteIndex;
			_image = image;
		}

		public Insert(SprBuilderImageView view) {
			_view = view;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_relativeIndex < 0) {
				_relativeIndex = _image.GrfImageType == GrfImageType.Indexed8 ? _absoluteIndex : _absoluteIndex - act.Sprite.NumberOfIndexed8Images;
			}

			if (_isPaletteSet == null) {
				_isPaletteSet = act.Sprite.Palette != null;
			}

			if (!_isPaletteSet.Value) {
				act.Sprite.Palette = new Pal();
			}

			if (_image.GrfImageType == GrfImageType.Indexed8) {
				if (act.Sprite.NumberOfIndexed8Images == 0) {
					act.Sprite.Palette.SetPalette(_image.Palette);
				}
				else {
					if (!Methods.ByteArrayCompare(act.Sprite.Palette.BytePalette, _image.Palette)) {
						byte[] palette = act.Sprite.Palette.BytePalette;
						_image.SetPalette(ref palette);
					}
				}
			}

			act.Sprite.AddImage(_image, _relativeIndex);
		}

		public void Undo(Act act) {
			act.Sprite.Remove(_relativeIndex, _image.GrfImageType);

			if (_isPaletteSet != null && !_isPaletteSet.Value) {
				act.Sprite.Palette = null;
			}
		}

		public string CommandDescription {
			get { return "Sprite added"; }
		}

		#endregion

		#region ICommand Members

		public void Execute(SprBuilderInterface sbi) {
			sbi.Insert(_view);
		}

		public void Undo(SprBuilderInterface sbi) {
			sbi.Delete(_view);
		}

		#endregion
	}
}