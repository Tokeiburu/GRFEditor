using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace GRFEditor {
	public class GRFEditorMain {
		private static Dictionary<string, Assembly> _preloadedAssemblies = new Dictionary<string, Assembly>();
		private static Dictionary<string, int> _assemblyValidation = new Dictionary<string, int> {
			//{ "ICSharpCode.AvalonEdit", 622592 },
			//{ "GRF", 2143744 },
			//{ "TokeiLibrary", 267776 },
			//{ "Be.Windows.Forms.HexBox", 86016 },
			//{ "zlib.net", 69632 },
			//{ "Utilities", 111616 },
			//{ "Gif.Components", 32768 },
			//{ "ColorPicker", 91648 },
			//{ "GrfMenuHandler64", 34304 },
			//{ "GrfMenuHandler32", 29184 },
			//{ "msvcp100", 421200 },
			//{ "msvcr100", 768848 },
			//{ "ActImaging", 20480 },
			//{ "Database", 59904 },
			//{ "Lua", 13824 },
			//{ "XDMessaging", 9728 },
			//{ "GrfToWpfBridge", 90112 },
			//{ "System.Threading", 387408 }
		};

		private static readonly string[] _registeredAssemblies = new string[] {
			"ErrorManager",
			"ICSharpCode.AvalonEdit",
			"GRF",
			"TokeiLibrary",
			"PaletteRecolorer",
			"Be.Windows.Forms.HexBox",
			"zlib.net",
			"Utilities",
			"cps",
			"Encryption",
			"Gif.Components",
			"ColorPicker",
			"GrfMenuHandler64",
			"GrfMenuHandler32",
			"msvcp100",
			"msvcr100",
			"ActImaging",
			"Database",
			"Lua",
			"XDMessaging",
			"ErrorManager",
			"GrfToWpfBridge",
			"System.Threading",
		};

		public static string ComputeHash(byte[] data) {
			using (MD5 md5 = new MD5CryptoServiceProvider()) {
				byte[] ba = md5.ComputeHash(data);
				StringBuilder sb = new StringBuilder(ba.Length * 2);

				foreach (byte b in ba) {
					sb.AppendFormat("{0:x2}", b);
				}

				return sb.ToString();
			}
		}

		[STAThread]
		public static void Main(string[] args) {
			//File.Delete("out.log");
			//
			//foreach (var assembly in _registeredAssemblies) {
			//	var resourceName = "GRFEditor.Files.Compressed." + assembly + ".dll";
			//
			//	using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
			//		if (stream != null) {
			//			byte[] assemblyData = new Byte[stream.Length];
			//			stream.Read(assemblyData, 0, assemblyData.Length);
			//			//var output = assemblyData;
			//			var output = Decompress(assemblyData);
			//
			//			File.AppendAllText("out.log", "MEM_" + resourceName + " = " + ComputeHash(output) + "\n");
			//
			//			if (File.Exists(assembly + ".dll")) {
			//				File.AppendAllText("out.log", "DIR_" + resourceName + " = " + ComputeHash(File.ReadAllBytes(assembly + ".dll")) + "\n");
			//			}
			//		}
			//	}
			//}

			AppDomain.CurrentDomain.AssemblyResolve += (sender, arguments) => {
				AssemblyName assemblyName = new AssemblyName(arguments.Name);
				
				if (_preloadedAssemblies.ContainsKey(assemblyName.Name))
					return _preloadedAssemblies[assemblyName.Name];
				
				if (assemblyName.Name.EndsWith(".resources"))
					return null;
				
				string resourceName = "GRFEditor.Files." + assemblyName.Name + ".dll";
				
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
					if (stream != null) {
						byte[] assemblyData = new Byte[stream.Length];
						stream.Read(assemblyData, 0, assemblyData.Length);
				
						if (_assemblyValidation.ContainsKey(assemblyName.Name)) {
							if (_assemblyValidation[assemblyName.Name] != assemblyData.Length) {
								MessageBox.Show("Assembly mismatch: " + assemblyName.Name);
							}
						}
				
						_preloadedAssemblies[assemblyName.Name] = Assembly.Load(assemblyData);
						return _preloadedAssemblies[assemblyName.Name];
					}
				}
				
				string compressedResourceName = "GRFEditor.Files.Compressed." + new AssemblyName(arguments.Name).Name + ".dll";
				
				using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(compressedResourceName)) {
					if (stream != null) {
						byte[] assemblyData = new Byte[stream.Length];
						stream.Read(assemblyData, 0, assemblyData.Length);
						assemblyData = Decompress(assemblyData);
				
						if (_assemblyValidation.ContainsKey(assemblyName.Name)) {
							if (_assemblyValidation[assemblyName.Name] != assemblyData.Length) {
								MessageBox.Show("Assembly mismatch: " + assemblyName.Name);
							}
						}
				
						_preloadedAssemblies[assemblyName.Name] = Assembly.Load(assemblyData);
						return _preloadedAssemblies[assemblyName.Name];
					}
				}
				
				if (_registeredAssemblies.ToList().Contains(assemblyName.Name)) {
					MessageBox.Show("Failed to load assembly : " + resourceName + "\r\n\r\nThe application will now shutdown.", "Assembly loader");
					Process.GetCurrentProcess().Kill();
				}

				return null;
			};

			Directory.SetCurrentDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));

			var app = new App();
			app.StartupUri = new Uri("EditorMainWindow.xaml", UriKind.Relative);
			//app.StartupUri = new Uri("TestWindow.xaml", UriKind.Relative);
			//app.StartupUri = new Uri("MapTest.xaml", UriKind.Relative);
			//app.StartupUri = new Uri("Basic.xaml", UriKind.Relative);
			//app.StartupUri = new Uri("WPF\\3DTests.xaml", UriKind.Relative);
			//app.StartupUri = new Uri("Tests.xaml", UriKind.Relative);
			app.Run();
		}

		public static byte[] Decompress(byte[] data) {
			using (MemoryStream memStream = new MemoryStream(data))
			using (GZipStream stream = new GZipStream(memStream, CompressionMode.Decompress)) {
				const int size = 4096;
				byte[] buffer = new byte[size];
				using (MemoryStream memory = new MemoryStream()) {
					int count;
					do {
						count = stream.Read(buffer, 0, size);
						if (count > 0) {
							memory.Write(buffer, 0, count);
						}
					} while (count > 0);
					return memory.ToArray();
				}
			}
		}

		public static byte[] Compress(byte[] data) {
			using (MemoryStream memory = new MemoryStream()) {
				using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true)) {
					gzip.Write(data, 0, data.Length);
				}
				return memory.ToArray();
			}
		}
	}
}