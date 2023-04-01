using System.Collections.Generic;
using GRF.FileFormats.SprFormat;
using GRF.Image;

namespace GRF.FileFormats.ActFormat.Commands {
	public class SpriteCommand : IActCommand {
		#region SpriteEdit enum

		public enum SpriteEdit {
			RemoveAt,
			RemoveRange,
			InsertAt,
			ReplaceAt,
			SpriteSourceChanged,
		}

		#endregion

		private readonly int _count;
		private readonly GrfImage _image;
		private readonly SpriteEdit _modif;
		private readonly Spr _sprite;
		private readonly int _spriteIndex;
		private GrfImage _conflict;
		private List<GrfImage> _conflicts;
		private Spr _oldSprite;
		private CopyStructureAct _previousState;

		public SpriteCommand(Spr sprite) {
			_sprite = sprite;
			_modif = SpriteEdit.SpriteSourceChanged;
		}

		public SpriteCommand(int spriteIndex, SpriteEdit modif) {
			_spriteIndex = spriteIndex;
			_modif = modif;
		}

		public SpriteCommand(int spriteIndex, GrfImage image, SpriteEdit modif) {
			_spriteIndex = spriteIndex;
			_modif = modif;
			_image = image;
		}

		public SpriteCommand(int spriteIndex, int count, SpriteEdit modif) {
			_spriteIndex = spriteIndex;
			_count = count;
			_modif = modif;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			if (_previousState == null)
				_previousState = new CopyStructureAct(act, CopyStructureMode.Actions);

			switch (_modif) {
				case SpriteEdit.RemoveAt:
					_conflict = act.RemoveSprite(_spriteIndex);
					break;
				case SpriteEdit.RemoveRange:
					_conflicts = act.RemoveSprites(_spriteIndex, _count);
					break;
				case SpriteEdit.InsertAt:
					act.Sprite.AddImage(_image, _spriteIndex);
					break;
				case SpriteEdit.ReplaceAt:
					_conflict = act.Sprite.Images[_spriteIndex];
					act.Sprite.Images[_spriteIndex] = _image;
					break;
				case SpriteEdit.SpriteSourceChanged:
					if (_oldSprite == null) {
						_oldSprite = act.Sprite;
					}

					act.SetSprite(_sprite);
					break;
			}

			_previousState.Clean(act);
		}

		public void Undo(Act act) {
			switch (_modif) {
				case SpriteEdit.RemoveAt:
					act.Sprite.AddImage(_conflict, _spriteIndex);
					break;
				case SpriteEdit.RemoveRange:
					for (int i = 0; i < _count; i++) {
						act.Sprite.AddImage(_conflicts[i], _spriteIndex + i);
					}
					break;
				case SpriteEdit.InsertAt:
					act.RemoveSprite(_spriteIndex);
					break;
				case SpriteEdit.ReplaceAt:
					act.Sprite.Images[_spriteIndex] = _conflict;
					break;
				case SpriteEdit.SpriteSourceChanged:
					act.SetSprite(_oldSprite);
					break;
			}

			_previousState.Apply(act);
		}

		public string CommandDescription {
			get {
				switch (_modif) {
					case SpriteEdit.RemoveAt:
						return "Sprite" + CommandsHolder.GetId(_spriteIndex) + " Remove at " + _spriteIndex;
					case SpriteEdit.RemoveRange:
						return "Sprite" + CommandsHolder.GetId(_spriteIndex) + " Remove range (" + _count + ")";
					case SpriteEdit.InsertAt:
						return "Sprite" + CommandsHolder.GetId(_spriteIndex) + " Insert at " + _spriteIndex;
					case SpriteEdit.ReplaceAt:
						return "Sprite" + CommandsHolder.GetId(_spriteIndex) + " Replace at " + _spriteIndex;
					case SpriteEdit.SpriteSourceChanged:
						return "Source sprite changed";
				}

				return "";
			}
		}

		#endregion
	}
}