using System;
using System.IO;
using ErrorManager;
using TokeiLibrary;
using Utilities.Extension;

namespace GRFEditor.Tools.GrfValidation {
	public class ValidationView {
		public ValidationView(Utilities.Extension.Tuple<ValidationTypes, string, string> info) {
			try {
				OriginalData = info;

				ValidationType = info.Item1.ToString();
				RelativePath = info.Item2;
				DisplayRelativePath = Path.GetFileName(RelativePath);

				if (info.Item1 == ValidationTypes.FeDuplicatePaths) {
					DisplayRelativePath = RelativePath;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException("part1", err);
			}

			Description = info.Item3;

			switch (info.Item1) {
				case ValidationTypes.FeSvn:
				case ValidationTypes.FeSpaceSaved:
				case ValidationTypes.FeDb:
				case ValidationTypes.VeComputeHash:
				case ValidationTypes.FeEmptyFiles:
					DataImage = ApplicationManager.PreloadResourceImage("help.png");
					break;
				case ValidationTypes.VcLoadEntries:
				case ValidationTypes.VcUnknown:
				case ValidationTypes.VcDecompressEntries:
				case ValidationTypes.VcInvalidEntryMetadata:
				case ValidationTypes.VcSpriteSoundIndex:
				case ValidationTypes.VcSpriteSoundMissing:
				case ValidationTypes.VcZlibChecksum:
				case ValidationTypes.FeInvalidFileTable:
				case ValidationTypes.FeNoExtension:
				case ValidationTypes.FeRootFiles:
					DataImage = ApplicationManager.PreloadResourceImage("error16.png");
					break;
				default:
					DataImage = ApplicationManager.PreloadResourceImage("warning16.png");
					break;
			}
		}

		public string RelativePath { get; set; }
		public string DisplayRelativePath { get; set; }
		public string ValidationType { get; set; }
		public object DataImage { get; set; }

		public Utilities.Extension.Tuple<ValidationTypes, string, string> OriginalData { get; private set; }

		public string Description { get; set; }

		public string ToolTipRelativePath {
			get { return RelativePath; }
		}

		public string ToolTipDescription {
			get { return Description; }
		}

		public bool Default {
			get { return true; }
		}

		public override string ToString() {
			return ValidationType + "\t" + RelativePath + "\t" + Description;
		}
	}
}