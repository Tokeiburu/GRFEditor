using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ErrorManager;
using GRF.Core;
using GRF.IO;
using GRF.GrfSystem;
using GRF.Threading;
using Utilities;

namespace GRF.FileFormats.TkPatchFormat {
	public class TkPatch : IDisposable, IProgress {
		private readonly string _fileName;
		private readonly ByteReaderStream _reader;

		public TkPatch(FileStream stream) : this(new ByteReaderStream(stream)) {
			_fileName = stream.Name;
		}

		public TkPatch(string fileName) : this(new ByteReaderStream(File.OpenRead(fileName))) {
			_fileName = fileName;
		}

		public TkPatch() {
			Header = new TkPatchHeader();
			Table = new TkPatchTable();
		}

		internal TkPatch(ByteReaderStream reader) {
			_reader = reader;
			Header = new TkPatchHeader(reader);
			Table = new TkPatchTable(this, reader);
		}

		public TkPatchHeader Header { get; set; }
		public TkPatchTable Table { get; set; }

		public byte[] this[TkPath name] {
			get { return Table[name].GetDecompressedData(); }
		}

		public float LimitedProgress {
			set { Progress = value >= 100f ? 99.99f : value; }
		}

		#region IDisposable Members

		public void Dispose() {
			if (_reader != null) {
				_reader.Close();
			}

			if (Table != null) {
				Table.Clear();
			}
		}

		#endregion

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		public void Save() {
			if (_fileName == null)
				throw new Exception("You must specify a filename since this is a new patch object.");

			string temp = TemporaryFilesManager.GetTemporaryFilePath("tkPatch_{0:0000}.tkp");
			if (Save(temp)) {
				File.Delete(_fileName);
				File.Move(temp, _fileName);
			}
			else {
				throw new Exception("Failed to save the patch object.");
			}
		}

		public bool Save(string filename) {
			try {
				Progress = -1f;
				IsCancelling = false;
				IsCancelled = false;

				using (FileStream stream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite)) {
					stream.SetLength(TkPatchHeader.StructSize);
					stream.Seek(TkPatchHeader.StructSize, SeekOrigin.Begin);

					uint offset = (int) TkPatchHeader.StructSize;

					for (int index = 0; index < Table.Entries.Count; index++) {
						TkPatchEntry entry = Table.Entries[index];
						if (IsCancelling) {
							throw new OperationCanceledException();
						}

						byte[] compressed = entry.GetCompressedData();

						if (compressed != null) {
							entry.Offset = offset;
							entry.SizeCompressed = compressed.Length;

							offset += (uint) compressed.Length;
							stream.Write(compressed, 0, compressed.Length);
						}

						LimitedProgress = (index + 1f) / Table.Entries.Count * 100f;
					}

					Header.FileTableOffset = (int) stream.Position;

					using (MemoryStream mem = new MemoryStream()) {
						foreach (TkPatchEntry entry in Table) {
							if (IsCancelling) {
								throw new OperationCanceledException();
							}

							entry.Write(mem);
						}

						mem.Flush();
						mem.Seek(0, SeekOrigin.Begin);

						byte[] data = new byte[mem.Length];
						mem.Read(data, 0, (int) mem.Length);
						data = Compression.CompressDotNet(data);
						stream.Write(data, 0, data.Length);
					}

					stream.Seek(0, SeekOrigin.Begin);
					Header.Write(stream);
				}

				return true;
			}
			catch (OperationCanceledException) {
				return false;
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
				return false;
			}
			finally {
				Progress = 100f;
				IsCancelling = false;
				IsCancelled = false;
			}
		}

		public void AddRemove(TkPath path) {
			Table[path] = new TkPatchEntry { Flags = EntryType.RemoveFile, TkPath = path };
		}

		public void Add(TkPath path, string filepath) {
			Table[path] = new TkPatchEntry(filepath) { TkPath = path };
		}

		public void Add(TkPath path, byte[] compressedData, int sizeDecompressed) {
			Table[path] = new TkPatchEntry(compressedData, sizeDecompressed) { TkPath = path };
		}

		public GrfHolder ToGrfHolder() {
			GrfHolder grf = new GrfHolder();
			grf.New();

			foreach (TkPatchEntry entry in Table) {
				grf.FileTable.AddEntry(entry.ToFileEntry(grf.Header));
			}

			grf.Container.Reader = _reader;
			return grf;
		}

		public GrfHolder ToGrfHolder(List<TkPath> paths) {
			GrfHolder grf = new GrfHolder();
			grf.New();

			foreach (TkPath entry in paths) {
				grf.FileTable.AddEntry(Table[entry].ToFileEntry(grf.Header));
			}

			grf.Container.Reader = _reader;
			return grf;
		}

		public static void Apply(TkPatch patch) {
			try {
				patch.Progress = -1;
				patch.IsCancelled = false;
				patch.IsCancelling = false;

				List<TkPath> paths = patch.Table.Entries.Select(p => p.TkPath).ToList();
				Dictionary<string, List<TkPath>> dico = new Dictionary<string, List<TkPath>>();
				List<TkPath> direct = paths.Where(p => p.RelativePath == null).ToList();

				int processed = 0;
				int total = paths.Count;

				for (int i = 0; i < direct.Count; i++) {
					if (patch.IsCancelling) {
						throw new OperationCanceledException();
					}

					patch.Table[direct[i]].Extract(Directory.GetCurrentDirectory());
					processed++;
					patch.LimitedProgress = (processed) / (float) total * 100f;
				}

				for (int i = 0; i < direct.Count; i++) {
					paths.Remove(direct[i]);
				}

				foreach (TkPath path in paths) {
					if (dico.ContainsKey(path.FilePath)) {
						dico[path.FilePath].Add(path);
					}
					else {
						if (!Directory.Exists(path.FilePath))
							dico[path.FilePath] = new List<TkPath> { path };
					}
				}

				foreach (KeyValuePair<string, List<TkPath>> pairs in dico) {
					using (GrfHolder grf = new GrfHolder(pairs.Key, GrfLoadOptions.OpenOrNew)) {
						if (patch.IsCancelling) {
							throw new OperationCanceledException();
						}

						grf.QuickMerge(patch.ToGrfHolder(pairs.Value), SyncMode.Asynchronous);

						while (!grf.IsOpened || grf.IsBusy || grf.Progress < 100f) {
							if (patch.IsCancelling) {
								throw new OperationCanceledException();
							}

							Thread.Sleep(200);
							patch.LimitedProgress = (processed) / (float) total * 100f + grf.Progress * (pairs.Value.Count / (float) total);
						}

						processed += pairs.Value.Count;
						patch.LimitedProgress = (processed) / (float) total * 100f;
					}
				}
			}
			catch (OperationCanceledException) {
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				patch.Progress = 100f;
				patch.IsCancelling = false;
				patch.IsCancelled = false;
			}
		}
	}
}