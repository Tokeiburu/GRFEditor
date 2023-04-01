using System;
using System.Collections.Generic;
using System.IO;
using Utilities;

namespace GrfToWpfBridge.TreeViewManager {
	public class BaseTree {
		public BaseTreeNode TreeNode;

		public bool DelayedUpdate { get; set; }

		public BaseTreeNode Primary {
			get { return TreeNode; }
		}

		public void AddPath(TkPath path) {
			_addPath(Path.GetFileName(path.FilePath) + (String.IsNullOrEmpty(path.RelativePath) ? "" : "\\" + path.RelativePath), path);
		}

		private void _addPath(string path, TkPath tkPath) {
			string[] folders = path.Split('\\');

			if (folders.Length == 1 && folders[0] == "")
				return;

			if (TreeNode == null)
				TreeNode = new BaseTreeNode(this, folders[0], tkPath, null);

			TreeNode.AddPath(folders, tkPath, 1);
		}

		public void Clear() {
			TreeNode = null;
		}

		private string[] _tkPathToFolders(TkPath path) {
			return (Path.GetFileName(path.FilePath) + (String.IsNullOrEmpty(path.RelativePath) ? "" : "\\" + path.RelativePath)).Split('\\');
		}

		public BaseTreeNode GetNode(TkPath path) {
			return _getNode(_tkPathToFolders(path));
		}

		private BaseTreeNode _getNode(string[] paths) {
			if (paths.Length == 1)
				return Primary;

			return Primary.Find(paths);
		}

		public void Rename(TkPath oldName, TkPath newName) {
			BaseTreeNode oldNode = GetNode(oldName);

			BaseTreeNode parent = oldNode.Parent;

			if (parent != null) {
				parent.Children.Remove(oldNode.Header);
			}

			oldNode.SetName(Path.GetFileName(newName.RelativePath));

			BaseTreeNode destinationParent = GetNode(new TkPath { FilePath = newName.FilePath, RelativePath = Path.GetDirectoryName(newName.RelativePath) });

			if (destinationParent != null) {
				destinationParent.Children.Add(oldNode.Header, oldNode);
				oldNode.Parent = destinationParent;
			}
		}

		public bool Contains(TkPath tkPath) {
			return GetNode(tkPath) != null;
		}

		public void AddNode(BaseTreeNode node) {
			BaseTreeNode parentNode = GetNode(new TkPath { FilePath = node.TkPath.FilePath, RelativePath = Path.GetDirectoryName(node.TkPath.RelativePath) });
			parentNode.Children.Add(node.Header, node);
			node.Parent = parentNode;
			List<BaseTreeNode> nodes = new List<BaseTreeNode>();
			nodes.Add(node);
			node.GetAllNodes(nodes);
		}
	}
}