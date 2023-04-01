using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using STATSTG = System.Runtime.InteropServices.ComTypes.STATSTG;

namespace GRF.FileFormats.DbFormat {
	/// <summary>
	/// The class <c>Interop</c> imports API-Methods from various dlls and defines some
	/// structures used by this methods.
	/// </summary>
	[SuppressUnmanagedCodeSecurity]
	public class Interop {
		/// <summary>
		/// Constant imports
		/// </summary>
		public const uint
			S_OK = 0,
			STG_E_FILENOTFOUND = 2147680258,
			STG_E_INVALIDNAME = 2147680508;

		/// <summary>
		/// Constant imports
		/// </summary>
		public const int
			STGTY_STORAGE = 1,
			STGTY_STREAM = 2,
			STGTY_LOCKBYTES = 3,
			STGTY_PROPERTY = 4,
			COMPACT_DATA = 0,
			COMPACT_DATA_AND_PATH = 1;

		/// <summary>
		/// WINTRUST_ACTION_GENERIC_VERIFY_V2 constant
		/// </summary>
		public static Guid WINTRUST_ACTION_GENERIC_VERIFY_V2;

		/// <summary>
		/// Static constructor
		/// </summary>
		static Interop() {
			WINTRUST_ACTION_GENERIC_VERIFY_V2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
		}

		#region Nested type: ITS_Control_Data

		/// <summary>
		/// ITStorage control data struct
		/// </summary>
		[ComVisible(false), StructLayout(LayoutKind.Sequential)]
		public struct ITS_Control_Data {
			/// <summary>
			/// Controldata flag
			/// </summary>
			public int cdwControlData;

			/// <summary>
			/// Controldata flag
			/// </summary>
			public int adwControlData;
		}

		#endregion

		#region ole32.dll imports

		/// <summary>
		/// Imports the ole32.dll function StgOpenStorage
		/// </summary>
		/// <param name="wcsName">storage name</param>
		/// <param name="pstgPriority">Points to previous opening of the storage object</param>
		/// <param name="grfMode">Access mode for the new storage object</param>
		/// <param name="snbExclude">Points to a block of stream names in the storage object</param>
		/// <param name="reserved">Reserved; must be zero</param>
		/// <param name="storage">out parameter returning the storage</param>
		/// <returns>Returns S_OK if succeeded</returns>
		[DllImport("Ole32.dll")]
		public static extern int StgOpenStorage([MarshalAs(UnmanagedType.LPWStr)] string wcsName, IStorage pstgPriority, int grfMode, IntPtr snbExclude, int reserved, out IStorage storage);

		#endregion

		#region storage interface imports

		#region Nested type: IEnumSTATSTG

		/// <summary>
		/// Imports the OLE interface IEnumSTATSG
		/// </summary>
		[Guid("0000000D-0000-0000-C000-000000000046")]
		[SuppressUnmanagedCodeSecurity]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface IEnumSTATSTG {
			/// <summary>
			/// Retrieves the next celt items in the enumeration sequence.
			/// If there are fewer than the requested number of elements left in the sequence,
			/// it retrieves the remaining elements.
			/// The number of elements actually retrieved is returned through pceltFetched
			/// (unless the caller passed in NULL for that parameter).
			/// </summary>
			/// <param name="celt">Number of objects to retreive</param>
			/// <param name="rgVar">Array of STATSG elements</param>
			/// <param name="pceltFetched">Number of elements actually supplied</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int Next(int celt, out STATSTG rgVar, out int pceltFetched);

			/// <summary>
			/// Skips over the next specified number of elements in the enumeration sequence.
			/// </summary>
			/// <param name="celt">Number of elements to skip</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int Skip(int celt);

			/// <summary>
			/// Resets the enumeration sequence to the beginning.
			/// </summary>
			/// <returns>Returns S_OK if succeeded</returns>
			int Reset();

			/// <summary>
			/// Creates another enumerator that contains the same enumeration state
			/// as the current one. Using this function, a client can record a
			/// particular point in the enumeration sequence, and then return
			/// to that point at a later time. The new enumerator supports the same
			/// interface as the original one.
			/// </summary>
			/// <param name="newEnum">Output parameter of the new enumerator</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int Clone(out IEnumSTATSTG newEnum);
		}

		#endregion

		#region Nested type: ILockBytes

		/// <summary>
		/// The ILockBytes interface is implemented on a byte array object that
		/// is backed by some physical storage, such as a disk file, global memory,
		/// or a database. It is used by a COM compound file storage object to give
		/// its root storage access to the physical device, while isolating the root
		/// storage from the details of accessing the physical storage.
		/// </summary>
		[Guid("0000000a-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface ILockBytes {
			/// <summary>
			/// Reads a specified number of bytes starting at a specified offset
			/// from the beginning of the byte array object.
			/// </summary>
			/// <param name="ulOffset">Specifies the starting point for reading data</param>
			/// <param name="pv">Points to the buffer into which the data is read</param>
			/// <param name="cb">Specifies the number of bytes to read</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int ReadAt(long ulOffset, out IntPtr pv, int cb);

			/// <summary>
			/// Writes the specified number of bytes starting at a specified offset
			/// from the beginning of the byte array.
			/// </summary>
			/// <param name="ulOffset">Specifies the starting point for writing data</param>
			/// <param name="pv">Points to the buffer containing the data to be written</param>
			/// <param name="cb">Specifies the number of bytes to write</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int WriteAt(long ulOffset, IntPtr pv, int cb);

			/// <summary>
			/// Ensures that any internal buffers maintained by the ILockBytes
			/// implementation are written out to the underlying physical storage.
			/// </summary>
			void Flush();

			/// <summary>
			/// Changes the size of the byte array.
			/// </summary>
			/// <param name="cb">Specifies the new size of the byte array in bytes</param>
			void SetSize(long cb);

			/// <summary>
			/// Restricts access to a specified range of bytes in the byte array.
			/// </summary>
			/// <param name="libOffset">Specifies the byte offset for the beginning of the range</param>
			/// <param name="cb">Specifies the length of the range in bytes</param>
			/// <param name="dwLockType">Specifies the type of restriction on accessing the specified range</param>
			void LockRegion(long libOffset, long cb, int dwLockType);

			/// <summary>
			/// Removes the access restriction on a previously locked range of bytes.
			/// </summary>
			/// <param name="libOffset">Specifies the byte offset for the beginning of the range</param>
			/// <param name="cb">Specifies the length of the range in bytes</param>
			/// <param name="dwLockType">Specifies the access restriction previously placed on the range</param>
			void UnlockRegion(long libOffset, long cb, int dwLockType);

			/// <summary>
			/// Retrieves a STATSTG structure containing information for this byte array object.
			/// </summary>
			/// <param name="pstatstg">Location for STATSTG structure</param>
			/// <param name="grfStatFlag">Values taken from the STATFLAG enumeration</param>
			void Stat(ref STATSTG pstatstg, int grfStatFlag);
		}

		#endregion

		#region Nested type: IStorage

		/// <summary>
		/// The IStorage interface supports the creation and management of structured storage objects.
		/// Structured storage allows hierarchical storage of information within a single file, and
		/// is often referred to as "a file system within a file". Elements of a structured storage
		/// object are storages and streams. Storages are analogous to directories, and streams are
		/// analogous to files. Within a structured storage there will be a primary storage object that
		/// may contain substorages, possibly nested, and streams. Storages provide the structure of the
		/// object, and streams contain the data, which is manipulated through the IStream interface.
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		[Guid("0000000B-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[ComImport]
		public interface IStorage {
			/// <summary>
			/// Creates and opens a stream object with the specified name contained in this storage object.
			/// All elements within a storage object — both streams and other storage objects — are kept in
			/// the same name space.
			/// </summary>
			/// <param name="pwcsName">Name of the new stream</param>
			/// <param name="grfMode">Access mode for the new stream</param>
			/// <param name="reserved1">Reserved; must be zero</param>
			/// <param name="reserved2">Reserved; must be zero</param>
			/// <returns>Returns S_OK if succeeded</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStream CreateStream([MarshalAs(UnmanagedType.BStr)] string pwcsName, [MarshalAs(UnmanagedType.U4)] int grfMode, [MarshalAs(UnmanagedType.U4)] int reserved1, [MarshalAs(UnmanagedType.U4)] int reserved2);

			/// <summary>
			/// Opens an existing stream object within this storage object in the specified access mode.
			/// </summary>
			/// <param name="pwcsName">Name of the stream</param>
			/// <param name="reserved1">Reserved; must be NULL</param>
			/// <param name="grfMode">Access mode for the new stream</param>
			/// <param name="reserved2">Reserved; must be zero</param>
			/// <returns>Returns S_OK if succeeded</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStream OpenStream([MarshalAs(UnmanagedType.BStr)] string pwcsName, IntPtr reserved1, [MarshalAs(UnmanagedType.U4)] int grfMode, [MarshalAs(UnmanagedType.U4)] int reserved2);

			/// <summary>
			/// Creates and opens a new storage object nested within this storage object.
			/// </summary>
			/// <param name="pwcsName">Name of the new storage object</param>
			/// <param name="grfMode">Access mode for the new storage object</param>
			/// <param name="reserved1">Reserved; must be zero</param>
			/// <param name="reserved2">Reserved; must be zero</param>
			/// <returns>Returns S_OK if succeeded</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStorage CreateStorage([MarshalAs(UnmanagedType.BStr)] string pwcsName, [MarshalAs(UnmanagedType.U4)] int grfMode, [MarshalAs(UnmanagedType.U4)] int reserved1, [MarshalAs(UnmanagedType.U4)] int reserved2);

			/// <summary>
			/// Opens an existing storage object with the specified name in the specified access mode.
			/// </summary>
			/// <param name="pwcsName">Name of the storage</param>
			/// <param name="pstgPriority">Points to previous opening of the storage object</param>
			/// <param name="grfMode">Access mode for the new storage object</param>
			/// <param name="snbExclude">Points to a block of stream names in the storage object</param>
			/// <param name="reserved">Reserved; must be zero</param>
			/// <returns>Returns S_OK if succeeded</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStorage OpenStorage([MarshalAs(UnmanagedType.BStr)] string pwcsName, IntPtr pstgPriority, [MarshalAs(UnmanagedType.U4)] int grfMode, IntPtr snbExclude, [MarshalAs(UnmanagedType.U4)] int reserved);

			/// <summary>
			/// Copies the entire contents of an open storage object to another storage object.
			/// </summary>
			/// <param name="ciidExclude">Number of elements in rgiidExclude</param>
			/// <param name="rgiidExclude">Array of interface identifiers (IIDs)</param>
			/// <param name="snbExclude">Points to a block of stream names in the storage object</param>
			/// <param name="pstgDest">Points to destination storage object</param>
			void CopyTo(int ciidExclude, [MarshalAs(UnmanagedType.LPArray)] Guid[] rgiidExclude, IntPtr snbExclude, [MarshalAs(UnmanagedType.Interface)] IStorage pstgDest);

			/// <summary>
			/// Copies or moves a substorage or stream from this storage object to another storage object.
			/// </summary>
			/// <param name="pwcsName">Name of the element to be moved</param>
			/// <param name="pstgDest">Points to destination storage object</param>
			/// <param name="pwcsNewName">Points to new name of element in destination</param>
			/// <param name="grfFlags">Specifies a copy or a move</param>
			void MoveElementTo([MarshalAs(UnmanagedType.BStr)] string pwcsName, [MarshalAs(UnmanagedType.Interface)] IStorage pstgDest, [MarshalAs(UnmanagedType.BStr)] string pwcsNewName, [MarshalAs(UnmanagedType.U4)] int grfFlags);

			/// <summary>
			/// Ensures that any changes made to a storage object open in transacted mode are reflected in
			/// the parent storage; for a root storage, reflects the changes in the actual device, for
			/// example, a file on disk. For a root storage object opened in direct mode, this method has no
			/// effect except to flush all memory buffers to the disk. For non-root storage objects in direct mode,
			/// this method has no effect.
			/// </summary>
			/// <param name="grfCommitFlags">Specifies how changes are to be committed</param>
			void Commit(int grfCommitFlags);

			/// <summary>
			/// Discards all changes that have been made to the storage object since the last commit.
			/// </summary>
			void Revert();

			/// <summary>
			/// Retrieves a pointer to an enumerator object that can be used to enumerate the storage and stream
			/// objects contained within this storage object.
			/// </summary>
			/// <param name="reserved1">Reserved; must be zero</param>
			/// <param name="reserved2">Reserved; must be NULL</param>
			/// <param name="reserved3">Reserved; must be zero</param>
			/// <param name="ppenum">Output variable that receives the IEnumSTATSTG interface</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int EnumElements([MarshalAs(UnmanagedType.U4)] int reserved1, IntPtr reserved2, [MarshalAs(UnmanagedType.U4)] int reserved3, [MarshalAs(UnmanagedType.Interface)] out IEnumSTATSTG ppenum);

			/// <summary>
			/// Removes the specified storage or stream from this storage object.
			/// </summary>
			/// <param name="pwcsName">Name of the element to be removed</param>
			void DestroyElement([MarshalAs(UnmanagedType.BStr)] string pwcsName);

			/// <summary>
			/// Renames the specified substorage or stream in this storage object.
			/// </summary>
			/// <param name="pwcsOldName">Old name of the element</param>
			/// <param name="pwcsNewName">New name of the element</param>
			void RenameElement([MarshalAs(UnmanagedType.BStr)] string pwcsOldName, [MarshalAs(UnmanagedType.BStr)] string pwcsNewName);

			/// <summary>
			/// Sets the modification, access, and creation times of the specified storage element, if supported
			/// by the underlying file system.
			/// </summary>
			/// <param name="pwcsName">Name of the element to be changed</param>
			/// <param name="pctime">New creation time for element, or NULL</param>
			/// <param name="patime">New access time for element, or NULL</param>
			/// <param name="pmtime">New modification time for element, or NULL</param>
			void SetElementTimes([MarshalAs(UnmanagedType.BStr)] string pwcsName, FILETIME pctime, FILETIME patime, FILETIME pmtime);

			/// <summary>
			/// Assigns the specified CLSID to this storage object.
			/// </summary>
			/// <param name="clsid">Class identifier to be assigned to the storage object</param>
			void SetClass(ref Guid clsid);

			/// <summary>
			/// Stores up to 32 bits of state information in this storage object.
			/// </summary>
			/// <param name="grfStateBits">Specifies new values of bits</param>
			/// <param name="grfMask">Specifies mask that indicates which bits are significant</param>
			void SetStateBits(int grfStateBits, int grfMask);

			/// <summary>
			/// Retrieves the STATSTG structure for this open storage object.
			/// </summary>
			/// <param name="pStatStg">Ouput STATSTG structure</param>
			/// <param name="grfStatFlag">Values taken from the STATFLAG enumeration</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int Stat(out STATSTG pStatStg, int grfStatFlag);
		}

		#endregion

		#region Nested type: ITStorage

		/// <summary>
		/// Imports the interface ITStorage
		/// </summary>
		[SuppressUnmanagedCodeSecurity]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("88CC31DE-27AB-11D0-9DF9-00A0C922E6EC")]
		[ComImport]
		public interface ITStorage {
			/// <summary>
			/// Creates a new doc file
			/// </summary>
			/// <param name="pwcsName">Name of the new stream</param>
			/// <param name="grfMode">Access mode for the new stream</param>
			/// <param name="reserved">Reserved; must be zero</param>
			/// <returns>An IStorage reference to the new file</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStorage StgCreateDocfile([MarshalAs(UnmanagedType.BStr)] string pwcsName, int grfMode, int reserved);

			/// <summary>
			/// Creates a new doc file on licked bytes
			/// </summary>
			/// <param name="plkbyt">ILockBytes interface</param>
			/// <param name="grfMode">Access mode for the new stream</param>
			/// <param name="reserved">Reserved; must be zero</param>
			/// <returns>An IStorage reference to the new file</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStorage StgCreateDocfileOnILockBytes(ILockBytes plkbyt, int grfMode, int reserved);

			/// <summary>
			/// Checks if a filename exists in the storage
			/// </summary>
			/// <param name="pwcsName">name of the file to check</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int StgIsStorageFile([MarshalAs(UnmanagedType.BStr)] string pwcsName);

			/// <summary>
			/// Checks if a ILockBytes is part of the storage
			/// </summary>
			/// <param name="plkbyt">ILockBytes instance</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int StgIsStorageILockBytes(ILockBytes plkbyt);

			/// <summary>
			/// Opens a storage
			/// </summary>
			/// <param name="pwcsName">Name of the storage</param>
			/// <param name="pstgPriority">Points to previous opening of the storage object</param>
			/// <param name="grfMode">Access mode for the new storage object</param>
			/// <param name="snbExclude">Points to a block of stream names in the storage object</param>
			/// <param name="reserved">Reserved; must be zero</param>
			/// <returns>An IStorage reference to the new file</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStorage StgOpenStorage([MarshalAs(UnmanagedType.BStr)] string pwcsName, IntPtr pstgPriority, [MarshalAs(UnmanagedType.U4)] int grfMode, IntPtr snbExclude, [MarshalAs(UnmanagedType.U4)] int reserved);

			/// <summary>
			/// Opens a storage
			/// </summary>
			/// <param name="plkbyt">ILockBytes instance</param>
			/// <param name="pStgPriority">Points to previous opening of the storage object</param>
			/// <param name="grfMode">Access mode for the new storage object</param>
			/// <param name="snbExclude">Points to a block of stream names in the storage object</param>
			/// <param name="reserved">Reserved; must be zero</param>
			/// <returns>An IStorage reference to the new file</returns>
			[return: MarshalAs(UnmanagedType.Interface)]
			IStorage StgOpenStorageOnILockBytes(ILockBytes plkbyt, IStorage pStgPriority, int grfMode, IntPtr snbExclude, int reserved);

			/// <summary>
			/// Set the file times of a storage file
			/// </summary>
			/// <param name="lpszName">Name of the element to be changed</param>
			/// <param name="pctime">New creation time for element, or NULL</param>
			/// <param name="patime">New access time for element, or NULL</param>
			/// <param name="pmtime">New modification time for element, or NULL</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int StgSetTimes([MarshalAs(UnmanagedType.BStr)] string lpszName, FILETIME pctime, FILETIME patime, FILETIME pmtime);

			/// <summary>
			/// Sets the storage control data
			/// </summary>
			/// <param name="pControlData">control data</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int SetControlData(ITS_Control_Data pControlData);

			/// <summary>
			/// Sets the default control data
			/// </summary>
			/// <param name="ppControlData">control data</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int DefaultControlData(ITS_Control_Data ppControlData);

			/// <summary>
			/// Compact a file
			/// </summary>
			/// <param name="pwcsName">filename</param>
			/// <param name="iLev">level</param>
			/// <returns>Returns S_OK if succeeded</returns>
			int Compact([MarshalAs(UnmanagedType.BStr)] string pwcsName, int iLev);
		}

		#endregion

		#region Nested type: UCOMITStorage

		/// <summary>
		/// Imports the class <c>UCOMITStorage</c>
		/// </summary>
		[Guid("5D02926A-212E-11D0-9DF9-00A0C922E6EC")]
		[ComImport]
		public class UCOMITStorage {
		}

		#endregion

		#endregion
	}
}