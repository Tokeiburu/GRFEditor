using GRF.Core;
using GRF.IO;
using System.Text;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs.Controls {

	public class PreviewTextLoader {
		public class LoadResult {
			public bool Success;
			public bool IsReadOnly;
			public string TextOutput;
			public string Syntax;
			public Encoding EditorEncoding;
		}

		public LoadResult Load(FileEntry entry) {
			LoadResult result = new LoadResult();
			result.Success = true;

			switch (entry.RelativePath.GetExtension()) {
				case ".bat":
				case ".scp":
				case ".ezv":
				case ".ase":
				case ".txt":
				case ".tsv":
				case ".xml":
				case ".layout":
				case ".font":
				case ".imageset":
					result.Syntax = "XML";
					break;
				case ".json":
					result.Syntax = "Json";
					result.EditorEncoding = EncodingService.Utf8;
					break;
				case ".lua":
					result.Syntax = "Lua";
					break;
				case ".csv":
					result.Syntax = "XML";
					result.EditorEncoding = EncodingService.Utf8;
					break;
				case ".integrity":
					//result.IsReadOnly = true;
					break;
			}

			if (entry.IsEmpty()) {
				result.TextOutput = "";
			}
			else {
				switch (entry.RelativePath.GetExtension()) {
					case ".json":
						result.TextOutput = EncodingService.Utf8.GetString(entry.GetDecompressedData());
						break;
					case ".csv":
						result.TextOutput = B64.Decode(EncodingService.Ansi.GetString(entry.GetDecompressedData()));
						break;
					default:
						result.TextOutput = EncodingService.DisplayEncoding.GetString(entry.GetDecompressedData());
						break;
				}
			}

			return result;
		}
	}
}
