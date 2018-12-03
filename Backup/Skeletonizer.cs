using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Drawing;
using CsGL.OpenGL;
using MyGeometry;

namespace IsolineEditing
{
	[TypeConverterAttribute(typeof(DeformerConverter))]
	public unsafe class Skeletonizer : IDisposable
	{
		#region import functions
		[DllImport("taucs.dll")]
		static extern unsafe void* CreaterCholeskySolver(int n, int nnz, int* rowIndex, int* colIndex, double* value);
		[DllImport("taucs.dll")]
		static extern unsafe int Solve(void* solver, double* x, double* b);
		[DllImport("taucs.dll")]
		static extern unsafe double SolveEx(void* solver, double* x, int xIndex, double* b, int bIndex);
		[DllImport("taucs.dll")]
		static extern unsafe int FreeSolver(void* sp);

		[DllImport("taucs.dll")]
		static extern unsafe void* CreaterSymbolicSolver(int n, int nnz, int* rowIndex, int* colIndex, double* value);
		[DllImport("taucs.dll")]
		static extern unsafe void FreeSymbolicSolver(void* sp);
		[DllImport("taucs.dll")]
		static extern unsafe int NumericFactor(void* sp, int n, int nnz, int* rowIndex, int* colIndex, double* value);
		[DllImport("taucs.dll")]
		static extern unsafe void FreeNumericFactor(void* sp);
		[DllImport("taucs.dll")]
		static extern unsafe int NumericSolve(void* sp, double* x, double* b);

		#endregion

		public class Options
		{
			// general options
			private bool addNoise = false;
			private double noiseRatio = 0.02;
			private bool twoDimensionModel = false;

			// options for geometry collapsing
			private int maxIterations = 30;
			private double laplacianConstraintWeight = 1.0;
			private double positionalConstraintWeight = 1.0;
			private double originalPositionalConstraintWeight = 0.0;
			private double laplacianConstraintScale = 2.0;
			private double positionalConstraintScale = 1.5;
			private double areaRatioThreshold = 0.001;
			private bool useSymbolicSolver = false;
			private bool useIterativeSolver = false;
			private double volumneRatioThreashold = 0.00001;

			// options for simplification
			private bool applySimplification = true;
			private bool displayIntermediateMesh = false;
			private bool useShapeEnergy = true;
			private bool useSamplingEnergy = true;
			private double shapeEnergyWeight = 0.1;
			private int targetVertexCount = 10;

			// options for embedding
			private bool applyEmbedding = true;
			private int numOfImprovement = 100;
			private bool postSimplify = true;
			private double postSimplifyErrorRatio = 0.9;
			private bool useBoundaryVerticesOnly = true;

			[Browsable(false)]
			[Category("0.General Options")]
			public bool AddNoise
			{
				get { return addNoise; }
				set { addNoise = value; }
			}
			[Browsable(false)]
			[Category("0.General Options")]
			public double NoiseRatio
			{
				get { return noiseRatio; }
				set { noiseRatio = value; }
			}
			[Browsable(false)]
			[Category("0.General Options")]
			public bool TwoDimensionModel
			{
				get { return twoDimensionModel; }
				set { twoDimensionModel = value; }
			}

			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public int MaxIterations
			{
				get { return maxIterations; }
				set { maxIterations = value; }
			}
			[Category("1.Geometry Contraction")]
			public double LaplacianConstraintWeight
			{
				get { return laplacianConstraintWeight; }
				set { laplacianConstraintWeight = value; }
			}
			[Category("1.Geometry Contraction")]
			public double PositionalConstraintWeight
			{
				get { return positionalConstraintWeight; }
				set { positionalConstraintWeight = value; }
			}
			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public double OriginalPositionalConstraintWeight
			{
				get { return originalPositionalConstraintWeight; }
				set { originalPositionalConstraintWeight = value; }
			}
			[Category("1.Geometry Contraction")]
			public double LaplacianConstraintScale
			{
				get { return laplacianConstraintScale; }
				set { laplacianConstraintScale = value; }
			}
			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public double PositionalConstraintScale
			{
				get { return positionalConstraintScale; }
				set { positionalConstraintScale = value; }
			}
			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public double AreaRatioThreshold
			{
				get { return areaRatioThreshold; }
				set { areaRatioThreshold = value; }
			}
			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public bool UseSymbolicSolver
			{
				get { return useSymbolicSolver; }
				set
				{
					useSymbolicSolver = value;
					if (useSymbolicSolver) useIterativeSolver = false;
				}
			}
			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public bool UseIterativeSolver
			{
				get { return useIterativeSolver; }
				set
				{
					useIterativeSolver = value;
					if (useIterativeSolver) useSymbolicSolver = false;
				}
			}
			[Browsable(false)]
			[Category("1.Geometry Contraction")]
			public double VolumneRatioThreashold
			{
				get { return volumneRatioThreashold; }
				set { volumneRatioThreashold = value; }
			}

			[Category("2.Connectivity Surgery")]
			public bool ApplyConnectivitySurgery
			{
				get { return applySimplification; }
				set
				{
					applySimplification = value;
					if (applySimplification == false)
						this.applyEmbedding = false;
				}
			}
			[Browsable(false)]
			[Category("2.Connectivity Surgery")]
			public bool DisplayIntermediateMesh
			{
				get { return displayIntermediateMesh; }
				set { displayIntermediateMesh = value; }
			}
			[Browsable(false)]
			[Category("2.Connectivity Surgery")]
			public bool UseShapeEnergy
			{
				get { return useShapeEnergy; }
				set { useShapeEnergy = value; }
			}
			[Browsable(false)]
			[Category("2.Connectivity Surgery")]
			public bool UseSamplingEnergy
			{
				get { return useSamplingEnergy; }
				set { useSamplingEnergy = value; }
			}
			[Browsable(false)]
			[Category("2.Connectivity Surgery")]
			public double ShapeEnergyWeight
			{
				get { return shapeEnergyWeight; }
				set { shapeEnergyWeight = value; }
			}
			[Browsable(false)]
			[Category("2.Connectivity Surgery")]
			public int TargetVertexCount
			{
				get { return targetVertexCount; }
				set { targetVertexCount = value; }
			}

			[Category("3.Embedding Refinement")]
			public bool ApplyEmbeddingRefinement
			{
				get { return applyEmbedding; }
				set
				{
					applyEmbedding = value;
					if (applyEmbedding == true)
						applySimplification = true;
				}
			}
			[Browsable(false)]
			[Category("3.Embedding Refinement")]
			public int NumOfImprovement
			{
				get { return numOfImprovement; }
				set { numOfImprovement = value; }
			}
			[Browsable(false)]
			[Category("3.Embedding Refinement")]
			public bool PostSimplify
			{
				get { return postSimplify; }
				set { postSimplify = value; }
			}
			[Browsable(false)]
			[Category("3.Embedding Refinement")]
			public double PostSimplifyErrorRatio
			{
				get { return postSimplifyErrorRatio; }
				set { postSimplifyErrorRatio = value; }
			}
			[Browsable(false)]
			[Category("3.Embedding Refinement")]
			public bool UseBoundaryVerticesOnly
			{
				get { return useBoundaryVerticesOnly; }
				set { useBoundaryVerticesOnly = value; }
			}

			public override string ToString()
			{
				return "Skeleton Extraction Options";
			}
		};
		public class SegmentationOpt
		{
			private int segmentCount = 10;

			public int SegmentCount
			{
				get { return segmentCount; }
				set
				{
					segmentCount = value;
					if (segmentCount <= 0) segmentCount = 1;
				}
			}
		};
		// data structure for simplification and embedding
		private class VertexRecord : PriorityQueueElement
		{
			public Vector3d pos;
			public double radius;
			public Set<int> adjV = new Set<int>(8);
			public Set<int> adjF = new Set<int>(8);
			public Set<int> collapseFrom = new Set<int>(2);
			public Dictionary<int, double> boundaryLength = null;
			public int vIndex = -1;
			public int minIndex = -1;
			public int pqIndex = -1;
			public Matrix4d matrix = new Matrix4d();
			public double minError = double.MaxValue;
			public int colorIndex = -1;
			public bool center = false;
			public double err = 0;
			public double nodeSize = 1;
			/*
						// added by youyi 14/11/2007
						public double branchErr = 0;        
						public int joint = 0;
						public bool bClustered = false;
			*/

			public VertexRecord(Mesh mesh, int i)
			{
				vIndex = i;
				pos = new Vector3d(mesh.VertexPos, i * 3);
				foreach (int index in mesh.AdjVV[i])
					adjV.Add(index);
				foreach (int index in mesh.AdjVF[i])
					adjF.Add(index);
			}

			public int PQIndex
			{
				get { return pqIndex; }
				set { pqIndex = value; }
			}
			public int CompareTo(object obj)
			{
				VertexRecord rec = obj as VertexRecord;
				if (minError < rec.minError) return -1;
				if (minError > rec.minError) return 1;
				return 0;
			}
		};

		private class CutRecord : IComparable
		{
			public int from = -1;
			public int to = -1;
			public double cost = 0;

			public CutRecord(int from, int to, double cost)
			{
				this.from = from;
				this.to = to;
				this.cost = cost;
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				CutRecord rec = obj as CutRecord;
				if (cost < rec.cost) return -1;
				if (cost > rec.cost) return 1;
				return 0;
			}

			#endregion
		};

		private Mesh mesh = null;
		private Options opt = null;
		//private SparseMatrix A = null;
		private CCSMatrix ccsA = null;
		private CCSMatrix ccsATA = null;
		private double[][] lap = new double[3][];
		private double[] originalVertexPos = null;
		private double[] collapsedVertexPos = null;
		private double[] lapWeight = null;
		private double[] posWeight = null;
		private double[] originalFaceArea = null;
		private double[] collapsedLength = null;
		private double[] oldAreaRatio = null;
		private VertexRecord[] vRec = null;
		private List<VertexRecord> simplifiedVertexRec = null;
		private void* solver = null;
		private void* symbolicSolver = null;
		private int iter = 0;
		private int[] faceIndex = null;
		private int[] adjSegmentVertex = null;
		private MultigridContractionSolver multigridSolver = null;

		// display options
		private bool displaySkeleton = true;
		private bool displayOriginalMesh = false;
		private bool displayNodeSphere = false;
		private bool displaySimplifiedMesh = false;
		private int displaySimplifiedMeshIndex = 0;
		private float skeletonNodeSize = 6.0f;
		private bool displayIntermediateMesh = false;
		private int remainingVertexCount = 0;

		public bool DisplaySkeleton
		{
			get { return displaySkeleton; }
			set { displaySkeleton = value; }
		}
		public bool DisplayOriginalMesh
		{
			get { return displayOriginalMesh; }
			set
			{
				displayOriginalMesh = value;
				if (displayOriginalMesh)
				{
					mesh.VertexPos = originalVertexPos;
					mesh.ComputeFaceNormal();
					mesh.ComputeVertexNormal();
				}
				else
				{
					mesh.VertexPos = collapsedVertexPos;
					mesh.ComputeFaceNormal();
					mesh.ComputeVertexNormal();
				}
			}
		}
		[Browsable(false)]
		public bool DisplayNodeSphere
		{
			get { return displayNodeSphere; }
			set { displayNodeSphere = value; }
		}
		[Browsable(false)]
		public bool DisplaySimplifiedMesh
		{
			get { return displaySimplifiedMesh; }
			set { displaySimplifiedMesh = value; }
		}
		public float SkeletonNodeSize
		{
			get { return skeletonNodeSize; }
			set { skeletonNodeSize = value; }
		}
		[Browsable(false)]
		public int DisplaySimplifiedMeshIndex
		{
			get { return displaySimplifiedMeshIndex; }
			set
			{
				if (multigridSolver != null)
				{
					if (value < 0) value = 0;
					if (value >= multigridSolver.Levels) value = multigridSolver.Levels - 1;
				}
				else
				{
					value = -1;
				}
				displaySimplifiedMeshIndex = value;
			}
		}
		public int NodeCount
		{
			get 
			{ 
				if (simplifiedVertexRec != null) 
					return simplifiedVertexRec.Count;
				return 0;
			}
		}

		public Skeletonizer(Mesh mesh, Options opt)
		{

			int n = mesh.VertexCount;
			int fn = mesh.FaceCount;
			this.mesh = mesh;
			this.opt = opt;
			if (opt.AddNoise) AddNoise();

			this.lapWeight = new double[n];
			this.posWeight = new double[n];
			this.collapsedLength = new double[n];
			this.originalVertexPos = (double[])mesh.VertexPos.Clone();
			this.originalFaceArea = new double[fn];
			for (int i = 0; i < fn; i++) originalFaceArea[i] = Math.Abs(mesh.ComputeFaceArea(i));
			for (int i = 0; i < n; i++)
			{
				lapWeight[i] = opt.LaplacianConstraintWeight;
				posWeight[i] = opt.PositionalConstraintWeight;
				collapsedLength[i] = 0;
			}

			if (opt.UseIterativeSolver)
				this.multigridSolver = new MultigridContractionSolver(mesh);
		}
		~Skeletonizer()
		{
			Dispose();
		}
		public void Display()
		{
			lock (this)
			{
				if (displaySkeleton && simplifiedVertexRec != null)
				{
					DrawSimplifiedVertices();
				}
				if (displayNodeSphere && simplifiedVertexRec != null)
				{
					DrawNodeSphere();
				}
				if (displaySimplifiedMesh && multigridSolver != null)
				{
					GL.glColor3d(0.0, 0.9, 0);
					DrawSimplifiedMesh_by_Vertices();
					// 				if (displaySimplifiedMeshIndex < multigridSolver.Levels - 1)
					// 				{
					// 					displaySimplifiedMeshIndex++;
					// 					GL.glColor3d(0.0, 0.8, 0);
					// 					DrawSimplifiedMesh();
					// 					displaySimplifiedMeshIndex--;
					// 				}
				}

				if (displayIntermediateMesh && vRec != null)
				{
					DrawIntermediateMesh();
				}
			}
		}
		public void WriteSkeleton(StreamWriter sw)
		{
			int count = 0;
			int[] map = new int[vRec.Length];
			foreach (VertexRecord rec in simplifiedVertexRec)
				map[rec.vIndex] = count++;

			sw.WriteLine(simplifiedVertexRec.Count);

			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				string s = rec.pos.x + " " + rec.pos.y + " " + rec.pos.z;
				foreach (int adj in rec.adjV)
					s += " " + map[adj];
				sw.WriteLine(s);
			}
		}
		public void WriteSegmentation(StreamWriter sw)
		{
			if (simplifiedVertexRec == null) throw new Exception();

			int n = mesh.VertexCount;
			int[] flags = new int[n];

			for (int i = 0; i < n; i++)
				flags[i] = 0;

			int counter = 0;
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				counter++;
				rec.minIndex = counter;
				flags[rec.vIndex] = counter;
				foreach (int index in rec.collapseFrom)
					flags[index] = counter;
			}

			sw.WriteLine(counter.ToString());
			sw.WriteLine(n.ToString());
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				sw.Write(rec.pos.x.ToString() + " ");
				sw.Write(rec.pos.y.ToString() + " ");
				sw.Write(rec.pos.z.ToString() + " ");

				foreach (int i in rec.adjV)
				{
					VertexRecord adjRec = vRec[i];
					sw.Write(adjRec.minIndex + " ");
				}
				sw.WriteLine();
			}

			for (int i = 0; i < n; i++)
			{
				sw.WriteLine(flags[i].ToString());
			}
		}
		public void Start()
		{
			Program.displayProperty.MeshDisplayMode = DisplayProperty.EnumMeshDisplayMode.SmoothShaded;
			Program.displayProperty.DisplaySelectedVertices = true;

				GeometryCollapse(opt.MaxIterations);
			if (opt.DisplayIntermediateMesh)
			{
				Program.displayProperty.MeshDisplayMode = DisplayProperty.EnumMeshDisplayMode.None;
				Program.displayProperty.DisplaySelectedVertices = false;
			}
			if (opt.ApplyConnectivitySurgery) Simplification();
			if (opt.ApplyEmbeddingRefinement) EmbeddingImproving();

			if (opt.ApplyConnectivitySurgery)
			{
				this.DisplayOriginalMesh = true;
				Program.displayProperty.MeshDisplayMode = DisplayProperty.EnumMeshDisplayMode.TransparentSmoothShaded;
				Program.displayProperty.DisplaySelectedVertices = false;
			}
			else
			{
				this.DisplayOriginalMesh = false;
				Program.displayProperty.MeshDisplayMode = DisplayProperty.EnumMeshDisplayMode.Wireframe;
				Program.displayProperty.DisplaySelectedVertices = false;
			}
			Program.PrintText("[Ready]");
			Program.currentForm.FinishSkeletonization();
		}
		private void DrawSimplifiedMesh_by_Faces()
		{
			Mesh m = this.mesh;
			int[] faceIndex = multigridSolver.Resolutions[displaySimplifiedMeshIndex].faceList;
			int fn = faceIndex.Length / 3;

			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
			GL.glDisable(GL.GL_CULL_FACE);
			Color c = Program.displayProperty.LineColor;
			GL.glColor3ub(c.R, c.G, c.B);

			GL.glLineWidth(Program.displayProperty.LineWidth);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = originalVertexPos)
			fixed (int* index = faceIndex)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glDrawElements(GL.GL_TRIANGLES, fn * 3, GL.GL_UNSIGNED_INT, index);
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glEnable(GL.GL_CULL_FACE);
		}
		private void DrawSimplifiedMesh_by_Edges()
		{
			Mesh m = this.mesh;
			int[] faceIndex = multigridSolver.Resolutions[displaySimplifiedMeshIndex].faceList;
			int fn = faceIndex.Length / 3;

			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
			GL.glDisable(GL.GL_CULL_FACE);
			Color c = Program.displayProperty.LineColor;
			GL.glColor3ub(c.R, c.G, c.B);

			GL.glLineWidth(Program.displayProperty.LineWidth);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = originalVertexPos)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glBegin(GL.GL_LINES);
				MultigridContractionSolver.Resolution r =
					multigridSolver.Resolutions[displaySimplifiedMeshIndex];
				int[] v = r.vertexList;
				Set<int>[] s = r.adjVertex;
				for (int i = 0; i < v.Length; i++)
				{
					foreach (int adj in s[i])
					{
						GL.glArrayElement(v[i]);
						GL.glArrayElement(adj);
					}
				}
				GL.glEnd();
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glEnable(GL.GL_CULL_FACE);
		}
		private void DrawSimplifiedMesh_by_Vertices()
		{
			Mesh m = this.mesh;

			GL.glLineWidth(Program.displayProperty.LineWidth);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = originalVertexPos)
			{
				GL.glPointSize(4f);
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glBegin(GL.GL_POINTS);
				MultigridContractionSolver.Resolution r =
					multigridSolver.Resolutions[displaySimplifiedMeshIndex];
				int[] v = r.vertexList;
				Set<int>[] s = r.adjVertex;
				for (int i = 0; i < v.Length; i++)
				{
					GL.glArrayElement(v[i]);
				}
				GL.glEnd();
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
			GL.glEnable(GL.GL_CULL_FACE);
		}

		// functions for geometry collapsing
		private void GeometryCollapse(int maxIter)
		{
			Program.PrintText("[Geometry collaping]");

			double originalVolume = mesh.Volume();
			double originalArea = 0;
			for (int i = 0; i < mesh.FaceCount; i++)
				originalArea += mesh.ComputeFaceArea(i);

			double currentVolume = 0;
			double currentArea = 0;
			if (maxIter == 0) return;
			// 			try
			{
				do
				{
					Win32.HiPerfTimer timer = new Win32.HiPerfTimer();
					timer.Start();

					if (opt.UseIterativeSolver)
					{
						double[][] pos = multigridSolver.SolveSystem(this.lapWeight, this.posWeight);
						for (int i = 0, j = 0; i < mesh.VertexCount; i++, j += 3)
						{
							mesh.VertexPos[j] = pos[0][i];
							mesh.VertexPos[j + 1] = pos[1][i];
							mesh.VertexPos[j + 2] = pos[2][i];
						}
						BuildMatrixA();
						mesh.ComputeFaceNormal();
						mesh.ComputeVertexNormal();
						iter++;
					}
					else
					{
						SparseMatrix A = null;
						lock (this)
						{
							A = BuildMatrixA();
						}
						this.ccsA = new CCSMatrix(A);
						this.ccsATA = MultiplyATA(ccsA);
						A = null;
						//System.GC.Collect();
						if (opt.UseSymbolicSolver)
						{
							this.symbolicSolver = SymbolicFactorization(ccsATA);
							Program.PrintText("Symbolic solver: " + (symbolicSolver != null).ToString());
							int ret = NumericFactorization(symbolicSolver, ccsATA);
							Program.PrintText("Numeric solver: " + (ret == 0).ToString());
						}
						else
						{
							if (solver != null) FreeSolver(solver);
							this.solver = Factorization(ccsATA);
							if (solver == null) throw new Exception();
							//GC.Collect();
						}
						lock (this)
							ImplicitSmooth();
					}

					timer.Stop();

					{
						currentVolume = mesh.Volume();
						currentArea = 0;
						for (int i = 0; i < mesh.FaceCount; i++)
							currentArea += mesh.ComputeFaceArea(i);
						Program.PrintText(
							iter +
							" Area: " + currentArea / originalArea +
							" Vol: " + currentVolume / originalVolume +
							" CPU Time: " + timer.Duration
							);

						// 					volume = mesh.Volume();
						// 					Program.PrintText(iter + " Vol: " + volume + " CPU Time: " + timer.Duration);
						//Thread.Sleep(0);
					}
				}
				while (
					currentVolume / originalVolume > opt.VolumneRatioThreashold &&
					//currentArea / originalArea > 0.001 &&
					//currentVolume / originalVolume > 0.00001 &&
					iter < maxIter);
			}
			/*
						catch (Exception e)
						{
							Program.PrintText(e.ToString());
						}
			*/
			Dispose();

			this.collapsedVertexPos = mesh.VertexPos.Clone() as double[];
		}
		private void GeometryCollapse2(int maxIter)
		{
			Program.PrintText("[Geometry Contraction]");

			double originalVolume = mesh.Volume();
			double originalArea = 0;
			for (int i = 0; i < mesh.FaceCount; i++)
				originalArea += mesh.ComputeFaceArea(i);

			double currentVolume = 0;
			double currentArea = 0;
			if (maxIter == 0) return;
			// 			try
			{
				do
				{
					Win32.HiPerfTimer timer = new Win32.HiPerfTimer();
					timer.Start();

					if (opt.UseIterativeSolver)
					{
						double[][] pos = multigridSolver.SolveSystem(this.lapWeight, this.posWeight);
						for (int i = 0, j = 0; i < mesh.VertexCount; i++, j += 3)
						{
							mesh.VertexPos[j] = pos[0][i];
							mesh.VertexPos[j + 1] = pos[1][i];
							mesh.VertexPos[j + 2] = pos[2][i];
						}
						BuildMatrixA();
						mesh.ComputeFaceNormal();
						mesh.ComputeVertexNormal();
						iter++;
					}
					else
					{
						SparseMatrix A = BuildMatrixA();
						this.ccsA = new CCSMatrix(A);
						this.ccsATA = MultiplyATA(ccsA);
						A = null;
						System.GC.Collect();
						if (opt.UseSymbolicSolver)
						{
							this.symbolicSolver = SymbolicFactorization(ccsATA);
							//Program.PrintText("Symbolic solver: " + (symbolicSolver != null).ToString());
							int ret = NumericFactorization(symbolicSolver, ccsATA);
							//Program.PrintText("Numeric solver: " + (ret == 0).ToString());
						}
						else
						{
							if (solver != null) FreeSolver(solver);
							this.solver = Factorization(ccsATA);
							if (solver == null) throw new Exception();
							GC.Collect();
						}
						ImplicitSmooth();
					}

					timer.Stop();

					currentVolume = mesh.Volume();
					currentArea = 0;
					for (int i = 0; i < mesh.FaceCount; i++)
						currentArea += mesh.ComputeFaceArea(i);
					Program.PrintText(
						"[Geometry Contraction] - iteration" +
						iter +
						// 						" Area: " + currentArea / originalArea + 
						// 						" Vol: " + currentVolume / originalVolume + 
						" CPU Time: " + timer.Duration
						);

					// 					volume = mesh.Volume();
					// 					Program.PrintText(iter + " Vol: " + volume + " CPU Time: " + timer.Duration);
					Thread.Sleep(0);
				}
				while (
					currentArea / originalArea > 0.001 &&
					currentVolume / originalVolume > 0.00001 &&
					iter < maxIter);
			}
			/*
						catch (Exception e)
						{
							Program.PrintText(e.ToString());
						}
			*/
			Dispose();

			this.collapsedVertexPos = mesh.VertexPos.Clone() as double[];
		}
		private void ImplicitSmooth()
		{
			int n = mesh.VertexCount;
			double[] x = new double[n];
			double[] b = new double[n * 3];
			double[] ATb = new double[n];
			double[] oldPos = (double[])mesh.VertexPos.Clone();

			for (int i = 0; i < 3; i++)
			{
				for (int j = 0, k = 0; j < n; j++, k += 3)
				{
					b[j] = 0;
					// 					b[j + n] = originalVertexPos[k+i] * posWeight[j];
					b[j + n] = mesh.VertexPos[k + i] * posWeight[j];
					b[j + n + n] = 0;
					// 						(mesh.VertexPos[k + i] - mesh.VertexNormal[k + i] * 0.1) 
					// 						* opt.OriginalPositionalConstraintWeight;
				}

				ccsA.PreMultiply(b, ATb);

				if (opt.UseSymbolicSolver)
				{
					fixed (double* _x = x, _ATb = ATb)
						NumericSolve(symbolicSolver, _x, _ATb);
				}
				else
				{
					fixed (double* _x = x, _ATb = ATb)
						Solve(solver, _x, _ATb);
				}

				lock (this)
				{
					for (int j = 0, k = 0; j < n; j++, k += 3)
						mesh.VertexPos[k + i] = x[j];
				}
			}
			iter++;

			{
				mesh.ComputeFaceNormal();
				mesh.ComputeVertexNormal();
				for (int i = 0, j = 0; i < n; i++, j += 3)
				{
					double d1 = (mesh.VertexPos[j] - oldPos[j]);
					double d2 = (mesh.VertexPos[j + 1] - oldPos[j + 1]);
					double d3 = (mesh.VertexPos[j + 2] - oldPos[j + 2]);
					this.collapsedLength[i] += Math.Sqrt(d1 * d1 + d2 * d2 + d3 * d3);
				}
			}
		}
		private SparseMatrix BuildMatrixA()
		{
			int n = mesh.VertexCount;
			int fn = mesh.FaceCount;
			SparseMatrix A = new SparseMatrix(n, n);
			double[] areaRatio = new double[fn];
			bool[] collapsed = new bool[n];

			if (oldAreaRatio == null)
			{
				oldAreaRatio = new double[fn];
				for (int i = 0; i < fn; i++) oldAreaRatio[i] = 0.4;
			}

			for (int i = 0, j = 0; i < fn; i++, j += 3)
			{
				int c1 = mesh.FaceIndex[j];
				int c2 = mesh.FaceIndex[j + 1];
				int c3 = mesh.FaceIndex[j + 2];
				Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
				Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
				Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
				// 				Vector3d v1 = new Vector3d(originalVertexPos, c1 * 3);
				// 				Vector3d v2 = new Vector3d(originalVertexPos, c2 * 3);
				// 				Vector3d v3 = new Vector3d(originalVertexPos, c3 * 3);
				areaRatio[i] = Math.Abs(mesh.ComputeFaceArea(i)) / originalFaceArea[i];
				if (areaRatio[i] < opt.AreaRatioThreshold)
				{
					// 					if (mesh.Flag[c1] == 0) mesh.Flag[c1] = (byte)iter;
					// 					if (mesh.Flag[c2] == 0) mesh.Flag[c2] = (byte)iter;
					// 					if (mesh.Flag[c3] == 0) mesh.Flag[c3] = (byte)iter;
					//					mesh.Flag[c1] = mesh.Flag[c2] = mesh.Flag[c3] = 1;
				}
				double cot1 = (v2 - v1).Dot(v3 - v1) / (v2 - v1).Cross(v3 - v1).Length();
				double cot2 = (v3 - v2).Dot(v1 - v2) / (v3 - v2).Cross(v1 - v2).Length();
				double cot3 = (v1 - v3).Dot(v2 - v3) / (v1 - v3).Cross(v2 - v3).Length();
				//cot1 = cot2 = cot3 = 1.0;
				try
				{
					if (double.IsNaN(cot1)) throw new Exception();
					if (double.IsNaN(cot2)) throw new Exception();
					if (double.IsNaN(cot3)) throw new Exception();
				}
				catch (Exception e)
				{
					Program.PrintText(e.Message);
					Program.PrintText("!!! " + cot1 + " " + cot2 + " " + cot3);
					cot1 = cot2 = cot3 = 0;
				}

				A.AddValueTo(c2, c2, -cot1); A.AddValueTo(c2, c3, cot1);
				A.AddValueTo(c3, c3, -cot1); A.AddValueTo(c3, c2, cot1);
				A.AddValueTo(c3, c3, -cot2); A.AddValueTo(c3, c1, cot2);
				A.AddValueTo(c1, c1, -cot2); A.AddValueTo(c1, c3, cot2);
				A.AddValueTo(c1, c1, -cot3); A.AddValueTo(c1, c2, cot3);
				A.AddValueTo(c2, c2, -cot3); A.AddValueTo(c2, c1, cot3);
			}

			double count = 0;
			lock (this)
				for (int i = 0; i < n; i++)
			{
				double totRatio = 0;
				double oldTotRatio = 0;
				foreach (int j in mesh.AdjVF[i])
				{
					totRatio += areaRatio[j];
					oldTotRatio += oldAreaRatio[j];
				}
				totRatio /= mesh.AdjVF[i].Length;
				oldTotRatio /= mesh.AdjVF[i].Length;

				double tot = 0;

				// normalized by area
				// 				tot = 0;
				// 				foreach (int j in mesh.AdjVF[i])
				// 					tot += mesh.ComputeFaceArea(j);
				// 				foreach (SparseMatrix.Element e in A.Rows[i])
				// 					e.value /= tot;

				tot = 0;
				foreach (SparseMatrix.Element e in A.Rows[i])
					if (e.i != e.j) tot += e.value;
				// 				if (tot > 1) posWeight[i] = tot;
				if (tot > 10000)
				{
					collapsed[i] = true;
						mesh.Flag[i] = 1;
					//posWeight[i] = 100000;
					foreach (SparseMatrix.Element e in A.Rows[i])
					{
						e.value /= (tot / 10000);
						// 						e.value = 0;
					}
				}
				// 				if (posWeight[i] > 100) posWeight[i] = 100;


				// normalized by row sum
				// 				tot = 0;
				// 				foreach (SparseMatrix.Element e in A.Rows[i])
				// 					if (e.i != e.j) tot += e.value;
				// 				foreach (SparseMatrix.Element e in A.Rows[i])
				// 					e.value /= tot;

				foreach (SparseMatrix.Element e in A.Rows[i])
					e.value *= lapWeight[i];

				{
					//lapWeight[i] *= 4.0 * (totRatio / oldTotRatio);
					lapWeight[i] *= opt.LaplacianConstraintScale;
					// 					if (totRatio > 0.001)
					// 						lapWeight[i] *= 2.0;
					if (lapWeight[i] > 2048) lapWeight[i] = 2048;

					double d = (1.0 / (Math.Sqrt(totRatio))) * opt.PositionalConstraintWeight;
					if (!double.IsNaN(d)) posWeight[i] = d;
					// 					if (posWeight[i] > 1000)
					// 					{
					// 						foreach (SparseMatrix.Element e in A.Rows[i])
					// 							e.value = 0;
					// 					}
					//					if (collapsed[i]) posWeight[i] = 100000;
 					if (posWeight[i] > 10000) posWeight[i] = 10000;
					count++;
					// 					Vector3d normal = new Vector3d(mesh.VertexNormal, i*3);
					// 					Vector3d delta = new Vector3d(lap[0][i], lap[1][i], lap[2][i]);
					// 					if (delta.Dot(normal) > 0)
					// 					{
					// 						mesh.Flag[i] = 1;
					// 						//posWeight[i] *= 2;
					// 					}
					// 					else
					// 						mesh.Flag[i] = 0;

					bool ok = true;
					foreach (SparseMatrix.Element e in A.Rows[i])
						if (double.IsNaN(e.value))
						{
							ok = false;
							Program.PrintText(e.i + ":" + e.j + "\n");
						}
					if (!ok)
						foreach (SparseMatrix.Element e in A.Rows[i])
						{
							if (e.i == e.j)
								e.value = -1;
							else
								e.value = 1.0 / mesh.AdjVV[i].Length;
						}
				}
				/*

								if (mesh.Flag[i] != 0) // totRatio < 0.01)
								{
									posWeight[i] *= opt.PositionalConstraintScale;
									if (posWeight[i] > 100000) posWeight[i] = 100000;
									count++;
								}
								else
								{
									lapWeight[i] *= opt.LaplacianConstraintScale;
									if (lapWeight[i] > 2048) lapWeight[i] = 2048;
								}
				*/
			}

			/*
						SparseMatrix tmpA = new SparseMatrix(2 * n, n);
						for (int i = 0; i < n; i++)
						{
							if (mesh.Flag[i] == 0)
								foreach (SparseMatrix.Element e in A.Rows[i])
									tmpA.AddElement(e);
						}
						A = tmpA;
						GC.Collect();
			*/
			for (int i = 0; i < 3; i++)
			{
				this.lap[i] = new double[n];
				double[] x = new double[n];
				for (int j = 0, k = i; j < n; j++, k += 3)
				{
					x[j] = mesh.VertexPos[k];
				}
				A.Multiply(x, lap[i]);
			}


			for (int i = 0; i < fn; i++)
				oldAreaRatio[i] = areaRatio[i];

			// positional constraints
			for (int i = 0; i < n; i++)
			{
				A.AddRow();
				A.AddElement(i + n, i, posWeight[i]);
			}
			// original positional constraints
			for (int i = 0; i < n; i++)
			{
				A.AddRow();
				A.AddElement(i + n + n, i, opt.OriginalPositionalConstraintWeight);
			}
			A.SortElement();
			return A;
		}
		private CCSMatrix MultiplyATA(CCSMatrix A)
		{
			int[] last = new int[A.RowSize];
			int[] next = new int[A.NumNonZero];
			int[] colIndex = new int[A.NumNonZero];
			for (int i = 0; i < last.Length; i++) last[i] = -1;
			for (int i = 0; i < next.Length; i++) next[i] = -1;
			for (int i = 0; i < A.ColumnSize; i++)
			{
				for (int j = A.ColIndex[i]; j < A.ColIndex[i + 1]; j++)
				{
					int k = A.RowIndex[j];
					if (last[k] != -1) next[last[k]] = j;
					last[k] = j;
					colIndex[j] = i;
				}
			}
			last = null;

			CCSMatrix ATA = new CCSMatrix(A.ColumnSize, A.ColumnSize);
			Set<int> set = new Set<int>();
			double[] tmp = new double[A.ColumnSize];
			List<int> ATA_RowIndex = new List<int>();
			List<double> ATA_Value = new List<double>();

			for (int i = 0; i < A.ColumnSize; i++) tmp[i] = 0;

			for (int j = 0; j < A.ColumnSize; j++)
			{
				for (int col = A.ColIndex[j]; col < A.ColIndex[j + 1]; col++)
				{
					int k = A.RowIndex[col];
					double val = A.Values[col];

					int curr = col;
					while (true)
					{
						int i = colIndex[curr];
						set.Add(i);
						tmp[i] += val * A.Values[curr];
						if (next[curr] != -1)
							curr = next[curr];
						else
							break;
					}
				}

				int[] s = set.ToArray(); Array.Sort(s);
				int count = 0;
				foreach (int k in s)
				{
					if (tmp[k] == 0) continue;
					ATA_RowIndex.Add(k);
					ATA_Value.Add(tmp[k]);
					tmp[k] = 0;
					count++;
				}
				ATA.ColIndex[j + 1] = ATA.ColIndex[j] + count;
				set.Clear();
			}

			ATA.RowIndex = ATA_RowIndex.ToArray(); ATA_RowIndex = null;
			ATA.Values = ATA_Value.ToArray(); ATA_Value = null;
			return ATA;
		}
		private SparseMatrix Multiply(SparseMatrix A1, SparseMatrix A2)
		{
			SparseMatrix M = new SparseMatrix(A1.RowSize, A2.ColumnSize);
			double[] tmp = new double[A2.ColumnSize];
			Set<int> marked = new Set<int>();

			for (int i = 0; i < tmp.Length; i++)
				tmp[i] = 0;

			for (int i = 0; i < A1.RowSize; i++)
			{
				marked.Clear();

				List<SparseMatrix.Element> rr = A1.Rows[i] as List<SparseMatrix.Element>;
				foreach (SparseMatrix.Element e1 in rr)
				{
					List<SparseMatrix.Element> rr2 = A2.Rows[e1.j] as List<SparseMatrix.Element>;
					foreach (SparseMatrix.Element e2 in rr2)
					{
						tmp[e2.j] += e1.value * e2.value;
						marked.Add(e2.j);
					}
				}

				foreach (int index in marked)
					if (tmp[index] != 0)
					{
						M.AddElement(i, index, tmp[index]);
						tmp[index] = 0;
					}
			}

			M.SortElement();
			return M;
		}
		private SparseMatrix Multiply1T(SparseMatrix A1, SparseMatrix A2)
		{
			SparseMatrix M = new SparseMatrix(A1.ColumnSize, A2.ColumnSize);
			double[] tmp = new double[A2.ColumnSize];
			Set<int> marked = new Set<int>();

			for (int i = 0; i < tmp.Length; i++)
				tmp[i] = 0;

			for (int i = 0; i < A1.ColumnSize; i++)
			{
				marked.Clear();

				List<SparseMatrix.Element> rr = A1.Columns[i] as List<SparseMatrix.Element>;
				foreach (SparseMatrix.Element e1 in rr)
				{
					List<SparseMatrix.Element> rr2 = A2.Rows[e1.i] as List<SparseMatrix.Element>;
					foreach (SparseMatrix.Element e2 in rr2)
					{
						tmp[e2.j] += e1.value * e2.value;
						marked.Add(e2.j);
					}
				}

				foreach (int index in marked)
					if (tmp[index] != 0)
					{
						M.AddElement(i, index, tmp[index]);
						tmp[index] = 0;
					}
			}

			M.SortElement();
			return M;
		}
		private void* Factorization(CCSMatrix C)
		{
			fixed (int* ri = C.RowIndex, ci = C.ColIndex)
			fixed (double* val = C.Values)
				return CreaterCholeskySolver(C.ColumnSize, C.NumNonZero, ri, ci, val);
		}
		private void* SymbolicFactorization(CCSMatrix C)
		{
			fixed (int* ri = C.RowIndex, ci = C.ColIndex)
			fixed (double* val = C.Values)
				return CreaterSymbolicSolver(C.ColumnSize, C.NumNonZero, ri, ci, val);
		}
		private int NumericFactorization(void* symoblicSolver, CCSMatrix C)
		{
			fixed (int* ri = C.RowIndex, ci = C.ColIndex)
			fixed (double* val = C.Values)
				return NumericFactor(symoblicSolver, C.ColumnSize, C.NumNonZero, ri, ci, val);
		}

		// functions for simplification
		private void Simplification()
		{
			Program.PrintText("[Connectivity Surgery]");

			int n = mesh.VertexCount;
			lock (mesh.FaceIndex) this.faceIndex = (int[])mesh.FaceIndex.Clone();
			VertexRecord[] records = new VertexRecord[n];
			for (int i = 0; i < n; i++)
			{
				records[i] = new VertexRecord(mesh, i);
			}
			vRec = records;
			//RemoveFaces();

			// init weights
			for (int i = 0; i < n; i++)
			{
				VertexRecord rec1 = vRec[i];
				Vector3d p1 = rec1.pos;
				rec1.minError = double.MaxValue;
				rec1.minIndex = -1;

				if (opt.UseShapeEnergy)
					foreach (int j in rec1.adjV)
					{
						Vector3d p2 = vRec[j].pos;
						Vector3d u = (p2 - p1).Normalize();
						Vector3d w = u.Cross(p1);
						Matrix4d m = new Matrix4d();
						m[0, 1] = -u.z; m[0, 2] = u.y; m[0, 3] = -w.x;
						m[1, 0] = u.z; m[1, 2] = -u.x; m[1, 3] = -w.y;
						m[2, 0] = -u.y; m[2, 1] = u.x; m[2, 3] = -w.z;
						rec1.matrix += m.Transpose() * m;
					}
				UpdateVertexRecords(rec1);
			}

			// put record into priority queue
			PriorityQueue queue = new PriorityQueue(n);
			for (int i = 0; i < n; i++) queue.Insert(vRec[i]);

			int facesLeft = mesh.FaceCount;
			int vertexLeft = mesh.VertexCount;
			int edgeLeft = 0;
			remainingVertexCount = vertexLeft;
			foreach (VertexRecord rec in vRec)
				foreach (int i in rec.adjV)
					edgeLeft++;
			edgeLeft /= 2;

			displayIntermediateMesh = true;
			while (facesLeft > 0 && vertexLeft > opt.TargetVertexCount && !queue.IsEmpty())
			{
				lock (this)
				{
					VertexRecord rec1 = (VertexRecord)queue.DeleteMin();
					VertexRecord rec2 = vRec[rec1.minIndex];
					rec2.matrix = (rec1.matrix + rec2.matrix);
					if (rec1.center)
						rec2.pos = (rec1.pos + rec2.pos) / 2.0;
					rec2.collapseFrom.Add(rec1.vIndex);
					foreach (int index in rec1.collapseFrom) rec2.collapseFrom.Add(index);
					rec1.collapseFrom = null;
					int r1 = rec1.vIndex;
					int r2 = rec2.vIndex;
					int count = 0;

					//					if (facesLeft % 2 == 1) throw new Exception();
					//					Program.PrintText((facesLeft - edgeLeft + vertexLeft).ToString());
					// 					Program.PrintText(facesLeft + " " + vertexLeft);

					foreach (int index in rec1.adjF)
					{
						int t = index * 3;
						int c1 = faceIndex[t];
						int c2 = faceIndex[t + 1];
						int c3 = faceIndex[t + 2];

						if ((c1 == r2 || c2 == r2 || c3 == r2) ||
							(c1 == c2 || c2 == c3 || c3 == c1))
						{
							// remove adj faces
							foreach (int index2 in rec1.adjV)
								vRec[index2].adjF.Remove(index);
							// decrease face count
							facesLeft--;
							count++;
						}
						else
						{
							// update face index
							if (c1 == r1) faceIndex[t] = r2;
							if (c2 == r1) faceIndex[t + 1] = r2;
							if (c3 == r1) faceIndex[t + 2] = r2;

							// add adj faces
							rec2.adjF.Add(index);
						}
					}

					// fix adj vertices
					foreach (int index in rec1.adjV)
					{
						VertexRecord recAdj = vRec[index];
						if (recAdj.adjV.Contains(r1))
						{
							recAdj.adjV.Remove(r1);
							edgeLeft--;
						}
						if (index != r2)
						{
							recAdj.adjV.Add(r2);
							rec2.adjV.Add(index);
						}
					}

					// update records
					foreach (int index in rec2.adjV)
					{
						UpdateVertexRecords(vRec[index]);
						queue.Update(vRec[index]);
					}
					UpdateVertexRecords(rec2);
					queue.Update(rec2);

					foreach (int index in rec2.adjF)
					{
						int t = index * 3;
						int c1 = faceIndex[t];
						int c2 = faceIndex[t + 1];
						int c3 = faceIndex[t + 2];

						if (c1 == c2 || c2 == c3 || c3 == c1)
						{
							rec2.adjF.Remove(index);
							foreach (int index2 in rec2.adjV)
								vRec[index2].adjF.Remove(index);
							// decrease face count
							facesLeft--;
						}
					}

					if (count == 0) Program.PrintText("!");

					// decrease vertex count
					vertexLeft--;
					remainingVertexCount = vertexLeft;
				}

				if (opt.DisplayIntermediateMesh && facesLeft % 100 == 0)
				{
					Thread.Sleep(100);
				}

			}

			// copy remaining vertices to resulting list
			simplifiedVertexRec = new List<VertexRecord>(512);
			while (!queue.IsEmpty())
			{
				VertexRecord rec = (VertexRecord)queue.DeleteMin();
				simplifiedVertexRec.Add(rec);
			}

//			PostSimplification();

			#region post process to ensure each segment has 4 or more vertices
			/*
			bool updated = false;
			do
			{
				updated = false;
				foreach (VertexRecord rec in simplifiedVertexRec)
				{
					if (rec.collapseFrom != null &&
						rec.collapseFrom.Count < 4
						&& rec.adjV.Count == 1)
					{
						int r1 = rec.vIndex;
						int adj = -1;
						foreach (int i in rec.adjV) adj = i;
						VertexRecord rec2 = vRec[adj];
						rec2.collapseFrom.Add(rec.vIndex);
						foreach (int index in rec.collapseFrom) rec2.collapseFrom.Add(index);
						rec.collapseFrom = null;
						updated = true;

						foreach (int index in rec.adjV)
						{
							VertexRecord recAdj = vRec[index];
							if (recAdj.adjV.Contains(r1))
								recAdj.adjV.Remove(r1);
							if (index != adj)
							{
								recAdj.adjV.Add(adj);
								rec2.adjV.Add(index);
							}
						}
					}
				}
			} while (updated);
			List<VertexRecord> updatedVertexRec = new List<VertexRecord>(32);
			foreach (VertexRecord rec in simplifiedVertexRec)
				if (rec.collapseFrom != null)
					updatedVertexRec.Add(rec);
			simplifiedVertexRec = updatedVertexRec;
			*/
			#endregion

			// release set objects
			foreach (VertexRecord rec in vRec)
			{
				rec.adjF = null;
				if (rec.collapseFrom == null)
					rec.adjV = null;
			}

			displayIntermediateMesh = false;

			AssignColorIndex();

			Program.PrintText("Nodes:" + simplifiedVertexRec.Count.ToString());
		}
		private void RemoveFaces()
		{
			for (int i = 0; i < mesh.FaceCount; i++)
			{
				// get vertices of current face
				int indexBase = i * 3;
				int c1 = mesh.FaceIndex[indexBase];
				int c2 = mesh.FaceIndex[indexBase + 1];
				int c3 = mesh.FaceIndex[indexBase + 2];
				Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
				Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
				Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);

				// find longest edge u1-u2
				double d1 = (v2 - v1).Length();
				double d2 = (v3 - v2).Length();
				double d3 = (v1 - v3).Length();
				int u1 = -1;
				int u2 = -1;
				if (d1 > d2 && d1 > d3) // remove edge c1-c2
				{
					u1 = c1;
					u2 = c2;
				}
				else if (d2 > d3)
				{
					u1 = c2;
					u2 = c3;
				}
				else
				{
					u1 = c3;
					u2 = c1;
				}

				// check is there other face connecting u1 and u2
				bool found = false;
				VertexRecord r1 = vRec[u1];
				VertexRecord r2 = vRec[u2];
				foreach (int faceIndex in r1.adjF)
				{
					if (faceIndex != i)
					{
						int indexBase2 = faceIndex * 3;
						int cc1 = mesh.FaceIndex[indexBase2];
						int cc2 = mesh.FaceIndex[indexBase2 + 1];
						int cc3 = mesh.FaceIndex[indexBase2 + 2];
						if (cc1 == u2 || cc2 == u2 || cc3 == u2)
						{
							found = true;
							break;
						}
					}
				}

				// if not find, remove connection between u1 and u2
				if (!found)
				{
					vRec[u1].adjV.Remove(u2);
					vRec[u2].adjV.Remove(u1);
				}

				// remove face in adjF fields
				vRec[c1].adjF.Remove(i);
				vRec[c2].adjF.Remove(i);
				vRec[c3].adjF.Remove(i);
			}
		}
		private void UpdateVertexRecords(VertexRecord rec1)
		{
			Vector3d p1 = rec1.pos;

			rec1.minError = double.MaxValue;
			rec1.minIndex = -1;
			if (rec1.adjV.Count > 1 && rec1.adjF.Count > 0) // Do not allow collapse at the end of skeleton
			{
				double totLength = 0;
				foreach (int j in rec1.adjV)
				{
					VertexRecord rec2 = vRec[j];
					totLength += (p1 - rec2.pos).Length();
				}
				totLength /= rec1.adjV.Count;

				foreach (int j in rec1.adjV)
				{
					// 					int adjCount = 0;
					// 					foreach (int k in vRec[j].adjV)
					// 						if (rec1.adjV.Contains(k))
					// 							adjCount++;
					// 					if (adjCount != 2) continue;

					bool found = false;
					foreach (int index in rec1.adjF)
					{
						int t = index * 3;
						int c1 = faceIndex[t];
						int c2 = faceIndex[t + 1];
						int c3 = faceIndex[t + 2];

						if (c1 == j || c2 == j || c3 == j)
						{
							found = true;
						}
					}
					if (!found) continue;

					VertexRecord rec2 = vRec[j];
					Vector3d p2 = rec2.pos;
					if (rec2.adjF.Count == 0) continue;

					double err = 0;

					if (opt.UseSamplingEnergy)
					{
						err = (p1 - p2).Length() * totLength;
						//err += (p1 - p2).Length();
					}

					if (opt.UseShapeEnergy)
					{
						Vector4d v1 = new Vector4d(p2, 1.0);
						Vector4d v2 = new Vector4d((p1 + p2) / 2.0, 1.0);
						Matrix4d m = (rec1.matrix + rec2.matrix);
						double e1 = v1.Dot(m * v1) * opt.ShapeEnergyWeight;
						double e2 = v2.Dot(m * v2) * opt.ShapeEnergyWeight;
						if (e1 < e2)
							err += e1;
						else
						{
							err += e2;
							rec1.center = true;
						}
					}

					if (err < rec1.minError)
					{
						rec1.minError = err;
						rec1.minIndex = j;
					}
				}

			}
		}
		private void AssignColorIndex()
		{
			bool[] used = new bool[3]; // total 3 colors
			Queue<VertexRecord> q = new Queue<VertexRecord>();

			foreach (VertexRecord rec in simplifiedVertexRec)
				rec.colorIndex = -1;

			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				if (rec.colorIndex != -1) continue;

				q.Enqueue(rec);

				while (q.Count > 0)
				{
					VertexRecord rec1 = q.Dequeue();
					for (int i = 0; i < used.Length; i++)
						used[i] = false;
					foreach (int adj in rec1.adjV)
					{
						VertexRecord rec2 = vRec[adj];
						if (rec2.colorIndex != -1)
							used[vRec[adj].colorIndex] = true;
						else
							q.Enqueue(rec2);
					}
					for (int i = 0; i < used.Length; i++)
						if (!used[i])
							rec1.colorIndex = i;
				}
			}

			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				mesh.Flag[rec.vIndex] = (byte)(rec.colorIndex + 1);
				foreach (int index in rec.collapseFrom)
					mesh.Flag[index] = (byte)(rec.colorIndex + 1);
			}
		}

		private void PostSimplification()
		{
			bool updated = false;
			do
			{
				updated = false;
				foreach (VertexRecord rec1 in simplifiedVertexRec)
				{
					if (rec1.collapseFrom == null) continue;
					if (rec1.adjV.Count <= 2) continue; // only joint nodes allow

					foreach (int adj in rec1.adjV)
					{
						VertexRecord rec2 = this.vRec[adj];
						if (rec2.adjV.Count <= 2) continue; // find adjacent joint node

						// compute min adjacent branches length
						double minLen = double.MaxValue;
						double avgLen = 0;
						int cc = 0;
						foreach (int a1 in rec1.adjV)
							if (a1 != adj)
							{
								double len = PostSimplification_ComputeBranchLength(rec1, a1);
								avgLen += len;
								cc++;
								if (len < minLen)
									minLen = len;
							}
						foreach (int a2 in rec2.adjV)
							if (a2 != rec1.vIndex)
							{
								double len = PostSimplification_ComputeBranchLength(rec2, a2);
								avgLen += len;
								cc++;
								if (len < minLen)
									minLen = len;
							}
						avgLen /= (double)cc;

						// compute edge length
						double edgeLen = (rec2.pos - rec1.pos).Length();

						// collapse 2 joint nodes if ...
						//if (edgeLen < minLen)
						if (edgeLen < avgLen * 0.5)
						{
							int r1 = rec1.vIndex;
							rec2.pos = (rec2.pos + rec1.pos) / 2.0;
							rec2.collapseFrom.Add(r1);
							foreach (int index in rec1.collapseFrom) rec2.collapseFrom.Add(index);
							rec1.collapseFrom = null;
							updated = true;

							foreach (int index in rec1.adjV)
							{
								VertexRecord recAdj = vRec[index];
								if (recAdj.adjV.Contains(r1))
									recAdj.adjV.Remove(r1);
								if (index != adj)
								{
									recAdj.adjV.Add(adj);
									rec2.adjV.Add(index);
								}
							}
						}
						break;
					}
				}
			} while (updated);

			List<VertexRecord> updatedVertexRec = new List<VertexRecord>(32);
			foreach (VertexRecord rec in simplifiedVertexRec)
				if (rec.collapseFrom != null)
					updatedVertexRec.Add(rec);
			simplifiedVertexRec = updatedVertexRec;

		}
		private double PostSimplification_ComputeBranchLength(VertexRecord rec, int adj)
		{
			VertexRecord rec2 = vRec[adj];
			double len = (rec2.pos - rec.pos).Length();

			if (rec2.adjV.Count != 2)
				return len;
			else
			{
				foreach (int adj2 in rec2.adjV)
					if (adj2 != rec.vIndex)
						return len + PostSimplification_ComputeBranchLength(rec2, adj2);
			}
			return len;
		}

		// functions for embedding
		private void EmbeddingImproving_old_sphere_fitting()
		{
			if (opt.TwoDimensionModel) return;

			if (opt.UseBoundaryVerticesOnly)
			{
				int vn = mesh.VertexCount;
				this.adjSegmentVertex = new int[vn];
				for (int i = 0; i < vn; i++)
				{
					adjSegmentVertex[i] = 0;
					int flag = mesh.Flag[i];
					foreach (int j in mesh.AdjVV[i])
						if (mesh.Flag[j] == flag)
							adjSegmentVertex[i]++;
				}
			}

			FitNodeSphere();
			ComputeEmbeddingError();
			if (opt.PostSimplify)
			{
				MergeJoint();
				FitNodeSphere();
				AssignColorIndex();
				ComputeEmbeddingError();
			}

			this.adjSegmentVertex = null;
		}
		private void EmbeddingImproving()
		{
			Program.PrintText("[Embedding Refinement]");

			int vn = mesh.VertexCount;
			int[] segmentIndex = new int[vn];
			bool[] marked = new bool[vn];

			// init local variables
			for (int i = 0; i < vn; i++) marked[i] = false;
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				rec.collapseFrom.Add(rec.vIndex);
				foreach (int index in rec.collapseFrom)
					segmentIndex[index] = rec.vIndex;
			}

			// for each skeletal node
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				Vector3d totDis = new Vector3d();
				rec.pos = new Vector3d(originalVertexPos, rec.vIndex * 3);

				if (rec.adjV.Count == 2)
				{
					// for each adjacent node
					foreach (int adj in rec.adjV)
					{
						Vector3d dis = new Vector3d();
						Vector3d q = new Vector3d();
						Set<int> boundaryVertices = new Set<int>();
						double totLen = 0;
						foreach (int i in rec.collapseFrom)
						{
							foreach (int j in mesh.AdjVV[i])
								if (segmentIndex[j] == adj)
								{
									marked[i] = true;
									boundaryVertices.Add(i);
									break;
								}
						}

						foreach (int i in boundaryVertices)
						{
							Vector3d p1 = new Vector3d(originalVertexPos, i * 3);
							Vector3d p2 = new Vector3d(mesh.VertexPos, i * 3);
							double len = 0;
							foreach (int j in mesh.AdjVV[i])
								if (marked[j])
								{
									Vector3d u = new Vector3d(originalVertexPos, j * 3);
									len += (p1 - u).Length();
								}
							q += p2 * len;
							dis += (p1 - p2) * len;
							totLen += len;
						}
						foreach (int i in boundaryVertices) marked[i] = false;

						Vector3d center = (q + dis) / totLen;
						if (totLen > 0) totDis += center;
					}
					//rec.pos += totDis / rec.adjV.Count;
					rec.pos = totDis / rec.adjV.Count;
				}
				else
				{
					Vector3d dis = new Vector3d();
					double totLen = 0;
					foreach (int i in rec.collapseFrom)
					{
						foreach (int j in mesh.AdjVV[i])
							if (segmentIndex[j] != rec.vIndex)
							{ marked[i] = true; }
					}
					foreach (int i in rec.collapseFrom)
					{
						if (!marked[i]) continue;

						Vector3d p1 = new Vector3d(originalVertexPos, i * 3);
						Vector3d p2 = new Vector3d(mesh.VertexPos, i * 3);
						double len = 0;
						foreach (int j in mesh.AdjVV[i])
							if (marked[j])
							{
								Vector3d u = new Vector3d(originalVertexPos, j * 3);
								len += (p1 - u).Length();
							}

						//dis += (p - p2);
						dis += (p1 - rec.pos) * len;
						totLen += len;
					}
					if (totLen > 0) rec.pos += dis / totLen;
				}

				/*
								foreach (int i in rec.collapseFrom)
								{
									if (marked[i] == true) continue;
									Vector3d p = new Vector3d(originalVertexPos, i * 3);
									Vector3d p2 = new Vector3d(mesh.VertexPos, i * 3);
									dis += (p - p2);
									marked[i] = true;
									count++;
								}
				*/
			}

			if (opt.PostSimplify)
			{
				MergeJoint2();
				AssignColorIndex();
			}
		}
		private void MergeJoint2()
		{
			int vn = mesh.VertexCount;
			int[] segmentIndex = new int[vn];

			#region init segment index
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				rec.collapseFrom.Add(rec.vIndex);
				foreach (int index in rec.collapseFrom)
					segmentIndex[index] = rec.vIndex;
			}
			#endregion

			bool updated = false;
			do
			{
				foreach (VertexRecord rec in simplifiedVertexRec)
				{
					if (rec.collapseFrom == null) continue;
					if (rec.adjV.Count <= 2) continue;

					Vector3d p = rec.pos;
					updated = false;

					#region compute radius
					double radius = 0;
					foreach (int index in rec.collapseFrom)
					{
						Vector3d q = new Vector3d(originalVertexPos, index * 3);
						radius += (p - q).Length();
					}
					radius /= rec.collapseFrom.Count;
					#endregion

					#region compute sd
					double sd = 0;
					foreach (int index in rec.collapseFrom)
					{
						Vector3d q = new Vector3d(originalVertexPos, index * 3);
						double diff = (p - q).Length() - radius;
						sd += diff * diff;
					}
					sd /= rec.collapseFrom.Count;
					sd = Math.Sqrt(sd);
					sd /= radius;
					#endregion

					Vector3d minCenter = new Vector3d();
					double minSD = double.MaxValue;
					double minRadius = double.MaxValue;
					int minAdj = -1;
					foreach (int adj in rec.adjV)
					{
						VertexRecord rec2 = vRec[adj];
						if (rec2.adjV.Count == 1) continue;
						Vector3d newCenter = new Vector3d();
						double newRadius = 0;
						double newSD = 0;

						#region compute new center
						Vector3d dis = new Vector3d();
						double totLen = 0;
						Set<int> marked = new Set<int>();

						foreach (int i in rec.collapseFrom)
							foreach (int j in mesh.AdjVV[i])
								if (segmentIndex[j] != rec.vIndex && segmentIndex[j] != adj)
									marked.Add(i);
						foreach (int i in marked)
						{
							Vector3d p1 = new Vector3d(originalVertexPos, i * 3);
							Vector3d p2 = new Vector3d(mesh.VertexPos, i * 3);
							double len = 0;
							foreach (int j in mesh.AdjVV[i])
								if (marked.Contains(j))
								{
									Vector3d u = new Vector3d(originalVertexPos, j * 3);
									len += (p1 - u).Length();
								}

							//dis += (p - p2);
							dis += p1 * len;
							totLen += len;
						}

						marked.Clear();
						foreach (int i in rec2.collapseFrom)
							foreach (int j in mesh.AdjVV[i])
								if (segmentIndex[j] != rec2.vIndex && segmentIndex[j] != rec.vIndex)
									marked.Add(i);
						foreach (int i in marked)
						{
							Vector3d p1 = new Vector3d(originalVertexPos, i * 3);
							Vector3d p2 = new Vector3d(mesh.VertexPos, i * 3);
							double len = 0;
							foreach (int j in mesh.AdjVV[i])
								if (marked.Contains(j))
								{
									Vector3d u = new Vector3d(originalVertexPos, j * 3);
									len += (p1 - u).Length();
								}

							//dis += (p - p2);
							dis += p1 * len;
							totLen += len;
						}

						newCenter = dis / totLen;
						#endregion

						#region compute new radius
						foreach (int index in rec.collapseFrom)
						{
							Vector3d q = new Vector3d(originalVertexPos, index * 3);
							newRadius += (newCenter - q).Length();
						}
						foreach (int index in rec2.collapseFrom)
						{
							Vector3d q = new Vector3d(originalVertexPos, index * 3);
							newRadius += (newCenter - q).Length();
						}
						newRadius /= (rec.collapseFrom.Count + rec2.collapseFrom.Count);
						#endregion

						#region compute sd
						foreach (int index in rec.collapseFrom)
						{
							Vector3d q = new Vector3d(originalVertexPos, index * 3);
							double diff = (newCenter - q).Length() - newRadius;
							newSD += diff * diff;
						}
						foreach (int index in rec2.collapseFrom)
						{
							Vector3d q = new Vector3d(originalVertexPos, index * 3);
							double diff = (newCenter - q).Length() - newRadius;
							newSD += diff * diff;
						}
						newSD /= (rec.collapseFrom.Count + rec2.collapseFrom.Count);
						newSD = Math.Sqrt(newSD);
						newSD /= newRadius;
						#endregion

						if (newSD < minSD)
						{
							minSD = newSD;
							minRadius = newRadius;
							minCenter = newCenter;
							minAdj = adj;
						}
					}


					#region merge node if new SD is smaller
					if (minAdj != -1 && minSD < opt.PostSimplifyErrorRatio * sd)
					{
						//Program.PrintText(rec.vIndex + "=>" + minAdj);
						int r1 = rec.vIndex;
						VertexRecord rec2 = vRec[minAdj];
						rec2.pos = minCenter;
						rec2.collapseFrom.Add(rec.vIndex);
						foreach (int index in rec.collapseFrom) rec2.collapseFrom.Add(index);
						rec.collapseFrom = null;
						updated = true;

						foreach (int index in rec.adjV)
						{
							VertexRecord recAdj = vRec[index];
							if (recAdj.adjV.Contains(r1))
								recAdj.adjV.Remove(r1);
							if (index != minAdj)
							{
								recAdj.adjV.Add(minAdj);
								rec2.adjV.Add(index);
							}
						}
					}
					#endregion
				}
			} while (updated);

			List<VertexRecord> updatedVertexRec = new List<VertexRecord>(32);
			foreach (VertexRecord rec in simplifiedVertexRec)
				if (rec.collapseFrom != null)
					updatedVertexRec.Add(rec);
			this.simplifiedVertexRec = updatedVertexRec;
		}
		private void FitNodeSphere()
		{
			for (int iter = 0; iter < opt.NumOfImprovement; iter++)
				foreach (VertexRecord rec in simplifiedVertexRec)
				{
					Vector3d c = rec.pos;
					//if (iter == 0) c = new Vector3d();

					if (rec.collapseFrom.Count <= 3)
					{
						rec.pos = new Vector3d();
						foreach (int i in rec.collapseFrom)
							rec.pos += new Vector3d(originalVertexPos, i * 3);
						rec.pos /= rec.collapseFrom.Count;
						rec.radius = 0;
						continue;
					}

					double r = 0;
					foreach (int i in rec.collapseFrom)
					{
						Vector3d p = new Vector3d(originalVertexPos, i * 3);
						double len = (p - c).Length();
						r += len;
					}
					r /= rec.collapseFrom.Count;
					rec.radius = r;


					double tot_w = 0;
					Vector3d new_c = new Vector3d();
					foreach (int i in rec.collapseFrom)
					{
						if (opt.UseBoundaryVerticesOnly &&
							rec.adjV.Count >= 2 &&
							mesh.AdjVV[i].Length == this.adjSegmentVertex[i])
							continue;

						Vector3d p = new Vector3d(originalVertexPos, i * 3);
						double d = (p - c).Length();
						double w = Math.Abs(d - r);// (d - r) / d;
						if (d == 0.0 || w == 0.0) continue;
						if (d < r) continue;
						//w = 1.0;
						Vector3d target = new Vector3d();
						target = p + (c - p) * (r / d);
						tot_w += w;
						new_c += w * target;
						if (double.IsNaN(new_c.x) || double.IsNaN(new_c.y) || double.IsNaN(new_c.z))
							throw new Exception();
					}
					if (tot_w > 0)
					{
						new_c /= tot_w;
						rec.pos = new_c;// c * 0.9 + new_c * 0.1;
					}
				}
		}
		private void ComputeEmbeddingError()
		{
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				Vector3d c = rec.pos;
				double r = rec.radius;
				double err = 0;

				foreach (int i in rec.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double diff = (p - c).Length() - r;
					err += diff * diff;
				}
				err /= rec.collapseFrom.Count;
				//err = Math.Sqrt(err) / r;
				err /= r * r;
				rec.err = err;
			}
		}
		private void ComputeEmbeddingError(VertexRecord rec)
		{
			Vector3d c = rec.pos;
			double r = rec.radius;

			for (int iter = 0; iter < opt.NumOfImprovement; iter++)
			{
				r = 0;
				foreach (int i in rec.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double len = (p - c).Length();
					r += len;
				}
				r /= rec.collapseFrom.Count;

				double tot_w = 0;
				Vector3d new_c = new Vector3d();
				foreach (int i in rec.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double d = (p - c).Length();
					double w = Math.Abs(d - r);
					if (d == 0.0 || w == 0.0) continue;
					if (d < r) continue;
					Vector3d target = new Vector3d();
					target = p + (c - p) * (r / d);
					tot_w += w;
					new_c += w * target;
					if (double.IsNaN(new_c.x) || double.IsNaN(new_c.y) || double.IsNaN(new_c.z))
						throw new Exception();
				}
				if (tot_w > 0)
				{
					new_c /= tot_w;
					c = new_c;// c * 0.9 + new_c * 0.1;
				}
			}

			double err = 0;
			foreach (int i in rec.collapseFrom)
			{
				Vector3d p = new Vector3d(originalVertexPos, i * 3);
				double diff = (p - c).Length() - r;
				err += diff * diff;
			}
			err /= rec.collapseFrom.Count;
			//err = Math.Sqrt(err) / r;
			err /= r * r;
			rec.err = err;
		}
		private double ComputeEmbeddingError(VertexRecord rec, VertexRecord rec2)
		{
			Vector3d c = rec.pos;
			double r = rec.radius;

			for (int iter = 0; iter < opt.NumOfImprovement; iter++)
			{
				r = 0;
				foreach (int i in rec.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double len = (p - c).Length();
					r += len;
				}
				foreach (int i in rec2.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double len = (p - c).Length();
					r += len;
				}
				r /= rec.collapseFrom.Count + rec2.collapseFrom.Count;

				double tot_w = 0;
				Vector3d new_c = new Vector3d();
				foreach (int i in rec.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double d = (p - c).Length();
					double w = Math.Abs(d - r);
					if (d == 0.0 || w == 0.0) continue;
					if (d < r) continue;
					Vector3d target = new Vector3d();
					target = p + (c - p) * (r / d);
					tot_w += w;
					new_c += w * target;
					if (double.IsNaN(new_c.x) || double.IsNaN(new_c.y) || double.IsNaN(new_c.z))
						throw new Exception();
				}
				foreach (int i in rec2.collapseFrom)
				{
					Vector3d p = new Vector3d(originalVertexPos, i * 3);
					double d = (p - c).Length();
					double w = Math.Abs(d - r);
					if (d == 0.0 || w == 0.0) continue;
					if (d < r) continue;
					Vector3d target = new Vector3d();
					target = p + (c - p) * (r / d);
					tot_w += w;
					new_c += w * target;
					if (double.IsNaN(new_c.x) || double.IsNaN(new_c.y) || double.IsNaN(new_c.z))
						throw new Exception();
				}
				if (tot_w > 0)
				{
					new_c /= tot_w;
					c = new_c;// c * 0.9 + new_c * 0.1;
				}
			}

			double err = 0;

			foreach (int i in rec.collapseFrom)
			{
				Vector3d p = new Vector3d(originalVertexPos, i * 3);
				double diff = (p - c).Length() - r;
				err += diff * diff;
			}

			foreach (int i in rec2.collapseFrom)
			{
				Vector3d p = new Vector3d(originalVertexPos, i * 3);
				double diff = (p - c).Length() - r;
				err += diff * diff;
			}
			err /= rec.collapseFrom.Count + rec2.collapseFrom.Count;
			//			err /= rec2.collapseFrom.Count;
			//			err = Math.Sqrt(err) / r;
			err /= r * r;
			return err;
		}
		private void MergeJoint()
		{
			foreach (VertexRecord rec in simplifiedVertexRec)
				ComputeMergeJointCost(rec);

			PriorityQueue queue = new PriorityQueue(simplifiedVertexRec.Count);
			foreach (VertexRecord rec in simplifiedVertexRec)
				queue.Insert(rec);

			while (!queue.IsEmpty())
			{
				VertexRecord rec = queue.GetMin() as VertexRecord;

				if (rec.minIndex == -1 || rec.minError > opt.PostSimplifyErrorRatio) break;

				queue.DeleteMin();

				VertexRecord rec2 = vRec[rec.minIndex];

				Program.PrintText(rec.vIndex.ToString() + " ==> " + rec2.vIndex.ToString() + " : " + rec.minError);

				rec2.collapseFrom.Add(rec.vIndex);
				foreach (int v in rec.collapseFrom)
					rec2.collapseFrom.Add(v);

				foreach (int index in rec.adjV)
				{
					VertexRecord adjRec = vRec[index];
					adjRec.adjV.Remove(rec.vIndex);

					if (index != rec2.vIndex)
					{
						rec2.adjV.Add(index);
						vRec[index].adjV.Add(rec2.vIndex);
					}
				}

				ComputeEmbeddingError(rec2);
				ComputeMergeJointCost(rec2);
				queue.Update(rec2);
				foreach (int index in rec2.adjV)
				{
					ComputeMergeJointCost(vRec[index]);
					queue.Update(vRec[index]);
				}
			}

			List<VertexRecord> resultList = new List<VertexRecord>(simplifiedVertexRec.Count);
			while (!queue.IsEmpty())
				resultList.Add((VertexRecord)queue.DeleteMin());
			simplifiedVertexRec = resultList;
		}
		private void ComputeMergeJointCost(VertexRecord rec)
		{
			rec.minError = double.MaxValue;
			rec.minIndex = -1; ;

			if (rec.adjV.Count < 3) return;

			foreach (int index in rec.adjV)
			{
				VertexRecord rec2 = vRec[index];
				double new_err = ComputeEmbeddingError(rec2, rec);
				double minError = new_err / rec.err;
				if (minError < rec.minError)
				{
					rec.minError = minError;
					rec.minIndex = rec2.vIndex;
				}
			}
			//Program.PrintText(rec.vIndex.ToString() + " cost: " + rec.minIndex + " " + rec.minError);
		}

		// other helping functions
		private void DrawSimplifiedVertices()
		{
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				// 				switch (rec.colorIndex)
				// 				{
				// 					case 0: GL.glColor3f(0.0f, 1.0f, 0.0f); break;
				// 					case 1: GL.glColor3f(1.0f, 0.0f, 0.0f); break;
				// 					case 2: GL.glColor3f(0.0f, 0.0f, 1.0f); break;
				// 				}
				GL.glPointSize(skeletonNodeSize);
				GL.glColor3f(1.0f, 0.0f, 0.0f);
				GL.glBegin(GL.GL_POINTS);
				GL.glVertex3d(rec.pos.x, rec.pos.y, rec.pos.z);
				GL.glEnd();
			}
			/*
						GL.glColor3f(0.0f, 0.8f, 0.0f);
						foreach (VertexRecord rec in simplifiedVertexRec)
						{
							if (rec.adjV.Count > 2)
								Program.Print3DText(rec.pos, "  " + rec.nodeSize.ToString());
						}
			*/
			GL.glColor3f(0.0f, 0.0f, 1.0f);
			GL.glLineWidth(2.0f);
			GL.glBegin(GL.GL_LINES);
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				foreach (int adj in rec.adjV)
				{
					VertexRecord rec2 = vRec[adj];
					GL.glVertex3d(rec.pos.x, rec.pos.y, rec.pos.z);
					GL.glVertex3d(rec2.pos.x, rec2.pos.y, rec2.pos.z);
				}
			}
			GL.glEnd();
		}
		private void DrawNodeSphere()
		{
			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);

			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
			GL.glLineWidth(0.5f);
			GLUquadric quad = GLU.gluNewQuadric();
			foreach (VertexRecord rec in simplifiedVertexRec)
			{
				GL.glColor3f(0.3f + (float)rec.err, 0.3f, 0.3f);
				GL.glTranslated(rec.pos.x, rec.pos.y, rec.pos.z);
				GLU.gluSphere(quad, rec.radius, 8, 8);
				GL.glTranslated(-rec.pos.x, -rec.pos.y, -rec.pos.z);
			}
			GLU.gluDeleteQuadric(quad);

			GL.glDisable(GL.GL_LIGHTING);
		}
		private void DrawRootNode()
		{

		}
		private void AddNoise()
		{
			Random r = new Random(mesh.VertexCount);

			for (int i = 0, j = 0; i < mesh.VertexCount; i++, j += 3)
			{
				Vector3d v = new Vector3d(mesh.VertexNormal, j) * opt.NoiseRatio;
				mesh.VertexPos[j] += (r.NextDouble() - 0.5) * v.x;
				mesh.VertexPos[j + 1] += (r.NextDouble() - 0.5) * v.y;
				mesh.VertexPos[j + 2] += (r.NextDouble() - 0.5) * v.z;
			}
			mesh.ComputeFaceNormal();
			mesh.ComputeVertexNormal();
		}
		private void DrawIntermediateMesh()
		{
			{
				foreach (VertexRecord rec in this.vRec)
				{
					if (rec.collapseFrom == null) continue;
					GL.glPointSize(4.0f);
					GL.glColor3f(1.0f, 0.0f, 0.0f);
					GL.glBegin(GL.GL_POINTS);
					GL.glVertex3d(rec.pos.x, rec.pos.y, rec.pos.z);
					GL.glEnd();
				}
				GL.glColor3f(0.0f, 0.0f, 1.0f);
				GL.glLineWidth(1.0f);
				GL.glBegin(GL.GL_LINES);
				foreach (VertexRecord rec in this.vRec)
				{
					if (rec.collapseFrom == null) continue;
					foreach (int adj in rec.adjV)
					{
						VertexRecord rec2 = vRec[adj];
						GL.glVertex3d(rec.pos.x, rec.pos.y, rec.pos.z);
						GL.glVertex3d(rec2.pos.x, rec2.pos.y, rec2.pos.z);
					}
				}
				GL.glEnd();
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (solver != null)
			{
				FreeSolver(solver);
				solver = null;
			}
			if (symbolicSolver != null)
			{
				FreeSymbolicSolver(symbolicSolver);
				symbolicSolver = null;
			}
		}

		#endregion

		public override string ToString()
		{
			return "";
		}
	}
}
