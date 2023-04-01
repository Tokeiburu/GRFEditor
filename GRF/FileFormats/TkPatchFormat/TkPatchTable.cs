using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GRF.Core;
using GRF.IO;
using Utilities;

namespace GRF.FileFormats.TkPatchFormat {
	public class TkPatchTable : IEnumerable<TkPatchEntry> {
		private readonly Dictionary<TkPath, TkPatchEntry> _indexedEntries = new Dictionary<TkPath, TkPatchEntry>();
		private List<TkPatchEntry> _entries;
		private bool _hasBeenModified;

		public TkPatchTable() {
		}

		public TkPatchTable(TkPatch tkPatch, ByteReaderStream reader) {
			reader.Position = tkPatch.Header.FileTableOffset;

			byte[] tableDecompressed = Compression.DecompressDotNet(reader.Bytes(reader.Length - reader.Position));

			ByteReader bReader = new ByteReader(tableDecompressed);

			while (bReader.CanRead) {
				TkPatchEntry entry = new TkPatchEntry(bReader, reader);
				_indexedEntries[entry.TkPath] = entry;
			}
		}

		internal bool HasBeenModified {
			set {
				if (value && _hasBeenModified == false) {
					_entries = null;
				}

				_hasBeenModified = value;
			}
		}

		public List<TkPatchEntry> Entries {
			get {
				if (_entries == null) {
					_entries = _indexedEntries.Values.ToList();
					_hasBeenModified = false;
				}

				return _entries;
			}
		}

		public TkPatchEntry this[TkPath path] {
			get { return _indexedEntries[path]; }
			set {
				_indexedEntries[path] = value;
				HasBeenModified = true;
			}
		}

		#region IEnumerable<TkPatchEntry> Members

		public IEnumerator<TkPatchEntry> GetEnumerator() {
			return _indexedEntries.Values.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		public override string ToString() {
			return "Thor table; Number of entries = " + _indexedEntries.Count;
		}

		public void Clear() {
			_indexedEntries.Clear();

			if (_entries != null) {
				_entries.Clear();
				_entries = null;
			}
		}
	}
}