#region --- License ---
/*
Copyright (c) 2006 - 2008 The Open Toolkit library.
Copyright 2013 Xamarin Inc

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
using System.IO;
using System.Runtime.InteropServices;
using GRF.IO;

namespace GRF.Graphics {
	/// <summary>
	/// Represents a 3D vector using three single-precision floating-point numbers.
	/// </summary>
	/// <remarks>
	/// The TkVector3 structure is suitable for interoperation with unmanaged code requiring three consecutive floats.
	/// </remarks>
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct TkVector3 : IEquatable<TkVector3> {
		#region Fields

		/// <summary>
		/// The X component of the TkVector3.
		/// </summary>
		public float X;

		/// <summary>
		/// The Y component of the TkVector3.
		/// </summary>
		public float Y;

		/// <summary>
		/// The Z component of the TkVector3.
		/// </summary>
		public float Z;

		#endregion

		public float this[int index] {
			get {
				if (index == 0)
					return X;
				if (index == 1)
					return Y;
				if (index == 2)
					return Z;
				throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
			}
			set {
				if (index == 0) {
					X = value;
				}
				else if (index == 1) {
					Y = value;
				}
				else {
					if (index != 2)
						throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
					Z = value;
				}
			}
		}

		#region Constructors

		/// <summary>
		/// Constructs a new instance.
		/// </summary>
		/// <param name="value">The value that will initialize this instance.</param>
		public TkVector3(float value) {
			X = value;
			Y = value;
			Z = value;
		}

		/// <summary>
		/// Constructs a new TkVector3.
		/// </summary>
		/// <param name="x">The x component of the TkVector3.</param>
		/// <param name="y">The y component of the TkVector3.</param>
		/// <param name="z">The z component of the TkVector3.</param>
		public TkVector3(float x, float y, float z) {
			X = x;
			Y = y;
			Z = z;
		}

		/// <summary>
		/// Constructs a new TkVector3.
		/// </summary>
		/// <param name="x">The x component of the TkVector3.</param>
		/// <param name="y">The y component of the TkVector3.</param>
		/// <param name="z">The z component of the TkVector3.</param>
		public TkVector3(double x, double y, double z) {
			X = (float)x;
			Y = (float)y;
			Z = (float)z;
		}

		/// <summary>
		/// Constructs a new TkVector3 from the given Vector2.
		/// </summary>
		/// <param name="v">The Vector2 to copy components from.</param>
		public TkVector3(TkVector2 v) {
			X = v.X;
			Y = v.Y;
			Z = 0.0f;
		}

		/// <summary>
		/// Constructs a new TkVector3 from the given TkVector3.
		/// </summary>
		/// <param name="v">The TkVector3 to copy components from.</param>
		public TkVector3(TkVector3 v) {
			X = v.X;
			Y = v.Y;
			Z = v.Z;
		}

		/// <summary>
		/// Constructs a new TkVector3 from a byte stream and an offset.
		/// </summary>
		/// <param name="data">The byte array data.</param>
		/// <param name="offset">The offset in the byte array.</param>
		public TkVector3(byte[] data, int offset) {
			X = BitConverter.ToSingle(data, offset);
			Y = BitConverter.ToSingle(data, offset + 4);
			Z = BitConverter.ToSingle(data, offset + 8);
		}
		/// <summary>
		/// Constructs a new TkVector3 from a binary stream.
		/// </summary>
		/// <param name="reader">The binary stream.</param>
		public TkVector3(IBinaryReader reader) {
			X = reader.Float();
			Y = reader.Float();
			Z = reader.Float();
		}

		/// <summary>
		/// Constructs a new TkVector3 from the given Vector4.
		/// </summary>
		/// <param name="v">The Vector4 to copy components from.</param>
		public TkVector3(TkVector4 v) {
			X = v.X;
			Y = v.Y;
			Z = v.Z;
		}

		#endregion

		#region Instance

		#region public void Add()

		/// <summary>Add the Vector passed as parameter to this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Add() method instead.")]
		public void Add(TkVector3 right) {
			X += right.X;
			Y += right.Y;
			Z += right.Z;
		}

		/// <summary>Add the Vector passed as parameter to this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Add() method instead.")]
		public void Add(ref TkVector3 right) {
			X += right.X;
			Y += right.Y;
			Z += right.Z;
		}

		#endregion public void Add()

		#region public void Sub()

		/// <summary>Subtract the Vector passed as parameter from this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Subtract() method instead.")]
		public void Sub(TkVector3 right) {
			X -= right.X;
			Y -= right.Y;
			Z -= right.Z;
		}

		/// <summary>Subtract the Vector passed as parameter from this instance.</summary>
		/// <param name="right">Right operand. This parameter is only read from.</param>
		[Obsolete("Use static Subtract() method instead.")]
		public void Sub(ref TkVector3 right) {
			X -= right.X;
			Y -= right.Y;
			Z -= right.Z;
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
				return (float)Math.Sqrt(X * X + Y * Y + Z * Z);
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
				return 1.0f / MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z);
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
				return X * X + Y * Y + Z * Z;
			}
		}

		#endregion

		#region public void Normalize()

		/// <summary>
		/// Returns a copy of the TkVector3 scaled to unit length.
		/// </summary>
		/// <returns>The normalized copy.</returns>
		public TkVector3 Normalized() {
			var v = this;
			v.Normalize();
			return v;
		}

		/// <summary>
		/// Scales the TkVector3 to unit length.
		/// </summary>
		public void Normalize() {
			float scale = 1.0f / Length;
			X *= scale;
			Y *= scale;
			Z *= scale;
		}

		#endregion

		#region public void NormalizeFast()

		/// <summary>
		/// Scales the TkVector3 to approximately unit length.
		/// </summary>
		public void NormalizeFast() {
			float scale = MathHelper.InverseSqrtFast(X * X + Y * Y + Z * Z);
			X *= scale;
			Y *= scale;
			Z *= scale;
		}

		#endregion

		#region public void Scale()

		/// <summary>
		/// Scales the current TkVector3 by the given amounts.
		/// </summary>
		/// <param name="sx">The scale of the X component.</param>
		/// <param name="sy">The scale of the Y component.</param>
		/// <param name="sz">The scale of the Z component.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(float sx, float sy, float sz) {
			X = X * sx;
			Y = Y * sy;
			Z = Z * sz;
		}

		/// <summary>Scales this instance by the given parameter.</summary>
		/// <param name="scale">The scaling of the individual components.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(TkVector3 scale) {
			X *= scale.X;
			Y *= scale.Y;
			Z *= scale.Z;
		}

		/// <summary>Scales this instance by the given parameter.</summary>
		/// <param name="scale">The scaling of the individual components.</param>
		[Obsolete("Use static Multiply() method instead.")]
		public void Scale(ref TkVector3 scale) {
			X *= scale.X;
			Y *= scale.Y;
			Z *= scale.Z;
		}

		#endregion public void Scale()

		#endregion

		#region Static

		#region Fields

		/// <summary>
		/// Defines a unit-length TkVector3 that points towards the X-axis.
		/// </summary>
		public static readonly TkVector3 UnitX = new TkVector3(1, 0, 0);

		/// <summary>
		/// Defines a unit-length TkVector3 that points towards the Y-axis.
		/// </summary>
		public static readonly TkVector3 UnitY = new TkVector3(0, 1, 0);

		/// <summary>
		/// /// Defines a unit-length TkVector3 that points towards the Z-axis.
		/// </summary>
		public static readonly TkVector3 UnitZ = new TkVector3(0, 0, 1);

		/// <summary>
		/// Defines a zero-length TkVector3.
		/// </summary>
		public static readonly TkVector3 Zero = new TkVector3(0, 0, 0);

		/// <summary>
		/// Defines an instance with all components set to 1.
		/// </summary>
		public static readonly TkVector3 One = new TkVector3(1, 1, 1);

		/// <summary>
		/// Defines the size of the TkVector3 struct in bytes.
		/// </summary>
		public static readonly int SizeInBytes = Marshal.SizeOf(new TkVector3());

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
		public static TkVector3 Sub(TkVector3 a, TkVector3 b) {
			a.X -= b.X;
			a.Y -= b.Y;
			a.Z -= b.Z;
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">Result of subtraction</param>
		[Obsolete("Use static Subtract() method instead.")]
		public static void Sub(ref TkVector3 a, ref TkVector3 b, out TkVector3 result) {
			result.X = a.X - b.X;
			result.Y = a.Y - b.Y;
			result.Z = a.Z - b.Z;
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
		public static TkVector3 Mult(TkVector3 a, float f) {
			a.X *= f;
			a.Y *= f;
			a.Z *= f;
			return a;
		}

		/// <summary>
		/// Multiply a vector and a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <param name="result">Result of the multiplication</param>
		[Obsolete("Use static Multiply() method instead.")]
		public static void Mult(ref TkVector3 a, float f, out TkVector3 result) {
			result.X = a.X * f;
			result.Y = a.Y * f;
			result.Z = a.Z * f;
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
		public static TkVector3 Div(TkVector3 a, float f) {
			float mult = 1.0f / f;
			a.X *= mult;
			a.Y *= mult;
			a.Z *= mult;
			return a;
		}

		/// <summary>
		/// Divide a vector by a scalar
		/// </summary>
		/// <param name="a">Vector operand</param>
		/// <param name="f">Scalar operand</param>
		/// <param name="result">Result of the division</param>
		[Obsolete("Use static Divide() method instead.")]
		public static void Div(ref TkVector3 a, float f, out TkVector3 result) {
			float mult = 1.0f / f;
			result.X = a.X * mult;
			result.Y = a.Y * mult;
			result.Z = a.Z * mult;
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
		public static TkVector3 Add(TkVector3 a, TkVector3 b) {
			Add(ref a, ref b, ref a);
			return a;
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">Left operand.</param>
		/// <param name="b">Right operand.</param>
		/// <param name="result">Result of operation.</param>
		public static void Add(ref TkVector3 a, ref TkVector3 b, ref TkVector3 result) {
			result = new TkVector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
		}

		#endregion

		#region Subtract

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>Result of subtraction</returns>
		public static TkVector3 Subtract(TkVector3 a, TkVector3 b) {
			Subtract(ref a, ref b, out a);
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">Result of subtraction</param>
		public static void Subtract(ref TkVector3 a, ref TkVector3 b, out TkVector3 result) {
			result = new TkVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
		}

		#endregion

		#region Multiply

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector3 Multiply(TkVector3 vector, float scale) {
			Multiply(ref vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(ref TkVector3 vector, float scale, out TkVector3 result) {
			result = new TkVector3(vector.X * scale, vector.Y * scale, vector.Z * scale);
		}

		/// <summary>
		/// Multiplies a vector by the components a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector3 Multiply(TkVector3 vector, TkVector3 scale) {
			Multiply(ref vector, ref scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(ref TkVector3 vector, ref TkVector3 scale, out TkVector3 result) {
			result = new TkVector3(vector.X * scale.X, vector.Y * scale.Y, vector.Z * scale.Z);
		}

		#endregion

		#region Divide

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector3 Divide(TkVector3 vector, float scale) {
			Divide(ref vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(ref TkVector3 vector, float scale, out TkVector3 result) {
			Multiply(ref vector, 1 / scale, out result);
		}

		/// <summary>
		/// Divides a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		public static TkVector3 Divide(TkVector3 vector, TkVector3 scale) {
			Divide(ref vector, ref scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divide a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(ref TkVector3 vector, ref TkVector3 scale, out TkVector3 result) {
			result = new TkVector3(vector.X / scale.X, vector.Y / scale.Y, vector.Z / scale.Z);
		}

		#endregion

		#region ComponentMin

		/// <summary>
		/// Calculate the component-wise minimum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>The component-wise minimum</returns>
		public static TkVector3 ComponentMin(TkVector3 a, TkVector3 b) {
			a.X = a.X < b.X ? a.X : b.X;
			a.Y = a.Y < b.Y ? a.Y : b.Y;
			a.Z = a.Z < b.Z ? a.Z : b.Z;
			return a;
		}

		/// <summary>
		/// Calculate the component-wise minimum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">The component-wise minimum</param>
		public static void ComponentMin(ref TkVector3 a, ref TkVector3 b, out TkVector3 result) {
			result.X = a.X < b.X ? a.X : b.X;
			result.Y = a.Y < b.Y ? a.Y : b.Y;
			result.Z = a.Z < b.Z ? a.Z : b.Z;
		}

		#endregion

		#region ComponentMax

		/// <summary>
		/// Calculate the component-wise maximum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <returns>The component-wise maximum</returns>
		public static TkVector3 ComponentMax(TkVector3 a, TkVector3 b) {
			a.X = a.X > b.X ? a.X : b.X;
			a.Y = a.Y > b.Y ? a.Y : b.Y;
			a.Z = a.Z > b.Z ? a.Z : b.Z;
			return a;
		}

		/// <summary>
		/// Calculate the component-wise maximum of two vectors
		/// </summary>
		/// <param name="a">First operand</param>
		/// <param name="b">Second operand</param>
		/// <param name="result">The component-wise maximum</param>
		public static void ComponentMax(ref TkVector3 a, ref TkVector3 b, out TkVector3 result) {
			result.X = a.X > b.X ? a.X : b.X;
			result.Y = a.Y > b.Y ? a.Y : b.Y;
			result.Z = a.Z > b.Z ? a.Z : b.Z;
		}

		#endregion

		#region Min

		/// <summary>
		/// Returns the TkVector3 with the minimum magnitude
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns>The minimum TkVector3</returns>
		public static TkVector3 Min(TkVector3 left, TkVector3 right) {
			return left.LengthSquared < right.LengthSquared ? left : right;
		}

		#endregion

		#region Max

		/// <summary>
		/// Returns the TkVector3 with the minimum magnitude
		/// </summary>
		/// <param name="left">Left operand</param>
		/// <param name="right">Right operand</param>
		/// <returns>The minimum TkVector3</returns>
		public static TkVector3 Max(TkVector3 left, TkVector3 right) {
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
		public static TkVector3 Clamp(TkVector3 vec, TkVector3 min, TkVector3 max) {
			vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			vec.Z = vec.Z < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
			return vec;
		}

		/// <summary>
		/// Clamp a vector to the given minimum and maximum vectors
		/// </summary>
		/// <param name="vec">Input vector</param>
		/// <param name="min">Minimum vector</param>
		/// <param name="max">Maximum vector</param>
		/// <param name="result">The clamped vector</param>
		public static void Clamp(ref TkVector3 vec, ref TkVector3 min, ref TkVector3 max, out TkVector3 result) {
			result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			result.Z = vec.Z < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
		}

		#endregion

		#region Normalize

		/// <summary>
		/// Scale a vector to unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <returns>The normalized vector</returns>
		public static TkVector3 Normalize(TkVector3 vec) {
			float scale = 1.0f / vec.Length;
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <param name="result">The normalized vector</param>
		public static void Normalize(ref TkVector3 vec, out TkVector3 result) {
			float scale = 1.0f / vec.Length;
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
			result.Z = vec.Z * scale;
		}

		#endregion

		#region NormalizeFast

		/// <summary>
		/// Scale a vector to approximately unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <returns>The normalized vector</returns>
		public static TkVector3 NormalizeFast(TkVector3 vec) {
			float scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to approximately unit length
		/// </summary>
		/// <param name="vec">The input vector</param>
		/// <param name="result">The normalized vector</param>
		public static void NormalizeFast(ref TkVector3 vec, out TkVector3 result) {
			float scale = MathHelper.InverseSqrtFast(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
			result.Z = vec.Z * scale;
		}

		#endregion

		#region Dot

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <returns>The dot product of the two inputs</returns>
		public static float Dot(TkVector3 left, TkVector3 right) {
			return left.X * right.X + left.Y * right.Y + left.Z * right.Z;
		}

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <param name="result">The dot product of the two inputs</param>
		public static void Dot(ref TkVector3 left, ref TkVector3 right, out float result) {
			result = left.X * right.X + left.Y * right.Y + left.Z * right.Z;
		}

		#endregion

		#region Cross

		/// <summary>
		/// Caclulate the cross (vector) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <returns>The cross product of the two inputs</returns>
		public static TkVector3 Cross(TkVector3 left, TkVector3 right) {
			TkVector3 result;
			Cross(ref left, ref right, out result);
			return result;
		}

		/// <summary>
		/// Caclulate the cross (vector) product of two vectors
		/// </summary>
		/// <param name="left">First operand</param>
		/// <param name="right">Second operand</param>
		/// <returns>The cross product of the two inputs</returns>
		/// <param name="result">The cross product of the two inputs</param>
		public static void Cross(ref TkVector3 left, ref TkVector3 right, out TkVector3 result) {
			result = new TkVector3(left.Y * right.Z - left.Z * right.Y,
				left.Z * right.X - left.X * right.Z,
				left.X * right.Y - left.Y * right.X);
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
		public static TkVector3 Lerp(TkVector3 a, TkVector3 b, float blend) {
			a.X = blend * (b.X - a.X) + a.X;
			a.Y = blend * (b.Y - a.Y) + a.Y;
			a.Z = blend * (b.Z - a.Z) + a.Z;
			return a;
		}

		/// <summary>
		/// Returns a new Vector that is the linear blend of the 2 given Vectors
		/// </summary>
		/// <param name="a">First input vector</param>
		/// <param name="b">Second input vector</param>
		/// <param name="blend">The blend factor. a when blend=0, b when blend=1.</param>
		/// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise</param>
		public static void Lerp(ref TkVector3 a, ref TkVector3 b, float blend, out TkVector3 result) {
			result.X = blend * (b.X - a.X) + a.X;
			result.Y = blend * (b.Y - a.Y) + a.Y;
			result.Z = blend * (b.Z - a.Z) + a.Z;
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
		public static TkVector3 BaryCentric(TkVector3 a, TkVector3 b, TkVector3 c, float u, float v) {
			return a + u * (b - a) + v * (c - a);
		}

		/// <summary>Interpolate 3 Vectors using Barycentric coordinates</summary>
		/// <param name="a">First input Vector.</param>
		/// <param name="b">Second input Vector.</param>
		/// <param name="c">Third input Vector.</param>
		/// <param name="u">First Barycentric Coordinate.</param>
		/// <param name="v">Second Barycentric Coordinate.</param>
		/// <param name="result">Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise</param>
		public static void BaryCentric(ref TkVector3 a, ref TkVector3 b, ref TkVector3 c, float u, float v, out TkVector3 result) {
			result = a; // copy

			TkVector3 temp = b; // copy
			Subtract(ref temp, ref a, out temp);
			Multiply(ref temp, u, out temp);
			Add(ref result, ref temp, ref result);

			temp = c; // copy
			Subtract(ref temp, ref a, out temp);
			Multiply(ref temp, v, out temp);
			Add(ref result, ref temp, ref result);
		}

		#endregion

		#region Transform
		
        /// <summary>
        /// Transform a Vector by the given Matrix.
        /// </summary>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="mat">The desired transformation.</param>
        /// <returns>The transformed vector.</returns>
        public static TkVector3 TransformRow(TkVector3 vec, TkMatrix3 mat) {
	        var result = new TkVector3();
            TransformRow(ref vec, ref mat, ref result);
            return result;
        }

        /// <summary>
        /// Transform a Vector by the given Matrix.
        /// </summary>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="mat">The desired transformation.</param>
        /// <param name="result">The transformed vector.</param>
        public static void TransformRow(ref TkVector3 vec, ref TkMatrix3 mat, ref TkVector3 result)
        {
            result.X = (vec.X * mat.Row0.X) + (vec.Y * mat.Row1.X) + (vec.Z * mat.Row2.X);
            result.Y = (vec.X * mat.Row0.Y) + (vec.Y * mat.Row1.Y) + (vec.Z * mat.Row2.Y);
            result.Z = (vec.X * mat.Row0.Z) + (vec.Y * mat.Row1.Z) + (vec.Z * mat.Row2.Z);
        }

        /// <summary>
        /// Transform a Vector by the given Matrix using right-handed notation.
        /// </summary>
        /// <param name="mat">The desired transformation.</param>
        /// <param name="vec">The vector to transform.</param>
        /// <returns>The transformed vector.</returns>
        public static TkVector3 TransformColumn(TkMatrix3 mat, TkVector3 vec) {
	        TkVector3 result = new TkVector3();
            TransformColumn(ref mat, ref vec, ref result);
            return result;
        }

        /// <summary>
        /// Transform a Vector by the given Matrix using right-handed notation.
        /// </summary>
        /// <param name="mat">The desired transformation.</param>
        /// <param name="vec">The vector to transform.</param>
        /// <param name="result">The transformed vector.</param>
		public static void TransformColumn(ref TkMatrix3 mat, ref TkVector3 vec, ref TkVector3 result)
        {
            result.X = (mat.Row0.X * vec.X) + (mat.Row0.Y * vec.Y) + (mat.Row0.Z * vec.Z);
            result.Y = (mat.Row1.X * vec.X) + (mat.Row1.Y * vec.Y) + (mat.Row1.Z * vec.Z);
            result.Z = (mat.Row2.X * vec.X) + (mat.Row2.Y * vec.Y) + (mat.Row2.Z * vec.Z);
        }

		/// <summary>Transform a direction vector by the given Matrix
		/// Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
		/// </summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static TkVector3 TransformVector(TkVector3 vec, TkMatrix4 mat) {
			TkVector3 v;
			v.X = Dot(vec, new TkVector3(mat.Column0));
			v.Y = Dot(vec, new TkVector3(mat.Column1));
			v.Z = Dot(vec, new TkVector3(mat.Column2));
			return v;
		}
		
		/// <summary>Transform a direction vector by the given Matrix
		/// Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
		/// </summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void TransformVector(ref TkVector3 vec, ref TkMatrix4 mat, out TkVector3 result) {
			result.X = vec.X * mat.Row0.X +
					   vec.Y * mat.Row1.X +
					   vec.Z * mat.Row2.X;
		
			result.Y = vec.X * mat.Row0.Y +
					   vec.Y * mat.Row1.Y +
					   vec.Z * mat.Row2.Y;
		
			result.Z = vec.X * mat.Row0.Z +
					   vec.Y * mat.Row1.Z +
					   vec.Z * mat.Row2.Z;
		}

		/// <summary>Transform a Normal by the given Matrix</summary>
		/// <remarks>
		/// This calculates the inverse of the given matrix, use TransformNormalInverse if you
		/// already have the inverse to avoid this extra calculation
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed normal</returns>
		public static TkVector3 TransformNormal(TkVector3 norm, TkMatrix4 mat) {
			mat.Invert();
			return TransformNormalInverse(norm, mat);
		}

		/// <summary>Transform a Normal by the given Matrix</summary>
		/// <remarks>
		/// This calculates the inverse of the given matrix, use TransformNormalInverse if you
		/// already have the inverse to avoid this extra calculation
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed normal</param>
		public static void TransformNormal(ref TkVector3 norm, ref TkMatrix4 mat, out TkVector3 result) {
			TkMatrix4 Inverse = TkMatrix4.Invert(mat);
			TransformNormalInverse(ref norm, ref Inverse, out result);
		}

		/// <summary>Transform a Normal by the (transpose of the) given Matrix</summary>
		/// <remarks>
		/// This version doesn't calculate the inverse matrix.
		/// Use this version if you already have the inverse of the desired transform to hand
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="invMat">The inverse of the desired transformation</param>
		/// <returns>The transformed normal</returns>
		public static TkVector3 TransformNormalInverse(TkVector3 norm, TkMatrix4 invMat) {
			TkVector3 n;
			n.X = Dot(norm, new TkVector3(invMat.Row0));
			n.Y = Dot(norm, new TkVector3(invMat.Row1));
			n.Z = Dot(norm, new TkVector3(invMat.Row2));
			return n;
		}

		/// <summary>Transform a Normal by the (transpose of the) given Matrix</summary>
		/// <remarks>
		/// This version doesn't calculate the inverse matrix.
		/// Use this version if you already have the inverse of the desired transform to hand
		/// </remarks>
		/// <param name="norm">The normal to transform</param>
		/// <param name="invMat">The inverse of the desired transformation</param>
		/// <param name="result">The transformed normal</param>
		public static void TransformNormalInverse(ref TkVector3 norm, ref TkMatrix4 invMat, out TkVector3 result) {
			result.X = norm.X * invMat.Row0.X +
					   norm.Y * invMat.Row0.Y +
					   norm.Z * invMat.Row0.Z;

			result.Y = norm.X * invMat.Row1.X +
					   norm.Y * invMat.Row1.Y +
					   norm.Z * invMat.Row1.Z;

			result.Z = norm.X * invMat.Row2.X +
					   norm.Y * invMat.Row2.Y +
					   norm.Z * invMat.Row2.Z;
		}

		///// <summary>Transform a Position by the given Matrix</summary>
		///// <param name="pos">The position to transform</param>
		///// <param name="mat">The desired transformation</param>
		///// <returns>The transformed position</returns>
		//public static TkVector3 TransformPosition(TkVector3 pos, Matrix4 mat) {
		//	TkVector3 p;
		//	p.X = Dot(pos, new TkVector3(mat.Column0)) + mat.Row3.X;
		//	p.Y = Dot(pos, new TkVector3(mat.Column1)) + mat.Row3.Y;
		//	p.Z = Dot(pos, new TkVector3(mat.Column2)) + mat.Row3.Z;
		//	return p;
		//}

		/// <summary>Transform a Position by the given Matrix</summary>
		/// <param name="pos">The position to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed position</param>
		public static void TransformPosition(ref TkVector3 pos, ref TkMatrix4 mat, out TkVector3 result) {
			result.X = pos.X * mat.Row0.X +
					   pos.Y * mat.Row1.X +
					   pos.Z * mat.Row2.X +
					   mat.Row3.X;

			result.Y = pos.X * mat.Row0.Y +
					   pos.Y * mat.Row1.Y +
					   pos.Z * mat.Row2.Y +
					   mat.Row3.Y;

			result.Z = pos.X * mat.Row0.Z +
					   pos.Y * mat.Row1.Z +
					   pos.Z * mat.Row2.Z +
					   mat.Row3.Z;
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static TkVector3 Transform(TkVector3 vec, TkMatrix4 mat) {
			TkVector3 result = new TkVector3();
			Transform(ref vec, ref mat, ref result);
			return result;
		}

		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void Transform(ref TkVector3 vec, ref TkMatrix4 mat, ref TkVector4 result) {
			TkVector4 v4 = new TkVector4(vec.X, vec.Y, vec.Z, 1.0f);
			TkVector4.Transform(ref v4, ref mat, ref result);
		}
		
		/// <summary>Transform a Vector by the given Matrix</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void Transform(ref TkVector3 vec, ref TkMatrix4 mat, ref TkVector3 result) {
			TkVector4 v4 = new TkVector4(vec.X, vec.Y, vec.Z, 1.0f);
			TkVector4.Transform(ref v4, ref mat, ref v4);
			result = v4.Xyz;
		}
		
		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The result of the operation.</returns>
		public static TkVector3 Transform(TkVector3 vec, TkQuaternion quat) {
			TkVector3 result = new TkVector3();
			Transform(ref vec, ref quat, ref result);
			return result;
		}
		
		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Transform(ref TkVector3 vec, ref TkQuaternion quat, ref TkVector3 result) {
			// Since vec.W == 0, we can optimize quat * vec * quat^-1 as follows:
			// vec + 2.0 * cross(quat.xyz, cross(quat.xyz, vec) + quat.w * vec)
			TkVector3 xyz = quat.Xyz, temp, temp2;
			Cross(ref xyz, ref vec, out temp);
			Multiply(ref vec, quat.W, out temp2);
			Add(ref temp, ref temp2, ref temp);
			Cross(ref xyz, ref temp, out temp);
			Multiply(ref temp, 2, out temp);
			Add(ref vec, ref temp, ref result);
		}

		/// <summary>Transform a TkVector3 by the given Matrix, and project the resulting Vector4 back to a TkVector3</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <returns>The transformed vector</returns>
		public static TkVector3 TransformPerspective(TkVector3 vec, TkMatrix4 mat) {
			TkVector3 result = new TkVector3();
			TransformPerspective(ref vec, ref mat, ref result);
			return result;
		}
		
		/// <summary>Transform a TkVector3 by the given Matrix, and project the resulting Vector4 back to a TkVector3</summary>
		/// <param name="vec">The vector to transform</param>
		/// <param name="mat">The desired transformation</param>
		/// <param name="result">The transformed vector</param>
		public static void TransformPerspective(ref TkVector3 vec, ref TkMatrix4 mat, ref TkVector3 result) {
			TkVector4 v = new TkVector4(vec, 1);
			TkVector4.Transform(ref v, ref mat, ref v);
			result.X = v.X / v.W;
			result.Y = v.Y / v.W;
			result.Z = v.Z / v.W;
		}

		#endregion

		#region CalculateAngle

		/// <summary>
		/// Calculates the angle (in radians) between two vectors.
		/// </summary>
		/// <param name="first">The first vector.</param>
		/// <param name="second">The second vector.</param>
		/// <returns>Angle (in radians) between the vectors.</returns>
		/// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
		public static float CalculateAngle(TkVector3 first, TkVector3 second) {
			return (float)Math.Acos((Dot(first, second)) / (first.Length * second.Length));
		}

		/// <summary>Calculates the angle (in radians) between two vectors.</summary>
		/// <param name="first">The first vector.</param>
		/// <param name="second">The second vector.</param>
		/// <param name="result">Angle (in radians) between the vectors.</param>
		/// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
		public static void CalculateAngle(ref TkVector3 first, ref TkVector3 second, out float result) {
			float temp;
			Dot(ref first, ref second, out temp);
			result = (float)Math.Acos(temp / (first.Length * second.Length));
		}

		#endregion

		#endregion

		#region Swizzle

		/// <summary>
		/// Gets or sets an OpenTK.Vector2 with the X and Y components of this instance.
		/// </summary>
		public TkVector2 Xy { get { return new TkVector2(X, Y); } set { X = value.X; Y = value.Y; } }

		#endregion

		#region Operators

		/// <summary>
		/// Adds two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector3 operator +(TkVector3 left, TkVector3 right) {
			left.X += right.X;
			left.Y += right.Y;
			left.Z += right.Z;
			return left;
		}

		/// <summary>
		/// Subtracts two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector3 operator -(TkVector3 left, TkVector3 right) {
			left.X -= right.X;
			left.Y -= right.Y;
			left.Z -= right.Z;
			return left;
		}

		/// <summary>
		/// Negates an instance.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector3 operator -(TkVector3 vec) {
			vec.X = -vec.X;
			vec.Y = -vec.Y;
			vec.Z = -vec.Z;
			return vec;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector3 operator *(TkVector3 vec, float scale) {
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		/// <summary>
		/// Multiplies an instance by a scalar.
		/// </summary>
		/// <param name="scale">The scalar.</param>
		/// <param name="vec">The instance.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector3 operator *(float scale, TkVector3 vec) {
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		/// <summary>
		/// Component-wise multiplication between the specified instance by a scale vector.
		/// </summary>
		/// <param name="scale">Left operand.</param>
		/// <param name="vec">Right operand.</param>
		/// <returns>Result of multiplication.</returns>
		public static TkVector3 operator *(TkVector3 vec, TkVector3 scale) {
			vec.X *= scale.X;
			vec.Y *= scale.Y;
			vec.Z *= scale.Z;
			return vec;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed vector.</returns>
		public static TkVector3 operator *(TkVector3 vec, TkMatrix3 mat) {
			var result = new TkVector3();
			TransformRow(ref vec, ref mat, ref result);
			return result;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix using right-handed notation.
		/// </summary>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="vec">The vector to transform.</param>
		/// <returns>The transformed vector.</returns>
		public static TkVector3 operator *(TkMatrix3 mat, TkVector3 vec) {
			var result = new TkVector3();
			TransformColumn(ref mat, ref vec, ref result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The multiplied vector.</returns>
		public static TkVector3 operator *(TkQuaternion quat, TkVector3 vec) {
			var result = new TkVector3();
            Transform(ref vec, ref quat, ref result);
            return result;
        }

		/// <summary>
		/// Divides an instance by a scalar.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>The result of the calculation.</returns>
		public static TkVector3 operator /(TkVector3 vec, float scale) {
			vec.X /= scale;
			vec.Y /= scale;
			vec.Z /= scale;
			return vec;
		}

		/// <summary>
		/// Component-wise division between the specified instance by a scale vector.
		/// </summary>
		/// <param name="vec">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the division.</returns>
		public static TkVector3 operator /(TkVector3 vec, TkVector3 scale) {
			vec.X /= scale.X;
			vec.Y /= scale.Y;
			vec.Z /= scale.Z;
			return vec;
		}

		/// <summary>
		/// Compares two instances for equality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left equals right; false otherwise.</returns>
		public static bool operator ==(TkVector3 left, TkVector3 right) {
			return left.Equals(right);
		}

		/// <summary>
		/// Compares two instances for inequality.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>True, if left does not equa lright; false otherwise.</returns>
		public static bool operator !=(TkVector3 left, TkVector3 right) {
			return !left.Equals(right);
		}

		#endregion

		#region Overrides

		#region public override string ToString()

		/// <summary>
		/// Returns a System.String that represents the current TkVector3.
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			return String.Format("({0}, {1}, {2})", X, Y, Z);
		}

		#endregion

		#region public override int GetHashCode()

		/// <summary>
		/// Returns the hashcode for this instance.
		/// </summary>
		/// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
		public override int GetHashCode() {
// ReSharper disable NonReadonlyFieldInGetHashCode
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		#endregion

		#region public override bool Equals(object obj)

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">The object to compare to.</param>
		/// <returns>True if the instances are equal; false otherwise.</returns>
		public override bool Equals(object obj) {
			if (!(obj is TkVector3))
				return false;

			return Equals((TkVector3)obj);
		}

		#endregion

		#endregion

		//#endregion

		#region IEquatable<TkVector3> Members

		/// <summary>Indicates whether the current vector is equal to another vector.</summary>
		/// <param name="other">A vector to compare with this vector.</param>
		/// <returns>true if the current vector is equal to the vector parameter; otherwise, false.</returns>
		public bool Equals(TkVector3 other) {
			return
// ReSharper disable CompareOfFloatsByEqualityOperator
				X == other.X &&
				Y == other.Y &&
				Z == other.Z;
		}

		#endregion

		public void Write(BinaryWriter writer) {
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}
	}
}