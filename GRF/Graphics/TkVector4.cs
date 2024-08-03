#region --- License ---
/*
Copyright (c) 2006 - 2008 The Open Toolkit library.

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
#endregion

using System;
using System.Runtime.InteropServices;

namespace GRF.Graphics {
	/// <summary>Represents a 4D vector using four single-precision floating-point numbers.</summary>
	/// <remarks>
	/// The Vector4 structure is suitable for interoperation with unmanaged code requiring four consecutive floats.
	/// </remarks>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct TkVector4 : IEquatable<TkVector4> {
		#region Fields

		/// <summary>
		/// The X component of the Vector4.
		/// </summary>
		public float X;

		/// <summary>
		/// The Y component of the Vector4.
		/// </summary>
		public float Y;

		/// <summary>
		/// The Z component of the Vector4.
		/// </summary>
		public float Z;

		/// <summary>
		/// The W component of the Vector4.
		/// </summary>
		public float W;

		/// <summary>
		/// Defines a unit-length Vector4 that points towards the X-axis.
		/// </summary>
		public static TkVector4 UnitX = new TkVector4(1, 0, 0, 0);

		/// <summary>
		/// Defines a unit-length Vector4 that points towards the Y-axis.
		/// </summary>
		public static TkVector4 UnitY = new TkVector4(0, 1, 0, 0);

		/// <summary>
		/// Defines a unit-length Vector4 that points towards the Z-axis.
		/// </summary>
		public static TkVector4 UnitZ = new TkVector4(0, 0, 1, 0);

		/// <summary>
		/// Defines a unit-length Vector4 that points towards the W-axis.
		/// </summary>
		public static TkVector4 UnitW = new TkVector4(0, 0, 0, 1);

		/// <summary>
		/// Defines a zero-length Vector4.
		/// </summary>
		public static TkVector4 Zero = new TkVector4(0, 0, 0, 0);

		/// <summary>
		/// Defines an instance with all components set to 1.
		/// </summary>
		public static readonly TkVector4 One = new TkVector4(1, 1, 1, 1);

		/// <summary>
		/// Defines the size of the Vector4 struct in bytes.
		/// </summary>
		public static readonly int SizeInBytes = Marshal.SizeOf(new TkVector4());

		#endregion

		#region Constructors

		/// <summary>
		/// Constructs a new instance.
		/// </summary>
		/// <param name="value">The value that will initialize this instance.</param>
		public TkVector4(float value) {
			X = value;
			Y = value;
			Z = value;
			W = value;
		}

		/// <summary>
		/// Constructs a new Vector4.
		/// </summary>
		/// <param name="x">The x component of the Vector4.</param>
		/// <param name="y">The y component of the Vector4.</param>
		/// <param name="z">The z component of the Vector4.</param>
		/// <param name="w">The w component of the Vector4.</param>
		public TkVector4(float x, float y, float z, float w) {
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		/// <summary>
		/// Constructs a new Vector4 from the given TkVector2.
		/// </summary>
		/// <param name="v">The TkVector2 to copy components from.</param>
		public TkVector4(TkVector2 v) {
			X = v.X;
			Y = v.Y;
			Z = 0.0f;
			W = 0.0f;
		}

		/// <summary>
		/// Constructs a new Vector4 from the given TkVector3.
		/// The w component is initialized to 0.
		/// </summary>
		/// <param name="v">The TkVector3 to copy components from.</param>
		/// <remarks><seealso cref="TkVector4(TkVector3, float)"/></remarks>
		public TkVector4(TkVector3 v) {
			X = v.X;
			Y = v.Y;
			Z = v.Z;
			W = 0.0f;
		}

		/// <summary>
		/// Constructs a new Vector4 from the specified TkVector3 and w component.
		/// </summary>
		/// <param name="v">The TkVector3 to copy components from.</param>
		/// <param name="w">The w component of the new Vector4.</param>
		public TkVector4(TkVector3 v, float w) {
			X = v.X;
			Y = v.Y;
			Z = v.Z;
			W = w;
		}

		/// <summary>
		/// Constructs a new Vector4 from the given Vector4.
		/// </summary>
		/// <param name="v">The Vector4 to copy components from.</param>
		public TkVector4(TkVector4 v) {
			X = v.X;
			Y = v.Y;
			Z = v.Z;
			W = v.W;
		}

		#endregion

		#region Public Members

		#region Instance

		#region public void Add()

		/// <summary>Add the Vector passed as parameter to this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Add() method instead.")]
		public void Add(TkVector4 right) {
			X += right.X;
			Y += right.Y;
			Z += right.Z;
			W += right.W;
		}

		/// <summary>Add the Vector passed as parameter to this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Add() method instead.")]
		public void Add(ref TkVector4 right) {
			X += right.X;
			Y += right.Y;
			Z += right.Z;
			W += right.W;
		}

		#endregion public void Add()

		#region public void Sub()

		/// <summary>Subtract the Vector passed as parameter from this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Subtract() method instead.")]
		public void Sub(TkVector4 right) {
			X -= right.X;
			Y -= right.Y;
			Z -= right.Z;
			W -= right.W;
		}

		/// <summary>Subtract the Vector passed as parameter from this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Subtract() method instead.")]
		public void Sub(ref TkVector4 right) {
			X -= right.X;
			Y -= right.Y;
			Z -= right.Z;
			W -= right.W;
		}

		#endregion public void Sub()

		#region public void Mult()

		/// <summary>Multiply this instance by a scalar.</summary>
		/// <param name="f">Scalar operand.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Mult(float f) {
			X *= f;
			Y *= f;
			Z *= f;
			W *= f;
		}

		#endregion public void Mult()

		#region public void Div()

		/// <summary>Divide this instance by a scalar.</summary>
		/// <param name="f">Scalar operand.</param>
		[Obsolete("Use static Divide() method instead.")]
		public void Div(float f) {
			float mult = 1.0f / f;
			X *= mult;
			Y *= mult;
			Z *= mult;
			W *= mult;
		}

		#endregion public void Div()

		#region public float Length

		/// <summary>
		/// Gets the length (magnitude) of the vector.
		/// </summary>
		/// <see cref="LengthFast"/>
		/// <seealso cref="LengthSquared"/>
		public float Length {
			get {
				return (float)Math.Sqrt(X * X + Y * Y + Z * Z + W * W);
			}
		}

		#endregion

		#region public float LengthFast

		/// <summary>
		/// Gets an approximation of the vector length (magnitude).
		/// </summary>
		/// <remarks>
		/// This property uses an approximation of the square root function to calculate vector magnitude, with
		/// an upper error bound of 0.001.
		/// </remarks>
		/// <see cref="Length"/>
		/// <seealso cref="LengthSquared"/>
		public float LengthFast {
			get {
				return 1.0f / MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z + W * W);
			}
		}

		#endregion

		#region public float LengthSquared

		/// <summary>
		/// Gets the square of the vector length (magnitude).
		/// </summary>
		/// <remarks>
		/// This property avoids the costly square root operation required by the Length property. This makes it more suitable
		/// for comparisons.
		/// </remarks>
		/// <see cref="Length"/>
		/// <seealso cref="LengthFast"/>
		public float LengthSquared {
			get {
				return X * X + Y * Y + Z * Z + W * W;
			}
		}

		#endregion

		#region public void Normalize()

		/// <summary>
		/// Returns a copy of the Vector3 scaled to unit length.
		/// </summary>
		/// <returns>The normalized copy.</returns>
		public TkVector4 Normalized() {
			var v = this;
			v.Normalize();
			return v;
		}

		/// <summary>
		/// Scales the Vector4 to unit length.
		/// </summary>
		public void Normalize() {
			float scale = 1.0f / Length;
			X *= scale;
			Y *= scale;
			Z *= scale;
			W *= scale;
		}

		#endregion

		#region public void NormalizeFast()

		/// <summary>
		/// Scales the Vector4 to approximately unit length.
		/// </summary>
		public void NormalizeFast() {
			float scale = MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z + W * W);
			X *= scale;
			Y *= scale;
			Z *= scale;
			W *= scale;
		}

		#endregion

		#region public void Scale()

		/// <summary>
		/// Scales the current Vector4 by the given amounts.
		/// </summary>
		/// <param name="sx">The scale of the X component.</param>
		/// <param name="sy">The scale of the Y component.</param>
		/// <param name="sz">The scale of the Z component.</param>
		/// <param name="sw">The scale of the Z component.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(float sx, float sy, float sz, float sw) {
			X = X * sx;
			Y = Y * sy;
			Z = Z * sz;
			W = W * sw;
		}

		/// <summary>Scales this instance by the given parameter.</summary>
		/// <param name="scale">The scaling of the individual components.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(TkVector4 scale) {
			X *= scale.X;
			Y *= scale.Y;
			Z *= scale.Z;
			W *= scale.W;
		}

		/// <summary>Scales this instance by the given parameter.</summary>
		/// <param name="scale">The scaling of the individual components.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(ref TkVector4 scale) {
			X *= scale.X;
			Y *= scale.Y;
			Z *= scale.Z;
			W *= scale.W;
		}

		#endregion public void Scale()

		#endregion

		#region Static

		#region Obsolete

		#region Sub

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>Result of subtraction</returns>
		public static TkVector4 Sub(TkVector4 a, TkVector4 b) {
			a.X -= b.X;
			a.Y -= b.Y;
			a.Z -= b.Z;
			a.W -= b.W;
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">Result of subtraction</param>
		public static void Sub(ref TkVector4 a, ref TkVector4 b, out TkVector4 result) {
			result.X = a.X - b.X;
			result.Y = a.Y - b.Y;
			result.Z = a.Z - b.Z;
			result.W = a.W - b.W;
		}

		#endregion

		#region Mult

		/// <summary>
		/// Multiply a vector and a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <returns>Result of the multiplication</returns>
		public static TkVector4 Mult(TkVector4 a, float f) {
			a.X *= f;
			a.Y *= f;
			a.Z *= f;
			a.W *= f;
			return a;
		}

		/// <summary>
		/// Multiply a vector and a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <param name="result">Result of the multiplication</param>
		public static void Mult(ref TkVector4 a, float f, out TkVector4 result) {
			result.X = a.X * f;
			result.Y = a.Y * f;
			result.Z = a.Z * f;
			result.W = a.W * f;
		}

		#endregion

		#region Div

		/// <summary>
		/// Divide a vector by a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <returns>Result of the division</returns>
		public static TkVector4 Div(TkVector4 a, float f) {
			float mult = 1.0f / f;
			a.X *= mult;
			a.Y *= mult;
			a.Z *= mult;
			a.W *= mult;
			return a;
		}

		/// <summary>
		/// Divide a vector by a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <param name="result">Result of the division</param>
		public static void Div(ref TkVector4 a, float f, out TkVector4 result) {
			float mult = 1.0f / f;
			result.X = a.X * mult;
			result.Y = a.Y * mult;
			result.Z = a.Z * mult;
			result.W = a.W * mult;
		}

		#endregion

		#endregion

		#region Add

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">Left operand.</param>
		/// <param name="b">Right operand.</param>
		/// <returns>Result of operation.</returns>
		public static TkVector4 Add(TkVector4 a, TkVector4 b) {
			Add(ref a, ref b, out a);
			return a;
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">Left operand.</param>
		/// <param name="b">Right operand.</param>
		/// <param name="result">Result of operation.</param>
		public static void Add(ref TkVector4 a, ref TkVector4 b, out TkVector4 result) {
			result = new TkVector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
		}

		#endregion

		#region Subtract

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>Result of subtraction</returns>
		public static TkVector4 Subtract(TkVector4 a, TkVector4 b) {
			Subtract(ref a, ref b, out a);
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">Result of subtraction</param>
		public static void Subtract(ref TkVector4 a, ref TkVector4 b, out TkVector4 result) {
			result = new TkVector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
		}

		#endregion

		#region Multiply

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector4 Multiply(TkVector4 vector, float scale) {
			Multiply(ref vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(ref TkVector4 vector, float scale, out TkVector4 result) {
			result = new TkVector4(vector.X * scale, vector.Y * scale, vector.Z * scale, vector.W * scale);
		}

		/// <summary>
		/// Multiplies a vector by the components a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector4 Multiply(TkVector4 vector, TkVector4 scale) {
			Multiply(ref vector, ref scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(ref TkVector4 vector, ref TkVector4 scale, out TkVector4 result) {
			result = new TkVector4(vector.X * scale.X, vector.Y * scale.Y, vector.Z * scale.Z, vector.W * scale.W);
		}

		#endregion

		#region Divide

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector4 Divide(TkVector4 vector, float scale) {
			Divide(ref vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(ref TkVector4 vector, float scale, out TkVector4 result) {
			Multiply(ref vector, 1 / scale, out result);
		}

		/// <summary>
		/// Divides a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector4 Divide(TkVector4 vector, TkVector4 scale) {
			Divide(ref vector, ref scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divide a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(ref TkVector4 vector, ref TkVector4 scale, out TkVector4 result) {
			result = new TkVector4(vector.X / scale.X, vector.Y / scale.Y, vector.Z / scale.Z, vector.W / scale.W);
		}

		#endregion

		#region Min

		/// <summary>
		/// Calculate the component-wise minimum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>The component-wise minimum</returns>
		public static TkVector4 Min(TkVector4 a, TkVector4 b) {
			a.X = a.X < b.X ? a.X : b.X;
			a.Y = a.Y < b.Y ? a.Y : b.Y;
			a.Z = a.Z < b.Z ? a.Z : b.Z;
			a.W = a.W < b.W ? a.W : b.W;
			return a;
		}

		/// <summary>
		/// Calculate the component-wise minimum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">The component-wise minimum</param>
		public static void Min(ref TkVector4 a, ref TkVector4 b, out TkVector4 result) {
			result.X = a.X < b.X ? a.X : b.X;
			result.Y = a.Y < b.Y ? a.Y : b.Y;
			result.Z = a.Z < b.Z ? a.Z : b.Z;
			result.W = a.W < b.W ? a.W : b.W;
		}

		#endregion

		#region Max

		/// <summary>
		/// Calculate the component-wise maximum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>The component-wise maximum</returns>
		public static TkVector4 Max(TkVector4 a, TkVector4 b) {
			a.X = a.X > b.X ? a.X : b.X;
			a.Y = a.Y > b.Y ? a.Y : b.Y;
			a.Z = a.Z > b.Z ? a.Z : b.Z;
			a.W = a.W > b.W ? a.W : b.W;
			return a;
		}

		/// <summary>
		/// Calculate the component-wise maximum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">The component-wise maximum</param>
		public static void Max(ref TkVector4 a, ref TkVector4 b, out TkVector4 result) {
			result.X = a.X > b.X ? a.X : b.X;
			result.Y = a.Y > b.Y ? a.Y : b.Y;
			result.Z = a.Z > b.Z ? a.Z : b.Z;
			result.W = a.W > b.W ? a.W : b.W;
		}

		#endregion

		#region Clamp

		/// <summary>
		/// Clamp a vector to the given minimum and maximum vectors
		/// </summary>
		/// <param name="vec">Input vector</param>
		/// <param name="min">Minimum vector</param>
		/// <param name="max">Maximum vector</param>
		/// <returns>The clamped vector</returns>
		public static TkVector4 Clamp(TkVector4 vec, TkVector4 min, TkVector4 max) {
			vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			vec.Z = vec.X < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
			vec.W = vec.Y < min.W ? min.W : vec.W > max.W ? max.W : vec.W;
			return vec;
		}

		/// <summary>
		/// Clamp a vector to the given minimum and maximum vectors
		/// </summary>
		/// <param name="vec">Input vector</param>
		/// <param name="min">Minimum vector</param>
		/// <param name="max">Maximum vector</param>
		/// <param name="result">The clamped vector</param>
		public static void Clamp(ref TkVector4 vec, ref TkVector4 min, ref TkVector4 max, out TkVector4 result) {
			result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			result.Z = vec.X < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
			result.W = vec.Y < min.W ? min.W : vec.W > max.W ? max.W : vec.W;
		}

		#endregion

		#region Normalize

		/// <summary>
		/// Scale a vector to unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <returns>The normalized vector</returns>
		public static TkVector4 Normalize(TkVector4 vec) {
			float scale = 1.0f / vec.Length;
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			vec.W *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <param name="result">The normalized vector</param>
		public static void Normalize(ref TkVector4 vec, out TkVector4 result) {
			float scale = 1.0f / vec.Length;
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
			result.Z = vec.Z * scale;
			result.W = vec.W * scale;
		}

		#endregion

		#region NormalizeFast

		/// <summary>
		/// Scale a vector to approximately unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <returns>The normalized vector</returns>
		public static TkVector4 NormalizeFast(TkVector4 vec) {
			float scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			vec.W *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to approximately unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <param name="result">The normalized vector</param>
		public static void NormalizeFast(ref TkVector4 vec, out TkVector4 result) {
			float scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z + vec.W * vec.W);
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
			result.Z = vec.Z * scale;
			result.W = vec.W * scale;
		}

		#endregion

		#region Dot

		/// <summary>
		/// Calculate the dot product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <returns>The dot product of the two inputs</returns>
		public static float Dot(TkVector4 left, TkVector4 right) {
			return left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
		}

		/// <summary>
		/// Calculate the dot product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <param name="result">The dot product of the two inputs</param>
		public static void Dot(ref TkVector4 left, ref TkVector4 right, out float result) {
			result = left.X * right.X + left.Y * right.Y + left.Z * right.Z + left.W * right.W;
		}

		#endregion

		#region Lerp

		/// <summary>
		/// Returns a new Vector that is the linear blend of the 2 given Vectors
		/// </summary>
		/// <param name="a">First input vector</param>
		/// <param name="b">Second input vector</param>
		/// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
		/// <returns>a when blend=0, b when blend=1, and a linear combination otherwise</returns>
		public static TkVector4 Lerp(TkVector4 a, TkVector4 b, float blend) {
			a.X = blend * (b.X - a.X) + a.X;
			a.Y = blend * (b.Y - a.Y) + a.Y;
			a.Z = blend * (b.Z - a.Z) + a.Z;
			a.W = blend * (b.W - a.W) + a.W;
			return a;
		}

		/// <summary>
		/// Returns a new Vector that is the linear blend of the 2 given Vectors
		/// </summary>
		/// <param name="a">First input vector</param>
		/// <param name="b">Second input vector</param>
		/// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
		/// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
		public static void Lerp(ref TkVector4 a, ref TkVector4 b, float blend, out TkVector4 result) {
			result.X = blend * (b.X - a.X) + a.X;
			result.Y = blend * (b.Y - a.Y) + a.Y;
			result.Z = blend * (b.Z - a.Z) + a.Z;
			result.W = blend * (b.W - a.W) + a.W;
		}

		#endregion

		#region Barycentric

		/// <summary>
		/// Interpolate 3 Vectors using Barycentric coordinates
		/// </summary>
		/// <param name="a">First input Vector</param>
		/// <param name="b">Second input Vector</param>
		/// <param name="c">Third input Vector</param>
		/// <param name="u">First Barycentric Coordinate</param>
		/// <param name="v">Second Barycentric Coordinate</param>
		/// <returns>a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</returns>
		public static TkVector4 BaryCentric(TkVector4 a, TkVector4 b, TkVector4 c, float u, float v) {
			return a + u * (b - a) + v * (c - a);
		}

		/// <summary>Interpolate 3 Vectors using Barycentric coordinates</summary>
		/// <param name="a">First input Vector.</param>
		/// <param name="b">Second input Vector.</param>
		/// <param name="c">Third input Vector.</param>
		/// <param name="u">First Barycentric Coordinate.</param>
		/// <param name="v">Second Barycentric Coordinate.</param>
		/// <param name="result">Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</param>
		public static void BaryCentric(ref TkVector4 a, ref TkVector4 b, ref TkVector4 c, float u, float v, out TkVector4 result) {
			result = a; // copy

			TkVector4 temp = b; // copy
			Subtract(ref temp, ref a, out temp);
			Multiply(ref temp, u, out temp);
			Add(ref result, ref temp, out result);

			temp = c; // copy
			Subtract(ref temp, ref a, out temp);
			Multiply(ref temp, v, out temp);
			Add(ref result, ref temp, out result);
		}

		#endregion

		#region Transform

		/// <summary>
		/// Transform a Vector by the given Matrix.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed vector.</returns>
		public static TkVector4 TransformRow(TkVector4 vec, TkMatrix4 mat) {
			var result = new TkVector4();
            TransformRow(ref vec, ref mat, ref result);
            return result;
        }

		/// <summary>
		/// Transform a Vector by the given Matrix.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="result">The transformed vector.</param>
		public static void TransformRow(ref TkVector4 vec, ref TkMatrix4 mat, ref TkVector4 result) {
			result = new TkVector4(
				(vec.X * mat.Row0.X) + (vec.Y * mat.Row1.X) + (vec.Z * mat.Row2.X) + (vec.W * mat.Row3.X),
				(vec.X * mat.Row0.Y) + (vec.Y * mat.Row1.Y) + (vec.Z * mat.Row2.Y) + (vec.W * mat.Row3.Y),
				(vec.X * mat.Row0.Z) + (vec.Y * mat.Row1.Z) + (vec.Z * mat.Row2.Z) + (vec.W * mat.Row3.Z),
				(vec.X * mat.Row0.W) + (vec.Y * mat.Row1.W) + (vec.Z * mat.Row2.W) + (vec.W * mat.Row3.W));
		}

        /// <summary>
        /// Transform a Vector by the given Matrix using right-handed notation.
        /// </summary>
        /// <param name="mat">The desired transformation.</param>
        /// <param name="vec">The vector to transform.</param>
        /// <returns>The transformed vector.</returns>
        public static TkVector4 TransformColumn(TkMatrix4 mat, TkVector4 vec) {
	        var result = new TkVector4();
            TransformColumn(ref mat, ref vec, ref result);
            return result;
        }

        /// <summary>
        /// Transform a Vector by the given Matrix using right-handed notation.
        /// </summary>
        /// <param name="mat">The desired transformation.</param>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="result">The transformed vector.</param>
        public static void TransformColumn(ref TkMatrix4 mat, ref TkVector4 vec, ref TkVector4 result)
        {
            result = new TkVector4(
                (mat.Row0.X * vec.X) + (mat.Row0.Y * vec.Y) + (mat.Row0.Z * vec.Z) + (mat.Row0.W * vec.W),
                (mat.Row1.X * vec.X) + (mat.Row1.Y * vec.Y) + (mat.Row1.Z * vec.Z) + (mat.Row1.W * vec.W),
                (mat.Row2.X * vec.X) + (mat.Row2.Y * vec.Y) + (mat.Row2.Z * vec.Z) + (mat.Row2.W * vec.W),
                (mat.Row3.X * vec.X) + (mat.Row3.Y * vec.Y) + (mat.Row3.Z * vec.Z) + (mat.Row3.W * vec.W));
        }

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static TkVector4 Transform(TkVector4 vec, TkMatrix4 mat) {
			TkVector4 result = new TkVector4();
			Transform(ref vec, ref mat, ref result);
			return result;
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void Transform(ref TkVector4 vec, ref TkMatrix4 mat, ref TkVector4 result) {
			result = new TkVector4(
				vec.X * mat.Row0.X + vec.Y * mat.Row1.X + vec.Z * mat.Row2.X + vec.W * mat.Row3.X,
				vec.X * mat.Row0.Y + vec.Y * mat.Row1.Y + vec.Z * mat.Row2.Y + vec.W * mat.Row3.Y,
				vec.X * mat.Row0.Z + vec.Y * mat.Row1.Z + vec.Z * mat.Row2.Z + vec.W * mat.Row3.Z,
				vec.X * mat.Row0.W + vec.Y * mat.Row1.W + vec.Z * mat.Row2.W + vec.W * mat.Row3.W);
		}
		
		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The result of the operation.</returns>
		public static TkVector4 Transform(TkVector4 vec, TkQuaternion quat) {
			TkVector4 result = new TkVector4();
			Transform(ref vec, ref quat, ref result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Transform(ref TkVector4 vec, ref TkQuaternion quat, ref TkVector4 result) {
			TkQuaternion v = new TkQuaternion(vec.X, vec.Y, vec.Z, vec.W), i, t;
			TkQuaternion.Invert(ref quat, out i);
			TkQuaternion.Multiply(ref quat, ref v, out t);
			TkQuaternion.Multiply(ref t, ref i, out v);

			result = new TkVector4(v.X, v.Y, v.Z, v.W);
		}

		#endregion

		#endregion

		#region Swizzle

		/// <summary>
		/// Gets or sets an OpenTK.TkVector2 with the X and Y components of this instance.
		/// </summary>
		public TkVector2 Xy { get { return new TkVector2(X, Y); } set { X = value.X; Y = value.Y; } }

		/// <summary>
		/// Gets or sets an OpenTK.TkVector3 with the X, Y and Z components of this instance.
		/// </summary>
		public TkVector3 Xyz { get { return new TkVector3(X, Y, Z); } set { X = value.X; Y = value.Y; Z = value.Z; } }

		#endregion

		#region Operators

		/// <summary>
		/// Adds two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector4 operator +(TkVector4 left, TkVector4 right) {
			left.X += right.X;
			left.Y += right.Y;
			left.Z += right.Z;
			left.W += right.W;
			return left;
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector4 operator -(TkVector4 left, TkVector4 right) {
			left.X -= right.X;
			left.Y -= right.Y;
			left.Z -= right.Z;
			left.W -= right.W;
			return left;
		}

		/// <summary>
		/// Negates an instance.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector4 operator -(TkVector4 vec) {
			vec.X = -vec.X;
			vec.Y = -vec.Y;
			vec.Z = -vec.Z;
			vec.W = -vec.W;
			return vec;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector4 operator *(TkVector4 vec, float scale) {
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			vec.W *= scale;
			return vec;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="scale">The scalar.</param>
		/// <param name="vec">The instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector4 operator *(float scale, TkVector4 vec) {
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			vec.W *= scale;
			return vec;
		}

		/// <summary>
		/// Component-wise multiplication between the specified instance by a scale vector.
		/// </summary>
		/// <param name="scale">Left operand.</param>
		/// <param name="vec">Right operand.</param>
		/// <returns>Result of multiplication.</returns>
		public static TkVector4 operator *(TkVector4 vec, TkVector4 scale) {
			vec.X *= scale.X;
			vec.Y *= scale.Y;
			vec.Z *= scale.Z;
			vec.W *= scale.W;
			return vec;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed vector.</returns>
		public static TkVector4 operator *(TkVector4 vec, TkMatrix4 mat) {
			var result = new TkVector4();
            TransformRow(ref vec, ref mat, ref result);
            return result;
        }

		/// <summary>
		/// Transform a Vector by the given Matrix using right-handed notation.
		/// </summary>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="vec">The vector to transform.</param>
		/// <returns>The transformed vector.</returns>
		public static TkVector4 operator *(TkMatrix4 mat, TkVector4 vec) {
			var result = new TkVector4();
			TransformColumn(ref mat, ref vec, ref result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <param name="vec">The vector to transform.</param>
		/// <returns>The transformed vector.</returns>
		public static TkVector4 operator *(TkQuaternion quat, TkVector4 vec)
        {
			var result = new TkVector4();
            Transform(ref vec, ref quat, ref result);
            return result;
        }

		/// <summary>
		/// Divides an instance by a scalar.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector4 operator /(TkVector4 vec, float scale) {
			vec.X /= scale;
			vec.Y /= scale;
			vec.Z /= scale;
			vec.W /= scale;
			return vec;
		}

		/// <summary>
		/// Component-wise division between the specified instance by a scale vector.
		/// </summary>
		/// <param name="vec">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the division.</returns>
		public static TkVector4 operator /(TkVector4 vec, TkVector4 scale) {
			vec.X /= scale.X;
			vec.Y /= scale.Y;
			vec.Z /= scale.Z;
			vec.W /= scale.W;
			return vec;
		}

		/// <summary>
		/// Compares two instances for equality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left equals right; false otherwise.</returns>
		public static bool operator ==(TkVector4 left, TkVector4 right) {
			return left.Equals(right);
		}

		/// <summary>
		/// Compares two instances for inequality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left does not equa lright; false otherwise.</returns>
		public static bool operator !=(TkVector4 left, TkVector4 right) {
			return !left.Equals(right);
		}

		#endregion

		#region Overrides

		#region public override string ToString()

		/// <summary>
		/// Returns a System.String that represents the current Vector4.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return String.Format("({0}, {1}, {2}, {3})", X, Y, Z, W);
		}

		#endregion

		#region public override int GetHashCode()

		/// <summary>
		/// Returns the hashcode for this instance.
		/// </summary>
		/// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
		public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode() ^ W.GetHashCode();
		}

		#endregion

		#region public override bool Equals(object obj)

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare to.</param>
		/// <returns>True if the instances are equal; false otherwise.</returns>
		public override bool Equals(object obj) {
			if (!(obj is TkVector4))
				return false;

			return Equals((TkVector4)obj);
		}

		#endregion

		#endregion

		#endregion

		#region IEquatable<Vector4> Members

		/// <summary>Indicates whether the current vector is equal to another vector.</summary>
		/// <param name="other">A vector to compare with this vector.</param>
		/// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
		public bool Equals(TkVector4 other) {
			return
				X == other.X &&
				Y == other.Y &&
				Z == other.Z &&
				W == other.W;
		}

		#endregion
	}
}