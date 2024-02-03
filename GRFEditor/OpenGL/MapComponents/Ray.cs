using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK;

namespace GRFEditor.OpenGL.MapComponents {
	public class Ray {
		public Vector3 Origin;
		public Vector3 Dir;
		public Vector3 InvDir;
		public int[] Sign = new int[3];

		public Ray(Vector3 origin, Vector3 dir) {
			Origin = origin;
			Dir = dir;

			InvDir = new Vector3(1.0f / Dir.X, 1.0f / Dir.Y, 1.0f / Dir.Z);
			Sign[0] = (InvDir.X < 0 ? 1 : 0);
			Sign[1] = (InvDir.Y < 0 ? 1 : 0);
			Sign[2] = (InvDir.Z < 0 ? 1 : 0);
		}

		public bool LineIntersectPolygon(List<Vector3> vertices, int verticeOffset, ref float t) {
			Vector3 N = Vector3.Cross(vertices[verticeOffset + 1] - vertices[verticeOffset + 0], vertices[verticeOffset + 2] - vertices[verticeOffset + 0]);

			if (N.Length < 0.000001f)
				return false;

			N = Vector3.Normalize(N);
			float D = -Vector3.Dot(N, vertices[verticeOffset]);
			float Denominator = Vector3.Dot(Dir, N);
			if (Math.Abs(Denominator) <= 0.001f) {
				return false;
			}

			float Numerator = Vector3.Dot(Origin, N) + D;
			t = -Numerator / Denominator;

			Vector3 intersection = Origin + Dir * t;

			for (int i = 0; i < 3; i++) {
				int nextVertex = (i + 1) % 3;
				Vector3 edgeVector = vertices[verticeOffset + nextVertex] - vertices[verticeOffset + i];
				Vector3 edgeNormal = Vector3.NormalizeFast(Vector3.Cross(edgeVector, N));
				float edgeD = -Vector3.Dot(edgeNormal, vertices[verticeOffset + i]);

				if (Vector3.Dot(edgeNormal, intersection) + edgeD > 0.001f)
					return false;
			}

			return true;
		}
	}

	public class AABB {
		public Vector3[] Bounds = new Vector3[2];
		public Vector3 Min;
		public Vector3 Max;

		public AABB(Vector3 min, Vector3 max) {
			Min = Bounds[0] = min;
			Max = Bounds[1] = max;
		}

		public bool HasRayCollision(Ray r, float minDistance, float maxDistance) {
			float tmin, tmax, tymin, tymax, tzmin, tzmax;

			tmin = (Bounds[r.Sign[0]].X - r.Origin.X) * r.InvDir.X;
			tmax = (Bounds[1 - r.Sign[0]].X - r.Origin.X) * r.InvDir.X;

			tymin = (Bounds[r.Sign[1]].Y - r.Origin.Y) * r.InvDir.Y;
			tymax = (Bounds[1 - r.Sign[1]].Y - r.Origin.Y) * r.InvDir.Y;

			if ((tmin > tymax) || (tymin > tmax))
				return false;

			if (tymin > tmin)
				tmin = tymin;

			if (tymax < tmax)
				tmax = tymax;

			tzmin = (Bounds[r.Sign[2]].Z - r.Origin.Z) * r.InvDir.Z;
			tzmax = (Bounds[1 - r.Sign[2]].Z - r.Origin.Z) * r.InvDir.Z;

			if ((tmin > tzmax) || (tzmin > tmax))
				return false;

			if (tzmin > tmin)
				tmin = tzmin;

			if (tzmax < tmax)
				tmax = tzmax;

			return ((tmin < maxDistance) && (tmax > minDistance));
		}
	}
}
