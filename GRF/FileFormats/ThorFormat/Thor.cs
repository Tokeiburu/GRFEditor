using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.Core.GrfCompression;
using GRF.IO;
using GRF.Threading;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;
using Utilities.Services;

namespace GRF.FileFormats.ThorFormat {
	/// <summary>
	/// Thor container
	/// </summary>
	public sealed class Thor : ContainerAbstract<ThorEntry> {
		/// <summary>
		/// Initializes a new instance of the <see cref="Thor" /> class.
		/// </summary>
		/// <param name="stream">The stream.</param>
		public Thor(FileStream stream)
			: base(new ByteReaderStream(stream)) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Thor" /> class.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		public Thor(string fileName)
			: base(new ByteReaderStream(fileName)) {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Thor" /> class.
		/// </summary>
		private Thor() {
			Header = new ThorHeader();
		}

		/// <summary>
		/// Gets the container's header.
		/// </summary>
		public new ThorHeader Header { get; set; }

		/// <summary>
		/// Initializes this instance.
		/// </summary>
		protected override void _init() {
			Header = new ThorHeader(_reader);

			_reader.Position = Header.FileTableOffset;

			switch(Header.Mode) {
				case 0x30:
					byte[] tableDecompressed = Compression.DecompressZlibDotNet(_reader.Bytes(Header.FileTableCompressedLength));

					ByteReader bReader = new ByteReader(tableDecompressed);

					while (bReader.CanRead) {
						ThorEntry entry = new ThorEntry(bReader, _reader);
						Table.AddEntry(entry);
					}

					break;
				case 0x21:
					Table.AddEntry(new ThorEntry(_reader, _reader, true));
					break;
				default:
					throw GrfExceptions.__FileFormatException2.Create("THOR", "Unknown mode: " + Header.Mode + ".");
			}
		}

		private Container _parse(Container container) {
			if (container == null) return null;
			List<FileEntry> entries = new List<FileEntry>(container.Table.Entries);
			container.InternalTable.Clear();
			entries.ForEach(p => p.RelativePath = Path.Combine(RgzFormat.Rgz.Root, p.RelativePath));
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
			Container parsed = _parse(new Container(_toGrfContainer(".thor", false, grfName)));
			parsed.FileName = oldFileName;

			if (Validation.IsValid) {
				parsed.InternalHeader.ThorSettings.TargetGrf = Header.TargetGrf;
				parsed.InternalHeader.ThorSettings.UseGrfMerging = Header.UseGrfMerging;
			}

			parsed.Validation.Add(Validation);

			return parsed;
		}

		public GrfHolder ToGrfHolderQuick() {
			GrfHolder grf = new GrfHolder();
			grf.New();

			foreach (ThorEntry entry in Table) {
				var d = entry.ToFileEntry(grf.Header);
				d.Stream = entry.Stream;
				grf.FileTable.AddEntry(d);
			}

			grf.Container.Reader = _reader;
			return grf;
		}

		/// <summary>
		/// Generates a Thor file from a GRF, it uses the attached properties to read the custom information.
		/// </summary>
		/// <param name="grf">The GRF.</param>
		/// <param name="fileName">Name of the output file.</param>
		/// <param name="result">...</param>
		public static void SaveFromGrf(GrfHolder grf, string fileName, ContainerSaveResult result) {
			SaveFromGrf(grf.Container, fileName, result);
		}

		private static void _validatePatchingMode(Container grf, Thor thor, bool repack) {
			var files = grf.InternalTable.Files.Where(p => !p.Contains(GrfStrings.GrfIntegrityFile));

			// Look for encrypted files
			bool isEncrypted = false;

			foreach (var entry in grf.InternalTable.Entries) {
				if ((entry.Modification & Modification.Encrypt) == Modification.Encrypt) {
					isEncrypted = true;
					break;
				}

				if (entry.SourceFilePath == null) {
					byte[] data = entry.GetCompressedData();

					if (data.Length > 1 && (data[0] != 0x00 && data[0] != 0x78)) {
						isEncrypted = true;
						break;
					}
				}
			}

			if (files.Any(p => !p.StartsWith("root\\data") && !p.StartsWith("data\\")) && thor.Header.UseGrfMerging) {
				if (ErrorHandler.YesNoRequest("You chose the 'Merge into GRF' patching mode but some files are not within the data folder.\r\n\r\n" +
											  "Do you wish to change the patching mode back to 'Merge into RO directory'? (Press 'No' to ignore this warning).", "Suspicious patching mode")) {
					thor.Header.UseGrfMerging = false;

					if (!Compression.IsNormalCompression) {
						Compression.CompressionAlgorithm = new CpsCompression();
					}
				}
			}

			if (!thor.Header.UseGrfMerging && isEncrypted) {
				if (ErrorHandler.YesNoRequest("You chose the 'Merge into RO directory' patching mode but some files are encrypted.\r\n\r\n" +
											  "Do you wish to change the patching mode back to 'Merge into GRF'? (Press 'No' to ignore this warning).", "Suspicious patching mode")) {
					thor.Header.UseGrfMerging = true;
				}
			}

			if (!repack && !thor.Header.UseGrfMerging) {
				if (!Compression.IsNormalCompression && 
					ErrorHandler.YesNoRequest("You are trying to compress a patch file using a custom compression method. The files identified are not going to be merged into a GRF and their extraction will most likely fail.\r\n\r\nDo you wish to change the compression back to zlib? (Press 'No' to ignore this warning).", "Suspicious compression")) {
					Compression.CompressionAlgorithm = new CpsCompression();
				}
			}
		}

		/// <summary>
		/// Generates a Thor file from a GRF, it uses the attached properties to read the custom information.
		/// </summary>
		/// <param name="grf">The GRF.</param>
		/// <param name="fileName">Name of the output file.</param>
		/// <param name="result">...</param>
		internal static void SaveFromGrf(Container grf, string fileName, ContainerSaveResult result) {
			Thor thor = new Thor();
			var thorSettings = grf.InternalHeader.ThorSettings;
			bool repack = thorSettings.Repack;
			ICompression oldCompression = Compression.CompressionAlgorithm;

			try {
				thor.Header.UseGrfMerging = thorSettings.UseGrfMerging;

				_validatePatchingMode(grf, thor, repack);
				
				if (repack)
					_repackGrfData(grf, result);

				using (ByteWriterStream stream = new ByteWriterStream(new FileStream(fileName, FileMode.Create, FileAccess.Write))) {
					thor.Header.TargetGrf = thorSettings.TargetGrf;
					thor.Header.Write(stream, grf);

					switch (thor.Header.Mode) {
						case 0x21:
							_writeClientExecutablePatch(grf, stream);
							break;
						case 0x30:
							_writePatch(grf, stream, thor);
							break;
					}
				}

				// Prevents being warned about changes to the GRF; the file will be reloaded
				grf.Commands.ClearCommands();
			}
			finally {
				Compression.CompressionAlgorithm = oldCompression;
			}

			//result.RequiresReload = true;
		}

		private static void _writePatch(Container grf, ByteWriterStream stream, Thor thor) {
			var redirection = new Dictionary<string, FileEntry>();
			var hashes = new TkDictionary<string, string>();
			ThorIntegrity integrity = new ThorIntegrity();

			int oldCommandIndex = grf.Commands.CommandIndex;

			try {
				grf.IsBusy = false;

				if (grf.Table.ContainsFile(RgzFormat.Rgz.Root + GrfStrings.GrfIntegrityFile))
					grf.Commands.RemoveFile(RgzFormat.Rgz.Root + GrfStrings.GrfIntegrityFile);

				grf.IsBusy = true;

				for (int i = 0; i < grf.Table.Entries.Count; i++) {
					AProgress.IsCancelling(grf);
					WritePatchEntry(grf, stream, grf.Table.Entries[i], integrity, redirection);
					grf.Progress = AProgress.LimitProgress((i + 1f) / grf.Table.Count * 100f);
				}

				if (integrity.AddIntegrityFile(grf)) {
					WritePatchEntry(grf, stream, grf.Table[GrfStrings.GrfIntegrityFile], integrity, redirection);
				}

				_writePatchFileTable(thor, grf, stream);
				thor.Header.Write(stream, grf);
			}
			catch {
				int count = grf.Commands.CommandIndex - oldCommandIndex;

				while (grf.Commands.CommandIndex > oldCommandIndex && grf.Commands.CanUndo) {
					grf.Commands.Undo();
				}

				grf.Commands.RemoveCommands(count);
				throw;
			}
		}

		internal static void WritePatchEntry(Container grf, ByteWriterStream stream, FileEntry entry, ThorIntegrity integrity, Dictionary<string, FileEntry> redirection) {
			if ((entry.Flags & EntryType.RemoveFile) == EntryType.RemoveFile)
				return;

			byte[] compressed;

			if (entry.RelativePath == GrfStrings.GrfIntegrityFile && !Compression.IsNormalCompression) {
				var old = Compression.CompressionAlgorithm;
				Compression.CompressionAlgorithm = new CpsCompression();
				compressed = entry.GetCompressedData();
				Compression.CompressionAlgorithm = old;
			}
			else {
				compressed = entry.GetCompressedData();
			}

			if (entry.SourceFilePath == null && entry.SizeCompressed != compressed.Length) {
				// The file is aligned
				byte[] temp = new byte[entry.SizeCompressed];
				Buffer.BlockCopy(compressed, 0, temp, 0, entry.SizeCompressed);
				compressed = temp;
			}

			int decompressedLength;

			if (entry.Added) {
				if (entry.SourceFilePath == GrfStrings.DataStreamId) {
					decompressedLength = entry.RawDataSource.Length;
					entry.SizeCompressedAlignment = Methods.Align(compressed.Length);
				}
				else {
					decompressedLength = (int)new FileInfo(entry.SourceFilePath ?? "").Length;
					entry.SizeCompressedAlignment = Methods.Align(compressed.Length);
				}
			}
			else {
				if (entry.NewSizeDecompressed <= 0) {
					entry.BypassSaveCheck = true;
					decompressedLength = entry.GetDecompressedData().Length;
					entry.BypassSaveCheck = false;
				}
				else {
					decompressedLength = entry.NewSizeDecompressed;
				}
			}

			entry.SizeDecompressed = decompressedLength;
			entry.NewSizeDecompressed = decompressedLength;

			if (compressed.Length > 1 && (entry.Modification & Modification.Encrypt) == Modification.Encrypt) {
				if (entry.GrfEditorEncrypt(compressed, 0)) {
					if (compressed[0] == 0x78) {
						entry.Modification &= ~Modification.Encrypt;
						entry.Modification |= Modification.Decrypt;
						entry.GrfEditorDecrypt(compressed, 0);
						entry.Modification &= ~Modification.Decrypt;
					}
				}
			}
			else {
				entry.GrfEditorEncrypt(compressed, 0);
			}

			entry.GrfEditorDecrypt(compressed, 0);

			entry.Offset = stream.PositionUInt;
			entry.NewSizeCompressed = compressed.Length;
			entry.NewSizeDecompressed = decompressedLength;

			integrity.AddHash(entry, compressed);

			Crc32Hash hashRedirection = new Crc32Hash();
			string hashKey = hashRedirection.ComputeHash(compressed);
			FileEntry redirectedEntry;

			if (redirection.TryGetValue(hashKey, out redirectedEntry)) {
				entry.Offset = redirectedEntry.Offset;
				entry.SizeDecompressed = redirectedEntry.SizeDecompressed;
				entry.NewSizeCompressed = redirectedEntry.NewSizeCompressed;
				entry.NewSizeDecompressed = redirectedEntry.NewSizeDecompressed;
			}
			else {
				redirection[hashKey] = entry;
				stream.Write(compressed);
			}

			// Fix: 2016-06-10
			// Add alignment space, this is required to preserve the integrity.
			if (entry.SizeCompressedAlignment - compressed.Length > 0) {
				stream.Write(new byte[entry.SizeCompressedAlignment - compressed.Length]);
			}
		}

		private static void _writePatchFileTable(Thor thor, Container grf, ByteWriterStream stream) {
			thor.Header.FileTableOffset = stream.Position;
			FileEntry entry;
			byte[] file;

			using (MemoryStream mStream = new MemoryStream()) {
				for (int i = 0; i < grf.Table.Entries.Count; i++) {
					entry = grf.Table.Entries[i];

					file = EncodingService.Ansi.GetBytes(EncodingService.GetAnsiString(entry.GetFixedFileName()));

					mStream.WriteByte((byte)file.Length);
					mStream.Write(file, 0, file.Length);

					if ((entry.Flags & EntryType.RemoveFile) == EntryType.RemoveFile) {
						mStream.WriteByte(0x01);
						continue;
					}

					mStream.WriteByte(0x00);
					mStream.Write(BitConverter.GetBytes(entry.Offset), 0, 4);
					mStream.Write(BitConverter.GetBytes(entry.NewSizeCompressed), 0, 4);
					mStream.Write(BitConverter.GetBytes(entry.NewSizeDecompressed), 0, 4);
				}

				mStream.Seek(0, SeekOrigin.Begin);
				byte[] dataCompressed = Compression.CompressZlibDotNet(mStream);
				thor.Header.FileTableCompressedLength = dataCompressed.Length;
				stream.Write(dataCompressed);
			}
		}

		private static void _writeClientExecutablePatch(Container grf, ByteWriterStream stream) {
			var entry = grf.Table.Entries[0];
			var compressed = entry.GetCompressedData();

			if (entry.SourceFilePath == null) {
				// The file is aligned
				byte[] temp = new byte[entry.SizeCompressed];
				Buffer.BlockCopy(compressed, 0, temp, 0, entry.SizeCompressed);
				compressed = temp;
			}

			entry.BypassSaveCheck = true;
			var decompressed = entry.GetDecompressedData();
			entry.BypassSaveCheck = false;

			var file = EncodingService.Ansi.GetBytes(EncodingService.GetAnsiString(entry.GetFixedFileName()));

			stream.Write(compressed.Length);
			stream.Write(decompressed.Length);
			stream.Write((byte)file.Length);
			stream.Write(file);
			stream.Write(compressed);
			stream.WriteAnsi("SEALED!");
		}

		private static void _repackGrfData(Container grf, ContainerSaveResult result) {
			try {
				grf.IsBusy = false;
				grf.LimitProgress(true);
				grf.Save(null, null, SavingMode.RepackSource, SyncMode.Synchronous);
			}
			finally {
				grf.LimitProgress(false);
				grf.IsBusy = true;
			}
		}

		#region Thor Packer Structure
		public void PackLoad(string fileName) {
			Table = new ContainerTable<ThorEntry>();
			_reader = new ByteReaderStream(fileName);

			// Check if the file contains the ELY header
			byte[] data = File.ReadAllBytes(fileName);

			bool isNewConf = fileName.IsExtension(".conf");
			int packedOffset = 0;

			if (!isNewConf) {
				var found = data.LastIndexOf("ELY");

				if (found > -1) {
					isNewConf = true;
					packedOffset = found;
				}
			}

			ThorPacker tmp = new ThorPacker(data, isNewConf, packedOffset);

			byte[] tableDecompressed = Compression.DecompressZlibDotNet(tmp.CompressedTable);

			ByteReader bReader = new ByteReader(tableDecompressed);

			while (bReader.CanRead) {
				ThorEntry entry = new ThorEntry();
				entry.RelativePath = bReader.String(bReader.Byte());
				entry.SizeCompressed = bReader.Int32();
				entry.SizeDecompressed = bReader.Int32();
				entry.Offset = (uint)(bReader.UInt32() + tmp.PackedOffset);
				entry.Stream = _reader;

				Table.AddEntry(entry);
			}
		}

		public static Thor PackerLoad(string fileName) {
			var thor = new Thor();
			thor.PackLoad(fileName);
			return thor;
		}

		#endregion
	}
}