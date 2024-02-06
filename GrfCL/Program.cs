using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace GrfCL {
	class Program {
		[STAThread]
		static void Main(string[] args) {
			AppDomain.CurrentDomain.AssemblyResolve += _loadEmbeddedResources;
			GrfCL.Run();
		}

		private static Assembly _loadEmbeddedResources(object sender, ResolveEventArgs args) {
			string resourceName = "GrfCL.Files." + new AssemblyName(args.Name).Name + ".dll";

			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
				if (stream != null) {
					byte[] assemblyData = new Byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);
					return Assembly.Load(assemblyData);
				}
			}

			string compressedResourceName = "GrfCL.Files.Compressed." + new AssemblyName(args.Name).Name + ".dll";

			using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(compressedResourceName)) {
				if (stream != null) {
					byte[] assemblyData = new Byte[stream.Length];
					stream.Read(assemblyData, 0, assemblyData.Length);
					assemblyData = Decompress(assemblyData);
					return Assembly.Load(assemblyData);
				}
			}

			throw new Exception("Unable to load the following resourse : " + resourceName);
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
	}
}
