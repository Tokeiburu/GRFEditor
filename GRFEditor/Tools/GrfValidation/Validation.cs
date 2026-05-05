namespace GRFEditor.Tools.GrfValidation {
	public static class ValidationStrings {
		public const string FeNoExtension = "File name has no extension.";
		public const string FeInvalidFileTable = "Invalid file alignment (lower than compressed size).";
		public const string FeMissingSprAct = "Complementing Spr file missing.";
		public const string FeMissingSprAct2 = "Spr file is not used.";
		public const string FeEmptyFiles = "Empty file (size = 0), remove it.";
		public const string FeDb = "Hidden database file for thumbnails, remove it.";
		public const string FeSvn = "Subversion file (usually hidden), remove it.";
		public const string FeRootFiles = "This file is at the root of the container, this is not recommended.";
		public const string FeSpaceSaved = "You can save {0} by saving this GRF.";
		public const string FeInvalidFileTable2 = "Invalid file alignment (misaligned bytes: {0}).";
		public const string FeDuplicateFiles = "This file has been found {0} times.";
		public const string FeDuplicatePaths = "This path has been found {0} times.";
	}

	public enum ValidationTypes {
		FeNoExtension,
		FeMissingSprAct,
		FeEmptyFiles,
		FeDb,
		FeSvn,
		FeDuplicateFiles,
		FeDuplicatePaths,
		FeSpaceSaved,
		FeInvalidFileTable,
		FeRootFiles,

		VcDecompressEntries,
		VcZlibChecksum,
		VcLoadEntries,
		VcInvalidEntryMetadata,
		VcSpriteIssues,
		VcSpriteIssuesRle,
		VcResourcesModelFiles,
		VcResourcesMapFiles,
		VcInvalidQuadTree,
		VcSpriteSoundIndex,
		VcSpriteIndex,
		VcSpriteSoundMissing,
		VcInvalidImageFormat,
		VcUnknown,

		VeFileNotFound,
		VeDifferentHashValue,
		VeComputeHash,
		VeFilesDifferentSize,
	}
}