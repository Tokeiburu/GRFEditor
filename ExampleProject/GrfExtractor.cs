using System.Collections.Generic;
using System.IO;
using System.Linq;
using GRF.Core;
using GRF.Threading;

namespace ExampleProject {
	/// <summary>
	/// A class is needed to extract files because we want an IProgress object for updates.
	/// </summary>
	public class GrfExtractor : IProgress {
		public void Extract(GrfHolder grf, string destination, List<string> grfFilesToCopy) {
			List<string> shellFilesDestination = grfFilesToCopy.Select(p => Path.Combine(destination, p)).ToList();
			List<string> folders = shellFilesDestination.Select(Path.GetDirectoryName).Distinct().ToList();

			foreach (string folder in folders) {
				if (!Directory.Exists(folder))
					Directory.CreateDirectory(folder);
			}

			// This is a 'slow' way of extracting files, I'd suggest using GRFThreadStreamCopyFiles's class instead
			// There's an example of the usage of this class in GRFEditor.Core.Services.ExtractingService.cs or
			// a simplified one in GrfCL.GrfCL.cs
			for (int index = 0; index < grfFilesToCopy.Count; index++) {
				string file = grfFilesToCopy[index];
				File.WriteAllBytes(shellFilesDestination[index], grf.FileTable[file].GetDecompressedData());

				Progress = (float) (index + 1) / grfFilesToCopy.Count * 100f;
				if (IsCancelling) {
					IsCancelled = true;
					break;
				}
			}
		}

		public float Progress { get; set; }
		public bool IsCancelling { get; set; }
		public bool IsCancelled { get; set; }
		public void CancelOperation() {
			IsCancelling = true;
		}
	}
}
