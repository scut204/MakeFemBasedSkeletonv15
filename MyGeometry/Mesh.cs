using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace MyGeometry
{
	public class Mesh
	{
		private int vertexCount;
		private int faceCount;
		private double [] vertexPos      = null;
		private double [] vertexNormal   = null;
		private int    [] faceIndex      = null;  // 存储3个点索引的indexlist
		private double [] faceNormal     = null;
		private double [] dualVertexPos  = null;
		private byte   [] flag           = null;
		private bool   [] isBoundary     = null;
		private float  [] color          = null;  // 以 vertexCount作为
		private int    [] singleVertexGroup = null;
		private int [][] adjVV = null;
		private int [][] adjVF = null;
		private int [][] adjFF = null;

		public int VertexCount
		{
			get { return vertexCount; }
		}
		public int FaceCount
		{
			get { return faceCount; }
		}
		public double[] VertexPos { 
			get { return vertexPos; } 
			set 
			{
				if (value.Length < vertexCount * 3)
					throw new Exception(); 
				vertexPos = value; 
			} 
		}
		public double [] VertexNormal { get { return vertexNormal; } }
		public int [] FaceIndex { get { return faceIndex; } }
		public double [] FaceNormal { get { return faceNormal; } }
		public double [] DualVertexPos { get { return dualVertexPos; } }
		public byte [] Flag
		{
			get { return flag; }
			set 
			{
				if (value.Length < vertexCount)
					throw new Exception();
				flag = value; 
			}
		}
		public bool [] IsBoundary
		{
			get { return isBoundary; }
			set { isBoundary = value; }
		}
		public int [] SingleVertexGroup
		{
			get { return singleVertexGroup; }
		}
		public float [] Color
		{
			get { return color; }
			set { color = value; }
		}
		public int[][] AdjVV
		{
			get { return adjVV; }
			set { adjVV = value; }
		}
		public int [][] AdjVF
		{
			get { return adjVF; }
			set { adjVF = value; }
		}
		public int [][] AdjFF
		{
			get { return adjFF; }
			set { adjFF = value; }
		}

		public Mesh(StreamReader sr)
		{
			ArrayList vlist = new ArrayList();
			ArrayList flist = new ArrayList();
			char[] delimiters = { ' ', '\t' };
			string s = "";

			while (sr.Peek() > -1)
			{
				s = sr.ReadLine();
				string[] tokens = s.Split(delimiters);
				switch (tokens[0].ToLower())
				{
					case "v":
						for (int i = 1; i < tokens.Length; i++)
						{
							if (tokens[i].Equals("")) continue;
							vlist.Add(Double.Parse(tokens[i]));
						}
						break;
					case "f":
						for (int i = 1; i < tokens.Length; i++)
						{
							if (tokens[i].Equals("")) continue;
							string[] tokens2 = tokens[i].Split('/');
							int index = Int32.Parse(tokens2[0]);
							if (index <= 0) index = vlist.Count+index+1;
							flist.Add(index - 1);
						}
						break;
				}
			}

			this.vertexCount = vlist.Count / 3;
			this.faceCount = flist.Count / 3;
			this.vertexPos = new double[vertexCount * 3];
			this.vertexNormal = new double[vertexCount * 3];
			this.faceIndex = new int[faceCount * 3];
			this.faceNormal = new double[faceCount * 3];
			this.flag = new byte[vertexCount];
			this.isBoundary = new bool[vertexCount];
			this.color = new float[faceCount * 3];

			for (int i = 0; i < vlist.Count; i++) vertexPos[i] = (double)vlist[i];
			for (int i = 0; i < flist.Count; i++) faceIndex[i] = (int)flist[i];
//			for (int i = 1; i < vlist.Count; i += 3) vertexPos[i] /= 2.0;

			ScaleToUnitBox();
			MoveToCenter();
			ComputeFaceNormal();
			ComputeVertexNormal();
			this.adjVV = BuildAdjacentMatrix().GetRowIndex();
			this.adjVF = BuildAdjacentMatrixFV().GetColumnIndex();
			this.adjFF = BuildAdjacentMatrixFF().GetRowIndex();
			FindBoundaryVertex();
			ComputeDualPosition();

			for (int i = 0; i < FaceCount; i++)
			{
				double area = ComputeFaceArea(i);
				if (double.IsNaN(area))
					//FormMain.CurrForm.OutputText("bad tri: " + i);
				if (AdjFF[i].Length != 3)
				{
					//FormMain.CurrForm.OutputText("bad FF adj: " + i + " " + AdjFF[i].Length);
				}
			}
		}
		public Mesh(StreamReader sr, String type)
		{
			if (type.Equals("cgal") == false) throw new Exception();

			char[] delimiters = { ' ', '\t', '\n', '\r' };
			String[] tokens = sr.ReadToEnd().Split(delimiters);

			//FormMain.CurrForm.OutputText(tokens[0]+" "+tokens[1]+" "+tokens[2]);


			this.vertexCount = Int32.Parse(tokens[0]);
			this.faceCount = Int32.Parse(tokens[1]);
			this.vertexPos = new double[vertexCount*3];
			this.faceIndex = new int[faceCount*3];

			int k = 3;
			for(int i=0,j=3; i<vertexCount-1; i++,j+=3)
			{
				while (tokens[k].Equals("")) k++; 
				vertexPos[j+0] = Double.Parse(tokens[k++]);
				while (tokens[k].Equals("")) k++; 
				vertexPos[j+1] = Double.Parse(tokens[k++]);
				vertexPos[j+2] = 0;
			}


			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				while (tokens[k].Equals("")) k++; 
				faceIndex[j+0] = Int32.Parse(tokens[k++]);
				while (tokens[k].Equals("")) k++; 
				faceIndex[j+1] = Int32.Parse(tokens[k++]);
				while (tokens[k].Equals("")) k++; 
				faceIndex[j+2] = Int32.Parse(tokens[k++]);
			}

			RemoveVertex(0);

			this.vertexNormal = new double[vertexCount*3];
			this.faceNormal = new double[faceCount*3];
			this.flag = new byte[vertexCount];
			this.isBoundary = new bool[vertexCount];

			ScaleToUnitBox();
			MoveToCenter();
			ComputeFaceNormal();
			ComputeVertexNormal();
			SparseMatrix adjMatrix = BuildAdjacentMatrix();
			SparseMatrix adjMatrixFV = BuildAdjacentMatrixFV();
			this.adjVV = adjMatrix.GetRowIndex();
			this.adjVF = adjMatrixFV.GetColumnIndex();
			FindBoundaryVertex();
			ComputeDualPosition();
		}
		public void Write(StreamWriter sw)
		{
			for (int i=0,j=0; i<vertexCount; i++,j+=3)
			{
				sw.Write("v ");
				sw.Write(vertexPos[j].ToString() + " ");
				sw.Write(vertexPos[j+1].ToString() + " ");
				sw.Write(vertexPos[j+2].ToString());
				sw.WriteLine();
			}

			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				sw.Write("f ");
				sw.Write((faceIndex[j]+1).ToString() + " ");
				sw.Write((faceIndex[j+1]+1).ToString() + " ");
				sw.Write((faceIndex[j+2]+1).ToString());
				sw.WriteLine();
			}
		}
		public void LoadSelectedVertexPositions(StreamReader sr)    // 这个是用来修改mesh网格
		{
			ArrayList vlist = new ArrayList();
			ArrayList flist = new ArrayList();
			char[] delimiters = { ' ', '\t' };
			string s = "";

			while (sr.Peek() > -1)
			{
				s = sr.ReadLine();
				string[] tokens = s.Split(delimiters);

				int index = Int32.Parse(tokens[0]);
				double x = Double.Parse(tokens[1]);
				double y = Double.Parse(tokens[2]);
				double z = Double.Parse(tokens[3]);
				if (index >= vertexCount) continue;
				index *= 3;
				this.vertexPos[index++] = x;
				this.vertexPos[index++] = y;
				this.vertexPos[index]   = z;
			}
		}
		public void SaveSelectedVertexPositions(StreamWriter sw)
		{
			for (int i=0,j=0; i<vertexCount; i++,j+=3)
			{
				if (this.flag[i] == 0) continue;
				sw.Write(i.ToString() + " ");
				sw.Write(vertexPos[j].ToString() + " ");
				sw.Write(vertexPos[j + 1].ToString() + " ");
				sw.Write(vertexPos[j + 2].ToString());
				sw.WriteLine();
			}
		}
		public Vector3d MaxCoord()
		{
			Vector3d maxCoord = new Vector3d(double.MinValue, double.MinValue, double.MinValue);
			for (int i=0,j=0; i<vertexCount; i++,j+=3) 
			{
				Vector3d v = new Vector3d(vertexPos, j);
				maxCoord = Vector3d.Max(maxCoord, v);
			}
			return maxCoord;
		}
		public Vector3d MinCoord()
		{
			Vector3d minCoord = new Vector3d(double.MaxValue, double.MaxValue, double.MaxValue);
			for (int i=0,j=0; i<vertexCount; i++,j+=3) 
			{
				Vector3d v = new Vector3d(vertexPos, j);
				minCoord = Vector3d.Min(minCoord, v);
			}
			return minCoord;
		}
		public double Volume()
		{
			double totVolume = 0;
			for (int i = 0, j = 0; i < faceCount; i++, j += 3)
			{
				int c1 = faceIndex[j] * 3;
				int c2 = faceIndex[j + 1] * 3;
				int c3 = faceIndex[j + 2] * 3;
				Vector3d a = new Vector3d(vertexPos, c1);
				Vector3d b = new Vector3d(vertexPos, c2);
				Vector3d c = new Vector3d(vertexPos, c3);
				totVolume += 
					a.x * b.y * c.z - 
					a.x * b.z * c.y - 
					a.y * b.x * c.z +
					a.y * b.z * c.x + 
					a.z * b.x * c.y - 
					a.z * b.y * c.x;
			}
			return totVolume;
		}
		public void MoveToCenter()
		{
			Vector3d center = (MaxCoord() + MinCoord()) / 2.0;

			for (int i=0,j=0; i<vertexCount; i++,j+=3) 
			{
				vertexPos[j] -= center.x;
				vertexPos[j+1] -= center.y;
				vertexPos[j+2] -= center.z;
			}
		}
		public void ScaleToUnitBox()
		{
			Vector3d d = MaxCoord() - MinCoord();
			double s = (d.x>d.y)? d.x:d.y;
			s = (s>d.z)? s: d.z;
			if (s<=0) return;
			for (int i=0; i<vertexPos.Length; i++)
				vertexPos[i] /= s;
		}
		public void Transform(Matrix4d tran)
		{
			for (int i=0,j=0; i<vertexCount; i++,j+=3)
			{
				Vector4d v = new Vector4d(vertexPos[j], vertexPos[j + 1], vertexPos[j + 2], 1.0);
				v = tran * v;
				vertexPos[j] = v.x;
				vertexPos[j+1] = v.y;
				vertexPos[j+2] = v.z;
			}
		}
		public void ComputeFaceNormal() 
		{
			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				int c1 = faceIndex[j] * 3;
				int c2 = faceIndex[j+1] * 3;
				int c3 = faceIndex[j+2] * 3;
				Vector3d v1 = new Vector3d(vertexPos, c1);
				Vector3d v2 = new Vector3d(vertexPos, c2);
				Vector3d v3 = new Vector3d(vertexPos, c3);
				Vector3d normal = (v2-v1).Cross(v3-v1).Normalize();
				faceNormal[j] = normal.x;
				faceNormal[j+1] = normal.y;
				faceNormal[j+2] = normal.z;
			}
		}
		public void ComputeVertexNormal() 
		{
			Array.Clear(vertexNormal, 0, vertexNormal.Length);
			for (int i=0,j=0; i<faceCount; i++,j+=3) 
			{
				int c1 = faceIndex[j] * 3;
				int c2 = faceIndex[j+1] * 3;
				int c3 = faceIndex[j+2] * 3;
				vertexNormal[c1] += faceNormal[j];
				vertexNormal[c2] += faceNormal[j];
				vertexNormal[c3] += faceNormal[j];
				vertexNormal[c1+1] += faceNormal[j+1];
				vertexNormal[c2+1] += faceNormal[j+1];
				vertexNormal[c3+1] += faceNormal[j+1];
				vertexNormal[c1+2] += faceNormal[j+2];
				vertexNormal[c2+2] += faceNormal[j+2];
				vertexNormal[c3+2] += faceNormal[j+2];
			}
			for (int i=0,j=0; i<vertexCount; i++,j+=3)
			{
				Vector3d n = new Vector3d(vertexNormal, j);
				n = n.Normalize();
				vertexNormal[j] = n.x;
				vertexNormal[j+1] = n.y;
				vertexNormal[j+2] = n.z;
			}
		}
		public SparseMatrix BuildAdjacentMatrix()
		{
			SparseMatrix m = new SparseMatrix(vertexCount, vertexCount, 6);

			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				int c1 = faceIndex[j];
				int c2 = faceIndex[j+1];
				int c3 = faceIndex[j+2];
				m.AddElementIfNotExist(c1, c2, 1.0);
				m.AddElementIfNotExist(c2, c3, 1.0);
				m.AddElementIfNotExist(c3, c1, 1.0);
				m.AddElementIfNotExist(c2, c1, 1.0);
				m.AddElementIfNotExist(c3, c2, 1.0);
				m.AddElementIfNotExist(c1, c3, 1.0);
			}

			m.SortElement();
			return m;
		}
		public SparseMatrix BuildAdjacentMatrixFV()
		{
			SparseMatrix m = new SparseMatrix(faceCount, vertexCount, 6);

			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				m.AddElementIfNotExist(i, faceIndex[j], 1.0);
				m.AddElementIfNotExist(i, faceIndex[j+1], 1.0);
				m.AddElementIfNotExist(i, faceIndex[j+2], 1.0);
			}

			m.SortElement();
			return m;
		}
		public SparseMatrix BuildAdjacentMatrixFF()
		{
			SparseMatrix m = new SparseMatrix(faceCount, faceCount, 3);

			for (int i = 0; i < faceCount; i++)
			{
				int v1 = faceIndex[i * 3];
				int v2 = faceIndex[i * 3 + 1];
				int v3 = faceIndex[i * 3 + 2];

				foreach (int j in adjVF[v1])
					if (j != i && IsContainVertex(j, v2))
						m.AddElementIfNotExist(i, j, 1.0);

				foreach (int j in adjVF[v2])
					if (j != i && IsContainVertex(j, v3))
						m.AddElementIfNotExist(i, j, 1.0);

				foreach (int j in adjVF[v3])
					if (j != i && IsContainVertex(j, v1))
						m.AddElementIfNotExist(i, j, 1.0);
			}

			return m;
		}
		public void FindBoundaryVertex()
		{
			for (int i = 0; i < vertexCount; i++)
			{
				int nAdjV = adjVV[i].Length;
				int nAdjF = adjVF[i].Length;
				this.isBoundary[i] = (nAdjV != nAdjF);
				if (nAdjV != nAdjF)
				{
					//FormMain.CurrForm.OutputText("bad: " + i);
					this.flag[i] = 1;
				}
			}
		}
		public void GroupingFlags()
		{
			for (int i=0; i<flag.Length; i++)
				if (flag[i] != 0) flag[i] = 255;

			byte id = 0;
			Queue queue = new Queue();
			List<int> singleVertexGroupList = new List<int>();
			for (int i=0; i<vertexCount; i++)
				if (flag[i] == 255)
				{
					id++;
					flag[i] = id;
					queue.Enqueue(i);
					bool found = false;
					while (queue.Count > 0)
					{
						int curr = (int)queue.Dequeue();
						foreach (int j in adjVV[curr])
						{
							if (flag[j] == 255)
							{
								flag[j] = id;
								queue.Enqueue(j);
								found = true;
							}
						}
					}

					if (!found) singleVertexGroupList.Add(i);
				}

			this.singleVertexGroup = singleVertexGroupList.ToArray();
		}
		public void FindSingleVertexGroup()
		{
			Set<int> s = new Set<int>();

			for (int i=0; i<vertexCount; i++)
			{
				if (flag[i] != 0)
				{
					bool found = false;
					foreach (int j in adjVV[i])
						if (flag[j] == flag[i])
						{
							found = true;
							break;
						}
					if (!found)
						s.Add(i);
				}
			}

			int[] arr = s.ToArray();
			Array.Sort(arr);
			this.singleVertexGroup = arr;
		}
		public void RemoveVertex(int index)
		{
			_RemoveVertex(index);

			this.vertexNormal = new double[vertexCount*3];
			this.faceNormal = new double[faceCount*3];
			this.flag = new byte[vertexCount];
			this.isBoundary = new bool[vertexCount];

			ComputeFaceNormal();
			ComputeVertexNormal();
			SparseMatrix adjMatrix = BuildAdjacentMatrix();
			SparseMatrix adjMatrixFV = BuildAdjacentMatrixFV();
			this.adjVV = adjMatrix.GetRowIndex();
			this.adjVF = adjMatrixFV.GetColumnIndex();
			FindBoundaryVertex();
			ComputeDualPosition();
		}
		public void RemoveVertex(ArrayList indice)
		{
			for (int i=0; i<indice.Count; i++)
				_RemoveVertex(((int)indice[i])-i);

			this.vertexNormal = new double[vertexCount*3];
			this.faceNormal = new double[faceCount*3];
			this.flag = new byte[vertexCount];
			this.isBoundary = new bool[vertexCount];

			ComputeFaceNormal();
			ComputeVertexNormal();
			SparseMatrix adjMatrix = BuildAdjacentMatrix();
			SparseMatrix adjMatrixFV = BuildAdjacentMatrixFV();
			this.adjVV = adjMatrix.GetRowIndex();
			this.adjVF = adjMatrixFV.GetColumnIndex();
			FindBoundaryVertex();
			ComputeDualPosition();
		}
		public bool IsContainVertex(int fIndex, int vIndex)
		{
			int b = fIndex * 3;
			int v1 = faceIndex[b];
			int v2 = faceIndex[b + 1];
			int v3 = faceIndex[b + 2];
			return (v1 == vIndex) || (v2 == vIndex) || (v3 == vIndex);
		}
		public void ComputeDualPosition()
		{
			if (dualVertexPos == null)
				dualVertexPos = new double[faceCount * 3];

			for (int i=0; i<dualVertexPos.Length; i++)
				dualVertexPos[i] = 0.0;

			for (int i=0,j=0; i<vertexCount; i++,j+=3)
				foreach (int k in adjVF[i])
				{
					int b = k * 3;
					dualVertexPos[b] += vertexPos[j];
					dualVertexPos[b+1] += vertexPos[j+1];
					dualVertexPos[b+2] += vertexPos[j+2];
				}

			for (int i = 0; i < dualVertexPos.Length; i++)
				dualVertexPos[i] /= 3.0;
		}
		public Vector3d ComputeDualPosition(int fIndex)
		{
			return new Vector3d(dualVertexPos, fIndex * 3);
		}
		public double ComputeFaceArea(int fIndex)
		{
			int b = fIndex * 3;
			Vector3d v1 = new Vector3d(VertexPos, faceIndex[b] * 3);
			Vector3d v2 = new Vector3d(VertexPos, faceIndex[b + 1] * 3);
			Vector3d v3 = new Vector3d(VertexPos, faceIndex[b + 2] * 3);
			return ((v2 - v1).Cross(v3 - v1)).Length() / 2.0;
		}
        
		public double AverageFaceArea()
		{
			double tot = 0;
			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				Vector3d v1 = new Vector3d(VertexPos, faceIndex[j] * 3);
				Vector3d v2 = new Vector3d(VertexPos, faceIndex[j + 1] * 3);
				Vector3d v3 = new Vector3d(VertexPos, faceIndex[j + 2] * 3);
				tot += ((v2 - v1).Cross(v3 - v1)).Length() / 2.0;
			}
			return (tot / faceCount);
		}
		public void SwapFlags(byte n1, byte n2)
		{
			for (int i=0; i<vertexCount; i++)
			{
				if (flag[i] == n1) flag[i] = n2;
				else if (flag[i] == n2) flag[i] = n1;
			}
		}
		private void _RemoveVertex(int index)
		{
			double [] vp = new double[(vertexCount-1)*3];
			for (int i=0,j=0,k=0; i<vertexCount; i++,j+=3) 
			{
				if (i == index) continue;
				vp[k++] = vertexPos[j];
				vp[k++] = vertexPos[j+1];
				vp[k++] = vertexPos[j+2];
			}

			ArrayList flist = new ArrayList(faceCount*3);
			for (int i=0,j=0; i<faceCount; i++,j+=3)
			{
				int c1 = faceIndex[j];
				int c2 = faceIndex[j+1];
				int c3 = faceIndex[j+2];
				if (c1==index || c2==index || c3==index) continue;
				if (c1>index) c1--; flist.Add(c1);
				if (c2>index) c2--; flist.Add(c2);
				if (c3>index) c3--; flist.Add(c3);
			}

			this.vertexCount--;
			this.vertexPos = vp;
			this.faceCount = flist.Count / 3;
			this.faceIndex = new int[flist.Count];

			for (int i=0; i<flist.Count; i++) faceIndex[i] = (int)flist[i];
		}
		private void _RemoveVertex(ArrayList indice)
		{

		}
	}
}
