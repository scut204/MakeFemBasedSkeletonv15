using System;

namespace MyGeometry
{
	public struct Vector2d
	{
		public static Vector2d MinValue = new Vector2d(double.MinValue, double.MinValue);
		public static Vector2d MaxValue = new Vector2d(double.MaxValue, double.MaxValue);

		public double x, y;

		public Vector2d(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public Vector2d(double[] arr, int index)
		{
			x = arr[index];
			y = arr[index+1];
		}


		public double this[int index] 
		{
			get 
			{
				if (index==0) return x;
				if (index==1) return y;
				throw new ArgumentException();
			}
			set 
			{
				if (index==0) x = value;
				if (index==1) y = value;
			}
		}
		public double Dot (Vector2d v)
		{
			return x*v.x + y*v.y;
		}
		public double Length()
		{
			return Math.Sqrt(x*x + y*y);
		}
		public Vector2d Normalize()
		{
			return this / this.Length();
		}
		public override string ToString()
		{
			return x.ToString() + " " + y.ToString();
		}
		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			Vector2d v = (Vector2d)obj;
			return (x == v.x) && (y == v.y);
		}
		public override int GetHashCode()
		{
			return (x+y).GetHashCode(); 
		}

		public static Vector2d Max(Vector2d v1, Vector2d v2) 
		{
			return new Vector2d( 
				(v1.x > v2.x) ? v1.x : v2.x,
				(v1.y > v2.y) ? v1.y : v2.y
				);
		}
		public static Vector2d Min(Vector2d v1, Vector2d v2) 
		{
			return new Vector2d(
				(v1.x < v2.x) ? v1.x : v2.x,
				(v1.y < v2.y) ? v1.y : v2.y
				);
		}

		static public Vector2d operator+ (Vector2d v1, Vector2d v2)
		{
			return new Vector2d(v1.x+v2.x, v1.y+v2.y);
		}
		static public Vector2d operator- (Vector2d v1, Vector2d v2)
		{
			return new Vector2d(v1.x-v2.x, v1.y-v2.y);
		}
		static public Vector2d operator* (Vector2d v, double s)
		{
			return new Vector2d(v.x*s, v.y*s);
		}
		static public Vector2d operator* (double s, Vector2d v)
		{
			return new Vector2d(v.x*s, v.y*s);
		}
		static public Vector2d operator/ (Vector2d v, double s)
		{
			return new Vector2d(v.x/s, v.y/s);
		}
		static public bool operator==(Vector2d v1, Vector2d v2)
		{
			return (v1.x==v2.x) && (v1.y==v2.y);
		}
		static public bool operator !=(Vector2d v1, Vector2d v2)
		{
			return !(v1 == v2);
		}
	}

	public struct Vector3d
	{
		public static Vector3d MinValue = new Vector3d(double.MinValue, double.MinValue, double.MinValue);
		public static Vector3d MaxValue = new Vector3d(double.MaxValue, double.MaxValue, double.MaxValue);

		public double x, y, z;


        public Vector3d(double[] pos)
        {
            this.x = pos[0];
            this.y = pos[1];
            this.z = pos[2];
        }
		public Vector3d(double x, double y, double z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
		public Vector3d(Vector2d v)
		{
			this.x = v.x;
			this.y = v.y;
			this.z = 0;
		}
		public Vector3d(Vector2d v, double z)
		{
			this.x = v.x;
			this.y = v.y;
			this.z = z;
		}

		public Vector3d(double[] arr, int index)
		{
			x = arr[index];
			y = arr[index+1];
			z = arr[index+2];
		}
        public static bool operator ==(Vector3d v1,Vector3d v2)
        {
            return v1.x != v2.x ? false :
                   v1.y != v2.y ? false :
                   v1.z != v2.z ? false : true;
        }
        public static bool operator !=(Vector3d v1, Vector3d v2)
        {
            return v1.x != v2.x ? true :
                   v1.y != v2.y ? true :
                   v1.z != v2.z ? true : false;
        }

        public double this[int index] 
		{
			get 
			{
				if (index==0) return x;
				if (index==1) return y;
				if (index==2) return z;
				throw new ArgumentException();
			}
			set 
			{
				if (index==0) x = value;
				if (index==1) y = value;
				if (index==2) z = value;
			}
		}
		public double Dot (Vector3d v)
		{
			return x*v.x + y*v.y + z*v.z;
		}
		public double Length()
		{
			return Math.Sqrt(x*x + y*y + z*z);
		}

		public Vector3d Cross(Vector3d v)
		{
			return new Vector3d(
				y * v.z - v.y * z,
				z * v.x - v.z * x,
				x * v.y - v.x * y
				);
		}
		public Vector3d Normalize()
		{
			return this / this.Length();
		}
        public static Vector3d RotationRodriguesMethod(Vector3d spinAxis, Vector3d v,double cost)
        {
            //double cost = Math.Cos(angle);
            double sint = Math.Pow(1-cost * cost,0.5);
            return v * cost + spinAxis.Cross(v) * sint + spinAxis * spinAxis.Dot(v) * (1 - cost);
            
        }

        public Matrix3d OuterCross(Vector3d v)
		{
			Matrix3d m = new Matrix3d();
			m[0, 0] = x * v.x;
			m[0, 1] = x * v.y;
			m[0, 2] = x * v.z;
			m[1, 0] = y * v.x;
			m[1, 1] = y * v.y;
			m[1, 2] = y * v.z;
			m[2, 0] = z * v.x;
			m[2, 1] = z * v.y;
			m[2, 2] = z * v.z;
			return m;
		}

		public override string ToString()
		{
			return x.ToString() + " " + y.ToString() + " " + z.ToString();
		}


		public static Vector3d Max(Vector3d v1, Vector3d v2) 
		{
			return new Vector3d( (v1.x > v2.x) ? v1.x : v2.x,
				(v1.y > v2.y) ? v1.y : v2.y,
				(v1.z > v2.z) ? v1.z : v2.z );
		}
		public static Vector3d Min(Vector3d v1, Vector3d v2) 
		{
			return new Vector3d( (v1.x < v2.x) ? v1.x : v2.x,
				(v1.y < v2.y) ? v1.y : v2.y,
				(v1.z < v2.z) ? v1.z : v2.z );
		}

		static public Vector3d operator+ (Vector3d v1, Vector3d v2)
		{
			return new Vector3d(v1.x+v2.x, v1.y+v2.y, v1.z+v2.z);
		}
        static public Vector3d operator- (Vector3d v1)
        {
            return new Vector3d(-v1.x, -v1.y, -v1.z);
        }
		static public Vector3d operator- (Vector3d v1, Vector3d v2)
		{
			return new Vector3d(v1.x-v2.x, v1.y-v2.y, v1.z-v2.z);
		}
		static public Vector3d operator* (Vector3d v, double s)
		{
			return new Vector3d(v.x*s, v.y*s, v.z*s);
		}
		static public Vector3d operator* (double s, Vector3d v)
		{
			return new Vector3d(v.x*s, v.y*s, v.z*s);
		}
		static public Vector3d operator/ (Vector3d v, double s)
		{
			return new Vector3d(v.x/s, v.y/s, v.z/s);
		}
	}

	public struct Vector4d
	{
		public double x, y, z, w;

		public Vector4d(double x, double y, double z, double w)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
		public Vector4d(Vector2d v)
		{
			this.x = v.x;
			this.y = v.y;
			this.z = 0;
			this.w = 0;
		}
		public Vector4d(Vector2d v, double z, double w)
		{
			this.x = v.x;
			this.y = v.y;
			this.z = z;
			this.w = w;
		}
		public Vector4d(Vector3d v)
		{
			this.x = v.x;
			this.y = v.y;
			this.z = v.z;
			this.w = 0;
		}
		public Vector4d(Vector3d v, double w)
		{
			this.x = v.x;
			this.y = v.y;
			this.z = v.z;
			this.w = w;
		}

		public Vector4d(double[] arr, int index)
		{
			x = arr[index];
			y = arr[index+1];
			z = arr[index+2];
			w = arr[index+3];
		}


		public double Dot (Vector4d v)
		{
			return x*v.x + y*v.y + z*v.z + w*v.w;
		}
		public double Length()
		{
			return Math.Sqrt(x*x + y*y + z*z + w*w);
		}

		public Vector4d Normalize()
		{
			return this / this.Length();
		}
		public Matrix4d OuterCross(Vector4d v)
		{
			Matrix4d m = new Matrix4d();
			m[0, 0] = x * v.x;
			m[0, 1] = x * v.y;
			m[0, 2] = x * v.z;
			m[0, 3] = x * v.w;
			m[1, 0] = y * v.x;
			m[1, 1] = y * v.y;
			m[1, 2] = y * v.z;
			m[1, 3] = y * v.w;
			m[2, 0] = z * v.x;
			m[2, 1] = z * v.y;
			m[2, 2] = z * v.z;
			m[2, 3] = z * v.w;
			m[3, 0] = w * v.x;
			m[3, 1] = w * v.y;
			m[3, 2] = w * v.z;
			m[3, 3] = w * v.w;
			return m;
		}

		public override string ToString()
		{
			return x.ToString() + " " + y.ToString() + " " + z.ToString() + " " + w.ToString();
		}


		public static Vector4d Max(Vector4d v1, Vector4d v2) 
		{
			return new Vector4d( 
				(v1.x > v2.x) ? v1.x : v2.x,
				(v1.y > v2.y) ? v1.y : v2.y,
				(v1.z > v2.z) ? v1.z : v2.z, 
				(v1.w > v2.w) ? v1.w : v2.w 
				);
		}
		public static Vector4d Min(Vector4d v1, Vector4d v2) 
		{
			return new Vector4d(
				(v1.x < v2.x) ? v1.x : v2.x,
				(v1.y < v2.y) ? v1.y : v2.y,
				(v1.z < v2.z) ? v1.z : v2.z,
				(v1.w < v2.w) ? v1.w : v2.w 
				);
		}

		static public Vector4d operator+ (Vector4d v1, Vector4d v2)
		{
			return new Vector4d(v1.x+v2.x, v1.y+v2.y, v1.z+v2.z, v1.w+v2.w);
		}
		static public Vector4d operator- (Vector4d v1, Vector4d v2)
		{
			return new Vector4d(v1.x-v2.x, v1.y-v2.y, v1.z-v2.z, v1.w-v2.w);
		}
		static public Vector4d operator* (Vector4d v, double s)
		{
			return new Vector4d(v.x*s, v.y*s, v.z*s, v.w * s);
		}
		static public Vector4d operator* (double s, Vector4d v)
		{
			return new Vector4d(v.x*s, v.y*s, v.z*s, v.w*s);
		}
		static public Vector4d operator/ (Vector4d v, double s)
		{
			return new Vector4d(v.x/s, v.y/s, v.z/s, v.w/s);
		}
	}
}