using System;
using System.Runtime.InteropServices;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable NonReadonlyFieldInGetHashCode
namespace GRF.Graphics {
	/// <summary>
	/// Represents a TkQuaternion.
	/// </summary>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct TkQuaternion : IEquatable<TkQuaternion> {
		/// <summary>
		/// The X, Y and Z components of this instance.
		/// </summary>
		public TkVector3 Xyz;

		/// <summary>
		/// The W component of this instance.
		/// </summary>
		public float W;

		/// <summary>
		/// Construct a new TkQuaternion from vector and w components
		/// </summary>
		/// <param name="v">The vector part</param>
		/// <param name="w">The w part</param>
		public TkQuaternion(TkVector3 v, float w) {
			Xyz = v;
			W = w;
		}

		/// <summary>
		/// Construct a new TkQuaternion
		/// </summary>
		/// <param name="x">The x component</param>
		/// <param name="y">The y component</param>
		/// <param name="z">The z component</param>
		/// <param name="w">The w component</param>
		public TkQuaternion(float x, float y, float z, float w)
			: this(new TkVector3(x, y, z), w) { }

		//public TkQuaternion(ref Matrix3 matrix) {
		//	double scale = Math.Pow(matrix.Determinant, 1.0d / 3.0d);
		//	float x, y, z;
		//
		//	w = (float)(Math.Sqrt(Math.Max(0, scale + matrix[0, 0] + matrix[1, 1] + matrix[2, 2])) / 2);
		//	x = (float)(Math.Sqrt(Math.Max(0, scale + matrix[0, 0] - matrix[1, 1] - matrix[2, 2])) / 2);
		//	y = (float)(Math.Sqrt(Math.Max(0, scale - matrix[0, 0] + matrix[1, 1] - matrix[2, 2])) / 2);
		//	z = (float)(Math.Sqrt(Math.Max(0, scale - matrix[0, 0] - matrix[1, 1] + matrix[2, 2])) / 2);
		//
		//	xyz = new TkVector3(x, y, z);
		//
		//	if (matrix[2, 1] - matrix[1, 2] < 0) X = -X;
		//	if (matrix[0, 2] - matrix[2, 0] < 0) Y = -Y;
		//	if (matrix[1, 0] - matrix[0, 1] < 0) Z = -Z;
		//}

        /// <summary>
        /// Initializes a new instance of the <see cref="Quaternion"/> struct from given Euler angles in radians.
        /// The rotations will get applied in following order:
        /// 1. around X axis, 2. around Y axis, 3. around Z axis.
        /// </summary>
        /// <param name="rotationX">Counterclockwise rotation around X axis in radian.</param>
        /// <param name="rotationY">Counterclockwise rotation around Y axis in radian.</param>
        /// <param name="rotationZ">Counterclockwise rotation around Z axis in radian.</param>
        public TkQuaternion(float rotationX, float rotationY, float rotationZ)
        {
            rotationX *= 0.5f;
            rotationY *= 0.5f;
            rotationZ *= 0.5f;

            var c1 = (float)Math.Cos(rotationX);
            var c2 = (float)Math.Cos(rotationY);
            var c3 = (float)Math.Cos(rotationZ);
            var s1 = (float)Math.Sin(rotationX);
            var s2 = (float)Math.Sin(rotationY);
            var s3 = (float)Math.Sin(rotationZ);

            W = (c1 * c2 * c3) - (s1 * s2 * s3);
            Xyz.X = (s1 * c2 * c3) + (c1 * s2 * s3);
            Xyz.Y = (c1 * s2 * c3) - (s1 * c2 * s3);
            Xyz.Z = (c1 * c2 * s3) + (s1 * s2 * c3);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TkQuaternion"/> struct from given Euler angles in radians.
        /// The rotations will get applied in following order:
        /// 1. Around X, 2. Around Y, 3. Around Z.
        /// </summary>
        /// <param name="eulerAngles">The counterclockwise euler angles as a Vector3.</param>
        public TkQuaternion(TkVector3 eulerAngles)
            : this(eulerAngles.X, eulerAngles.Y, eulerAngles.Z)
        {
        }

		/// <summary>
        /// Gets or sets the X component of this instance.
        /// </summary>
        public float X
        {
			get { return Xyz.X; }
			set { Xyz.X = value; }
		}

        /// <summary>
        /// Gets or sets the Y component of this instance.
        /// </summary>
        public float Y
        {
            get { return Xyz.Y; }
			set { Xyz.Y = value; }
        }

        /// <summary>
        /// Gets or sets the Z component of this instance.
        /// </summary>
        public float Z
        {
			get { return Xyz.Z; }
			set { Xyz.Z = value; }
        }

		/// <summary>
		/// Convert the current quaternion to axis angle representation
		/// </summary>
		/// <param name="axis">The resultant axis</param>
		/// <param name="angle">The resultant angle</param>
		public void ToAxisAngle(out TkVector3 axis, out float angle) {
			TkVector4 result = ToAxisAngle();
			axis = result.Xyz;
			angle = result.W;
		}

		/// <summary>
		/// Convert this instance to an axis-angle representation.
		/// </summary>
		/// <returns>A TkVector4 that is the axis-angle representation of this quaternion.</returns>
		public TkVector4 ToAxisAngle() {
			TkQuaternion q = this;
			if (Math.Abs(q.W) > 1.0f)
				q.Normalize();

			TkVector4 result = new TkVector4();

			result.W = 2.0f * (float)Math.Acos(q.W); // angle
			float den = (float)Math.Sqrt(1.0 - q.W * q.W);
			if (den > 0.0001f) {
				result.Xyz = q.Xyz / den;
			}
			else {
				// This occurs when the angle is zero. 
				// Not a problem: just set an arbitrary normalized axis.
				result.Xyz = TkVector3.UnitX;
			}

			return result;
		}

		/// <summary>
		/// Convert the current quaternion to Euler angle representation.
		/// </summary>
		/// <param name="angles">The Euler angles in radians.</param>
		public void ToEulerAngles(out TkVector3 angles) {
			angles = ToEulerAngles();
		}

		/// <summary>
		/// Convert this instance to an Euler angle representation.
		/// </summary>
		/// <returns>The Euler angles in radians.</returns>
		public TkVector3 ToEulerAngles() {
			/*
			reference
			http://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
			http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/
			*/

			var q = this;

			TkVector3 eulerAngles;

			// Threshold for the singularities found at the north/south poles.
			const float SINGULARITY_THRESHOLD = 0.4999995f;

			var sqw = q.W * q.W;
			var sqx = q.X * q.X;
			var sqy = q.Y * q.Y;
			var sqz = q.Z * q.Z;
			var unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
			var singularityTest = (q.X * q.Z) + (q.W * q.Y);

			if (singularityTest > SINGULARITY_THRESHOLD * unit) {
				eulerAngles.Z = 2 * (float)Math.Atan2(q.X, q.W);
				eulerAngles.Y = MathHelper.PiOver2;
				eulerAngles.X = 0;
			}
			else if (singularityTest < -SINGULARITY_THRESHOLD * unit) {
				eulerAngles.Z = -2 * (float)Math.Atan2(q.X, q.W);
				eulerAngles.Y = -MathHelper.PiOver2;
				eulerAngles.X = 0;
			}
			else {
				eulerAngles.Z = (float)Math.Atan2(2 * ((q.W * q.Z) - (q.X * q.Y)), sqw + sqx - sqy - sqz);
				eulerAngles.Y = (float)Math.Asin(2 * singularityTest / unit);
				eulerAngles.X = (float)Math.Atan2(2 * ((q.W * q.X) - (q.Y * q.Z)), sqw - sqx - sqy + sqz);
			}

			return eulerAngles;
		}

		/// <summary>
		/// Gets the length (magnitude) of the quaternion.
		/// </summary>
		/// <seealso cref="LengthSquared"/>
		public float Length {
			get {
				return (float)Math.Sqrt(W * W + Xyz.LengthSquared);
			}
		}

		/// <summary>
		/// Gets the square of the quaternion length (magnitude).
		/// </summary>
		public float LengthSquared {
			get {
				return W * W + Xyz.LengthSquared;
			}
		}

		/// <summary>
		/// Scales the TkQuaternion to unit length.
		/// </summary>
		public void Normalize() {
			float scale = 1.0f / this.Length;
			Xyz *= scale;
			W *= scale;
		}

		/// <summary>
		/// Convert this quaternion to its conjugate
		/// </summary>
		public void Conjugate() {
			Xyz = -Xyz;
		}

		/// <summary>
		/// Defines the identity quaternion.
		/// </summary>
		public static TkQuaternion Identity = new TkQuaternion(0, 0, 0, 1);

		/// <summary>
		/// Add two quaternions
		/// </summary>
		/// <param name="left">The first operand</param>
		/// <param name="right">The second operand</param>
		/// <returns>The result of the addition</returns>
		public static TkQuaternion Add(TkQuaternion left, TkQuaternion right) {
			return new TkQuaternion(
				left.Xyz + right.Xyz,
				left.W + right.W);
		}

		/// <summary>
		/// Add two quaternions
		/// </summary>
		/// <param name="left">The first operand</param>
		/// <param name="right">The second operand</param>
		/// <param name="result">The result of the addition</param>
		public static void Add(ref TkQuaternion left, ref TkQuaternion right, out TkQuaternion result) {
			result = new TkQuaternion(
				left.Xyz + right.Xyz,
				left.W + right.W);
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <returns>The result of the operation.</returns>
		public static TkQuaternion Sub(TkQuaternion left, TkQuaternion right) {
			return new TkQuaternion(
				left.Xyz - right.Xyz,
				left.W - right.W);
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The left instance.</param>
		/// <param name="right">The right instance.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Sub(ref TkQuaternion left, ref TkQuaternion right, out TkQuaternion result) {
			result = new TkQuaternion(
				left.Xyz - right.Xyz,
				left.W - right.W);
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		[Obsolete("Use Multiply instead.")]
		public static TkQuaternion Mult(TkQuaternion left, TkQuaternion right) {
			return new TkQuaternion(
				right.W * left.Xyz + left.W * right.Xyz + TkVector3.Cross(left.Xyz, right.Xyz),
				left.W * right.W - TkVector3.Dot(left.Xyz, right.Xyz));
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <param name="result">A new instance containing the result of the calculation.</param>
		[Obsolete("Use Multiply instead.")]
		public static void Mult(ref TkQuaternion left, ref TkQuaternion right, out TkQuaternion result) {
			result = new TkQuaternion(
				right.W * left.Xyz + left.W * right.Xyz + TkVector3.Cross(left.Xyz, right.Xyz),
				left.W * right.W - TkVector3.Dot(left.Xyz, right.Xyz));
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static TkQuaternion Multiply(TkQuaternion left, TkQuaternion right) {
			TkQuaternion result;
			Multiply(ref left, ref right, out result);
			return result;
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <param name="result">A new instance containing the result of the calculation.</param>
		public static void Multiply(ref TkQuaternion left, ref TkQuaternion right, out TkQuaternion result) {
			result = new TkQuaternion(
				right.W * left.Xyz + left.W * right.Xyz + TkVector3.Cross(left.Xyz, right.Xyz),
				left.W * right.W - TkVector3.Dot(left.Xyz, right.Xyz));
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <param name="result">A new instance containing the result of the calculation.</param>
		public static void Multiply(ref TkQuaternion quaternion, float scale, out TkQuaternion result) {
			result = new TkQuaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		[Obsolete("Use the overload without the 'ref float scale'.")]
		public static void Multiply(ref TkQuaternion quaternion, ref float scale, out TkQuaternion result) {
			result = new TkQuaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static TkQuaternion Multiply(TkQuaternion quaternion, float scale) {
			return new TkQuaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		/// <summary>
		/// Get the conjugate of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion</param>
		/// <returns>The conjugate of the given quaternion</returns>
		public static TkQuaternion Conjugate(TkQuaternion q) {
			return new TkQuaternion(-q.Xyz, q.W);
		}

		/// <summary>
		/// Get the conjugate of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion</param>
		/// <param name="result">The conjugate of the given quaternion</param>
		public static void Conjugate(ref TkQuaternion q, out TkQuaternion result) {
			result = new TkQuaternion(-q.Xyz, q.W);
		}

		/// <summary>
		/// Get the inverse of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion to invert</param>
		/// <returns>The inverse of the given quaternion</returns>
		public static TkQuaternion Invert(TkQuaternion q) {
			TkQuaternion result;
			Invert(ref q, out result);
			return result;
		}

		/// <summary>
		/// Get the inverse of the given quaternion
		/// </summary>
		/// <param name="q">The quaternion to invert</param>
		/// <param name="result">The inverse of the given quaternion</param>
		public static void Invert(ref TkQuaternion q, out TkQuaternion result) {
			float lengthSq = q.LengthSquared;
			if (lengthSq != 0.0) {
				float i = 1.0f / lengthSq;
				result = new TkQuaternion(q.Xyz * -i, q.W * i);
			}
			else {
				result = q;
			}
		}

		/// <summary>
		/// Scale the given quaternion to unit length
		/// </summary>
		/// <param name="q">The quaternion to normalize</param>
		/// <returns>The normalized quaternion</returns>
		public static TkQuaternion Normalize(TkQuaternion q) {
			TkQuaternion result;
			Normalize(ref q, out result);
			return result;
		}

		/// <summary>
		/// Scale the given quaternion to unit length
		/// </summary>
		/// <param name="q">The quaternion to normalize</param>
		/// <param name="result">The normalized quaternion</param>
		public static void Normalize(ref TkQuaternion q, out TkQuaternion result) {
			float scale = 1.0f / q.Length;
			result = new TkQuaternion(q.Xyz * scale, q.W * scale);
		}

		/// <summary>
		/// Build a quaternion from the given axis and angle
		/// </summary>
		/// <param name="axis">The axis to rotate about</param>
		/// <param name="angle">The rotation angle in radians</param>
		/// <returns></returns>
		public static TkQuaternion FromAxisAngle(TkVector3 axis, float angle) {
			if (axis.LengthSquared == 0.0f)
				return Identity;

			TkQuaternion result = Identity;

			angle *= 0.5f;
			axis.Normalize();
			result.Xyz = axis * (float)Math.Sin(angle);
			result.W = (float)Math.Cos(angle);

			return Normalize(result);
		}

		/// <summary>
		/// Do Spherical linear interpolation between two quaternions 
		/// </summary>
		/// <param name="q1">The first quaternion</param>
		/// <param name="q2">The second quaternion</param>
		/// <param name="blend">The blend factor</param>
		/// <returns>A smooth blend between the given quaternions</returns>
		public static TkQuaternion Slerp(TkQuaternion q1, TkQuaternion q2, float blend) {
			// if either input is zero, return the other.
			if (q1.LengthSquared == 0.0f) {
				if (q2.LengthSquared == 0.0f) {
					return Identity;
				}
				return q2;
			}
			else if (q2.LengthSquared == 0.0f) {
				return q1;
			}


			float cosHalfAngle = q1.W * q2.W + TkVector3.Dot(q1.Xyz, q2.Xyz);

			if (cosHalfAngle >= 1.0f || cosHalfAngle <= -1.0f) {
				// angle = 0.0f, so just return one input.
				return q1;
			}
			else if (cosHalfAngle < 0.0f) {
				q2.Xyz = -q2.Xyz;
				q2.W = -q2.W;
				cosHalfAngle = -cosHalfAngle;
			}

			float blendA;
			float blendB;
			if (cosHalfAngle < 0.99f) {
				// do proper slerp for big angles
				float halfAngle = (float)Math.Acos(cosHalfAngle);
				float sinHalfAngle = (float)Math.Sin(halfAngle);
				float oneOverSinHalfAngle = 1.0f / sinHalfAngle;
				blendA = (float)Math.Sin(halfAngle * (1.0f - blend)) * oneOverSinHalfAngle;
				blendB = (float)Math.Sin(halfAngle * blend) * oneOverSinHalfAngle;
			}
			else {
				// do lerp if angle is really small.
				blendA = 1.0f - blend;
				blendB = blend;
			}

			TkQuaternion result = new TkQuaternion(blendA * q1.Xyz + blendB * q2.Xyz, blendA * q1.W + blendB * q2.W);
			if (result.LengthSquared > 0.0f)
				return Normalize(result);
			else
				return Identity;
		}

		/// <summary>
		/// Adds two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkQuaternion operator +(TkQuaternion left, TkQuaternion right) {
			left.Xyz += right.Xyz;
			left.W += right.W;
			return left;
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkQuaternion operator -(TkQuaternion left, TkQuaternion right) {
			left.Xyz -= right.Xyz;
			left.W -= right.W;
			return left;
		}

		/// <summary>
		/// Multiplies two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkQuaternion operator *(TkQuaternion left, TkQuaternion right) {
			Multiply(ref left, ref right, out left);
			return left;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static TkQuaternion operator *(TkQuaternion quaternion, float scale) {
			Multiply(ref quaternion, scale, out quaternion);
			return quaternion;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="quaternion">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>A new instance containing the result of the calculation.</returns>
		public static TkQuaternion operator *(float scale, TkQuaternion quaternion) {
			return new TkQuaternion(quaternion.X * scale, quaternion.Y * scale, quaternion.Z * scale, quaternion.W * scale);
		}

		/// <summary>
		/// Compares two instances for equality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left equals right; false otherwise.</returns>
		public static bool operator ==(TkQuaternion left, TkQuaternion right) {
			return left.Equals(right);
		}

		/// <summary>
		/// Compares two instances for inequality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left does not equal right; false otherwise.</returns>
		public static bool operator !=(TkQuaternion left, TkQuaternion right) {
			return !left.Equals(right);
		}

		#region public override string ToString()

		/// <summary>
		/// Returns a System.String that represents the current TkQuaternion.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return String.Format("V: {0}, W: {1}", Xyz, W);
		}

		#endregion

		#region public override bool Equals (object o)

		/// <summary>
		/// Compares this object instance to another object for equality. 
		/// </summary>
		/// <param name="other">The other object to be used in the comparison.</param>
		/// <returns>True if both objects are Quaternions of equal value. Otherwise it returns false.</returns>
		public override bool Equals(object other) {
			if (other is TkQuaternion == false) return false;
			return this == (TkQuaternion)other;
		}

		#endregion

		#region public override int GetHashCode ()

		/// <summary>
		/// Provides the hash code for this object. 
		/// </summary>
		/// <returns>A hash code formed from the bitwise XOR of this objects members.</returns>
		public override int GetHashCode() {
			return Xyz.GetHashCode() ^ W.GetHashCode();
		}

		#endregion

		#region IEquatable<TkQuaternion> Members

		/// <summary>
		/// Compares this TkQuaternion instance to another TkQuaternion for equality. 
		/// </summary>
		/// <param name="other">The other TkQuaternion to be used in the comparison.</param>
		/// <returns>True if both instances are equal; false otherwise.</returns>
		public bool Equals(TkQuaternion other) {
			return Xyz == other.Xyz && W == other.W;
		}

		#endregion
	}
}