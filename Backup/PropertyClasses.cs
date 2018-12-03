using System.Drawing;
using System.IO;
using System.ComponentModel;
using MyGeometry;

namespace IsolineEditing
{
	public class MeshRecord
	{
		private string filename;
		private Mesh mesh = null;
		private Matrix4d modelViewMatrix = Matrix4d.IdentityMatrix();
		private Deformer deformer = null;
		private Skeletonizer skeletonizer = null;

		public string Filename
		{
			get { return filename; }
		}
		public int VertexCount
		{
			get { return mesh.VertexCount; }
		}
		public int FaceCount
		{
			get { return mesh.FaceCount; }
		}

		[Browsable(false)]
		public Mesh Mesh
		{
			get { return mesh; }
		}
		[Browsable(false)]
		public Matrix4d ModelViewMatrix
		{
			get { return modelViewMatrix; }
			set { modelViewMatrix = value; }
		}
		public Deformer Deformer
		{
			get { return deformer; }
			set { deformer = value; }
		}
		public Skeletonizer Skeletonizer
		{
			get { return skeletonizer; }
			set { skeletonizer = value; }
		}

		public MeshRecord(string filename, Mesh mesh)
		{
			this.filename = filename;
			this.mesh = mesh;
		}

		public override string ToString()
		{
			return Path.GetFileName(filename);
		}
	};

	public class DisplayProperty
	{
		public enum EnumMeshDisplayMode
		{
			None, Points, Wireframe, FlatShaded, SmoothShaded,
			FlatShadedHiddenLine, SmoothShadedHiddenLine,
			TransparentSmoothShaded, TransparentFlatShaded,
			/*TransparentSmoothShaded2*/
		};

		private EnumMeshDisplayMode meshDisplayMode = EnumMeshDisplayMode.SmoothShaded;
		private Color meshColor = Color.CornflowerBlue;
		private Color pointColor = Color.Blue;
		private Color lineColor = Color.Black;
		private float pointSize = 2.0f;
		private float lineWidth = 1.0f;
        private bool displaySelectedVertices = true;

		public EnumMeshDisplayMode MeshDisplayMode
		{
			get { return meshDisplayMode; }
			set { meshDisplayMode = value; }
		}
		[Category("Color")] public Color MeshColor
		{
			get { return meshColor; }
			set { meshColor = value; }
		}
		[Category("Color")] public Color PointColor
		{
			get { return pointColor; }
			set { pointColor = value; }
		}
		[Category("Color")] public Color LineColor
		{
			get { return lineColor; }
			set { lineColor = value; }
		}
		[Category("Element size")] public float PointSize
		{
			get { return pointSize; }
			set { pointSize = value; if (pointSize <= 0) pointSize = 0.1f; }
		}
		[Category("Element size")] public float LineWidth
		{
			get { return lineWidth; }
			set { lineWidth = value; }
		}
        public bool DisplaySelectedVertices
        {
            get { return displaySelectedVertices; }
            set { displaySelectedVertices = value; }
        }

	};

	public class ToolsProperty
	{
		public enum EnumSelectingMethod { Rectangle, Point };

		private bool selectionLaser = true;
		private double depthTolerance = -0.0001;
		private EnumSelectingMethod selectionMethod = EnumSelectingMethod.Rectangle;

		[Category("Selection Tool")]
		public bool Laser
		{
			get { return selectionLaser; }
			set { selectionLaser = value; }
		}
		[Category("Selection Tool")]
		public double DepthTolerance
		{
			get { return depthTolerance; }
			set { depthTolerance = value; }
		}
		[Category("Selection Tool")]
		public EnumSelectingMethod SelectionMethod
		{
			get { return selectionMethod; }
			set { selectionMethod = value; }
		}

	};
}