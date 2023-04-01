using System;
using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;

namespace GRF.FileFormats.DbFormat {
	/// <summary>
	/// The class <c>IBaseStorageWrapper</c> is the base wrapper for the Interop.IStorage interface
	/// </summary>
	public class BaseStorageWrapper {
		/// <summary>
		/// Internal member storing the baseUrl of the file(s)
		/// </summary>
		private static string _baseUrl;

		/// <summary>
		/// Internal member storing the file object collection (if files where enumerated)
		/// </summary>
		public FileObjectCollection FoCollection;

		/// <summary>
		/// Internal member storing the actual storage interface
		/// </summary>
		protected Interop.IStorage storage;

		/// <summary>
		/// Constructor of the class
		/// </summary>
		public BaseStorageWrapper() {
			FoCollection = new FileObjectCollection();
		}

		/// <summary>
		/// Gets the internal Interop.IStorage member
		/// </summary>
		public Interop.IStorage Storage {
			get { return storage; }
		}

		/// <summary>
		/// Gets the base url of files
		/// </summary>
		public static string BaseUrl {
			get { return _baseUrl; }

			set { _baseUrl = String.Concat(value, "::/"); }
		}

		/// <summary>
		/// Enumerates an Interop.IStorage object and creates the internal file object collection
		/// </summary>
		/// <param name="stgEnum">Interop.IStorage to enumerate</param>
		public virtual void EnumIStorageObject(Interop.IStorage stgEnum) {
			EnumIStorageObject(stgEnum, "");
		}

		/// <summary>
		/// Enumerates an Interop.IStorage object and creates the internal file object collection
		/// </summary>
		/// <param name="stgEnum">Interop.IStorage to enumerate</param>
		/// <param name="basePath">Sets the base url for the storage files</param>
		protected void EnumIStorageObject(Interop.IStorage stgEnum, string basePath) {
			Interop.IEnumSTATSTG iEnumStatstg;

			STATSTG sTatstg;

			int i;

			stgEnum.EnumElements(0, IntPtr.Zero, 0, out iEnumStatstg);
			iEnumStatstg.Reset();
			while (iEnumStatstg.Next(1, out sTatstg, out i) == (int) Interop.S_OK) {
				if (i == 0)
					break;

				var newFileObj = new FileObject();
				newFileObj.FileType = sTatstg.type;
				switch (sTatstg.type) {
					case 1:
						Interop.IStorage iStorage = stgEnum.OpenStorage(sTatstg.pwcsName, IntPtr.Zero, 16, IntPtr.Zero, 0);
						if (iStorage != null) {
							string str = String.Concat(basePath, sTatstg.pwcsName);
							newFileObj.FileStorage = iStorage;
							newFileObj.FilePath = basePath;
							newFileObj.FileName = sTatstg.pwcsName;
							FoCollection.Add(newFileObj);
							EnumIStorageObject(iStorage, str);
						}
						break;

					case 2:
						IStream uComiStream = stgEnum.OpenStream(sTatstg.pwcsName, IntPtr.Zero, 16, 0);
						newFileObj.FilePath = basePath;
						newFileObj.FileName = sTatstg.pwcsName;
						newFileObj.FileStream = uComiStream;
						FoCollection.Add(newFileObj);
						break;

					case 4:
						Debug.WriteLine("Ignoring IProperty type ...");
						break;

					case 3:
						Debug.WriteLine("Ignoring ILockBytes type ...");
						break;

					default:
						Debug.WriteLine("Unknown object type ...");
						break;
				}
			}
		}
	}
}