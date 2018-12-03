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

		public class Resolution
		{
			public int[] vertexList = null;
			public int[] faceList = null;
			public Set<int>[] adjVertex = null;
			public Set<int>[] adjFace = null;
			public int[] collapsedIndex = null;
			public Dictionary<int, double>[] weight = null;

			public Resolution(Mesh mesh)
			{
				int vn = mesh.VertexCount;
				vertexList = new int[vn];
				faceList = (int[]) mesh.FaceIndex.Clone();
				adjVertex = new Set<int>[vn];
				adjFace = new Set<int>[vn];

				for (int i = 0; i < vn; i++)
				{
					vertexList[i] = i;
					adjVertex[i] = new Set<int>(8);
					adjFace[i] = new Set<int>(8);
					foreach (int adj in mesh.AdjVV[i])
						adjVertex[i].Add(adj);
					foreach (int adj in mesh.AdjVF[i])
						adjFace[i].Add(adj);
				}
			}
			public Resolution()
			{

			}
		};

		private Mesh mesh = null;
		private int targetVertexCount = 2000;
		private List<Resolution> resolutions = new List<Resolution>();
		private double convergeRatio = 0.001;

		public int Levels
		{
			get { return resolutions.Count; }
		}
		public List<Resolution> Resolutions
		{
			get { return resolutions; }
		}

		public MultigridContractionSolver(Mesh mesh)
		{
			Program.PrintText("[Create Multigrid Solver]");
			this.mesh = mesh;

			resolutions.Add(new Resolution(mesh));
			while (resolutions[resolutions.Count - 1].vertexList.Length > targetVertexCount)
			{
				Resolution r = resolutions[resolutions.Count - 1];
				Program.PrintText(r.vertexList.Length.ToString());
				resolutions.Add(GetNextLevel(r));
			}
			Program.PrintText(resolutions[resolutions.Count - 1].vertexList.Length.ToString());
			resolutions.Reverse();
		}
		private Resolution GetNextLevel(Resolution r)
		{
			int vn = mesh.VertexCount;

			int[] oldMap = new int[vn];
			for (int i = 0; i < vn; i++) 
				oldMap[i] = -1;
			for (int i = 0; i < r.vertexList.Length; i++)
				oldMap[r.vertexList[i]] = i;

			#region select independent vertex set to collapse
			bool[] marked = new bool[vn];
			for (int i = 0; i < vn; i++) marked[i] = false;
			List<int> collapsedIndex = new List<int>();
			List<int> remainingIndex = new List<int>();
			for (int i = 0; i < r.vertexList.Length; i++ )
			{
				int index = r.vertexList[i];

				bool found = false;
				foreach (int adj in r.adjVertex[i])
				{
					int count = 0;
					foreach (int adj2 in r.adjVertex[oldMap[adj]])
						if (r.adjVertex[oldMap[adj2]].Contains(index))
							count++;
					if (count == 2)
					{
						found = true;
						break;
					}
				}
				if (found == false) marked[index] = true;
				
				if (marked[index])
					remainingIndex.Add(index);
				else
				{
					collapsedIndex.Add(index);
					foreach (int adj in r.adjVertex[i])
						marked[adj] = true;
				}
			}
			#endregion

			#region find collapse to vertices
			int[] collapseTo = new int[vn];
			for (int i = 0; i < vn; i++) collapseTo[i] = i;
			for (int i = 0; i < collapsedIndex.Count; i++)
			{
				int index = collapsedIndex[i];
				Vector3d p = new Vector3d(mesh.VertexPos, index * 3);
				double minLen = double.MaxValue;
				int minAdj = -1;
				foreach (int adj in r.adjVertex[oldMap[index]])
				{
					int count = 0;
					foreach (int adj2 in r.adjVertex[oldMap[adj]])
						if (r.adjVertex[oldMap[adj2]].Contains(index))
							count++;
					if (count != 2) continue;

					if (adj == index)
					{
						Program.PrintText("?");
						continue;
					}
					Vector3d q = new Vector3d(mesh.VertexPos, adj * 3);
					double len = (p - q).Length();
					if (len < minLen)
					{
						minLen = len;
						minAdj = adj;
					}
					if (adj == index) throw new Exception();
				}
				if (minAdj == -1) throw new Exception();
				collapseTo[index] = minAdj;
			}
			#endregion

			#region create new adjacent vertex list
			Set<int>[] tmpAdjList = new Set<int>[r.vertexList.Length];
			for (int i = 0; i < r.vertexList.Length; i++)
			{
				Set<int> s = new Set<int>(6);
				foreach (int adj in r.adjVertex[i]) s.Add(adj);
				tmpAdjList[i] = s;
			}
			foreach (int index in collapsedIndex)
			{
				int i = oldMap[index];
				int to = collapseTo[index];
				int k = oldMap[to];
				foreach (int adj in r.adjVertex[i])
				{
					int j = oldMap[adj];
					tmpAdjList[j].Remove(index);
					if (adj != to && j != -1)
					{
						tmpAdjList[j].Add(to);
						tmpAdjList[k].Add(adj);
					}
				}
			}

			List<Set<int>> newAdjVertexIndex = new List<Set<int>>();
			for (int i = 0; i < r.vertexList.Length; i++)
			{
				int index = r.vertexList[i];
				if (marked[index])
					newAdjVertexIndex.Add(tmpAdjList[i]);
			}
			#endregion

			#region create new face index list
			List<int> newFaceIndex = new List<int>();
			for (int i=0; i<r.faceList.Length; i+=3)
			{
				int c1 = r.faceList[i];
				int c2 = r.faceList[i + 1];
				int c3 = r.faceList[i + 2];
				c1 = collapseTo[c1];
				c2 = collapseTo[c2];
				c3 = collapseTo[c3];
				if (c1 == c2 || c2 == c3 || c3 == c1) continue;
				newFaceIndex.Add(c1);
				newFaceIndex.Add(c2);
				newFaceIndex.Add(c3);
			}
			#endregion

			#region create new adjacent face list
			int[] map = new int[vn];
			for (int i = 0; i < vn; i++) map[i] = -1;
			Set<int>[] newAdjFaceIndex = new Set<int>[remainingIndex.Count];
			for (int i = 0; i < remainingIndex.Count; i++)
			{
				map[remainingIndex[i]] = i;
				newAdjFaceIndex[i] = new Set<int>();
			}
			for (int i=0,j=0; i<newFaceIndex.Count; i+=3,j++)
			{
				int c1 = map[newFaceIndex[i]];
				int c2 = map[newFaceIndex[i + 1]];
				int c3 = map[newFaceIndex[i + 2]];
				if (c1 == -1 || c2 == -1 || c3 == -1) throw new Exception();
				newAdjFaceIndex[c1].Add(j);
				newAdjFaceIndex[c2].Add(j);
				newAdjFaceIndex[c3].Add(j);
			}
			#endregion

			Dictionary<int, double>[] interpolateWeight = new Dictionary<int, double>[collapsedIndex.Count];
			for (int i=0; i<collapsedIndex.Count; i++)
			{
				Dictionary<int, double> dict = new Dictionary<int, double>(6);
				interpolateWeight[i] = dict;
				int index = collapsedIndex[i];
				int j = oldMap[index];
				foreach (int adj in r.adjFace[j])
				{
					int k = adj * 3;
					int c1 = r.faceList[k];
					int c2 = r.faceList[k + 1];
					int c3 = r.faceList[k + 2];
					if (c2 == index) { c2 = c3; c3 = c1; c1 = index; }
					if (c3 == index) { c3 = c2; c2 = c1; c1 = index; }
					Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
					Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
					Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
					double cot1 = (v2 - v1).Dot(v3 - v1) / (v2 - v1).Cross(v3 - v1).Length();
					double cot2 = (v3 - v2).Dot(v1 - v2) / (v3 - v2).Cross(v1 - v2).Length();
					double cot3 = (v1 - v3).Dot(v2 - v3) / (v1 - v3).Cross(v2 - v3).Length();
					if (double.IsNaN(cot1)) throw new Exception();
					if (double.IsNaN(cot2)) throw new Exception();
					if (double.IsNaN(cot3)) throw new Exception();
					if (dict.ContainsKey(c3)) dict[c3] += cot2;
					else dict.Add(c3, cot2);
					if (dict.ContainsKey(c2)) dict[c2] += cot3;
					else dict.Add(c2, cot3);
				}
			}

			Resolution r2 = new Resolution();
			r2.vertexList = remainingIndex.ToArray();
			r2.faceList = newFaceIndex.ToArray();
			r2.adjVertex = newAdjVertexIndex.ToArray();
			r2.adjFace = newAdjFaceIndex;
			r2.collapsedIndex = collapsedIndex.ToArray();
			r2.weight = interpolateWeight;
			return r2;
		}

		public double[][] SolveSystem(double[] lapWeight, double[] posWeight)
		{
			#region Init Variables
			int vn = mesh.VertexCount;
			int fn = mesh.FaceCount;
			double[][] pos = new double[3][];
			pos[0] = new double[vn];
			pos[1] = new double[vn];
			pos[2] = new double[vn];
			for (int i=0,j=0; i<vn; i++,j+=3)
			{
				pos[0][i] = mesh.VertexPos[j];
				pos[1][i] = mesh.VertexPos[j + 1];
				pos[2][i] = mesh.VertexPos[j + 2];
			}
			#endregion

			for (int level = 0; level < resolutions.Count; level++)
			{
				Resolution r = resolutions[level];

				// build matrix
				ColMatrix colA = BuildMatrixA(r, lapWeight, posWeight);
				CCSMatrix ccsATA = MultiplyATA(colA);
				string s = "#: " + r.vertexList.Length + " iter: ";

				// solve system
				int n = r.vertexList.Length;
				double[] x = new double[n];
				double[] b = new double[n * 2];
				double[] ATb = new double[n];
				void* solver = null;
				if (level == 0) solver = Factorization(ccsATA);

				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < n; j++)
					{
						b[j] = 0;
						int k = r.vertexList[j];
						//b[j + n] = mesh.VertexPos[k * 3 + i] * posWeight[k] * (currentArea[j] / originalArea[k]);
						b[j + n] = mesh.VertexPos[k * 3 + i] * posWeight[k];
						//x[j] = mesh.VertexPos[k * 3 + i];
						x[j] = pos[i][k];
					}
					colA.PreMultiply(b, ATb);

					if (level == 0)
						fixed (double* _x = x, _ATb = ATb)
							Solve(solver, _x, _ATb);
					else
					{
						int iter = colA.ATACG(x, ATb, convergeRatio, n);
						s += " " + iter;
					}

					//if (level == resolutions.Count-1)
					for (int j = 0; j < n; j++)
						pos[i][r.vertexList[j]] = x[j];
				}
				Program.PrintText(s);
				
				// interpolation solution
				if (level < resolutions.Count - 1)
				{
					for (int i=0; i<r.collapsedIndex.Length; i++)
					{
						int index = r.collapsedIndex[i];
						Vector3d p = new Vector3d();
						double totWeight = 0;
						foreach (int adj in r.weight[i].Keys)
						{
							double w = r.weight[i][adj];
							p.x += pos[0][adj] * w;
							p.y += pos[1][adj] * w;
							p.z += pos[2][adj] * w;
							totWeight += w;
						}
						pos[0][index] = p.x /= totWeight;
						pos[1][index] = p.y /= totWeight;
						pos[2][index] = p.z /= totWeight;
					}
				}
			}

			return pos;
		}
		private ColMatrix BuildMatrixA(Resolution r, double[] lapWeight, double[] posWeight)
		{
			// build backward map
			int[] map = new int[mesh.VertexCount];
			for (int i = 0; i < r.vertexList.Length; i++)
				map[r.vertexList[i]] = i;

			int n = r.vertexList.Length;
			int fn = r.faceList.Length / 3;
			SparseMatrix A = new SparseMatrix(2 * n, n);

			for (int i = 0, j = 0; i < fn; i++, j += 3)
			{
				int c1 = r.faceList[j];
				int c2 = r.faceList[j + 1];
				int c3 = r.faceList[j + 2];
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
					e.value *= lapWeight[r.vertexList[i]];
			}

			// positional constraints
			for (int i = 0; i < n; i++)
			{
				A.AddElement(i + n, i, posWeight[r.vertexList[i]]);
			}

			A.SortElement();
			ColMatrixCreator cmA = new ColMatrixCreator(2 * n, n);
			foreach (List<SparseMatrix.Element> row in A.Rows)
				foreach (SparseMatrix.Element e in row)
					cmA.AddValueTo(e.i, e.j, e.value);

			return cmA;
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
	}
}
