using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GRF.Graphics {
	public struct TkQuaternion {
		private static int c_identityHashCode = TkQuaternion.GetIdentityHashCode();
		private static TkQuaternion s_identity = TkQuaternion.GetIdentity();
		internal float _x;
		internal float _y;
		internal float _z;
		internal float _w;
		private bool _isNotDistinguishedIdentity;

		/// <summary>
		/// Gets the Identity quaternion
		/// </summary>
		/// 
		/// <returns>
		/// The Identity quaternion.
		/// </returns>
		public static TkQuaternion Identity {
			get {
				return TkQuaternion.s_identity;
			}
		}

		/// <summary>
		/// Gets the quaternion's axis.
		/// </summary>
		/// 
		/// <returns>
		/// <see cref="T:System.Windows.Media.Media3D.Vertex"/> that represents the quaternion's axis.
		/// </returns>
		public Vertex Axis {
			get {
				if (this.IsDistinguishedIdentity || this._x == 0.0 && this._y == 0.0 && this._z == 0.0)
					return new Vertex(0.0, 1.0, 0.0);
				Vertex Vertex = new Vertex(this._x, this._y, this._z);
				Vertex.Normalize();
				return Vertex;
			}
		}

		/// <summary>
		/// Gets the quaternion's angle, in degrees.
		/// </summary>
		/// 
		/// <returns>
		/// Double that represents the quaternion's angle, in degrees.
		/// </returns>
		public float Angle {
			get {
				if (this.IsDistinguishedIdentity)
					return 0;
				float y = (float)Math.Sqrt(this._x * this._x + this._y * this._y + this._z * this._z);
				float x = this._w;
				if (y > float.MaxValue) {
					float num1 = Math.Max(Math.Abs(this._x), Math.Max(Math.Abs(this._y), Math.Abs(this._z)));
					float num2 = this._x / num1;
					float num3 = this._y / num1;
					float num4 = this._z / num1;
					y = (float)Math.Sqrt(num2 * num2 + num3 * num3 + num4 * num4);
					x = this._w / num1;
				}
				return (float)(Math.Atan2(y, x) * 114.591559026165);
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the quaternion is normalized.
		/// </summary>
		/// 
		/// <returns>
		/// true if the quaternion is normalized, false otherwise.
		/// </returns>
		public bool IsNormalized {
			get {
				if (this.IsDistinguishedIdentity)
					return true;
				else
					return Math.Abs(Length() - 1.0) < 0.0005;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the specified quaternion is an <see cref="P:System.Windows.Media.Media3D.Quaternion.Identity"/> quaternion.
		/// </summary>
		/// 
		/// <returns>
		/// true if the quaternion is the <see cref="P:System.Windows.Media.Media3D.Quaternion.Identity"/> quaternion, false otherwise.
		/// </returns>
		public bool IsIdentity {
			get {
				if (this.IsDistinguishedIdentity)
					return true;
				if (this._x == 0.0 && this._y == 0.0 && this._z == 0.0)
					return this._w == 1.0;
				else
					return false;
			}
		}

		/// <summary>
		/// Gets the X component of the quaternion.
		/// </summary>
		/// 
		/// <returns>
		/// The X component of the quaternion.
		/// </returns>
		public float X {
			get {
				return this._x;
			}
			set {
				if (this.IsDistinguishedIdentity) {
					this = TkQuaternion.s_identity;
					this.IsDistinguishedIdentity = false;
				}
				this._x = value;
			}
		}

		/// <summary>
		/// Gets the Y component of the quaternion.
		/// </summary>
		/// 
		/// <returns>
		/// The Y component of the quaternion.
		/// </returns>
		public float Y {
			get {
				return this._y;
			}
			set {
				if (this.IsDistinguishedIdentity) {
					this = TkQuaternion.s_identity;
					this.IsDistinguishedIdentity = false;
				}
				this._y = value;
			}
		}

		/// <summary>
		/// Gets the Z component of the quaternion.
		/// </summary>
		/// 
		/// <returns>
		/// The Z component of the quaternion.
		/// </returns>
		public float Z {
			get {
				return this._z;
			}
			set {
				if (this.IsDistinguishedIdentity) {
					this = TkQuaternion.s_identity;
					this.IsDistinguishedIdentity = false;
				}
				this._z = value;
			}
		}

		/// <summary>
		/// Gets the W component of the quaternion.
		/// </summary>
		/// 
		/// <returns>
		/// The W component of the quaternion.
		/// </returns>
		public float W {
			get {
				if (this.IsDistinguishedIdentity)
					return 1;
				else
					return this._w;
			}
			set {
				if (this.IsDistinguishedIdentity) {
					this = TkQuaternion.s_identity;
					this.IsDistinguishedIdentity = false;
				}
				this._w = value;
			}
		}

		private bool IsDistinguishedIdentity {
			get {
				return !this._isNotDistinguishedIdentity;
			}
			set {
				this._isNotDistinguishedIdentity = !value;
			}
		}

		static TkQuaternion() {
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Windows.Media.Media3D.Quaternion"/> structure.
		/// </summary>
		/// <param name="x">Value of the new <see cref="T:System.Windows.Media.Media3D.Quaternion"/>'s X coordinate.</param><param name="y">Value of the new <see cref="T:System.Windows.Media.Media3D.Quaternion"/>'s Y coordinate.</param><param name="z">Value of the new <see cref="T:System.Windows.Media.Media3D.Quaternion"/>'s Z coordinate.</param><param name="w">Value of the new <see cref="T:System.Windows.Media.Media3D.Quaternion"/>'s W coordinate.</param>
		public TkQuaternion(float x, float y, float z, float w) {
			this._x = x;
			this._y = y;
			this._z = z;
			this._w = w;
			this._isNotDistinguishedIdentity = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:System.Windows.Media.Media3D.Quaternion"/> structure.
		/// </summary>
		/// <param name="axisOfRotation"><see cref="T:System.Windows.Media.Media3D.Vertex"/> that represents the axis of rotation.</param><param name="angleInDegrees">Angle to rotate around the specified axis, in degrees.</param>
		public TkQuaternion(Vertex axisOfRotation, float angleInDegrees) {
			angleInDegrees %= 360f;
			float num = (float)(angleInDegrees * (Math.PI / 180.0));
			float length = axisOfRotation.Length;
			if (length == 0f)
				throw new InvalidOperationException("Quaternion_ZeroAxisSpecified");
			Vertex Vertex = axisOfRotation / length * Math.Sin(0.5 * num);
			this._x = Vertex.X;
			this._y = Vertex.Y;
			this._z = Vertex.Z;
			this._w = (float)Math.Cos(0.5 * num);
			this._isNotDistinguishedIdentity = true;
		}

		/// <summary>
		/// Adds the specified <see cref="T:System.Windows.Media.Media3D.Quaternion"/> values.
		/// </summary>
		/// 
		/// <returns>
		/// Quaternion that is the sum of the two specified  <see cref="T:System.Windows.Media.Media3D.Quaternion"/> values.
		/// </returns>
		/// <param name="left">First quaternion to add.</param><param name="right">Second quaternion to add.</param>
		public static TkQuaternion operator +(TkQuaternion left, TkQuaternion right) {
			if (right.IsDistinguishedIdentity) {
				if (left.IsDistinguishedIdentity)
					return new TkQuaternion(0.0f, 0.0f, 0.0f, 2.0f);
				++left._w;
				return left;
			}
			else {
				if (!left.IsDistinguishedIdentity)
					return new TkQuaternion(left._x + right._x, left._y + right._y, left._z + right._z, left._w + right._w);
				++right._w;
				return right;
			}
		}

		/// <summary>
		/// Subtracts a specified quaternion from another.
		/// </summary>
		/// 
		/// <returns>
		/// Quaternion that is the result of subtraction.
		/// </returns>
		/// <param name="left">Quaternion from which to subtract.</param><param name="right">Quaternion to subtract from the first quaternion.</param>
		public static TkQuaternion operator -(TkQuaternion left, TkQuaternion right) {
			if (right.IsDistinguishedIdentity) {
				if (left.IsDistinguishedIdentity)
					return new TkQuaternion(0.0f, 0.0f, 0.0f, 0.0f);
				--left._w;
				return left;
			}
			else if (left.IsDistinguishedIdentity)
				return new TkQuaternion(-right._x, -right._y, -right._z, 1.0f - right._w);
			else
				return new TkQuaternion(left._x - right._x, left._y - right._y, left._z - right._z, left._w - right._w);
		}

		/// <summary>
		/// Multiplies the specified quaternion by another.
		/// </summary>
		/// 
		/// <returns>
		/// Quaternion that is the product of multiplication.
		/// </returns>
		/// <param name="left">First quaternion.</param><param name="right">Second quaternion.</param>
		public static TkQuaternion operator *(TkQuaternion left, TkQuaternion right) {
			if (left.IsDistinguishedIdentity)
				return right;
			if (right.IsDistinguishedIdentity)
				return left;
			else
				return new TkQuaternion(left._w * right._x + left._x * right._w + left._y * right._z - left._z * right._y, left._w * right._y + left._y * right._w + left._z * right._x - left._x * right._z, left._w * right._z + left._z * right._w + left._x * right._y - left._y * right._x, left._w * right._w - left._x * right._x - left._y * right._y - left._z * right._z);
		}

		/// <summary>
		/// Compares two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances for exact equality.
		/// </summary>
		/// 
		/// <returns>
		/// true if the two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances are exactly equal, false otherwise.
		/// </returns>
		/// <param name="quaternion1">First Quaternion to compare.</param><param name="quaternion2">Second Quaternion to compare.</param>
		public static bool operator ==(TkQuaternion quaternion1, TkQuaternion quaternion2) {
			if (quaternion1.IsDistinguishedIdentity || quaternion2.IsDistinguishedIdentity)
				return quaternion1.IsIdentity == quaternion2.IsIdentity;
			if (quaternion1.X == quaternion2.X && quaternion1.Y == quaternion2.Y && quaternion1.Z == quaternion2.Z)
				return quaternion1.W == quaternion2.W;
			else
				return false;
		}

		/// <summary>
		/// Compares two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances for exact inequality.
		/// </summary>
		/// 
		/// <returns>
		/// true if the two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances are exactly equal, false otherwise.
		/// </returns>
		/// <param name="quaternion1">First quaternion to compare.</param><param name="quaternion2">Second quaternion to compare.</param>
		public static bool operator !=(TkQuaternion quaternion1, TkQuaternion quaternion2) {
			return !(quaternion1 == quaternion2);
		}

		/// <summary>
		/// Replaces a quaternion with its conjugate.
		/// </summary>
		public void Conjugate() {
			if (this.IsDistinguishedIdentity)
				return;
			this._x = -this._x;
			this._y = -this._y;
			this._z = -this._z;
		}

		/// <summary>
		/// Replaces the specified quaternion with its inverse
		/// </summary>
		public void Invert() {
			if (this.IsDistinguishedIdentity)
				return;
			this.Conjugate();
			float num = this._x * this._x + this._y * this._y + this._z * this._z + this._w * this._w;
			this._x = this._x / num;
			this._y = this._y / num;
			this._z = this._z / num;
			this._w = this._w / num;
		}

		/// <summary>
		/// Returns a normalized quaternion.
		/// </summary>
		public void Normalize() {
			if (this.IsDistinguishedIdentity)
				return;
			float d = this._x * this._x + this._y * this._y + this._z * this._z + this._w * this._w;
			if (d > float.MaxValue) {
				float num = 1.0f / TkQuaternion.Max(Math.Abs(this._x), Math.Abs(this._y), Math.Abs(this._z), Math.Abs(this._w));
				this._x = this._x * num;
				this._y = this._y * num;
				this._z = this._z * num;
				this._w = this._w * num;
				d = this._x * this._x + this._y * this._y + this._z * this._z + this._w * this._w;
			}
			float num1 = 1.0f / (float)Math.Sqrt(d);
			this._x = this._x * num1;
			this._y = this._y * num1;
			this._z = this._z * num1;
			this._w = this._w * num1;
		}

		/// <summary>
		/// Adds the specified quaternions.
		/// </summary>
		/// 
		/// <returns>
		/// Quaternion that is the result of addition.
		/// </returns>
		/// <param name="left">First quaternion to add.</param><param name="right">Second quaternion to add.</param>
		public static TkQuaternion Add(TkQuaternion left, TkQuaternion right) {
			return left + right;
		}

		/// <summary>
		/// Subtracts a Quaternion from another.
		/// </summary>
		/// 
		/// <returns>
		/// Quaternion that is the result of subtraction.
		/// </returns>
		/// <param name="left">Quaternion from which to subtract.</param><param name="right">Quaternion to subtract from the first quaternion.</param>
		public static TkQuaternion Subtract(TkQuaternion left, TkQuaternion right) {
			return left - right;
		}

		/// <summary>
		/// Multiplies the specified <see cref="T:System.Windows.Media.Media3D.Quaternion"/> values.
		/// </summary>
		/// 
		/// <returns>
		/// Quaternion that is the result of multiplication.
		/// </returns>
		/// <param name="left">First quaternion to multiply.</param><param name="right">Second quaternion to multiply.</param>
		public static TkQuaternion Multiply(TkQuaternion left, TkQuaternion right) {
			return left * right;
		}

		private void Scale(float scale) {
			if (this.IsDistinguishedIdentity) {
				this._w = scale;
				this.IsDistinguishedIdentity = false;
			}
			else {
				this._x = this._x * scale;
				this._y = this._y * scale;
				this._z = this._z * scale;
				this._w = this._w * scale;
			}
		}

		private float Length() {
			if (this.IsDistinguishedIdentity)
				return 1.0f;
			float d = this._x * this._x + this._y * this._y + this._z * this._z + this._w * this._w;
			if (d <= float.MaxValue)
				return (float)Math.Sqrt(d);
			float num1 = Math.Max(Math.Max(Math.Abs(this._x), Math.Abs(this._y)), Math.Max(Math.Abs(this._z), Math.Abs(this._w)));
			float num2 = this._x / num1;
			float num3 = this._y / num1;
			float num4 = this._z / num1;
			float num5 = this._w / num1;
			return (float)Math.Sqrt(num2 * num2 + num3 * num3 + num4 * num4 + num5 * num5) * num1;
		}

		/// <summary>
		/// Interpolates between two orientations using spherical linear interpolation.
		/// </summary>
		/// 
		/// <returns>
		/// <see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the orientation resulting from the interpolation.
		/// </returns>
		/// <param name="from"><see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the starting orientation.</param><param name="to"><see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the ending orientation.</param><param name="t">Interpolation coefficient.</param>
		public static TkQuaternion Slerp(TkQuaternion from, TkQuaternion to, float t) {
			return TkQuaternion.Slerp(from, to, t, true);
		}

		/// <summary>
		/// Interpolates between orientations, represented as <see cref="T:System.Windows.Media.Media3D.Quaternion"/> structures, using spherical linear interpolation.
		/// </summary>
		/// 
		/// <returns>
		/// <see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the orientation resulting from the interpolation.
		/// </returns>
		/// <param name="from"><see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the starting orientation.</param><param name="to"><see cref="T:System.Windows.Media.Media3D.Quaternion"/> that represents the ending orientation.</param><param name="t">Interpolation coefficient.</param><param name="useShortestPath">Boolean that indicates whether to compute quaternions that constitute the shortest possible arc on a four-dimensional unit sphere.</param>
		public static TkQuaternion Slerp(TkQuaternion from, TkQuaternion to, float t, bool useShortestPath) {
			if (from.IsDistinguishedIdentity)
				from._w = 1.0f;
			if (to.IsDistinguishedIdentity)
				to._w = 1.0f;
			float num1 = from.Length();
			float num2 = to.Length();
			from.Scale(1.0f / num1);
			to.Scale(1.0f / num2);
			float d = from._x * to._x + from._y * to._y + from._z * to._z + from._w * to._w;
			if (useShortestPath) {
				if (d < 0.0) {
					d = -d;
					to._x = -to._x;
					to._y = -to._y;
					to._z = -to._z;
					to._w = -to._w;
				}
			}
			else if (d < -1.0)
				d = -1.0f;
			if (d > 1.0)
				d = 1.0f;
			float num3;
			float num4;
			if (d > 0.999999) {
				num3 = 1.0f - t;
				num4 = t;
			}
			else if (d < -0.9999999999) {
				to = new TkQuaternion(-from.Y, from.X, -from.W, from.Z);
				float num5 = (float)(t * Math.PI);
				num3 = (float)Math.Cos(num5);
				num4 = (float)Math.Sin(num5);
			}
			else {
				float num5 = (float)Math.Acos(d);
				float num6 = (float)Math.Sqrt(1.0 - d * d);
				num3 = (float)Math.Sin((1.0 - t) * num5) / num6;
				num4 = (float)Math.Sin(t * num5) / num6;
			}
			float num7 = (float)(num1 * Math.Pow(num2 / num1, t));
			float num8 = num3 * num7;
			float num9 = num4 * num7;
			return new TkQuaternion(num8 * from._x + num9 * to._x, num8 * from._y + num9 * to._y, num8 * from._z + num9 * to._z, num8 * from._w + num9 * to._w);
		}

		private static float Max(float a, float b, float c, float d) {
			if (b > a)
				a = b;
			if (c > a)
				a = c;
			if (d > a)
				a = d;
			return a;
		}

		private static int GetIdentityHashCode() {
			return 0.0.GetHashCode() ^ 1.0.GetHashCode();
		}

		private static TkQuaternion GetIdentity() {
			return new TkQuaternion(0.0f, 0.0f, 0.0f, 1.0f) {
				IsDistinguishedIdentity = true
			};
		}

		/// <summary>
		/// Compares two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances for equality.
		/// </summary>
		/// 
		/// <returns>
		/// true if the two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances are exactly equal, false otherwise.
		/// </returns>
		/// <param name="quaternion1">First <see cref="T:System.Windows.Media.Media3D.Quaternion"/> to compare.</param><param name="quaternion2">Second <see cref="T:System.Windows.Media.Media3D.Quaternion"/> to compare.</param>
		public static bool Equals(TkQuaternion quaternion1, TkQuaternion quaternion2) {
			if (quaternion1.IsDistinguishedIdentity || quaternion2.IsDistinguishedIdentity)
				return quaternion1.IsIdentity == quaternion2.IsIdentity;
			if (quaternion1.X.Equals(quaternion2.X) && quaternion1.Y.Equals(quaternion2.Y) && quaternion1.Z.Equals(quaternion2.Z))
				return quaternion1.W.Equals(quaternion2.W);
			else
				return false;
		}

		/// <summary>
		/// Compares two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances for equality.
		/// </summary>
		/// 
		/// <returns>
		/// true if the two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances are exactly equal, false otherwise.
		/// </returns>
		/// <param name="o">Object with which to compare.</param>
		public override bool Equals(object o) {
			if (o == null || !(o is TkQuaternion))
				return false;
			else
				return TkQuaternion.Equals(this, (TkQuaternion)o);
		}

		/// <summary>
		/// Compares two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances for equality.
		/// </summary>
		/// 
		/// <returns>
		/// true if the two <see cref="T:System.Windows.Media.Media3D.Quaternion"/> instances are exactly equal, false otherwise.
		/// </returns>
		/// <param name="value">Quaternion with which to compare.</param>
		public bool Equals(TkQuaternion value) {
			return TkQuaternion.Equals(this, value);
		}

		/// <summary>
		/// Returns the hash code for the <see cref="T:System.Windows.Media.Media3D.Quaternion"/>.
		/// </summary>
		/// 
		/// <returns>
		/// An integer type that represents the hash code for the <see cref="T:System.Windows.Media.Media3D.Quaternion"/>.
		/// </returns>
		public override int GetHashCode() {
			if (this.IsDistinguishedIdentity)
				return TkQuaternion.c_identityHashCode;
			int hashCode1 = this.X.GetHashCode();
			float num1 = this.Y;
			int hashCode2 = num1.GetHashCode();
			int num2 = hashCode1 ^ hashCode2;
			num1 = this.Z;
			int hashCode3 = num1.GetHashCode();
			int num3 = num2 ^ hashCode3;
			num1 = this.W;
			int hashCode4 = num1.GetHashCode();
			return num3 ^ hashCode4;
		}


		/// <summary>
		/// Creates a string representation of the object.
		/// </summary>
		/// 
		/// <returns>
		/// String representation of the object.
		/// </returns>
		public override string ToString() {
			return this.ConvertToString((string)null, (IFormatProvider)null);
		}

		/// <summary>
		/// Creates a string representation of the object.
		/// </summary>
		/// 
		/// <returns>
		/// String representation of the object.
		/// </returns>
		/// <param name="provider">Culture-specific formatting information.</param>
		public string ToString(IFormatProvider provider) {
			return this.ConvertToString((string)null, provider);
		}

		internal string ConvertToString(string format, IFormatProvider provider) {
			if (this.IsIdentity)
				return "Identity";

			return "{x:" + _x + ", y:" + _y + ", z:" + _z + ", w:" + _w + "}";
		}
	}
}
