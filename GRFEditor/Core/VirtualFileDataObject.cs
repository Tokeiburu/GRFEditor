using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using ErrorManager;
using GRF.Core;
using GRF.Image;
using GRF.System;
using GRF.Threading;
using GrfToWpfBridge;
using TokeiLibrary;
using Utilities;
using Utilities.Extension;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace GRFEditor.Core {
	public class VirtualFileDataObjectProgress : IProgress {
		public int ItemsProcessed = 0;
		public int ItemsToProcess = 0;

		public VirtualFileDataObject Vfo;
		public bool Finished { get; set; }

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		public void Update() {
			Progress = -1;

			try {
				while (Progress < 100 && !Finished) {
					Thread.Sleep(200);

					Progress = (ItemsProcessed - 1) / (float) ItemsToProcess * 100f;

					if (IsCancelling) {
						Vfo.Cancel = true;
						IsCancelled = true;
						return;
					}
				}
			}
			finally {
				Progress = 100f;
			}
		}
	}

	public sealed class VirtualFileDataObject : IDataObject, IAsyncOperation {
		private static readonly short FILECONTENTS = (short) (DataFormats.GetDataFormat(NativeMethods.CFSTR_FILECONTENTS).Id);
		private static readonly short FILEDESCRIPTORW = (short) (DataFormats.GetDataFormat(NativeMethods.CFSTR_FILEDESCRIPTORW).Id);
		private static readonly short FILESOURCE = (short) (DataFormats.GetDataFormat(DataFormats.StringFormat).Id);
		private static readonly short PASTESUCCEEDED = (short) (DataFormats.GetDataFormat(NativeMethods.CFSTR_PASTESUCCEEDED).Id);
		private static readonly short PERFORMEDDROPEFFECT = (short) (DataFormats.GetDataFormat(NativeMethods.CFSTR_PERFORMEDDROPEFFECT).Id);
		private static readonly short PREFERREDDROPEFFECT = (short) (DataFormats.GetDataFormat(NativeMethods.CFSTR_PREFERREDDROPEFFECT).Id);

		private readonly List<DataObject> _dataObjects = new List<DataObject>();
		private readonly Action<VirtualFileDataObject> _endAction;
		private readonly Action<VirtualFileDataObject> _startAction;
		private bool _inOperation;

		public VirtualFileDataObject() {
			IsAsynchronous = true;
		}

		public VirtualFileDataObject(Action<VirtualFileDataObject> startAction, Action<VirtualFileDataObject> endAction)
			: this() {
			_startAction = startAction;
			_endAction = endAction;
		}

		public bool Cancel { get; set; }
		public DragAndDropSource Source { get; set; }
		public bool IsAsynchronous { get; set; }
		public string SelectedPath { get; set; }

		#region IAsyncOperation Members

		void IAsyncOperation.SetAsyncMode(int fDoOpAsync) {
			IsAsynchronous = NativeMethods.VARIANT_FALSE != fDoOpAsync;
		}

		void IAsyncOperation.GetAsyncMode(out int pfIsOpAsync) {
			pfIsOpAsync = IsAsynchronous ? NativeMethods.VARIANT_TRUE : NativeMethods.VARIANT_FALSE;
		}

		void IAsyncOperation.StartOperation(IBindCtx pbcReserved) {
			_inOperation = true;
			if (null != _startAction) {
				_startAction(this);
			}
		}

		void IAsyncOperation.InOperation(out int pfInAsyncOp) {
			pfInAsyncOp = _inOperation ? NativeMethods.VARIANT_TRUE : NativeMethods.VARIANT_FALSE;
		}

		void IAsyncOperation.EndOperation(int hResult, IBindCtx pbcReserved, uint dwEffects) {
			if (null != _endAction) {
				_endAction(this);
			}
			_inOperation = false;
		}

		#endregion

		#region IDataObject Members

		int IDataObject.DAdvise(ref FORMATETC pFormatetc, ADVF advf, IAdviseSink adviseSink, out int connection) {
			Marshal.ThrowExceptionForHR(NativeMethods.OLE_E_ADVISENOTSUPPORTED);
			throw new NotImplementedException();
		}

		void IDataObject.DUnadvise(int connection) {
			Marshal.ThrowExceptionForHR(NativeMethods.OLE_E_ADVISENOTSUPPORTED);
			throw new NotImplementedException();
		}

		int IDataObject.EnumDAdvise(out IEnumSTATDATA enumAdvise) {
			Marshal.ThrowExceptionForHR(NativeMethods.OLE_E_ADVISENOTSUPPORTED);
			throw new NotImplementedException();
		}

		IEnumFORMATETC IDataObject.EnumFormatEtc(DATADIR direction) {
			if (direction == DATADIR.DATADIR_GET) {
				if (0 == _dataObjects.Count) {
					// Note: SHCreateStdEnumFmtEtc fails for a count of 0; throw helpful exception
					throw new InvalidOperationException("VirtualFileDataObject requires at least one data object to enumerate.");
				}

				// Create enumerator and return it
				IEnumFORMATETC enumerator;
				if (
					NativeMethods.Succeeded(NativeMethods.SHCreateStdEnumFmtEtc((uint) (_dataObjects.Count),
					                                                            _dataObjects.Select(d => d.FORMATETC).ToArray(),
					                                                            out enumerator))) {
					return enumerator;
				}

				// Returning null here can cause an AV in the caller; throw instead
				Marshal.ThrowExceptionForHR(NativeMethods.E_FAIL);
			}
			throw new NotImplementedException();
		}

		int IDataObject.GetCanonicalFormatEtc(ref FORMATETC formatIn, out FORMATETC formatOut) {
			throw new NotImplementedException();
		}

		void IDataObject.GetData(ref FORMATETC format, out STGMEDIUM medium) {
			medium = new STGMEDIUM();
			var hr = ((IDataObject) this).QueryGetData(ref format);
			if (NativeMethods.Succeeded(hr)) {
				// Find the best match
				var formatCopy = format;
				// Cannot use ref or out parameter inside an anonymous method, lambda expression, or query expression
				var dataObject = _dataObjects.FirstOrDefault(d => (d.FORMATETC.cfFormat == formatCopy.cfFormat) &&
				                                                  (d.FORMATETC.dwAspect == formatCopy.dwAspect) &&
				                                                  (0 != (d.FORMATETC.tymed & formatCopy.tymed) &&
				                                                   (d.FORMATETC.lindex == formatCopy.lindex)));
				if (dataObject != null && !Cancel) {
					if (!IsAsynchronous && (FILEDESCRIPTORW == dataObject.FORMATETC.cfFormat) && !_inOperation) {
						// Enter the operation and call the start action
						_inOperation = true;
						if (null != _startAction) {
							_startAction(this);
						}
					}

					// Populate the STGMEDIUM
					medium.tymed = dataObject.FORMATETC.tymed;
					var result = dataObject.GetData(); // Possible call to user code
					hr = result.Item2;
					if (NativeMethods.Succeeded(hr)) {
						medium.unionmember = result.Item1;
					}
				}
				else {
					// Couldn't find a match
					hr = NativeMethods.DV_E_FORMATETC;
				}
			}
			if (!NativeMethods.Succeeded(hr)) // Not redundant; hr gets updated in the block above
			{
				Marshal.ThrowExceptionForHR(hr);
			}
		}

		void IDataObject.GetDataHere(ref FORMATETC format, ref STGMEDIUM medium) {
			throw new NotImplementedException();
		}

		int IDataObject.QueryGetData(ref FORMATETC format) {
			var formatCopy = format;
			// Cannot use ref or out parameter inside an anonymous method, lambda expression, or query expression
			var formatMatches = _dataObjects.Where(d => d.FORMATETC.cfFormat == formatCopy.cfFormat).ToList();
			if (!formatMatches.Any()) {
				return NativeMethods.DV_E_FORMATETC;
			}
			var tymedMatches = formatMatches.Where(d => 0 != (d.FORMATETC.tymed & formatCopy.tymed)).ToList();
			if (!tymedMatches.Any()) {
				return NativeMethods.DV_E_TYMED;
			}
			var aspectMatches = tymedMatches.Where(d => d.FORMATETC.dwAspect == formatCopy.dwAspect);
			if (!aspectMatches.Any()) {
				return NativeMethods.DV_E_DVASPECT;
			}
			return NativeMethods.S_OK;
		}

		void IDataObject.SetData(ref FORMATETC formatIn, ref STGMEDIUM medium, bool release) {
			var handled = false;
			if ((formatIn.dwAspect == DVASPECT.DVASPECT_CONTENT) &&
			    (formatIn.tymed == TYMED.TYMED_HGLOBAL) &&
			    (medium.tymed == formatIn.tymed)) {
				// Supported format; capture the data
				var ptr = NativeMethods.GlobalLock(medium.unionmember);
				if (IntPtr.Zero != ptr) {
					try {
						var length = NativeMethods.GlobalSize(ptr).ToInt32();
						var data = new byte[length];
						Marshal.Copy(ptr, data, 0, length);
						// Store it in our own format
						SetData(formatIn.cfFormat, data);
						handled = true;
					}
					finally {
						NativeMethods.GlobalUnlock(medium.unionmember);
					}
				}

				// Release memory if we now own it
				if (release) {
					Marshal.FreeHGlobal(medium.unionmember);
				}
			}

			// Handle synchronous mode
			if (!IsAsynchronous && (PERFORMEDDROPEFFECT == formatIn.cfFormat) && _inOperation) {
				// Call the end action and exit the operation
				if (null != _endAction) {
					_endAction(this);
				}
				_inOperation = false;
			}

			// Throw if unhandled
			if (!handled) {
				throw new NotImplementedException();
			}
		}

		#endregion

		public void SetData(short dataFormat, IEnumerable<byte> data) {
			_dataObjects.Add(
				new DataObject {
					FORMATETC = new FORMATETC {
						cfFormat = dataFormat,
						ptd = IntPtr.Zero,
						dwAspect = DVASPECT.DVASPECT_CONTENT,
						lindex = -1,
						tymed = TYMED.TYMED_HGLOBAL
					},
					GetData = () => {
						var dataArray = data.ToArray();
						var ptr = Marshal.AllocHGlobal(dataArray.Length);
						Marshal.Copy(dataArray, 0, ptr, dataArray.Length);
						return new Utilities.Extension.Tuple<IntPtr, int>(ptr, NativeMethods.S_OK);
					},
				});
		}

		public void SetData(short dataFormat, int index, FileDescriptor descriptor) {
			_dataObjects.Add(
				new DataObject {
					FORMATETC = new FORMATETC {
						cfFormat = dataFormat,
						ptd = IntPtr.Zero,
						dwAspect = DVASPECT.DVASPECT_CONTENT,
						lindex = index,
						tymed = TYMED.TYMED_ISTREAM
					},
					GetData = () => {
						// Create IStream for data
						var iStream = NativeMethods.CreateStreamOnHGlobal(IntPtr.Zero, true);
						if (descriptor.StreamContents != null) {
							// Wrap in a .NET-friendly Stream and call provided code to fill it
							using (var stream = new IStreamWrapper(iStream)) {
								descriptor.StreamContents(descriptor.GrfData, descriptor.FilePath, stream, descriptor.Argument);
							}
						}
						// Return an IntPtr for the IStream
						IntPtr ptr = Marshal.GetComInterfaceForObject(iStream, typeof (IStream));
						Marshal.ReleaseComObject(iStream);
						return new Utilities.Extension.Tuple<IntPtr, int>(ptr, NativeMethods.S_OK);
					},
				});
		}

		public void SetData(List<FileDescriptor> fileDescriptors) {
			// Prepare buffer
			var bytes = new List<byte>();
			// Add FILEGROUPDESCRIPTOR header
			bytes.AddRange(StructureBytes(new NativeMethods.FILEGROUPDESCRIPTOR { cItems = (uint) (fileDescriptors.Count()) }));
			// Add n FILEDESCRIPTORs
			foreach (var fileDescriptor in fileDescriptors) {
				// Set required fields
				var FILEDESCRIPTOR = new NativeMethods.FILEDESCRIPTOR {
					cFileName = fileDescriptor.Name,
				};
				// Set optional timestamp
				if (fileDescriptor.ChangeTimeUtc.HasValue) {
					FILEDESCRIPTOR.dwFlags |= NativeMethods.FD_CREATETIME | NativeMethods.FD_WRITESTIME;
					var changeTime = fileDescriptor.ChangeTimeUtc.Value.ToLocalTime().ToFileTime();
					var changeTimeFileTime = new FILETIME {
						dwLowDateTime =
							(int) (changeTime & 0xffffffff),
						dwHighDateTime =
							(int) (changeTime >> 32),
					};
					FILEDESCRIPTOR.ftLastWriteTime = changeTimeFileTime;
					FILEDESCRIPTOR.ftCreationTime = changeTimeFileTime;
				}
				// Set optional length
				if (fileDescriptor.Length.HasValue) {
					FILEDESCRIPTOR.dwFlags |= NativeMethods.FD_FILESIZE;
					FILEDESCRIPTOR.nFileSizeLow = (uint) (fileDescriptor.Length & 0xffffffff);
					FILEDESCRIPTOR.nFileSizeHigh = (uint) (fileDescriptor.Length >> 32);
				}
				// Add structure to buffer
				bytes.AddRange(StructureBytes(FILEDESCRIPTOR));
			}

			// Set CFSTR_FILEDESCRIPTORW
			switch (Source) {
				case DragAndDropSource.ListView:
					SetData(FILESOURCE, Encoding.ASCII.GetBytes("ListView\0"));
					break;
				case DragAndDropSource.ListViewSearch:
					SetData(FILESOURCE, Encoding.ASCII.GetBytes("ListViewSearch\0"));
					break;
				case DragAndDropSource.TreeView:
					SetData(FILESOURCE, Encoding.ASCII.GetBytes("TreeView\0"));
					break;
				case DragAndDropSource.SpriteEditor:
					SetData(FILESOURCE, Encoding.ASCII.GetBytes("SpriteEditor\0"));
					break;
				case DragAndDropSource.ResourceExtractor:
					SetData(FILESOURCE, Encoding.ASCII.GetBytes("ResourceExtractor\0"));
					break;
				case DragAndDropSource.Other:
					SetData(FILESOURCE, Encoding.ASCII.GetBytes("Other\0"));
					break;
			}

			SetData(FILEDESCRIPTORW, bytes);
			// Set n CFSTR_FILECONTENTS
			var index = 0;
			foreach (var fileDescriptor in fileDescriptors) {
				SetData(FILECONTENTS, index, fileDescriptor);
				index++;
			}
		}

		private static IEnumerable<byte> StructureBytes(object source) {
			// Set up for call to StructureToPtr
			var size = Marshal.SizeOf(source.GetType());
			var ptr = Marshal.AllocHGlobal(size);
			var bytes = new byte[size];
			try {
				Marshal.StructureToPtr(source, ptr, false);
				// Copy marshalled bytes to buffer
				Marshal.Copy(ptr, bytes, 0, size);
			}
			finally {
				Marshal.FreeHGlobal(ptr);
			}
			return bytes;
		}

		public static DragDropEffects DoDragDrop(DependencyObject dragSource, IDataObject dataObject, DragDropEffects allowedEffects) {
			int[] finalEffect = new int[1];
			try {
				NativeMethods.DoDragDrop(dataObject, new DropSource(), (int) allowedEffects, finalEffect);
			}
			finally {
				var virtualFileDataObject = dataObject as VirtualFileDataObject;
				if ((null != virtualFileDataObject) && !virtualFileDataObject.IsAsynchronous && virtualFileDataObject._inOperation) {
					// Call the end action and exit the operation
					if (null != virtualFileDataObject._endAction) {
						virtualFileDataObject._endAction(virtualFileDataObject);
					}
					virtualFileDataObject._inOperation = false;
				}
			}
			return (DragDropEffects) (finalEffect[0]);
		}

		public static void SetDraggable(Image imagePreview, GrfImageWrapper wrapper) {
			imagePreview.Dispatch(delegate {
				imagePreview.MouseMove += (e, a) => {
					if (a.LeftButton == MouseButtonState.Pressed) {
						VirtualFileDataObject virtualFileDataObject = new VirtualFileDataObject();

						string name = (string) imagePreview.Tag;

						List<FileDescriptor> descriptors = new List<FileDescriptor> {
							new FileDescriptor {
								Name = name + (name.GetExtension() == null ? Imaging.GuessExtension(((BitmapSource) imagePreview.Source).Format) : ""),
								Argument = wrapper,
								StreamContents = (grfData, filePath, stream, argument) => {
									GrfImageWrapper image = (GrfImageWrapper) argument;

									if (image.Image != null) {
										GrfImage grfImage = image.Image;

										string outputPath = TemporaryFilesManager.GetTemporaryFilePath("image_{0:0000}") + Imaging.GuessExtension(grfImage.GrfImageType);
										grfImage.Save(outputPath);

										var data = File.ReadAllBytes(outputPath);
										stream.Write(data, 0, data.Length);

										image.Image.Save(outputPath);
									}
								}
							}
						};

						virtualFileDataObject.Source = DragAndDropSource.Other;
						virtualFileDataObject.SetData(descriptors);

						try {
							DoDragDrop(null, virtualFileDataObject, DragDropEffects.Copy);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}
				};
			});
		}

		#region Nested type: DataObject

		private class DataObject {
			public FORMATETC FORMATETC { get; set; }

			public Func<Utilities.Extension.Tuple<IntPtr, int>> GetData { get; set; }
		}

		#endregion

		#region Nested type: DropSource

		private class DropSource : NativeMethods.IDropSource {
			#region IDropSource Members

			public int QueryContinueDrag(int fEscapePressed, uint grfKeyState) {
				var escapePressed = (0 != fEscapePressed);
				var keyStates = (DragDropKeyStates) grfKeyState;
				if (escapePressed) {
					return NativeMethods.DRAGDROP_S_CANCEL;
				}
				else if (DragDropKeyStates.None == (keyStates & DragDropKeyStates.LeftMouseButton)) {
					return NativeMethods.DRAGDROP_S_DROP;
				}
				return NativeMethods.S_OK;
			}

			public int GiveFeedback(uint dwEffect) {
				return NativeMethods.DRAGDROP_S_USEDEFAULTCURSORS;
			}

			#endregion
		}

		#endregion

		#region Nested type: FileDescriptor

		public class FileDescriptor {
			public string Name { get; set; }

			public Int64? Length { get; set; }

			public DateTime? ChangeTimeUtc { get; set; }

			public Action<GrfHolder, string, Stream, object> StreamContents { get; set; }

			public GrfHolder GrfData { get; set; }
			public string FilePath { get; set; }
			public object Argument { get; set; }
		}

		#endregion

		#region Nested type: IStreamWrapper

		private class IStreamWrapper : Stream {
			private readonly IStream _iStream;

			public IStreamWrapper(IStream iStream) {
				_iStream = iStream;
			}

			public override bool CanRead {
				get { return false; }
			}

			public override bool CanSeek {
				get { return false; }
			}

			public override bool CanWrite {
				get { return true; }
			}

			public override long Length {
				get { throw new NotImplementedException(); }
			}

			public override long Position {
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public override void Flush() {
				throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count) {
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin) {
				throw new NotImplementedException();
			}

			public override void SetLength(long value) {
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count) {
				_iStream.Write(offset == 0 ? buffer : buffer.Skip(offset).ToArray(), count, IntPtr.Zero);
			}
		}

		#endregion
	}

	internal class DragInfo {
		public string Path { get; set; }
	}

	public enum DragAndDropSource {
		ListView,
		ListViewSearch,
		TreeView,
		SpriteEditor,
		ResourceExtractor,
		Other,
	}

	[ComImport]
	[Guid("3D8B0590-F691-11d2-8EA9-006097DF5BD4")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	internal interface IAsyncOperation {
		void SetAsyncMode([In] Int32 fDoOpAsync);
		void GetAsyncMode([Out] out Int32 pfIsOpAsync);
		void StartOperation([In] IBindCtx pbcReserved);
		void InOperation([Out] out Int32 pfInAsyncOp);
		void EndOperation([In] Int32 hResult, [In] IBindCtx pbcReserved, [In] UInt32 dwEffects);
	}
}