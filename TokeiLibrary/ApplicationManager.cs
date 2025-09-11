using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Threading;
using ErrorManager;
using Microsoft.Win32;
using Utilities;

namespace TokeiLibrary {
	public static class ApplicationManager {
		private static int _latestErrorCount;
		private static bool _crashReportEnabled;
		private static DateTime _previousTime = DateTime.Now;
		private static readonly Dictionary<string, byte[]> _resources = new Dictionary<string, byte[]>();
		private static readonly Dictionary<string, BitmapSource> _preloadedResources = new Dictionary<string, BitmapSource>();

		static ApplicationManager() {
			DpiX = 96;
			DpiY = 96;

			//try {
			//	var dpiProperty = typeof(SystemParameters).GetProperty("DpiX", BindingFlags.NonPublic | BindingFlags.Static);
			//	DpiX = (int)dpiProperty.GetValue(null, null);
			//	DpiY = DpiX;
			//}
			//catch {
			//}
		}

		public static bool CrashReportEnabled {
			get {
				return _crashReportEnabled;
			}
			set {
				if (value) {
					AppDomain.CurrentDomain.UnhandledException += _currentDomain_UnhandledException;
					Application.Current.DispatcherUnhandledException += _current_DispatcherUnhandledException;
				}
				else {
					AppDomain.CurrentDomain.UnhandledException -= _currentDomain_UnhandledException;
					Application.Current.DispatcherUnhandledException -= _current_DispatcherUnhandledException;
				}
				_crashReportEnabled = value;
			}
		}
		public static void Shutdown() {
			try {
				Application.Current.Shutdown();
			}
			catch { }
			Process.GetCurrentProcess().Kill();
		}
		public static byte[] GetResource(string resource) {
			try {
				return _findResource(resource);
			}
			catch {
				//MessageBox.Show("Resource not found in the program's resources.\r\n\r\n" + resource);
			}

			return null;
		}
		public static List<byte[]> GetResources(string pattern) {
			try {
				return _findResources(pattern);
			}
			catch {
				//MessageBox.Show("Resource not found in the program's resources.\r\n\r\n" + resource);
			}

			return null;
		}
		public static byte[] GetResource(string resource, Assembly assembly) {
			try {
				return _findResource(resource, assembly);
			}
			catch {
				//MessageBox.Show("Resource not found in the program's resources.\r\n\r\n" + resource);
			}

			return null;
		}

		public static byte[] GetResource(string resource, bool searchInLocal) {
			try {
				if (searchInLocal) {
					string filename = Path.GetFileName(resource);

					if (File.Exists(resource))
						return File.ReadAllBytes(resource);
				}

				return _findResource(resource);
			}
			catch {
				//MessageBox.Show("Resource not found in the program's resources.\r\n\r\n" + resource);
			}

			return null;
		}

		private static byte[] _findResource(string resource, Assembly assembly = null) {
			try {
				if (_resources.Count > 10)
					_resources.Clear();

				if (_resources.ContainsKey(resource))
					return _resources[resource];
				else {
					foreach (Assembly currentAssembly in new Assembly[] { Assembly.GetCallingAssembly(), Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly(), assembly }) {
						if (currentAssembly == null) continue;

						string[] names = currentAssembly.GetManifestResourceNames();

						if (names.Any(p => p.EndsWith("." + resource))) {
							string name = names.First(p => p.EndsWith("." + resource));
							Stream file = currentAssembly.GetManifestResourceStream(name);

							if (file != null) {
								byte[] data = new byte[file.Length];
								file.Read(data, 0, (int)file.Length);

								if (name.EndsWith("Compressed." + resource) || name.Contains(".Compressed.")) {
									data = GZip.Decompress(data);
								}

								_resources.Add(resource, data);
								return data;
							}
						}
					}

					StreamResourceInfo stream = Application.GetResourceStream(new Uri(@"pack://application:,,,/Resources/" + resource, UriKind.RelativeOrAbsolute));

					if (stream != null) {
						byte[] resourceData = new byte[stream.Stream.Length];

						stream.Stream.Read(resourceData, 0, resourceData.Length);
						_resources.Add(resource, resourceData);
						return resourceData;
					}
				}
			}
			catch { }

			return null;
		}
		private static List<byte[]> _findResources(string pattern, Assembly assembly = null) {
			List<byte[]> resources = new List<byte[]>();

			try {
				if (_resources.Count > 10)
					_resources.Clear();

				foreach (Assembly currentAssembly in new Assembly[] {Assembly.GetCallingAssembly(), Assembly.GetEntryAssembly(), Assembly.GetExecutingAssembly(), assembly}) {
					if (currentAssembly == null) continue;

					string[] names = currentAssembly.GetManifestResourceNames();

					foreach (var name in names.Where(p => p.Contains(pattern))) {
						Stream file = currentAssembly.GetManifestResourceStream(name);

						if (file != null) {
							byte[] data = new byte[file.Length];
							file.Read(data, 0, (int)file.Length);

							if (name.Contains("Compressed.") || name.Contains(".Compressed.")) {
								data = GZip.Decompress(data);
							}

							resources.Add(data);
						}
					}
				}
			}
			catch { }

			return resources;
		}

		public static Func<string, BitmapFrame, BitmapSource> ImageProcessing;

		private static BitmapSource _imageProcessing(string name, BitmapFrame frame) {
			return frame;
		}

		public static BitmapSource GetResourceImage(string resource) {
			try {
				if (ImageProcessing == null) {
					ImageProcessing = _imageProcessing;
				}

				byte[] imageData = _findResource(resource);

				if (imageData == null)
					return null;

				using (MemoryStream file = new MemoryStream(imageData)) {
					try {
						if (resource.ToLower().EndsWith(".jpg")) {
							JpegBitmapDecoder decoder = new JpegBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
							return ImageProcessing(resource, decoder.Frames[0]);
						}
						if (resource.ToLower().EndsWith(".png")) {
							PngBitmapDecoder decoder = new PngBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
							return ImageProcessing(resource, decoder.Frames[0]);
						}
						if (resource.ToLower().EndsWith(".bmp")) {
							BmpBitmapDecoder decoder = new BmpBitmapDecoder(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
							return ImageProcessing(resource, decoder.Frames[0]);
						}
					}
					catch (Exception err) {
						ErrorHandler.HandleException(err);
					}
				}
			}
			catch {
				//MessageBox.Show("Resource not found in the program's resources.\r\n\r\n" + resource);
			}

			return null;
		}

		private static void _errorValidation() {
			if ((DateTime.Now - _previousTime).Seconds < 1) {
				_latestErrorCount++;
			}
			else {
				_latestErrorCount = 0;
			}

			_previousTime = DateTime.Now;

			if (_latestErrorCount >= 2) {
				MessageBox.Show("The error handling service has failed to report an issue and appears to be in a looping state. " +
								"The current exception has been logged in the file \"crash.log\". The program will now shutdown.");
				Shutdown();
			}
		}

		private static void _currentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			_errorValidation();

			try {
				string crash = "\r\n\r\n\r\n" +
							   PrettyLine(DateTime.Now.ToString(CultureInfo.InvariantCulture)) + "\r\n" +
							   PrettyLine(8, "Loaded assemblies : ") + "\r\n" +
							   _writeLoadedAssemblies() + "\r\n";

				crash += ErrorHandler.GenerateOutput(e.ExceptionObject as Exception);

				try {
					if (Configuration.WriteExceptionsInCurrentFolder) {
						File.AppendAllText("crash.log", crash);
					}

					File.AppendAllText(Path.Combine(Configuration.ProgramDataPath, "crash.log"), crash);
				}
				catch (Exception err) {
					MessageBox.Show("Failed to write to crash.log.\r\n\r\n" + err.Message);
				}
			}
			catch { }

			ErrorHandler.HandleException("The application has thrown an exception.\r\n\r\nThe application will most likely crash.", e.ExceptionObject as Exception, ErrorLevel.Critical);
		}
		private static void _current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			_errorValidation();

			try {
				string crash = "\r\n\r\n\r\n" +
							   PrettyLine(DateTime.Now.ToString(CultureInfo.InvariantCulture)) + "\r\n" +
							   PrettyLine(8, "Loaded assemblies : ") + "\r\n" +
							   _writeLoadedAssemblies() + "\r\n";

				crash += ErrorHandler.GenerateOutput(e.Exception);

				try {
					if (Configuration.WriteExceptionsInCurrentFolder) {
						File.AppendAllText("crash.log", crash);
					}

					File.AppendAllText(Path.Combine(Configuration.ProgramDataPath, "crash.log"), crash);
				}
				catch (Exception err) {
					MessageBox.Show("Failed to write to crash.log.\r\n\r\n" + err.Message);
				}
			}
			catch { }

			ErrorHandler.HandleException("The main UI thread has thrown an exception.\r\n\r\n" + (e.Exception != null ? e.Exception.Message : ""), e.Exception, ErrorLevel.Critical);
			e.Handled = true;
		}

		private static string _writeLoadedAssemblies() {
			try {
				StringBuilder builder = new StringBuilder();
				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
					builder.Append(_generateString(12, ' ') + assembly.GetName().FullName + "\r\n");
				}
				return builder.ToString();
			}
			catch {
				return "Failed to load assemblies.";
			}
		}
		public static string PrettyLine(int indent, string toString) {
			try {
				return _generateString(indent, ' ') + toString;
			}
			catch {
				return toString;
			}
		}
		public static string PrettyLine(string toString) {
			try {
				const string LineHeader = "--------------";
				const int LineLength = 55;
				int spaceRemaining = LineLength - toString.Length - (2 * LineHeader.Length);
				string toReturn = "";

				if (spaceRemaining >= 0) {
					toReturn += LineHeader;
					toReturn += _generateString(spaceRemaining / 2, ' ');
					toReturn += toString;
					toReturn += _generateString((spaceRemaining + 1) / 2, ' ');
					toReturn += LineHeader;
				}
				else {
					return LineHeader + "    " + toString + "    " + LineHeader;
				}

				return toReturn;
			}
			catch {
				return toString;
			}
		}
		private static string _generateString(int count, char c) {
			StringBuilder builder = new StringBuilder(count);

			for (int i = 0; i < count; i++)
				builder.Append(c);

			return builder.ToString();
		}

		public static void AddContextMenu(string application, string extension, params string[] keys) {
			using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension)) {
				if (key != null)
					key.SetValue(null, application + extension);
			}

			using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(application + extension + "\\shell\\CascadeMenu")) {
				if (key != null) {
					if (keys.Length % 2 != 0)
						throw new Exception("Invalid number of parameters : keys");

					for (int i = 0; i < keys.Length; i++) {
						key.SetValue(keys[i], keys[i + 1]);
						i++;
					}
				}
			}

			RefreshExplorer();
		}

		public static void RemoveContextMenu(string application, string extension) {
			Registry.ClassesRoot.DeleteSubKeyTree(application + extension);

			RefreshExplorer();
		}

		public static void AddContextMenuRegistry(string application, string extension, IEnumerable<string>[] listKeys) {
			using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension)) {
				if (key != null)
					key.SetValue(null, application + extension);
			}

			foreach (string[] keys in listKeys) {
				List<string> extractedKeys = keys.ToList();

				using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(application + extension + "\\" + extractedKeys[0])) {
					if (key != null) {
						if (extractedKeys.Count % 2 != 1)
							throw new Exception("Invalid number of parameters : keys");

						for (int i = 1; i < extractedKeys.Count; i++) {
							key.SetValue(extractedKeys[i], extractedKeys[i + 1]);
							i++;
						}
					}
				}
			}

			RefreshExplorer();
		}

		public static BitmapSource PreloadResourceImage(string image) {
			if (_preloadedResources.Count > 30)
				_preloadedResources.Clear();

			if (!_preloadedResources.ContainsKey(image)) {
				var frame = GetResourceImage(image);
				_preloadedResources[image] = WpfImaging.FixDPI(frame, DpiX, DpiY);
			}

			return _preloadedResources[image];
		}

		#region File association

		public static double DpiX { get; set; }
		public static double DpiY { get; set; }

		/// <summary>
		/// Flags that indicate the meaning of the <i>dwItem1</i> and <i>dwItem2</i> parameters. 
		/// The uFlags parameter must be one of the following values.
		/// </summary>
		[Flags]
		public enum HChangeNotifyFlags {
			/// <summary>
			/// The <i>dwItem1</i> and <i>dwItem2</i> parameters are DWORD values. 
			/// </summary>
			SHCNF_DWORD = 0x0003,
			/// <summary>
			/// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of ITEMIDLIST structures that 
			/// represent the item(s) affected by the change. 
			/// Each ITEMIDLIST must be relative to the desktop folder. 
			/// </summary>
			SHCNF_IDLIST = 0x0000,
			/// <summary>
			/// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of 
			/// maximum length MAX_PATH that contain the full path names 
			/// of the items affected by the change. 
			/// </summary>
			SHCNF_PATHA = 0x0001,
			/// <summary>
			/// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings of 
			/// maximum length MAX_PATH that contain the full path names 
			/// of the items affected by the change. 
			/// </summary>
			SHCNF_PATHW = 0x0005,
			/// <summary>
			/// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that 
			/// represent the friendly names of the printer(s) affected by the change. 
			/// </summary>
			SHCNF_PRINTERA = 0x0002,
			/// <summary>
			/// <i>dwItem1</i> and <i>dwItem2</i> are the addresses of null-terminated strings that 
			/// represent the friendly names of the printer(s) affected by the change. 
			/// </summary>
			SHCNF_PRINTERW = 0x0006,
			/// <summary>
			/// The function should not return until the notification 
			/// has been delivered to all affected components. 
			/// As this flag modifies other data-type flags, it cannot by used by itself.
			/// </summary>
			SHCNF_FLUSH = 0x1000,
			/// <summary>
			/// The function should begin delivering notifications to all affected components 
			/// but should return as soon as the notification process has begun. 
			/// As this flag modifies other data-type flags, it cannot by used by itself.
			/// </summary>
			SHCNF_FLUSHNOWAIT = 0x2000
		}

		[DllImport("shell32.dll")]
		static extern void SHChangeNotify(HChangeNotifyEventID wEventId,
		                                  HChangeNotifyFlags uFlags,
		                                  IntPtr dwItem1,
		                                  IntPtr dwItem2);

		public static void AddExtension(string application, string fileName, string extension, bool openWithGrfEditor, string iconPath = null) {
			//if (Methods.IsWin10OrHigher()) {
			//	
			//}
			//else 
			if (Methods.IsWinVistaOrHigher()) {
				// ReSharper disable PossibleNullReferenceException
				string program = Path.GetFileName(application);
				string programId = program + extension;

				RegistryKey file = Registry.CurrentUser.CreateSubKey("Software\\Classes\\" + extension);
				RegistryKey app = Registry.CurrentUser.CreateSubKey("Software\\Classes\\Applications\\" + programId);
				RegistryKey link = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + extension);

				file.CreateSubKey("DefaultIcon").SetValue("", Path.Combine(Configuration.ProgramDataPath, extension.Substring(1, extension.Length - 1)) + ".ico");
				file.CreateSubKey("PerceivedType").SetValue("", fileName);

				if (openWithGrfEditor) {
					app.CreateSubKey("shell\\open\\command").SetValue("", "\"" + application + "\" \"%1\"");
					link.CreateSubKey("UserChoice").SetValue("Progid", "Applications\\" + programId);
					app.CreateSubKey("DefaultIcon").SetValue(null, iconPath ?? Path.Combine(Configuration.ProgramDataPath, extension.Substring(1, extension.Length - 1)) + ".ico");
				}

				try {
					byte[] data = GetResource(extension.Substring(1, extension.Length - 1) + ".ico");
					File.WriteAllBytes(Path.Combine(Configuration.ProgramDataPath, extension.Substring(1, extension.Length - 1) + ".ico"), data);
				}
				catch {
					throw new Exception("Couldn't find the resource : " + extension.Substring(1, extension.Length - 1) + ".ico");
				}
			}
			else if (Methods.IsWinXPOrHigher()) {
				application = "grfeditor";

				using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(extension)) {
					if (key != null)
						key.SetValue(null, application + extension);
				}

				using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(application + extension)) {
					if (key != null)
						key.SetValue(null, fileName);
				}

				using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(application + extension + "\\DefaultIcon")) {
					if (key != null)
						key.SetValue(null, iconPath ?? Path.Combine(Methods.ApplicationPath, extension.Substring(1, extension.Length - 1)) + ".ico");
				}

				if (openWithGrfEditor) {
					Registry.ClassesRoot.CreateSubKey(application + extension + "\\shell");
					Registry.ClassesRoot.CreateSubKey(application + extension + "\\shell\\open");

					using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(application + extension + "\\shell\\open\\command")) {
						if (key != null)
							key.SetValue(null, "\"" + Process.GetCurrentProcess().MainModule.FileName + "\" \"%1\"");
					}
				}

				try {
					byte[] data = GetResource(extension.Substring(1, extension.Length - 1) + ".ico");
					File.WriteAllBytes(Path.Combine(Configuration.ProgramDataPath, extension.Substring(1, extension.Length - 1) + ".ico"), data);
				}
				catch {
					throw new Exception("Couldn't find the resource : " + extension.Substring(1, extension.Length - 1) + ".ico");
				}
			}

			RefreshExplorer();
			// ReSharper restore PossibleNullReferenceException
		}

		public static void RemoveExtension(string application, string extension) {
			if (Methods.IsWinVistaOrHigher()) {
				string program = Path.GetFileName(application);
				string programId = program + extension;

				try {
					try { Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\" + extension); } catch { }
					try { Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\Applications\\" + programId); } catch { }
					try { Registry.CurrentUser.DeleteSubKeyTree("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FileExts\\" + extension); } catch { }
				}
				catch { }

				RefreshExplorer();
			}
			else if (Methods.IsWinXPOrHigher()) {
				application = "grfeditor";

				try {
					try { Registry.ClassesRoot.DeleteSubKeyTree(application + extension); } catch { }
					try { Registry.ClassesRoot.DeleteSubKeyTree(extension); } catch { }
				}
				catch { }

				RefreshExplorer();
			}
		}

		public static void RefreshExplorer() {
			SHChangeNotify(HChangeNotifyEventID.SHCNE_ASSOCCHANGED, HChangeNotifyFlags.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
		}

		#region enum HChangeNotifyEventID
		/// <summary>
		/// Describes the event that has occurred. 
		/// Typically, only one event is specified at a time. 
		/// If more than one event is specified, the values contained 
		/// in the <i>dwItem1</i> and <i>dwItem2</i> 
		/// parameters must be the same, respectively, for all specified events. 
		/// This parameter can be one or more of the following values. 
		/// </summary>
		/// <remarks>
		/// <para><b>Windows NT/2000/XP:</b> <i>dwItem2</i> contains the index 
		/// in the system image list that has changed. 
		/// <i>dwItem1</i> is not used and should be <see langword="null"/>.</para>
		/// <para><b>Windows 95/98:</b> <i>dwItem1</i> contains the index 
		/// in the system image list that has changed. 
		/// <i>dwItem2</i> is not used and should be <see langword="null"/>.</para>
		/// </remarks>
		[Flags]
		public enum HChangeNotifyEventID {
			/// <summary>
			/// All events have occurred. 
			/// </summary>
			SHCNE_ALLEVENTS = 0x7FFFFFFF,

			/// <summary>
			/// A file type association has changed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> 
			/// must be specified in the <i>uFlags</i> parameter. 
			/// <i>dwItem1</i> and <i>dwItem2</i> are not used and must be <see langword="null"/>. 
			/// </summary>
			SHCNE_ASSOCCHANGED = 0x08000000,

			/// <summary>
			/// The attributes of an item or folder have changed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the item or folder that has changed. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>.
			/// </summary>
			SHCNE_ATTRIBUTES = 0x00000800,

			/// <summary>
			/// A nonfolder item has been created. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the item that was created. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>.
			/// </summary>
			SHCNE_CREATE = 0x00000002,

			/// <summary>
			/// A nonfolder item has been deleted. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the item that was deleted. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_DELETE = 0x00000004,

			/// <summary>
			/// A drive has been added. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the root of the drive that was added. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_DRIVEADD = 0x00000100,

			/// <summary>
			/// A drive has been added and the Shell should create a new window for the drive. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the root of the drive that was added. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_DRIVEADDGUI = 0x00010000,

			/// <summary>
			/// A drive has been removed. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the root of the drive that was removed.
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_DRIVEREMOVED = 0x00000080,

			/// <summary>
			/// Not currently used. 
			/// </summary>
			SHCNE_EXTENDED_EVENT = 0x04000000,

			/// <summary>
			/// The amount of free space on a drive has changed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the root of the drive on which the free space changed.
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_FREESPACE = 0x00040000,

			/// <summary>
			/// Storage media has been inserted into a drive. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the root of the drive that contains the new media. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_MEDIAINSERTED = 0x00000020,

			/// <summary>
			/// Storage media has been removed from a drive. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the root of the drive from which the media was removed. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_MEDIAREMOVED = 0x00000040,

			/// <summary>
			/// A folder has been created. <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> 
			/// or <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the folder that was created. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_MKDIR = 0x00000008,

			/// <summary>
			/// A folder on the local computer is being shared via the network. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the folder that is being shared. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_NETSHARE = 0x00000200,

			/// <summary>
			/// A folder on the local computer is no longer being shared via the network. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the folder that is no longer being shared. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_NETUNSHARE = 0x00000400,

			/// <summary>
			/// The name of a folder has changed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the previous pointer to an item identifier list (PIDL) or name of the folder. 
			/// <i>dwItem2</i> contains the new PIDL or name of the folder. 
			/// </summary>
			SHCNE_RENAMEFOLDER = 0x00020000,

			/// <summary>
			/// The name of a nonfolder item has changed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the previous PIDL or name of the item. 
			/// <i>dwItem2</i> contains the new PIDL or name of the item. 
			/// </summary>
			SHCNE_RENAMEITEM = 0x00000001,

			/// <summary>
			/// A folder has been removed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the folder that was removed. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_RMDIR = 0x00000010,

			/// <summary>
			/// The computer has disconnected from a server. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the server from which the computer was disconnected. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// </summary>
			SHCNE_SERVERDISCONNECT = 0x00004000,

			/// <summary>
			/// The contents of an existing folder have changed, 
			/// but the folder still exists and has not been renamed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_IDLIST"/> or 
			/// <see cref="HChangeNotifyFlags.SHCNF_PATH"/> must be specified in <i>uFlags</i>. 
			/// <i>dwItem1</i> contains the folder that has changed. 
			/// <i>dwItem2</i> is not used and should be <see langword="null"/>. 
			/// If a folder has been created, deleted, or renamed, use SHCNE_MKDIR, SHCNE_RMDIR, or 
			/// SHCNE_RENAMEFOLDER, respectively, instead. 
			/// </summary>
			SHCNE_UPDATEDIR = 0x00001000,

			/// <summary>
			/// An image in the system image list has changed. 
			/// <see cref="HChangeNotifyFlags.SHCNF_DWORD"/> must be specified in <i>uFlags</i>. 
			/// </summary>
			SHCNE_UPDATEIMAGE = 0x00008000,

		}
		#endregion // enum HChangeNotifyEventID

		#endregion

		public static event Action ThemeChanged;

		public static void OnThemeChanged() {
			Action handler = ThemeChanged;
			if (handler != null) handler();
		}
	}
}
