using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using ErrorManager;
using GRF.Core;
using GRF.Image;
using GRF.Image.Decoders;
using GRF.GrfSystem;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using GRFEditor.Core.Services;
using GrfToWpfBridge;
using GrfToWpfBridge.Application;
using TokeiLibrary;
using TokeiLibrary.WPF;
using TokeiLibrary.WPF.Styles;
using Utilities;
using Utilities.CommandLine;
using Utilities.Extension;
using Utilities.Services;
using AsyncOperation = GrfToWpfBridge.Application.AsyncOperation;

namespace GRFEditor.WPF {
	/// <summary>
	/// Interaction logic for MultiProgressWindow.xaml
	/// </summary>
	public partial class MultiProgressWindow : TkWindow, IProgress, IDisposable {
		private readonly MultiInitParameters _multiInit = new MultiInitParameters();
		private readonly GenericCLOption _option;
		private AsyncOperation _asyncOperation;
		private ExtractingService _extractingService;
		private string _filePath = "";
		private GrfHolder _grf;

		public MultiProgressWindow(GenericCLOption option, string title = "GRF Editor")
			: base(title, "help.ico", SizeToContent.Height, ResizeMode.CanMinimize) {
			_option = option;
			InitializeComponent();
			ShowInTaskbar = true;
			WindowStartupLocation = WindowStartupLocation.CenterScreen;

			_setup();
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		#endregion

		#region IProgress Members

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }

		#endregion

		protected override void OnClosing(CancelEventArgs e) {
			//if (_asyncOperation.IsRunning)
			_asyncOperation.Cancel();

			_button.IsEnabled = false;
			e.Cancel = true;

			new Thread(new ThreadStart(
				           delegate {
					           Thread.Sleep(500);
					           ApplicationManager.Shutdown();
					           e.Cancel = false;
					           base.OnClosing(e);
				           }
				           )).Start();
		}

		private void _setup() {
			string extension = null;
			GrfEditorConfiguration.CpuPerformanceManagement = true;
			_multiInit.ActivateNoMainWindow = true;

			if (_option.Args.Count > 0)
				_filePath = _option.Args[1];

			_grf = new GrfHolder();

			switch (_option.Args[0]) {
				case "imageConvert":
					Title = "Image convert...";
					_multiInit.IsPrivateAsync = true;
					_multiInit.PrimaryMethod = () => ImageProvider.GetImage(File.ReadAllBytes(_filePath), _filePath.GetExtension()).SaveTo(_filePath, PathRequest.ExtractSetting);
					_tbUpdate.Text = "Converting image " + Path.GetFileName(_filePath) + "...";
					break;
				case "extractGrfFromContainerPath":
					_grf.Open(_filePath);
					Title = "Extraction...";
					_multiInit.ActivateExtractingService = true;
					_multiInit.PrimaryMethod = () => _extractingService.ExtractAll(_grf, Path.GetDirectoryName(_filePath));
					_tbUpdate.Text = "Extracting " + Path.GetFileName(_filePath) + "...";
					break;
				case "extractGrfUsingFileName":
					_grf.Open(_filePath);
					Title = "Extraction...";
					_multiInit.ActivateExtractingService = true;
					_multiInit.PrimaryMethod = () => _extractingService.ExtractAll(_grf, Path.Combine(Path.GetDirectoryName(_filePath), Path.GetFileNameWithoutExtension(_filePath)));
					_tbUpdate.Text = "Extracting " + Path.GetFileName(_filePath) + "...";
					break;
				case "extractGrfTo":
					Title = "Extraction...";
					string path = PathRequest.FolderExtract();

					if (path != null) {
						_grf.Open(_filePath);
						_multiInit.ActivateExtractingService = true;
						_multiInit.PrimaryMethod = () => _extractingService.ExtractAll(_grf, path);
						_tbUpdate.Text = "Extracting " + Path.GetFileName(_filePath) + "...";
					}
					else {
						_multiInit.OperationAborted = true;
					}

					break;
				case "changeFolderToKorean":
					Title = "To Korean...";
					_multiInit.IsPrivateAsync = true;
					_multiInit.PrimaryMethod = () => _convert(EncodingService.Korean);
					_tbUpdate.Text = "Converting to 'Korean' (949)...";
					break;
				case "changeFolderToANSI":
					Title = "To ANSI...";
					_multiInit.IsPrivateAsync = true;
					_multiInit.PrimaryMethod = () => _convert(EncodingService.Ansi);
					_tbUpdate.Text = "Converting to 'ANSI' (1252)...";
					break;
				case "openWithGrfEditor":
					Title = "Launching GRF Editor...";
					_multiInit.IsPrivateAsync = true;
					_multiInit.PrimaryMethod = () => {
						ProcessStartInfo info = new ProcessStartInfo();
						info.Arguments = "\"" + _option.Args[1] + "\"";
						info.WorkingDirectory = Methods.ApplicationPath;
						info.FileName = Methods.ApplicationFullPath;
						Process.Start(info);

						ApplicationManager.Shutdown();
					};

					_tbUpdate.Text = "Launching GRF Editor...";
					break;
				case "openWithGrfCL":
					Title = "Launching GrfCL...";

					if (!File.Exists(Path.Combine(Methods.ApplicationPath, "GrfCL.exe"))) {
						_multiInit.Exception = "GrfCL couldn't be located : " + Path.Combine(Methods.ApplicationPath, "GrfCL.exe");
						_multiInit.OperationAborted = true;
					}
					else {
						_multiInit.IsPrivateAsync = true;
						_multiInit.PrimaryMethod = () => {
							ProcessStartInfo info = new ProcessStartInfo();
							//-log false
							info.Arguments = "-open \"" + _option.Args[1] + "\" -sM";
							info.WorkingDirectory = Methods.ApplicationPath;
							info.FileName = Path.Combine(Methods.ApplicationPath, "GrfCL.exe");
							Process.Start(info);

							ApplicationManager.Shutdown();
						};

						_tbUpdate.Text = "Launching GrfCL...";
					}
					break;
				case "compressToGpf":
				case "compressToRgz":
				case "compressToGrf":
				case "compressTo":
					Title = "Compressing folder to container file";

					switch (_option.Args[0]) {
						case "compressToGpf":
							extension = ".gpf";
							break;
						case "compressToRgz":
							extension = ".rgz";
							break;
						case "compressToGrf":
							extension = ".grf";
							break;
					}

					extension = extension ?? ".grf";
					string grfName;

					if (_option.Args[0] == "compressTo") {
						grfName = PathRequest.SaveFileEditor("filter", "Container Files|*.grf;*.gpf;*.rgz|GRF|*.grf|GPF|*.gpf|RGZ|*.rgz");

						if (grfName == null) {
							_multiInit.OperationAborted = true;
						}
						else {
							_grf.New(grfName);
						}
					}
					else {
						grfName = Path.Combine(Path.GetDirectoryName(_option.Args[1]), Path.GetFileNameWithoutExtension(_option.Args[1]) + extension);

						if (File.Exists(grfName)) {
							MessageBoxResult result = WindowProvider.ShowDialog("A file with the same name has been found (" + grfName + ") would you like to merge the files or change the name?", "File name already exists", MessageBoxButton.YesNoCancel, "Merge", "New file...");

							if (result == MessageBoxResult.Cancel) {
								_multiInit.OperationAborted = true;
							}
							else if (result == MessageBoxResult.Yes) {
								_grf.Open(grfName);
							}
							else if (result == MessageBoxResult.No) {
								grfName = PathRequest.SaveFileEditor("filter", "Container Files|*.grf;*.gpf;*.rgz|GRF|*.grf|GPF|*.gpf|RGZ|*.rgz");

								if (grfName == null) {
									_multiInit.OperationAborted = true;
								}
								else {
									_grf.New(grfName);
								}
							}
						}
						else {
							_grf.New(grfName);
						}
					}

					if (!_multiInit.OperationAborted) {
						_multiInit.IsPrivateAsync = true;
						_multiInit.PrimaryMethod = () => {
							_grf.Commands.AddFilesInDirectory("", _option.Args[1]);
							string temp = TemporaryFilesManager.GetTemporaryFilePath("container_{0:0000}" + _grf.FileName.GetExtension());
							string oldFileName = _grf.FileName;
							_grf.Save(temp, SyncMode.Asynchronous);

							while (_grf.IsBusy) {
								Progress = _grf.Progress >= 100f ? 99.99f : _grf.Progress;
							}

							try {
								_grf.Close();

								if (File.Exists(oldFileName)) {
									File.Delete(oldFileName);
								}

								File.Move(temp, oldFileName);
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);

								try {
									File.Exists(temp);
								}
								catch {
								}
							}

							Progress = 100f;
						};
						_tbUpdate.Text = "Saving container...";
					}
					break;
				case "imageCut12":
				case "imageCut12First":
				case "imageCut12Second":
					string fileName = _option.Args[1];

					try {
						GrfImage imageSource = ImageProvider.GetImage(File.ReadAllBytes(fileName), fileName.GetExtension());

						if (imageSource.Width != 1024 || imageSource.Height != 768) {
							if (WindowProvider.ShowDialog("The image dimensions aren't of the expected format (1024x768).\r\n\r\nWidth = " + imageSource.Width +
							                              "\r\nHeight = " + imageSource.Height + "\r\n\r\nDo you want to cut the image anyway?", "Invalid dimensions", MessageBoxButton.YesNoCancel) != MessageBoxResult.Yes) {
								_multiInit.OperationAborted = true;
							}
						}

						string pathDestination = "";

						switch (_option.Args[0]) {
							case "imageCut12":
								pathDestination = Path.GetFileNameWithoutExtension(fileName) + "{0}-{1}.bmp";
								break;
							case "imageCut12First":
								pathDestination = "t_¹è°æ{0}-{1}.bmp";
								break;
							case "imageCut12Second":
								pathDestination = "t2_¹è°æ{0}-{1}.bmp";
								break;
						}

						if (!_multiInit.OperationAborted) {
							_multiInit.IsPrivateAsync = true;
							_multiInit.PrimaryMethod = () => {
								imageSource.Convert(new Bgr24FormatConverter());

								for (int y = 0; y < 3; y++) {
									for (int x = 0; x < 4; x++) {
										GrfImage temp = imageSource.Extract(256 * x, 256 * y, 256, 256);
										temp.Save(Path.Combine(
											Path.GetDirectoryName(fileName),
											String.Format(pathDestination, y + 1, x + 1)));
										//WpfImaging.SaveImage(temp.Cast<BitmapSource>(), , PixelFormats.Bgr24);
										Progress = (4f * y + x + 1) / 12f * 100f;
									}
								}
							};
						}
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
						_multiInit.OperationAborted = true;
					}
					break;
				default:
					_multiInit.Exception = "Command unrecognized : " + _option.Args[0];
					_multiInit.OperationAborted = true;
					break;
			}

			_internalSetup();
		}

		private void _convert(Encoding encoding) {
			string headPath = Path.GetDirectoryName(_filePath);

			if (headPath == null)
				return;

			// 1) We start by converting the directories
			List<string> directories = Directory.GetDirectories(_filePath, "*", SearchOption.AllDirectories).Select(p => p.ReplaceFirst(headPath + "\\", "")).ToList();
			directories.Add(_filePath.ReplaceFirst(headPath + "\\", ""));
			directories = directories.OrderByDescending(p => p).ToList();

			EncodingService.DisplayEncoding = encoding;
			List<string> files = Directory.GetFiles(_filePath, "*", SearchOption.AllDirectories).ToList();

			for (int i = 0; i < files.Count; i++) {
				string oldFileName = files[i];
				string newFileName = Path.Combine(headPath, EncodingService.FromAnyToDisplayEncoding(files[i].ReplaceFirst(headPath + "\\", "")));

				try {
					if (oldFileName != newFileName) {
						if (!Directory.Exists(Path.GetDirectoryName(newFileName))) {
							Directory.CreateDirectory(Path.GetDirectoryName(newFileName));
						}

						if (File.Exists(newFileName))
							File.Delete(newFileName);

						File.Move(oldFileName, newFileName);
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
					return;
				}

				Progress = (i) / (float) files.Count * 100f;
			}

			for (int index = 0; index < directories.Count; index++) {
				string oldDirectoryName = Path.Combine(headPath, directories[index]);
				string newDirectoryName = Path.Combine(headPath, EncodingService.FromAnyToDisplayEncoding(directories[index]));

				try {
					if (oldDirectoryName != newDirectoryName) {
						if (Directory.GetFiles(oldDirectoryName, "*", SearchOption.AllDirectories).Length == 0)
							Directory.Delete(oldDirectoryName);
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
					return;
				}
			}

			Progress = 100f;
		}

		private void _finalizeProgress() {
			Progress = 100f;
		}

		private void _initProgress() {
			Progress = -1f;
			IsCancelling = false;
			IsCancelled = false;
		}

		private void _internalSetup() {
			if (_multiInit.Exception != null) {
				ErrorHandler.HandleException(_multiInit.Exception, ErrorLevel.Warning);
			}

			if (_multiInit.OperationAborted)
				ApplicationManager.Shutdown();

			if (_grf.IsOpened) {
				if (_grf.Header.FoundErrors) {
					ErrorHandler.HandleException("Couldn't load the container properly. The current command cannot execute.");
					ApplicationManager.Shutdown();
				}
			}

			_asyncOperation = new AsyncOperation(_progress);

			if (_multiInit.ActivateExtractingService) {
				_extractingService = new ExtractingService(_asyncOperation);
			}

			if (_multiInit.StartMethod != null) {
				_multiInit.StartMethod();
			}

			if (_multiInit.PrimaryMethod != null) {
				_asyncOperation.Finished += _ => this.Dispatch(p => p.Close());
				_asyncOperation.Cancelling += _ => this.Dispatch(p => p._button.IsEnabled = false);

				if (_multiInit.IsPrivateAsync) {
					_asyncOperation.SetAndRunOperation(new GrfThread(() => _async(_multiInit.PrimaryMethod), this, 200, null, true, true));
				}
				else {
					_multiInit.PrimaryMethod();
				}
			}
		}

		private void _async(Action func) {
			try {
				_initProgress();
				func();
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				_finalizeProgress();
			}
		}

		private void _buttonClose2(object sender, RoutedEventArgs e) {
			Close();
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_grf != null)
					_grf.Dispose();
			}
		}

		#region Nested type: MultiInitParameters

		public class MultiInitParameters {
			public Action PrimaryMethod;
			public Action StartMethod;
			public string Exception { get; set; }
			public bool ActivateExtractingService { get; set; }

			public bool ActivateNoMainWindow {
				set {
					if (value)
						ErrorHandler.SetErrorHandler(new DefaultErrorHandler { IgnoreNoMainWindow = true });
				}
			}

			public bool IsPrivateAsync { get; set; }
			public bool OperationAborted { get; set; }
		}

		#endregion
	}
}