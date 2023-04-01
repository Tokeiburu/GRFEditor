using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using GRF.Image;

namespace GRF.FileFormats.DbFormat {
	public struct CatalogHeader {
		public short HeaderSize;
		public short Reserved;
		public int ThumbCount;
		public int ThumbHeight;
		public int ThumbWidth;
	}

	public struct CatalogItem {
		public string Filename;
		public int HeaderSize;
		public int ItemId;
		public DateTime ModifiedTime;
		public short Reserved;
	}


	/// <summary>
	/// Summary description for ThumbDB.
	/// </summary>
	public class ThumbDB {
		private readonly ArrayList _catalogItems = new ArrayList();
		private readonly string _thumbDBFile;

		public ThumbDB(string thumbDBFile) {
			_thumbDBFile = thumbDBFile;
			_loadCatalog();
		}

		public string[] GetThumbfiles() {
			var files = new string[_catalogItems.Count];
			int index = 0;
			foreach (CatalogItem item in _catalogItems) {
				files[index] = item.Filename;
				index++;
			}
			return files;
		}

		public byte[] GetThumbDataFromId(string filename) {
			return null;
		}

		public byte[] GetThumbData(string filename) {
			var wrapper = new StorageWrapper(_thumbDBFile, false);
			foreach (CatalogItem catItem in _catalogItems) {
				if (catItem.Filename == filename) {
					string streamName = _buildReverseString(catItem.ItemId);
					FileObject fileObject = wrapper.OpenUcomStream(null, streamName);
					var rawJpgData = new byte[fileObject.Length];
					fileObject.Read(rawJpgData, 0, (int) fileObject.Length);
					fileObject.Close();

					// 3 ints of header data need to be removed
					// Don't know what first int is.
					// 2nd int is thumb index
					// 3rd is size of thumbnail data.
					int headerSize = BitConverter.ToInt32(rawJpgData, 0);
					int sizeOfThumbnail = BitConverter.ToInt32(rawJpgData, 8);
					var jpgData = new byte[sizeOfThumbnail];
					Buffer.BlockCopy(rawJpgData, headerSize, jpgData, 0, sizeOfThumbnail);

					if (jpgData.Length > 0 && jpgData[0] != 0xff) {
						throw new Exception("Jpeg information missing.");
						//int frameSize = BitConverter.ToInt32(jpgData, 4);

						//byte[] tmp = new byte[frameSize];
						//Buffer.BlockCopy(jpgData, 16, tmp, 0, frameSize);
						//jpgData = tmp;
					}

					return jpgData;
				}
			}
			return null;
		}

		public GrfImage GetThumbnailImage(string filename) {
			byte[] thumbData = GetThumbData(filename);
			if (null == thumbData) {
				return null;
			}
			return new GrfImage(ref thumbData, -1, -1, GrfImageType.NotEvaluatedJpg);
		}

		private void _loadCatalog() {
			var wrapper = new StorageWrapper(_thumbDBFile, false);
			FileObject fileObject = wrapper.OpenUcomStream(null, "Catalog");
			if (fileObject != null) {
				var fileData = new byte[fileObject.Length];
				fileObject.Read(fileData, 0, (int) fileObject.Length);
				var ms = new MemoryStream(fileData);
				var br = new BinaryReader(ms);
				var ch = new CatalogHeader();
				ch.HeaderSize = br.ReadInt16();
				ch.Reserved = br.ReadInt16();
				ch.ThumbCount = br.ReadInt32();

				if (ch.HeaderSize > 8) {
					ch.ThumbWidth = br.ReadInt32();
				}
				if (ch.HeaderSize > 12) {
					ch.ThumbHeight = br.ReadInt32();
				}

				for (int index = 0; index < ch.ThumbCount; index++) {
					var item = new CatalogItem();
					item.HeaderSize = br.ReadInt32();
					item.ItemId = br.ReadInt32();
					long low = (uint) br.ReadInt32();
					int high = br.ReadInt32();
					long highTime = (long) high << 32;
					item.ModifiedTime = DateTime.FromFileTime(highTime | low);
					ushort usChar;
					while ((usChar = br.ReadUInt16()) != 0x0000) {
						var aChar = new byte[2];
						aChar[0] = (byte) (usChar & 0x00FF);
						aChar[1] = (byte) ((usChar & 0xFF00) >> 8);
						item.Filename += Encoding.Unicode.GetString(aChar);
					}

					item.Reserved = br.ReadInt16();
					_catalogItems.Add(item);
				}
			}
		}

		private static string _buildReverseString(int itemId) {
			string itemString = itemId.ToString(CultureInfo.InvariantCulture);
			string reverse = "";
			for (int index = itemString.Length - 1; index >= 0; index--) {
				reverse += itemString[index];
			}
			return reverse;
		}
	}
}