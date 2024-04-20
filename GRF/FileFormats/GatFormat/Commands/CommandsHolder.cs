using Utilities.Commands;

namespace GRF.FileFormats.GatFormat.Commands {
	public class CommandsHolder : AbstractCommand<IGatCommand> {
		private readonly Gat _gat;

		public CommandsHolder(Gat gat) {
			_gat = gat;
		}

		protected override void _execute(IGatCommand command) {
			command.Execute(_gat);
		}

		protected override void _undo(IGatCommand command) {
			command.Undo(_gat);
		}

		protected override void _redo(IGatCommand command) {
			command.Execute(_gat);
		}

		//public void SetCellType(int x, int y) { }

		public void Begin() {
			StoreAndExecute(new GroupCommand(_gat));
		}

		public void BeginNoDelay() {
			StoreAndExecute(new GroupCommand(_gat, true));
		}

		public void End() {
			EndEdit();
		}
	}

	public interface IGatCommand {
		string CommandDescription { get; }
		void Execute(Gat gat);
		void Undo(Gat gat);
	}
}