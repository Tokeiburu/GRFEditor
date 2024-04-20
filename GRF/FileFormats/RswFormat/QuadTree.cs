using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ErrorManager;
using GRF.Core;
using GRF.FileFormats.GndFormat;
using GRF.FileFormats.RswFormat.RswObjects;
using GRF.Graphics;
using GRF.IO;
using Utilities.CommandLine;

namespace GRF.FileFormats.RswFormat {
	public class QuadTree {
		public const int NodesCount = 1365;
		public const int MaxLevel = 5;

		public List<QuadTreeNode> Nodes {
			get {
				return _nodes;
			}
			set {
				_nodes = value;
			}
		}

		private List<QuadTreeNode> _nodes = new List<QuadTreeNode>();

		public QuadTree() {
			for (int x = 0; x < NodesCount; x++) {
				_nodes.Add(new QuadTreeNode());
			}
		}

		public QuadTree(IBinaryReader reader) {
			int i = 0;

			for (int x = 0; x < NodesCount; x++) {
				_nodes.Add(new QuadTreeNode());
			}

			_readQuadTree(reader, 0, ref i);
		}

		public void GenerateQuadTree(GrfHolder grf, int sizeX, int sizeY, Gnd gnd, Rsw rsw, float margin) {
			List<BoundingBox> allBoundingBoxes = new List<BoundingBox>();
			//
			//foreach (RswObject obj in rsw.Objects) {
			//	if (obj.Type == RswObjectType.Model) {
			//		Model rsmModel = (Model) obj;
			//
			//		if (grf != null && grf.FileTable.Contains("data\\model\\" + rsmModel.ModelName)) {
			//			Rsm rsm = new Rsm(grf.FileTable["data\\model\\" + rsmModel.ModelName].GetDecompressedData());
			//			rsm.CalculateBoundingBox();
			//			var rsmBox = new BoundingBox(rsm.Box);
			//			rsmBox.BaseCenter();
			//
			//			Matrix4 matrix = Matrix4.Identity;
			//			matrix = Matrix4.Scale(matrix, rsmModel.Scale);
			//			matrix.SelfTranslate(rsmModel.Position);
			//			matrix = Matrix4.RotateZ(matrix, (float) (rsmModel.Rotation[2] / 180f * Math.PI));
			//			matrix = Matrix4.RotateX(matrix, (float) (rsmModel.Rotation[0] / 180f * Math.PI));
			//			matrix = Matrix4.RotateY(matrix, (float) (rsmModel.Rotation[1] / 180f * Math.PI));
			//			rsmBox.Multiply(matrix);
			//
			//			allBoundingBoxes.Add(rsmBox);
			//		}
			//		else {
			//			CLHelper.Warning = "Couldn't locate " + "data\\model\\" + rsmModel.ModelName;
			//		}
			//	}
			//}

			int i = 0;
			_writeQuadTree(0, 5 * sizeX - 1, 5 * sizeY - 1, -5 * sizeX, -5 * sizeY, ref i, gnd, rsw, 0, allBoundingBoxes);
		}

		private void _writeQuadTree(int level, float max1, float max3, float min1, float min3, ref int i, Gnd gnd, Rsw rsw, float margin, List<BoundingBox> boxes) {
			try {
				float max2 = 0;
				float min2 = 0;
				float average;

				try {
					if (gnd != null) {
						max2 = float.MinValue;
						min2 = float.MaxValue;

						float yOffset = gnd.Header.Height / 2f;
						int yStart = (int) (yOffset + min3 / gnd.Header.ProportionRatio);
						int yEnd = (int) (yOffset + max3 / gnd.Header.ProportionRatio + 1);

						float xOffset = gnd.Header.Width / 2f;
						int xStart = (int) (xOffset + min1 / gnd.Header.ProportionRatio);
						int xEnd = (int) (xOffset + max1 / gnd.Header.ProportionRatio + 1);

						for (int j = yStart; j < yEnd; j++) {
							for (int k = xStart; k < xEnd; k++) {
								try {
									average = gnd.Cubes[j * gnd.Header.Width + k].Average;

									if (average < min2)
										min2 = average;
									if (average > max2)
										max2 = average;
								}
								catch (Exception err) {
									ErrorHandler.HandleException(err);
								}
							}
						}

						float t = min2;
						min2 = max2;
						max2 = t;

						min2 *= -1f;
						max2 *= -1f;

						List<BoundingBox> models = boxes.Where(p =>
						                                       (min1 <= p.Max[0] && p.Min[0] <= max1) &&
						                                       //(min2 <= p.Max[1] && p.Min[1] <= max2) &&
						                                       (min3 <= p.Max[2] && p.Min[2] <= max3)).ToList();

						//List<BoundingBox> modelsExcluded = boxes.Where(p =>
						//                                               !((min1 <= p.Max[0] && p.Min[0] <= max1) &&
						//                                                 //(min2 <= p.Max[1] && p.Min[1] <= max2) &&
						//                                                 (min3 <= p.Max[2] && p.Min[2] <= max3))).ToList();

						//Z.F(modelsExcluded);
						//models = boxes;

						float max = 0;
						float min = 0;

						if (models.Any()) {
							max = models.Max(p => p.Max.Y) + margin;
							min = models.Min(p => p.Min.Y) - margin;
						}

						if (min < min2)
							min2 = min;
						if (max > max2)
							max2 = max;
					}
				}
				catch (Exception err) {
					ErrorHandler.HandleException(err);
				}

				// Must revert the Y axis
				QuadTreeNode node = _nodes[i];
				node.Max[0] = max1;
				node.Max[1] = - min2 - margin;
				node.Max[2] = max3;

				if (node.Max[1] < 0) {
					node.Max[1] = 0;
				}

				node.Min[0] = min1;
				node.Min[1] = - max2 + margin;
				node.Min[2] = min3;

				if (node.Min[1] > 0) {
					node.Min[1] = 0;
				}

				node.Halfsize[0] = (max1 - min1) / 2;
				node.Halfsize[1] = (max2 - min2) / 2;
				node.Halfsize[2] = (max3 - min3) / 2;

				node.Center[0] = node.Halfsize[0] + min1;
				node.Center[1] = node.Halfsize[1] + min2;
				node.Center[2] = node.Halfsize[2] + min3;

				//Output += _read(node.Max);
				//Output += _read(node.Min);
				//Output += _read(node.Halfsize);
				//Output += _read(node.Center);

				i++;

				if (level < MaxLevel) {
					node.Child[0] = i;
					_writeQuadTree(level + 1, node.Center[0], node.Center[2], min1, min3, ref i, gnd, rsw, margin, boxes);
					node.Child[1] = i;
					_writeQuadTree(level + 1, max1, node.Center[2], node.Center[0], min3, ref i, gnd, rsw, margin, boxes);
					node.Child[2] = i;
					_writeQuadTree(level + 1, node.Center[0], max3, min1, node.Center[2], ref i, gnd, rsw, margin, boxes);
					node.Child[3] = i;
					_writeQuadTree(level + 1, max1, max3, node.Center[0], node.Center[2], ref i, gnd, rsw, margin, boxes);
				}
				else {
					node.Child[0] = 0;
					node.Child[1] = 0;
					node.Child[2] = 0;
					node.Child[3] = 0;
				}
			}
			catch (Exception err) {
				ErrorHandler.HandleException(err);
			}
		}

		public void GenerateFlatQuadTree(int sizeX, int sizeY, Gnd gnd, Rsw rsw, float margin) {
			if (DataHolder.QuadTreeAlreadyCalculated(sizeX, sizeY)) {
				_nodes = DataHolder.GetQuadTree(sizeX, sizeY);
			}
			else {
				int i = 0;
				_writeFlatQuadTree(0, 5 * sizeX - 1, 5 * sizeY - 1, -5 * sizeX, -5 * sizeY, ref i, gnd, rsw, margin);

				DataHolder.AddQuadTree(sizeX, sizeY, _nodes);
			}
		}

		public void GenerateQuadTree(int sizeX, int sizeY) {
			if (DataHolder.QuadTreeAlreadyCalculated(sizeX, sizeY)) {
				_nodes = DataHolder.GetQuadTree(sizeX, sizeY);
			}
			else {
				int i = 0;
				_writeFlatQuadTree(0, 5 * sizeX - 1, 5 * sizeY - 1, -5 * sizeX, -5 * sizeY, ref i, null, null, 0);

				DataHolder.AddQuadTree(sizeX, sizeY, _nodes);
			}
		}

		public void Write(BinaryWriter stream) {
			foreach (QuadTreeNode node in _nodes) {
				node.Write(stream);
			}
		}

		private void _readQuadTree(IBinaryReader reader, int level, ref int i) {
			QuadTreeNode node = _nodes[i];

			node.Max = reader.ArrayFloat(3);
			node.Min = reader.ArrayFloat(3);
			node.Halfsize = reader.ArrayFloat(3);
			node.Center = reader.ArrayFloat(3);

			i++;

			if (level < MaxLevel) {
				node.Child[0] = i;
				_readQuadTree(reader, level + 1, ref i);
				node.Child[1] = i;
				_readQuadTree(reader, level + 1, ref i);
				node.Child[2] = i;
				_readQuadTree(reader, level + 1, ref i);
				node.Child[3] = i;
				_readQuadTree(reader, level + 1, ref i);
			}
			else {
				node.Child[0] = 0;
				node.Child[1] = 0;
				node.Child[2] = 0;
				node.Child[3] = 0;
			}
		}

		private void _writeFlatQuadTree(int level, float max1, float max3, float min1, float min3, ref int i, Gnd gnd, Rsw rsw, float margin) {
			float max2 = 0;
			float min2 = 0;
			float average;

			if (gnd != null) {
				max2 = float.MinValue;
				min2 = float.MaxValue;

				for (int j = (int) (gnd.Header.Height / 2f + min3 / gnd.Header.ProportionRatio); j < (int) (gnd.Header.Height / 2f + max3 / gnd.Header.ProportionRatio + 1); j++) {
					for (int k = (int) (gnd.Header.Width / 2f + min1 / gnd.Header.ProportionRatio); k < (int) (gnd.Header.Width / 2f + max1 / gnd.Header.ProportionRatio + 1); k++) {
						average = gnd.Cubes[j * gnd.Header.Height + k].Average;

						if (average < min2)
							min2 = average;
						if (average > max2)
							max2 = average;
					}
				}

				List<RswObject> models = rsw.Objects.Where(p => min1 - 10f <= p.Position.X && p.Position.X <= max3 + 10f &&
				                                                min3 - 10f <= p.Position.Z && p.Position.Z <= max3 + 10f).ToList();
				float max = 0;
				float min = 0;
				if (models.Any()) {
					max = models.Max(p => p.Position.Y);
					min = models.Min(p => p.Position.Y);
				}

				if (min < min2)
					min2 = min;
				if (max > max2)
					max2 = max;
			}

			QuadTreeNode node = _nodes[i];
			node.Max[0] = max1;
			node.Max[1] = max2 + margin;
			node.Max[2] = max3;

			node.Min[0] = min1;
			node.Min[1] = min2 - margin;
			node.Min[2] = min3;

			node.Halfsize[0] = (max1 - min1) / 2;
			node.Halfsize[1] = (max2 - min2) / 2;
			node.Halfsize[2] = (max3 - min3) / 2;

			node.Center[0] = node.Halfsize[0] + min1;
			node.Center[1] = node.Halfsize[1] + min2;
			node.Center[2] = node.Halfsize[2] + min3;

			//Output += _read(node.Max);
			//Output += _read(node.Min);
			//Output += _read(node.Halfsize);
			//Output += _read(node.Center);

			i++;

			if (level < MaxLevel) {
				node.Child[0] = i;
				_writeFlatQuadTree(level + 1, node.Center[0], node.Center[2], min1, min3, ref i, gnd, rsw, margin);
				node.Child[1] = i;
				_writeFlatQuadTree(level + 1, max1, node.Center[2], node.Center[0], min3, ref i, gnd, rsw, margin);
				node.Child[2] = i;
				_writeFlatQuadTree(level + 1, node.Center[0], max3, min1, node.Center[2], ref i, gnd, rsw, margin);
				node.Child[3] = i;
				_writeFlatQuadTree(level + 1, max1, max3, node.Center[0], node.Center[2], ref i, gnd, rsw, margin);
			}
			else {
				node.Child[0] = 0;
				node.Child[1] = 0;
				node.Child[2] = 0;
				node.Child[3] = 0;
			}
		}

		public void Print(string filename) {
			int i = 0;
			using (StreamWriter writer = new StreamWriter(filename)) {
				_printQuadTree(0, ref i, writer);
			}
		}

		private void _printQuadTree(int level, ref int i, StreamWriter writer) {
			QuadTreeNode node = _nodes[i];

			_print(level, node.Max, writer);
			_print(level, node.Min, writer);
			_print(level, node.Halfsize, writer);
			_print(level, node.Center, writer);

			i++;

			if (level < MaxLevel) {
				_printQuadTree(level + 1, ref i, writer);
				_printQuadTree(level + 1, ref i, writer);
				_printQuadTree(level + 1, ref i, writer);
				_printQuadTree(level + 1, ref i, writer);
			}
			else {
				node.Child[0] = 0;
				node.Child[1] = 0;
				node.Child[2] = 0;
				node.Child[3] = 0;
			}
		}

		private void _print(int level, float[] val, StreamWriter writer) {
			writer.WriteLine(CLHelper.Fill(' ', level * 2) + "{" + val[0] + ", " + val[1] + ", " + val[2] + "}");
		}
	}
}