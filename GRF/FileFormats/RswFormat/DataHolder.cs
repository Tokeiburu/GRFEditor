using System.Collections.Generic;
using System.Linq;
using Utilities.Extension;

namespace GRF.FileFormats.RswFormat {
	public sealed class DataHolder {
		public static List<Tuple<int, int, List<QuadTreeNode>>> QuadTrees = new List<Tuple<int, int, List<QuadTreeNode>>>();

		public static bool QuadTreeAlreadyCalculated(int sizeX, int sizeY) {
			return QuadTrees.Any(quadTree => quadTree.Item1 == sizeX && quadTree.Item2 == sizeY);
		}

		public static List<QuadTreeNode> GetQuadTree(int sizeX, int sizeY) {
			return (from quadTree in QuadTrees where quadTree.Item1 == sizeX && quadTree.Item2 == sizeY select quadTree.Item3).FirstOrDefault();
		}

		public static void AddQuadTree(int sizeX, int sizeY, List<QuadTreeNode> nodes) {
			QuadTrees.Add(new Tuple<int, int, List<QuadTreeNode>>(sizeX, sizeY, nodes));
		}
	}
}