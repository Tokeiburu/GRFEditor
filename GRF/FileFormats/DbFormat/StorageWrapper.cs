using System;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace GRF.FileFormats.DbFormat {
	/// <summary>
	/// The class <c>IStorageWrapper</c> extends <c>IBaseStorageWrapper</c> and adds functionality for
	/// the interface IStorage.
	/// </summary>
	public class StorageWrapper : BaseStorageWrapper {
		/// <summary>
		/// Constructor of the class
		/// </summary>
		/// <param name="workPath">workpath of the storage</param>
		/// <param name="enumStorage">true if the storage should be enumerated automatically</param>
		public StorageWrapper(string workPath, bool enumStorage) {
			Interop.StgOpenStorage(workPath, null, 32, IntPtr.Zero, 0, out storage);
			BaseUrl = workPath;
			STATSTG sTatstg;
			storage.Stat(out sTatstg, 1);
			if (enumStorage) {
				base.EnumIStorageObject(storage);
			}
		}

		/// <summary>
		/// Enumerates an IStorage object and creates the file object collection
		/// </summary>
		/// <param name="stgEnum">IStorage to enumerate</param>
		public override void EnumIStorageObject(Interop.IStorage stgEnum) {
			base.EnumIStorageObject(storage);
		}

		/// <summary>
		/// Opens an UCOMIStream and returns the associated file object
		/// </summary>
		/// <param name="parentStorage">storage used to open the stream</param>
		/// <param name="fileName">filename of the stream</param>
		/// <returns>A <see cref="FileObject">FileObject</see> instance if the file was found, otherwise null.</returns>
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
			catch (Exception ex) {
				retObject = null;

				Debug.WriteLine("ITStorageWrapper.OpenUCOMStream() - Failed for file '" + fileName + "'");
				Debug.Indent();
				Debug.WriteLine("Exception: " + ex.Message);
				Debug.Unindent();
			}

			return retObject;
		}
	}
}