using System.Collections.Generic;
using System.Linq;
using GRF.ContainerFormat;
using GRF.ContainerFormat.Commands;
using GRF.Hash;
using Utilities;
using Utilities.Commands;
using Utilities.Extension;
using Utilities.Hash;

namespace GRF.Core {
	/// <summary>
	/// This class is used as part of the testing library [not public].
	/// </summary>
	public static class Snapshot {
		private static IHash _hash = new Crc32EHash();

		public static IHash Hash {
			get { return _hash; }
			set { _hash = value; }
		}
	}

	/// <summary>
	/// This class is used as part of the testing library [not public].
	/// </summary>
	/// <typeparam name="TEntry">The type of the entry.</typeparam>
	public sealed class ContainerSnapshot<TEntry> where TEntry : ContainerEntry {
		private Dictionary<string, byte[]> _hashes = new Dictionary<string, byte[]>();
		private Dictionary<string, FileEntry> _table = new Dictionary<string, FileEntry>();

		public ContainerSnapshot(ContainerAbstract<TEntry> container) {
			CommandHelper = container.Commands;
			Container = container;
			Commands = new List<IContainerCommand<TEntry>>(CommandHelper.Commands);

			foreach (var entry in container.Table) {
				_table[entry.RelativePath] = new FileEntry((FileEntry) (object) entry);
				_table[entry.RelativePath].Stream = null;
			}

			//foreach (var entry in container.Table) {
			//    _hashes[entry.RelativePath] = Hash.ComputeByteHash(entry.GetDecompressedData());
			//}
			_hashes = new FolderHash().HashContainer((ContainerAbstract<FileEntry>) (object) container, Snapshot.Hash);
		}

		public Dictionary<string, byte[]> Hashes {
			get { return _hashes; }
			set { _hashes = value; }
		}

		public List<IContainerCommand<TEntry>> Commands { get; set; }

		public AbstractCommand<IContainerCommand<TEntry>> CommandHelper { get; private set; }

		public ContainerAbstract<TEntry> Container { get; private set; }

		public Dictionary<string, FileEntry> Table {
			get { return _table; }
			set { _table = value; }
		}

		public static ContainerSnapshot<TEntry> Get(GrfHolder container) {
			return new ContainerSnapshot<TEntry>((ContainerAbstract<TEntry>) (object) container.Container);
		}

		public static ContainerSnapshot<TEntry> Get(ContainerAbstract<TEntry> container) {
			return new ContainerSnapshot<TEntry>(container);
		}

		public bool Equals(ContainerSnapshot<TEntry> snapshot, out List<string> errors) {
			HashSet<string> source = _hashes.Keys.ToList().ToHashSet();
			HashSet<string> dest = snapshot._hashes.Keys.ToHashSet();
			errors = new List<string>();

			foreach (var s in source) {
				if (!dest.Remove(s)) {
					errors.Add("File not found : " + s);
					return false;
				}
			}

			if (dest.Count > 0) {
				errors.Add("Too many files left : \r\n" + Methods.Aggregate(dest.ToList(), "\r\n"));
				return false;
			}


			foreach (var pair in _hashes) {
				if (!Methods.ByteArrayCompare(snapshot.Hashes[pair.Key], pair.Value)) {
					errors.Add("File different : " + pair.Key);
					return false;
				}
			}

			return true;
		}
	}
}