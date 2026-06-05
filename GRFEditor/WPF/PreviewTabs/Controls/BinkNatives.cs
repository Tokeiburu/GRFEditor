using ErrorManager;
using GRF.Image;
using GRF.IO;
using GRFEditor.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using TokeiLibrary;
using Utilities;

namespace GRFEditor.WPF.PreviewTabs.Controls {
	public class Bink : IDisposable {
		private IntPtr _binkPtr;
		public int Width;
		public int Height;
		public int FrameCount;
		public int CurrentFrame = -1;
		public double Fps;
		private GrfImage _image;
		private GCHandle _pinnedArray;
		private IntPtr _pixels;

		public Bink(string name) {
			_binkPtr = BinkNatives.BinkOpen(name, 0);

			if (_binkPtr == IntPtr.Zero)
				throw new Exception("Unable to load the BIK file '" + name + "'.");

			Width = Marshal.ReadInt32(_binkPtr, 0);
			Height = Marshal.ReadInt32(_binkPtr, 4);
			FrameCount = Marshal.ReadInt32(_binkPtr, 8);

			int frameRate = Marshal.ReadInt32(_binkPtr, 20);
			int frameRateDiv = Marshal.ReadInt32(_binkPtr, 24);
			Fps = (double)frameRate / frameRateDiv;

			_image = new GrfImage(new byte[4 * Width * Height], Width, Height, GrfImageType.Bgr32);

			_pinnedArray = GCHandle.Alloc(_image.Pixels, GCHandleType.Pinned);
			_pixels = _pinnedArray.AddrOfPinnedObject();
			CurrentFrame = -1;
		}

		public GrfImage GetImage(int index) {
			if (index < 0 || index >= FrameCount) {
				return null;
			}

			BinkNatives.BinkGoto(_binkPtr, (uint)index, 0);
			_getImage();
			CurrentFrame = index;
			return _image;
		}

		private void _getImage() {
			BinkNatives.BinkDoFrame(_binkPtr);
			BinkNatives.BinkCopyToBuffer(_binkPtr, _pixels, (uint)(Width * 4), (uint)Height, 0, 0, 3);
		}

		public GrfImage GetNextFrame(out int index) {
			if (CurrentFrame == -1) {
				index = 0;
				return GetImage(index);
			}

			BinkNatives.BinkNextFrame(_binkPtr);
			CurrentFrame++;
			CurrentFrame = CurrentFrame % FrameCount;
			index = CurrentFrame;

			_getImage();
			return _image;
		}

		public void Dispose() {
			if (_binkPtr != IntPtr.Zero) {
				BinkNatives.BinkClose(_binkPtr);
				_binkPtr = IntPtr.Zero;
			}

			if (_pinnedArray.IsAllocated) {
				_pinnedArray.Free();
			}
		}
	}

	public static class BinkNatives {
		private static IntPtr _hModule = IntPtr.Zero;

		static BinkNatives() {
			var binkDllPath = GrfPath.Combine(GrfEditorConfiguration.ProgramDataPath, "binkw64.dll");

			if (!File.Exists(binkDllPath)) {
				File.WriteAllBytes(binkDllPath, ApplicationManager.GetResource("binkw64.dll"));
			}

			_hModule = NativeMethods.LoadLibrary(binkDllPath);

			if (_hModule == IntPtr.Zero) {
				return;
			}

			BinkOpen = LoadFunction<BinkOpenDelegate>("BinkOpen");
			BinkDoFrame = LoadFunction<BinkDoFrameDelegate>("BinkDoFrame");
			BinkNextFrame = LoadFunction<BinkNextFrameDelegate>("BinkNextFrame");
			BinkClose = LoadFunction<BinkCloseDelegate>("BinkClose");
			BinkCopyToBuffer = LoadFunction<BinkCopyToBufferDelegate>("BinkCopyToBuffer");
			BinkGoto = LoadFunction<BinkGotoDelegate>("BinkGoto");
		}

		private static T LoadFunction<T>(string name) {
			IntPtr pFunc = NativeMethods.GetProcAddress(_hModule, name);

			if (pFunc == IntPtr.Zero) {
				throw new Exception($"Failed to find function export: {name}");
			}

			return Marshal.GetDelegateForFunctionPointer<T>(pFunc);
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		public delegate IntPtr BinkOpenDelegate([MarshalAs(UnmanagedType.LPStr)] string name, uint flags);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int BinkDoFrameDelegate(IntPtr bink);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void BinkNextFrameDelegate(IntPtr bink);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void BinkCloseDelegate(IntPtr bink);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int BinkCopyToBufferDelegate(IntPtr bink, IntPtr destBuffer, uint destPitch, uint destHeight, uint destX, uint destY, uint copyFlags);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void BinkGotoDelegate(IntPtr bink, uint frameNumber, uint flags);

		
		public static BinkOpenDelegate BinkOpen;
		public static BinkDoFrameDelegate BinkDoFrame;
		public static BinkNextFrameDelegate BinkNextFrame;
		public static BinkCloseDelegate BinkClose;
		public static BinkCopyToBufferDelegate BinkCopyToBuffer;
		public static BinkGotoDelegate BinkGoto;
	}
}
