using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using ErrorManager;
using Utilities;
using Utilities.Extension;

namespace GRF.Core.GroupedGrf {
	public class MultiGrfPath {
		public string Path { get; set; }
		public bool FromConfiguration { get; set; }
		public bool IsCurrentlyLoadedGrf { get; set; }

		public MultiGrfPath(string path) {
			Path = path;
		}
	}

	public class MultiGrfReader : IDisposable {
		private readonly Dictionary<string, byte[]> _bufferedData = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, GrfHolder> _containers = new Dictionary<string, GrfHolder>(StringComparer.OrdinalIgnoreCase);
		private readonly MultiFileTable _multiFileTable;
		private readonly List<MultiGrfPath> _paths = new List<MultiGrfPath>();
		private bool _disposed;
		public bool CurrentGrfAlwaysFirst { get; set; }

		public MultiGrfReader() {
			_multiFileTable = new MultiFileTable(this);
			FileTable = _multiFileTable;
		}

		public FileTable FileTable { get; private set; }

		public string LatestFile { get; private set; }

		public Dictionary<string, GrfHolder> Containers {
			get { return _containers; }
		}

		public ReadOnlyCollection<MultiGrfPath> Paths {
			get { return _paths.AsReadOnly(); }
		}

		#region IDisposable Members

		public void Dispose() {
			Dispose(true);
		}

		#endregion

		public void Update(List<MultiGrfPath> paths, GrfHolder extraGrf = null) {
			_openGrfs(paths, extraGrf);
		}

		public void Update(string path) {
			Update(new List<MultiGrfPath> { new MultiGrfPath(path) });
		}

		public void Update(GrfHolder grf) {
			Update(new List<MultiGrfPath> { new MultiGrfPath(grf.FileName) { IsCurrentlyLoadedGrf = true }}, grf);
		}

		public void Add(string path) {
			_openGrfs(new List<MultiGrfPath> { new MultiGrfPath(path) }, null, false);
		}

		public void Reload() {
			try {
				foreach (var container in Containers.Values.Where(p => File.Exists(p.FileName))) {
					string fileName = container.FileName;
					container.Close();
					container.Open(fileName);
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public TkPath FindTkPath(string relativePath) {
			if (String.IsNullOrEmpty(relativePath))
				return null;

			var entry = FileTable[relativePath];
			if (entry != null) {
				if (entry.SourceFilePath != null)
					return new TkPath(entry.SourceFilePath);

				return new TkPath(entry.Stream.FileName, relativePath);
			}

			return null;
		}

		public TkPath FindTkPathAbsolute(string relativePath) {
			if (String.IsNullOrEmpty(relativePath))
				return null;

			var entry = FileTable[relativePath];
			if (entry != null) {
				if (entry.SourceFilePath != null)
					return new TkPath(entry.SourceFilePath);

				return new TkPath(entry.Stream.FileName, relativePath);
			}

			return null;
		}

		private void _openGrfs(List<MultiGrfPath> paths, GrfHolder extraGrf, bool clear = true) {
			try {
				if (clear) {
					_paths.Clear();

					var copy = new Dictionary<string, GrfHolder>(_containers);

					foreach (var grf in copy.Values) {
						grf.Attached["MultiGrfRreader.Delete"] = null;
					}

					_containers.Clear();

					foreach (var resource in paths) {
						if (copy.ContainsKey(resource.Path)) {
							if (copy[resource.Path] == extraGrf)	// Never keep the current GRF
								continue;
							_containers[resource.Path] = copy[resource.Path];
							copy[resource.Path].Attached["MultiGrfRreader.Delete"] = false;
						}
					}

					foreach (var grf in copy.Values) {
						var value = grf.Attached["MultiGrfRreader.Delete"];

						if (value != null && (bool)value == true) {
							grf.Close();
						}
					}

					copy.Clear();
				}

				foreach (var resource in paths) {
					if (resource.IsCurrentlyLoadedGrf) {
						if (CurrentGrfAlwaysFirst)
							_paths.Insert(0, new MultiGrfPath(extraGrf.FileName) { IsCurrentlyLoadedGrf = true });
						else
							_paths.Add(new MultiGrfPath(extraGrf.FileName) { IsCurrentlyLoadedGrf = true });

						if (!_containers.ContainsKey(extraGrf.FileName)) {
							_containers[extraGrf.FileName] = extraGrf;
						}
					}
					else if ((!String.IsNullOrEmpty(resource.Path)) && File.Exists(resource.Path)) {
						_paths.Add(resource);

						if (!_containers.ContainsKey(resource.Path)) {
							GrfHolder grf = new GrfHolder();
							grf.Open(resource.Path);
							_containers[resource.Path] = grf;
						}
					}
					else {
						_paths.Add(resource);
					}
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public byte[] GetData(string relativePath) {
			try {
				if (relativePath.StartsWith(GrfStrings.RgzRoot))
					relativePath = relativePath.Remove(0, GrfStrings.RgzRoot.Length);

				var entry = FileTable[relativePath];

				if (entry != null) {
					if (entry.SourceFilePath != null)
						LatestFile = entry.SourceFilePath;
					else
						LatestFile = relativePath;

					return entry.GetDecompressedData();
				}

				if (File.Exists(relativePath)) {
					LatestFile = relativePath;
					return File.ReadAllBytes(relativePath);
				}

				LatestFile = null;
				return null;
			}
			catch {
				LatestFile = null;
				return null;
			}
		}

		public byte[] GetDataBuffered(string relativePath) {
			if (_bufferedData.ContainsKey(relativePath))
				return _bufferedData[relativePath];

			if (_bufferedData.Count > 15)
				_bufferedData.Clear();

			byte[] data;

			var entry = FileTable[relativePath];
			if (entry != null) {
				data = entry.GetDecompressedData();
				_bufferedData[relativePath] = data;
				return data;
			}

			if (File.Exists(relativePath)) {
				data = File.ReadAllBytes(relativePath);
				_bufferedData[relativePath] = data;
				return data;
			}

			_bufferedData[relativePath] = null;
			return null;
		}

		public GrfHolder GetGrf(string file) {
			if (_containers.ContainsKey(file))
				return _containers[file];

			return null;
		}

		protected void Dispose(bool disposing) {
			if (!_disposed) {
				if (disposing) {
					if (_containers != null) {
						_containers.Clear();
					}
				}

				_disposed = true;
			}
		}

		public static implicit operator MultiGrfReader(GrfHolder grf) {
			MultiGrfReader reader = new MultiGrfReader();
			reader.Update(grf);
			return reader;
		}

		public FileEntry GetEntry(string relativePath) {
			var entry = FileTable[relativePath];
			if (entry != null) {
				return entry;
			}

			return null;
		}

		public IEnumerable<string> FilesInDirectory(string directory) {
			return FileTable.GetFiles(directory, null, SearchOption.TopDirectoryOnly, true);
		}

		public bool Exists(string file) {
			return FileTable[file] != null;
		}

		public void Clear() {
			_containers.Values.ToList().ForEach(c => {
				if (c.IsOpened) {
					c.Commands.UndoAll();
				}
			});
		}

		public void Close() {
			_paths.Clear();

			var copy = new Dictionary<string, GrfHolder>(_containers);

			foreach (var grf in copy.Values) {
				grf.Attached["MultiGrfRreader.Delete"] = null;
			}

			_containers.Clear();

			foreach (var grf in copy.Values) {
				var value = grf.Attached["MultiGrfRreader.Delete"];

				if (value != null && (bool)value == true) {
					grf.Close();
				}
			}

			copy.Clear();
		}

		public void SetData(string relativePath, byte[] data) {
			if (File.Exists(relativePath)) {
				File.WriteAllBytes(relativePath, data);
			}
			else if (FileTable.ContainsFile(relativePath)) {
				TkPath path = FindTkPathAbsolute(relativePath);

				try {
					if (String.IsNullOrEmpty(path.RelativePath)) {
						File.WriteAllBytes(path.FilePath, data);
					}
					else {
						_containers[path.FilePath].Commands.AddFile(relativePath, data);
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public void SaveAndReload() {
			try {
				foreach (var container in Containers) {
					if (container.Value.IsModified) {
						container.Value.QuickSave();

						if (container.Value.CancelReload)
							throw new OperationCanceledException();

						container.Value.Reload();
					}
				}
			}
			finally {
				Unlock();
			}
		}

		public void Save() {
			try {
				foreach (var container in Containers) {
					if (container.Value.IsModified) {
						container.Value.QuickSave();

						if (container.Value.CancelReload)
							throw new OperationCanceledException();
					}
				}
			}
			finally {
				Unlock();
			}
		}

		public void Lock() {
			_multiFileTable.Lock();
		}

		public void Unlock() {
			_multiFileTable.Unlock();
		}
	}
}