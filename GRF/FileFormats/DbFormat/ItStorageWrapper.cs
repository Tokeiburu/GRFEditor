using System;
using System.Runtime.InteropServices.ComTypes;

namespace GRF.FileFormats.DbFormat {
	public class ItStorageWrapper : BaseStorageWrapper {
		private readonly Interop.UCOMITStorage _comItStorage;
		private readonly Interop.ITStorage _comItStorageInterfaced;

		public ItStorageWrapper(string workPath, bool enumStorage) {
			_comItStorage = new Interop.UCOMITStorage();
			_comItStorageInterfaced = (Interop.ITStorage) _comItStorage;
			storage = _comItStorageInterfaced.StgOpenStorage(workPath, IntPtr.Zero, 32, IntPtr.Zero, 0);
			BaseUrl = workPath;
			if (enumStorage) {
				base.EnumIStorageObject(storage);
			}
		}

		public override void EnumIStorageObject(Interop.IStorage stgEnum) {
			base.EnumIStorageObject(storage);
		}

		public Interop.IStorage OpenSubStorage(Interop.IStorage parentStorage, string storageName) {
			if (parentStorage == null)
				parentStorage = storage;

			Interop.IStorage retObject;

			STATSTG sTatstg;
			sTatstg.pwcsName = storageName;
			sTatstg.type = 1;

			try {
				Interop.IStorage iStorage = parentStorage.OpenStorage(sTatstg.pwcsName, IntPtr.Zero, 16, IntPtr.Zero, 0);

				retObject = iStorage;
			}
			catch (Exception) {
				retObject = null;
			}

			return retObject;
		}

		public FileObject OpenUcomStream(Interop.IStorage parentStorage, string fileName) {
			if (parentStorage == null)
				parentStorage = storage;

			FileObject retObject;

			STATSTG sTatstg;
			sTatstg.pwcsName = fileName;
			sTatstg.type = 2;

			try {
				retObject = new FileObject();

				IStream uComiStream = parentStorage.OpenStream(sTatstg.pwcsName, IntPtr.Zero, 16, 0);

				if (uComiStream != null) {
					retObject.FileType = sTatstg.type;
					retObject.FilePath = "";
					retObject.FileName = sTatstg.pwcsName;
					retObject.FileStream = uComiStream;
				}
				else {
					retObject = null;
				}
			}
			catch (Exception) {
				retObject = null;
			}

			return retObject;
		}
	}
}