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

using GRF.IO;
using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

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

		/// <summary>
		/// Initializes a new instance of the <see cref="TkVector3"/> struct.
		/// </summary>
		/// <param name="value">The value that will initialize this instance.</param>
		public TkVector3(float value) {
			X = value;
			Y = value;
			Z = value;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TkVector3"/> struct.
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
		/// Initializes a new instance of the <see cref="TkVector3"/> struct.
		/// </summary>
		/// <param name="v">The TkVector2 to copy components from.</param>
		public TkVector3(in TkVector2 v) {
			X = v.X;
			Y = v.Y;
			Z = 0.0f;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TkVector3"/> struct.
		/// </summary>
		/// <param name="v">The TkVector3 to copy components from.</param>
		public TkVector3(in TkVector3 v) {
			X = v.X;
			Y = v.Y;
			Z = v.Z;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TkVector3"/> struct.
		/// </summary>
		/// <param name="v">The TkVector4 to copy components from.</param>
		public TkVector3(in TkVector4 v) {
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
		/// Gets or sets the value at the index of the Vector.
		/// </summary>
		/// <param name="index">The index of the component from the Vector.</param>
		/// <exception cref="IndexOutOfRangeException">Thrown if the index is less than 0 or greater than 2.</exception>
		public float this[int index] {
			get {
				if (index == 0) {
					return X;
				}

				if (index == 1) {
					return Y;
				}

				if (index == 2) {
					return Z;
				}

				throw new IndexOutOfRangeException("You tried to access this vector at index: " + index);
			}

			set {
				if (index == 0) {
					X = value;
				}
				else if (index == 1) {
					Y = value;
				}
				else if (index == 2) {
					Z = value;
				}
				else {
					throw new IndexOutOfRangeException("You tried to set this vector at index: " + index);
				}
			}
		}

		/// <summary>
		/// Gets the length (magnitude) of the vector.
		/// </summary>
		/// <see cref="LengthFast"/>
		/// <seealso cref="LengthSquared"/>
		public float Length => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

		/// <summary>
		/// Gets an approximation of the vector length (magnitude).
		/// </summary>
		/// <remarks>
		/// This property uses an approximation of the square root function to calculate vector magnitude, with
		/// an upper error bound of 0.001.
		/// </remarks>
		/// <see cref="Length"/>
		/// <seealso cref="LengthSquared"/>
		public float LengthFast => 1.0f / MathHelper.InverseSqrtFast((X * X) + (Y * Y) + (Z * Z));

		/// <summary>
		/// Gets the square of the vector length (magnitude).
		/// </summary>
		/// <remarks>
		/// This property avoids the costly square root operation required by the Length property. This makes it more suitable
		/// for comparisons.
		/// </remarks>
		/// <see cref="Length"/>
		/// <seealso cref="LengthFast"/>
		public float LengthSquared => (X * X) + (Y * Y) + (Z * Z);

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
			var scale = 1.0f / Length;
			X *= scale;
			Y *= scale;
			Z *= scale;
		}

		/// <summary>
		/// Scales the TkVector3 to approximately unit length.
		/// </summary>
		public void NormalizeFast() {
			var scale = MathHelper.InverseSqrtFast((X * X) + (Y * Y) + (Z * Z));
			X *= scale;
			Y *= scale;
			Z *= scale;
		}

		/// <summary>
		/// Defines a unit-length TkVector3 that points towards the X-axis.
		/// </summary>
		public static readonly TkVector3 UnitX = new TkVector3(1, 0, 0);

		/// <summary>
		/// Defines a unit-length TkVector3 that points towards the Y-axis.
		/// </summary>
		public static readonly TkVector3 UnitY = new TkVector3(0, 1, 0);

		/// <summary>
		/// Defines a unit-length TkVector3 that points towards the Z-axis.
		/// </summary>
		public static readonly TkVector3 UnitZ = new TkVector3(0, 0, 1);

		/// <summary>
		/// Defines an instance with all components set to 0.
		/// </summary>
		public static readonly TkVector3 Zero = new TkVector3(0, 0, 0);

		/// <summary>
		/// Defines an instance with all components set to 1.
		/// </summary>
		public static readonly TkVector3 One = new TkVector3(1, 1, 1);

		/// <summary>
		/// Defines an instance with all components set to positive infinity.
		/// </summary>
		public static readonly TkVector3 PositiveInfinity = new TkVector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

		/// <summary>
		/// Defines an instance with all components set to negative infinity.
		/// </summary>
		public static readonly TkVector3 NegativeInfinity = new TkVector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

		/// <summary>
		/// Defines the size of the TkVector3 struct in bytes.
		/// </summary>
		public static readonly int SizeInBytes = Marshal.SizeOf<TkVector3>();

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">Left operand.</param>
		/// <param name="b">Right operand.</param>
		/// <returns>Result of operation.</returns>
		[Pure]
		public static TkVector3 Add(TkVector3 a, TkVector3 b) {
			Add(in a, in b, out a);
			return a;
		}

		/// <summary>
		/// Adds two vectors.
		/// </summary>
		/// <param name="a">Left operand.</param>
		/// <param name="b">Right operand.</param>
		/// <param name="result">Result of operation.</param>
		public static void Add(in TkVector3 a, in TkVector3 b, out TkVector3 result) {
			result.X = a.X + b.X;
			result.Y = a.Y + b.Y;
			result.Z = a.Z + b.Z;
		}

		/// <summary>
		/// Subtract one Vector from another.
		/// </summary>
		/// <param name="a">First operand.</param>
		/// <param name="b">Second operand.</param>
		/// <returns>Result of subtraction.</returns>
		[Pure]
		public static TkVector3 Subtract(TkVector3 a, TkVector3 b) {
			Subtract(in a, in b, out a);
			return a;
		}

		/// <summary>
		/// Subtract one Vector from another.
		/// </summary>
		/// <param name="a">First operand.</param>
		/// <param name="b">Second operand.</param>
		/// <param name="result">Result of subtraction.</param>
		public static void Subtract(in TkVector3 a, in TkVector3 b, out TkVector3 result) {
			result.X = a.X - b.X;
			result.Y = a.Y - b.Y;
			result.Z = a.Z - b.Z;
		}

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		[Pure]
		public static TkVector3 Multiply(TkVector3 vector, float scale) {
			Multiply(in vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(in TkVector3 vector, float scale, out TkVector3 result) {
			result.X = vector.X * scale;
			result.Y = vector.Y * scale;
			result.Z = vector.Z * scale;
		}

		/// <summary>
		/// Multiplies a vector by the components a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		[Pure]
		public static TkVector3 Multiply(TkVector3 vector, TkVector3 scale) {
			Multiply(in vector, in scale, out vector);
			return vector;
		}

		/// <summary>
		/// Multiplies a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Multiply(in TkVector3 vector, in TkVector3 scale, out TkVector3 result) {
			result.X = vector.X * scale.X;
			result.Y = vector.Y * scale.Y;
			result.Z = vector.Z * scale.Z;
		}

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		[Pure]
		public static TkVector3 Divide(TkVector3 vector, float scale) {
			Divide(in vector, scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divides a vector by a scalar.
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(in TkVector3 vector, float scale, out TkVector3 result) {
			result.X = vector.X / scale;
			result.Y = vector.Y / scale;
			result.Z = vector.Z / scale;
		}

		/// <summary>
		/// Divides a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <returns>Result of the operation.</returns>
		[Pure]
		public static TkVector3 Divide(TkVector3 vector, TkVector3 scale) {
			Divide(in vector, in scale, out vector);
			return vector;
		}

		/// <summary>
		/// Divide a vector by the components of a vector (scale).
		/// </summary>
		/// <param name="vector">Left operand.</param>
		/// <param name="scale">Right operand.</param>
		/// <param name="result">Result of the operation.</param>
		public static void Divide(in TkVector3 vector, in TkVector3 scale, out TkVector3 result) {
			result.X = vector.X / scale.X;
			result.Y = vector.Y / scale.Y;
			result.Z = vector.Z / scale.Z;
		}

		/// <summary>
		/// Returns a vector created from the smallest of the corresponding components of the given vectors.
		/// </summary>
		/// <param name="a">First operand.</param>
		/// <param name="b">Second operand.</param>
		/// <returns>The component-wise minimum.</returns>
		[Pure]
		public static TkVector3 ComponentMin(TkVector3 a, TkVector3 b) {
			a.X = a.X < b.X ? a.X : b.X;
			a.Y = a.Y < b.Y ? a.Y : b.Y;
			a.Z = a.Z < b.Z ? a.Z : b.Z;
			return a;
		}

		/// <summary>
		/// Returns a vector created from the smallest of the corresponding components of the given vectors.
		/// </summary>
		/// <param name="a">First operand.</param>
		/// <param name="b">Second operand.</param>
		/// <param name="result">The component-wise minimum.</param>
		public static void ComponentMin(in TkVector3 a, in TkVector3 b, out TkVector3 result) {
			result.X = a.X < b.X ? a.X : b.X;
			result.Y = a.Y < b.Y ? a.Y : b.Y;
			result.Z = a.Z < b.Z ? a.Z : b.Z;
		}

		/// <summary>
		/// Returns a vector created from the largest of the corresponding components of the given vectors.
		/// </summary>
		/// <param name="a">First operand.</param>
		/// <param name="b">Second operand.</param>
		/// <returns>The component-wise maximum.</returns>
		[Pure]
		public static TkVector3 ComponentMax(TkVector3 a, TkVector3 b) {
			a.X = a.X > b.X ? a.X : b.X;
			a.Y = a.Y > b.Y ? a.Y : b.Y;
			a.Z = a.Z > b.Z ? a.Z : b.Z;
			return a;
		}

		/// <summary>
		/// Returns a vector created from the largest of the corresponding components of the given vectors.
		/// </summary>
		/// <param name="a">First operand.</param>
		/// <param name="b">Second operand.</param>
		/// <param name="result">The component-wise maximum.</param>
		public static void ComponentMax(in TkVector3 a, in TkVector3 b, out TkVector3 result) {
			result.X = a.X > b.X ? a.X : b.X;
			result.Y = a.Y > b.Y ? a.Y : b.Y;
			result.Z = a.Z > b.Z ? a.Z : b.Z;
		}

		/// <summary>
		/// Returns the TkVector3 with the minimum magnitude. If the magnitudes are equal, the second vector
		/// is selected.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns>The minimum TkVector3.</returns>
		[Pure]
		public static TkVector3 MagnitudeMin(TkVector3 left, TkVector3 right) {
			return left.LengthSquared < right.LengthSquared ? left : right;
		}

		/// <summary>
		/// Returns the TkVector3 with the minimum magnitude. If the magnitudes are equal, the second vector
		/// is selected.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <param name="result">The magnitude-wise minimum.</param>
		public static void MagnitudeMin(in TkVector3 left, in TkVector3 right, out TkVector3 result) {
			result = left.LengthSquared < right.LengthSquared ? left : right;
		}

		/// <summary>
		/// Returns the TkVector3 with the maximum magnitude. If the magnitudes are equal, the first vector
		/// is selected.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <returns>The maximum TkVector3.</returns>
		[Pure]
		public static TkVector3 MagnitudeMax(TkVector3 left, TkVector3 right) {
			return left.LengthSquared >= right.LengthSquared ? left : right;
		}

		/// <summary>
		/// Returns the TkVector3 with the maximum magnitude. If the magnitudes are equal, the first vector
		/// is selected.
		/// </summary>
		/// <param name="left">Left operand.</param>
		/// <param name="right">Right operand.</param>
		/// <param name="result">The magnitude-wise maximum.</param>
		public static void MagnitudeMax(in TkVector3 left, in TkVector3 right, out TkVector3 result) {
			result = left.LengthSquared >= right.LengthSquared ? left : right;
		}

		/// <summary>
		/// Clamp a vector to the given minimum and maximum vectors.
		/// </summary>
		/// <param name="vec">Input vector.</param>
		/// <param name="min">Minimum vector.</param>
		/// <param name="max">Maximum vector.</param>
		/// <returns>The clamped vector.</returns>
		[Pure]
		public static TkVector3 Clamp(TkVector3 vec, TkVector3 min, TkVector3 max) {
			vec.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			vec.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			vec.Z = vec.Z < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
			return vec;
		}

		/// <summary>
		/// Clamp a vector to the given minimum and maximum vectors.
		/// </summary>
		/// <param name="vec">Input vector.</param>
		/// <param name="min">Minimum vector.</param>
		/// <param name="max">Maximum vector.</param>
		/// <param name="result">The clamped vector.</param>
		public static void Clamp(in TkVector3 vec, in TkVector3 min, in TkVector3 max, out TkVector3 result) {
			result.X = vec.X < min.X ? min.X : vec.X > max.X ? max.X : vec.X;
			result.Y = vec.Y < min.Y ? min.Y : vec.Y > max.Y ? max.Y : vec.Y;
			result.Z = vec.Z < min.Z ? min.Z : vec.Z > max.Z ? max.Z : vec.Z;
		}

		/// <summary>
		/// Compute the euclidean distance between two vectors.
		/// </summary>
		/// <param name="vec1">The first vector.</param>
		/// <param name="vec2">The second vector.</param>
		/// <returns>The distance.</returns>
		[Pure]
		public static float Distance(TkVector3 vec1, TkVector3 vec2) {
			Distance(in vec1, in vec2, out float result);
			return result;
		}

		/// <summary>
		/// Compute the euclidean distance between two vectors.
		/// </summary>
		/// <param name="vec1">The first vector.</param>
		/// <param name="vec2">The second vector.</param>
		/// <param name="result">The distance.</param>
		public static void Distance(in TkVector3 vec1, in TkVector3 vec2, out float result) {
			result = (float)Math.Sqrt(((vec2.X - vec1.X) * (vec2.X - vec1.X)) + ((vec2.Y - vec1.Y) * (vec2.Y - vec1.Y)) +
									  ((vec2.Z - vec1.Z) * (vec2.Z - vec1.Z)));
		}

		/// <summary>
		/// Compute the squared euclidean distance between two vectors.
		/// </summary>
		/// <param name="vec1">The first vector.</param>
		/// <param name="vec2">The second vector.</param>
		/// <returns>The squared distance.</returns>
		[Pure]
		public static float DistanceSquared(TkVector3 vec1, TkVector3 vec2) {
			DistanceSquared(in vec1, in vec2, out float result);
			return result;
		}

		/// <summary>
		/// Compute the squared euclidean distance between two vectors.
		/// </summary>
		/// <param name="vec1">The first vector.</param>
		/// <param name="vec2">The second vector.</param>
		/// <param name="result">The squared distance.</param>
		public static void DistanceSquared(in TkVector3 vec1, in TkVector3 vec2, out float result) {
			result = ((vec2.X - vec1.X) * (vec2.X - vec1.X)) + ((vec2.Y - vec1.Y) * (vec2.Y - vec1.Y)) +
					 ((vec2.Z - vec1.Z) * (vec2.Z - vec1.Z));
		}

		/// <summary>
		/// Scale a vector to unit length.
		/// </summary>
		/// <param name="vec">The input vector.</param>
		/// <returns>The normalized copy.</returns>
		[Pure]
		public static TkVector3 Normalize(TkVector3 vec) {
			var scale = 1.0f / vec.Length;
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to unit length.
		/// </summary>
		/// <param name="vec">The input vector.</param>
		/// <param name="result">The normalized vector.</param>
		public static void Normalize(in TkVector3 vec, out TkVector3 result) {
			var scale = 1.0f / vec.Length;
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
			result.Z = vec.Z * scale;
		}

		/// <summary>
		/// Scale a vector to approximately unit length.
		/// </summary>
		/// <param name="vec">The input vector.</param>
		/// <returns>The normalized copy.</returns>
		[Pure]
		public static TkVector3 NormalizeFast(TkVector3 vec) {
			var scale = MathHelper.InverseSqrtFast((vec.X * vec.X) + (vec.Y * vec.Y) + (vec.Z * vec.Z));
			vec.X *= scale;
			vec.Y *= scale;
			vec.Z *= scale;
			return vec;
		}

		/// <summary>
		/// Scale a vector to approximately unit length.
		/// </summary>
		/// <param name="vec">The input vector.</param>
		/// <param name="result">The normalized vector.</param>
		public static void NormalizeFast(in TkVector3 vec, out TkVector3 result) {
			var scale = MathHelper.InverseSqrtFast((vec.X * vec.X) + (vec.Y * vec.Y) + (vec.Z * vec.Z));
			result.X = vec.X * scale;
			result.Y = vec.Y * scale;
			result.Z = vec.Z * scale;
		}

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors.
		/// </summary>
		/// <param name="left">First operand.</param>
		/// <param name="right">Second operand.</param>
		/// <returns>The dot product of the two inputs.</returns>
		[Pure]
		public static float Dot(TkVector3 left, TkVector3 right) {
			return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
		}

		/// <summary>
		/// Calculate the dot (scalar) product of two vectors.
		/// </summary>
		/// <param name="left">First operand.</param>
		/// <param name="right">Second operand.</param>
		/// <param name="result">The dot product of the two inputs.</param>
		public static void Dot(in TkVector3 left, in TkVector3 right, out float result) {
			result = (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
		}

		/// <summary>
		/// Caclulate the cross (vector) product of two vectors.
		/// </summary>
		/// <param name="left">First operand.</param>
		/// <param name="right">Second operand.</param>
		/// <returns>The cross product of the two inputs.</returns>
		[Pure]
		public static TkVector3 Cross(TkVector3 left, TkVector3 right) {
			Cross(in left, in right, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Caclulate the cross (vector) product of two vectors.
		/// </summary>
		/// <param name="left">First operand.</param>
		/// <param name="right">Second operand.</param>
		/// <param name="result">The cross product of the two inputs.</param>
		public static void Cross(in TkVector3 left, in TkVector3 right, out TkVector3 result) {
			result.X = (left.Y * right.Z) - (left.Z * right.Y);
			result.Y = (left.Z * right.X) - (left.X * right.Z);
			result.Z = (left.X * right.Y) - (left.Y * right.X);
		}

		/// <summary>
		/// Returns a new vector that is the linear blend of the 2 given vectors.
		/// </summary>
		/// <param name="a">First input vector.</param>
		/// <param name="b">Second input vector.</param>
		/// <param name="blend">The blend factor.</param>
		/// <returns>a when blend=0, b when blend=1, and a linear combination otherwise.</returns>
		[Pure]
		public static TkVector3 Lerp(TkVector3 a, TkVector3 b, float blend) {
			a.X = (blend * (b.X - a.X)) + a.X;
			a.Y = (blend * (b.Y - a.Y)) + a.Y;
			a.Z = (blend * (b.Z - a.Z)) + a.Z;
			return a;
		}

		/// <summary>
		/// Returns a new vector that is the linear blend of the 2 given vectors.
		/// </summary>
		/// <param name="a">First input vector.</param>
		/// <param name="b">Second input vector.</param>
		/// <param name="blend">The blend factor.</param>
		/// <param name="result">a when blend=0, b when blend=1, and a linear combination otherwise.</param>
		public static void Lerp(in TkVector3 a, in TkVector3 b, float blend, out TkVector3 result) {
			result.X = (blend * (b.X - a.X)) + a.X;
			result.Y = (blend * (b.Y - a.Y)) + a.Y;
			result.Z = (blend * (b.Z - a.Z)) + a.Z;
		}

		/// <summary>
		/// Returns a new vector that is the component-wise linear blend of the 2 given vectors.
		/// </summary>
		/// <param name="a">First input vector.</param>
		/// <param name="b">Second input vector.</param>
		/// <param name="blend">The blend factor.</param>
		/// <returns>a when blend=0, b when blend=1, and a component-wise linear combination otherwise.</returns>
		[Pure]
		public static TkVector3 Lerp(TkVector3 a, TkVector3 b, TkVector3 blend) {
			a.X = (blend.X * (b.X - a.X)) + a.X;
			a.Y = (blend.Y * (b.Y - a.Y)) + a.Y;
			a.Z = (blend.Z * (b.Z - a.Z)) + a.Z;
			return a;
		}

		/// <summary>
		/// Returns a new vector that is the component-wise linear blend of the 2 given vectors.
		/// </summary>
		/// <param name="a">First input vector.</param>
		/// <param name="b">Second input vector.</param>
		/// <param name="blend">The blend factor.</param>
		/// <param name="result">a when blend=0, b when blend=1, and a component-wise linear combination otherwise.</param>
		public static void Lerp(in TkVector3 a, in TkVector3 b, TkVector3 blend, out TkVector3 result) {
			result.X = (blend.X * (b.X - a.X)) + a.X;
			result.Y = (blend.Y * (b.Y - a.Y)) + a.Y;
			result.Z = (blend.Z * (b.Z - a.Z)) + a.Z;
		}

		/// <summary>
		/// Interpolate 3 Vectors using Barycentric coordinates.
		/// </summary>
		/// <param name="a">First input Vector.</param>
		/// <param name="b">Second input Vector.</param>
		/// <param name="c">Third input Vector.</param>
		/// <param name="u">First Barycentric Coordinate.</param>
		/// <param name="v">Second Barycentric Coordinate.</param>
		/// <returns>a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c otherwise.</returns>
		[Pure]
		public static TkVector3 BaryCentric(TkVector3 a, TkVector3 b, TkVector3 c, float u, float v) {
			BaryCentric(in a, in b, in c, u, v, out var result);
			return result;
		}

		/// <summary>
		/// Interpolate 3 Vectors using Barycentric coordinates.
		/// </summary>
		/// <param name="a">First input Vector.</param>
		/// <param name="b">Second input Vector.</param>
		/// <param name="c">Third input Vector.</param>
		/// <param name="u">First Barycentric Coordinate.</param>
		/// <param name="v">Second Barycentric Coordinate.</param>
		/// <param name="result">
		/// Output Vector. a when u=v=0, b when u=1,v=0, c when u=0,v=1, and a linear combination of a,b,c
		/// otherwise.
		/// </param>
		[Pure]
		public static void BaryCentric
		(
			in TkVector3 a,
			in TkVector3 b,
			in TkVector3 c,
			float u,
			float v,
			out TkVector3 result
		) {
			Subtract(in b, in a, out var ab);
			Multiply(in ab, u, out var abU);
			Add(in a, in abU, out var uPos);

			Subtract(in c, in a, out var ac);
			Multiply(in ac, v, out var acV);
			Add(in uPos, in acV, out result);
		}

		/// <summary>
		/// Transform a direction vector by the given Matrix.
		/// Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed vector.</returns>
		[Pure]
		public static TkVector3 TransformVector(TkVector3 vec, TkMatrix4 mat) {
			TransformVector(in vec, in mat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a direction vector by the given Matrix.
		/// Assumes the matrix has a bottom row of (0,0,0,1), that is the translation part is ignored.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="result">The transformed vector.</param>
		public static void TransformVector(in TkVector3 vec, in TkMatrix4 mat, out TkVector3 result) {
			result.X = (vec.X * mat.Row0.X) +
					   (vec.Y * mat.Row1.X) +
					   (vec.Z * mat.Row2.X);

			result.Y = (vec.X * mat.Row0.Y) +
					   (vec.Y * mat.Row1.Y) +
					   (vec.Z * mat.Row2.Y);

			result.Z = (vec.X * mat.Row0.Z) +
					   (vec.Y * mat.Row1.Z) +
					   (vec.Z * mat.Row2.Z);
		}

		/// <summary>
		/// Transform a Normal by the given Matrix.
		/// </summary>
		/// <remarks>
		/// This calculates the inverse of the given matrix, use TransformNormalInverse if you
		/// already have the inverse to avoid this extra calculation.
		/// </remarks>
		/// <param name="norm">The normal to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed normal.</returns>
		[Pure]
		public static TkVector3 TransformNormal(TkVector3 norm, TkMatrix4 mat) {
			TransformNormal(in norm, in mat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a Normal by the given Matrix.
		/// </summary>
		/// <remarks>
		/// This calculates the inverse of the given matrix, use TransformNormalInverse if you
		/// already have the inverse to avoid this extra calculation.
		/// </remarks>
		/// <param name="norm">The normal to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="result">The transformed normal.</param>
		public static void TransformNormal(in TkVector3 norm, in TkMatrix4 mat, out TkVector3 result) {
			var inverse = TkMatrix4.Invert(mat);
			TransformNormalInverse(in norm, in inverse, out result);
		}

		/// <summary>
		/// Transform a Normal by the (transpose of the) given Matrix.
		/// </summary>
		/// <remarks>
		/// This version doesn't calculate the inverse matrix.
		/// Use this version if you already have the inverse of the desired transform to hand.
		/// </remarks>
		/// <param name="norm">The normal to transform.</param>
		/// <param name="invMat">The inverse of the desired transformation.</param>
		/// <returns>The transformed normal.</returns>
		[Pure]
		public static TkVector3 TransformNormalInverse(TkVector3 norm, TkMatrix4 invMat) {
			TransformNormalInverse(in norm, in invMat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a Normal by the (transpose of the) given Matrix.
		/// </summary>
		/// <remarks>
		/// This version doesn't calculate the inverse matrix.
		/// Use this version if you already have the inverse of the desired transform to hand.
		/// </remarks>
		/// <param name="norm">The normal to transform.</param>
		/// <param name="invMat">The inverse of the desired transformation.</param>
		/// <param name="result">The transformed normal.</param>
		public static void TransformNormalInverse(in TkVector3 norm, in TkMatrix4 invMat, out TkVector3 result) {
			result.X = (norm.X * invMat.Row0.X) +
					   (norm.Y * invMat.Row0.Y) +
					   (norm.Z * invMat.Row0.Z);

			result.Y = (norm.X * invMat.Row1.X) +
					   (norm.Y * invMat.Row1.Y) +
					   (norm.Z * invMat.Row1.Z);

			result.Z = (norm.X * invMat.Row2.X) +
					   (norm.Y * invMat.Row2.Y) +
					   (norm.Z * invMat.Row2.Z);
		}

		/// <summary>
		/// Transform a Position by the given Matrix.
		/// </summary>
		/// <param name="pos">The position to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed position.</returns>
		[Pure]
		public static TkVector3 TransformPosition(TkVector3 pos, TkMatrix4 mat) {
			TransformPosition(in pos, in mat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a Position by the given Matrix.
		/// </summary>
		/// <param name="pos">The position to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="result">The transformed position.</param>
		public static void TransformPosition(in TkVector3 pos, in TkMatrix4 mat, out TkVector3 result) {
			result.X = (pos.X * mat.Row0.X) +
					   (pos.Y * mat.Row1.X) +
					   (pos.Z * mat.Row2.X) +
					   mat.Row3.X;

			result.Y = (pos.X * mat.Row0.Y) +
					   (pos.Y * mat.Row1.Y) +
					   (pos.Z * mat.Row2.Y) +
					   mat.Row3.Y;

			result.Z = (pos.X * mat.Row0.Z) +
					   (pos.Y * mat.Row1.Z) +
					   (pos.Z * mat.Row2.Z) +
					   mat.Row3.Z;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed vector.</returns>
		[Pure]
		public static TkVector3 TransformRow(TkVector3 vec, TkMatrix3 mat) {
			TransformRow(in vec, in mat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="result">The transformed vector.</param>
		public static void TransformRow(in TkVector3 vec, in TkMatrix3 mat, out TkVector3 result) {
			result.X = (vec.X * mat.Row0.X) + (vec.Y * mat.Row1.X) + (vec.Z * mat.Row2.X);
			result.Y = (vec.X * mat.Row0.Y) + (vec.Y * mat.Row1.Y) + (vec.Z * mat.Row2.Y);
			result.Z = (vec.X * mat.Row0.Z) + (vec.Y * mat.Row1.Z) + (vec.Z * mat.Row2.Z);
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The result of the operation.</returns>
		[Pure]
		public static TkVector3 Transform(TkVector3 vec, TkQuaternion quat) {
			Transform(in vec, in quat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <param name="result">The result of the operation.</param>
		public static void Transform(in TkVector3 vec, in TkQuaternion quat, out TkVector3 result) {
			// Since vec.W == 0, we can optimize quat * vec * quat^-1 as follows:
			// vec + 2.0 * cross(quat.xyz, cross(quat.xyz, vec) + quat.w * vec)
			TkVector3 xyz = quat.Xyz;
			Cross(in xyz, in vec, out TkVector3 temp);
			Multiply(in vec, quat.W, out TkVector3 temp2);
			Add(in temp, in temp2, out temp);
			Cross(in xyz, in temp, out temp2);
			Multiply(in temp2, 2f, out temp2);
			Add(in vec, in temp2, out result);
		}

		/// <summary>
		/// Transform a Vector by the given Matrix using right-handed notation.
		/// </summary>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="vec">The vector to transform.</param>
		/// <returns>The transformed vector.</returns>
		[Pure]
		public static TkVector3 TransformColumn(TkMatrix3 mat, TkVector3 vec) {
			TransformColumn(in mat, in vec, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix using right-handed notation.
		/// </summary>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="result">The transformed vector.</param>
		public static void TransformColumn(in TkMatrix3 mat, in TkVector3 vec, out TkVector3 result) {
			result.X = (mat.Row0.X * vec.X) + (mat.Row0.Y * vec.Y) + (mat.Row0.Z * vec.Z);
			result.Y = (mat.Row1.X * vec.X) + (mat.Row1.Y * vec.Y) + (mat.Row1.Z * vec.Z);
			result.Z = (mat.Row2.X * vec.X) + (mat.Row2.Y * vec.Y) + (mat.Row2.Z * vec.Z);
		}

		/// <summary>
		/// Transform a TkVector3 by the given Matrix, and project the resulting TkVector4 back to a TkVector3.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <returns>The transformed vector.</returns>
		[Pure]
		public static TkVector3 TransformPerspective(TkVector3 vec, TkMatrix4 mat) {
			TransformPerspective(in vec, in mat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a TkVector3 by the given Matrix, and project the resulting TkVector4 back to a TkVector3.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="result">The transformed vector.</param>
		public static void TransformPerspective(in TkVector3 vec, in TkMatrix4 mat, out TkVector3 result) {
			var v = new TkVector4(vec.X, vec.Y, vec.Z, 1);
			TkVector4.TransformRow(in v, in mat, out v);
			result.X = v.X / v.W;
			result.Y = v.Y / v.W;
			result.Z = v.Z / v.W;
		}

		/// <summary>
		/// Calculates the angle (in radians) between two vectors.
		/// </summary>
		/// <param name="first">The first vector.</param>
		/// <param name="second">The second vector.</param>
		/// <returns>Angle (in radians) between the vectors.</returns>
		/// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
		[Pure]
		public static float CalculateAngle(TkVector3 first, TkVector3 second) {
			CalculateAngle(in first, in second, out float result);
			return result;
		}

		/// <summary>
		/// Calculates the angle (in radians) between two vectors.
		/// </summary>
		/// <param name="first">The first vector.</param>
		/// <param name="second">The second vector.</param>
		/// <param name="result">Angle (in radians) between the vectors.</param>
		/// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
		public static void CalculateAngle(in TkVector3 first, in TkVector3 second, out float result) {
			Dot(in first, in second, out float temp);
			result = (float)Math.Acos(MathHelper.Clamp(temp / (first.Length * second.Length), -1.0f, 1.0f));
		}

		/// <summary>
		/// Projects a vector from object space into screen space.
		/// </summary>
		/// <param name="vector">The vector to project.</param>
		/// <param name="x">The X coordinate of the viewport.</param>
		/// <param name="y">The Y coordinate of the viewport.</param>
		/// <param name="width">The width of the viewport.</param>
		/// <param name="height">The height of the viewport.</param>
		/// <param name="minZ">The minimum depth of the viewport.</param>
		/// <param name="maxZ">The maximum depth of the viewport.</param>
		/// <param name="worldViewProjection">The world-view-projection matrix.</param>
		/// <returns>The vector in screen space.</returns>
		/// <remarks>
		/// To project to normalized device coordinates (NDC) use the following parameters:
		/// Project(vector, -1, -1, 2, 2, -1, 1, worldViewProjection).
		/// </remarks>
		[Pure]
		public static TkVector3 Project
		(
			TkVector3 vector,
			float x,
			float y,
			float width,
			float height,
			float minZ,
			float maxZ,
			TkMatrix4 worldViewProjection
		) {
			TkVector4 result;

			result.X =
				(vector.X * worldViewProjection.M11) +
				(vector.Y * worldViewProjection.M21) +
				(vector.Z * worldViewProjection.M31) +
				worldViewProjection.M41;

			result.Y =
				(vector.X * worldViewProjection.M12) +
				(vector.Y * worldViewProjection.M22) +
				(vector.Z * worldViewProjection.M32) +
				worldViewProjection.M42;

			result.Z =
				(vector.X * worldViewProjection.M13) +
				(vector.Y * worldViewProjection.M23) +
				(vector.Z * worldViewProjection.M33) +
				worldViewProjection.M43;

			result.W =
				(vector.X * worldViewProjection.M14) +
				(vector.Y * worldViewProjection.M24) +
				(vector.Z * worldViewProjection.M34) +
				worldViewProjection.M44;

			result /= result.W;

			result.X = x + (width * ((result.X + 1.0f) / 2.0f));
			result.Y = y + (height * ((result.Y + 1.0f) / 2.0f));
			result.Z = minZ + ((maxZ - minZ) * ((result.Z + 1.0f) / 2.0f));

			return new TkVector3(result.X, result.Y, result.Z);
		}

		/// <summary>
		/// Projects a vector from screen space into object space.
		/// </summary>
		/// <param name="vector">The vector to project.</param>
		/// <param name="x">The X coordinate of the viewport.</param>
		/// <param name="y">The Y coordinate of the viewport.</param>
		/// <param name="width">The width of the viewport.</param>
		/// <param name="height">The height of the viewport.</param>
		/// <param name="minZ">The minimum depth of the viewport.</param>
		/// <param name="maxZ">The maximum depth of the viewport.</param>
		/// <param name="inverseWorldViewProjection">The inverse of the world-view-projection matrix.</param>
		/// <returns>The vector in object space.</returns>
		/// <remarks>
		/// To project from normalized device coordinates (NDC) use the following parameters:
		/// Project(vector, -1, -1, 2, 2, -1, 1, inverseWorldViewProjection).
		/// </remarks>
		[Pure]
		public static TkVector3 Unproject
		(
			TkVector3 vector,
			float x,
			float y,
			float width,
			float height,
			float minZ,
			float maxZ,
			TkMatrix4 inverseWorldViewProjection
		) {
			float tempX = ((vector.X - x) / width * 2.0f) - 1.0f;
			float tempY = ((vector.Y - y) / height * 2.0f) - 1.0f;
			float tempZ = ((vector.Z - minZ) / (maxZ - minZ) * 2.0f) - 1.0f;

			TkVector3 result;
			result.X =
				(tempX * inverseWorldViewProjection.M11) +
				(tempY * inverseWorldViewProjection.M21) +
				(tempZ * inverseWorldViewProjection.M31) +
				inverseWorldViewProjection.M41;

			result.Y =
				(tempX * inverseWorldViewProjection.M12) +
				(tempY * inverseWorldViewProjection.M22) +
				(tempZ * inverseWorldViewProjection.M32) +
				inverseWorldViewProjection.M42;

			result.Z =
				(tempX * inverseWorldViewProjection.M13) +
				(tempY * inverseWorldViewProjection.M23) +
				(tempZ * inverseWorldViewProjection.M33) +
				inverseWorldViewProjection.M43;

			float tempW =
				(tempX * inverseWorldViewProjection.M14) +
				(tempY * inverseWorldViewProjection.M24) +
				(tempZ * inverseWorldViewProjection.M34) +
				inverseWorldViewProjection.M44;

			result /= tempW;

			return result;
		}

		/// <summary>
		/// Gets or sets an OpenTK.Vector2 with the X and Y components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector2 Xy {
			get => new TkVector2(X, Y);
			set {
				X = value.X;
				Y = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector2 with the X and Z components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector2 Xz {
			get => new TkVector2(X, Z);
			set {
				X = value.X;
				Z = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector2 with the Y and X components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector2 Yx {
			get => new TkVector2(Y, X);
			set {
				Y = value.X;
				X = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector2 with the Y and Z components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector2 Yz {
			get => new TkVector2(Y, Z);
			set {
				Y = value.X;
				Z = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector2 with the Z and X components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector2 Zx {
			get => new TkVector2(Z, X);
			set {
				Z = value.X;
				X = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector2 with the Z and Y components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector2 Zy {
			get => new TkVector2(Z, Y);
			set {
				Z = value.X;
				Y = value.Y;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector3 with the X, Z, and Y components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector3 Xzy {
			get => new TkVector3(X, Z, Y);
			set {
				X = value.X;
				Z = value.Y;
				Y = value.Z;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector3 with the Y, X, and Z components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector3 Yxz {
			get => new TkVector3(Y, X, Z);
			set {
				Y = value.X;
				X = value.Y;
				Z = value.Z;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector3 with the Y, Z, and X components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector3 Yzx {
			get => new TkVector3(Y, Z, X);
			set {
				Y = value.X;
				Z = value.Y;
				X = value.Z;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector3 with the Z, X, and Y components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector3 Zxy {
			get => new TkVector3(Z, X, Y);
			set {
				Z = value.X;
				X = value.Y;
				Y = value.Z;
			}
		}

		/// <summary>
		/// Gets or sets an OpenTK.TkVector3 with the Z, Y, and X components of this instance.
		/// </summary>
		[XmlIgnore]
		public TkVector3 Zyx {
			get => new TkVector3(Z, Y, X);
			set {
				Z = value.X;
				Y = value.Y;
				X = value.Z;
			}
		}

		/// <summary>
		/// Adds two instances.
		/// </summary>
		/// <param name="left">The first instance.</param>
		/// <param name="right">The second instance.</param>
		/// <returns>The result of the calculation.</returns>
		[Pure]
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
		[Pure]
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
		[Pure]
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
		[Pure]
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
		[Pure]
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
		[Pure]
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
		[Pure]
		public static TkVector3 operator *(TkVector3 vec, TkMatrix3 mat) {
			TransformRow(in vec, in mat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transform a Vector by the given Matrix using right-handed notation.
		/// </summary>
		/// <param name="mat">The desired transformation.</param>
		/// <param name="vec">The vector to transform.</param>
		/// <returns>The transformed vector.</returns>
		[Pure]
		public static TkVector3 operator *(TkMatrix3 mat, TkVector3 vec) {
			TransformColumn(in mat, in vec, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Transforms a vector by a quaternion rotation.
		/// </summary>
		/// <param name="vec">The vector to transform.</param>
		/// <param name="quat">The quaternion to rotate the vector by.</param>
		/// <returns>The multiplied vector.</returns>
		[Pure]
		public static TkVector3 operator *(TkQuaternion quat, TkVector3 vec) {
			Transform(in vec, in quat, out TkVector3 result);
			return result;
		}

		/// <summary>
		/// Divides an instance by a scalar.
		/// </summary>
		/// <param name="vec">The instance.</param>
		/// <param name="scale">The scalar.</param>
		/// <returns>The result of the calculation.</returns>
		[Pure]
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
		[Pure]
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
		/// <returns>True, if left does not equal right; false otherwise.</returns>
		public static bool operator !=(TkVector3 left, TkVector3 right) {
			return !(left == right);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TkVector3"/> struct using a tuple containing the component
		/// values.
		/// </summary>
		/// <param name="values">A tuple containing the component values.</param>
		/// <returns>A new instance of the <see cref="TkVector3"/> struct with the given component values.</returns>
		[Pure]
		public static implicit operator TkVector3((float X, float Y, float Z) values) {
			return new TkVector3(values.X, values.Y, values.Z);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return String.Format("({0}, {1}, {2})", X, Y, Z);
		}

		/// <inheritdoc />
		public override bool Equals(object obj) {
			return obj is TkVector3 && Equals((TkVector3)obj);
		}

		/// <inheritdoc />
		public bool Equals(TkVector3 other) {
			return X == other.X &&
				   Y == other.Y &&
				   Z == other.Z;
		}

		/// <inheritdoc />
		public override int GetHashCode() {
			return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
		}

		/// <summary>
		/// Deconstructs the vector into it's individual components.
		/// </summary>
		/// <param name="x">The X component of the vector.</param>
		/// <param name="y">The Y component of the vector.</param>
		/// <param name="z">The Z component of the vector.</param>
		[Pure]
		public void Deconstruct(out float x, out float y, out float z) {
			x = X;
			y = Y;
			z = Z;
		}

		public void Write(BinaryWriter writer) {
			writer.Write(X);
			writer.Write(Y);
			writer.Write(Z);
		}
	}
}
