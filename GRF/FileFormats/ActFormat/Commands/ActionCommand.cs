using System.Collections.Generic;
using Utilities.Extension;

namespace GRF.FileFormats.ActFormat.Commands {
	public class ActionCommand : IActCommand {
		#region ActionEdit enum

		public enum ActionEdit {
			RemoveAt,
			InsertAt,
			ReplaceTo,
			CopyAt,
			Switch,
			Move,
		}

		#endregion

		private readonly int _actionIndex;
		private readonly int _actionIndexTo;
		private readonly ActionEdit _edit;
		private readonly int _range = 1;
		private Action _conflict;
		private List<Action> _conflicts;

		public ActionCommand(int actionIndex, ActionEdit edit) {
			_actionIndex = actionIndex;
			_actionIndexTo = actionIndex;
			_edit = edit;
		}

		public ActionCommand(int actionIndex, int actionIndexTo, ActionEdit edit) {
			_actionIndex = actionIndex;
			_actionIndexTo = actionIndexTo;
			_edit = edit;
		}

		public ActionCommand(int actionIndex, int range, int actionIndexTo, ActionEdit edit) {
			_actionIndex = actionIndex;
			_range = range;
			_actionIndexTo = actionIndexTo;
			_edit = edit;
		}

		public bool Executed { get; set; }

		public int ActionIndexTo {
			get { return _actionIndexTo; }
		}

		public ActionEdit Edit {
			get { return _edit; }
		}

		#region IActCommand Members

		public void Execute(Act act) {
			Executed = true;

			switch (_edit) {
				case ActionEdit.RemoveAt:
					if (_conflict == null)
						_conflict = act[_actionIndex];

					act.Actions.RemoveAt(_actionIndex);
					break;
				case ActionEdit.CopyAt:
					act.Actions.Insert(_actionIndexTo, new Action(act[_actionIndex]));
					break;
				case ActionEdit.ReplaceTo:
					if (_conflict == null)
						_conflict = act[_actionIndexTo];

					act.Actions[_actionIndexTo] = new Action(act[_actionIndex]);
					break;
				case ActionEdit.InsertAt:
					act.Actions.Insert(_actionIndexTo, new Action { Frames = new List<Frame> { new Frame() } });
					break;
				case ActionEdit.Move:
					act.Actions.Move(_actionIndex, _range, _actionIndexTo);
					break;
				case ActionEdit.Switch:
					if (_conflicts == null)
						_conflicts = new List<Action>(act.Actions);

					act.Actions.Switch(_actionIndex, _range, _actionIndexTo, 1);
					break;
			}
		}

		public void Undo(Act act) {
			Executed = false;

			switch (_edit) {
				case ActionEdit.RemoveAt:
					act.Actions.Insert(_actionIndex, _conflict);
					break;
				case ActionEdit.CopyAt:
				case ActionEdit.InsertAt:
					act.Actions.RemoveAt(_actionIndexTo);
					break;
				case ActionEdit.ReplaceTo:
					act.Actions[_actionIndexTo] = _conflict;
					break;
				case ActionEdit.Move:
					act.Actions.ReverseMove(_actionIndex, _range, _actionIndexTo);
					break;
				case ActionEdit.Switch:
					act.Actions.Clear();
					act.Actions.AddRange(_conflicts);
					break;
			}
		}

		public string CommandDescription {
			get {
				switch (_edit) {
					case ActionEdit.RemoveAt:
						return CommandsHolder.GetId(_actionIndex) + " Remove";
					case ActionEdit.CopyAt:
						return CommandsHolder.GetId(_actionIndex) + " Copy and insert to " + CommandsHolder.GetId(_actionIndexTo);
					case ActionEdit.InsertAt:
						return CommandsHolder.GetId(_actionIndex) + " Insert new";
					case ActionEdit.ReplaceTo:
						return CommandsHolder.GetId(_actionIndex) + " Copy and replace to " + CommandsHolder.GetId(_actionIndexTo);
					case ActionEdit.Switch:
						return CommandsHolder.GetId(_actionIndex) + " Switch with " + CommandsHolder.GetId(_actionIndexTo);
					case ActionEdit.Move:
						return CommandsHolder.GetId(_actionIndex) + " Moved to " + CommandsHolder.GetId(_actionIndexTo);
				}

				return "Unknown";
			}
		}

		#endregion
	}
}