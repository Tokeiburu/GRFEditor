using GRF.Graphics;
using System;
using System.Threading.Tasks;
using Utilities;

namespace GRF.Image {
	/// <summary>
	/// A displacement map used to modify an image.
	/// Each vector represents how much the pixel should be moved.
	/// </summary>
	public class WarpField {
		public TkVector2[] Displacements;
		public int Width;
		public int Height;
		public TkVector2 Offset = new TkVector2(0);
		public bool UseClosestNearbyPixel { get; set; }
		public byte AlphaCutoff { get; set; } = 220;

		public WarpField(int width, int height) {
			Width = width;
			Height = height;
			Displacements = new TkVector2[width * height];
		}

		public ref TkVector2 At(int x, int y) {
			return ref Displacements[y * Width + x];
		}

		public void Reset() {
			Displacements = new TkVector2[Width * Height];
		}

		public void ApplyStroke(in TkVector2 start, in TkVector2 end, float pressure, float radius) {
			int distance = (int)TkVector2.Distance(start, end);
			int steps = (int)(TkVector2.Distance(start, end) / (radius * 0.25f));

			//pressure *= radius;

			if (steps == 0)
				steps = distance;

			for (int i = 0; i < steps; i++) {
				float t = i / (float)steps;
				TkVector2 p = TkVector2.Lerp(start, end, t);
				TkVector2 next = TkVector2.Lerp(start, end, t + 1f / steps);

				ApplyStrokeSub(p, next, pressure, radius);
			}
		}

		private void ApplyStrokeSub(TkVector2 start, TkVector2 end, float pressure, float radius) {
			TkVector2 stroke = end - start;
			float strokeLengthSq = stroke.LengthSquared;
			TkVector2 dir = TkVector2.Normalize(stroke);

			if (strokeLengthSq < 1e-6f)
				return;

			float minXf = (float)Math.Min(start.X, end.X) - radius;
			float maxXf = (float)Math.Max(start.X, end.X) + radius;
			float minYf = (float)Math.Min(start.Y, end.Y) - radius;
			float maxYf = (float)Math.Max(start.Y, end.Y) + radius;

			int minX = Methods.Clamp((int)minXf, 0, Width);
			int maxX = Methods.Clamp((int)maxXf, 0, Width);
			int minY = Methods.Clamp((int)minYf, 0, Height);
			int maxY = Methods.Clamp((int)maxYf, 0, Height);

			int nWidth = maxX - minX;
			int nHeight = maxY - minY;

			Parallel.For(0, nWidth * nHeight, index => {
				int x = index % nWidth + minX;
				int y = index / nWidth + minY;

				TkVector2 p = new TkVector2(x, y);

				float t = TkVector2.Dot(p - start, stroke) / strokeLengthSq;
				t = Methods.Clamp(t, 0f, 1f);

				TkVector2 closest = start + t * stroke;
				float dist = TkVector2.Distance(p, closest);

				if (dist > radius)
					return;

				float tr = dist / radius;

				float radial = 1f - tr;
				radial = Methods.Clamp(radial, 0f, 1f);
				radial = radial * radial * (3f - 2f * radial);

				float falloff = radial;
				float segmentLength = stroke.Length;
				TkVector2 delta = dir * (pressure * segmentLength) * falloff;
				At(x, y) += delta;
			});
		}

		public void ApplyTwirl(TkVector2 center, float angle, float radius, Curve curve) {
			if (angle == 0)
				return;

			int minX = Methods.Clamp((int)(center.X - radius), 0, Width);
			int maxX = Methods.Clamp((int)(center.X + radius), 0, Width);
			int minY = Methods.Clamp((int)(center.Y - radius), 0, Height);
			int maxY = Methods.Clamp((int)(center.Y + radius), 0, Height);
			int nWidth = maxX - minX;
			int nHeight = maxY - minY;

			angle = MathHelper.DegreesToRadians(angle);

			Parallel.For(0, nWidth * nHeight, index => {
				int x = index % nWidth + minX;
				int y = index / nWidth + minY;

				TkVector2 p = new TkVector2(x, y);
				TkVector2 v = p - center;
				float dist = v.Length;

				if (dist > radius || dist < 1e-5f)
					return;

				float tr = dist / radius;

				float falloff = 1f - tr;
				falloff = (float)curve.GetPoint(falloff);

				float currentAngle = angle * falloff;
				float sin = (float)Math.Sin(currentAngle);
				float cos = (float)Math.Cos(currentAngle);

				// Rotate vector
				TkVector2 rotated = new TkVector2(
					v.X * cos - v.Y * sin,
					v.X * sin + v.Y * cos
				);

				TkVector2 target = center + rotated;
				TkVector2 delta = p - target;

				At(x, y) += delta;
			});
		}

		public void ApplyTwirl(TkVector2 center, float angle, float radius, float distRadius, Curve curve) {
			if (angle == 0)
				return;

			distRadius = Math.Max(1, distRadius);

			int minX = Methods.Clamp((int)(center.X - radius - distRadius), 0, Width);
			int maxX = Methods.Clamp((int)(center.X + radius + distRadius), 0, Width);
			int minY = Methods.Clamp((int)(center.Y - radius - distRadius), 0, Height);
			int maxY = Methods.Clamp((int)(center.Y + radius + distRadius), 0, Height);
			int nWidth = maxX - minX;
			int nHeight = maxY - minY;

			angle = MathHelper.DegreesToRadians(angle);

			Parallel.For(0, nWidth * nHeight, index => {
				int x = index % nWidth + minX;
				int y = index / nWidth + minY;

				TkVector2 p = new TkVector2(x, y);
				TkVector2 v = p - center;
				float dist = v.Length;

				if (dist > radius + distRadius || dist < 1e-5f || dist < radius - distRadius)
					return;

				float tr;

				if (dist > radius)
					tr = (dist - radius) / distRadius;
				else
					tr = (radius - dist) / distRadius;

				float falloff = 1f - tr;
				falloff = (float)curve.GetPoint(falloff);

				float currentAngle = angle * falloff * dist / radius;
				float sin = (float)Math.Sin(currentAngle);
				float cos = (float)Math.Cos(currentAngle);

				// Rotate vector
				TkVector2 rotated = new TkVector2(
					v.X * cos - v.Y * sin,
					v.X * sin + v.Y * cos
				);

				TkVector2 target = center + rotated;
				TkVector2 delta = p - target;

				At(x, y) += delta;
			});
		}

		public void ApplyRadial(TkVector2 center, float pressure, float radius, Curve curve) {
			if (pressure == 0)
				return;

			int minX = Methods.Clamp((int)(center.X - radius), 0, Width);
			int maxX = Methods.Clamp((int)(center.X + radius), 0, Width);
			int minY = Methods.Clamp((int)(center.Y - radius), 0, Height);
			int maxY = Methods.Clamp((int)(center.Y + radius), 0, Height);
			int nWidth = maxX - minX;
			int nHeight = maxY - minY;

			Parallel.For(0, nWidth * nHeight, index => {
				int x = index % nWidth + minX;
				int y = index / nWidth + minY;

				TkVector2 p = new TkVector2(x, y);
				TkVector2 v = p - center;
				float dist = v.Length;

				if (dist > radius || dist < 1e-5f)
					return;

				float tr = dist / radius;
				float falloff = 1f - tr;

				falloff = Methods.Clamp(falloff, 0f, 1f);
				falloff = (float)curve.GetPoint(falloff);

				float strength = pressure * falloff;
				strength = Methods.Clamp(strength, -1f, 1f);

				float newDist;
				TkVector2 target;
				TkVector2 delta;

				if (strength >= 0f) {
					newDist = dist * (1f + strength);

					target = center + v * newDist / dist;
					delta = target - p;
				}
				else {
					float t = -strength;
					newDist = dist * (1f - t) + radius * t;

					target = center + v * newDist / dist;
					delta = p - target;
				}

				At(x, y) += delta;
			});
		}

		public void ApplyVortex(TkVector2 center, float pressure, float radius, float collapse, Curve curve) {
			if (pressure == 0 && collapse == 0)
				return;

			int minX = Methods.Clamp((int)(center.X - radius), 0, Width);
			int maxX = Methods.Clamp((int)(center.X + radius), 0, Width);
			int minY = Methods.Clamp((int)(center.Y - radius), 0, Height);
			int maxY = Methods.Clamp((int)(center.Y + radius), 0, Height);
			int nWidth = maxX - minX;
			int nHeight = maxY - minY;

			Parallel.For(0, nWidth * nHeight, index => {
				int x = index % nWidth + minX;
				int y = index / nWidth + minY;

				TkVector2 p = new TkVector2(x, y);
				TkVector2 v = p - center;
				float dist = v.Length;

				if (dist > radius)
					return;

				float tr = dist / radius;

				float falloff = 1f - tr;
				falloff = Methods.Clamp(falloff, 0f, 1f);
				falloff = (float)curve.GetPoint(falloff);

				TkVector2 dir;

				if (dist < 1e-5f) {
					dir = new TkVector2(1f, 0);
				}
				else {
					dir = v / dist;
				}

				TkVector2 perp = new TkVector2(-dir.Y, dir.X);

				float swirl = pressure * falloff;
				float inward = falloff * collapse;

				TkVector2 delta = perp * swirl + (-dir) * inward;
				delta *= radius;

				At(x, y) += delta;
			});
		}

		private TkVector2 SampleDisplacement(float x, float y) {
			int x0 = (int)Math.Floor(x);
			int y0 = (int)Math.Floor(y);
			int x1 = x0 + 1;
			int y1 = y0 + 1;

			float tx = x - x0;
			float ty = y - y0;

			x0 = Methods.Clamp(x0, 0, Width - 1);
			y0 = Methods.Clamp(y0, 0, Height - 1);
			x1 = Methods.Clamp(x1, 0, Width - 1);
			y1 = Methods.Clamp(y1, 0, Height - 1);

			TkVector2 d00 = At(x0, y0);
			TkVector2 d10 = At(x1, y0);
			TkVector2 d01 = At(x0, y1);
			TkVector2 d11 = At(x1, y1);

			TkVector2 dx0 = d00 + (d10 - d00) * tx;
			TkVector2 dx1 = d01 + (d11 - d01) * tx;

			return dx0 + (dx1 - dx0) * ty;
		}

		public void ApplyAndReset(GrfImage image) {
			image.ApplyWarpField(this);
			Reset();
		}

		public void Apply(GrfImage image) {
			image.ApplyWarpField(this);
		}

		public TkVector2 Dummy = new TkVector2(0, 0);

		public ref TkVector2 GetSafe(int x, int y) {
			x += (int)Offset.X;
			y += (int)Offset.Y;

			if (x < 0 || x >= Width || y < 0 || y >= Height)
				return ref Dummy;

			return ref Displacements[y * Width + x];
		}
	}
}
