using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using NumericalRecipes;

namespace MyGeometry
{
	public struct Matrix2d
	{
		private const int len = 4;
		private const int row_size = 2;
		double a, b, c, d;

		public Matrix2d(double [] arr) 
		{
			a = arr[0];
			b = arr[1];
			c = arr[2];
			d = arr[3];
		}
		public Matrix2d(double [,] arr) 
		{
			a = arr[0,0];
			b = arr[0,1];
			c = arr[1,0];
			d = arr[1,1];
		}

		// using column vectors
		public Matrix2d(Vector2d v1, Vector2d v2) 
		{
			a = v1.x;
			b = v2.x;
			c = v1.y;
			d = v2.y;
		}
 
		public Matrix2d(double a, double b, double c, double d) 
		{
			this.a = a;
			this.b = b;
			this.c = c;
			this.d = d;
		}
 

		public static Matrix2d operator * (Matrix2d m1, Matrix2d m2) 
		{
			Matrix2d ret = new Matrix2d(
				m1.a * m2.a + m1.b * m2.c,
				m1.a * m2.b + m1.b * m2.d,
				m1.c * m2.a + m1.d * m2.c,
				m1.c * m2.b + m1.d * m2.d
				);
			return ret;
		}
		public static Vector2d operator * (Matrix2d m, Vector2d v) 
		{
			return new Vector2d(m.A*v.x+m.B*v.y, m.C*v.x+m.D*v.y);
		}
		public Matrix2d Inverse()
		{
			double det = (a*d - b*c);
			if (double.IsNaN(det)) throw new ArithmeticException();
			return new Matrix2d(d/det, -b/det, -c/det, a/det);
		}
		public Matrix2d Transpose() 
		{
			return new Matrix2d(a, c, b, d);
		}
		public double Trace()
		{
			return a + d;
		}

		public double A
		{
			get { return a; }
			set { a = value; }
		}
		public double B
		{
			get { return b; }
			set { b = value; }
		}
		public double C
		{
			get { return c; }
			set { c = value; }
		}
		public double D
		{
			get { return d; }
			set { d = value; }
		}
		public override string ToString()
		{
			return 
				a.ToString("F5") + " " + b.ToString("F5") + " " +
				c.ToString("F5") + " " + d.ToString("F5");
		}
	}	
	
	public class Matrix3d
	{
		public static bool lastSVDIsFullRank = false;
		private const int len = 9;
		private const int row_size = 3;
        private double[] e = new double[len];

        public Matrix3d() { }
		public Matrix3d(double [] arr) 
		{
			for (int i=0; i<len; i++) e[i] = arr[i];
		}
		public Matrix3d(double [,] arr) 
		{
            for (int i=0; i<row_size; i++)
				for (int j=0; j<row_size; j++)
					this[i,j] = arr[i,j];
		}
		public Matrix3d(Matrix3d m) : this(m.e) { }
		public Matrix3d(Vector3d v1, Vector3d v2, Vector3d v3)
		{
			for (int i=0; i<row_size; i++) 
			{
				this[i,0] = v1[i];
				this[i,1] = v2[i];
				this[i,2] = v3[i];
			}
		}

        public void Clear()
        {
            for (int i = 0; i < len; i++) e[i] = 0;
        }
        public double this[int index]
		{
			get { return e[index]; }
			set { e[index] = value; }
		}
		public double this[int row, int column] 
		{
			get { return e[row*row_size + column]; }
			set { e[row*row_size + column] = value; }
		}
		public double [] ToArray()
		{
			return (double[])e.Clone();
		}
		public double Trace()
		{
			return e[0] + e[4] + e[8];
		}
		public double SqNorm()
		{
			double sq = 0;
			for (int i=0; i<len; i++) sq += e[i] * e[i];
			return sq;
		}
		public Matrix3d Transpose()
		{
			Matrix3d m = new Matrix3d();
			for (int i = 0; i < row_size; i++)
				for (int j = 0; j < row_size; j++)
					m[j, i] = this[i, j];
			return m;
		}
		public Matrix3d Inverse()
		{
			double a = e[0];
			double b = e[1];
			double c = e[2];
			double d = e[3];
			double E = e[4];
			double f = e[5];
			double g = e[6];
			double h = e[7];
			double i = e[8];
			double det = a * (E * i - f * h) - b * (d * i - f * g) + c * (d * h - E * g);
			if (det == 0) throw new ArithmeticException();

			Matrix3d inv = new Matrix3d();
			inv[0] = (E * i - f * h) / det;
			inv[1] = (c * h - b * i) / det;
			inv[2] = (b * f - c * E) / det;
			inv[3] = (f * g - d * i) / det;
			inv[4] = (a * i - c * g) / det;
			inv[5] = (c * d - a * f) / det;
			inv[6] = (d * h - E * g) / det;
			inv[7] = (b * g - a * h) / det;
			inv[8] = (a * E - b * d) / det;
			return inv;
		}
		public Matrix3d InverseSVD()
		{
			SVD svd = new SVD(e, 3, 3);
			Matrix3d inv = new Matrix3d(svd.Inverse);
			lastSVDIsFullRank = svd.FullRank;
			return inv;
		}
		public Matrix3d InverseTranspose()
		{
			double a = e[0];
			double b = e[1];
			double c = e[2];
			double d = e[3];
			double E = e[4];
			double f = e[5];
			double g = e[6];
			double h = e[7];
			double i = e[8];
			double det = a * (E * i - f * h) - b * (d * i - f * g) + c * (d * h - E * g);
			if (det == 0) throw new ArithmeticException();

			Matrix3d inv = new Matrix3d();
			inv[0] = (E * i - f * h) / det;
			inv[3] = (c * h - b * i) / det;
			inv[6] = (b * f - c * E) / det;
			inv[1] = (f * g - d * i) / det;
			inv[4] = (a * i - c * g) / det;
			inv[7] = (c * d - a * f) / det;
			inv[2] = (d * h - E * g) / det;
			inv[5] = (b * g - a * h) / det;
			inv[8] = (a * E - b * d) / det;
			return inv;
		}
		public Matrix3d OrthogonalFactor(double eps)
		{
			Matrix3d Q = new Matrix3d(this);
			Matrix3d Q2 = new Matrix3d();
			double err = 0;
			do
			{
				Q2 = (Q + Q.InverseTranspose()) / 2.0;
				err = (Q2 - Q).SqNorm();
				Q = Q2;
			} while (err > eps);

			return Q2;
		}
		public Matrix3d OrthogonalFactorSVD()
		{
			SVD svd = new SVD(e, 3, 3);
			lastSVDIsFullRank = svd.FullRank;
			return new Matrix3d(svd.OrthogonalFactor);
		}
		public Matrix3d OrthogonalFactorIter()
		{
			return (this + this.InverseTranspose())/2;
		}
		public static Matrix3d IdentityMatrix()
		{
			Matrix3d m = new Matrix3d();
			m[0] = m[4] = m[8] =  1.0;
			return m;
		}
		public static Vector3d operator * (Matrix3d m, Vector3d v) 
		{
			Vector3d ret = new Vector3d();
			ret.x = m[0]*v.x + m[1]*v.y + m[2]*v.z;
			ret.y = m[3]*v.x + m[4]*v.y + m[5]*v.z;
			ret.z = m[6]*v.x + m[7]*v.y + m[8]*v.z;
			return ret;
		}
		public static Matrix3d operator * (Matrix3d m1, Matrix3d m2)
		{
			Matrix3d ret = new Matrix3d();
			for (int i = 0; i < row_size; i++)
				for (int j = 0; j < row_size; j++)
				{
					ret[i, j] = 0.0;
					for (int k = 0; k < row_size; k++)
						ret[i, j] += m1[i, k] * m2[k, j];
				}
			return ret;
		}
		public static Matrix3d operator + (Matrix3d m1, Matrix3d m2)
		{
			Matrix3d ret = new Matrix3d();
			for (int i=0; i<len; i++) ret[i] = m1[i] + m2[i];
			return ret;
		}
		public static Matrix3d operator - (Matrix3d m1, Matrix3d m2)
		{
			Matrix3d ret = new Matrix3d();
			for (int i = 0; i < len; i++) ret[i] = m1[i] - m2[i];
			return ret;
		}
		public static Matrix3d operator * (Matrix3d m, double d)
		{
			Matrix3d ret = new Matrix3d();
			for (int i = 0; i < len; i++) ret[i] = m[i] * d;
			return ret;
		}
		public static Matrix3d operator / (Matrix3d m, double d)
		{
			Matrix3d ret = new Matrix3d();
			for (int i=0; i<len; i++) ret[i] = m[i] / d;
			return ret;
		}
		public override string ToString()
		{
			return
				e[0].ToString("F5") + " " + e[1].ToString("F5") + " " + e[2].ToString("F5") +
				e[3].ToString("F5") + " " + e[4].ToString("F5") + " " + e[5].ToString("F5") +
				e[6].ToString("F5") + " " + e[7].ToString("F5") + " " + e[08].ToString("F5");
		}
	}

	[XmlRootAttribute(IsNullable = false)]
	public class Matrix4d
	{
		private const int len = 16;
		private const int row_size = 4;
		private double [] e = new double[len];

		public Matrix4d()
		{
		}
		public Matrix4d(double [] arr) 
		{
			for (int i=0; i<len; i++) e[i] = arr[i];
		}
		public Matrix4d(double [,] arr) 
		{
			for (int i=0; i<row_size; i++)
				for (int j=0; j<row_size; j++)
					this[i,j] = arr[i,j];
		}
		public Matrix4d(Matrix4d m) : this(m.e) { }
		public Matrix4d(Matrix3d m)
		{
			for (int i=0; i<3; i++)
				for (int j=0; j<3; j++)
					this[i,j] = m[i,j];
		}
		public Matrix4d(StreamReader sr)
		{
			int c = 0;
			char[] delimiters = { ' ', '\t' };

			while (sr.Peek() > -1)
			{
				string s = sr.ReadLine();
				string[] tokens = s.Split(delimiters);
				for (int i = 0; i < tokens.Length; i++)
				{
					e[c++] = Double.Parse(tokens[i]);
					if (c >= len) return;
				}
			}
		}


		public double this[int index]
		{
			get { return e[index]; }
			set { e[index] = value; }
		}
		public double this[int row, int column] 
		{
			get { return e[row*row_size + column]; }
			set { e[row*row_size + column] = value; }
		}
		public double [] Element
		{
			get { return e; }
			set 
			{ 
				if (value.Length < len) 
					throw new Exception();
				e = value; 
			}
		}
		public double [] ToArray()
		{
			return (double[])e.Clone();
		}
		public Matrix3d ToMatrix3d()
		{
			Matrix3d ret = new Matrix3d();
			ret[0] = e[0];
			ret[1] = e[1];
			ret[2] = e[2];
			ret[3] = e[4];
			ret[4] = e[5];
			ret[5] = e[6];
			ret[6] = e[8];
			ret[7] = e[9];
			ret[8] = e[10];
			return ret;
		}
		public double Trace()
		{
			return e[0] + e[5] + e[10] + e[15];
		}
		public Matrix4d Inverse() 
		{
			SVD svd = new SVD(e, row_size, row_size);
			if (svd.State == false) throw new ArithmeticException();
			return new Matrix4d(svd.Inverse);
		}
		public Matrix4d Transpose() 
		{
			Matrix4d m = new Matrix4d();
			for (int i=0; i<row_size; i++)
				for (int j=0; j<row_size; j++)
					m[j,i] = this[i,j];
			return m;
		}
		public static Matrix4d IdentityMatrix()
		{
			Matrix4d m = new Matrix4d();
			m[0] = m[5] = m[10] = m[15] = 1.0;
			return m;
		}
		public static Vector4d operator * (Vector4d v, Matrix4d m) 
		{
			return m.Transpose() * v;
		}
		public static Matrix4d operator * (Matrix4d m1, Matrix4d m2) 
		{
			Matrix4d ret = new Matrix4d();
			for (int i=0; i<row_size; i++)
				for (int j=0; j<row_size; j++) 
				{
					ret [i,j] = 0.0;
					for (int k=0; k<row_size; k++)
						ret[i,j] += m1[i,k] * m2[k,j];
				}
			return ret;
		}
		public static Vector4d operator * (Matrix4d m, Vector4d v) 
		{
			Vector4d ret = new Vector4d();
			ret.x = m[0]*v.x  + m[1]*v.y  + m[2]*v.z  + m[3]*v.w;
			ret.y = m[4]*v.x  + m[5]*v.y  + m[6]*v.z  + m[7]*v.w;
			ret.z = m[8]*v.x  + m[9]*v.y  + m[10]*v.z + m[11]*v.w;
			ret.w = m[12]*v.x + m[13]*v.y + m[14]*v.z + m[15]*v.w;
			return ret;
		}
		public static Matrix4d operator +(Matrix4d m1, Matrix4d m2)
		{
			Matrix4d ret = new Matrix4d();
			for (int i = 0; i < len; i++) ret[i] = m1[i] + m2[i];
			return ret;
		}
		public static Matrix4d operator -(Matrix4d m1, Matrix4d m2)
		{
			Matrix4d ret = new Matrix4d();
			for (int i = 0; i < len; i++) ret[i] = m1[i] - m2[i];
			return ret;
		}
		public static Matrix4d operator *(Matrix4d m, double d)
		{
			Matrix4d ret = new Matrix4d();
			for (int i = 0; i < len; i++) ret[i] = m[i] * d;
			return ret;
		}

		public override string ToString()
		{
			string s = "";
			foreach (double d in e)
				s += d.ToString() + "\n";
			return s;
		}
	}

	public class MatrixNd
	{
		private int m;
		private int n;
		private double[] e;

		public MatrixNd(int m, int n)
		{
			this.m = m;
			this.n = n;
			e = new double[m * n];
			for (int i = 0; i < m * n; i++)
				e[i] = 0;
		}
		public MatrixNd(SparseMatrix right) 
			: this(right.RowSize, right.ColumnSize)
		{
			int b = 0;
			foreach (List<SparseMatrix.Element> row in right.Rows)
			{
				foreach (SparseMatrix.Element element in row)
					e[b + element.j] = element.value;
				b += n;
			}
		}
		public MatrixNd(SparseMatrix right, bool transpose)
			: this(right.ColumnSize, right.RowSize)
		{
			int b = 0;
			foreach (List<SparseMatrix.Element> col in right.Columns)
			{
				foreach (SparseMatrix.Element element in col)
					e[b + element.i] = element.value;
				b += n;
			}
		}

		public double this[int row, int column]
		{
			get { return e[row * n + column]; }
			set { e[row * n + column] = value;  }
		}
		public int RowSize
		{
			get { return m; }
		}
		public int ColumnSize
		{
			get { return n; }
		}

		public void Multiply(double[] xIn, double[] xOut)
		{
			if (xIn.Length<n || xOut.Length<m) throw new ArgumentException();

			for (int i=0,b=0; i<m; i++,b+=n)
			{
				double sum = 0;
				for (int j = 0; j < n; j++)
					sum += e[b + j] * xIn[j];
				xOut[i] = sum;
			}
		}
	}
}
