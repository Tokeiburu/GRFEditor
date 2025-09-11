using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace Utilities {
	public static class NativeMethods {
		[DllImport("user32.dll")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("dwmapi.dll")]
		public static extern IntPtr DwmIsCompositionEnabled(out bool pfEnabled);

		[DllImport("user32.dll")]
		public static extern IntPtr GetActiveWindow();

		[DllImport("kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true)]
		public static extern IntPtr OpenFile([MarshalAs(UnmanagedType.LPStr)]string lpFileName, out OFSTRUCT lpReOpenBuff, OpenFileStyle uStyle);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		[DllImport("user32.dll", EntryPoint = "GetKeyboardState", SetLastError = true)]
		public static extern bool NativeGetKeyboardState([Out] byte[] keyStates);

		[DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
		public static extern int memcmp(byte[] b1, byte[] b2, long count);

		[DllImport("shlwapi.dll", CharSet = CharSet.Auto)]
		public static extern bool PathCompactPathEx([Out] StringBuilder pszOut, string szPath, int cchMax, int dwFlags);

		[DllImport("kernel32.dll", EntryPoint = "LoadLibrary")]
		public static extern IntPtr LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpLibFileName);

		[DllImport("kernel32.dll")]
		public static extern uint GetLastError();

		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
		public static extern IntPtr GetProcAddress(int hModule, [MarshalAs(UnmanagedType.LPStr)] string lpProcName);

		[DllImport("kernel32.dll", EntryPoint = "GetProcAddress")]
		public static extern IntPtr GetProcAddress2(IntPtr hModule, IntPtr lpProcName);

		[DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
		public static extern bool FreeLibrary(int hModule);

		[DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
		public static extern bool FreeLibrary(IntPtr hModule);

		[DllImport("pdh.dll", SetLastError = true)]
		public static extern int PdhOpenQuery(string dataSource, IntPtr userData, out IntPtr query);

		[DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern int PdhAddCounter(IntPtr query, string fullCounterPath, IntPtr userData, out IntPtr counter);

		[DllImport("pdh.dll", SetLastError = true)]
		public static extern int PdhCollectQueryData(IntPtr query);

		[DllImport("pdh.dll", SetLastError = true)]
		public static extern int PdhGetFormattedCounterValue(IntPtr counter, uint format, out uint type, out PDH_FMT_COUNTERVALUE value);

		[DllImport("pdh.dll", SetLastError = true)]
		public static extern int PdhCloseQuery(IntPtr query);

		// Use PDH_FMT_DOUBLE for a floating-point value.
		public const uint PDH_FMT_DOUBLE = 0x00000200;
		public const uint ERROR_SUCCESS = 0;

		[StructLayout(LayoutKind.Sequential)]
		public struct PDH_FMT_COUNTERVALUE {
			public uint CStatus;
			public double doubleValue;
		}

		[DllImport("shell32.dll")]
		public static extern int SHCreateStdEnumFmtEtc(uint cfmt, FORMATETC[] afmt, out IEnumFORMATETC ppenumFormatEtc);

		[return: MarshalAs(UnmanagedType.Interface)]
		[DllImport("ole32.dll", PreserveSig = false)]
		public static extern IStream CreateStreamOnHGlobal(IntPtr hGlobal, [MarshalAs(UnmanagedType.Bool)] bool fDeleteOnRelease);

		[DllImport("ole32.dll", CharSet = CharSet.Auto, ExactSpelling = true, PreserveSig = false)]
		public static extern void DoDragDrop(IDataObject dataObject, IDropSource dropSource, int allowedEffects, int[] finalEffect);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GlobalLock(IntPtr hMem);

		[return: MarshalAs(UnmanagedType.Bool)]
		[DllImport("kernel32.dll")]
		public static extern bool GlobalUnlock(IntPtr hMem);

		[DllImport("kernel32.dll")]
		public static extern IntPtr GlobalSize(IntPtr handle);

		[DllImport("user32.dll", EntryPoint = "DestroyIcon", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern bool DestroyIcon(IntPtr hIcon);

		[DllImport("Shell32.dll")]
		public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

		[DllImport("shell32.dll", EntryPoint = "SHGetDesktopFolder", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int SHGetDesktopFolder_([MarshalAs(UnmanagedType.Interface)] out IShellFolder ppshf);


		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public extern static bool IsWow64Process(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] out bool isWow64);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public extern static IntPtr GetCurrentProcess();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public extern static IntPtr GetModuleHandle(string moduleName);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		public extern static IntPtr GetProcAddress(IntPtr hModule, string methodName);


		[DllImport("shell32.dll", EntryPoint = "SHOpenFolderAndSelectItems")]
		static extern int SHOpenFolderAndSelectItems_(
			[In] IntPtr pidlFolder, uint cidl, [In, Optional, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
			int dwFlags);

		public static void SHOpenFolderAndSelectItems(IntPtr pidlFolder, IntPtr[] apidl, int dwFlags) {
			var cidl = (apidl != null) ? (uint)apidl.Length : 0U;
			var result = SHOpenFolderAndSelectItems_(pidlFolder, cidl, apidl, dwFlags);
			Marshal.ThrowExceptionForHR(result);
		}

		[DllImport("Kernel32")]
		public extern static Boolean CloseHandle(IntPtr handle);

		[DllImport("shell32.dll")]
		public static extern void ILFree([In] IntPtr pidl);

		[DllImport("user32.dll")]
		internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

		[DllImport("user32.dll")]
		internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateFile(
			 [MarshalAs(UnmanagedType.LPTStr)] string filename,
			 [MarshalAs(UnmanagedType.U4)] FileAccess access,
			 [MarshalAs(UnmanagedType.U4)] FileShare share,
			 IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
			 [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			 [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
			 IntPtr templateFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
		public static extern IntPtr CreateFileA(
			 [MarshalAs(UnmanagedType.LPStr)] string filename,
			 [MarshalAs(UnmanagedType.U4)] FileAccess access,
			 [MarshalAs(UnmanagedType.U4)] FileShare share,
			 IntPtr securityAttributes,
			 [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			 [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
			 IntPtr templateFile);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern IntPtr CreateFileW(
			 [MarshalAs(UnmanagedType.LPWStr)] string filename,
			 [MarshalAs(UnmanagedType.U4)] FileAccess access,
			 [MarshalAs(UnmanagedType.U4)] FileShare share,
			 IntPtr securityAttributes,
			 [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
			 [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
			 IntPtr templateFile);

		[DllImport("kernel32.dll")]
		public static extern bool GetFileSizeEx(IntPtr hFile, out long lpFileSize);

		[ComImport,
		 Guid("000214E6-0000-0000-C000-000000000046"),
		 InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
		 ComConversionLoss]
		public interface IShellFolder {
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void ParseDisplayName(IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In, MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, [Out] out uint pchEaten, [Out] out IntPtr ppidl, [In, Out] ref uint pdwAttributes);
			[PreserveSig]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int EnumObjects([In] IntPtr hwnd, [In] SHCONT grfFlags, [MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenumIDList);

			[PreserveSig]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int BindToObject([In] IntPtr pidl, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IShellFolder ppv);

			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void BindToStorage([In] ref IntPtr pidl, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In] ref Guid riid, out IntPtr ppv);

			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CompareIDs([In] IntPtr lParam, [In] ref IntPtr pidl1, [In] ref IntPtr pidl2);

			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void CreateViewObject([In] IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);

			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetAttributesOf([In] uint cidl, [In] IntPtr apidl, [In, Out] ref uint rgfInOut);


			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetUIObjectOf([In] IntPtr hwndOwner, [In] uint cidl, [In] IntPtr apidl, [In] ref Guid riid, [In, Out] ref uint rgfReserved, out IntPtr ppv);

			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void GetDisplayNameOf([In] ref IntPtr pidl, [In] uint uFlags, out IntPtr pName);

			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			void SetNameOf([In] IntPtr hwnd, [In] ref IntPtr pidl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszName, [In] uint uFlags, [Out] IntPtr ppidlOut);
		}

		[ComImport,
		 Guid("000214F2-0000-0000-C000-000000000046"),
		 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IEnumIDList {
			[PreserveSig]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int Next(uint celt, IntPtr rgelt, out uint pceltFetched);

			[PreserveSig]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int Skip([In] uint celt);

			[PreserveSig]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int Reset();

			[PreserveSig]
			[MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
			int Clone([MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenum);
		}


		/// <summary>
		/// Holds information, whether the Windows is a server, workstation or domain controller.
		/// </summary>
		public enum ProductType : byte {
			/// <summary>
			/// The operating system is Windows 10, Windows 8, Windows 7,...
			/// </summary>
			/// <remarks>VER_NT_WORKSTATION</remarks>
			Workstation = 0x0000001,
			/// <summary>
			/// The system is a domain controller and the operating system is Windows Server.
			/// </summary>
			/// <remarks>VER_NT_DOMAIN_CONTROLLER</remarks>
			DomainController = 0x0000002,
			/// <summary>
			/// The operating system is Windows Server. Note that a server that is also a domain controller
			/// is reported as VER_NT_DOMAIN_CONTROLLER, not VER_NT_SERVER.
			/// </summary>
			/// <remarks>VER_NT_SERVER</remarks>
			Server = 0x0000003
		}


		/// <summary>
		/// Holds specific information for certain Windows variants (e.g. Small Business, Datacenter,...)
		/// </summary>
		[Flags]
		public enum SuiteMask : ushort {
			/// <summary>
			/// Microsoft BackOffice components are installed. 
			/// </summary>
			VER_SUITE_BACKOFFICE = 0x00000004,
			/// <summary>
			/// Windows Server 2003, Web Edition is installed
			/// </summary>
			VER_SUITE_BLADE = 0x00000400,
			/// <summary>
			/// Windows Server 2003, Compute Cluster Edition is installed.
			/// </summary>
			VER_SUITE_COMPUTE_SERVER = 0x00004000,
			/// <summary>
			/// Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed. 
			/// </summary>
			VER_SUITE_DATACENTER = 0x00000080,
			/// <summary>
			/// Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or Windows 2000 Advanced Server is installed.
			/// Refer to the Remarks section for more information about this bit flag. 
			/// </summary>
			VER_SUITE_ENTERPRISE = 0x00000002,
			/// <summary>
			/// Windows XP Embedded is installed. 
			/// </summary>
			VER_SUITE_EMBEDDEDNT = 0x00000040,
			/// <summary>
			/// Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed. 
			/// </summary>
			VER_SUITE_PERSONAL = 0x00000200,
			/// <summary>
			/// Remote Desktop is supported, but only one interactive session is supported. This value is set unless the system is running in application server mode. 
			/// </summary>
			VER_SUITE_SINGLEUSERTS = 0x00000100,
			/// <summary>
			/// Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
			/// Refer to the Remarks section for more information about this bit flag. 
			/// </summary>
			VER_SUITE_SMALLBUSINESS = 0x00000001,
			/// <summary>
			/// Microsoft Small Business Server is installed with the restrictive client license in force. Refer to the Remarks section for more information about this bit flag. 
			/// </summary>
			VER_SUITE_SMALLBUSINESS_RESTRICTED = 0x00000020,
			/// <summary>
			/// Windows Storage Server 2003 R2 or Windows Storage Server 2003is installed. 
			/// </summary>
			VER_SUITE_STORAGE_SERVER = 0x00002000,
			/// <summary>
			/// Terminal Services is installed. This value is always set.
			/// If VER_SUITE_TERMINAL is set but VER_SUITE_SINGLEUSERTS is not set, the system is running in application server mode.
			/// </summary>
			VER_SUITE_TERMINAL = 0x00000010,
			/// <summary>
			/// Windows Home Server is installed. 
			/// </summary>
			VER_SUITE_WH_SERVER = 0x00008000

			//VER_SUITE_MULTIUSERTS = 0x00020000
		}

		public enum NTSTATUS : uint {
			/// <summary>
			/// The operation completed successfully. 
			/// </summary>
			STATUS_SUCCESS = 0x00000000
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct OSVERSIONINFOEX {
			// The OSVersionInfoSize field must be set to Marshal.SizeOf(typeof(OSVERSIONINFOEX))
			public int OSVersionInfoSize;
			public int MajorVersion;
			public int MinorVersion;
			public int BuildNumber;
			public int PlatformId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string CSDVersion;
			public ushort ServicePackMajor;
			public ushort ServicePackMinor;
			public SuiteMask SuiteMask;
			public ProductType ProductType;
			public byte Reserved;
		}

		[DllImport("ntdll.dll", EntryPoint = "RtlGetVersion", SetLastError = true, CharSet = CharSet.Unicode)]
		internal static extern NTSTATUS RtlGetVersion(ref OSVERSIONINFOEX versionInfo);

		#region Constants

		public const int DRAGDROP_S_DROP = 0x00040100;
		public const int DRAGDROP_S_CANCEL = 0x00040101;
		public const int DRAGDROP_S_USEDEFAULTCURSORS = 0x00040102;
		public const int DV_E_DVASPECT = -2147221397;
		public const int DV_E_FORMATETC = -2147221404;
		public const int DV_E_TYMED = -2147221399;
		public const int E_FAIL = -2147467259;
		public const uint FD_CREATETIME = 0x00000008;
		public const uint FD_WRITESTIME = 0x00000020;
		public const uint FD_FILESIZE = 0x00000040;
		public const int OLE_E_ADVISENOTSUPPORTED = -2147221501;
		public const int S_OK = 0;
		public const int S_FALSE = 1;
		public const int VARIANT_FALSE = 0;
		public const int VARIANT_TRUE = -1;

		public const string CFSTR_FILECONTENTS = "FileContents";
		public const string CFSTR_FILEDESCRIPTORW = "FileGroupDescriptorW";
		public const string CFSTR_FILESOURCE = "FileSource";
		public const string CFSTR_PASTESUCCEEDED = "Paste Succeeded";
		public const string CFSTR_PERFORMEDDROPEFFECT = "Performed DropEffect";
		public const string CFSTR_PREFERREDDROPEFFECT = "Preferred DropEffect";

		#endregion

		#region Enums
		[Flags]
		public enum OpenFileStyle : uint {
			OF_CANCEL = 0x00000800,  // Ignored. For a dialog box with a Cancel button, use OF_PROMPT.
			OF_CREATE = 0x00001000,  // Creates a new file. If file exists, it is truncated to zero (0) length.
			OF_DELETE = 0x00000200,  // Deletes a file.
			OF_EXIST = 0x00004000,  // Opens a file and then closes it. Used to test that a file exists
			OF_PARSE = 0x00000100,  // Fills the OFSTRUCT structure, but does not do anything else.
			OF_PROMPT = 0x00002000,  // Displays a dialog box if a requested file does not exist 
			OF_READ = 0x00000000,  // Opens a file for reading only.
			OF_READWRITE = 0x00000002,  // Opens a file with read/write permissions.
			OF_REOPEN = 0x00008000,  // Opens a file by using information in the reopen buffer.

			// For MS-DOS–based file systems, opens a file with compatibility mode, allows any process on a 
			// specified computer to open the file any number of times.
			// Other efforts to open a file with other sharing modes fail. This flag is mapped to the 
			// FILE_SHARE_READ|FILE_SHARE_WRITE flags of the CreateFile function.
			OF_SHARE_COMPAT = 0x00000000,

			// Opens a file without denying read or write access to other processes.
			// On MS-DOS-based file systems, if the file has been opened in compatibility mode
			// by any other process, the function fails.
			// This flag is mapped to the FILE_SHARE_READ|FILE_SHARE_WRITE flags of the CreateFile function.
			OF_SHARE_DENY_NONE = 0x00000040,

			// Opens a file and denies read access to other processes.
			// On MS-DOS-based file systems, if the file has been opened in compatibility mode,
			// or for read access by any other process, the function fails.
			// This flag is mapped to the FILE_SHARE_WRITE flag of the CreateFile function.
			OF_SHARE_DENY_READ = 0x00000030,

			// Opens a file and denies write access to other processes.
			// On MS-DOS-based file systems, if a file has been opened in compatibility mode,
			// or for write access by any other process, the function fails.
			// This flag is mapped to the FILE_SHARE_READ flag of the CreateFile function.
			OF_SHARE_DENY_WRITE = 0x00000020,

			// Opens a file with exclusive mode, and denies both read/write access to other processes.
			// If a file has been opened in any other mode for read/write access, even by the current process,
			// the function fails.
			OF_SHARE_EXCLUSIVE = 0x00000010,

			// Verifies that the date and time of a file are the same as when it was opened previously.
			// This is useful as an extra check for read-only files.
			OF_VERIFY = 0x00000400,

			// Opens a file for write access only.
			OF_WRITE = 0x00000001
		}

		[Flags]
		public enum SHCONT : ushort {
			SHCONTF_CHECKING_FOR_CHILDREN = 0x0010,
			SHCONTF_FOLDERS = 0x0020,
			SHCONTF_NONFOLDERS = 0x0040,
			SHCONTF_INCLUDEHIDDEN = 0x0080,
			SHCONTF_INIT_ON_FIRST_NEXT = 0x0100,
			SHCONTF_NETPRINTERSRCH = 0x0200,
			SHCONTF_SHAREABLE = 0x0400,
			SHCONTF_STORAGE = 0x0800,
			SHCONTF_NAVIGATION_ENUM = 0x1000,
			SHCONTF_FASTITEMS = 0x2000,
			SHCONTF_FLATLIST = 0x4000,
			SHCONTF_ENABLE_ASYNC = 0x8000
		}

		[Flags]
		public enum SFGAO : uint {
			BROWSABLE = 0x8000000,
			CANCOPY = 1,
			CANDELETE = 0x20,
			CANLINK = 4,
			CANMONIKER = 0x400000,
			CANMOVE = 2,
			CANRENAME = 0x10,
			CAPABILITYMASK = 0x177,
			COMPRESSED = 0x4000000,
			CONTENTSMASK = 0x80000000,
			DISPLAYATTRMASK = 0xfc000,
			DROPTARGET = 0x100,
			ENCRYPTED = 0x2000,
			FILESYSANCESTOR = 0x10000000,
			FILESYSTEM = 0x40000000,
			FOLDER = 0x20000000,
			GHOSTED = 0x8000,
			HASPROPSHEET = 0x40,
			HASSTORAGE = 0x400000,
			HASSUBFOLDER = 0x80000000,
			HIDDEN = 0x80000,
			ISSLOW = 0x4000,
			LINK = 0x10000,
			NEWCONTENT = 0x200000,
			NONENUMERATED = 0x100000,
			READONLY = 0x40000,
			REMOVABLE = 0x2000000,
			SHARE = 0x20000,
			STORAGE = 8,
			STORAGEANCESTOR = 0x800000,
			STORAGECAPMASK = 0x70c50008,
			STREAM = 0x400000,
			VALIDATE = 0x1000000
		}

		[Flags]
		public enum SHGFI : uint {
			ADDOVERLAYS = 0x20,
			ATTR_SPECIFIED = 0x20000,
			ATTRIBUTES = 0x800,
			DISPLAYNAME = 0x200,
			EXETYPE = 0x2000,
			ICON = 0x100,
			ICONLOCATION = 0x1000,
			LARGEICON = 0,
			LINKOVERLAY = 0x8000,
			OPENICON = 2,
			OVERLAYINDEX = 0x40,
			PIDL = 8,
			SELECTED = 0x10000,
			SHELLICONSIZE = 4,
			SMALLICON = 1,
			SYSICONINDEX = 0x4000,
			TYPENAME = 0x400,
			USEFILEATTRIBUTES = 0x10
		}

		[Flags]
		public enum FILE_ATTRIBUTE {
			READONLY = 0x00000001,
			HIDDEN = 0x00000002,
			SYSTEM = 0x00000004,
			DIRECTORY = 0x00000010,
			ARCHIVE = 0x00000020,
			DEVICE = 0x00000040,
			NORMAL = 0x00000080,
			TEMPORARY = 0x00000100,
			SPARSE_FILE = 0x00000200,
			REPARSE_POINT = 0x00000400,
			COMPRESSED = 0x00000800,
			OFFLINE = 0x00001000,
			NOT_CONTENT_INDEXED = 0x00002000,
			ENCRYPTED = 0x00004000
		}
		#endregion

		#region Structures
		[StructLayout(LayoutKind.Sequential)]
		public struct OFSTRUCT {
			public byte cBytes;
			public byte fFixedDisc;
			public UInt16 nErrCode;
			public UInt16 Reserved1;
			public UInt16 Reserved2;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szPathName;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT {
			public int X;
			public int Y;

			public POINT(int x, int y) {
				this.X = x;
				this.Y = y;
			}
		};

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct SHFILEINFO {
			private const int MAX_PATH = 260;

			public IntPtr hIcon;
			public int iIcon;
			public SFGAO dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
			public string szTypeName;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct FILEGROUPDESCRIPTOR {
			public UInt32 cItems;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct FILEDESCRIPTOR {
			public UInt32 dwFlags;
			public readonly Guid clsid;
			public readonly Int32 sizelcx;
			public readonly Int32 sizelcy;
			public readonly Int32 pointlx;
			public readonly Int32 pointly;
			public readonly UInt32 dwFileAttributes;
			public FILETIME ftCreationTime;
			public readonly FILETIME ftLastAccessTime;
			public FILETIME ftLastWriteTime;
			public UInt32 nFileSizeHigh;
			public UInt32 nFileSizeLow;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
			public string cFileName;
		}
		#endregion

		#region Interfaces

		[ComImport]
		[Guid("00000121-0000-0000-C000-000000000046")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IDropSource {
			[PreserveSig]
			int QueryContinueDrag(int fEscapePressed, uint grfKeyState);

			[PreserveSig]
			int GiveFeedback(uint dwEffect);
		}

		#endregion

		#region Methods
		public static bool Succeeded(int hr) {
			return (0 <= hr);
		}
		#endregion
	}
}
