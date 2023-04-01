using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GRF.ContainerFormat;
using GRF.Core;
using GRF.IO;
using Utilities;
using Utilities.Extension;
using Utilities.Hash;

namespace GRF.Hash {
	public class HashObject {
		public HashObject() {
			Header = new HashObjectHeader();
			Directories = new List<string>();
			Hashes = new Dictionary<TkPath, byte[]>();
		}

		public HashObject(byte[] data)
			: this(new ByteReader(data)) {
		}

		public HashObject(string file) : this(new ByteReaderStream(file)) {
		}

		public HashObject(IBinaryReader data) {
			Directories = new List<string>();
			Header = new HashObjectHeader(data);

			if (Header.IsCompatibleWith(1, 0)) {
				ExploreMethod = (HashExploreMethod)data.Byte();
				IsPartial = data.Byte() != 0;

				byte hashMethod = data.Byte();

				switch (hashMethod) {
					case 0: HashMethod = new Crc32Hash(); break;
					case 1: HashMethod = new Md5Hash(); break;
					case 2: HashMethod = new FileSizeHash(); break;
					case 3: HashMethod = new QuickHash(new Md5Hash()); break;
					default: throw GrfExceptions.__UnknownHashAlgorithm.Create();
				}

				ByteReader reader = new ByteReader(Compression.DecompressDotNet(data.Bytes(data.Length - data.Position)));

				int length = reader.Int32();

				HeadDirectory = reader.StringUnicode(length);
				length = reader.Int32();
				Directories = Methods.StringToList(reader.StringUnicode(length), ':');

				_expandDirectory();

				NumberOfFilesHashed = reader.Int32();

				List<int> references = new List<int>(NumberOfFilesHashed);

				for (int i = 0; i < NumberOfFilesHashed; i++) {
					references.Add(reader.Int32());
				}

				length = reader.Int32();

				List<string> fileNames = Methods.StringToList(reader.StringUnicode(length), ':');

				for (int i = 0; i < fileNames.Count; i++) {
					fileNames[i] = Path.Combine(Directories[references[i]], fileNames[i]);
				}

				int hashLength = HashMethod.HashLength;

				Hashes = new Dictionary<TkPath, byte[]>();

				for (int i = 0; i < NumberOfFilesHashed; i++) {
					byte[] hashData = reader.Bytes(hashLength);
					Hashes[new TkPath(fileNames[i])] = hashData;
				}
			}
			else {
				throw GrfExceptions.__UnsupportedFileVersion.Create();
			}
		}

		// The paths are saved with the unicode encoding
		public Dictionary<TkPath, byte[]> Hashes { get; set; }
		public HashObjectHeader Header { get; private set; }

		public HashExploreMethod ExploreMethod { get; set; }
		public int NumberOfFilesHashed { get; private set; }
		public bool IsPartial { get; set; }
		public IHash HashMethod { get; set; }
		public string HeadDirectory { get; set; }

		public List<string> Directories { get; set; }

		private void _expandDirectory() {
			for (int i = Directories.Count - 1; i > 0; i--) {
				string currentItem = Directories[i];

				if (currentItem.StartsWith("|")) {
					int index = _getIndex(currentItem);
					string mIndx = "|" + index + "_";

					Directories = Directories.Select(p => p.Replace(mIndx, Directories[index])).ToList();
				}
			}
		}

		private int _getIndex(string currentItem) {
			int offsetEnd = currentItem.IndexOf('_', 1);

			return Int32.Parse(currentItem.Substring(1, offsetEnd - 1));
		}

		public void Save(string fileName) {
			using (Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write)) {
				Save(stream);
			}
		}

		public void Save(Stream stream) {
			Header.Write(stream);
			stream.WriteByte((byte)ExploreMethod);
			stream.WriteByte((byte)(IsPartial ? 1 : 0));

			byte hashMethod;

			if (HashMethod is Crc32Hash) hashMethod = 0;
			else if (HashMethod is Md5Hash) hashMethod = 1;
			else if (HashMethod is FileSizeHash) hashMethod = 2;
			else if (HashMethod is QuickHash) hashMethod = 3;
			else throw GrfExceptions.__UnknownHashAlgorithm.Create();

			stream.WriteByte(hashMethod);

			var hashList = Hashes.Select(p => new Tuple<TkPath, byte[]>(p.Key, p.Value)).OrderBy(p => p.Item1.GetFullPath()).ToList();

			List<int> referenced = _compressDirectories(hashList);

			using (Stream table = new MemoryStream()) {
				byte[] toWrite = Encoding.Unicode.GetBytes(HeadDirectory);
				_writeInt32(table, toWrite.Length);
				table.Write(toWrite, 0, toWrite.Length);

				toWrite = Encoding.Unicode.GetBytes(Methods.ListToString(Directories, ':'));
				_writeInt32(table, toWrite.Length);
				table.Write(toWrite, 0, toWrite.Length);

				_writeInt32(table, Hashes.Count);

				for (int i = 0; i < Hashes.Count; i++) {
					_writeInt32(table, referenced[i]);
				}

				toWrite = Encoding.Unicode.GetBytes(Methods.ListToString(hashList.Select(p => _getFileName(p.Item1)).ToList(), ':'));
				_writeInt32(table, toWrite.Length);
				table.Write(toWrite, 0, toWrite.Length);

				byte[] hashTable = new byte[Hashes.Count * HashMethod.HashLength];

				for (int i = 0; i < Hashes.Count; i++) {
					Buffer.BlockCopy(hashList[i].Item2, 0, hashTable, HashMethod.HashLength * i, HashMethod.HashLength);
				}

				table.Write(hashTable, 0, hashTable.Length);

				table.Seek(0, SeekOrigin.Begin);
				byte[] compressed = Compression.Compress(table);
				stream.Write(compressed, 0, compressed.Length);
			}
		}

		private void _writeInt32(Stream table, int length) {
			table.Write(BitConverter.GetBytes(length), 0, 4);
		}

		private List<int> _compressDirectories(IList<Tuple<TkPath, byte[]>> hashList) {
			Directories = hashList.Select(p => _getHeadPath(p.Item1)).Distinct().ToList();
			Directories = _explode(Directories);

			if (Directories.Count > int.MaxValue) {
				throw new Exception("Too many directories found.");
			}

			List<int> referenced = new List<int>();

			for (int i = 0; i < Hashes.Count; i++) {
				referenced.Add(Directories.IndexOf(_getHeadPath(hashList[i].Item1)));
			}

			for (int i = 0; i < Directories.Count - 1; i++) {
				string currentItem = Directories[i];

				if (currentItem == "")
					continue;

				Directories = Directories.Take(i + 1).Concat(Directories.Skip(i + 1).Select(p => _recur(p, currentItem, i))).ToList();//.ReplaceFirst(currentItem, "|" + i))).ToList();
			}

			return referenced;
		}

		private List<string> _explode(List<string> directories) {
			List<string> toReturn = new List<string>();

			for (int i = 0; i < directories.Count; i++) {
				string dir = directories[i];
				toReturn.Add(dir);

				if (String.IsNullOrEmpty(dir))
					continue;

				//string par = dir;

				while (!String.IsNullOrEmpty(dir = Path.GetDirectoryName(dir))) {
					if (!toReturn.Contains(dir))
						toReturn.Add(dir);
				}
			}

			return toReturn.Distinct().OrderBy(p => p).ToList();
		}

		private string _recur(string dir, string currentItem, int i) {
			if (dir.StartsWith(currentItem)) {
				return dir.ReplaceFirst(currentItem, "|" + i + "_");
			}
			return dir;
		}

		private string _getHeadPath(TkPath path) {
			if (path.RelativePath == null)
				return Path.GetDirectoryName(path.FilePath);
			return path.FilePath + "?" + Path.GetDirectoryName(path.RelativePath);
		}

		private string _getFileName(TkPath path) {
			if (path.RelativePath == null)
				return Path.GetFileName(path.FilePath);
			return Path.GetFileName(path.RelativePath);
		}
	}
}
