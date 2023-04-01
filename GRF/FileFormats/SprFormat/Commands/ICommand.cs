using GRF.FileFormats.SprFormat.Builder;

namespace GRF.FileFormats.SprFormat.Commands {
	public interface ICommand {
		void Execute(SprBuilderInterface sbi);
		void Undo(SprBuilderInterface sbi);
	}
}