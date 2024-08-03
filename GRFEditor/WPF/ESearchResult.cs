using System.Collections.Generic;
using GRF.Core;
using GRF.IO;
using GrfToWpfBridge;
using ICSharpCode.AvalonEdit.Search;

namespace GRFEditor.WPF {
	public class ESearchResult {
		public string FileName { get; set; }
		public string FullPath { get; set; }
		public string FileType { get; set; }
		public FileEntry Entry { get; set; }
		public string Path {
			get { return GrfPath.GetDirectoryName(FullPath); }
		}
		public int NumberOfMatches {
			get { return Results.Count; }
		}

		public object DataImage {
			get {
				if (Entry != null) {
					if (Entry.DataImage == null)
						Entry.DataImage = IconProvider.GetSmallIcon(Entry.RelativePath);

					return Entry.DataImage;
				}

				return IconProvider.GetSmallIcon(FileName);
			}
		}

		public List<ISearchResult> Results { get; set; }

		public bool Normal {
			get { return true; }
		}

		public TextSource Source { get; set; }
	}
}