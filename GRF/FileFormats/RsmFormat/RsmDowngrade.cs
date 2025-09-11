using System.Collections.Generic;
using GRF.Graphics;

namespace GRF.FileFormats.RsmFormat {
	public static class Rsm2Converter {
		public static void Flatten(Rsm rsm) {
			if (rsm.Version < 2.0)
				return;

			_setFlattenPositions(null, rsm.MainMesh);
			_downgradeTextures(rsm);

			rsm.Header.SetVersion(1, 4);
			rsm.AnimationLength = (int)(rsm.AnimationLength * 1000f / rsm.FrameRatePerSecond);
			_addRootNode(rsm);
			rsm.Reserved = new byte[16];
		}

		public static void Downgrade(Rsm rsm) {
			if (rsm.Version < 2.0)
				return;

			_calcMatrixes(null, rsm.MainMesh);
			_setPositions(null, rsm.MainMesh);
			_setScale(rsm.MainMesh);
			_setRotations(rsm);
			_downgradeTextures(rsm);

			rsm.Header.SetVersion(1, 4);
			rsm.AnimationLength = (int)(rsm.AnimationLength * 1000f / rsm.FrameRatePerSecond);
			_addRootNode(rsm);
			rsm.Reserved = new byte[16];
		}

		private static void _setFlattenPositions(Mesh parent, Mesh child) {
			var box = child.CalculateBoundingBox();
			if (parent == null)
				child.GlobalPosition = new TkVector3();
			else
				child.GlobalPosition = new TkVector3(box.PCenter.X, box.Min.Y, box.PCenter.Z);

			child.LocalPosition = new TkVector3(0, 0, 0);

			foreach (var node in child.Children) {
				_setFlattenPositions(child, node);
			}

			if (parent != null) {
				child.GlobalPosition -= parent.GlobalPosition;
			}

			child.GlobalPosition = new TkVector3(0, 0, 0);

			child.RotationKeyFrames.Clear();
			child.TransformationMatrix = TkMatrix3.Identity;
		}

		private static void _downgradeTextures(Rsm rsm) {
			if (rsm.Version >= 2.3) {
				rsm.Textures.Clear();
				Dictionary<string, int> textures2Id = new Dictionary<string, int>();

				foreach (var mesh in rsm.Meshes) {
					Dictionary<int, int> meshTid2rsmTid = new Dictionary<int, int>();

					for (int meshTid = 0; meshTid < mesh.Textures.Count; meshTid++) {
						var texture = mesh.Textures[meshTid];

						if (!textures2Id.ContainsKey(texture)) {
							textures2Id[texture] = rsm.Textures.Count;
							rsm.Textures.Add(texture);
						}

						meshTid2rsmTid[mesh.TextureIndexes[meshTid]] = textures2Id[texture];
					}

					for (int i = 0; i < mesh.TextureIndexes.Count; i++) {
						mesh.TextureIndexes[i] = meshTid2rsmTid[mesh.TextureIndexes[i]];
					}
				}
			}
		}

		private static void _addRootNode(Rsm rsm) {
			var oriRoot = rsm.MainMesh;
			var newRootMesh = new Mesh(rsm) { Name = "__RSM2RSM1" };
			newRootMesh.Children.Add(oriRoot);
			rsm.MainMesh.ParentName = "__RSM2RSM1";
			newRootMesh.GlobalScale = new TkVector3(-1, -1, 1);
			rsm.Meshes.Insert(0, newRootMesh);
			rsm.MainMesh = newRootMesh;
			rsm.MainMeshNames.Clear();
			rsm.MainMeshNames.Add("__RSM2RSM1");
		}


		private static void _calcMatrixes(Mesh parent, Mesh mesh) {
			mesh.Matrix1 = TkMatrix4.Identity;
			mesh.Matrix2 = TkMatrix4.Identity;
			mesh.InvertTransformationMatrix = new TkMatrix4(mesh.TransformationMatrix).Inverted();

			if (mesh.RotationKeyFrames.Count > 0) {

			}
			else {
				mesh.Matrix1 = mesh.Matrix1 * new TkMatrix4(mesh.TransformationMatrix);

				if (parent != null) {
					mesh.Matrix1 = mesh.Matrix1 * parent.InvertTransformationMatrix;
				}
			}

			mesh.Matrix2 = mesh.Matrix1;
			TkVector3 position = new TkVector3();

			if (mesh.PosKeyFrames.Count > 0) {
				position = mesh.PosKeyFrames[0].Position;
			}
			else {
				if (parent != null) {
					position = mesh.LocalPosition - parent.LocalPosition;
					position = new TkVector3(new TkVector4(position.X, position.Y, position.Z, 0) * parent.InvertTransformationMatrix);
				}
				else {
					position = mesh.LocalPosition;
				}
			}

			var mat = mesh.Matrix2;
			mat.Row3 = new TkVector4(position.X, position.Y, position.Z, mat.Row3.W);
			mesh.Matrix2 = mat;

			Mesh meshT = mesh;

			while (meshT.Parent != null) {
				meshT = meshT.Parent;
				mesh.Matrix2 = mesh.Matrix2 * meshT.Matrix1;
			}

			if (parent != null) {
				var mat2 = mesh.Matrix2;
				mat2.Row3 += parent.Matrix2.Row3;
				mesh.Matrix2 = mat2;
			}

			foreach (var child in mesh.Children) {
				_calcMatrixes(mesh, child);
			}
		}

		private static void _setPositions(Mesh parent, Mesh child) {
			child.GlobalPosition = new TkVector3(child.Matrix2.Row3);
			child.LocalPosition = new TkVector3(0, 0, 0);

			foreach (var node in child.Children) {
				_setPositions(child, node);
			}

			if (parent != null) {
				child.GlobalPosition -= parent.GlobalPosition;
				child.GlobalPosition *= new TkVector3(child.Parent.InvertTransformationMatrix.Row0.Length, child.Parent.InvertTransformationMatrix.Row1.Length, child.Parent.InvertTransformationMatrix.Row2.Length);
			}
		}

		private static void _setScale(Mesh child) {
			child.GlobalScale = new TkVector3(child.Matrix1.Row0.Length, child.Matrix1.Row1.Length, child.Matrix1.Row2.Length);

			foreach (var node in child.Children) {
				_setScale(node);
			}
		}

		private static void _setRotations(Rsm rsm) {
			for (int index = 0; index < rsm.Meshes.Count; index++) {
				var mesh = rsm.Meshes[index];

				for (int i = 0; i < mesh.RotationKeyFrames.Count; i++) {
					var q = mesh.RotationKeyFrames[i].Quaternion;

					mesh.RotationKeyFrames[i] = new RotKeyFrame {
						Frame = (int)(mesh.RotationKeyFrames[i].Frame * 1000f / rsm.FrameRatePerSecond),
						Quaternion = new TkQuaternion(
							q.X,
							q.Y,
							q.Z,
							q.W)
					};
				}

				if (mesh.RotationKeyFrames.Count > 0) {
					mesh.TransformationMatrix = TkMatrix3.Identity;
				}
				else {
					// Keep the rotation matrix, but remove scaling
					mesh.TransformationMatrix.Row0 = TkVector3.Normalize(mesh.TransformationMatrix.Row0);
					mesh.TransformationMatrix.Row1 = TkVector3.Normalize(mesh.TransformationMatrix.Row1);
					mesh.TransformationMatrix.Row2 = TkVector3.Normalize(mesh.TransformationMatrix.Row2);
				}
			}
		}
	}
}
