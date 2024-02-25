using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ErrorManager;
using GRF.ContainerFormat;
using GRF.IO;
using GRF.System;
using Utilities;

namespace GRF.Core.GrfCompression {
	/// <summary>
	/// Gravity's official zlib compression.
	/// </summary>
	public class CpsCompression : CustomCompression {
		/// <summary>
		/// Initializes a new instance of the <see cref="CpsCompression" /> class.
		/// </summary>
		public CpsCompression() {
			_init();
		}

		protected void _init() {
			string dllName = Wow.Is64BitProcess ? "comp_x64.dll" : "cps.dll";
			string outputPath = Path.Combine(Path.IsPathRooted(Settings.TempPath) ? GrfPath.GetDirectoryName(Settings.TempPath) : Settings.TempPath, dllName);
			Assembly currentAssembly = Assembly.GetAssembly(typeof(Compression));
			string[] names = currentAssembly.GetManifestResourceNames();
			string ResourceName = "Files." + dllName;
			byte[] cps = null;

			if (names.Any(p => p.EndsWith(ResourceName))) {
				Stream file = currentAssembly.GetManifestResourceStream(names.First(p => p.EndsWith(ResourceName)));
				if (file != null) {
					cps = new byte[file.Length];
					file.Read(cps, 0, (int) file.Length);
				}
			}

			if (cps == null)
				throw GrfExceptions.__CompressionDllFailed.Create(ResourceName);

			try {
				File.WriteAllBytes(outputPath, cps);
			}
			catch {
			}

			_hModule = NativeMethods.LoadLibrary(outputPath);

			if (_hModule == IntPtr.Zero) {
				if (Wow.Is64BitProcess) {
					ErrorHandler.HandleException(GrfExceptions.__CompressionDllFailed2.Display(ResourceName, "Microsoft Visual Studio C++ 2022 (x64) | downloading the x86 version will not be compatible\r\n\r\nLink: https://aka.ms/vs/17/release/vc_redist.x64.exe"));
				}
				else {
					ErrorHandler.HandleException(GrfExceptions.__CompressionDllFailed2.Display(ResourceName, "Microsoft Visual Studio C++ 2010 (x86) | downloading the x64 version will not be compatible"));
				}

				Success = false;
				return;
			}

			IntPtr intPtr = NativeMethods.GetProcAddress(_hModule, "uncompress");
			_decompress = (DecompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (DecompressMethod));

			intPtr = NativeMethods.GetProcAddress(_hModule, "zlib_compress");

			if (intPtr == IntPtr.Zero)
				intPtr = NativeMethods.GetProcAddress(_hModule, "compress");

			_compress = (CompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (CompressMethod));

			Success = true;
		}

		public override string ToString() {
			return GrfStrings.DisplayGravityDll;
		}
	}
}