using System;
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
				if (stream == null) {
					throw new Exception("Unable to load the following resourse : " + resourceName);
				}

				byte[] assemblyData = new Byte[stream.Length];
				stream.Read(assemblyData, 0, assemblyData.Length);
				return Assembly.Load(assemblyData);
			}
		}
	}
}
