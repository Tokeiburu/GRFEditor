﻿#region --- License ---
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
	/// <summary>Represents a 2D vector using two single-precision floating-point numbers.</summary>
	/// <remarks>
	/// The Vector2 structure is suitable for interoperation with unmanaged code requiring two consecutive floats.
	/// </remarks>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct TkVector2 : IEquatable<TkVector2> {
		#region Fields

		/// <summary>
		/// The X component of the Vector2.
		/// </summary>
		public float X;

		/// <summary>
		/// The Y component of the Vector2.
		/// </summary>
		public float Y;

		#endregion

		public float this[int index] {
			get {
				if (index == 0)
					return X;
				if (index == 1)
					return Y;
				throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
			}
			set {
				if (index == 0) {
					X = value;
				}
				else {
					if (index != 1)
						throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
					Y = value;
				}
			}
		}

		#region Constructors

		/// <summary>
		/// Constructs a new instance.
		/// </summary>
		/// <param name="value">The value that will initialize this instance.</param>
		public TkVector2(float value) {
			X = value;
			Y = value;
		}

		/// <summary>
		/// Constructs a new Vector2.
		/// </summary>
		/// <param name="x">The x coordinate of the net Vector2.</param>
		/// <param name="y">The y coordinate of the net Vector2.</param>
		public TkVector2(float x, float y) {
			X = x;
			Y = y;
		}

		/// <summary>
		/// Constructs a new Vector2.
		/// </summary>
		/// <param name="x">The x coordinate of the net Vector2.</param>
		/// <param name="y">The y coordinate of the net Vector2.</param>
		public TkVector2(double x, double y) {
			X = (float)x;
			Y = (float)y;
		}

		/// <summary>
		/// Constructs a new Vector3 from a byte stream and an offset.
		/// </summary>
		/// <param name="data">The byte array data.</param>
		/// <param name="offset">The offset in the byte array.</param>
		public TkVector2(byte[] data, int offset) {
			X = BitConverter.ToSingle(data, offset);
			Y = BitConverter.ToSingle(data, offset + 4);
		}

		/// <summary>
		/// Constructs a new Vector2 from the given Vector2.
		/// </summary>
		/// <param name="v">The Vector2 to copy components from.</param>
		[Obsolete]
		public TkVector2(TkVector2 v) {
			X = v.X;
			Y = v.Y;
		}

		/// <summary>
		/// Constructs a new Vector2 from the given Vector3.
		/// </summary>
		/// <param name="v">The Vector3 to copy components from. Z is discarded.</param>
		[Obsolete]
		public TkVector2(TkVector3 v) {
			X = v.X;
			Y = v.Y;
		}

		///// <summary>
		///// Constructs a new Vector2 from the given Vector4.
		///// </summary>
		///// <param name="v">The Vector4 to copy components from. Z and W are discarded.</param>
		//[Obsolete]
		//public Vector2(Vector4 v) {
		//	X = v.X;
		//	Y = v.Y;
		//}

		#endregion

		#region Public Members

		#region Instance

		#region public void Add()

		/// <summary>Add the Vector passed as parameter to this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Add() method instead.")]
		public void Add(TkVector2 right) {
			X += right.X;
			Y += right.Y;
		}

		/// <summary>Add the Vector passed as parameter to this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Add() method instead.")]
		public void Add(ref TkVector2 right) {
			X += right.X;
			Y += right.Y;
		}

		#endregion public void Add()

		#region public void Sub()

		/// <summary>Subtract the Vector passed as parameter from this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Subtract() method instead.")]
		public void Sub(TkVector2 right) {
			X -= right.X;
			Y -= right.Y;
		}

		/// <summary>Subtract the Vector passed as parameter from this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Subtract() method instead.")]
		public void Sub(ref TkVector2 right) {
			X -= right.X;
			Y -= right.Y;
		}

		#endregion public void Sub()

		#region public void Mult()

		/// <summary>Multiply this instance by a scalar.</summary>
		/// <param name="f">Scalar operand.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Mult(float f) {
			X *= f;
			Y *= f;
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
				return (float)Math.Sqrt(X * X + Y * Y);
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
				return 1.0f / MathHelper.InverseSqrtFast(X * X + Y * Y);
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
				return X * X + Y * Y;
			}
		}

		#endregion

		#region public Vector2 PerpendicularRight

		/// <summary>
		/// Gets the perpendicular vector on the right side of this vector.
		/// </summary>
		public TkVector2 PerpendicularRight {
			get {
				return new TkVector2(Y, -X);
			}
		}

		#endregion

		#region public Vector2 PerpendicularLeft

		/// <summary>
		/// Gets the perpendicular vector on the left side of this vector.
		/// </summary>
		public TkVector2 PerpendicularLeft {
			get {
				return new TkVector2(-Y, X);
			}
		}

		#endregion

		#region public void Normalize()

		/// <summary>
		/// Scales the Vector2 to unit length.
		/// </summary>
		public void Normalize() {
			float scale = 1.0f / Length;
			X *= scale;
			Y *= scale;
		}

		#endregion

		#region public void NormalizeFast()

		/// <summary>
		/// Scales the Vector2 to approximately unit length.
		/// </summary>
		public void NormalizeFast() {
			float scale = MathHelper.InverseSqrtFast(X * X + Y * Y);
			X *= scale;
			Y *= scale;
		}

		#endregion

		#region public void Scale()

		/// <summary>
		/// Scales the current Vector2 by the given amounts.
		/// </summary>
		/// <param name="sx">The scale of the X component.</param>
		/// <param name="sy">The scale of the Y component.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(float sx, float sy) {
			X = X * sx;
			Y = Y * sy;
		}

		/// <summary>Scales this instance by the given parameter.</summary>
		/// <param name="scale">The scaling of the individual components.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(TkVector2 scale) {
			X *= scale.X;
			Y *= scale.Y;
		}

		/// <summary>Scales this instance by the given parameter.</summary>
		/// <param name="scale">The scaling of the individual components.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(ref TkVector2 scale) {
			X *= scale.X;
			Y *= scale.Y;
		}

		#endregion public void Scale()

		#endregion

		#region Static

		#region Fields

		/// <summary>
		/// Defines a unit-length Vector2 that points towards the X-axis.
		/// </summary>
		public static readonly TkVector2 UnitX = new TkVector2(1, 0);

		/// <summary>
		/// Defines a unit-length Vector2 that points towards the Y-axis.
		/// </summary>
		public static readonly TkVector2 UnitY = new TkVector2(0, 1);

		/// <summary>
		/// Defines a zero-length Vector2.
		/// </summary>
		public static readonly TkVector2 Zero = new TkVector2(0, 0);

		/// <summary>
		/// Defines an instance with all components set to 1.
		/// </summary>
		public static readonly TkVector2 One = new TkVector2(1, 1);

		/// <summary>
		/// Defines the size of the Vector2 struct in bytes.
		/// </summary>
		public static readonly int SizeInBytes = Marshal.SizeOf(new TkVector2());

		#endregion

		#region Obsolete

		#region Sub

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>Result of subtraction</returns>
		[Obsolete("Use static Subtract() method instead.")]
		public static TkVector2 Sub(TkVector2 a, TkVector2 b) {
			a.X -= b.X;
			a.Y -= b.Y;
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">Result of subtraction</param>
		[Obsolete("Use static Subtract() method instead.")]
		public static void Sub(ref TkVector2 a, ref TkVector2 b, out TkVector2 result) {
			result.X = a.X - b.X;
			result.Y = a.Y - b.Y;
		}

		#endregion

		#region Mult

		/// <summary>
		/// Multiply a vector and a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <returns>Result of the multiplication</returns>
		[Obsolete("Use static Multiply() method instead.")]
		public static TkVector2 Mult(TkVector2 a, float f) {
			a.X *= f;
			a.Y *= f;
			return a;
		}

		/// <summary>
		/// Multiply a vector and a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <param name="result">Result of the multiplication</param>
		[Obsolete("Use static Multiply() method instead.")]
		public static void Mult(ref TkVector2 a, float f, out TkVector2 result) {
			result.X = a.X * f;
			result.Y = a.Y * f;
		}

		#endregion

		#region Div

		/// <summary>
		/// Divide a vector by a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <returns>Result of the division</returns>
		[Obsolete("Use static Divide() method instead.")]
		public static TkVector2 Div(TkVector2 a, float f) {
			float mult = 1.0f / f;
			a.X *= mult;
			a.Y *= mult;
			return a;
		}

		/// <summary>
		/// Divide a vector by a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <param name="result">Result of the division</param>
		[Obsolete("Use static Divide() method instead.")]
		public static void Div(ref TkVector2 a, float f, out TkVector2 result) {
			float mult = 1.0f / f;
			result.X = a.X * mult;
			result.Y = a.Y * mult;
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
		public static TkVector2 Add(TkVector2 a, TkVector2 b) {
			Add(ref a, ref b, out a);
			return a;
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">Left operand.</param>
		/// <param name="b">Right operand.</param>
		/// <param name="result">Result of operation.</param>
		public static void Add(ref TkVector2 a, ref TkVector2 b, out TkVector2 result) {
			result = new TkVector2(a.X + b.X, a.Y + b.Y);
		}

		#endregion

		#region Subtract

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>Result of subtraction</returns>
		public static TkVector2 Subtract(TkVector2 a, TkVector2 b) {
			Subtract(ref a, ref b, out a);
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">Result of subtraction</param>
		public static void Subtract(ref TkVector2 a, ref TkVector2 b, out TkVector2 result) {
			result = new TkVector2(a.X - b.X, a.Y - b.Y);
		}

		#endregion

		#region Multiply

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector2 Multiply(TkVector2 vector, float scale) {
			Multiply(ref vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(ref TkVector2 vector, float scale, out TkVector2 result) {
			result = new TkVector2(vector.X * scale, vector.Y * scale);
		}

		/// <summary>
		/// Multiplies a vector by the components a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector2 Multiply(TkVector2 vector, TkVector2 scale) {
			Multiply(ref vector, ref scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(ref TkVector2 vector, ref TkVector2 scale, out TkVector2 result) {
			result = new TkVector2(vector.X * scale.X, vector.Y * scale.Y);
		}

		#endregion

		#region Divide

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector2 Divide(TkVector2 vector, float scale) {
			Divide(ref vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(ref TkVector2 vector, float scale, out TkVector2 result) {
			Multiply(ref vector, 1 / scale, out result);
		}

		/// <summary>
		/// Divides a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector2 Divide(TkVector2 vector, TkVector2 scale) {
			Divide(ref vector, ref scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divide a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(ref TkVector2 vector, ref TkVector2 scale, out TkVector2 result) {
			result = new TkVector2(vector.X / scale.X, vector.Y / scale.Y);
		}

		#endregion

		#region ComponentMin

		/// <summary>
		/// Calculate the component-wise minimum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>The component-wise minimum</returns>
		public static TkVector2 ComponentMin(TkVector2 a, TkVector2 b) {
			a.X = a.X < b.X ? a.X : b.X;
			a.Y = a.Y < b.Y ? a.Y : b.Y;
			return a;
		}

		/// <summary>
		/// Calculate the component-wise minimum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">The component-wise minimum</param>
		public static void ComponentMin(ref TkVector2 a, ref TkVector2 b, out TkVector2 result) {
			result.X = a.X < b.X ? a.X : b.X;
			result.Y = a.Y < b.Y ? a.Y : b.Y;
		}

		#endregion

		#region ComponentMax

		/// <summary>
		/// Calculate the component-wise maximum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>The component-wise maximum</returns>
		public static TkVector2 ComponentMax(TkVector2 a, TkVector2 b) {
			a.X = a.X > b.X ? a.X : b.X;
			a.Y = a.Y > b.Y ? a.Y : b.Y;
			return a;
		}

		/// <summary>
		/// Calculate the component-wise maximum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">The component-wise maximum</param>
		public static void ComponentMax(ref TkVector2 a, ref TkVector2 b, out TkVector2 result) {
			result.X = a.X > b.X ? a.X : b.X;
			result.Y = a.Y > b.Y ? a.Y : b.Y;
		}

		#endregion

		#region Min

		/// <summary>
		/// Returns the Vector3 with the minimum magnitude
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns>The minimum Vector3</returns>
		public static TkVector2 Min(TkVector2 left, TkVector2 right) {
			return left.LengthSquared < right.LengthSquared ? left : right;
		}

		#endregion

		#region Max

		/// <summary>
		/// Returns the Vector3 with the minimum magnitude
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns>The minimum Vector3</returns>
		public static TkVector2 Max(TkVector2 left, TkVector2 right) {
			return left.LengthSquared >= right.LengthSquared ? left : right;
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
		public static TkVector2 Clamp(TkVector2 vec, TkVector2 min, TkVector2 max) {
			vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			return vec;
		}

		/// <summary>
		/// Clamp a vector to the given minimum and maximum vectors
		/// </summary>
		/// <param name="vec">Input vector</param>
		/// <param name="min">Minimum vector</param>
		/// <param name="max">Maximum vector</param>
		/// <param name="result">The clamped vector</param>
		public static void Clamp(ref TkVector2 vec, ref TkVector2 min, ref TkVector2 max, out TkVector2 result) {
			result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
		}

		#endregion

		#region Normalize

		/// <summary>
		/// Returns a copy of the Vector3 scaled to unit length.
		/// </summary>
		/// <returns>The normalized copy.</returns>
		public TkVector2 Normalized() {
			var v = this;
			v.Normalize();
			return v;
		}

		/// <summary>
		/// Scale a vector to unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <returns>The normalized vector</returns>
		public static TkVector2 Normalize(TkVector2 vec) {
			float scale = 1.0f / vec.Length;
			vec.X *= scale;
			vec.Y *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <param name="result">The normalized vector</param>
		public static void Normalize(ref TkVector2 vec, out TkVector2 result) {
			float scale = 1.0f / vec.Length;
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
		}

		#endregion

		#region NormalizeFast

		/// <summary>
		/// Scale a vector to approximately unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <returns>The normalized vector</returns>
		public static TkVector2 NormalizeFast(TkVector2 vec) {
			float scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y);
			vec.X *= scale;
			vec.Y *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to approximately unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <param name="result">The normalized vector</param>
		public static void NormalizeFast(ref TkVector2 vec, out TkVector2 result) {
			float scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y);
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
		}

		#endregion

		#region Dot

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <returns>The dot product of the two inputs</returns>
		public static float Dot(TkVector2 left, TkVector2 right) {
			return left.X * right.X + left.Y * right.Y;
		}

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <param name="result">The dot product of the two inputs</param>
		public static void Dot(ref TkVector2 left, ref TkVector2 right, out float result) {
			result = left.X * right.X + left.Y * right.Y;
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
		public static TkVector2 Lerp(TkVector2 a, TkVector2 b, float blend) {
			a.X = blend * (b.X - a.X) + a.X;
			a.Y = blend * (b.Y - a.Y) + a.Y;
			return a;
		}

		/// <summary>
		/// Returns a new Vector that is the linear blend of the 2 given Vectors
		/// </summary>
		/// <param name="a">First input vector</param>
		/// <param name="b">Second input vector</param>
		/// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
		/// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
		public static void Lerp(ref TkVector2 a, ref TkVector2 b, float blend, out TkVector2 result) {
			result.X = blend * (b.X - a.X) + a.X;
			result.Y = blend * (b.Y - a.Y) + a.Y;
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
		public static TkVector2 BaryCentric(TkVector2 a, TkVector2 b, TkVector2 c, float u, float v) {
			return a + u * (b - a) + v * (c - a);
		}

		/// <summary>Interpolate 3 Vectors using Barycentric coordinates</summary>
		/// <param name="a">First input Vector.</param>
		/// <param name="b">Second input Vector.</param>
		/// <param name="c">Third input Vector.</param>
		/// <param name="u">First Barycentric Coordinate.</param>
		/// <param name="v">Second Barycentric Coordinate.</param>
		/// <param name="result">Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</param>
		public static void BaryCentric(ref TkVector2 a, ref TkVector2 b, ref TkVector2 c, float u, float v, out TkVector2 result) {
			result = a; // copy

			TkVector2 temp = b; // copy
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

		///// <summary>
		///// Transforms a vector by a quaternion rotation.
		///// </summary>
		///// <param name="vec">The vector to transform.</param>
		///// <param name="quat">The quaternion to rotate the vector by.</param>
		///// <returns>The result of the operation.</returns>
		//public static Vector2 Transform(Vector2 vec, Quaternion quat) {
		//	Vector2 result;
		//	Transform(ref vec, ref quat, out result);
		//	return result;
		//}
		//
		///// <summary>
		///// Transforms a vector by a quaternion rotation.
		///// </summary>
		///// <param name="vec">The vector to transform.</param>
		///// <param name="quat">The quaternion to rotate the vector by.</param>
		///// <param name="result">The result of the operation.</param>
		//public static void Transform(ref Vector2 vec, ref Quaternion quat, out Vector2 result) {
		//	Quaternion v = new Quaternion(vec.X, vec.Y, 0, 0), i, t;
		//	Quaternion.Invert(ref quat, out i);
		//	Quaternion.Multiply(ref quat, ref v, out t);
		//	Quaternion.Multiply(ref t, ref i, out v);
		//
		//	result = new Vector2(v.X, v.Y);
		//}

		#endregion

		#endregion

		#region Operators

		/// <summary>
		/// Adds the specified instances.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns>Result of addition.</returns>
		public static TkVector2 operator +(TkVector2 left, TkVector2 right) {
			left.X += right.X;
			left.Y += right.Y;
			return left;
		}

		/// <summary>
		/// Subtracts the specified instances.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns>Result of subtraction.</returns>
		public static TkVector2 operator -(TkVector2 left, TkVector2 right) {
			left.X -= right.X;
			left.Y -= right.Y;
			return left;
		}

		/// <summary>
		/// Negates the specified instance.
		/// </summary>
		/// <param name="vec">Operand.</param>
		/// <returns>Result of negation.</returns>
		public static TkVector2 operator -(TkVector2 vec) {
			vec.X = -vec.X;
			vec.Y = -vec.Y;
			return vec;
		}

		/// <summary>
		/// Multiplies the specified instance by a scalar.
		/// </summary>
		/// <param name="vec">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of multiplication.</returns>
		public static TkVector2 operator *(TkVector2 vec, float scale) {
			vec.X *= scale;
			vec.Y *= scale;
			return vec;
		}

		/// <summary>
		/// Multiplies the specified instance by a scalar.
		/// </summary>
		/// <param name="scale">Left operand.</param>
		/// <param name="vec">Right operand.</param>
		/// <returns>Result of multiplication.</returns>
		public static TkVector2 operator *(float scale, TkVector2 vec) {
			vec.X *= scale;
			vec.Y *= scale;
			return vec;
		}

		/// <summary>
		/// Divides the specified instance by a scalar.
		/// </summary>
		/// <param name="vec">Left operand</param>
		/// <param name="scale">Right operand</param>
		/// <returns>Result of the division.</returns>
		public static TkVector2 operator /(TkVector2 vec, float scale) {
			float mult = 1.0f / scale;
			vec.X *= mult;
			vec.Y *= mult;
			return vec;
		}

		/// <summary>
		/// Compares the specified instances for equality.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns>True if both instances are equal; false otherwise.</returns>
		public static bool operator ==(TkVector2 left, TkVector2 right) {
			return left.Equals(right);
		}

		/// <summary>
		/// Compares the specified instances for inequality.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns>True if both instances are not equal; false otherwise.</returns>
		public static bool operator !=(TkVector2 left, TkVector2 right) {
			return !left.Equals(right);
		}

		#endregion

		#region Overrides

		#region public override string ToString()

		/// <summary>
		/// Returns a System.String that represents the current Vector2.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return String.Format("({0}, {1})", X, Y);
		}

		#endregion

		#region public override int GetHashCode()

		/// <summary>
		/// Returns the hashcode for this instance.
		/// </summary>
		/// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
		public override int GetHashCode() {
// ReSharper disable NonReadonlyFieldInGetHashCode
			return X.GetHashCode() ^ Y.GetHashCode();
		}

		#endregion

		#region public override bool Equals(object obj)

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare to.</param>
		/// <returns>True if the instances are equal; false otherwise.</returns>
		public override bool Equals(object obj) {
			if (!(obj is TkVector2))
				return false;

			return Equals((TkVector2)obj);
		}

		#endregion

		#endregion

		#endregion

		#region IEquatable<Vector2> Members

		/// <summary>Indicates whether the current vector is equal to another vector.</summary>
		/// <param name="other">A vector to compare with this vector.</param>
		/// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
		public bool Equals(TkVector2 other) {
			return
// ReSharper disable CompareOfFloatsByEqualityOperator
				X == other.X &&
				Y == other.Y;
		}

		#endregion


		public static double CalculateAngle(TkVector2 u, TkVector2 v) {
			return Math.Acos(((u.X * v.X) + (u.Y * v.Y)) / (Math.Pow(u.X * u.X + u.Y * u.Y, 0.5) * Math.Pow(v.X * v.X + v.Y * v.Y, 0.5)));
		}

		public static double CalculateSignedAngle(TkVector2 a, TkVector2 b) {
			return a[0] * b[1] - a[1] * b[0];
		}

		public static double CalculateDistance(TkVector2 u, TkVector2 v) {
			return (u - v).Length;
		}



		public void RotateZ(float angle) {
			double sin = Math.Sin(angle * Math.PI / 180f);
			double cos = Math.Cos(angle * Math.PI / 180f);
			float x = X;

			X = (float)(X * cos + Y * sin);
			Y = (float)(-x * sin + Y * cos);
		}

		public void RotateZ(float angle, TkVector2 center) {
			TkVector2 current = this - center;
			current.RotateZ(angle);
			current = current + center;

			X = current.X;
			Y = current.Y;
		}
	}
}