using System;
using System.Collections.Generic;
using System.Text;
using MyGeometry;
using CsGL.OpenGL;

namespace IsolineEditing
{
	public unsafe class IsolineDeformer : Deformer
	{
		#region private class FaceRecord
		private class FaceRecord : IComparable
		{
			public int index;
			public int e1, e2;
			public double ratio1, ratio2;
			public Vector3d n1, n2, iso;
			public FaceRecord() { e1 = e2 = -1; }
			public bool flag = false;
			public bool start = false;

			public FaceRecord(int index)
			{
				this.index = index;
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				FaceRecord rec = obj as FaceRecord;
				return index - rec.index;
			}

			#endregion
		};
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
						try
						{
							C.rowIndex[r[j]].Add(i);
						}
						catch (OutOfMemoryException e)
						{
							Program.PrintText("Exception raised " + e.Message + "\n");
							GC.Collect();
							C.rowIndex[r[j]].Add(i);
						}
						try
						{
							C.values[r[j]].Add(v[j]);
						}
						catch (OutOfMemoryException e)
						{
							Program.PrintText("Exception raised " + e.Message + "\n");
							GC.Collect();
							C.values[r[j]].Add(v[j]);
						}
					}
				}

				return C;
			}
			public ColMatrix Transpose_Destroy()
			{
				ColMatrixCreator C = new ColMatrixCreator(this.n, this.m);

				for (int i = 0; i < n; i++)
				{
					int[] r = rowIndex[i];
					double[] v = values[i];
					for (int j = 0; j < r.Length; j++)
					{
						try
						{
							C.rowIndex[r[j]].Add(i);
						}
						catch (OutOfMemoryException e)
						{
							Program.PrintText("Exception raised " + e.Message + "\n");
							GC.Collect();
							C.rowIndex[r[j]].Add(i);
						}
						try
						{
							C.values[r[j]].Add(v[j]);
						}
						catch (OutOfMemoryException e)
						{
							Program.PrintText("Exception raised " + e.Message + "\n");
							GC.Collect();
							C.values[r[j]].Add(v[j]);
						}
					}
					rowIndex[i] = null;
					values[i] = null;
				}

				return C;
			}

			public static implicit operator ColMatrix(ColMatrixCreator CC)
			{
				return CC.ToColMatrix();
			}
		};

		public enum EnumDisplayMode { None, Isolines, IsolineVertices };

		private Mesh mesh = null;
		private double[][] hf;
		private double[] isovalues;
		private int regionNum = 10;
		private EnumDisplayMode displayMode = EnumDisplayMode.None;
		private bool filterIsolineFaces = true;
		private FaceRecord[][][] fullFaceRec;
		private FaceRecord[][][] faceRec;
		private int samplesPerIsoline = 10;
		private int isolineDisplayIndex = 0;
		private ColMatrix colMT = null;
		private int[][][] isolineVertices;
		private int[][] handleVertices;
		private double[] oldVertexPos;
		private Vector3d[][] isolineCenter;

		public EnumDisplayMode DisplayMode
		{
			get { return displayMode; }
			set { displayMode = value; }
		}
		public int IsolineDisplayIndex
		{
			get { return isolineDisplayIndex; }
			set 
			{ 
				isolineDisplayIndex = value;
				if (isolineDisplayIndex < 0) isolineDisplayIndex = 0;
				if (isolineDisplayIndex >= hf.Length) isolineDisplayIndex = hf.Length - 1;
			}
		}

		public IsolineDeformer(Mesh mesh)
		{
			this.mesh = mesh;
			this.oldVertexPos = (double[])mesh.VertexPos.Clone();

			////// harmonic fields
			Program.PrintText("Harmonic fields ... \n");
			MultigridHarmonicFieldSolver hfSolver
				= new MultigridHarmonicFieldSolver(mesh, 0.25, 1000, this.regionNum);
			this.hf = hfSolver.HarmonicFields; hfSolver = null;
			this.isovalues = GetIsovalues(0.0, regionNum, regionNum);
			GC.Collect();

			Program.PrintText("isoline faces ... \n");
			this.fullFaceRec = this.faceRec = LocateIsolines(hf, isovalues);
			if (filterIsolineFaces)
				this.faceRec = FilterFaceRecords(this.fullFaceRec);
			GC.Collect();

			////// isoline vertices
			Program.PrintText("isoline vertices ... \n");
			this.isolineVertices = FindIsolineVertices();
			this.handleVertices = FindHandleVertices();
			GC.Collect();

			////// matrix M
			Program.PrintText("matrix M ... \n");
			//this.colMT = (hf.Length < 3) ? BuildMatrixMT_2handle() : BuildMatrixMT();
			this.colMT = BuildMatrixMT();
			GC.Collect();

		}

		private double[] GetIsovalues(double min, double max, int steps)
		{
			double[] isovalues = new double[steps - 1];
			double s = (max - min) / steps;
			for (int i = 0; i < steps-1; i++)
				isovalues[i] = s * (i+1);
			return isovalues;
		}
		private FaceRecord[][][] LocateIsolines(double[][] hf, double[] isovalue)
		{
			FaceRecord[][][] rec = new FaceRecord[hf.Length][][];

			for (int i = 0; i < hf.Length; i++)
			{
				double[] f = hf[i];
				rec[i] = new FaceRecord[isovalue.Length][];

				for (int j = 0; j < isovalue.Length; j++)
				{
					double v = isovalue[j];
					List<FaceRecord> list = new List<FaceRecord>();

					for (int k = 0, m = 0; k < mesh.FaceCount; k++, m += 3)
					{
						int c1 = mesh.FaceIndex[m];
						int c2 = mesh.FaceIndex[m + 1];
						int c3 = mesh.FaceIndex[m + 2];
						FaceRecord r = null;

						if ((f[c1] <= v && f[c2] >= v) || (f[c2] <= v && f[c1] >= v))
						{
							if (r == null) { r = new FaceRecord(); r.index = k; }
							if (r.e1 == -1) { r.e1 = 0; r.ratio1 = (v - f[c1]) / (f[c2] - f[c1]); }
							else { r.e2 = 0; r.ratio2 = (v - f[c1]) / (f[c2] - f[c1]); }
						}
						if ((f[c2] <= v && f[c3] >= v) || (f[c3] <= v && f[c2] >= v))
						{
							if (r == null) { r = new FaceRecord(); r.index = k; }
							if (r.e1 == -1) { r.e1 = 1; r.ratio1 = (v - f[c2]) / (f[c3] - f[c2]); }
							else { r.e2 = 1; r.ratio2 = (v - f[c2]) / (f[c3] - f[c2]); }
						}
						if ((f[c3] <= v && f[c1] >= v) || (f[c1] <= v && f[c3] >= v))
						{
							if (r == null) { r = new FaceRecord(); r.index = k; }
							if (r.e1 == -1) { r.e1 = 2; r.ratio1 = (v - f[c3]) / (f[c1] - f[c3]); }
							else { r.e2 = 2; r.ratio2 = (v - f[c3]) / (f[c1] - f[c3]); }
						}

						if (r == null) continue;
						if (r.e1 == -1 || r.e2 == -1) continue;

						Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
						Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
						Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
						Vector3d n1 = (v1 - v2).Cross(v3 - v2).Normalize();
						Vector3d p = new Vector3d(), q = new Vector3d();
						switch (r.e1)
						{
							case 0: p = v2 * r.ratio1 + v1 * (1.0 - r.ratio1); break;
							case 1: p = v3 * r.ratio1 + v2 * (1.0 - r.ratio1); break;
							case 2: p = v1 * r.ratio1 + v3 * (1.0 - r.ratio1); break;
						}
						switch (r.e2)
						{
							case 0: q = v2 * r.ratio2 + v1 * (1.0 - r.ratio2); break;
							case 1: q = v3 * r.ratio2 + v2 * (1.0 - r.ratio2); break;
							case 2: q = v1 * r.ratio2 + v3 * (1.0 - r.ratio2); break;
						}
						r.n1 = n1;
						r.iso = q - p;
						r.n2 = n1.Cross(r.iso).Normalize();
						list.Add(r);
					}

					// 					if (this.option.FilterIsolineFaces)
					// 						list = FilterFaceRecords(list);
					rec[i][j] = list.ToArray();
				}
			}

			return rec;
		}
		private FaceRecord[][][] FilterFaceRecords(FaceRecord[][][] records)
		{
			FaceRecord[][][] filteredRec = new FaceRecord[records.Length][][];

			for (int i = 0; i < records.Length; i++)
			{
				filteredRec[i] = new FaceRecord[records[i].Length][];
				for (int j = 0; j < records[i].Length; j++)
				{
					FaceRecord[] list = records[i][j];

					if (list.Length <= this.samplesPerIsoline * 2)
						filteredRec[i][j] = list;
					else
					{
						List<FaceRecord> outList = new List<FaceRecord>();
						int step = list.Length / this.samplesPerIsoline;
						int count = 0;
						int nRings = 0;

						Array.Sort(list);
						for (int k = 0; k < list.Length; k++)
						{
							FaceRecord rec = list[k];
							if (rec.flag) continue;

							FaceRecord curr = rec;
							curr.start = true;
							count = 0;
							nRings++;

							while (true)
							{
								curr.flag = true;
								if (count == 0) { outList.Add(curr); count = step; }
								count--;

								bool found = false;
								{
									int adj = this.mesh.AdjFF[curr.index][curr.e1];
									int nextIndex = Array.BinarySearch(list, new FaceRecord(adj));
									if (nextIndex >= 0 && list[nextIndex].flag == false)
									{ curr = list[nextIndex]; found = true; }
								}
								if (!found)
								{
									int adj = this.mesh.AdjFF[curr.index][curr.e2];
									int nextIndex = Array.BinarySearch(list, new FaceRecord(adj));
									// 									int nextIndex = list.BinarySearch(new FaceRecord(adj));
									if (nextIndex >= 0 && list[nextIndex].flag == false)
									{ curr = list[nextIndex]; found = true; }
								}

								if (!found) break;
							}
						}

						if (nRings > 1) foreach (FaceRecord rec in outList) rec.flag = false;

						filteredRec[i][j] = outList.ToArray();
					}
				}
			}


			return filteredRec;
		}
		private ColMatrix BuildMatrixMT()
		{
			int cn = (regionNum + 1) * 4;
			int vn = mesh.VertexCount;
			int tn = hf.Length * cn;

			ColMatrixCreator C = new ColMatrixCreator(tn, vn);
			Set<int> tmpIndex = new Set<int>();
			double[] tmp = new double[tn];

			for (int i = 0, j = 0; i < vn; i++, j += 3)
			{
				Vector3d u = new Vector3d(mesh.VertexPos, j);

				for (int k = 0; k < hf.Length; k++)
				{
					//double hv = (hf[k][i] > 0) ? hf[k][i] : -hf[k][i];
					double hv = hf[k][i];
					if (hv < 0) hv = 0;
					if (hv > regionNum) hv = regionNum;
					int tranIndex = (int)Math.Floor(hv);
					if (tranIndex < 0) tranIndex = 0;

					Vector3d v1 = u - isolineCenter[k][tranIndex];

					int indexBase = (k * cn) + (tranIndex * 4);
					double ratio2 = hv - Math.Floor(hv);
					double ratio1 = 1.0 - ratio2;
					double weight = hf[k][i] / regionNum;
					ratio1 *= weight;
					ratio2 *= weight;
					tmpIndex.Add(indexBase); tmp[indexBase] += v1.x * ratio1; indexBase++;
					tmpIndex.Add(indexBase); tmp[indexBase] += v1.y * ratio1; indexBase++;
					tmpIndex.Add(indexBase); tmp[indexBase] += v1.z * ratio1; indexBase++;
					tmpIndex.Add(indexBase); tmp[indexBase] += ratio1; indexBase++;

					if (tranIndex < regionNum)
					{
						Vector3d v2 = u - isolineCenter[k][(tranIndex + 1)];
						tmpIndex.Add(indexBase); tmp[indexBase] += v2.x * ratio2; indexBase++;
						tmpIndex.Add(indexBase); tmp[indexBase] += v2.y * ratio2; indexBase++;
						tmpIndex.Add(indexBase); tmp[indexBase] += v2.z * ratio2; indexBase++;
						tmpIndex.Add(indexBase); tmp[indexBase] += ratio2;
					}
				}

				int[] t = tmpIndex.ToArray();
				Array.Sort(t);
				foreach (int k in t)
				{
					if (tmp[k] == 0) continue;
					C.rowIndex[i].Add(k);
					C.values[i].Add(tmp[k]);
					tmp[k] = 0;
				}
				tmpIndex.Clear();
			}

			return C;
		}
		private ColMatrix BuildMatrixMT_2handle()
		{
			int cn = (regionNum + 1) * 4;
			int vn = mesh.VertexCount;
			int tn = cn;

			ColMatrixCreator C = new ColMatrixCreator(tn, vn);
			Set<int> tmpIndex = new Set<int>();
			double[] tmp = new double[tn];

			for (int i = 0, j = 0; i < vn; i++, j += 3)
			{
				Vector3d u = new Vector3d(mesh.VertexPos, j);

				{
					int k = 0;
					//double hv = (hf[k][i] > 0) ? hf[k][i] : -hf[k][i];
					double hv = hf[k][i];
					if (hv < 0) hv = 0;
					if (hv > regionNum) hv = regionNum;
					int tranIndex = (int)Math.Floor(hv);
					if (tranIndex < 0) tranIndex = 0;

					int indexBase = (k * cn) + (tranIndex * 4);
					double ratio2 = hv - Math.Floor(hv);
					double ratio1 = 1.0 - ratio2;
					double weight = hf[k][i] / regionNum;
					tmpIndex.Add(indexBase); tmp[indexBase] += u.x * ratio1; indexBase++;
					tmpIndex.Add(indexBase); tmp[indexBase] += u.y * ratio1; indexBase++;
					tmpIndex.Add(indexBase); tmp[indexBase] += u.z * ratio1; indexBase++;
					tmpIndex.Add(indexBase); tmp[indexBase] += ratio1; indexBase++;

					if (tranIndex < regionNum)
					{
						tmpIndex.Add(indexBase); tmp[indexBase] += u.x * ratio2; indexBase++;
						tmpIndex.Add(indexBase); tmp[indexBase] += u.y * ratio2; indexBase++;
						tmpIndex.Add(indexBase); tmp[indexBase] += u.z * ratio2; indexBase++;
						tmpIndex.Add(indexBase); tmp[indexBase] += ratio2;
					}
				}

				int[] t = tmpIndex.ToArray();
				Array.Sort(t);
				foreach (int k in t)
				{
					if (tmp[k] == 0) continue;
					C.rowIndex[i].Add(k);
					C.values[i].Add(tmp[k]);
					tmp[k] = 0;
				}
				tmpIndex.Clear();
			}

			return C;
		}
		private int[][][] FindIsolineVertices()
		{
			int[][][] isolineVertices = new int[hf.Length][][];
			this.isolineCenter = new Vector3d[hf.Length][];

			for (int i = 0; i < hf.Length; i++)
			{
				isolineVertices[i] = new int[regionNum + 1][];
				isolineCenter[i] = new Vector3d[regionNum + 1];

				List<int> vlist0 = new List<int>();
				List<int> vlist1 = new List<int>();
				for (int j = 0; j < mesh.VertexCount; j++)
				{
					if (mesh.Flag[j] == i + 1)
					{
						foreach (int k in mesh.AdjVV[j])
							if (mesh.Flag[k] != i + 1)
							{ 
								vlist1.Add(j);
								isolineCenter[i][regionNum] 
									+= new Vector3d(mesh.VertexPos, j * 3);
								break; 
							}
					}
					else if (mesh.Flag[j] != 0)
					{
						foreach (int k in mesh.AdjVV[j])
							if (mesh.Flag[k] != mesh.Flag[j])
							{ 
								vlist0.Add(j);
								isolineCenter[i][0]
									+= new Vector3d(mesh.VertexPos, j * 3);
								break; 
							}
					}
				}
				isolineVertices[i][0] = vlist0.ToArray();
				isolineVertices[i][regionNum] = vlist1.ToArray();
				isolineCenter[i][0] /= isolineVertices[i][0].Length;
				isolineCenter[i][regionNum] /= isolineVertices[i][regionNum].Length;

				for (int j = 0; j < isovalues.Length; j++)
				{
					double[] f = hf[i];
					double v = isovalues[j];
					Set<int> vSet = new Set<int>();
					for (int k = 0; k < mesh.VertexCount; k++)
						foreach (int k2 in mesh.AdjVV[k])
							if (f[k] <= v && f[k2] >= v)
							{
								vSet.Add(k);
								vSet.Add(k2);
							}
					isolineVertices[i][j + 1] = vSet.ToArray();
					foreach (int k in vSet)
						isolineCenter[i][j + 1] += new Vector3d(mesh.VertexPos, k * 3);
					isolineCenter[i][j + 1] /= vSet.Count;
				}
			}

			return isolineVertices;
		}
		private int[][] FindHandleVertices()
		{
			Set<int>[] handleVertexSet = new Set<int>[hf.Length];
			for (int i = 0; i < hf.Length; i++)
				handleVertexSet[i] = new Set<int>();

			for (int i = 0; i < mesh.VertexCount; i++)
			{
				if (mesh.Flag[i] != 0)
					handleVertexSet[mesh.Flag[i] - 1].Add(i);
			}

			int[][] handleVertices = new int[hf.Length][];
			for (int i = 0; i < hf.Length; i++)
				handleVertices[i] = handleVertexSet[i].ToArray();

			return handleVertices;
		}
		private Vector3d[][] FindIsolineCenter()
		{
			Vector3d[][] isolineCenter = new Vector3d[isolineVertices.Length][];

			for (int i = 0; i < isolineCenter.Length; i++)
			{
				isolineCenter[i] = new Vector3d[isolineVertices[i].Length];

				for (int j=0; j<isolineVertices[i].Length; j++)
				{
					isolineCenter[i][j] = new Vector3d();
					for (int k = 0; k < isolineVertices[i][j].Length; k++)
						isolineCenter[i][j] += new Vector3d(mesh.VertexPos, isolineVertices[i][j][k] *3);
					isolineCenter[i][j] /= (double)isolineVertices[i][j].Length;
				}
			}
			return isolineCenter;
		}
		private void ComputeLocalTransformation()
		{
			Vector3d[][] newCenter = FindIsolineCenter();
			double[][] tx = new double[3][];

			for (int i=0; i<3; i++)
			{
				tx[i] = new double[hf.Length * (regionNum + 1) * 4];
			}
			
			// for each handle
			int txIndex = 0;
			for (int i=0; i<hf.Length; i++)
			{
				// for each isoline now
				for (int j=0; j<isolineVertices[i].Length; j++)
				{
					Matrix3d UUT = new Matrix3d();
					Matrix3d VUT = new Matrix3d();
					int stepSize = 1;
					for (int k = j - stepSize; k <= j + stepSize; k++)
					{
						//int k = j;
						if (k < 0) continue;
						if (k >= isolineVertices[i].Length) continue;
						foreach (int index in isolineVertices[i][k])
						{
							int c = index * 3;
							Vector3d u = new Vector3d(oldVertexPos, c) - isolineCenter[i][j];
							Vector3d v = new Vector3d(mesh.VertexPos, c) - newCenter[i][j];
							UUT += u.OuterCross(u);
							VUT += v.OuterCross(u);
						}
					}

					Matrix3d T = VUT * UUT.Inverse();
					Matrix3d R = T.OrthogonalFactor(10e-6);
					Vector3d t = newCenter[i][j];// -isolineCenter[i][j];
					//R = R.Transpose();
					tx[0][txIndex] = R[0, 0];
					tx[0][txIndex + 1] = R[0, 1];
					tx[0][txIndex + 2] = R[0, 2];
					tx[0][txIndex + 3] = t[0];
					tx[1][txIndex]     = R[1, 0];
					tx[1][txIndex + 1] = R[1, 1];
					tx[1][txIndex + 2] = R[1, 2];
					tx[1][txIndex + 3] = t[1];
					tx[2][txIndex]     = R[2, 0];
					tx[2][txIndex + 1] = R[2, 1];
					tx[2][txIndex + 2] = R[2, 2];
					tx[2][txIndex + 3] = t[2];
					txIndex += 4;
				}
			}

			double[][] x = new double[3][];
			for (int i=0; i<3; i++)
			{
				x[i] = new double[colMT.ColumnSize];
				colMT.PreMultiply(tx[i], x[i]);
			}

			for (int i=0,j=0; i<mesh.VertexCount; i++,j+=3)
			{
				if (mesh.Flag[i] != 0) continue;
				mesh.VertexPos[j]   = x[0][i];
				mesh.VertexPos[j+1] = x[1][i];
				mesh.VertexPos[j+2] = x[2][i];
			}
		}

		private void DrawIsolines(int index)
		{
			//-------------------------------------------------- hongbo
			GL.glPushAttrib(GL.GL_LINE_BIT | GL.GL_ENABLE_BIT);
			GL.glLineWidth(3.0f);
			GL.glEnable(GL.GL_LINE_SMOOTH);
			//-------------------------------------------------- hongbo

			GL.glEnable(GL.GL_LIGHTING);
			GL.glEnable(GL.GL_NORMALIZE);
			GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_LINE);
			GL.glDisable(GL.GL_POLYGON_OFFSET_FILL);
			GL.glEnable(GL.GL_CULL_FACE);

			Mesh m = this.mesh;

			GL.glColor3f(1.0f, 0.0f, 0.0f);
			GL.glBegin(GL.GL_LINES);
			int max = (this.isolineDisplayIndex < this.fullFaceRec[index].Length) ?
				this.isolineDisplayIndex : this.fullFaceRec[index].Length;
			for (int i = this.fullFaceRec[index].Length - 1; i >= isolineDisplayIndex; i--)
				for (int j = 0; j < this.fullFaceRec[index][i].Length; j++)
				{
					FaceRecord rec = fullFaceRec[index][i][j];
					int f_index = rec.index * 3;
					int c1 = mesh.FaceIndex[f_index];
					int c2 = mesh.FaceIndex[f_index + 1];
					int c3 = mesh.FaceIndex[f_index + 2];
					Vector3d v1 = new Vector3d(mesh.VertexPos, c1 * 3);
					Vector3d v2 = new Vector3d(mesh.VertexPos, c2 * 3);
					Vector3d v3 = new Vector3d(mesh.VertexPos, c3 * 3);
					Vector3d normal = (v2 - v1).Cross(v3 - v1).Normalize();
					Vector3d p = new Vector3d(), q = new Vector3d();

					switch (rec.e1)
					{
						case 0: p = v2 * rec.ratio1 + v1 * (1.0 - rec.ratio1); break;
						case 1: p = v3 * rec.ratio1 + v2 * (1.0 - rec.ratio1); break;
						case 2: p = v1 * rec.ratio1 + v3 * (1.0 - rec.ratio1); break;
					}
					switch (rec.e2)
					{
						case 0: q = v2 * rec.ratio2 + v1 * (1.0 - rec.ratio2); break;
						case 1: q = v3 * rec.ratio2 + v2 * (1.0 - rec.ratio2); break;
						case 2: q = v1 * rec.ratio2 + v3 * (1.0 - rec.ratio2); break;
					}

					GL.glNormal3d(normal.x, normal.y, normal.z);
					GL.glVertex3d(p.x, p.y, p.z);
					GL.glVertex3d(q.x, q.y, q.z);
				}
			GL.glEnd();

			//-------------------------------------------------- hongbo
			GL.glPopAttrib();
			//-------------------------------------------------- hongbo

			GL.glEnable(GL.GL_POLYGON_OFFSET_FILL);
			GL.glEnable(GL.GL_CULL_FACE);
			GL.glDisable(GL.GL_LIGHTING);
			GL.glDisable(GL.GL_NORMALIZE);
		}
		private void DrawIsolineVertices(int index)
		{
			int[][] isoVertex = isolineVertices[index];

			GL.glPointSize(4.0f);
			GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
			fixed (double* vp = mesh.VertexPos)
			{
				GL.glVertexPointer(3, GL.GL_DOUBLE, 0, vp);
				GL.glColor3f(1.0f, 0.0f, 0.0f);
				GL.glBegin(GL.GL_POINTS);
				for (int i = 0; i < isoVertex.Length; i++)
				{
					for (int j = 0; j < isoVertex[i].Length; j++)
					{
						GL.glArrayElement(isoVertex[i][j]);
					}
				}
				GL.glEnd();
			}
			GL.glDisableClientState(GL.GL_VERTEX_ARRAY);
		}

		// Deformer Members
		void Deformer.Deform()
		{
			ComputeLocalTransformation();
		}
		void Deformer.Update()
		{
		}
		void Deformer.Display()
		{
			switch (displayMode)
			{
				case EnumDisplayMode.Isolines:
					DrawIsolines(isolineDisplayIndex);
					break;
				case EnumDisplayMode.IsolineVertices:
					DrawIsolineVertices(isolineDisplayIndex);
					break;
			}
		}
		void Deformer.Move()
		{
		}
		void Deformer.MouseDown()
		{
		}
		void Deformer.MouseUp()
		{
		}
	}
}
