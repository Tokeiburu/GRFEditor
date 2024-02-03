using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using TokeiLibrary;
using TokeiLibrary.WPF;
using Utilities;
using Utilities.Services;

namespace GrfToWpfBridge.TreeViewManager {
	public class DeletedPath {
		public TreeNode Node;
		public int Index;
	}

	public class TreeViewPathManager {
		private readonly List<List<TkPath>> _addedPaths = new List<List<TkPath>>();
		private readonly List<DeletedPath> _deletedPaths = new List<DeletedPath>();
		private readonly Tree _tree;
		private readonly TkView _treeView;
		private string _container;

		public TreeViewPathManager(TkView treeView) {
			_treeView = treeView;
			_tree = new Tree(_treeView);
		}

		public string GetContainerPath() {
			return ((ProjectTreeViewItem) _treeView.Items[0]).TkPath.FilePath;
		}

		public void AddNewGrf(string name) {
			ClearAll();
			_container = name;
			AddPath(new TkPath { FilePath = name, RelativePath = "data" });
		}

		public void AddNewRgz(string name) {
			ClearAll();
			_container = name;
			AddPath(new TkPath { FilePath = name, RelativePath = "root" });
		}

		public void AddPath(TkPath path) {
			path.RelativePath = path.RelativePath.Replace("\\\\", "\\");

			string containerPath = path.FilePath;

			if (_container != containerPath) {
				ClearAll();
				_container = containerPath;
			}

			_tree.AddPath(path);
		}

		public void Select(TkPath path) {
			_tree.Select(path);
		}

		public void Select(int index) {
			_treeView.Dispatcher.Invoke(new Action(() => ((TkTreeViewItem) _treeView.Items[index]).IsSelected = true));
		}

		public void ExpandFirstNode() {
			_tree.ExpandFirstNode();
		}

		public void ExpandAll() {
		    _treeView.Dispatcher.Invoke(new Action(delegate {
		        foreach (TkTreeViewItem item in _treeView.Items) {
		            _expandAll(item);
		        }
		    }));
		}

		private void _expandAll(TkTreeViewItem item) {
		    item.IsExpanded = true;

		    foreach (TkTreeViewItem sitem in item.Items) {
		        _expandAll(sitem);
		    }
		}

		public string GetCurrentRelativePath() {
			return (string) _treeView.Dispatcher.Invoke(new Func<string>(delegate {
				if (_treeView.SelectedItem == null)
					return "";

				string relativePath = "";
				TkTreeViewItem currentNode = (TkTreeViewItem) _treeView.SelectedItem;

				if (currentNode.Parent is TreeView)
					return "";

				relativePath += currentNode.HeaderText;

				while (true) {
					currentNode = currentNode.Parent as TkTreeViewItem;

					if (currentNode == null)
						break;

					if (currentNode.Parent is TreeView)
						break;


					relativePath = currentNode.HeaderText + "\\" + relativePath;
				}

				return relativePath;
			}));
		}

		public void ClearAll() {
			ClearCommands();
			_addedPaths.Clear();
			_deletedPaths.Clear();
			_tree.Clear();
			_container = null;

			_treeView.Dispatch(() => _treeView.SelectedItem = null);
			_treeView.Dispatch(() => _treeView.Items.Clear());
			_treeView.Dispatch(() => _treeView.DisplayEncoding = EncodingService.DisplayEncoding);
		}

		public void Rename(TkPath oldName, TkPath newName) {
			TreeNode node = _tree.GetNode(oldName);
			var currentNode = node.Tvi;

			if (Path.GetDirectoryName(oldName.RelativePath) == Path.GetDirectoryName(newName.RelativePath)) {
				currentNode.HeaderText = Path.GetFileName(newName.RelativePath);
			}
			else {
				TkTreeViewItem parent = _tree.GetNode(oldName).Tvi.Parent as TkTreeViewItem;

				if (parent != null) {
					parent.Items.Remove(currentNode);
					_tree.GetNode(new TkPath { FilePath = newName.FilePath, RelativePath = Path.GetDirectoryName(newName.RelativePath) }).Tvi.Items.Add(currentNode);
				}
			}

			_tree.Rename(oldName, newName);
		}

		public static TkPath GetTkPath(object selectedItem) {
			if (selectedItem == null) return null;

			TkTreeViewItem item = selectedItem as TkTreeViewItem;

			string currentPath = null;
			if (item != null) {
				currentPath = (string) item.Dispatcher.Invoke(new Func<string>(() => item.HeaderText));

				while (item.Parent != null && !(item.Parent is TreeView)) {
					item = ((TkTreeViewItem) item.Parent);
					currentPath = item.Dispatcher.Invoke(new Func<string>(() => item.HeaderText)) + "\\" + currentPath;
				}
			}

			if (currentPath == null)
				return new TkPath { FilePath = null, RelativePath = null };

			string[] folders = currentPath.Split('\\');

			if (folders.Length == 1)
				return new TkPath { FilePath = folders[0], RelativePath = "" };

			return new TkPath { FilePath = folders[0], RelativePath = folders.Skip(1).Aggregate((a, b) => a + '\\' + b) };
		}

		public void DeletePath(TkPath tkPath, bool saveCommand = true) {
			if (!_tree.Contains(tkPath)) {
				return;
			}

			var item = _tree.GetNode(tkPath).Tvi;
			TkTreeViewItem itemParent = item.Parent as TkTreeViewItem;

			int parentIndex = 0;

			if (itemParent != null) {
				parentIndex = itemParent.Items.IndexOf(item);
				itemParent.Items.Remove(item);
			}

			if (itemParent != null && itemParent.Items.Count == 0)
				itemParent.IsExpanded = false;

			TreeNode node = _tree.GetNode(tkPath);

			if (saveCommand)
				_deletedPaths.Add(new DeletedPath { Node = node, Index = parentIndex });

			node.Parent.Children.Remove(node.Header);
		}

		public void UndoDeletePath(string containerPath) {
			_tree.AddNode(_deletedPaths.Last());
			_deletedPaths.RemoveAt(_deletedPaths.Count - 1);
		}

		public void Expand(TkPath path) {
			_tree.Expand(path);
		}

		public void Expand(string path) {
			_tree.Expand(path);
		}

		public TkTreeViewItem GetNode(TkPath path) {
			return _tree.GetNode(path).Tvi;
		}

		public void AddFolders(string containerPath, List<string> grfPaths) {
			grfPaths = grfPaths.Distinct().ToList();
			List<TkPath> potentialPathsAdded = grfPaths.Distinct().Select(p => new TkPath { FilePath = containerPath, RelativePath = p }).ToList();

			// We explode all the paths
			for (int i = 0; i < grfPaths.Count; i++) {
				string[] folders = grfPaths[i].Split('\\');

				for (int j = 0; j < folders.Length; j++) {
					potentialPathsAdded.Add(new TkPath { FilePath = containerPath, RelativePath = Methods.Aggregate(folders.Take(j + 1).ToList(), "\\") });
				}
			}

			potentialPathsAdded = potentialPathsAdded.OrderBy(p => p.RelativePath).Distinct().ToList();

			List<TkPath> pathsAdded = new List<TkPath>();

			foreach (TkPath potentialPath in potentialPathsAdded) {
				if (!_tree.Contains(potentialPath)) {
					pathsAdded.Add(potentialPath);
					AddPath(potentialPath);
				}
			}

			_addedPaths.Add(pathsAdded);
		}

		public void AddFoldersUndo(string containerPath) {
			List<TkPath> addedPaths = _addedPaths.Last();
			_addedPaths.RemoveAt(_addedPaths.Count - 1);

			foreach (TkPath folder in addedPaths) {
				DeletePath(folder, false);
			}
		}

		public void ClearCommands() {
			_addedPaths.Clear();
			_deletedPaths.Clear();
		}

		public string GetCurrentPath() {
			return (string) _treeView.Dispatcher.Invoke(new Func<string>(delegate {
				if (_treeView.SelectedItem == null)
					return "";

				string relativePath = "";
				TkTreeViewItem currentNode = (TkTreeViewItem) _treeView.SelectedItem;

				if (currentNode is ProjectTreeViewItem)
					return "";

				relativePath += currentNode.HeaderText;

				while (currentNode.Parent != null && !(currentNode.Parent is ProjectTreeViewItem)) {
					currentNode = (TkTreeViewItem) currentNode.Parent;
					relativePath = currentNode.HeaderText + "\\" + relativePath;
				}

				var item = currentNode.Parent as ProjectTreeViewItem;

				if (item != null) {
					if (item.Parent is ProjectTreeViewItem) {
						relativePath = item.HeaderText + "\\" + relativePath;
						return ((ProjectTreeViewItem)item.Parent).TkPath.FilePath + "?" + relativePath;
					}

					return item.TkPath.FilePath + "?" + relativePath;
				}

				return relativePath;
			}));
		}

		public List<string> GetExpandedFolders() {
			return (List<string>) _treeView.Dispatcher.Invoke(new Func<List<string>>(delegate {
				if (_treeView.Items.Count == 0)
					return null;

				List<string> paths = new List<string>();

				paths.AddRange(_getExpandedFolders(_treeView.Items[0] as TkTreeViewItem));

				return paths;
			}));
		}

		private IEnumerable<string> _getExpandedFolders(TkTreeViewItem currentNode) {
			List<string> paths = new List<string>();

			foreach (TkTreeViewItem node in currentNode.Items) {
				if (node.IsExpanded && node.Items.Count == 0)
					continue;
				if (!node.IsExpanded)
					continue;
				if (node.IsExpanded) {
					paths.Add(GetTkPath(node).RelativePath);
					paths.AddRange(_getExpandedFolders(node));
				}
			}

			return paths;
		}

		public void RenamePrimaryProject(string fileName) {
			_container = fileName;
			_tree.Primary.SetName(fileName);
		}

		public void SelectFirstNode() {
			_treeView.Dispatcher.Invoke(new Action(delegate {
				if (_treeView.Items.Count == 1) {
					TkTreeViewItem item = (TkTreeViewItem) _treeView.Items[0];

					if (item.Items.Count > 0) {
						((TkTreeViewItem)item.Items[0]).IsSelected = true;
					}
				}
			}));
		}

		public void AddPaths(string containerPath, List<string> paths) {
			if (_container != containerPath) {
				ClearAll();
				_container = containerPath;
			}

			var tkPaths = paths.Select(p => new TkPath { FilePath = containerPath, RelativePath = p.Replace("\\\\", "\\") }).ToList();

			try {
				_tree.DelayedUpdate = true;
				string fileName = Path.GetFileName(containerPath);

				foreach (var tkPath in tkPaths) {
					string[] folders = (fileName + (String.IsNullOrEmpty(tkPath.RelativePath) ? "" : "\\" + tkPath.RelativePath)).Split('\\');

					if (folders.Length == 1 && folders[0] == "")
						return;

					if (_tree.TreeNode == null)
						_tree.TreeNode = new TreeNode(_tree, folders[0], tkPath, null);

					_tree.TreeNode.AddPath(folders, tkPath, 1);
				}
			}
			finally {
				_tree.DelayedUpdate = false;
				_tree.AddTvisToTree();
			}
		}
	}
}