﻿/*
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GRF.Graphics
{
    /// <summary>
    /// Represents a 3x3 matrix containing 3D rotation and scale.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct TkMatrix3 : IEquatable<TkMatrix3>, IFormattable
    {
        /// <summary>
        /// First row of the matrix.
        /// </summary>
        public TkVector3 Row0;

        /// <summary>
        /// Second row of the matrix.
        /// </summary>
        public TkVector3 Row1;

        /// <summary>
        /// Third row of the matrix.
        /// </summary>
        public TkVector3 Row2;

        /// <summary>
        /// The identity matrix.
        /// </summary>
        public static readonly TkMatrix3 Identity = new TkMatrix3(TkVector3.UnitX, TkVector3.UnitY, TkVector3.UnitZ);

        /// <summary>
        /// The zero matrix.
        /// </summary>
        public static readonly TkMatrix3 Zero = new TkMatrix3(TkVector3.Zero, TkVector3.Zero, TkVector3.Zero);

        /// <summary>
        /// Initializes a new instance of the <see cref="TkMatrix3"/> struct.
        /// </summary>
        /// <param name="row0">Top row of the matrix.</param>
        /// <param name="row1">Second row of the matrix.</param>
        /// <param name="row2">Bottom row of the matrix.</param>
        public TkMatrix3(TkVector3 row0, TkVector3 row1, TkVector3 row2)
        {
            Row0 = row0;
            Row1 = row1;
            Row2 = row2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TkMatrix3"/> struct.
        /// </summary>
        /// <param name="m00">First item of the first row of the matrix.</param>
        /// <param name="m01">Second item of the first row of the matrix.</param>
        /// <param name="m02">Third item of the first row of the matrix.</param>
        /// <param name="m10">First item of the second row of the matrix.</param>
        /// <param name="m11">Second item of the second row of the matrix.</param>
        /// <param name="m12">Third item of the second row of the matrix.</param>
        /// <param name="m20">First item of the third row of the matrix.</param>
        /// <param name="m21">Second item of the third row of the matrix.</param>
        /// <param name="m22">Third item of the third row of the matrix.</param>
        [SuppressMessage("ReSharper", "SA1117", Justification = "For better readability of Matrix struct.")]
        public TkMatrix3
        (
            float m00, float m01, float m02,
            float m10, float m11, float m12,
            float m20, float m21, float m22
        )
        {
            Row0 = new TkVector3(m00, m01, m02);
            Row1 = new TkVector3(m10, m11, m12);
            Row2 = new TkVector3(m20, m21, m22);
        }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="TkMatrix3"/> struct.
        ///// </summary>
        ///// <param name="matrix">A Matrix4 to take the upper-left 3x3 from.</param>
        //public TkMatrix3(Matrix4 matrix)
        //{
        //    Row0 = matrix.Row0.Xyz;
        //    Row1 = matrix.Row1.Xyz;
        //    Row2 = matrix.Row2.Xyz;
        //}

        /// <summary>
        /// Gets the determinant of this matrix.
        /// </summary>
        public float Determinant
        {
            get
            {
                float m11 = Row0.X;
                float m12 = Row0.Y;
                float m13 = Row0.Z;
                float m21 = Row1.X;
                float m22 = Row1.Y;
                float m23 = Row1.Z;
                float m31 = Row2.X;
                float m32 = Row2.Y;
                float m33 = Row2.Z;

                return (m11 * m22 * m33) + (m12 * m23 * m31) + (m13 * m21 * m32)
                       - (m13 * m22 * m31) - (m11 * m23 * m32) - (m12 * m21 * m33);
            }
        }

	    /// <summary>
	    /// Gets the first column of this matrix.
	    /// </summary>
	    public TkVector3 Column0 {
		    get { return new TkVector3(Row0.X, Row1.X, Row2.X); }
	    }

	    /// <summary>
        /// Gets the second column of this matrix.
        /// </summary>
        public TkVector3 Column1 {
		    get { return new TkVector3(Row0.Y, Row1.Y, Row2.Y); }
	    }

        /// <summary>
        /// Gets the third column of this matrix.
        /// </summary>
        public TkVector3 Column2 {
		    get { return new TkVector3(Row0.Z, Row1.Z, Row2.Z); }
	    }

	    /// <summary>
	    /// Gets or sets the value at row 1, column 1 of this instance.
	    /// </summary>
	    public float M11 {
		    get { return Row0.X; }
		    set { Row0.X = value; }
	    }

	    /// <summary>
        /// Gets or sets the value at row 1, column 2 of this instance.
        /// </summary>
        public float M12 {
		    get { return Row0.Y; }
		    set { Row0.Y = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 1, column 3 of this instance.
        /// </summary>
        public float M13 {
		    get { return Row0.Z; }
		    set { Row0.Z = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 2, column 1 of this instance.
        /// </summary>
        public float M21 {
		    get { return Row1.X; }
		    set { Row1.X = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 2, column 2 of this instance.
        /// </summary>
        public float M22 {
		    get { return Row1.Y; }
		    set { Row1.Y = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 2, column 3 of this instance.
        /// </summary>
        public float M23 {
		    get { return Row1.Z; }
		    set { Row1.Z = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 3, column 1 of this instance.
        /// </summary>
        public float M31 {
		    get { return Row2.X; }
		    set { Row2.X = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 3, column 2 of this instance.
        /// </summary>
        public float M32 {
		    get { return Row2.Y; }
		    set { Row2.Y = value; }
	    }

        /// <summary>
        /// Gets or sets the value at row 3, column 3 of this instance.
        /// </summary>
        public float M33 {
		    get { return Row2.Z; }
		    set { Row2.Z = value; }
	    }

	    /// <summary>
	    /// Gets or sets the values along the main diagonal of the matrix.
	    /// </summary>
	    public TkVector3 Diagonal {
		    get { return new TkVector3(Row0.X, Row1.Y, Row2.Z); }
		    set {
			    Row0.X = value.X;
			    Row1.Y = value.Y;
			    Row2.Z = value.Z;
		    }
	    }

	    /// <summary>
	    /// Gets the trace of the matrix, the sum of the values along the diagonal.
	    /// </summary>
	    public float Trace {
		    get { return Row0.X + Row1.Y + Row2.Z; }
	    }

	    /// <summary>
	    /// Gets or sets the value at a specified row and column.
	    /// </summary>
	    /// <param name="rowIndex">The index of the row.</param>
	    /// <param name="columnIndex">The index of the column.</param>
	    /// <returns>The element at the given row and column index.</returns>
	    public float this[int rowIndex, int columnIndex] {
		    get {
			    if (rowIndex == 0) {
				    return Row0[columnIndex];
			    }

			    if (rowIndex == 1) {
				    return Row1[columnIndex];
			    }

			    if (rowIndex == 2) {
				    return Row2[columnIndex];
			    }

			    throw new IndexOutOfRangeException("You tried to access this matrix at: (" + rowIndex + ", " +
			                                       columnIndex + ")");
		    }

		    set {
			    if (rowIndex == 0) {
				    Row0[columnIndex] = value;
			    }
			    else if (rowIndex == 1) {
				    Row1[columnIndex] = value;
			    }
			    else if (rowIndex == 2) {
				    Row2[columnIndex] = value;
			    }
			    else {
				    throw new IndexOutOfRangeException("You tried to set this matrix at: (" + rowIndex + ", " +
				                                       columnIndex + ")");
			    }
		    }
	    }

		/// <summary>Gets the component at the index into the matrix.</summary>
		/// <param name="index">The index into the components of the matrix.</param>
		/// <returns>The component at the given index into the matrix.</returns>
		public float this[int index] {
			get {
				switch (index) {
					case 0: return M11;
					case 1: return M12;
					case 2: return M13;
					case 3: return M21;
					case 4: return M22;
					case 5: return M23;
					case 6: return M31;
					case 7: return M32;
					case 8: return M33;
					default: throw new IndexOutOfRangeException();
				}
			}
			set {
				switch (index) {
					case 0: M11 = value; return;
					case 1: M12 = value; return;
					case 2: M13 = value; return;
					case 3: M21 = value; return;
					case 4: M22 = value; return;
					case 5: M23 = value; return;
					case 6: M31 = value; return;
					case 7: M32 = value; return;
					case 8: M33 = value; return;
					default: throw new IndexOutOfRangeException();
				}
			}
		}

	    /// <summary>
        /// Converts this instance into its inverse.
        /// </summary>
        public void Invert()
        {
            this = Invert(this);
        }

        /// <summary>
        /// Converts this instance into its transpose.
        /// </summary>
        public void Transpose()
        {
            this = Transpose(this);
        }

        /// <summary>
        /// Returns a normalized copy of this instance.
        /// </summary>
        /// <returns>The normalized copy.</returns>
        public TkMatrix3 Normalized()
        {
            var m = this;
            m.Normalize();
            return m;
        }

        /// <summary>
        /// Divides each element in the Matrix by the <see cref="Determinant"/>.
        /// </summary>
        public void Normalize()
        {
            var determinant = Determinant;
            Row0 /= determinant;
            Row1 /= determinant;
            Row2 /= determinant;
        }

        /// <summary>
        /// Returns an inverted copy of this instance.
        /// </summary>
        /// <returns>The inverted copy.</returns>
        public TkMatrix3 Inverted()
        {
            var m = this;
            if (m.Determinant != 0)
            {
                m.Invert();
            }

            return m;
        }

        /// <summary>
        /// Returns a copy of this TkMatrix3 without scale.
        /// </summary>
        /// <returns>The matrix without scaling.</returns>
        public TkMatrix3 ClearScale()
        {
            var m = this;
            m.Row0 = m.Row0.Normalized();
            m.Row1 = m.Row1.Normalized();
            m.Row2 = m.Row2.Normalized();
            return m;
        }

        /// <summary>
        /// Returns a copy of this TkMatrix3 without rotation.
        /// </summary>
        /// <returns>The matrix without rotation.</returns>
        public TkMatrix3 ClearRotation()
        {
            var m = this;
            m.Row0 = new TkVector3(m.Row0.Length, 0, 0);
            m.Row1 = new TkVector3(0, m.Row1.Length, 0);
            m.Row2 = new TkVector3(0, 0, m.Row2.Length);
            return m;
        }

        /// <summary>
        /// Returns the scale component of this instance.
        /// </summary>
        /// <returns>The scale components.</returns>
        public TkVector3 ExtractScale()
        {
            return new TkVector3(Row0.Length, Row1.Length, Row2.Length);
        }

        /// <summary>
        /// Returns the rotation component of this instance. Quite slow.
        /// </summary>
        /// <param name="rowNormalize">
        /// Whether the method should row-normalize (i.e. remove scale from) the Matrix. Pass false if
        /// you know it's already normalized.
        /// </param>
        /// <returns>The rotation.</returns>
        public TkQuaternion ExtractRotation(bool rowNormalize = true)
        {
            var row0 = Row0;
            var row1 = Row1;
            var row2 = Row2;

            if (rowNormalize)
            {
                row0 = row0.Normalized();
                row1 = row1.Normalized();
                row2 = row2.Normalized();
            }

            // code below adapted from Blender
            var q = default(TkQuaternion);
            var trace = 0.25 * (row0[0] + row1[1] + row2[2] + 1.0);

            if (trace > 0)
            {
                var sq = Math.Sqrt(trace);

                q.W = (float)sq;
                sq = 1.0 / (4.0 * sq);
                q.X = (float)((row1[2] - row2[1]) * sq);
                q.Y = (float)((row2[0] - row0[2]) * sq);
                q.Z = (float)((row0[1] - row1[0]) * sq);
            }
            else if (row0[0] > row1[1] && row0[0] > row2[2])
            {
                var sq = 2.0 * Math.Sqrt(1.0 + row0[0] - row1[1] - row2[2]);

                q.X = (float)(0.25 * sq);
                sq = 1.0 / sq;
                q.W = (float)((row2[1] - row1[2]) * sq);
                q.Y = (float)((row1[0] + row0[1]) * sq);
                q.Z = (float)((row2[0] + row0[2]) * sq);
            }
            else if (row1[1] > row2[2])
            {
                var sq = 2.0 * Math.Sqrt(1.0 + row1[1] - row0[0] - row2[2]);

                q.Y = (float)(0.25 * sq);
                sq = 1.0 / sq;
                q.W = (float)((row2[0] - row0[2]) * sq);
                q.X = (float)((row1[0] + row0[1]) * sq);
                q.Z = (float)((row2[1] + row1[2]) * sq);
            }
            else
            {
                var sq = 2.0 * Math.Sqrt(1.0 + row2[2] - row0[0] - row1[1]);

                q.Z = (float)(0.25 * sq);
                sq = 1.0 / sq;
                q.W = (float)((row1[0] - row0[1]) * sq);
                q.X = (float)((row2[0] + row0[2]) * sq);
                q.Y = (float)((row2[1] + row1[2]) * sq);
            }

            q.Normalize();
            return q;
        }

        /// <summary>
        /// Build a rotation matrix from the specified axis/angle rotation.
        /// </summary>
        /// <param name="axis">The axis to rotate about.</param>
        /// <param name="angle">Angle in radians to rotate counter-clockwise (looking in the direction of the given axis).</param>
        /// <param name="result">A matrix instance.</param>
        public static void CreateFromAxisAngle(TkVector3 axis, float angle, ref TkMatrix3 result)
        {
            // normalize and create a local copy of the vector.
            axis.Normalize();
            float axisX = axis.X, axisY = axis.Y, axisZ = axis.Z;

            // calculate angles
            var cos = (float)Math.Cos(-angle);
            var sin = (float)Math.Sin(-angle);
            var t = 1.0f - cos;

            // do the conversion math once
            float tXX = t * axisX * axisX;
            float tXY = t * axisX * axisY;
            float tXZ = t * axisX * axisZ;
            float tYY = t * axisY * axisY;
            float tYZ = t * axisY * axisZ;
            float tZZ = t * axisZ * axisZ;

            float sinX = sin * axisX;
            float sinY = sin * axisY;
            float sinZ = sin * axisZ;

            result.Row0.X = tXX + cos;
            result.Row0.Y = tXY - sinZ;
            result.Row0.Z = tXZ + sinY;
            result.Row1.X = tXY + sinZ;
            result.Row1.Y = tYY + cos;
            result.Row1.Z = tYZ - sinX;
            result.Row2.X = tXZ - sinY;
            result.Row2.Y = tYZ + sinX;
            result.Row2.Z = tZZ + cos;
        }

        /// <summary>
        /// Build a rotation matrix from the specified axis/angle rotation.
        /// </summary>
        /// <param name="axis">The axis to rotate about.</param>
        /// <param name="angle">Angle in radians to rotate counter-clockwise (looking in the direction of the given axis).</param>
        /// <returns>A matrix instance.</returns>
        public static TkMatrix3 CreateFromAxisAngle(TkVector3 axis, float angle) {
	        TkMatrix3 result = new TkMatrix3();
            CreateFromAxisAngle(axis, angle, ref result);
            return result;
        }

        /// <summary>
        /// Build a rotation matrix from the specified quaternion.
        /// </summary>
        /// <param name="q">TkQuaternion to translate.</param>
        /// <param name="result">Matrix result.</param>
        public static void CreateFromQuaternion(ref TkQuaternion q, ref TkMatrix3 result)
        {
            // Adapted from https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation#TkQuaternion-derived_rotation_matrix
            // with the caviat that opentk uses row-major matrices so the matrix we create is transposed
            float sqx = q.X * q.X;
            float sqy = q.Y * q.Y;
            float sqz = q.Z * q.Z;
            float sqw = q.W * q.W;

            float xy = q.X * q.Y;
            float xz = q.X * q.Z;
            float xw = q.X * q.W;

            float yz = q.Y * q.Z;
            float yw = q.Y * q.W;

            float zw = q.Z * q.W;

            float s2 = 2f / (sqx + sqy + sqz + sqw);

            result.Row0.X = 1f - (s2 * (sqy + sqz));
            result.Row1.Y = 1f - (s2 * (sqx + sqz));
            result.Row2.Z = 1f - (s2 * (sqx + sqy));

            result.Row0.Y = s2 * (xy + zw);
            result.Row1.X = s2 * (xy - zw);

            result.Row2.X = s2 * (xz + yw);
            result.Row0.Z = s2 * (xz - yw);

            result.Row2.Y = s2 * (yz - xw);
            result.Row1.Z = s2 * (yz + xw);
        }

        /// <summary>
        /// Build a rotation matrix from the specified quaternion.
        /// </summary>
        /// <param name="q">TkQuaternion to translate.</param>
        /// <returns>A matrix instance.</returns>
        public static TkMatrix3 CreateFromQuaternion(TkQuaternion q)
        {
			var result = new TkMatrix3();
            CreateFromQuaternion(ref q, ref result);
            return result;
        }

        /// <summary>
        /// Builds a rotation matrix for a rotation around the x-axis.
        /// </summary>
        /// <param name="angle">The counter-clockwise angle in radians.</param>
        /// <param name="result">The resulting TkMatrix3 instance.</param>
        public static void CreateRotationX(float angle, out TkMatrix3 result)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            result = Identity;
            result.Row1.Y = cos;
            result.Row1.Z = sin;
            result.Row2.Y = -sin;
            result.Row2.Z = cos;
        }

        /// <summary>
        /// Builds a rotation matrix for a rotation around the x-axis.
        /// </summary>
        /// <param name="angle">The counter-clockwise angle in radians.</param>
        /// <returns>The resulting TkMatrix3 instance.</returns>
        public static TkMatrix3 CreateRotationX(float angle)
        {
			var result = new TkMatrix3();
            CreateRotationX(angle, out result);
            return result;
        }

        /// <summary>
        /// Builds a rotation matrix for a rotation around the y-axis.
        /// </summary>
        /// <param name="angle">The counter-clockwise angle in radians.</param>
        /// <param name="result">The resulting TkMatrix3 instance.</param>
        public static void CreateRotationY(float angle, ref TkMatrix3 result)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            result = Identity;
            result.Row0.X = cos;
            result.Row0.Z = -sin;
            result.Row2.X = sin;
            result.Row2.Z = cos;
        }

        /// <summary>
        /// Builds a rotation matrix for a rotation around the y-axis.
        /// </summary>
        /// <param name="angle">The counter-clockwise angle in radians.</param>
        /// <returns>The resulting TkMatrix3 instance.</returns>
        public static TkMatrix3 CreateRotationY(float angle)
        {
			var result = new TkMatrix3();
            CreateRotationY(angle, ref result);
            return result;
        }

        /// <summary>
        /// Builds a rotation matrix for a rotation around the z-axis.
        /// </summary>
        /// <param name="angle">The counter-clockwise angle in radians.</param>
        /// <param name="result">The resulting TkMatrix3 instance.</param>
        public static void CreateRotationZ(float angle, ref TkMatrix3 result)
        {
            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            result = Identity;
            result.Row0.X = cos;
            result.Row0.Y = sin;
            result.Row1.X = -sin;
            result.Row1.Y = cos;
        }

        /// <summary>
        /// Builds a rotation matrix for a rotation around the z-axis.
        /// </summary>
        /// <param name="angle">The counter-clockwise angle in radians.</param>
        /// <returns>The resulting TkMatrix3 instance.</returns>
        public static TkMatrix3 CreateRotationZ(float angle)
        {
			var result = new TkMatrix3();
            CreateRotationZ(angle, ref result);
            return result;
        }

        /// <summary>
        /// Creates a scale matrix.
        /// </summary>
        /// <param name="scale">Single scale factor for the x, y, and z axes.</param>
        /// <returns>A scale matrix.</returns>
        public static TkMatrix3 CreateScale(float scale)
        {
			var result = new TkMatrix3();
            CreateScale(scale, ref result);
            return result;
        }

        /// <summary>
        /// Creates a scale matrix.
        /// </summary>
        /// <param name="scale">Scale factors for the x, y, and z axes.</param>
        /// <returns>A scale matrix.</returns>
        public static TkMatrix3 CreateScale(TkVector3 scale)
        {
			var result = new TkMatrix3();
            CreateScale(ref scale, ref result);
            return result;
        }

        /// <summary>
        /// Creates a scale matrix.
        /// </summary>
        /// <param name="x">Scale factor for the x axis.</param>
        /// <param name="y">Scale factor for the y axis.</param>
        /// <param name="z">Scale factor for the z axis.</param>
        /// <returns>A scale matrix.</returns>
        public static TkMatrix3 CreateScale(float x, float y, float z)
        {
			var result = new TkMatrix3();
            CreateScale(x, y, z, ref result);
            return result;
        }

        /// <summary>
        /// Creates a scale matrix.
        /// </summary>
        /// <param name="scale">Single scale factor for the x, y, and z axes.</param>
        /// <param name="result">A scale matrix.</param>
        public static void CreateScale(float scale, ref TkMatrix3 result)
        {
            result = Identity;
            result.Row0.X = scale;
            result.Row1.Y = scale;
            result.Row2.Z = scale;
        }

        /// <summary>
        /// Creates a scale matrix.
        /// </summary>
        /// <param name="scale">Scale factors for the x, y, and z axes.</param>
        /// <param name="result">A scale matrix.</param>
        public static void CreateScale(ref TkVector3 scale, ref TkMatrix3 result)
        {
            result = Identity;
            result.Row0.X = scale.X;
            result.Row1.Y = scale.Y;
            result.Row2.Z = scale.Z;
        }

        /// <summary>
        /// Creates a scale matrix.
        /// </summary>
        /// <param name="x">Scale factor for the x axis.</param>
        /// <param name="y">Scale factor for the y axis.</param>
        /// <param name="z">Scale factor for the z axis.</param>
        /// <param name="result">A scale matrix.</param>
        public static void CreateScale(float x, float y, float z, ref TkMatrix3 result)
        {
            result = Identity;
            result.Row0.X = x;
            result.Row1.Y = y;
            result.Row2.Z = z;
        }

        /// <summary>
        /// Adds two instances.
        /// </summary>
        /// <param name="left">The left operand of the addition.</param>
        /// <param name="right">The right operand of the addition.</param>
        /// <returns>A new instance that is the result of the addition.</returns>
        public static TkMatrix3 Add(TkMatrix3 left, TkMatrix3 right) {
	        var result = new TkMatrix3();
            Add(ref left, ref right, ref result);
            return result;
        }

        /// <summary>
        /// Adds two instances.
        /// </summary>
        /// <param name="left">The left operand of the addition.</param>
        /// <param name="right">The right operand of the addition.</param>
        /// <param name="result">A new instance that is the result of the addition.</param>
        public static void Add(ref TkMatrix3 left, ref TkMatrix3 right, ref TkMatrix3 result)
        {
            TkVector3.Add(ref left.Row0, ref right.Row0, ref result.Row0);
            TkVector3.Add(ref left.Row1, ref right.Row1, ref result.Row1);
            TkVector3.Add(ref left.Row2, ref right.Row2, ref result.Row2);
        }

        /// <summary>
        /// Multiplies two instances.
        /// </summary>
        /// <param name="left">The left operand of the multiplication.</param>
        /// <param name="right">The right operand of the multiplication.</param>
        /// <returns>A new instance that is the result of the multiplication.</returns>
        public static TkMatrix3 Mult(TkMatrix3 left, TkMatrix3 right) {
	        var result = new TkMatrix3();
            Mult(ref left, ref right, ref result);
            return result;
        }

        /// <summary>
        /// Multiplies two instances.
        /// </summary>
        /// <param name="left">The left operand of the multiplication.</param>
        /// <param name="right">The right operand of the multiplication.</param>
        /// <param name="result">A new instance that is the result of the multiplication.</param>
        public static void Mult(ref TkMatrix3 left, ref TkMatrix3 right, ref TkMatrix3 result)
        {
            float leftM11 = left.Row0.X;
            float leftM12 = left.Row0.Y;
            float leftM13 = left.Row0.Z;
            float leftM21 = left.Row1.X;
            float leftM22 = left.Row1.Y;
            float leftM23 = left.Row1.Z;
            float leftM31 = left.Row2.X;
            float leftM32 = left.Row2.Y;
            float leftM33 = left.Row2.Z;
            float rightM11 = right.Row0.X;
            float rightM12 = right.Row0.Y;
            float rightM13 = right.Row0.Z;
            float rightM21 = right.Row1.X;
            float rightM22 = right.Row1.Y;
            float rightM23 = right.Row1.Z;
            float rightM31 = right.Row2.X;
            float rightM32 = right.Row2.Y;
            float rightM33 = right.Row2.Z;

            result.Row0.X = (leftM11 * rightM11) + (leftM12 * rightM21) + (leftM13 * rightM31);
            result.Row0.Y = (leftM11 * rightM12) + (leftM12 * rightM22) + (leftM13 * rightM32);
            result.Row0.Z = (leftM11 * rightM13) + (leftM12 * rightM23) + (leftM13 * rightM33);
            result.Row1.X = (leftM21 * rightM11) + (leftM22 * rightM21) + (leftM23 * rightM31);
            result.Row1.Y = (leftM21 * rightM12) + (leftM22 * rightM22) + (leftM23 * rightM32);
            result.Row1.Z = (leftM21 * rightM13) + (leftM22 * rightM23) + (leftM23 * rightM33);
            result.Row2.X = (leftM31 * rightM11) + (leftM32 * rightM21) + (leftM33 * rightM31);
            result.Row2.Y = (leftM31 * rightM12) + (leftM32 * rightM22) + (leftM33 * rightM32);
            result.Row2.Z = (leftM31 * rightM13) + (leftM32 * rightM23) + (leftM33 * rightM33);
        }

        /// <summary>
        /// Calculate the inverse of the given matrix.
        /// </summary>
        /// <param name="matrix">The matrix to invert.</param>
        /// <param name="result">The inverse of the given matrix if it has one, or the input if it is singular.</param>
        /// <exception cref="InvalidOperationException">Thrown if the TkMatrix3 is singular.</exception>
        public static void Invert(ref TkMatrix3 matrix, ref TkMatrix3 result)
        {
            // Original implementation can be found here:
            // https://github.com/niswegmann/small-matrix-inverse/blob/6eac02b84ad06870692abaf828638a391548502c/invert3x3_c.h
            float row0X = matrix.Row0.X, row0Y = matrix.Row0.Y, row0Z = matrix.Row0.Z;
            float row1X = matrix.Row1.X, row1Y = matrix.Row1.Y, row1Z = matrix.Row1.Z;
            float row2X = matrix.Row2.X, row2Y = matrix.Row2.Y, row2Z = matrix.Row2.Z;

            // Compute the elements needed to calculate the determinant
            // so that we can throw without writing anything to the out parameter.
            float invRow0X = (+row1Y * row2Z) - (row1Z * row2Y);
            float invRow1X = (-row1X * row2Z) + (row1Z * row2X);
            float invRow2X = (+row1X * row2Y) - (row1Y * row2X);

            // Compute determinant:
            float det = (row0X * invRow0X) + (row0Y * invRow1X) + (row0Z * invRow2X);

            if (det == 0f)
            {
                throw new InvalidOperationException("Matrix is singular and cannot be inverted.");
            }

            // Compute adjoint:
            result.Row0.X = invRow0X;
            result.Row0.Y = (-row0Y * row2Z) + (row0Z * row2Y);
            result.Row0.Z = (+row0Y * row1Z) - (row0Z * row1Y);
            result.Row1.X = invRow1X;
            result.Row1.Y = (+row0X * row2Z) - (row0Z * row2X);
            result.Row1.Z = (-row0X * row1Z) + (row0Z * row1X);
            result.Row2.X = invRow2X;
            result.Row2.Y = (-row0X * row2Y) + (row0Y * row2X);
            result.Row2.Z = (+row0X * row1Y) - (row0Y * row1X);

            // Multiply adjoint with reciprocal of determinant:
            det = 1.0f / det;

            result.Row0.X *= det;
            result.Row0.Y *= det;
            result.Row0.Z *= det;
            result.Row1.X *= det;
            result.Row1.Y *= det;
            result.Row1.Z *= det;
            result.Row2.X *= det;
            result.Row2.Y *= det;
            result.Row2.Z *= det;
        }

        /// <summary>
        /// Calculate the inverse of the given matrix.
        /// </summary>
        /// <param name="mat">The matrix to invert.</param>
        /// <returns>The inverse of the given matrix.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the Matrix4 is singular.</exception>
        public static TkMatrix3 Invert(TkMatrix3 mat)
        {
			var result = new TkMatrix3();
            Invert(ref mat, ref result);
            return result;
        }

        /// <summary>
        /// Calculate the transpose of the given matrix.
        /// </summary>
        /// <param name="mat">The matrix to transpose.</param>
        /// <returns>The transpose of the given matrix.</returns>
        public static TkMatrix3 Transpose(TkMatrix3 mat)
        {
            return new TkMatrix3(mat.Column0, mat.Column1, mat.Column2);
        }

        /// <summary>
        /// Calculate the transpose of the given matrix.
        /// </summary>
        /// <param name="mat">The matrix to transpose.</param>
        /// <param name="result">The result of the calculation.</param>
        public static void Transpose(ref TkMatrix3 mat, ref TkMatrix3 result)
        {
            result.Row0.X = mat.Row0.X;
            result.Row0.Y = mat.Row1.X;
            result.Row0.Z = mat.Row2.X;
            result.Row1.X = mat.Row0.Y;
            result.Row1.Y = mat.Row1.Y;
            result.Row1.Z = mat.Row2.Y;
            result.Row2.X = mat.Row0.Z;
            result.Row2.Y = mat.Row1.Z;
            result.Row2.Z = mat.Row2.Z;
        }

        /// <summary>
        /// Matrix multiplication.
        /// </summary>
        /// <param name="left">left-hand operand.</param>
        /// <param name="right">right-hand operand.</param>
        /// <returns>A new Matrix3d which holds the result of the multiplication.</returns>
        public static TkMatrix3 operator *(TkMatrix3 left, TkMatrix3 right)
        {
            return Mult(left, right);
        }

        /// <summary>
        /// Compares two instances for equality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left equals right; false otherwise.</returns>
        public static bool operator ==(TkMatrix3 left, TkMatrix3 right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Compares two instances for inequality.
        /// </summary>
        /// <param name="left">The first instance.</param>
        /// <param name="right">The second instance.</param>
        /// <returns>True, if left does not equal right; false otherwise.</returns>
        public static bool operator !=(TkMatrix3 left, TkMatrix3 right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Returns a System.String that represents the current Matrix3d.
        /// </summary>
        /// <returns>The string representation of the matrix.</returns>
        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <inheritdoc cref="ToString(string, IFormatProvider)"/>
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        /// <inheritdoc cref="ToString(string, IFormatProvider)"/>
        public string ToString(IFormatProvider formatProvider)
        {
            return ToString(null, formatProvider);
        }

        /// <inheritdoc/>
        public string ToString(string format, IFormatProvider formatProvider) {
	        return String.Format(
		        "|{00}, {01}, {02}|\n" +
		        "|{03}, {04}, {05}|\n" +
		        "|{06}, {07}, {08}|\n",
		        M11, M12, M12,
		        M21, M22, M22,
		        M31, M32, M32);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A System.Int32 containing the unique hashcode for this instance.</returns>
        public override int GetHashCode() {
			return
				M11.GetHashCode() ^ M12.GetHashCode() ^ M13.GetHashCode() ^
				M21.GetHashCode() ^ M22.GetHashCode() ^ M23.GetHashCode() ^
				M31.GetHashCode() ^ M32.GetHashCode() ^ M33.GetHashCode();
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if the instances are equal; false otherwise.</returns>
        public override bool Equals(object obj)
        {
            return obj is TkMatrix3 && Equals((TkMatrix3)obj);
        }

        /// <summary>
        /// Indicates whether the current matrix is equal to another matrix.
        /// </summary>
        /// <param name="other">A matrix to compare with this matrix.</param>
        /// <returns>true if the current matrix is equal to the matrix parameter; otherwise, false.</returns>
        public bool Equals(TkMatrix3 other)
        {
            return
                Row0 == other.Row0 &&
                Row1 == other.Row1 &&
                Row2 == other.Row2;
        }
    }
}