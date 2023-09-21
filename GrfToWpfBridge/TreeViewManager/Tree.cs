using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;

namespace GrfToWpfBridge.TreeViewManager {
	public class Tree {
		public TreeNode TreeNode;
		public TreeView TreeView;
		private Brush _foreground;

		public Tree(TreeView treeView) {
			TreeView = treeView;

			if (treeView is TkView) {
				((TkView) treeView).EncodingChanged += delegate { _updateDisplayEncoding(); };
			}
		}

		public bool DelayedUpdate { get; set; }

		public TreeNode Primary {
			get { return TreeNode; }
		}

		public Brush Foreground {
			get {
				if (_foreground == null) {
					TreeView.Dispatch(delegate {
						_foreground = new SolidColorBrush(Colors.Black);
						_foreground.Freeze();
					});
				}

				return _foreground;
			}
		}

		private void _updateDisplayEncoding() {
			List<TreeNode> nodes = _getAllNodes();

			nodes.ForEach(p => p.UpdateDisplayEncoding());
		}

		public void AddPath(TkPath path) {
			_addPath(Path.GetFileName(path.FilePath) + (String.IsNullOrEmpty(path.RelativePath) ? "" : "\\" + path.RelativePath), path);
		}

		private void _addPath(string path, TkPath tkPath) {
			string[] folders = path.Split('\\');

			if (folders.Length == 1 && folders[0] == "")
				return;

			if (TreeNode == null)
				TreeNode = new TreeNode(this, folders[0], tkPath, null);

			TreeNode.AddPath(folders, tkPath, 1);
		}

		private List<TreeNode> _getAllNodes() {
			List<TreeNode> nodes = new List<TreeNode>();

			if (TreeNode == null)
				return nodes;

			nodes.Add(TreeNode);
			TreeNode.GetAllNodes(nodes);
			return nodes;
		}

		public void Update() {
			List<TreeNode> nodes = _getAllNodes();
			TreeView.Dispatch(() => nodes.ForEach(p => p.SafeSet()));
		}

		public void Clear() {
			TreeNode = null;
		}

		public void Select(TkPath path) {
			_select(_tkPathToFolders(path));
		}

		public void ExpandFirstNode() {
			if (Primary != null)
				Primary.Expand();
		}

		private void _select(string[] paths) {
			if (paths.Length == 1)
				TreeNode.Select();
			else
				TreeNode.Select(paths);
		}

		private string[] _tkPathToFolders(TkPath path) {
			return (Path.GetFileName(path.FilePath) + (String.IsNullOrEmpty(path.RelativePath) ? "" : "\\" + path.RelativePath)).Split('\\');
		}

		public TreeNode GetNode(TkPath path) {
			return _getNode(_tkPathToFolders(path));
		}

		private TreeNode _getNode(string[] paths) {
			if (paths.Length == 1)
				return Primary;

			return Primary.Find(paths);
		}

		public void Expand(TkPath path) {
			_expand(_tkPathToFolders(path));
		}

		public void Expand(string path) {
			_expand(new string[] { "" }.Concat(path.Split('\\')).ToArray());
		}

		private void _expand(string[] paths) {
			if (paths.Length == 1)
				TreeNode.Expand();
			else
				TreeNode.Expand(paths);
		}

		public void Rename(TkPath oldName, TkPath newName) {
			TreeNode oldNode = GetNode(oldName);
			TreeNode parent = oldNode.Parent;

			if (parent != null) {
				parent.Children.Remove(oldNode.Header);
			}

			oldNode.SetName(Path.GetFileName(newName.RelativePath));

			TreeNode destinationParent = GetNode(new TkPath { FilePath = newName.FilePath, RelativePath = Path.GetDirectoryName(newName.RelativePath) });

			if (destinationParent != null) {
				destinationParent.Children.Add(oldNode.Header, oldNode);
				oldNode.Parent = destinationParent;
			}
		}

		public bool Contains(TkPath tkPath) {
			return GetNode(tkPath) != null;
		}

		public void AddNode(TreeNode node) {
			TreeNode parentNode = GetNode(new TkPath { FilePath = node.TkPath.FilePath, RelativePath = Path.GetDirectoryName(node.TkPath.RelativePath) });
			parentNode.Children.Add(node.Header, node);
			node.Parent = parentNode;
			List<TreeNode> nodes = new List<TreeNode>();
			nodes.Add(node);
			node.GetAllNodes(nodes);
			nodes.ForEach(p => p.Set());
		}

		public void AddTvisToTree() {
			TreeView.Dispatch(delegate {
				TreeNode.AddTvisToTree();
			});
		}
	}
}