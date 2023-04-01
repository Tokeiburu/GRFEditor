using System;
using System.Collections.Generic;
using System.Linq;
using GRF.FileFormats.PalFormat;
using GRF.Image;
using Utilities;

namespace GRF.FileFormats.ActFormat.Commands {
	[Flags]
	public enum CopyStructureMode {
		Sprite = 1 << 1,
		SoundFiles = 1 << 2,
		Actions = 1 << 3,
		Full = Sprite | SoundFiles | Actions,
	}

	public class CopyStructureAct {
		private readonly byte[] _palette;
		public List<Action> Actions;
		public List<GrfImage> Images;
		public List<string> SoundFiles;

		private Dictionary<int, Action> _changedActions;
		private Dictionary<int, string> _changedSounds;
		private Dictionary<int, GrfImage> _changedSprites;
		private bool _hasBeenCleaned;

		public CopyStructureAct(Act act, CopyStructureMode mode) {
			if ((mode & CopyStructureMode.Actions) == CopyStructureMode.Actions) {
				Actions = new List<Action>();

				foreach (Action action in act) {
					Actions.Add(new Action(action));
				}
			}

			if ((mode & CopyStructureMode.SoundFiles) == CopyStructureMode.SoundFiles) {
				SoundFiles = new List<string>(act.SoundFiles);
			}

			if ((mode & CopyStructureMode.Sprite) == CopyStructureMode.Sprite) {
				Images = new List<GrfImage>(act.Sprite.Images.Count);

				foreach (GrfImage image in act.Sprite.Images) {
					Images.Add(image.Copy());
				}

				if (act.Sprite.Palette != null) {
					_palette = new byte[1024];
					Buffer.BlockCopy(act.Sprite.Palette.BytePalette, 0, _palette, 0, 1024);
				}
			}
		}

		public void Apply(Act act) {
			if (!_hasBeenCleaned) {
				if (Actions != null)
					act.Actions = Actions.Select(action => new Action(action)).ToList();

				if (SoundFiles != null) {
					act.SoundFiles.Clear();
					act.SoundFiles.AddRange(SoundFiles);
				}

				if (Images != null) {
					act.Sprite.Images.Clear();
					act.Sprite.Images.AddRange(Images.Select(p => p.Copy()));
					act.Sprite.ReloadCount();
				}
			}
			else {
				if (Actions != null) {
					if (_changedActions != null) {
						foreach (var pair in _changedActions) {
							act.Actions[pair.Key] = new Action(pair.Value);
						}
					}
					else {
						act.Actions = Actions.Select(action => new Action(action)).ToList();
					}
				}

				if (SoundFiles != null) {
					if (_changedSounds != null) {
						foreach (var pair in _changedSounds) {
							act.SoundFiles[pair.Key] = pair.Value;
						}
					}
					else {
						act.SoundFiles.Clear();
						act.SoundFiles.AddRange(SoundFiles);
					}
				}

				if (Images != null) {
					if (_palette == null) {
						act.Sprite.Palette = null;
					}
					else {
						if (act.Sprite.Palette == null)
							act.Sprite.Palette = new Pal();

						act.Sprite.Palette.SetPalette(_palette);
					}

					if (_changedSprites != null) {
						foreach (var pair in _changedSprites) {
							act.Sprite.Images[pair.Key] = pair.Value.Copy();
							act.Sprite.ReloadCount();
						}
					}
					else {
						act.Sprite.Images.Clear();
						act.Sprite.Images.AddRange(Images.Select(p => p.Copy()));
						act.Sprite.ReloadCount();
					}
				}
			}
		}

		public void Clean(Act act) {
			if (!_hasBeenCleaned) {
				if (Actions != null && act.NumberOfActions == Actions.Count) {
					_changedActions = new Dictionary<int, Action>();

					for (int i = 0; i < act.NumberOfActions; i++) {
						if (!act[i].Equals(Actions[i])) {
							_changedActions[i] = Actions[i];
						}
					}

					Actions.Clear();
				}

				if (SoundFiles != null && act.SoundFiles.Count == SoundFiles.Count) {
					_changedSounds = new Dictionary<int, string>();

					for (int i = 0; i < SoundFiles.Count; i++) {
						if (act.SoundFiles[i] != SoundFiles[i]) {
							_changedSounds[i] = SoundFiles[i];
						}
					}

					SoundFiles.Clear();
				}

				if (Images != null && act.Sprite.Images.Count == Images.Count) {
					_changedSprites = new Dictionary<int, GrfImage>();

					for (int i = 0; i < Images.Count; i++) {
						if (!act.Sprite.Images[i].Equals(Images[i])) {
							_changedSprites[i] = Images[i];
						}
					}

					Images.Clear();
				}

				_hasBeenCleaned = true;
			}
		}

		public bool HasChanged(Act act) {
			if (!_hasBeenCleaned) {
				return Actions != null || SoundFiles != null || Images != null;
			}

			if (Actions != null) {
				if (Actions.Count > 0) return true;
				if (_changedActions == null) return true;
				if (_changedActions.Any()) return true;
				// Else : inconclusive
			}

			if (SoundFiles != null) {
				if (SoundFiles.Count > 0) return true;
				if (_changedSounds == null) return true;
				if (_changedSounds.Any()) return true;
				// Else : inconclusive
			}

			if (Images != null) {
				if (Images.Count > 0) return true;
				if (_changedSprites == null) return true;
				if (_changedSprites.Any()) return true;
				// Else : inconclusive

				if (_palette == null && act.Sprite.Palette == null) {
				}
				else if (_palette != null && act.Sprite.Palette != null) {
					if (!Methods.ByteArrayCompare(_palette, act.Sprite.Palette.BytePalette)) {
						return true;
					}
				}
				else {
					return true;
				}
			}

			return false;
		}
	}
}