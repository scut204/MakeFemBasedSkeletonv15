using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using CsGL.OpenGL;
using MyGeometry;

namespace IsolineEditing
{
	public unsafe partial class MeshView : OpenGLControl
	{
		[DllImport("opengl32", EntryPoint = "wglUseFontBitmaps", CallingConvention=CallingConvention.Winapi)]
		public static extern bool wglUseFontBitmaps(
		IntPtr hDC,
		[MarshalAs(UnmanagedType.U4)] UInt32 first,
		[MarshalAs(UnmanagedType.U4)] UInt32 count,
		[MarshalAs(UnmanagedType.U4)] UInt32 listBase
		);

		[DllImport("GDI32.DLL", EntryPoint = "SelectObject",
		CallingConvention = CallingConvention.Winapi)]
		public static extern IntPtr SelectObject(
		[In] IntPtr hDC,
		[In] IntPtr font
		);

		private class FaceDepth : IComparable
		{
			public int index;
			public double depth;
			public FaceDepth(int index, double depth)
			{
				this.index = index;
				this.depth = depth;
			}

			public int CompareTo(object obj)
			{
				FaceDepth f = obj as FaceDepth;
				if (depth < f.depth) return -1;
				if (depth > f.depth) return 1;
				return 0;
			}
		};

		private MeshRecord currMeshRecord = null;
		private Matrix4d currTransformation = Matrix4d.IdentityMatrix();
		private Vector2d currMousePosition = new Vector2d();

		// for viewing
		private Trackball ball;
		private double scaleRatio;
		private int[] faceDepth = null;

		// for selection
		private Vector2d mouseDownPosition = new Vector2d();
		private bool isMouseDown = false;

		// for moving
		private Trackball movingBall = new Trackball(200, 200);
		private Vector3d projectedCenter = new Vector3d();
		private Vector4d handleCenter = new Vector4d();
		private List<int> handleIndex = new List<int>();
		private List<Vector3d> oldHandlePos = new List<Vector3d>();
		private int handleFlag = -1;

		// testing
		private bool initFont = false;
		public UInt32 fontBase = 0;

		public MeshView()
		{
			InitializeComponent();

			ball = new Trackball(this.Width * 1.0, this.Height * 1.0);
		}
		~MeshView()
		{
			ReleaseFont();
		}
		public Matrix4d CurrTransformation
		{
			get { return currTransformation; }
			set { currTransformation = value; }
		}
		
		private void BuildFont(PaintEventArgs pe)
		{
			IntPtr dc = pe.Graphics.GetHdc();
			IntPtr oldFontH = IntPtr.Zero;
			System.Drawing.Font font =
				new Font(
				"Verdana",
				12F,
				System.Drawing.FontStyle.Regular,
				System.Drawing.GraphicsUnit.Point,
				((System.Byte)(0)));

			fontBase = GL.glGenLists(128);

			IntPtr fontH = font.ToHfont();
			oldFontH = SelectObject(dc, fontH);

			bool ret = wglUseFontBitmaps(
				dc,
				0,
				128,
				fontBase);

			SelectObject(dc, oldFontH);						// Selects The Font We Want

			pe.Graphics.ReleaseHdc(dc);

			if (!ret) throw new Exception();
		}
		private void ReleaseFont()
		{
			GL.glDeleteLists(fontBase, 255);
		}

		protected override void InitGLContext()
		{
			base.InitGLContext();
			Color c = SystemColors.Control;
			//Color c = Color.Black;
			GL.glClearColor(c.R / 255f, c.G / 255f, c.B / 255f, 0.0f);     // BackGround Color         
			//GL.glClearColor(1f, 1f, 1f, 0.0f);     // BackGround Color         
			GL.glShadeModel(GL.GL_SMOOTH);         // Set Smooth Shading                 
			GL.glClearDepth(1.0f);                 // Depth buffer setup             
			GL.glEnable(GL.GL_DEPTH_TEST);         // Enables Depth Testing             
			GL.glDepthFunc(GL.GL_LEQUAL);             // The Type Of Depth Test To Do     
			GL.glHint(GL.GL_PERSPECTIVE_CORRECTION_HINT, GL.GL_NICEST);     /* Really Nice Perspective Calculations */
			GL.glEnable(GL.GL_CULL_FACE);
			GL.glPolygonOffset(1f, 1f);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);

			//GL.glEnable(GL.GL_POLYGON_SMOOTH);
			GL.glEnable(GL.GL_POINT_SMOOTH);
			GL.glEnable(GL.GL_LINE_SMOOTH);
			GL.glEnable(GL.GL_BLEND);

			float[] LightDiffuse = { 1f, 1f, 1f, 0f };
			float[] LightSpecular = { 0.7f, 0.7f, 0.7f, 1f };
			float[] SpecularRef = { 0.5f, 0.5f, 0.5f, 1f };
			GL.glLightfv(GL.GL_LIGHT0, GL.GL_DIFFUSE, LightDiffuse);
			GL.glLightfv(GL.GL_LIGHT0, GL.GL_SPECULAR, LightSpecular);
			GL.glEnable(GL.GL_LIGHT0);
			GL.glEnable(GL.GL_COLOR_MATERIAL);
			GL.glColorMaterial(GL.GL_FRONT, GL.GL_DIFFUSE);
			GL.glMaterialfv(GL.GL_FRONT, GL.GL_SPECULAR, SpecularRef);
			GL.glMateriali(GL.GL_FRONT, GL.GL_SHININESS, 128);
		}
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			if (this.Width == 0 || this.Height == 0) return;
			this.scaleRatio = (this.Width > this.Height) ? this.Height : this.Width;
			this.InitMatrix();
			this.ball.SetBounds(this.Width * 1.0, this.Height * 1.0);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			this.currMousePosition = new Vector2d(e.X, this.Height - e.Y);
			this.mouseDownPosition = currMousePosition;
			this.isMouseDown = true;

			switch (Program.currentMode)
			{
				case Program.EnumOperationMode.Viewing:
					switch (e.Button)
					{
						case MouseButtons.Left: ball.Click(currMousePosition, Trackball.MotionType.Rotation); break;
						case MouseButtons.Middle: ball.Click(currMousePosition / this.scaleRatio, Trackball.MotionType.Pan); break;
						case MouseButtons.Right: ball.Click(currMousePosition, Trackball.MotionType.Scale); break;
					}
					break;

				case Program.EnumOperationMode.Selection:
					break;

				case Program.EnumOperationMode.Moving:
					if (this.StartMoving() == true)
					{
						Vector2d p = mouseDownPosition - new Vector2d(projectedCenter.x, projectedCenter.y);
						p.x += 100;
						p.y += 100;
						switch (e.Button)
						{
							case MouseButtons.Right: movingBall.Click(p, Trackball.MotionType.Rotation); break;
							case MouseButtons.Left: movingBall.Click(p / this.scaleRatio, Trackball.MotionType.Pan); break;
							case MouseButtons.Middle: movingBall.Click(p, Trackball.MotionType.Scale); break;
						}
						if (currMeshRecord.Deformer != null)
							currMeshRecord.Deformer.MouseDown();
						this.Refresh();
					}
					break;
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			this.currMousePosition = new Vector2d(e.X, this.Height - e.Y);

			switch (Program.currentMode)
			{
				case Program.EnumOperationMode.Viewing:
					switch (e.Button)
					{
						case MouseButtons.Left: ball.Drag(currMousePosition); break;
						case MouseButtons.Middle: ball.Drag(currMousePosition / this.scaleRatio); break;
						case MouseButtons.Right: ball.Drag(currMousePosition); break;
					}
					if (isMouseDown)
						this.Refresh();
					break;

				case Program.EnumOperationMode.Selection:
					if (isMouseDown)
						this.Refresh();
					break;

				case Program.EnumOperationMode.Moving:
					if (isMouseDown)
					{
						Vector2d p = currMousePosition - new Vector2d(projectedCenter.x, projectedCenter.y);
						int d = 0;
						p.x += 100;
						p.y += 100;
						switch (e.Button)
						{
							case MouseButtons.Right: movingBall.Drag(p); break;
							case MouseButtons.Left: movingBall.Drag(p / this.scaleRatio); d = 1; break;
							case MouseButtons.Middle: movingBall.Drag(p); break;
						}
						Matrix4d currInverseTransformation = this.currTransformation.Inverse();
						Matrix4d tran = currInverseTransformation * this.movingBall.GetMatrix() * currTransformation;
						//Matrix4d tran = currTransformation * this.movingBall.GetMatrix() * currInverseTransformation;
						double[] pos = this.currMeshRecord.Mesh.VertexPos;
						for (int i = 0; i < handleIndex.Count; i++)
						{
							int j = handleIndex[i] * 3;
							Vector4d q = new Vector4d((Vector3d)this.oldHandlePos[i], d);
							q = tran * (q - this.handleCenter) + this.handleCenter;
							pos[j] = q.x;
							pos[j + 1] = q.y;
							pos[j + 2] = q.z;
						}
						if (currMeshRecord.Deformer != null)
						{
							currMeshRecord.Deformer.Move();
							currMeshRecord.Deformer.Deform();
							currMeshRecord.Deformer.Update();
						}
						this.Refresh();
					}
					break;
			}
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			this.currMousePosition = new Vector2d(e.X, this.Height - e.Y);
			this.isMouseDown = false;

			switch (Program.currentMode)
			{
				case Program.EnumOperationMode.Viewing:
					if (currMousePosition == mouseDownPosition) break;
					Matrix4d m = ball.GetMatrix();
					this.currTransformation = m * currTransformation;
//					this.currInverseTransformation = this.currTransformation.Inverse();
					this.ball.End();
					this.Refresh();
					break;

				case Program.EnumOperationMode.Selection:
					switch (Program.toolsProperty.SelectionMethod)
					{
						case ToolsProperty.EnumSelectingMethod.Rectangle:
							SelectVertexByRect();
							break;
						case ToolsProperty.EnumSelectingMethod.Point:
							SelectVertexByPoint();
							break;
					}
					currMeshRecord.Mesh.GroupingFlags();
					this.Refresh();
					break;

				case Program.EnumOperationMode.Moving:
					if (currMeshRecord.Deformer != null)
					{
						currMeshRecord.Deformer.MouseUp();
						currMeshRecord.Deformer.Deform();
					}

					this.currMeshRecord.Mesh.ComputeFaceNormal();
					this.currMeshRecord.Mesh.ComputeVertexNormal();
					this.movingBall.End();
					this.handleFlag = -1;
					this.Refresh();
					break;
			}

		}
		protected override void OnPaint(PaintEventArgs pe)
		{
			base.OnPaint(pe);

			if (!initFont)
			{
				BuildFont(pe);
				initFont = true;
			}
		}

		public override void glDraw()
		{
			base.glDraw();
			GL.glClear(GL.GL_COLOR_BUFFER_BIT);
			if (this.DesignMode == true) return;
			if (currMeshRecord == null) return;

			lock ((currMeshRecord.Skeletonizer==null)? new Object(): currMeshRecord.Skeletonizer)
			{


 			Matrix4d m = ball.GetMatrix() * currTransformation;
			GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
 			GL.glMatrixMode(GL.GL_MODELVIEW);
 			GL.glLoadMatrixd(m.Transpose().ToArray());
			

				switch (Program.displayProperty.MeshDisplayMode)
				{
					case DisplayProperty.EnumMeshDisplayMode.Points:
						DrawPoints();
						break;

					case DisplayProperty.EnumMeshDisplayMode.Wireframe:
						DrawWireframe();
						break;

					case DisplayProperty.EnumMeshDisplayMode.FlatShaded:
						DrawFlatShaded();
						break;

					case DisplayProperty.EnumMeshDisplayMode.SmoothShaded:
						DrawSmoothShaded();
						break;

					case DisplayProperty.EnumMeshDisplayMode.FlatShadedHiddenLine:
						DrawFlatHiddenLine();
						break;

					case DisplayProperty.EnumMeshDisplayMode.SmoothShadedHiddenLine:
						DrawSmoothHiddenLine();
						break;

					case DisplayProperty.EnumMeshDisplayMode.TransparentSmoothShaded:
						if (isMouseDown)
							DrawSmoothShaded();
						else
							DrawTransparentSmoothShaded();
						break;
					case DisplayProperty.EnumMeshDisplayMode.TransparentFlatShaded:
						if (isMouseDown)
							DrawFlatShaded();
						else
							DrawTransparentFlatShaded();
						break;
					/*
									case DisplayProperty.EnumMeshDisplayMode.TransparentSmoothShaded2:
										if (isMouseDown)
											DrawSmoothShaded();
										else
											DrawTransparentSmoothShaded2();
										break;
					*/
				}

/**/
				if (currMeshRecord.Deformer != null)
					currMeshRecord.Deformer.Display();

				if (currMeshRecord.Skeletonizer != null)
					currMeshRecord.Skeletonizer.Display();

				switch (Program.currentMode)
				{
					case Program.EnumOperationMode.Selection:
						if (isMouseDown)
							DrawSelectionRect();
						break;
				}

				if (Program.displayProperty.DisplaySelectedVertices)
					DrawSelectedVertice_ByPoint();

			}


		}
		private void InitMatrix()
		{
			double w = Size.Width;
			double h = Size.Height;
			GL.glMatrixMode(GL.GL_PROJECTION);
			GL.glLoadIdentity();

			if (w > h)
			{
				double ratio = (w / h) / 2.0;
				GL.glOrtho(-ratio, ratio, -0.5, 0.5, -100, 100);
			}
			else
			{
				double ratio = (h / w) / 2.0;
				GL.glOrtho(-0.5, 0.5, -ratio, ratio, -100, 100);
			}

			GL.glMatrixMode(GL.GL_MODELVIEW);
			GL.glLoadIdentity();
		}
		private void DrawPoints()
		{
			Mesh m = currMeshRecord.Mesh;

			GL.glPointSize(Program.displayProperty.PointSize);
			Color c = Program.displayProperty.PointColor;
			GL.glColor3ub(c.R, c.G, c.B);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = m.VertexPos)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glDrawArrays(GL.GL_POINTS, 0, m.VertexCount);
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
		}
		private void DrawWireframe()
		{
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
			GL.glDisable(GL.GL_CULL_FACE);
			Color c = Program.displayProperty.LineColor;
			GL.glColor3ub(c.R, c.G, c.B);

			Mesh m = currMeshRecord.Mesh;

			GL.glLineWidth(Program.displayProperty.LineWidth);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = m.VertexPos)
			fixed (int* index = m.FaceIndex)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glDrawElements(GL.GL_TRIANGLES, m.FaceCount * 3, GL.GL_UNSIGNED_INT, index);
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glEnable(GL.GL_CULL_FACE);
		}
		private void DrawFlatShaded()
		{
			GL.glShadeModel(GL.GL_FLAT);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);

			Mesh m = currMeshRecord.Mesh;

			Color c = Program.displayProperty.MeshColor;
			GL.glColor3ub(c.R, c.G, c.B);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = m.VertexPos)
			fixed (double* np = m.FaceNormal)
			{
				GL.glBegin(GL.GL_TRIANGLES);
				for (int i = 0, j = 0; i < m.FaceCount; i++, j += 3)
				{
					GL.glNormal3dv(np + j);
					GL.glVertex3dv(vp + m.FaceIndex[j]* 3);
					GL.glVertex3dv(vp + m.FaceIndex[j+1] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j+2] * 3);
				}
				GL.glEnd();
			}
			GL.glDisable(GL.GL_LIGHTING);
		}
		private void DrawSmoothShaded()
		{
			GL.glShadeModel(GL.GL_SMOOTH);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);

			Mesh m = currMeshRecord.Mesh;

			Color c = Program.displayProperty.MeshColor;
			GL.glColor3ub(c.R, c.G, c.B);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			GL.glEnableClientState(GL.GL_NORMAL_ARRAY);
			fixed (double* vp = m.VertexPos)
			fixed (double* np = m.VertexNormal)
			fixed (int* index = m.FaceIndex)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glNormalPointer(GL.GL_DOUBLE, 0, np);
				GL.glDrawElements(GL.GL_TRIANGLES, m.FaceCount * 3, GL.GL_UNSIGNED_INT, index);
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glDisableClientState(GL.GL_NORMAL_ARRAY);
			GL.glDisable(GL.GL_LIGHTING);
		}
		private void DrawSmoothShaded2()
		{
			Mesh m = currMeshRecord.Mesh;

			GL.glShadeModel(GL.GL_SMOOTH);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			GL.glEnableClientState(GL.GL_NORMAL_ARRAY);
			fixed (double* vp = m.VertexPos)
			fixed (double* np = m.VertexNormal)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glNormalPointer(GL.GL_DOUBLE, 0, np);
				GL.glBegin(GL.GL_TRIANGLES);
				for (int i = 0; i < m.FaceCount*3; i++)
				{
					int j = m.FaceIndex[i];
					if (m.Flag[j] == 0) continue;
					switch (m.Flag[j] % 6)
					{
						case 0: GL.glColor3f(1.0f, 1.0f, 0.0f); break;
						case 1: GL.glColor3f(1.0f, 0.0f, 0.0f); break;
						case 2: GL.glColor3f(0.0f, 0.7f, 0.0f); break;
						case 3: GL.glColor3f(1.0f, 0.0f, 0.0f); break;
						case 4: GL.glColor3f(0.0f, 1.0f, 1.0f); break;
						case 5: GL.glColor3f(1.0f, 0.0f, 1.0f); break;
					}
					GL.glArrayElement(j);
				}
				GL.glEnd();
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glDisableClientState(GL.GL_NORMAL_ARRAY);
			GL.glDisable(GL.GL_LIGHTING);
		}

		private void DrawSmoothHiddenLine()
		{
			GL.glEnable(GL.GL_POLYGON_OFFSET_FILL);
			DrawSmoothShaded();
			//GL.glDisable(GL.GL_POLYGON_OFFSET_FILL);
			DrawWireframe();
		}
		private void DrawFlatHiddenLine()
		{
			GL.glEnable(GL.GL_POLYGON_OFFSET_FILL);
			DrawFlatShaded();
			//GL.glDisable(GL.GL_POLYGON_OFFSET_FILL);
			DrawWireframe();

			//GL.GL_EXT_vertex_shader
		}
		private void DrawSelectionRect()
		{
			GL.glMatrixMode(GL.GL_PROJECTION);
			GL.glPushMatrix();
			GL.glLoadIdentity();
			GL.gluOrtho2D(0, this.Width, 0, this.Height);
			GL.glMatrixMode(GL.GL_MODELVIEW);
			GL.glPushMatrix();
			GL.glLoadIdentity();

			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
			GL.glDisable(GL.GL_CULL_FACE);
			GL.glDisable(GL.GL_DEPTH_TEST);
			GL.glColor3f(0.0f, 0.0f, 0.0f);
			GL.glRectd(mouseDownPosition.x, mouseDownPosition.y, currMousePosition.x, currMousePosition.y);
			GL.glEnable(GL.GL_CULL_FACE);
			GL.glEnable(GL.GL_DEPTH_TEST);

			GL.glMatrixMode(GL.GL_PROJECTION);
			GL.glPopMatrix();
			GL.glMatrixMode(GL.GL_MODELVIEW);
			GL.glPopMatrix();
		}
		private void DrawSelectedVertice_ByPoint()
		{
			Mesh m = currMeshRecord.Mesh;

			GL.glColor3f(1.0f, 0.4f, 0.4f);
			GL.glPointSize(Program.displayProperty.PointSize);
			fixed (double* vp = m.VertexPos)
			{
				GL.glBegin(GL.GL_POINTS);
				for (int i = 0; i < m.VertexCount; i++)
				{
					if (m.Flag[i] == 0) continue;
					switch (m.Flag[i] % 6)
					{
						case 0: GL.glColor3f(1.0f, 1.0f, 0.0f); break;
						case 1: GL.glColor3f(0.0f, 1.0f, 0.0f); break;
						case 2: GL.glColor3f(0.0f, 0.0f, 1.0f); break;
						case 3: GL.glColor3f(1.0f, 0.0f, 0.0f); break;
						case 4: GL.glColor3f(0.0f, 1.0f, 1.0f); break;
						case 5: GL.glColor3f(1.0f, 0.0f, 1.0f); break;
					}
					GL.glVertex3dv(vp + i * 3);
				}
				GL.glEnd();
			}
		}
		private void DrawTransparentSmoothShaded()
		{
			Mesh m = currMeshRecord.Mesh;
			SortFaces();

			GL.glShadeModel(GL.GL_SMOOTH);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);


			Color c = Program.displayProperty.MeshColor;
			GL.glColor4ub(c.R, c.G, c.B, 128);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.glDisable(GL.GL_DEPTH_TEST);
			GL.glDisable(GL.GL_CULL_FACE);
			fixed (double* vp = m.VertexPos)
			fixed (double* np = m.VertexNormal)
			fixed (int* fd = faceDepth)
			{
				GL.glBegin(GL.GL_TRIANGLES);
				for (int i = 0; i < m.FaceCount; i++)
				{
					int j = faceDepth[i] * 3;
					GL.glNormal3dv(np + m.FaceIndex[j] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j] * 3);
					GL.glNormal3dv(np + m.FaceIndex[j + 1] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j + 1] * 3);
					GL.glNormal3dv(np + m.FaceIndex[j + 2] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j + 2] * 3);
				}
				GL.glEnd();
			}
			GL.glDisable(GL.GL_LIGHTING);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.glEnable(GL.GL_DEPTH_TEST);
			GL.glEnable(GL.GL_CULL_FACE);
		}
		private void DrawTransparentSmoothShaded(byte alpha)
		{
			Mesh m = currMeshRecord.Mesh;
			SortFaces();

			GL.glShadeModel(GL.GL_SMOOTH);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);


			Color c = Program.displayProperty.MeshColor;
			GL.glColor4ub(c.R, c.G, c.B, alpha);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.glDisable(GL.GL_DEPTH_TEST);
			GL.glDisable(GL.GL_CULL_FACE);
			fixed (double* vp = m.VertexPos)
			{
				GL.glBegin(GL.GL_TRIANGLES);
				for (int i = 0; i < m.FaceCount; i++)
				{
					int j = faceDepth[i] * 3;
					GL.glVertex3dv(vp + m.FaceIndex[j] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j + 1] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j + 2] * 3);
				}
				GL.glEnd();
			}
			GL.glDisable(GL.GL_LIGHTING);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.glEnable(GL.GL_DEPTH_TEST);
			GL.glEnable(GL.GL_CULL_FACE);
		}
		private void DrawTransparentSmoothShaded2()
		{
			if (currMeshRecord == null) return;
			if (currMeshRecord.Mesh == null) return;
			if (currMeshRecord.Skeletonizer == null) return;

			currMeshRecord.Skeletonizer.DisplayOriginalMesh = false;
			DrawFlatHiddenLine();
			//DrawSmoothShaded();
			//DrawSelectedVertice_ByPoint();
			currMeshRecord.Skeletonizer.DisplayOriginalMesh = true;
			DrawTransparentSmoothShaded(64);
		}
		private void DrawTransparentFlatShaded()
		{
			Mesh m = currMeshRecord.Mesh;
			SortFaces();

			GL.glShadeModel(GL.GL_SMOOTH);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);


			Color c = Program.displayProperty.MeshColor;
			GL.glColor4ub(c.R, c.G, c.B, 180);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.glDisable(GL.GL_DEPTH_TEST);
			GL.glDisable(GL.GL_CULL_FACE);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = m.VertexPos)
			fixed (double* np = m.FaceNormal)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glNormalPointer(GL.GL_DOUBLE, 0, np);
				GL.glBegin(GL.GL_TRIANGLES);
				for (int i = 0; i < m.FaceCount; i++)
				{
					int j = faceDepth[i] * 3;
					GL.glNormal3dv(np + j);
					GL.glVertex3dv(vp + m.FaceIndex[j] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j + 1] * 3);
					GL.glVertex3dv(vp + m.FaceIndex[j + 2] * 3);
				}
				GL.glEnd();
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glDisable(GL.GL_LIGHTING);
			GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
			GL.glEnable(GL.GL_DEPTH_TEST);
			GL.glEnable(GL.GL_CULL_FACE);
		}

		private void SelectVertexByRect()
		{
			Vector2d minV = Vector2d.Min(mouseDownPosition, currMousePosition);
			Vector2d size = Vector2d.Max(mouseDownPosition, currMousePosition) - minV;
			Rectangle rect = new Rectangle((int)minV.x, (int)minV.y, (int)size.x, (int)size.y);
			Rectangle viewport = new Rectangle(0, 0, this.Width, this.Height);
			OpenGLProjector projector = new OpenGLProjector();
			Mesh m = currMeshRecord.Mesh;
			bool laser = Program.toolsProperty.Laser;
			double eps = Program.toolsProperty.DepthTolerance;

			if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
				for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
				{
					Vector3d v = projector.Project(m.VertexPos, j);
					if (viewport.Contains((int)v.x, (int)v.y))
					{
						bool flag = rect.Contains((int)v.x, (int)v.y);
						flag &= (laser || projector.GetDepthValue((int)v.x, (int)v.y) - v.z >= eps);
						if (flag) m.Flag[i] = (byte)1;
					}
				}

			else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
				for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
				{
					Vector3d v = projector.Project(m.VertexPos, j);
					if (viewport.Contains((int)v.x, (int)v.y))
					{
						bool flag = rect.Contains((int)v.x, (int)v.y);
						flag &= (laser || projector.GetDepthValue((int)v.x, (int)v.y) - v.z >= eps);
						if (flag) m.Flag[i] = (byte)0;
					}
				}

			else
				for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
				{
					Vector3d v = projector.Project(m.VertexPos, j);
					if (viewport.Contains((int)v.x, (int)v.y))
					{
						bool flag = rect.Contains((int)v.x, (int)v.y);
						flag &= (laser || projector.GetDepthValue((int)v.x, (int)v.y) - v.z >= eps);
						m.Flag[i] = (byte)((flag) ? 1 : 0);
					}
				}
		}
		private void SelectVertexByPoint()
		{
			Rectangle viewport = new Rectangle(0, 0, this.Width, this.Height);
			OpenGLProjector projector = new OpenGLProjector();
			Mesh m = currMeshRecord.Mesh;
			bool laser = Program.toolsProperty.Laser;
			double eps = Program.toolsProperty.DepthTolerance;

			double minDis = double.MaxValue;
			int minIndex = -1;
			for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
			{
				Vector3d v = projector.Project(m.VertexPos, j);
				Vector2d u = new Vector2d(v.x, v.y);
				if (!viewport.Contains((int)v.x, (int)v.y)) continue;
				if (!laser && projector.GetDepthValue((int)v.x, (int)v.y) - v.z < eps) continue;

				double dis = (u - currMousePosition).Length();
				if (dis < minDis)
				{
					minIndex = i;
					minDis = dis;
				}
			}
			if (minIndex == -1) return;
			//FormMain.CurrForm.OutputText("selected vertex: " + minIndex.ToString());

			if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
				m.Flag[minIndex] = (byte)1;
			else if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
				m.Flag[minIndex] = (byte)0;
			else
			{
				for (int i = 0; i < m.VertexCount; i++) m.Flag[i] = (byte)0;
				m.Flag[minIndex] = (byte)1;
			}
		}
		private bool StartMoving()
		{
			// find closest selected vertex
			this.GrabContext();
			OpenGLProjector projector = new OpenGLProjector();
			Mesh m = currMeshRecord.Mesh;
			Deformer deformer = currMeshRecord.Deformer;
			Rectangle viewport = new Rectangle(0, 0, this.Width, this.Height);
			double eps = Program.toolsProperty.DepthTolerance;
			double minDis = double.MaxValue;
			int minIndex = -1;

			for (int i = 0, j = 0; i < m.VertexCount; i++, j += 3)
			{
				if (m.Flag[i] == 0) continue;
				Vector3d v3d = projector.Project(m.VertexPos, j);
				Vector2d v = new Vector2d(v3d.x, v3d.y);
				if (viewport.Contains((int)v.x, (int)v.y) == false) continue;
				if (projector.GetDepthValue((int)v.x, (int)v.y) - v3d.z < eps) continue;

				double dis = (v - mouseDownPosition).Length();
				if (dis < minDis)
				{
					minDis = dis;
					minIndex = i;
				}
			}
			if (minIndex == -1)
			{
				this.handleFlag = -1;
				this.handleIndex.Clear();
				this.oldHandlePos.Clear();
				return false;
			}

			// find boundary box
			int flag = m.Flag[minIndex];
			this.handleFlag = flag;
			this.handleIndex.Clear();
			this.oldHandlePos.Clear();

			Vector3d c = new Vector3d(0, 0, 0);
			int count = 0;
			{
				for (int i = 0; i < m.VertexCount; i++)
					if (m.Flag[i] == flag)
					{
						Vector3d p = new Vector3d(m.VertexPos, i * 3);
						c += p;
						this.handleIndex.Add(i);
						this.oldHandlePos.Add(p);
						count++;
					}
			}
			c /= (double)count;

			this.handleCenter = new Vector4d(c, 0);
			this.projectedCenter = projector.Project(handleCenter.x, handleCenter.y, handleCenter.z);
			return true;
		}
		private void SortFaces()
		{
			Mesh m = currMeshRecord.Mesh;
			Matrix4d tran = ball.GetMatrix() * currTransformation;

			//sort faces
			FaceDepth[] d = new FaceDepth[m.FaceCount];
			for (int i = 0; i < m.FaceCount; i++)
			{
				Vector4d v = new Vector4d(new Vector3d(m.DualVertexPos, i * 3), 1.0);
				v = tran * v;
				d[i] = new FaceDepth(i, v.z);
			}
			Array.Sort(d);
			faceDepth = new int[m.FaceCount];
			for (int i = 0; i < m.FaceCount; i++)
				faceDepth[i] = d[i].index;
		}

		public void SetModel(MeshRecord rec)
		{
			if (currMeshRecord != null)
				currMeshRecord.ModelViewMatrix = currTransformation;
			if (rec != null) 
				currTransformation = rec.ModelViewMatrix;
			currMeshRecord = rec;
			this.Refresh();
		}
	}
}
