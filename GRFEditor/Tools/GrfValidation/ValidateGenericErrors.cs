using ErrorManager;
using GRF.Core;
using GRF.Threading;
using GRFEditor.ApplicationConfiguration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.Tools.GrfValidation {
	public class ValidateGenericErrors {
		public void FindErrors(ProgressObject prog, ValidateResult result, GrfHolder container) {
			try {
				List<KeyValuePair<string, FileEntry>> entries = container.FileTable.FastAccessEntries;

				if (GrfEditorConfiguration.FeSpaceSaved)
					_findErrorsSpaceSaved(entries, result);

				if (GrfEditorConfiguration.FeInvalidFileTable)
					_findErrorsInvalidFileTable(entries, container, result);

				if (GrfEditorConfiguration.FeNoExtension)
					_findErrorsNoExtension(entries, result);

				if (GrfEditorConfiguration.FeMissingSprAct)
					_findErrorsMissingSprAct(entries, result);

				if (GrfEditorConfiguration.FeEmptyFiles)
					_findErrorsEmptyFiles(entries, result);

				if (GrfEditorConfiguration.FeDb)
					_findErrorsDb(entries, result);

				if (GrfEditorConfiguration.FeSvn)
					_findErrorSvn(entries, result);

				if (GrfEditorConfiguration.FeDuplicateFiles)
					_findErrorsDuplicateFiles(entries, container, result);

				if (GrfEditorConfiguration.FeRootFiles)
					_findErrorsRootFiles(entries, result);

				if (GrfEditorConfiguration.FeDuplicatePaths)
					_findErrorsDuplicatePaths(entries, result);
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
			finally {
				prog.Finish();
			}
		}

		private void _findErrorsDuplicatePaths(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			List<string> folders = entries.Select(p => Path.GetDirectoryName(p.Key)).Distinct().ToList();

			var duplicates = entries
				.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
				.Where(g => g.Count() > 1);

			foreach (var entry in duplicates) {
				result.AddErrors(ValidationTypes.FeDuplicatePaths, entry.Key, String.Format(ValidationStrings.FeDuplicatePaths, entry.Count()));
			}
		}

		private void _findErrorsRootFiles(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			foreach (var entry in entries.Where(p => String.IsNullOrEmpty(Path.GetDirectoryName(p.Key)))) {
				result.AddErrors(ValidationTypes.FeRootFiles, entry.Key, ValidationStrings.FeRootFiles);
			}
		}

		private void _findErrorsDuplicateFiles(List<KeyValuePair<string, FileEntry>> entries, GrfHolder container, ValidateResult result) {
			Dictionary<string, int> entriesDuplicates = new Dictionary<string, int>();
			string lowerCaseEntry;

			foreach (KeyValuePair<string, FileEntry> entry in entries) {
				lowerCaseEntry = container.FileTable[entry.Key].RelativePath;

				if (entriesDuplicates.ContainsKey(lowerCaseEntry)) {
					entriesDuplicates[lowerCaseEntry]++;
				}
				else {
					entriesDuplicates[lowerCaseEntry] = 0;
				}
			}

			foreach (var entry in entriesDuplicates.Where(p => p.Value > 0)) {
				result.AddErrors(ValidationTypes.FeDuplicateFiles, entry.Key, String.Format(ValidationStrings.FeDuplicateFiles, entry.Value + 1));
			}
		}

		private void _findErrorSvn(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			foreach (var entry in entries.Where(p => p.Key.IsExtension(".svn"))) {
				result.AddErrors(ValidationTypes.FeSvn, entry.Key, ValidationStrings.FeSvn);
			}
		}

		private void _findErrorsDb(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			foreach (var entry in entries.Where(p => p.Key.IsExtension(".db"))) {
				result.AddErrors(ValidationTypes.FeDb, entry.Key, ValidationStrings.FeDb);
			}
		}

		private void _findErrorsEmptyFiles(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			string emptySizeString = Methods.FileSizeToString(0);

			foreach (var entry in entries.Where(p => p.Value.DisplaySize == emptySizeString)) {
				result.AddErrors(ValidationTypes.FeEmptyFiles, entry.Key, ValidationStrings.FeEmptyFiles);
			}
		}

		private void _findErrorsMissingSprAct(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			string garmentFolder = EncodingService.FromAnyToDisplayEncoding(@"data\sprite\·Îºê\");
			List<FileEntry> actFiles = entries.Where(p => !p.Key.Contains(garmentFolder) && p.Key.IsExtension(".act")).Select(p => p.Value).ToList();
			List<FileEntry> sprFiles = entries.Where(p => !p.Key.Contains(garmentFolder) && p.Key.IsExtension(".spr")).Select(p => p.Value).ToList();
			HashSet<string> sprFileNamesCut = new HashSet<string>(sprFiles.Select(p => p.RelativePath.Replace(Path.GetExtension(p.RelativePath), "")));
			HashSet<string> actFileNamesCut = new HashSet<string>(actFiles.Select(p => p.RelativePath.Replace(Path.GetExtension(p.RelativePath), "")));

			foreach (var entry in actFiles.Where(p => !sprFileNamesCut.Contains(p.RelativePath.RemoveExtension()))) {
				result.AddErrors(ValidationTypes.FeMissingSprAct, entry.RelativePath, ValidationStrings.FeMissingSprAct);
			}

			foreach (var entry in sprFiles.Where(p => !actFileNamesCut.Contains(p.RelativePath.RemoveExtension()))) {
				result.AddErrors(ValidationTypes.FeMissingSprAct, entry.RelativePath, ValidationStrings.FeMissingSprAct2);
			}
		}

		private void _findErrorsNoExtension(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			foreach (var entry in entries.Where(p => !Path.GetFileName(p.Key).Contains('.'))) {
				result.AddErrors(ValidationTypes.FeNoExtension, entry.Key, ValidationStrings.FeNoExtension);
			}
		}

		private void _findErrorsInvalidFileTable(List<KeyValuePair<string, FileEntry>> entries, GrfHolder container, ValidateResult result) {
			List<FileEntry> entriesList = entries.Where(p => !p.Value.Added).Select(p => p.Value).ToList();

			foreach (var entry in entriesList.Where(p => p.SizeCompressedAlignment < p.SizeCompressed)) {
				result.AddErrors(ValidationTypes.FeInvalidFileTable, entry.RelativePath, ValidationStrings.FeInvalidFileTable);
			}

			if (container.Header.IsMajorVersion(1)) {
				foreach (var entry in entriesList.Where(p => p.SizeCompressedAlignment % 8 != 0)) {
					result.AddErrors(ValidationTypes.FeInvalidFileTable, entry.RelativePath, String.Format(ValidationStrings.FeInvalidFileTable2, entry.SizeCompressedAlignment % 8));
				}
			}
		}

		private void _findErrorsSpaceSaved(List<KeyValuePair<string, FileEntry>> entries, ValidateResult result) {
			long size = 0;

			List<FileEntry> entriesList = entries.Select(p => p.Value).OrderBy(p => p.FileExactOffset).ToList();

			if (entries.Count > 0) {
				size = entriesList[0].FileExactOffset - GrfHeader.DataByteSize;
			}

			for (int i = 0; i < entries.Count - 1; i++) {
				size += entriesList[i + 1].FileExactOffset - entriesList[i].FileExactOffset - entriesList[i].SizeCompressedAlignment;
			}

			if (size > 0) {
				string spaceSavedString = Methods.FileSizeToString(size);
				result.AddErrors(ValidationTypes.FeSpaceSaved, "", String.Format(ValidationStrings.FeSpaceSaved, spaceSavedString));
			}
		}
	}
}
