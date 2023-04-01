using System.Collections.Generic;
using System.IO;
using System.Linq;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GrfToWpfBridge.TreeViewManager {
	public class BaseTreeNode {
		private readonly Dictionary<string, BaseTreeNode> _children = new Dictionary<string, BaseTreeNode>();
		private readonly TkPath _path;
		private readonly BaseTree _root;

		public BaseTreeNode(BaseTree root) {
			_root = root;
		}

		public BaseTreeNode(BaseTree root, string header, TkPath path, BaseTreeNode parent) {
			_path = path;
			Header = header;
			Parent = parent;
			_root = root;
		}

		public TkPath TkPath {
			get { return _path; }
		}

		public TkTreeViewItem Tvi { get; private set; }

		public string Header { get; private set; }

		public Dictionary<string, BaseTreeNode> Children {
			get { return _children; }
		}

		public BaseTreeNode Parent { get; set; }

		public void GetAllNodes(List<BaseTreeNode> nodes) {
			List<BaseTreeNode> children = _children.Values.ToList();
			nodes.AddRange(children);
			children.ForEach(p => p.GetAllNodes(nodes));
		}

		public void AddPath(string[] folders, TkPath path, int index = 0) {
			if (index >= folders.Length)
				return;

			if (!_children.ContainsKey(folders[index])) {
				_children[folders[index]] = new BaseTreeNode(_root, folders[index], new TkPath { FilePath = path.FilePath, RelativePath = Methods.Aggregate(folders.Skip(1).Take(index).ToList(), "\\") }, this);
			}

			_children[folders[index]].AddPath(folders, path, index + 1);
		}

		public BaseTreeNode Find(string[] paths, int index = 1) {
			if (index >= paths.Length)
				return null;

			// We retrieve the last node
			if (index == paths.Length - 1) {
				if (_children.ContainsKey(paths[index]))
					return _children[paths[index]];

				return null;
			}

			if (_children.ContainsKey(paths[index]))
				return _children[paths[index]].Find(paths, index + 1);

			return null;
		}

		public void SetName(string fileName) {
			Header = Path.GetFileName(fileName);
			Tvi.Dispatch(p => p.HeaderText = Path.GetFileName(fileName));
		}

		public void UpdateDisplayEncoding() {
			Header = EncodingService.DisplayEncoding.GetString(EncodingService.GetOldDisplayEncoding().GetBytes(Header));
			List<Tuple<string, BaseTreeNode>> children = Children.ToList().Select(p => new Tuple<string, BaseTreeNode>(p.Key, p.Value)).ToList();

			Children.Clear();

			children.ForEach(p => p.Item1 = EncodingService.DisplayEncoding.GetString(EncodingService.GetOldDisplayEncoding().GetBytes(p.Item1)));
			children.ForEach(p => Children[p.Item1] = p.Item2);
		}
	}
}