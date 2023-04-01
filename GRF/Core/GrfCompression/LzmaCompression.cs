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
	/// Curiosity's compression!
	/// </summary>
	public class LzmaCompression : CustomCompression {
		/// <summary>
		/// Initializes a new instance of the <see cref="LzmaCompression" /> class.
		/// </summary>
		public LzmaCompression() {
			_init();
		}

		protected void _init() {
			string outputPath = Path.Combine(GrfPath.GetDirectoryName(Settings.TempPath), "Resources.lzma.dll");

			Assembly currentAssembly = Assembly.GetAssembly(typeof (Compression));

			string[] names = currentAssembly.GetManifestResourceNames();
			const string ResourceName = "Files.lzma.dll";
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
				ErrorHandler.HandleException(GrfExceptions.__CompressionDllFailed.Display(ResourceName));
				Success = false;
				return;
			}

			IntPtr intPtr = NativeMethods.GetProcAddress(_hModule, "uncompress");
			_decompress = (DecompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (DecompressMethod));

			intPtr = NativeMethods.GetProcAddress(_hModule, "compress2");

			if (intPtr == IntPtr.Zero)
				intPtr = NativeMethods.GetProcAddress(_hModule, "compress");

			_compress = (CompressMethod) Marshal.GetDelegateForFunctionPointer(intPtr, typeof (CompressMethod));

			Success = true;
		}

		public override string ToString() {
			return GrfStrings.DisplayLzmaDll;
		}
	}
}