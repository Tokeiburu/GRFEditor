using GRF.ContainerFormat;

namespace GRF.IO {
	public struct EntrySearchNode<TEntry> where TEntry : ContainerEntry {
		public TEntry Entry;
		public string FileName;

		public EntrySearchNode(TEntry entry, string fileName) {
			Entry = entry;
			FileName = fileName;
		}
	}
}