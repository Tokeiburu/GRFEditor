using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Extension;
using Utilities.Services;

namespace GrfToWpfBridge.TreeViewManager {
	public class TreeNode {
		//private readonly Dictionary<string, TreeNode> _children = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, TreeNode> _children = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);
		private readonly bool _enableDrop = true;
		private readonly TkPath _path;
		private readonly Tree _root;
		private TreeNode _parent;
		private bool _set = false;

		public TreeNode(Tree root) {
			_root = root;
		}

		public TreeNode(Tree root, string header, TkPath path, TreeNode parent) {
			_path = path;
			Header = header;
			_parent = parent;
			_root = root;

			if (parent == null && header != null && header.IsExtension(".thor", ".rgz")) {
				_enableDrop = false;
			}

			if (!_root.DelayedUpdate) {
				Set();
			}
		}

		public TkPath TkPath {
			get { return _path; }
		}

		public TkTreeViewItem Tvi { get; private set; }

		public string Header { get; private set; }

		public Dictionary<string, TreeNode> Children {
			get { return _children; }
		}

		public TreeNode Parent {
			get { return _parent; }
			set { _parent = value; }
		}

		public void AddTvis() {
			
		}

		public void UndoDelete(int index) {
			if (_parent == null) {
				_root.TreeView.Items.Insert(index, Tvi);
			}
			else {
				_parent.Tvi.Items.Insert(index, Tvi);
			}
		}

		public void Set() {
			if (_set)
				return;

			if (_parent == null) {
				_root.TreeView.Dispatch(delegate {
					_setTvi();
					_root.TreeView.Items.Add(Tvi);
				});
			}
			else {
				_parent.Tvi.Dispatch(delegate {
					_setTvi();
					_parent.Tvi.Items.Add(Tvi);
				});
			}

			_set = true;
		}

		private void _setTvi() {
			if (Header.GetExtension() == null) {
				if (Header == "root") {
					Tvi = new ProjectTreeViewItem(new TkPath { FilePath = ".root" }, _root.TreeView as TkView) { HeaderText = Header };
				}
				else {
					Tvi = new TkTreeViewItem(_root.TreeView as TkView) { HeaderText = Header };
				}
			}
			else {
				Tvi = new ProjectTreeViewItem(_path, _root.TreeView as TkView) { HeaderText = Header, CanBeDropped = _enableDrop };
			}

			//Tvi = new TkTreeViewItem(_root.TreeView as TkView) { HeaderText = Header, Foreground = _root.Foreground };
		}

		public void GetAllNodes(List<TreeNode> nodes) {
			List<TreeNode> children = _children.Values.ToList();
			nodes.AddRange(children);
			children.ForEach(p => p.GetAllNodes(nodes));
		}

		public void AddPath(string[] folders, TkPath path, int index = 0) {
			if (index >= folders.Length)
				return;

			if (!_children.ContainsKey(folders[index])) {
				_children[folders[index]] = new TreeNode(_root, folders[index], new TkPath { FilePath = path.FilePath, RelativePath = Methods.Aggregate(folders.Skip(1).Take(index).ToList(), "\\") }, this);
			}

			_children[folders[index]].AddPath(folders, path, index + 1);
		}

		public void SafeSet() {
			if (_parent == null) {
				_setTvi();
				_root.TreeView.Items.Add(Tvi);
			}
			else {
				_setTvi();
				_parent.Tvi.Items.Add(Tvi);
			}
		}

		public void Select() {
			_root.TreeView.Dispatch(() => Tvi.IsSelected = true);
		}

		public void Select(string[] paths, int index = 1) {
			if (index >= paths.Length)
				return;

			Expand();

			// We retrieve the last node
			if (index == paths.Length - 1) {
				if (_children.ContainsKey(paths[index]))
					_children[paths[index]].Select();

				return;
			}

			if (_children.ContainsKey(paths[index]))
				_children[paths[index]].Select(paths, index + 1);
		}

		public void Expand(string[] paths, int index = 1) {
			if (index >= paths.Length)
				return;

			// We retrieve the last node
			if (index == paths.Length - 1) {
				if (_children.ContainsKey(paths[index]))
					_children[paths[index]].Expand();

				return;
			}

			if (_children.ContainsKey(paths[index])) {
				_children[paths[index]].Expand();
				_children[paths[index]].Expand(paths, index + 1);
			}
		}

		public void Expand() {
			_root.TreeView.Dispatch(() => Tvi.IsExpanded = true);
		}

		public TreeNode Find(string[] paths, int index = 1) {
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
			List<Tuple<string, TreeNode>> children = Children.ToList().Select(p => new Tuple<string, TreeNode>(p.Key, p.Value)).ToList();

			Children.Clear();

			children.ForEach(p => p.Item1 = EncodingService.DisplayEncoding.GetString(EncodingService.GetOldDisplayEncoding().GetBytes(p.Item1)));
			children.ForEach(p => Children[p.Item1] = p.Item2);
		}

		public void SetDelayed() {
			if (_set)
				return;

			if (_parent == null) {
				_setTvi();
				_root.TreeView.Items.Add(Tvi);
			}
			else {
				_setTvi();
				_parent.Tvi.Items.Add(Tvi);
			}

			_set = true;
		}

		public void AddTvisToTree() {
			SetDelayed();

			foreach (var child in _children) {
				child.Value.AddTvisToTree();
			}
		}
	}
}