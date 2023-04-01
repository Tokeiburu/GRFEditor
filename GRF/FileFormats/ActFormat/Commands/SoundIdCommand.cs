namespace GRF.FileFormats.ActFormat.Commands {
	public class SoundIdCommand : IActCommand {
		#region SoundIdEdit enum

		public enum SoundIdEdit {
			InsertAt,
			RemoveAt,
			SetFrameSound,
		}

		#endregion

		private readonly int _actionIndex;
		private readonly SoundIdEdit _edit;
		private readonly int _frameIndex;
		private readonly int _soundId;
		private CopyStructureAct _copy;
		private string _name;
		private int _oldSoundId;

		public SoundIdCommand(int actionIndex, int frameIndex, int soundId) {
			_actionIndex = actionIndex;
			_frameIndex = frameIndex;
			_soundId = soundId;
			_edit = SoundIdEdit.SetFrameSound;
		}

		public SoundIdCommand(string name, int soundId, SoundIdEdit edit) {
			_name = name;
			_soundId = soundId;
			_edit = edit;
			_edit = SoundIdEdit.InsertAt;
		}

		#region IActCommand Members

		public void Execute(Act act) {
			switch (_edit) {
				case SoundIdEdit.SetFrameSound:
					_oldSoundId = act[_actionIndex, _frameIndex].SoundId;
					act[_actionIndex, _frameIndex].SoundId = _soundId;
					break;
				case SoundIdEdit.InsertAt:
					act.SoundFiles.Insert(_soundId, _name);
					break;
				case SoundIdEdit.RemoveAt:
					if (_copy == null) {
						_copy = new CopyStructureAct(act, CopyStructureMode.SoundFiles);
						_name = act.SoundFiles[_soundId];
					}

					act.GetAllFrames().ForEach(p => {
						if (p.SoundId == _soundId)
							p.SoundId = -1;
						if (p.SoundId > _soundId) {
							p.SoundId--;
						}
					});

					act.SoundFiles.RemoveAt(_soundId);
					break;
			}
		}

		public void Undo(Act act) {
			switch (_edit) {
				case SoundIdEdit.SetFrameSound:
					act[_actionIndex, _frameIndex].SoundId = _oldSoundId;
					break;
				case SoundIdEdit.InsertAt:
					act.SoundFiles.RemoveAt(_soundId);
					break;
				case SoundIdEdit.RemoveAt:
					_copy.Apply(act);
					act.SoundFiles.Insert(_soundId, _name);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_edit) {
					case SoundIdEdit.SetFrameSound:
						return CommandsHolder.GetId(_actionIndex, _frameIndex) + " Sound ID changed " + _soundId;
					case SoundIdEdit.InsertAt:
						return "Sound ID added at " + _soundId + ", '" + _name + "'";
					case SoundIdEdit.RemoveAt:
						return "Sound ID removed at " + _soundId + ", '" + _name + "'";
				}

				return "Unknown";
			}
		}

		#endregion
	}
}