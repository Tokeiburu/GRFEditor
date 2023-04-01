namespace GRF.FileFormats.PalFormat {
	public interface IPaletteCommand {
		string CommandDescription { get; }
		void Execute(Pal palette);
		void Undo(Pal palette);
	}
}