using System.Collections.Generic;
using System.IO;
using GRF.ContainerFormat;

namespace GRF.Core {
	public class GrfDirectory {
		private readonly Container _grf;
		private readonly string _path;

		internal GrfDirectory(Container grf, string path) {
			_grf = grf;
			_path = path;

			if (!_grf.InternalTable.ContainsDirectory(path))
				throw GrfExceptions.__PathNotFound.Create(path);
		}

		public List<FileEntry> Entries {
			get { return _grf.Table.EntriesInDirectory(_path, SearchOption.TopDirectoryOnly); }
		}

		public List<string> Files {
			get { return _grf.Table.GetFiles(_path, SearchOption.TopDirectoryOnly); }
		}
	}
}