using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GRF.ContainerFormat;
using GRF.IO;
using GRF.GrfSystem;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRF.Core {
	public enum GrfBlockType {
		None,
		Header,
		Entry,
		FileTable,
		EntryMetaData,
	}

	public class GrfBlock {
		public byte[] Data { get; set; }
		public string DataPath { get; set; }
		private readonly TkDictionary<string, object> _properties = new TkDictionary<string, object>();

		public bool IsCompressed { get; set; }
		public GrfBlockType Type {get; set; }
		public TkDictionary<string, object> Properties {
			get { return _properties; }
		}

		public GrfBlock() {
		}

		public GrfBlock(byte[] data, GrfBlockType type) {
			Data = data;
			Type = type;
		}

		public void SetProperty(string key, object value) {
			Properties[key] = value;
		}

		public void Compress() {
			if (IsCompressed)
				return;

			Compress(TemporaryFilesManager.GetTemporaryFilePath("block_{0:000000}.gro"));
		}

		public void Compress(string path) {
			if (IsCompressed)
				return;

			var data = GetData();
			var compressed = Compression.Compress(data);
			File.WriteAllBytes(path, compressed);
			Properties["SizeCompressed"] = compressed.Length;
			DataPath = path;
			Data = null;
			IsCompressed = true;
		}

		public byte[] GetData() {
			if (Data != null)
				return Data;

			if (DataPath != null)
				return File.ReadAllBytes(DataPath);

			throw new Exception("Data not found in GrfBlock.");
		}

		public void Validate() {
			if (Type == GrfBlockType.Entry) {
				if (!IsCompressed)
					throw new Exception("File blocks must be compressed before creating the file table.");

				if (Properties["RelativePath"] == null)
					throw new Exception("The property 'RelativePath' must be set for the Entry block type (use GrfBlock.SetProperty).");

				if (Properties["SizeDecompressed"] == null)
					throw new Exception("The property 'SizeDecompressed' must be set for the Entry block type (use GrfBlock.SetProperty).");

				if (Properties["SizeCompressed"] == null)
					throw new Exception("The property 'SizeCompressed' must be set for the Entry block type (use GrfBlock.SetProperty).");

				if (Properties["EntryType"] == null)
					throw new Exception("The property 'EntryType' must be set for the Entry block type (use GrfBlock.SetProperty).");
			}
		}

		public void Save(string path) {
			File.WriteAllBytes(path, GetData());
		}

		public void Write(Stream stream) {
			var data = GetData();

			stream.Write(data, 0, data.Length);
		}

		public void Write(Stream stream, int size) {
			var data = GetData();

			stream.Write(data, 0, data.Length);
		}

		public int GetLength() {
			switch(Type) {
				case GrfBlockType.Header:
					return GrfHeader.DataByteSize;
				case GrfBlockType.FileTable:
					return Data.Length;
				case GrfBlockType.Entry:
					return Methods.Align((int)Properties["SizeCompressed"]);
			}

			return GetData().Length;
		}
	}

	public static class GrfBlockBuilder {
		public static int GetFileSize(string systemPath) {
			return (int)new FileInfo(systemPath).Length;
		}

		public static void CreateGrf(string file, GrfBlock header, List<GrfBlock> blocks, GrfBlock fileTable) {
			GrfPath.Delete(file);

			using (var stream = File.Create(file)) {
				Int64 totalSize = 0;

				totalSize += header.GetLength();
				totalSize += fileTable.GetLength();

				foreach (var block in blocks) {
					if (block.Type != GrfBlockType.Entry)
						throw new Exception("Expected Entry block type, received: " + block.Type);

					if (!block.IsCompressed) {
						block.Compress();
					}

					block.Validate();
					totalSize += block.GetLength();
				}

				if (totalSize > UInt32.MaxValue) {
					throw GrfExceptions.__GrfSizeLimitReached.Create();
				}

				stream.SetLength(totalSize);

				header.Write(stream);

				foreach (var block in blocks) {
					block.Write(stream);

					var align = Methods.Align((int)block.Properties["SizeCompressed"]) - (int)block.Properties["SizeCompressed"];

					if (align > 0) {
						stream.Seek(align, SeekOrigin.Current);
					}
				}

				fileTable.Write(stream);
			}
		}

		public static void CreateGrf(string file, List<GrfBlock> blocks) {
			Int64 totalSize = 0;

			var fileTable = CreateFileTable(blocks);

			foreach (var block in blocks) {
				totalSize += block.GetLength();
			}

			if (totalSize + fileTable.GetLength() > UInt32.MaxValue) {
				throw GrfExceptions.__GrfSizeLimitReached.Create();
			}

			CreateGrf(file, CreateGrfHeader((uint)totalSize, blocks.Count), blocks, fileTable);
		}

		public static GrfBlock CreateGrfHeader(uint fileTableOffset, int filesCount) {
			using (BinaryWriter writer = new BinaryWriter(new MemoryStream())) {
				writer.Write("Master of Magic\0".Bytes(16, Encoding.ASCII), 0, 16);
				var Key = Encoding.ASCII.GetString(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 });
				writer.Write(Key.Bytes(14, Encoding.ASCII), 0, 14);
				writer.Write(fileTableOffset);
				writer.Write(0);
				writer.Write(filesCount + 7 + 0);
				writer.Write((2 << 8) + 0);
				return new GrfBlock(((MemoryStream)writer.BaseStream).ToArray(), GrfBlockType.Header);
			}
		}

		public static GrfBlock CreateFileEntryUncompressed(string systemPath, string relativePath) {
			GrfBlock block = new GrfBlock();
			block.SetProperty("RelativePath", relativePath);
			block.SetProperty("SizeDecompressed", GetFileSize(systemPath));
			block.SetProperty("EntryType", (byte)EntryType.File);
			block.DataPath = systemPath;
			block.Type = GrfBlockType.Entry;
			block.IsCompressed = false;
			return block;
		}

		public static GrfBlock CreateFileEntryCompressed(string systemPath, string relativePath) {
			GrfBlock block = new GrfBlock();
			block.SetProperty("RelativePath", relativePath);
			block.SetProperty("SizeCompressed", GetFileSize(systemPath));
			block.SetProperty("EntryType", (byte)EntryType.File);
			block.DataPath = systemPath;
			block.Type = GrfBlockType.Entry;
			block.IsCompressed = true;
			return block;
		}

		public static GrfBlock CreateFileTable(List<GrfBlock> files) {
			foreach (var block in files) {
				if (block.Type != GrfBlockType.Entry)
					throw new Exception("Expected Entry block type, received: " + block.Type);

				if (!block.IsCompressed) {
					block.Compress();
				}

				block.Validate();
			}

			int offset = 0;

			using (MemoryStream tableStream = new MemoryStream()) {
				using (MemoryStream metaDataStream = new MemoryStream()) {
					foreach (var file in files) {
						byte[] fileName = EncodingService.Ansi.GetBytes(EncodingService.GetAnsiString((string)file.Properties["RelativePath"]));
						byte[] data = new byte[18 + fileName.Length];

						int sizeCompressedAligned = Methods.Align((int)file.Properties["SizeCompressed"]);
						Buffer.BlockCopy(fileName, 0, data, 0, fileName.Length);
						Buffer.BlockCopy(BitConverter.GetBytes((int)file.Properties["SizeCompressed"]), 0, data, fileName.Length + 1, 4);
						Buffer.BlockCopy(BitConverter.GetBytes(sizeCompressedAligned), 0, data, fileName.Length + 5, 4);
						Buffer.BlockCopy(BitConverter.GetBytes((int)file.Properties["SizeDecompressed"]), 0, data, fileName.Length + 9, 4);
						data[fileName.Length + 13] = (byte)file.Properties["EntryType"];
						Buffer.BlockCopy(BitConverter.GetBytes(offset), 0, data, fileName.Length + 14, 4);
						offset += sizeCompressedAligned;
						metaDataStream.Write(data, 0, data.Length);
					}

					metaDataStream.Seek(0, SeekOrigin.Begin);

					int tableSize = (int)metaDataStream.Length;
					var tableCompressed = Compression.CompressDotNet(metaDataStream);

					byte[] tableHeader = new byte[8];
					Buffer.BlockCopy(BitConverter.GetBytes(tableCompressed.Length), 0, tableHeader, 0, 4);
					Buffer.BlockCopy(BitConverter.GetBytes(tableSize), 0, tableHeader, 4, 4);

					tableStream.Write(tableHeader, 0, tableHeader.Length);
					tableStream.Write(tableCompressed, 0, tableCompressed.Length);

					GrfBlock fileTable = new GrfBlock(tableStream.ToArray(), GrfBlockType.FileTable);
					fileTable.SetProperty("TableSize", tableSize);
					fileTable.SetProperty("TableSizeCompressed", tableCompressed.Length);
					return fileTable;
				}
			}
		}
	}
}
