using System;
using System.Collections.Generic;
using System.IO;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using GRF.System;
using GRF.Threading;

namespace GRF.FileFormats.RgzFormat {
	public class Rgz : ContainerAbstract<RgzEntry> {
		public const string Root = "root\\";

		private ByteReaderStream _dataReader;
		private string _decompressedFileName;

		public Rgz(string fileName) : this(new ByteReaderStream(fileName)) {
		}

		internal Rgz(ByteReaderStream reader) : base(reader) {
		}

		protected override void _init() {
			try {
				AProgress.Init(this);

				_decompressedFileName = TemporaryFilesManager.GetTemporaryFilePath("rgz_decomp_{0:0000}.dat");

				Compression.GZipDecompress(this, _reader, _decompressedFileName);
				_dataReader = new ByteReaderStream(_decompressedFileName);

				while (_dataReader.CanRead) {
					Progress = 50f + _dataReader.Position / (float) _dataReader.Length * 50f;
					AProgress.IsCancelling(this);

					char entryType = _dataReader.Char();

					switch (entryType) {
						case 'f':
							Table.AddEntry(new RgzEntry(_dataReader));
							break;
						case 'd':
							_dataReader.Forward(_dataReader.Byte());
							break;
						case 'e':
							break;
					}
				}

				State = ContainerState.Normal;
			}
			catch (OperationCanceledException) {
				State = ContainerState.LoadCancelled;
			}
			catch (Exception err) {
				State = ContainerState.Error;
				ErrorHandler.HandleException(err);
			}
			finally {
				AProgress.Finalize(this);
			}
		}

		protected override void _onDispose() {
			if (_dataReader != null) {
				_dataReader.Close();
				_dataReader = null;
			}

			if (_decompressedFileName != null) {
				GrfPath.Delete(_decompressedFileName);
				_decompressedFileName = null;
			}
		}

		private Container _parse(Container container) {
			List<FileEntry> entries = new List<FileEntry>(container.Table.Entries);
			container.InternalTable.Clear();
			entries.ForEach(p => p.RelativePath = Path.Combine(Root, p.RelativePath));
			entries.ForEach(p => container.Table.AddEntry(p));
			return container;
		}

		/// <summary>
		/// Converts the current container to a GRF container.
		/// </summary>
		/// <param name="grfName">Name of the GRF.</param>
		/// <returns>The converted container to a GRF container.</returns>
		internal override Container ToGrfContainer(string grfName = null) {
			string oldFileName = _reader.Stream.Name;
			Container parsed = _parse(new Container(_toGrfContainer(".thor", true, grfName)));
			parsed.FileName = oldFileName;
			return parsed;
		}
	}
}