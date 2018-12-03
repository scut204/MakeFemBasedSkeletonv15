using System;

namespace MyGeometry
{
	public class Trackball
	{
		public enum MotionType { None, Rotation, Pan, Scale }

		private MotionType type = MotionType.None;
		private Vector2d stPt, edPt;
		private Vector3d stVec;
		private Vector3d edVec;
		private Vector4d quat;
		private double w, h;
		private double adjustWidth;
		private double adjustHeight;

		public Trackball(double w, double h)
		{
			SetBounds(w,h);
		}

		public void SetBounds(double w, double h)
		{
			double b = (w<h)?w:h;
			this.w = w / 2.0;
			this.h = h / 2.0;
			this.adjustWidth = 1.0 / ((b - 1.0) * 0.5);
			this.adjustHeight = 1.0 / ((b - 1.0) * 0.5);
		}

		public void Click(Vector2d pt, MotionType type)
		{
			this.stPt = pt;
			this.stVec = MapToSphere(pt);
			this.type = type;
		}

		public void Drag(Vector2d pt)
		{
			edPt = pt;
			edVec = MapToSphere(pt);

			double epsilon = 1.0e-5;
			Vector3d prep = stVec.Cross(edVec);

			if (prep.Length() > epsilon)
				quat = new Vector4d(prep, stVec.Dot(edVec));
			else
				quat = new Vector4d();
		}
		public void End()
		{
			quat = new Vector4d();
			type = MotionType.None;
		}
		public Matrix4d GetMatrix()
		{
			if (type == MotionType.Rotation)
				return QuatToMatrix4d(quat);

			if (type == MotionType.Scale)
			{
				Matrix4d m = Matrix4d.IdentityMatrix();
				m[0,0] = m[1,1] = m[2,2] = 1.0 + (edPt.x - stPt.x) * adjustWidth;
				return m;
			}

			if (type == MotionType.Pan)
			{
				Matrix4d m = Matrix4d.IdentityMatrix();
				m[0,3] = edPt.x - stPt.x;
				m[1,3] = edPt.y - stPt.y;
				return m;
			}

			return Matrix4d.IdentityMatrix();
		}

		public double GetScale()
		{
			if (type == MotionType.Scale)
				return 1.0 + (edPt.x - stPt.x) * adjustWidth;
			else
				return 1.0;
		}


		private Vector3d MapToSphere(Vector2d pt)
		{
			Vector2d v = new Vector2d();
			v.x = (w - pt.x) * adjustWidth;
			v.y = (h - pt.y) * adjustHeight;

			double lenSq = v.Dot(v);
			if (lenSq > 1.0)
			{
				double norm = 1.0 / Math.Sqrt(lenSq);
				return new Vector3d(v.x * norm, v.y * norm, 0);
			}
			else
			{
				return new Vector3d(v.x, v.y, Math.Sqrt(1.0 - lenSq));
			}
		}
		private Matrix3d QuatToMatrix3d(Vector4d q)
		{
			double n = q.Dot(q);
			double s = (n > 0.0) ? (2.0 / n) : 0.0f;

			double xs, ys, zs;
			double wx, wy, wz;
			double xx, xy, xz;
			double yy, yz, zz;
			xs = q.x * s;  ys = q.y * s;  zs = q.z * s;
			wx = q.w * xs; wy = q.w * ys; wz = q.w * zs;
			xx = q.x * xs; xy = q.x * ys; xz = q.x * zs;
			yy = q.y * ys; yz = q.y * zs; zz = q.z * zs;

			Matrix3d m = new Matrix3d();
			m[0,0] = 1.0 - (yy + zz); m[1,0] =        xy - wz;  m[2,0] =        xz + wy;
			m[0,1] =        xy + wz;  m[1,1] = 1.0 - (xx + zz); m[2,1] =        yz - wx;
			m[0,2] =        xz - wy;  m[1,2] =        yz + wx;  m[2,2] = 1.0 - (xx + yy);
			return m;
		}
		private Matrix4d QuatToMatrix4d(Vector4d q)
		{
			double n = q.Dot(q);
			double s = (n > 0.0) ? (2.0 / n) : 0.0f;

			double xs, ys, zs;
			double wx, wy, wz;
			double xx, xy, xz;
			double yy, yz, zz;
			xs = q.x * s; ys = q.y * s; zs = q.z * s;
			wx = q.w * xs; wy = q.w * ys; wz = q.w * zs;
			xx = q.x * xs; xy = q.x * ys; xz = q.x * zs;
			yy = q.y * ys; yz = q.y * zs; zz = q.z * zs;

			Matrix4d m = new Matrix4d();
			m[0, 0] = 1.0 - (yy + zz); m[1, 0] = xy - wz; m[2, 0] = xz + wy;
			m[0, 1] = xy + wz; m[1, 1] = 1.0 - (xx + zz); m[2, 1] = yz - wx;
			m[0, 2] = xz - wy; m[1, 2] = yz + wx; m[2, 2] = 1.0 - (xx + yy);
			m[3, 3] = 1.0;
			return m;
		}
	}
}
