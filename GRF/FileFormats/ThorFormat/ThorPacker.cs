using System;
using System.IO;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.FileFormats.ThorFormat {
	/// <summary>
	/// Class used to pack data in an executable.
	/// </summary>
	internal class ThorPacker {
		public ThorPacker() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ThorPacker" /> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="option"> </param>
		/// <param name="elyOffset"> </param>
		public ThorPacker(byte[] data, bool option, int elyOffset) {
			if (option) {
				ByteReader reader = new ByteReader(data, elyOffset);

				PackedOffset = elyOffset + 6;
				NonPackedData = reader.Bytes(6);

				reader.Position = elyOffset + 10;

				int tableLength = reader.Int32();
				int tableOffset = reader.Int32() + elyOffset;

				reader.Position = tableOffset;
				CompressedTable = reader.Bytes(tableLength);

				reader.Position = elyOffset + 18;
				CompressedData = reader.Bytes(tableOffset - reader.Position);

				reader.Position = PackedOffset;
				NumberOfFiles = reader.Int32();
			}
			else {
				ByteReader reader = new ByteReader(data, data.Length - 4);

				PackedOffset = data.Length - reader.Int32() - 4;

				reader.Position = PackedOffset + 8;

				int tableOffset = reader.Int32();

				reader.Position = tableOffset;
				CompressedTable = reader.Bytes(reader.Length - tableOffset - 4);

				reader.Position = PackedOffset + 12;
				CompressedData = reader.Bytes(tableOffset - reader.Position);

				reader.Position = PackedOffset;
				NumberOfFiles = reader.Int32();

				reader.Position = 0;
				NonPackedData = reader.Bytes(PackedOffset);
			}
		}

		/// <summary>
		/// Gets or sets the packed offset.
		/// </summary>
		public int PackedOffset { get; set; }

		/// <summary>
		/// Gets or sets the non packed data.
		/// </summary>
		public byte[] NonPackedData { get; set; }

		/// <summary>
		/// Gets or sets the compressed table data.
		/// </summary>
		public byte[] CompressedTable { get; set; }

		/// <summary>
		/// Gets or sets the compressed data.
		/// </summary>
		public byte[] CompressedData { get; set; }

		/// <summary>
		/// Gets or sets the number of files.
		/// </summary>
		public int NumberOfFiles { get; set; }

		public void Write(string path) {
			if (path.IsExtension(".conf")) {
				int tableOffset = CompressedData.Length + 18;
				byte[] newData = new byte[tableOffset + CompressedTable.Length];

				Buffer.BlockCopy(new byte[] { 0x45, 0x4c, 0x59, 0x00, 0x01, 0x00 }, 0, newData, 0, 6);
				Buffer.BlockCopy(CompressedData, 0, newData, 0 + 18, CompressedData.Length);
				Buffer.BlockCopy(CompressedTable, 0, newData, tableOffset, CompressedTable.Length);
				Buffer.BlockCopy(BitConverter.GetBytes(NumberOfFiles), 0, newData, 6, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(CompressedTable.Length), 0, newData, 10, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(tableOffset), 0, newData, 14, 4);

				File.WriteAllBytes(path, newData);
			}
			else {
				int tableOffset = PackedOffset + CompressedData.Length + 12;
				byte[] newData = new byte[tableOffset + CompressedTable.Length + 4];

				Buffer.BlockCopy(NonPackedData, 0, newData, 0, PackedOffset);
				Buffer.BlockCopy(CompressedData, 0, newData, PackedOffset + 12, CompressedData.Length);
				Buffer.BlockCopy(CompressedTable, 0, newData, tableOffset, CompressedTable.Length);
				Buffer.BlockCopy(BitConverter.GetBytes(NumberOfFiles), 0, newData, PackedOffset + 0, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(CompressedTable.Length), 0, newData, PackedOffset + 4, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(tableOffset), 0, newData, PackedOffset + 8, 4);
				Buffer.BlockCopy(BitConverter.GetBytes(newData.Length - PackedOffset - 4), 0, newData, newData.Length - 4, 4);

				File.WriteAllBytes(path, newData);
			}
		}

		/// <summary>
		/// Updates the table data.
		/// </summary>
		/// <typeparam name="T">Entry type.</typeparam>
		/// <param name="table">The table.</param>
		public void UpdateTableData<T>(ContainerTable<T> table) where T : ContainerEntry {
			NumberOfFiles = table.Entries.Count;

			byte[] tableData;

			using (MemoryStream tableDataStream = new MemoryStream()) {
				var offset = (uint) 0;
				foreach (var entry in table) {
					var uncompressedData = entry.GetDecompressedData(); //
					var compressedData = Compression.CompressDotNet(uncompressedData);
					//var compressedData = entry.GetCompressedData();

					entry.SizeDecompressed = uncompressedData.Length;

					tableDataStream.Write(compressedData, 0, compressedData.Length);
					entry.TemporaryOffset = offset;
					entry.TemporarySizeCompressedAlignment = compressedData.Length;
					offset += (uint) compressedData.Length;
				}

				tableDataStream.Seek(0, SeekOrigin.Begin);
				tableData = new byte[tableDataStream.Length];
				tableDataStream.Read(tableData, 0, tableData.Length);

				CompressedData = tableData;
			}

			RepackTable(table);
		}

		/// <summary>
		/// Repacks the table.
		/// </summary>
		/// <typeparam name="T">Entry type.</typeparam>
		/// <param name="table">The table.</param>
		public void RepackTable<T>(ContainerTable<T> table) where T : ContainerEntry {
			T entry;
			byte[] file;

			using (MemoryStream tableStream = new MemoryStream()) {
				for (int i = 0; i < table.Entries.Count; i++) {
					entry = table.Entries[i];

					var tFile = entry.RelativePath.ReplaceFirst(GrfStrings.RgzRoot, "");

					if (!tFile.StartsWith("Languages")) {
						tFile = tFile.Replace("\\", "/");
					}
					else {
						tFile = tFile.Replace("/", "\\");
					}

					file = EncodingService.Ansi.GetBytes(EncodingService.GetAnsiString(tFile));

					tableStream.WriteByte((byte) file.Length);
					tableStream.Write(file, 0, file.Length);

					tableStream.Write(BitConverter.GetBytes(entry.TemporarySizeCompressedAlignment == 0 ? entry.SizeCompressed : entry.TemporarySizeCompressedAlignment), 0, 4);
					tableStream.Write(BitConverter.GetBytes(entry.SizeDecompressed), 0, 4);
					tableStream.Write(BitConverter.GetBytes(entry.TemporaryOffset + 12), 0, 4);
				}

				tableStream.Seek(0, SeekOrigin.Begin);
				byte[] tableData = new byte[tableStream.Length];
				tableStream.Read(tableData, 0, tableData.Length);

				CompressedTable = Compression.Compress(tableData);
			}
		}
	}
}