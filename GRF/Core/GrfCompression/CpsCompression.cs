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
			string outputPath = Path.Combine(GrfPath.GetDirectoryName(Settings.TempPath), "Resources.cps.dll");

			Assembly currentAssembly = Assembly.GetAssembly(typeof (Compression));

			string[] names = currentAssembly.GetManifestResourceNames();
			const string ResourceName = "Files.cps.dll";
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

			if (_hModule == 0) {
				ErrorHandler.HandleException(GrfExceptions.__CompressionDllFailed2.Display(ResourceName, "Microsoft Visual Studio C++ 2010 (x86) | downloading the x64 version will not be compatible"));
				Success = false;
				return;
			}

			IntPtr intPtr = NativeMethods.GetProcAddress(_hModule, "uncompress");
			_decompress = (DecompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (DecompressMethod));

			intPtr = NativeMethods.GetProcAddress(_hModule, "compress");
			_compress = (CompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (CompressMethod));

			Success = true;
		}

		public override string ToString() {
			return GrfStrings.DisplayGravityDll;
		}
	}
}