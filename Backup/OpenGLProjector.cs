using System;
using CsGL.OpenGL;
using MyGeometry;

namespace IsolineEditing
{
	public class OpenGLProjector 
	{
		#region Private Instance Fields
		private double[] modelView = new double[16];
		private double[] projection = new double[16];
		private int[] viewport = new int[4];
		private float[] depthBuffer = null;
		#endregion

		#region Public Properties
		public double[] ModelViewMatrix
		{
			get { return modelView; }
		}
		public double[] ProjectionMatrix 
		{
			get { return projection; }
		}
		public int[] Viewport 
		{
			get { return viewport; }
		}
		public float[] DepthBuffer 
		{
			get { return depthBuffer; }
		}

		#endregion

		public unsafe OpenGLProjector() 
		{
			GL.glGetDoublev(GL.GL_MODELVIEW_MATRIX, modelView);
			GL.glGetDoublev(GL.GL_PROJECTION_MATRIX, projection);
			GL.glGetIntegerv(GL.GL_VIEWPORT, viewport);
			
			depthBuffer = new float[viewport[2] * viewport[3]];
			fixed (float* pt = depthBuffer) 
			{
				GL.glReadPixels(viewport[0], viewport[1], viewport[2], viewport[3], 
					GL.GL_DEPTH_COMPONENT, GL.GL_FLOAT, pt);
			}
		}

		public Vector3d UnProject(double inX, double inY, double inZ) 
		{
			double x,y,z;
			GL.gluUnProject(inX, inY, inZ, modelView, projection, viewport, out x, out y, out z);
			return new Vector3d(x,y,z);
		}
		public Vector3d UnProject(Vector3d p) 
		{
			double x,y,z;
			GL.gluUnProject(p.x, p.y, p.z, modelView, projection, viewport, out x, out y, out z);
			return new Vector3d(x,y,z);
		}
		public Vector3d UnProject(double[] arr, int index) 
		{
			double x,y,z;
			GL.gluUnProject(arr[index], arr[index+1], arr[index+2], modelView, projection, viewport, out x, out y, out z);
			return new Vector3d(x,y,z);
		}
		public Vector3d Project(double inX, double inY, double inZ) 
		{
			double x,y,z;
			GL.gluProject(inX, inY, inZ, modelView, projection, viewport, out x, out y, out z);
			return new Vector3d(x,y,z);
		}
		public Vector3d Project(Vector3d p) 
		{
			double x,y,z;
			GL.gluProject(p.x, p.y, p.z, modelView, projection, viewport, out x, out y, out z);
			return new Vector3d(x,y,z);
		}
		public Vector3d Project(double[] arr, int index) 
		{
			double x,y,z;
			GL.gluProject(arr[index], arr[index+1], arr[index+2], modelView, projection, viewport, out x, out y, out z);
			return new Vector3d(x,y,z);
		}
		public double GetDepthValue(int x, int y) 
		{
			return depthBuffer[(y-viewport[1])*viewport[2] + (x-viewport[0])];
		}
	}
}
