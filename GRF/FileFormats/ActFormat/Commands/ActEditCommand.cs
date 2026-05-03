using GRF.Image;
using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace GRF.FileFormats.ActFormat.Commands {
	public class ActEditCommand : IActCommand {
		protected enum ActImageChangeType {
			Delete,
			Replace,
		}

		protected class ActImageChange {
			public GrfImage Image;
			public ActImageChangeType Type;
			public int Index;

			public ActImageChange(int index, GrfImage image, ActImageChangeType action) {
				Image = image;
				Index = index;
				Type = action;
			}
		}

		public enum ActEditTypes {
			NoChange,
			Modified,
			Deleted,
			Added,
		}

		private string _commandName;
		private List<Action> _oldActions;
		private List<string> _oldSounds;
		private List<GrfImage> _oldImages;
		private (ActEditTypes EditType, byte[] Old, byte[] New) _changedPalette;
		private List<string> _newSounds;
		private byte[] _oldPalette;

		private Dictionary<int, (ActEditTypes EditType, Action Old, Action New)> _changedActions = new Dictionary<int, (ActEditTypes EditType, Action Old, Action New)>();
		private Dictionary<int, (ActEditTypes EditType, GrfImage Old, GrfImage New)> _changedImages = new Dictionary<int, (ActEditTypes EditType, GrfImage Old, GrfImage New)>();

		private bool _hasBeenCleaned = false;

		public string CommandDescription => _commandName;

		public ActEditCommand(Act act, string commandName) {
			_commandName = commandName;
			_oldActions = act.CopyActions();
			_oldSounds = new List<string>(act.SoundFiles);
			_oldImages = act.Sprite.Images.Select(p => p.Copy()).ToList();
			
			if (act.Sprite.Palette != null) {
				_oldPalette = Methods.Copy(act.Sprite.Palette.BytePalette);
			}
		}

		public void Execute(Act act) {
			int oldActionCount = act.NumberOfActions;
			_applyChanges(act, true);
			act.InvalidateSpriteVisual();
			if (oldActionCount != act.NumberOfActions)
				act.OnActionCountChanged();
		}

		public void Undo(Act act) {
			int oldActionCount = act.NumberOfActions;
			_applyChanges(act, false);
			act.InvalidateSpriteVisual();
			if (oldActionCount != act.NumberOfActions)
				act.OnActionCountChanged();
		}

		public bool HasChanged(Act act) {
			Clean(act);

			return
				_changedPalette.EditType != ActEditTypes.NoChange ||
				_oldSounds != null ||
				_changedActions.Any(p => p.Value.EditType != ActEditTypes.NoChange) ||
				_changedImages.Any(p => p.Value.EditType != ActEditTypes.NoChange);
		}

		public void Clean(Act act) {
			if (_hasBeenCleaned)
				return;
			
			// Current actions
			for (int i = 0; i < Math.Min(_oldActions.Count, act.Actions.Count); i++) {
				_changedActions[i] = _oldActions[i].Equals(act.Actions[i]) ? (ActEditTypes.NoChange, null, null) : (ActEditTypes.Modified, _oldActions[i].Clone(), act[i].Clone());
			}

			// Added actions
			for (int i = _oldActions.Count; i < act.Actions.Count; i++) {
				_changedActions[i] = (ActEditTypes.Added, null, act[i].Clone());
			}

			// Deleted actions
			for (int i = act.Actions.Count; i < _oldActions.Count; i++) {
				_changedActions[i] = (ActEditTypes.Deleted, _oldActions[i].Clone(), null);
			}

			if (_oldSounds.Count != act.SoundFiles.Count) {
				_newSounds = new List<string>(act.SoundFiles);
			}
			else {
				bool changed = false;

				for (int i = 0; i < _oldSounds.Count; i++) {
					if (_oldSounds[i] != act.SoundFiles[i]) {
						changed = true;
						break;
					}
				}

				if (changed)
					_newSounds = new List<string>(act.SoundFiles);
			}

			if (_newSounds == null)
				_oldSounds = null;


			// Current actions
			for (int i = 0; i < Math.Min(_oldImages.Count, act.Sprite.Images.Count); i++) {
				_changedImages[i] = _oldImages[i].Equals(act.Sprite.Images[i]) ? (ActEditTypes.NoChange, null, null) : (ActEditTypes.Modified, _oldImages[i].Clone(), act.Sprite.Images[i].Clone());
			}

			// Added actions
			for (int i = _oldImages.Count; i < act.Sprite.Images.Count; i++) {
				_changedImages[i] = (ActEditTypes.Added, null, act.Sprite.Images[i].Clone());
			}

			// Deleted actions
			for (int i = act.Sprite.Images.Count; i < _oldImages.Count; i++) {
				_changedImages[i] = (ActEditTypes.Deleted, _oldImages[i].Clone(), null);
			}

			byte[] newPalette = act.Sprite.Palette?.BytePalette;

			if (_oldPalette == null && newPalette == null)
				_changedPalette = (ActEditTypes.NoChange, null, null);
			else if (_oldPalette == null && newPalette != null)
				_changedPalette = (ActEditTypes.Added, null, (byte[])newPalette.Clone());
			else if (_oldPalette != null && newPalette == null)
				_changedPalette = (ActEditTypes.Deleted, (byte[])_oldPalette.Clone(), null);
			else if (Methods.ByteArrayCompare(_oldPalette, newPalette))
				_changedPalette = (ActEditTypes.NoChange, null, null);
			else
				_changedPalette = (ActEditTypes.Modified, (byte[])_oldPalette.Clone(), (byte[])newPalette.Clone());

			_oldPalette = null;
			_hasBeenCleaned = true;
		}

		private void _applyChanges(Act act, bool execute) {
			foreach (var entry in _changedActions.OrderBy(p => p.Key)) {
				switch (entry.Value.EditType) {
					case ActEditTypes.NoChange:
						break;
					case ActEditTypes.Modified:
						act.Actions[entry.Key] = execute ? entry.Value.New.Clone() : entry.Value.Old.Clone();
						break;
					case ActEditTypes.Deleted:
						if (execute)
							act.Actions.RemoveAt(act.Actions.Count - 1);
						else
							act.Actions.Add(entry.Value.Old.Clone());
						break;
					case ActEditTypes.Added:
						if (execute)
							act.Actions.Add(entry.Value.New.Clone());
						else
							act.Actions.RemoveAt(act.Actions.Count - 1);
						break;
				}
			}

			if (_oldSounds != null) {
				act.SoundFiles.Clear();

				if (execute)
					act.SoundFiles.AddRange(_newSounds);
				else
					act.SoundFiles.AddRange(_oldSounds);
			}

			foreach (var entry in _changedImages.OrderBy(p => p.Key)) {
				switch (entry.Value.EditType) {
					case ActEditTypes.NoChange:
						break;
					case ActEditTypes.Modified:
						act.Sprite.Images[entry.Key] = execute ? entry.Value.New.Clone() : entry.Value.Old.Clone();
						break;
					case ActEditTypes.Deleted:
						if (execute)
							act.Sprite.Images.RemoveAt(act.Sprite.Images.Count - 1);
						else
							act.Sprite.Images.Add(entry.Value.Old.Clone());
						break;
					case ActEditTypes.Added:
						if (execute)
							act.Sprite.Images.Add(entry.Value.New.Clone());
						else
							act.Sprite.Images.RemoveAt(act.Sprite.Images.Count - 1);
						break;
				}
			}

			act.Sprite.ReloadCount();

			switch (_changedPalette.EditType) {
				case ActEditTypes.NoChange:
					break;
				case ActEditTypes.Modified:
					act.Sprite.Palette.SetPalette(execute ? _changedPalette.New : _changedPalette.Old);
					break;
				case ActEditTypes.Deleted:
					if (execute) {
						act.Sprite.Palette = null;
					}
					else {
						act.Sprite.Palette = new PalFormat.Pal();
						act.Sprite.Palette.SetPalette(_changedPalette.Old);
					}
					break;
				case ActEditTypes.Added:
					if (execute) {
						act.Sprite.Palette = new PalFormat.Pal();
						act.Sprite.Palette.SetPalette(_changedPalette.New);
					}
					else {
						act.Sprite.Palette = null;
					}
					break;
			}
		}
	}
}
