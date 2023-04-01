using System;
using System.Collections.Generic;
using System.Linq;
using Utilities;

namespace GRF.Hash {
	public class HashObjectComparerView {
		private readonly byte[] _hashMaster;
		private readonly byte[] _hashUser;

		public HashObjectComparerView(HashObjectValidationTypes type, string hMaster, string hUser, byte[] hashMaster, byte[] hashUser) {
			ValidationType = type.ToString();

			OriginalMaster = hMaster;
			OriginalUser = hUser;
			DisplayFilePathMaster = hMaster ?? hUser ?? "";
			FilePathMaster = hMaster ?? hUser ?? "";
			ErrorType = type;

			_hashMaster = hashMaster;
			_hashUser = hashUser;

			ErrorToolTip = type.Description;
		}

		public HashObjectValidationTypes ErrorType { get; set; }

		public string OriginalMaster { get; set; }
		public string OriginalUser { get; set; }

		public byte[] RawHashMaster {
			get { return _hashMaster; }
		}

		public byte[] RawHashUser {
			get { return _hashUser; }
		}

		public string ValidationType { get; private set; }

		public string DisplayFilePathMaster { get; private set; }
		public string FilePathMaster { get; private set; }

		public string ErrorToolTip { get; private set; }

		public string HashMaster {
			get {
				return _hashMaster != null ? Methods.ByteArrayToString(_hashMaster) : "";
			}
		}

		public string HashUser {
			get {
				return _hashUser != null ? Methods.ByteArrayToString(_hashUser) : "";
			}
		}

		public bool Default {
			get { return true; }
		}

		public override string ToString() {
			return String.Format("{0}\t{1}\t{2}\t{3}", ErrorType, FilePathMaster, HashMaster, HashUser);
		}
	}

	public class HashObjectComparer {
		public static List<HashObjectComparerView> GetIssues(HashObject hoMaster, HashObject hoUser, bool useExactPath) {
			List<HashObjectComparerView> errors = new List<HashObjectComparerView>();

			Dictionary<string, byte[]> hUser = new Dictionary<string, byte[]>();
			Dictionary<string, byte[]> hMaster = new Dictionary<string, byte[]>();

			List<string> scannedGrfs = hoUser.Hashes.Where(p => !String.IsNullOrEmpty(p.Key.RelativePath)).Select(p => p.Key.FilePath).Distinct().ToList();

			foreach (var pair in hoMaster.Hashes) {
				string fullpath = pair.Key.GetFullPath();

				if (hMaster.ContainsKey(fullpath)) {
					errors.Add(new HashObjectComparerView(
								   HashObjectValidationTypes.HoDuplicateFile,
								   pair.Key.GetFullPath(), null,
								   pair.Value, null));
					continue;
				}

				hMaster.Add(fullpath, pair.Value);
			}

			foreach (var pair in hoUser.Hashes) {
				if (Methods.ByteArrayCompare(pair.Value, hoMaster.HashMethod.Error)) {
					errors.Add(new HashObjectComparerView(
								   HashObjectValidationTypes.HoGenericError,
								   null, pair.Key.GetFullPath(),
								   null, hoMaster.HashMethod.Error));
					continue;
				}

				string fullpath = pair.Key.GetFullPath();

				if (hUser.ContainsKey(fullpath)) {
					errors.Add(new HashObjectComparerView(
								   HashObjectValidationTypes.HoDuplicateFile,
								   null, pair.Key.GetFullPath(),
								   null, pair.Value));
					continue;
				}

				hUser.Add(fullpath, pair.Value);
			}

			List<string> processedFiles = new List<string>();
			string dataPath;
			string grfDataPath;
			bool missing;

			foreach (KeyValuePair<string, byte[]> hMasterPair in hMaster) {
				missing = true;

				if (!useExactPath && hMasterPair.Key.Contains("?")) {
					dataPath = hMasterPair.Key.Split('?')[1];

					if (hUser.ContainsKey(dataPath)) {
						if (!Methods.ByteArrayCompare(hMasterPair.Value, hUser[dataPath])) {
							errors.Add(new HashObjectComparerView(
										   HashObjectValidationTypes.HoPathOverride,
										   "Master : " + hMasterPair.Key + "; User : " + dataPath, hMasterPair.Key + ">" + dataPath,
										   hMasterPair.Value, hUser[dataPath]));
						}

						missing = false;
						hUser.Remove(dataPath);
						processedFiles.Add(hMasterPair.Key);
					}

					for (int i = 0; i < scannedGrfs.Count; i++) {
						grfDataPath = scannedGrfs[i] + "?" + dataPath;

						if (hUser.ContainsKey(grfDataPath)) {
							if (!Methods.ByteArrayCompare(hMasterPair.Value, hUser[grfDataPath])) {
								if (grfDataPath == hMasterPair.Key) {
									//if (false) {
									errors.Add(new HashObjectComparerView(
												   HashObjectValidationTypes.HoHashDifferent,
												   hMasterPair.Key, hMasterPair.Key,
												   hMasterPair.Value, hUser[hMasterPair.Key]));
								}
								else {
									errors.Add(new HashObjectComparerView(
												   HashObjectValidationTypes.HoPathOverride,
												   "Master : " + hMasterPair.Key + "; User : " + grfDataPath, hMasterPair.Key + ">" + grfDataPath,
												   hMasterPair.Value, hUser[grfDataPath]));
								}
							}

							missing = false;
							hUser.Remove(grfDataPath);
							processedFiles.Add(hMasterPair.Key);
						}
					}

					if (missing) {
						errors.Add(new HashObjectComparerView(
								   HashObjectValidationTypes.HoMissingFile,
								   hMasterPair.Key, null,
								   hMasterPair.Value, null));
					}

					continue;
				}

				if (hUser.ContainsKey(hMasterPair.Key)) {
					if (!Methods.ByteArrayCompare(hMasterPair.Value, hUser[hMasterPair.Key])) {
						errors.Add(new HashObjectComparerView(
									   HashObjectValidationTypes.HoHashDifferent,
									   hMasterPair.Key, hMasterPair.Key,
									   hMasterPair.Value, hUser[hMasterPair.Key]));
					}

					hUser.Remove(hMasterPair.Key);
					processedFiles.Add(hMasterPair.Key);
				}
				else {
					errors.Add(new HashObjectComparerView(
								   HashObjectValidationTypes.HoMissingFile,
								   hMasterPair.Key, null,
								   hMasterPair.Value, null));
					// Methods.ByteArrayToString(hMasterPair.Value), null));
				}
			}

			for (int i = 0; i < processedFiles.Count; i++) {
				hUser.Remove(processedFiles[i]);
			}

			errors.AddRange(hUser.Select(extra => new HashObjectComparerView(HashObjectValidationTypes.HoExtraFile, null, extra.Key, null, extra.Value)));

			return errors;
		}
	}

	public sealed class HashObjectValidationTypes {
		public static HashObjectValidationTypes HoMissingFile = new HashObjectValidationTypes("HoMissingFile", "warning16.png", "This file is missing from the user directory.");
		public static HashObjectValidationTypes HoHashMatch = new HashObjectValidationTypes("HoHashMatch", "validity.png", "Both the user and the master files have the same hash.");
		public static HashObjectValidationTypes HoDuplicateFile = new HashObjectValidationTypes("HoDuplicateFile", "error16.png", "This file has been found twice in the GRF (lower casing issue?).");
		public static HashObjectValidationTypes HoGenericError = new HashObjectValidationTypes("HoGenericError", "error16.png", "This file couldn't be decompressed or there \r\nwas an unknown error while reading the file.");
		public static HashObjectValidationTypes HoHashDifferent = new HashObjectValidationTypes("HoHashDifferent", "error16.png", "The user and master files are different.");
		public static HashObjectValidationTypes HoPathOverride = new HashObjectValidationTypes("HoPathOverride", "error16.png", "The user data path overrides the file in the master files.");
		public static HashObjectValidationTypes HoExtraFile = new HashObjectValidationTypes("HoExtraFile", "help.png", "This file doesn't appear in the master object, only the user has it.");

		private HashObjectValidationTypes(string type, string icon, string description) {
			Type = type;
			Icon = icon;
			Description = description;
		}

		public string Type { get; private set; }
		public string Icon { get; private set; }
		public string Description { get; private set; }

		public override string ToString() {
			return Type;
		}
	}
}
