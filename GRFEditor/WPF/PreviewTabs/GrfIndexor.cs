using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF;
using GRF.Core;
using GRF.Core.GroupedGrf;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RsmFormat;
using GRF.FileFormats.RswFormat;
using GRF.FileFormats.StrFormat;
using GRF.System;
using GRFEditor.ApplicationConfiguration;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GRFEditor.WPF.PreviewTabs {
	public struct MappedObject {
		public HashSet<MappedObject> Children;
		public HashSet<MappedObject> Parents;

		public MappedObject(string name)
			: this() {
			Children = new HashSet<MappedObject>();
			Parents = new HashSet<MappedObject>();
			OriginalName = name;
		}

		public string OriginalName { get; set; }

		public override string ToString() {
			return OriginalName;
		}

		public void Write(TextWriter writer) {
			writer.WriteLine("{0}:{1}:{2}", OriginalName, string.Join(",", Children.Select(p => p.OriginalName).ToArray()), string.Join(",", Parents.Select(p => p.OriginalName).ToArray()));
		}

		public void Clear() {
			Children.Clear();
			Parents.Clear();
		}
	}

	public class GrfIndexor {
		private readonly Dictionary<string, MappedObject> _data = new Dictionary<string, MappedObject>(StringComparer.OrdinalIgnoreCase);
		private readonly object _lock = new object();

		public void Clear() {
			_data.ToList().ForEach(p => p.Value.Clear());
			_data.Clear();
		}

		public void Add(TkPath path, MultiGrfReader metaGrf, Action<float> progress) {
			if (path.RelativePath != null) return;

			string name = String.Format("db_{0}_{1}.tdb", path.FilePath.GetHashCode(), new FileInfo(path.FilePath.ReplaceFirst(GrfStrings.CurrentlyOpenedGrfHeader, "")).LastWriteTimeUtc.Ticks);
			string fileName = Path.Combine(GrfEditorConfiguration.ProgramDataPath, name);

			if (File.Exists(fileName)) {
				_load(fileName);
			}
			else {
				GrfHolder grf = metaGrf.GetGrf(path.FilePath.ReplaceFirst(GrfStrings.CurrentlyOpenedGrfHeader, ""));

				if (grf == null) {
					grf = new GrfHolder();
					grf.Open(path.FilePath.ReplaceFirst(GrfStrings.CurrentlyOpenedGrfHeader, ""));
				}

				grf.ThreadOperation(progress, () => false, (entry, data) => {
					string file = entry.RelativePath;
					//progress(index / (float)count * 100f);

					if (!_data.ContainsKey(file)) {
						lock (_lock) {
							_data[file] = new MappedObject(file);
						}
					}

					MappedObject map = _data[file];
					List<string> resources = new List<string>();

					switch (file.GetExtension()) {
						case ".str":
							Str str = new Str(metaGrf.GetData(file));
							resources.AddRange(str.Textures.Select(p => @"data\texture\effect\" + p));
							break;
						case ".gnd":
							var dataEntry = ((MultiType)metaGrf.GetData(file)).GetBinaryReader();
							GndHeader gndHeader = new GndHeader(dataEntry);

							for (int i = 0; i < gndHeader.TextureCount; i++) {
								resources.Add(@"data\texture\" + dataEntry.String(gndHeader.TexturePathSize, '\0'));
							}

							break;
						case ".rsw":
							Rsw rsw = new Rsw(metaGrf.GetData(file));
							resources.AddRange(rsw.ModelResources.Select(p => @"data\model\" + p));
							break;
						case ".rsm":
						case ".rsm2":
							var byteData = metaGrf.GetData(file);

							var binaryReader = ((MultiType)byteData).GetBinaryReader();
							RsmHeader rsmHeader = new RsmHeader(binaryReader);

							if (rsmHeader.Version < 2.0) {
								binaryReader.Forward(8);

								if (rsmHeader.Version >= 1.4) {
									binaryReader.Forward(1);
								}

								binaryReader.Forward(16);
								var count = binaryReader.Int32();

								for (int i = 0; i < count; i++) {
									resources.Add(@"data\texture\" + binaryReader.String(40, '\0'));
								}
							}
							else {
								binaryReader.Position = 0;
								Rsm rsm2 = new Rsm(binaryReader);

								resources.AddRange(rsm2.Textures.Select(p => @"data\texture\" + p));

								foreach (var mesh in rsm2.Meshes) {
									resources.AddRange(mesh.Textures.Select(p => @"data\texture\" + p));
								}

								resources = resources.Distinct().ToList();
							}

							break;
					}

					if (resources.Count > 0) {
						foreach (string resource in resources.Distinct()) {
							try {
								if (!_data.ContainsKey(resource)) {
									lock (_lock) {
										_data[resource] = new MappedObject(resource);
									}
								}

								MappedObject child = _data[resource];
								child.Parents.Add(map);

								map.Children.Add(child);
							}
							catch (Exception err) {
								ErrorHandler.HandleException(err);
							}
						}

						lock (_lock) {
							_data[file] = map;
						}
					}
				});

				using (MemoryStream stream = new MemoryStream())
				using (StreamWriter writer = new StreamWriter(stream, EncodingService.Ansi)) {
					List<MappedObject> maps = _data.Values.ToList();

					for (int index = 0; index < maps.Count; index++) {
						MappedObject map = maps[index];
						map.Write(writer);
					}

					writer.Flush();

					stream.Seek(0, SeekOrigin.Begin);
					byte[] compressed = Compression.CompressDotNet(stream);
					File.WriteAllBytes(fileName, compressed);
				}
			}
		}

		private void _load(string fileName) {
			string file = TemporaryFilesManager.GetTemporaryFilePath("db_{0:0000}.db");
			File.WriteAllBytes(file, Compression.DecompressDotNet(File.ReadAllBytes(fileName)));

			List<string[]> lines = File.ReadAllLines(file, EncodingService.Ansi).Select(p => p.Split(':')).ToList();

			for (int i = 0; i < lines.Count; i++) {
				string originalName = lines[i][0];

				if (!_data.ContainsKey(originalName))
					_data[originalName] = new MappedObject(originalName);
			}

			for (int i = 0; i < lines.Count; i++) {
				string[] line = lines[i];

				try {
					MappedObject map = _data[line[0]];

					List<string> children = Methods.StringToList(line[1]);

					for (int j = 0; j < children.Count; j++) {
						try {
							map.Children.Add(_data[children[j]]);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}

					List<string> parents = Methods.StringToList(line[2]);

					for (int j = 0; j < parents.Count; j++) {
						try {
							map.Parents.Add(_data[parents[j]]);
						}
						catch (Exception err) {
							ErrorHandler.HandleException(err);
						}
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}
			}
		}

		public void DeleteIndexes() {
			try {
				foreach (string file in Directory.GetFiles(GrfEditorConfiguration.ProgramDataPath, "*.tdb", SearchOption.TopDirectoryOnly)) {
					File.Delete(file);
				}
			}
			catch {
			}
		}

		public List<string> FindUsage(string relativePath) {
			MappedObject map = _data[relativePath];

			return map.Parents.Select(p => p.OriginalName).ToList();
		}
	}
}