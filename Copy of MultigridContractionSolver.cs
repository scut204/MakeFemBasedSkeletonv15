using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MyGeometry;

namespace IsolineEditing
{
	public unsafe class MultigridContractionSolver
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
        #endregion

		private class ColMatrixCreator
		{
			public int m, n;
			public List<int>[] rowIndex;
			public List<double>[] values;

			public int RowSize
			{
				get { return m; }
			}
			public int ColumnSize
			{
				get { return n; }
			}

			public ColMatrixCreator(int m, int n)
			{
				this.m = m;
				this.n = n;
				this.rowIndex = new List<int>[n];
				this.values = new List<double>[n];
				for (int i = 0; i < n; i++)
				{
					rowIndex[i] = new List<int>();
					values[i] = new List<double>();
				}
			}
			public void AddValueTo(int i, int j, double value)
			{
				List<int> r = rowIndex[j];
				if (i >= this.m) this.m = i + 1;

				int ri = -1;
				for (int k = 0; k < r.Count; k++)
					if (r[k] == i)
					{
						ri = k;
						break;
					}
				if (ri == -1)
				{
					r.Add(i);
					values[j].Add(value);
				}
				else
				{
					values[j][ri] += value;
				}
			}
			public ColMatrix ToColMatrix()
			{
				ColMatrix C = new ColMatrix(this.m, this.n);

				for (int i = 0; i < n; i++)
				{
					try
					{ C.rowIndex[i] = this.rowIndex[i].ToArray(); }
					catch (OutOfMemoryException)
					{ GC.Collect(); C.rowIndex[i] = this.rowIndex[i].ToArray(); }
					this.rowIndex[i] = null;

					try
					{ C.values[i] = this.values[i].ToArray(); }
					catch (OutOfMemoryException)
					{ GC.Collect(); C.values[i] = this.values[i].ToArray(); }
					this.values[i] = null;
				}

				return C;
			}
		}
		private class ColMatrix
		{
			public int m, n;
			public int[][] rowIndex;
			public double[][] values;

			public int RowSize
			{
				get { return m; }
			}
			public int ColumnSize
			{
				get { return n; }
			}

			public ColMatrix(int m, int n)
			{
				this.m = m;
				this.n = n;
				this.rowIndex = new int[n][];
				this.values = new double[n][];
				for (int i = 0; i < n; i++)
				{
					rowIndex[i] = null;
					values[i] = null;
				}
			}
			public int NumOfElements()
			{
				int c = 0;
				foreach (int[] r in rowIndex)
					c += r.Length;
				return c;
			}
			public void Multiply(double[] xIn, double[] xOut)
			{
				if (xIn.Length < n || xOut.Length < m) throw new ArgumentException();

				for (int i = 0; i < m; i++) xOut[i] = 0;
				for (int i = 0; i < n; i++)
				{
					int[] r = rowIndex[i];
					double[] v = values[i];
					for (int j = 0; j < r.Length; j++)
					{
						int ri = r[j];
						xOut[ri] += v[j] * xIn[i];
					}
				}
			}
			public void PreMultiply(double[] xIn, double[] xOut)
			{
				if (xIn.Length < m || xOut.Length < n) throw new ArgumentException();

				for (int i = 0; i < n; i++) xOut[i] = 0;

				for (int i = 0; i < n; i++)
				{
					double sum = 0.0;
					int[] r = rowIndex[i];
					double[] v = values[i];
					for (int j = 0; j < r.Length; j++)
					{
						int ri = r[j];
						sum += v[j] * xIn[ri];
					}
					xOut[i] = sum;
				}
			}
			public void PreMultiply(double[] xIn, double[] xOut, int[] index)
			{
				if (xIn.Length < m || xOut.Length < n) throw new ArgumentException();

				for (int i = 0; i < n; i++) xOut[i] = 0;

				foreach (int i in index)
				{
					double sum = 0.0;
					int[] r = rowIndex[i];
					double[] v = values[i];
					for (int j = 0; j < r.Length; j++)
					{
						int ri = r[j];
						sum += v[j] * xIn[ri];
					}
					xOut[i] = sum;
				}
			}
			public void PreMultiplyOffset(double[] xIn, double[] xOut, int startOut, int offsetOut)
			{
				for (int i = startOut; i < n + offsetOut; i += offsetOut)
					xOut[i] = 0;

				for (int i = 0, k = startOut; i < n; i++, k += offsetOut)
				{
					double sum = 0.0;
					int[] r = rowIndex[i];
					double[] v = values[i];
					for (int j = 0; j < r.Length; j++)
					{
						int ri = r[j];
						sum += v[j] * xIn[ri];
					}
					xOut[k] = sum;
				}
			}
			public ColMatrix Transpose()
			{
				ColMatrixCreator C = new ColMatrixCreator(this.n, this.m);

				for (int i = 0; i < n; i++)
				{
					int[] r = rowIndex[i];
					double[] v = values[i];
					for (int j = 0; j < r.Length; j++)
					{
						C.rowIndex[r[j]].Add(i);
						C.values[r[j]].Add(v[j]);
					}
				}

				return C;
			}

			public static implicit operator ColMatrix(ColMatrixCreator CC)
			{
				return CC.ToColMatrix();
			}

			public int ATACG(double[] x, double[] b, double eps, int maxIter)
			{
				double[] inv = new double[n];
				double[] r = new double[n];
				double[] d = new double[n];
				double[] q = new double[n];
				double[] s = new double[n];
				double errNew = 0;
				double err = 0;
				double errOld = 0;
				double[] tmp = new double[m];

				for (int i = 0; i < n; i++) inv[i] = 0;

				for (int i = 0; i < n; i++)
				{
					for (int j = 0; j < rowIndex[i].Length; j++)
					{
						double val = values[i][j];
						inv[i] += val * val;
					}
				}
				for (int i = 0; i < n; i++) inv[i] = 1.0 / inv[i];

				int iter = 0;
				//PreMultiply(x, r);
				Multiply(x, tmp); PreMultiply(tmp, r);
				for (int i = 0; i < n; i++) r[i] = b[i] - r[i];
				for (int i = 0; i < n; i++) d[i] = inv[i] * r[i];
				for (int i = 0; i < n; i++) errNew += r[i] * d[i];
				err = errNew;

				Program.PrintText("err: " + err.ToString());

				while (iter < maxIter && errNew > eps * err)
				{
					//PreMultiply(d, q);
					Multiply(d, tmp); PreMultiply(tmp, q);
					double alpha = 0;
					for (int i = 0; i < n; i++) alpha += d[i] * q[i];
					alpha = errNew / alpha;

					for (int i = 0; i < n; i++) x[i] += alpha * d[i];

					if (iter % 50 == 0)
					{
						//PreMultiply(x, r);
						Multiply(x, tmp); PreMultiply(tmp, r);
						for (int i = 0; i < n; i++) r[i] = b[i] - r[i];
					}
					else
					{
						for (int i = 0; i < n; i++) r[i] -= alpha * q[i];
					}

					for (int i = 0; i < n; i++) s[i] = inv[i] * r[i];

					errOld = errNew;
					errNew = 0;
					for (int i = 0; i < n; i++) errNew += r[i] * s[i];
					double beta = errNew / errOld;
					for (int i = 0; i < n; i++) d[i] = s[i] + beta * d[i];
					iter++;
				}
				return iter;
			}
		};
		public class EdgeRecord : PriorityQueueElement
		{
			public int pqIndex = -1;
			public int vIndex = -1;
			public int minIndex = -1;
			public double minError = double.MaxValue;
			public double area = 0;
			public Set<int> adjV = new Set<int>(6);

			public EdgeRecord(int vIndex)
			{
				this.vIndex = vIndex;
			}
			public int PQIndex
			{
				get { return pqIndex; }
				set { pqIndex = value; }
			}
			public int CompareTo(object obj)
			{
				EdgeRecord rec = obj as EdgeRecord;
				if (minError < rec.minError) return -1;
				if (minError > rec.minError) return 1;
				return 0;
			}
		};
		public class VertexRecord
		{
			public int index = -1;
			public Set<int> adjV = new Set<int>(8);
			public VertexRecord(int index)
			{
				this.index = index;
			}
		};
		
		private Mesh mesh = null;
		private double simplificationRatio = 0.25;
		private int targetVertices = 2000;
		private double convergeRatio = 1e-4;
		private List<List<VertexRecord>> simplifiedMeshes = new List<List<VertexRecord>>();
		private List<List<EdgeRecord>> collapsedRecords = new List<List<EdgeRecord>>();
		private List<int[]> faceRecords = new List<int[]>();
		private double[] originalArea = null;
		private double[] currentArea = null;

		public int TotalLevel
		{
			get { return simplifiedMeshes.Count; }
		}
		public List<List<VertexRecord>> SimplifiedMeshes
		{
			get { return simplifiedMeshes; }
		}
		public List<int[]> FaceRecords
		{
			get { return faceRecords;  }
		}

		public MultigridContractionSolver(Mesh mesh, double ratio, int target)
		{
			Program.PrintText("[Create Multigrid Solver]");
			this.mesh = mesh;
			//this.simplificationRatio = ratio;
			//this.targetVertices = target;

			Simplify();
		}

		public double[][] SolveSystem(double[] lapWeight, double[] posWeight)
		{
			int vn = mesh.VertexCount;
			int fn = mesh.FaceCount;
			double[][] pos = new double[3][];
			pos[0] = new double[vn];
			pos[1] = new double[vn];
			pos[2] = new double[vn];
// 			for (int i = 0; i < simplifiedMeshes[0].Count; i++)
// 			{
// 				int j = simplifiedMeshes[0][i].index;
// 				pos[0][j] = mesh.VertexPos[j * 3];
// 				pos[1][j] = mesh.VertexPos[j * 3 + 1];
// 				pos[2][j] = mesh.VertexPos[j * 3 + 2];
// 			}
			for (int i = 0; i < vn; i++)
			{
				int j = i;
				pos[0][j] = mesh.VertexPos[j * 3];
				pos[1][j] = mesh.VertexPos[j * 3 + 1];
				pos[2][j] = mesh.VertexPos[j * 3 + 2];
			}

			this.originalArea = new double[vn];
			for (int i = 0; i < vn; i++) originalArea[i] = 0;
			for (int i=0,j=0; i<fn; i++,j+=3)
			{
				int c1 = mesh.FaceIndex[j];
				int c2 = mesh.FaceIndex[j+1];
				int c3 = mesh.FaceIndex[j+2];
				double area = mesh.ComputeFaceArea(i);
				originalArea[c1] += area;
				originalArea[c2] += area;
				originalArea[c3] += area;
			}

			double weightAdjustment = Math.Pow((simplificationRatio), simplifiedMeshes.Count - 1);
			weightAdjustment = 1.0;
			for (int level = 0; level < simplifiedMeshes.Count; level++)
			{
				ColMatrix colA = BuildMatrixA(simplifiedMeshes[level], faceRecords[level], lapWeight, posWeight, weightAdjustment);
				CCSMatrix ccsATA = MultiplyATA(colA);
				//CCSMatrix ccsA = new CCSMatrix(A);
				//RowBasedMatrix rbA = new RowBasedMatrix(A); A = null;
				List<VertexRecord> records = simplifiedMeshes[level];
				int n = records.Count;
				double[] x = new double[n];
				double[] b = new double[n * 2];
				double[] ATb = new double[n];
				double[] inv = null;
				void* solver = null;
				string s = "";
				s += weightAdjustment.ToString();
				//weightAdjustment /= (simplificationRatio);

				if (level == 0)
				{
					solver = Factorization(ccsATA);
					if (solver == null) throw new Exception();
				}

				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < n; j++)
					{
						b[j] = 0;
						int k = records[j].index;
						b[j + n] = mesh.VertexPos[k * 3 + i] * posWeight[k] * (currentArea[j] / originalArea[k]);
						//b[j + n] = mesh.VertexPos[k * 3 + i] * posWeight[k];
						//x[j] = mesh.VertexPos[k * 3 + i];
						x[j] = pos[i][k];
					}
					colA.PreMultiply(b, ATb);

					if (level == 0)
					{
						fixed (double* _x = x, _ATb = ATb)
							Solve(solver, _x, _ATb);
					}
					else
					{
						int iter = colA.ATACG(x, ATb, convergeRatio, n);
						s += " " + iter;
					}

					//if (level == 0)
					for (int j = 0; j < n; j++)
					{
						int k = records[j].index;
						pos[i][k] = x[j];
						//Program.PrintText(x[j].ToString());
					}
				}

				if (solver != null)
				{
					FreeSolver(solver);
					solver = null;
				}

				if (level < simplifiedMeshes.Count - 1)
				{
					//Program.PrintText(collapsedRecords[level].Count.ToString());
					foreach (EdgeRecord rec in collapsedRecords[level])
					{
						Vector3d p = new Vector3d();
						foreach (int j in rec.adjV)
						{
							p.x += pos[0][j];
							p.y += pos[1][j];
							p.z += pos[2][j];
						}
						pos[0][rec.vIndex] = p.x /= rec.adjV.Count;
						pos[1][rec.vIndex] = p.y /= rec.adjV.Count;
						pos[2][rec.vIndex] = p.z /= rec.adjV.Count;
					}
				}
				Program.PrintText(s);
			}
			return pos;
		}
		private SparseMatrix BuildMatrixA_old(List<VertexRecord> records, int[] faceIndex, double[] lapWeight, double[] posWeight)
		{
			// build backward map
			int[] map = new int[mesh.VertexCount];
			for (int i = 0; i < records.Count; i++)
				map[records[i].index] = i;

			int n = records.Count;
			int fn = faceIndex.Length / 3;
			SparseMatrix A = new SparseMatrix(2 * n, n);

			for (int i = 0, j = 0; i < fn; i++, j += 3)
			{
				int c1 = faceIndex[j];
				int c2 = faceIndex[j + 1];
				int c3 = faceIndex[j + 2];
				Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
				Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
				Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
				double cot1 = (v2 - v1).Dot(v3 - v1) / (v2 - v1).Cross(v3 - v1).Length();
				double cot2 = (v3 - v2).Dot(v1 - v2) / (v3 - v2).Cross(v1 - v2).Length();
				double cot3 = (v1 - v3).Dot(v2 - v3) / (v1 - v3).Cross(v2 - v3).Length();
				//cot1 = cot2 = cot3 = 1.0;
				if (double.IsNaN(cot1)) throw new Exception();
				if (double.IsNaN(cot2)) throw new Exception();
				if (double.IsNaN(cot3)) throw new Exception();

				c1 = map[c1];
				c2 = map[c2];
				c3 = map[c3];
				A.AddValueTo(c2, c2, -cot1); A.AddValueTo(c2, c3, cot1);
				A.AddValueTo(c3, c3, -cot1); A.AddValueTo(c3, c2, cot1);
				A.AddValueTo(c3, c3, -cot2); A.AddValueTo(c3, c1, cot2);
				A.AddValueTo(c1, c1, -cot2); A.AddValueTo(c1, c3, cot2);
				A.AddValueTo(c1, c1, -cot3); A.AddValueTo(c1, c2, cot3);
				A.AddValueTo(c2, c2, -cot3); A.AddValueTo(c2, c1, cot3);
			}
			for (int i = 0; i < n; i++)
			{
				double tot = 0;
				foreach (SparseMatrix.Element e in A.Rows[i])
					if (e.i != e.j) tot += e.value;
				if (tot > 10000)
				{
					foreach (SparseMatrix.Element e in A.Rows[i])
					{
						e.value /= (tot / 10000);
					}
				}
				foreach (SparseMatrix.Element e in A.Rows[i])
					e.value *= lapWeight[records[i].index];
			}

			// positional constraints
			for (int i = 0; i < n; i++)
			{
				A.AddElement(i + n, i, posWeight[records[i].index]);
			}

			A.SortElement();
			return A;
		}
		private ColMatrix BuildMatrixA(List<VertexRecord> records, int[] faceIndex, double[] lapWeight, double[] posWeight, double weightAdjustment)
		{
			// build backward map
			int[] map = new int[mesh.VertexCount];
			for (int i = 0; i < records.Count; i++)
				map[records[i].index] = i;

			int n = records.Count;
			int fn = faceIndex.Length / 3;
			SparseMatrix A = new SparseMatrix(2 * n, n);

			this.currentArea = new double[n];
			for (int i = 0; i < n; i++) currentArea[i] = 0;

			for (int i = 0, j = 0; i < fn; i++, j += 3)
			{
				int c1 = faceIndex[j];
				int c2 = faceIndex[j + 1];
				int c3 = faceIndex[j + 2];
				Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
				Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
				Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
				double cot1 = (v2 - v1).Dot(v3 - v1) / (v2 - v1).Cross(v3 - v1).Length();
				double cot2 = (v3 - v2).Dot(v1 - v2) / (v3 - v2).Cross(v1 - v2).Length();
				double cot3 = (v1 - v3).Dot(v2 - v3) / (v1 - v3).Cross(v2 - v3).Length();
				//cot1 = cot2 = cot3 = 1.0;
				if (double.IsNaN(cot1)) throw new Exception();
				if (double.IsNaN(cot2)) throw new Exception();
				if (double.IsNaN(cot3)) throw new Exception();

				c1 = map[c1];
				c2 = map[c2];
				c3 = map[c3];
				A.AddValueTo(c2, c2, -cot1); A.AddValueTo(c2, c3, cot1);
				A.AddValueTo(c3, c3, -cot1); A.AddValueTo(c3, c2, cot1);
				A.AddValueTo(c3, c3, -cot2); A.AddValueTo(c3, c1, cot2);
				A.AddValueTo(c1, c1, -cot2); A.AddValueTo(c1, c3, cot2);
				A.AddValueTo(c1, c1, -cot3); A.AddValueTo(c1, c2, cot3);
				A.AddValueTo(c2, c2, -cot3); A.AddValueTo(c2, c1, cot3);

				double area = ((v2 - v1).Cross(v3 - v1)).Length() / 2.0;
				currentArea[c1] += area;
				currentArea[c2] += area;
				currentArea[c3] += area;
			}
			for (int i = 0; i < n; i++)
			{
				double tot = 0;
				foreach (SparseMatrix.Element e in A.Rows[i])
					if (e.i != e.j) tot += e.value;
				if (tot > 10000)
				{
					foreach (SparseMatrix.Element e in A.Rows[i])
					{
						e.value /= (tot / 10000);
					}
				}
				foreach (SparseMatrix.Element e in A.Rows[i])
					e.value *= lapWeight[records[i].index] * weightAdjustment;
			}

			// positional constraints
			for (int i = 0; i < n; i++)
			{
				int j = records[i].index;
				double weight = posWeight[j] * (currentArea[i] / originalArea[j]);
				if (weight <= 0) weight = posWeight[i];
				A.AddValueTo(i + n, i, weight);
			}

			A.SortElement();
			ColMatrixCreator cmA = new ColMatrixCreator(2 * n, n);
			foreach (List<SparseMatrix.Element> r in A.Rows)
				foreach (SparseMatrix.Element e in r)
					cmA.AddValueTo(e.i, e.j, e.value);

			return cmA;
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
		private CCSMatrix MultiplyATA(ColMatrix A)
		{
			int[] count = new int[A.RowSize];
			for (int i = 0; i < count.Length; i++) count[i] = 0;
			foreach (int[] r in A.rowIndex)
				foreach (int ri in r) count[ri]++;

			int[][] colIndex = new int[A.RowSize][];
			int[][] listIndex = new int[A.RowSize][];
			for (int i = 0; i < A.RowSize; i++)
			{
				colIndex[i] = new int[count[i]];
				listIndex[i] = new int[count[i]];
			}
			for (int i = 0; i < count.Length; i++) count[i] = 0;

			for (int i = 0; i < A.values.Length; i++)
			{
				int[] row = A.rowIndex[i];
				for (int j = 0; j < row.Length; j++)
				{
					int r = row[j];
					int c = count[r];
					colIndex[r][c] = i;
					listIndex[r][c] = j;
					count[r]++;
				}
			}
			count = null;

			CCSMatrix ATA = new CCSMatrix(A.ColumnSize, A.ColumnSize);
			Set<int> set = new Set<int>();
			double[] tmp = new double[A.ColumnSize];
			List<int> ATA_RowIndex = new List<int>();
			List<double> ATA_Value = new List<double>();

			for (int i = 0; i < A.ColumnSize; i++) tmp[i] = 0;

			for (int j = 0; j < A.ColumnSize; j++)
			{
				for (int ri = 0; ri < A.rowIndex[j].Length; ri++)
				{
					int k = A.rowIndex[j][ri];
					double val = A.values[j][ri];

					for (int k2 = 0; k2 < colIndex[k].Length; k2++)
					{
						int i = colIndex[k][k2];
						if (i < j) continue;
						set.Add(i);
						tmp[i] += val * A.values[i][listIndex[k][k2]];
					}
				}

				int[] s = set.ToArray(); Array.Sort(s);
				int cc = 0;
				foreach (int k in s)
				{
					if (tmp[k] == 0) continue;
					ATA_RowIndex.Add(k);
					ATA_Value.Add(tmp[k]);
					tmp[k] = 0;
					cc++;
				}
				ATA.ColIndex[j + 1] = ATA.ColIndex[j] + cc;
				set.Clear();
			}

			ATA.RowIndex = ATA_RowIndex.ToArray(); ATA_RowIndex = null;
			ATA.Values = ATA_Value.ToArray(); ATA_Value = null;
			return ATA;
		}
		private void* Factorization(CCSMatrix C)
		{
			fixed (int* ri = C.RowIndex, ci = C.ColIndex)
			fixed (double* val = C.Values)
				return CreaterCholeskySolver(C.ColumnSize, C.NumNonZero, ri, ci, val);
		}
		private void Solve_by_Factorization()
		{

		}

		private void Simplify()
		{
			Program.PrintText("Simplification");
			int vn = mesh.VertexCount;
// 			if (vn <= targetVertices) return;

			int nextLevel = (int)(vn * simplificationRatio);
			if (nextLevel < targetVertices)
				nextLevel = targetVertices;

			// copy connectivity
			// VertexRecord[] vRec = new VertexRecord[vn];

			EdgeRecord[] edgeRec = CreateEdgeCollapseRecords();
			List<EdgeRecord> collapseRec = new List<EdgeRecord>();
			bool[] collapsed = new bool[vn];
			PriorityQueue queue = new PriorityQueue(vn);
			for (int i = 0; i < vn; i++)
			{
				queue.Insert(edgeRec[i]);
				collapsed[i] = false;
			}

			{
				Program.PrintText("output mesh level: " + vn);
				List<VertexRecord> vRecList = new List<VertexRecord>();
				for (int j = 0; j < vn; j++)
				{
					if (collapsed[j]) continue;
					VertexRecord r = new VertexRecord(j);
					foreach (int adj in edgeRec[j].adjV)
						r.adjV.Add(adj);

					vRecList.Add(r);
				}
				simplifiedMeshes.Add(vRecList);
				collapseRec.Reverse();
				collapsedRecords.Add(collapseRec);
				faceRecords.Add((int[])mesh.FaceIndex.Clone());
				collapseRec = new List<EdgeRecord>();
			}

			int count = vn;
			for (int i = 0; i < vn - targetVertices; i++)
			{
				EdgeRecord rec1 = (EdgeRecord)queue.DeleteMin();
				EdgeRecord rec2 = edgeRec[rec1.minIndex];
				rec2.area += rec1.area;
				collapseRec.Add(rec1);
				collapsed[rec1.vIndex] = true;
				count--;

				foreach (int j in rec1.adjV)
				{
					edgeRec[j].adjV.Remove(rec1.vIndex);
					if (j != rec2.vIndex)
					{
						edgeRec[j].adjV.Add(rec2.vIndex);
						edgeRec[rec2.vIndex].adjV.Add(j);
					}
				}

				foreach (int j in rec2.adjV)
					UpdateEdgeCollapseRecords(queue, edgeRec, j);
				UpdateEdgeCollapseRecords(queue, edgeRec, rec2.vIndex);

				if (count == nextLevel)
				{
					Program.PrintText("output mesh level: " + count);
					List<VertexRecord> vRecList = new List<VertexRecord>();
					for (int j=0; j<vn; j++)
					{
						if (collapsed[j]) continue;
						VertexRecord r = new VertexRecord(j);
						foreach (int adj in edgeRec[j].adjV)
							r.adjV.Add(adj);

						vRecList.Add(r);
					}

					simplifiedMeshes.Add(vRecList);
					int[] fr = BuildCollapsedFaceIndex(faceRecords[faceRecords.Count - 1], collapseRec);
					faceRecords.Add(fr);
					collapseRec.Reverse();
					collapsedRecords.Add(collapseRec);
					collapseRec = new List<EdgeRecord>();

					nextLevel = (int)(nextLevel * simplificationRatio);
					if (nextLevel < targetVertices)
						nextLevel = targetVertices;
				}
			}

			simplifiedMeshes.Reverse();
			collapsedRecords.Reverse();
			faceRecords.Reverse();
		}
		private EdgeRecord[] CreateEdgeCollapseRecords()
		{
			int vn = mesh.VertexCount;
			int fn = mesh.FaceCount;

			EdgeRecord[] rec = new EdgeRecord[vn];
			for (int i = 0; i < vn; i++) rec[i] = new EdgeRecord(i);

			double[] K = new double[10];
			for (int i = 0, j = 0; i < fn; i++, j += 3)
			{
				int c1 = mesh.FaceIndex[j];
				int c2 = mesh.FaceIndex[j + 1];
				int c3 = mesh.FaceIndex[j + 2];
				Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
				Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
				Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
				double faceArea = ((v2 - v1).Cross(v3 - v1)).Length() / 2.0;
				rec[c1].area += faceArea;
				rec[c2].area += faceArea;
				rec[c3].area += faceArea;
			}

			for (int i = 0, j = 0; i < vn; i++, j += 3)
			{
				Vector3d u = new Vector3d(mesh.VertexPos, j);
				EdgeRecord uRec = rec[i];
				foreach (int k in mesh.AdjVV[i])
				{
					Vector3d uv = u - new Vector3d(mesh.VertexPos, k * 3);
					double err = uRec.area * uv.Dot(uv);
					if (err < uRec.minError)
					{
						uRec.minError = err;
						uRec.minIndex = k;
					}
					uRec.adjV.Add(k);
				}
			}
			return rec;
		}
		private void UpdateEdgeCollapseRecords(PriorityQueue queue, EdgeRecord[] edgeRec, int index)
		{
			Vector3d u = new Vector3d(mesh.VertexPos, index * 3);
			EdgeRecord uRec = edgeRec[index];

			uRec.minError = double.MaxValue;
			foreach (int j in uRec.adjV)
			{
				int adjCount = 0;
				foreach (int k in edgeRec[j].adjV)
					if (uRec.adjV.Contains(k))
						adjCount++;
				if (adjCount != 2) continue;

				Vector3d uv = u - new Vector3d(mesh.VertexPos, j * 3);
				double err = uRec.area * uv.Dot(uv);
				if (err < uRec.minError)
				{
					uRec.minError = err;
					uRec.minIndex = j;
				}
			}
			queue.Update(uRec);
		}
		private int[] BuildCollapsedFaceIndex(int[] oldIndex, List<EdgeRecord> collapsedRecords)
		{
			List<int> newIndex = new List<int>();
			int[] collapseIndex = BuildCollapseIndex(collapsedRecords);

			int fn = oldIndex.Length / 3;

			for (int i = 0, j = 0; i < fn; i++, j += 3)
			{
				int f1 = oldIndex[j];
				int f2 = oldIndex[j + 1];
				int f3 = oldIndex[j + 2];
				if (collapseIndex[f1] != -1) f1 = collapseIndex[f1];
				if (collapseIndex[f2] != -1) f2 = collapseIndex[f2];
				if (collapseIndex[f3] != -1) f3 = collapseIndex[f3];
				if (f1 == f2 || f2 == f3 || f3 == f1) continue;
				newIndex.Add(f1);
				newIndex.Add(f2);
				newIndex.Add(f3);
			}

			return newIndex.ToArray();
		}
		private int[] BuildCollapseIndex(List<EdgeRecord> collapseRec)
		{
			int vn = mesh.VertexCount;
			int[] collapseIndex = new int[vn];
			for (int i = 0; i < vn; i++)
				collapseIndex[i] = -1;

			for (int i = collapseRec.Count - 1; i >= 0; i--)
			{
				EdgeRecord rec = collapseRec[i];
				int s = rec.vIndex;
				int t = rec.minIndex;

				if (collapseIndex[t] == -1)
					collapseIndex[s] = t;
				else
					collapseIndex[s] = collapseIndex[t];
			}
			return collapseIndex;
		}
	}
}
