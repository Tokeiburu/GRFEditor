using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using GRF.Core;
using GRF.GrfSystem;
using GRF.Threading;
using Utilities.Services;

internal static class ProtectedGrfTests {
	private sealed class Vector {
		internal string Tag;
		internal byte[] Name, Keys, Payload, Output;
		internal int Size;
	}

	private static int Main() {
		string temporary = Path.Combine(Path.GetTempPath(), "protected-grf-tests-" + Guid.NewGuid().ToString("N"));
		try {
			Directory.CreateDirectory(temporary);
			Settings.TempPath = Path.Combine(temporary, "work");
			Settings.CpuMonitoringEnabled = false;
			EncodingService.DisplayEncoding = Encoding.GetEncoding(949);
			Compression.CompressionAlgorithm = Compression.ZlibDotNet;
			List<Vector> vectors = ReadVectors();
			CheckDecoder(vectors);
			Console.WriteLine("PASS decoder vectors: 25 accepted, 15 rejected");
			CheckArchive(vectors.Single(v => v.Tag == "zlib-rc6-footer"), temporary);
			Console.WriteLine("PASS archive integration: reads, extraction, callbacks, saves and rename safety");
			return 0;
		}
		catch (Exception error) {
			Console.Error.WriteLine(error);
			return 1;
		}
		finally {
			if (Directory.Exists(temporary)) Directory.Delete(temporary, true);
		}
	}

	private static List<Vector> ReadVectors() {
		using (var reader = new BinaryReader(File.OpenRead(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fixtures", "protected-grf.bin")))) {
			int count = reader.ReadInt32();
			var vectors = new List<Vector>();
			for (int i = 0; i < count; i++) {
				vectors.Add(new Vector {
					Tag = Encoding.UTF8.GetString(ReadBytes(reader)),
					Name = ReadBytes(reader), Keys = ReadBytes(reader),
					Payload = ReadBytes(reader), Output = ReadBytes(reader), Size = reader.ReadInt32()
				});
			}
			Assert(count == 40 && reader.BaseStream.Position == reader.BaseStream.Length, "Fixture bundle is incomplete.");
			return vectors;
		}
	}

	private static byte[] ReadBytes(BinaryReader reader) {
		return reader.ReadBytes(reader.ReadInt32());
	}

	private static void CheckDecoder(List<Vector> vectors) {
		Type decoder = typeof(GrfHolder).Assembly.GetType("GRF.Core.GrfCompression.ProtectedGrfDecoder", true);
		MethodInfo keysMethod = decoder.GetMethod("DecodeArchiveKeys", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo decodeMethod = decoder.GetMethod("Decode", BindingFlags.Static | BindingFlags.NonPublic);
		int accepted = 0, rejected = 0;
		foreach (Vector vector in vectors) {
			bool positive = vector.Tag.StartsWith("boundary-", StringComparison.Ordinal) ||
				new[] { "raw-name", "zlib-rc6-footer", "lz4-aes-checksum", "threeway-partial", "body1-lz4" }.Contains(vector.Tag);
			try {
				object keys = keysMethod.Invoke(null, new object[] { vector.Keys });
				byte[] decoded = vector.Payload.Length == 0 ? new byte[0] :
					(byte[])decodeMethod.Invoke(null, new object[] { vector.Payload, vector.Name, vector.Size, keys });
				Assert(positive, "Malformed vector was accepted: " + vector.Tag);
				Equal(vector.Output, decoded, vector.Tag);
				accepted++;
			}
			catch (TargetInvocationException error) {
				if (positive || !(error.InnerException is InvalidDataException))
					throw new Exception("Unexpected decoder failure: " + vector.Tag, error.InnerException);
				rejected++;
			}
		}
		Assert(accepted == 25 && rejected == 15, "Fixture classification changed.");
	}

	private static void CheckArchive(Vector vector, string temporary) {
		byte[] ordinary = Encoding.ASCII.GetBytes("ordinary GRF contents");
		string archive = Path.Combine(temporary, "source.grf");
		WriteArchive(archive, vector, ordinary, true);
		string protectedPath = Encoding.GetEncoding(949).GetString(vector.Name).Replace('/', '\\');
		using (var grf = new GrfHolder(archive)) {
			FileEntry entry = grf.FileTable[protectedPath];
			Equal(vector.Output, entry.GetDecompressedData(), "single protected read");
			Equal(ordinary, grf.FileTable["data\\ordinary.txt"].GetDecompressedData(), "ordinary V3 read");

			string extraction = Path.Combine(temporary, "extract");
			grf.Extract(extraction, "", SearchOption.AllDirectories, null, null,
				ExtractOptions.Normal | ExtractOptions.OverrideCpuPerf, SyncMode.Synchronous);
			Equal(vector.Output, File.ReadAllBytes(Path.Combine(extraction, protectedPath)), "bulk protected extraction");
			Equal(ordinary, File.ReadAllBytes(Path.Combine(extraction, "data", "ordinary.txt")), "bulk ordinary extraction");

			var callbacks = new Dictionary<string, byte[]>();
			var worker = new ThreadGenericGrf();
			List<FileEntry> entries = grf.FileTable.Entries.ToList();
			worker.Init(grf, entries, 0, entries.Count);
			worker.Init((file, bytes) => callbacks.Add(file.RelativePath, bytes), () => false);
			worker.Start();
			Assert(SpinWait.SpinUntil(() => worker.Terminated, TimeSpan.FromSeconds(30)), "Generic callback worker did not finish.");
			Assert(!worker.Error, "Generic callback worker failed: " + worker.Exception);
			Equal(vector.Output, callbacks[protectedPath], "generic protected callback");
			Equal(ordinary, callbacks["data\\ordinary.txt"], "generic ordinary callback");

			string copy = Path.Combine(temporary, "copy.grf");
			AssertSaved(grf.SaveAs(copy), "SaveAs");
			CheckSavedArchive(copy, vector, ordinary, protectedPath);
			string repacked = Path.Combine(temporary, "repacked.grf");
			AssertSaved(grf.RepackAs(repacked), "RepackAs");
			CheckSavedArchive(repacked, vector, ordinary, protectedPath);
			Equal(vector.Output, grf.FileTable[protectedPath].GetDecompressedData(), "protected preview immediately after repack");

			grf.Commands.Rename(protectedPath, "data\\renamed.txt");
			string renamed = Path.Combine(temporary, "rename-must-not-exist.grf");
			var result = grf.SaveAs(renamed);
			Assert(!result.Success && result.Error != null && !File.Exists(renamed), "Renamed protected save was not rejected before creating output.");
		}

		string ordinaryV2 = Path.Combine(temporary, "ordinary-v2.grf");
		WriteArchive(ordinaryV2, vector, ordinary, false);
		using (var grf = new GrfHolder(ordinaryV2))
			Equal(ordinary, grf.FileTable["data\\ordinary.txt"].GetDecompressedData(), "ordinary V2 read");
	}

	private static void WriteArchive(string path, Vector vector, byte[] ordinary, bool version3) {
		byte[] compressed = Compression.CompressZlibDotNet(ordinary);
		using (var stream = File.Create(path))
		using (var writer = new BinaryWriter(stream)) {
			writer.Write(Encoding.ASCII.GetBytes("Master of Magic\0"));
			writer.Write(Enumerable.Range(1, 14).Select(i => (byte)i).ToArray());
			int protectedAlignment = ((vector.Payload.Length + 7) / 8) * 8 + 16;
			long tableOffset = (version3 ? protectedAlignment : 0) + compressed.Length;
			if (version3) { writer.Write(tableOffset); writer.Write(2); }
			else { writer.Write((uint)tableOffset); writer.Write(0); writer.Write(8); }
			writer.Write(version3 ? 0x300 : 0x200);
			if (version3) { writer.Write(vector.Payload); writer.Write(new byte[protectedAlignment - vector.Payload.Length]); }
			writer.Write(compressed);
			byte[] metadata;
			using (var table = new MemoryStream())
			using (var record = new BinaryWriter(table)) {
				if (version3) WriteRecord(record, vector.Name, vector.Payload.Length, vector.Size, 0x80, 0, true, protectedAlignment);
				WriteRecord(record, Encoding.ASCII.GetBytes("data/ordinary.txt"), compressed.Length, ordinary.Length,
					1, version3 ? protectedAlignment : 0, version3);
				metadata = table.ToArray();
			}
			byte[] packedTable = Compression.CompressZlibDotNet(metadata);
			if (version3) writer.Write(vector.Keys.Length);
			writer.Write(packedTable.Length); writer.Write(metadata.Length); writer.Write(packedTable);
			if (version3) writer.Write(vector.Keys);
		}
	}

	private static void WriteRecord(BinaryWriter writer, byte[] name, int packed, int output, byte flags, long offset, bool version3, int alignment = 0) {
		writer.Write(name); writer.Write((byte)0);
		writer.Write(packed); writer.Write(alignment == 0 ? packed : alignment); writer.Write(output); writer.Write(flags);
		if (version3) writer.Write(offset); else writer.Write((uint)offset);
	}

	private static void CheckSavedArchive(string path, Vector vector, byte[] ordinary, string protectedPath) {
		using (var reader = new BinaryReader(File.OpenRead(path))) {
			reader.BaseStream.Position = 30;
			long tableOffset = reader.ReadInt64();
			reader.BaseStream.Position = 46 + tableOffset;
			int footerLength = reader.ReadInt32(), packedSize = reader.ReadInt32();
			reader.ReadInt32();
			byte[] table = Compression.DecompressZlibDotNet(reader.ReadBytes(packedSize));
			Equal(vector.Keys, reader.ReadBytes(footerLength), "saved footer bytes");
			Assert(reader.BaseStream.Position == reader.BaseStream.Length, "Unexpected saved archive trailing bytes.");
			bool found = false;
			using (var records = new BinaryReader(new MemoryStream(table))) {
				while (records.BaseStream.Position < records.BaseStream.Length) {
					var rawName = new List<byte>();
					byte value;
					while ((value = records.ReadByte()) != 0) rawName.Add(value);
					records.ReadInt32(); records.ReadInt32(); records.ReadInt32();
					byte flag = records.ReadByte(); records.ReadInt64();
					if ((flag & 0x80) != 0) { Equal(vector.Name, rawName.ToArray(), "saved protected filename bytes"); found = true; }
				}
			}
			Assert(found, "Saved archive lost its protected entry.");
		}
		using (var grf = new GrfHolder(path)) {
			Equal(vector.Output, grf.FileTable[protectedPath].GetDecompressedData(), "saved protected contents");
			Equal(ordinary, grf.FileTable["data\\ordinary.txt"].GetDecompressedData(), "saved ordinary contents");
		}
	}

	private static void AssertSaved(GRF.ContainerFormat.ContainerSaveResult result, string operation) {
		Assert(result.Success, operation + " failed: " + result.Error);
	}

	private static void Equal(byte[] expected, byte[] actual, string context) {
		Assert(expected.SequenceEqual(actual), "Byte mismatch: " + context);
	}

	private static void Assert(bool condition, string message) {
		if (!condition) throw new Exception(message);
	}
}
