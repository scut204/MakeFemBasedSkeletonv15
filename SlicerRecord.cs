using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MyGeometry;
using System.Diagnostics;

namespace IsolineEditing
{
    /// <summary>
    /// 单个面片的记录
    /// </summary>
    public class SlicerRecord
    {
        public struct PointRecord
        {
            public Vector3d p;
            public Set<int> adjFF;  // 记录原来mesh上的faceindex
            public PointRecord(Vector3d pt)
            {
                adjFF = new Set<int>();
                p = pt;
            }
            public PointRecord(Vector3d pt, int fi1 ,int fi2)
            {
                adjFF = new Set<int>();
                adjFF.Add(fi1);
                adjFF.Add(fi2);
                p = pt;
            }
            public PointRecord(Vector3d pt, int fi)
            {
                adjFF = new Set<int>();
                adjFF.Add(fi);
                p = pt;
            }

            public void AddadjFF(int fi)
            {
                adjFF.Add(fi);
            }
            public int GetAdjFaceIndex(int fi)
            {
                adjFF.Remove(fi);
                int ret = adjFF.ToArray()[0];
                adjFF.Add(fi);
                return ret;
            }
        }  
        public struct Lineindex
        {
            public int p1;     // 生成线段的点索引
            public int p2;
            public int fid;   // a line, a face
            public Lineindex(int i1 , int i2, int f)
            {
                p1 = i1;
                p2 = i2;
                fid = f;
            }
        }
        public List<PointRecord> pointInfoList  = null;
        public List<Lineindex> lineList = null;
        public Vector3d slicerCenter;
        public Vector3d skeletonNodepos;
        public Vector3d slicerNormal;
        public double radius = 0.0;
        public double perimeter = 0.0;
        public SlicerRecord(HalfSlicerRecord s1)
        {
            this.pointInfoList = s1.pointInfoList;
            this.lineList = s1.lineList;
            this.lineList.Add(new Lineindex());
            this.lineList = RebuildLineList();
            for(int i = 0; i < pointInfoList.Count; i++)
            {
                this.slicerCenter += pointInfoList[i].p / pointInfoList.Count;
            }
            this.slicerNormal = s1.slicerNormal;
        }
        public SlicerRecord()
        {
            this.slicerCenter = new Vector3d();
        }
        public SlicerRecord(Mesh ms, Plane pl)     // using nearest-neighbour point method
        {
            this.slicerNormal = pl.nv;
            this.pointInfoList = new List<PointRecord>();
            this.lineList = new List<Lineindex>();
            Vector3d skeNodePos = pl.pt;
            skeletonNodepos = skeNodePos;
            List<Vector3d> plist = new List<Vector3d>();  
            List<HashSet<int>> psted = new List<HashSet<int>>();
            //List<int> llist = new List<int>();
            Dictionary<int,int> face2line = new Dictionary<int,int>();
            Vector3d[] jtp = new Vector3d[2];
            HashSet<int>[] lineEndPoint = new HashSet<int>[2] {new HashSet<int>(), new HashSet<int>()};   // 对mesh的顶点索引
            int countLineIndex = 0;
            for (int i = 0; i < ms.FaceCount; i++)
            {
                if (GetFacePlaneCross(ms, pl, i,ref jtp, ref lineEndPoint))    //得到的是交点v和mesh上的点索引和mesh index
                {
                    face2line.Add(i,countLineIndex);   // face 到 line的索引
                    plist.Add(jtp[0]);     // jtp 有点的坐标信息
                    plist.Add(jtp[1]);
                    psted.Add(lineEndPoint[0]);
                    psted.Add(lineEndPoint[1]);
                    //llist.Add(countLineIndex);
                    countLineIndex++;
                    jtp = new Vector3d[2];
                    lineEndPoint = new HashSet<int>[2] { new HashSet<int>(), new HashSet<int>() };
                }
            }

            if (countLineIndex == 0)
            {
                MessageBox.Show("No slicer found!");
                return;
            }

            double minDist = double.MaxValue;
            int minFi = -1;
            for (int i = 0; i < ms.FaceCount; i++)
            {
                int k;
                if(face2line.TryGetValue(i,out k))
                {
                    if (minDist > (skeNodePos - plist[k*2]).Length())
                    {
                        minDist = (skeNodePos - plist[k*2]).Length();
                        minFi = i;      // 获得最小的面
                    }
                }
            }

            int pseed;
            bool[] pVisited = new bool[countLineIndex];
            
            // seed visit 
            if (face2line.TryGetValue(minFi, out pseed))
            {
                pointInfoList.Add(new PointRecord(plist[pseed * 2], minFi));
                pointInfoList.Add(new PointRecord(plist[pseed * 2 + 1], minFi));
                lineList.Add(new Lineindex(0, 1, minFi));
                pVisited[pseed] = true;
            }
            int minFistart = 0;

            // BFS
            Stack<int> faceS = new Stack<int>();
            faceS.Push(minFi);   // 将生成的面索引推进去
            bool[] Fvisited = new bool[ms.FaceCount];

            Fvisited[minFi] = true;
            HashSet<int> endFlagStartEndSet = null;
            while (faceS.Count != 0)
            {
                int curr = faceS.Pop();
                int currPi;
                face2line.TryGetValue(curr, out currPi);
                for (int i = 0; i < ms.AdjFF[curr].Length; i++)    // 对每一个邻近面进行查找
                {
                    int tAdjFi = ms.AdjFF[curr][i];

                    int tAdjPi ;
                    KeyValuePair<int, int> fof;
                    int[] tempComLine = new int[2];
                    if (face2line.TryGetValue(tAdjFi, out tAdjPi))  // 环切面则继续处理
                    {
                        if (tAdjFi == minFi && endFlagStartEndSet != null
                            && FindCommonLines(psted, currPi, tAdjPi, out tempComLine, out fof))
                        {
                            if (endFlagStartEndSet.SetEquals(tempComLine))//这样就到了终点
                            {
                                int currPtCount = pointInfoList.Count;
                                pointInfoList[fof.Value].AddadjFF(curr);
                                lineList.Add(new Lineindex(currPtCount-1, (1-minFistart), curr));
                                //debug 
                                Debug.Assert(minFistart != (1 - fof.Value), "minFi start point not match");
                                break;
                            }
                        }
                        
                        if (Fvisited[tAdjFi]) continue;
                        if (FindCommonLines(psted, currPi, tAdjPi, out tempComLine,out fof))
                        {// 有公共邻边且邻边被平面切割
                            if (curr == minFi) // 那么才刚刚开始第一个三角面
                            {
                                pointInfoList[fof.Key].AddadjFF(tAdjFi);   // 给tempComLine
                                endFlagStartEndSet = psted[currPi * 2 + (1 - fof.Key)]; // register endFLag
                                minFistart = fof.Key;
                                //pointInfoList.Add(new PointRecord(plist[tAdjPi*2 + 1 - fof.Value],tAdjFi));
                                //lineList.Add(new Lineindex(fof.Key, currLineSlide + 1, tAdjFi));
                            }
                            else
                            {                                
                                int currPtCount = pointInfoList.Count;
                                //pointInfoList[currPtCount-1].AddadjFF(tAdjFi);// 由于之前的点已经存在，所以只需要添加新的面索引
                                pointInfoList.Add(new PointRecord(plist[currPi * 2 + fof.Key], tAdjFi,curr)); // 
                                if(currPtCount == 2) // 即刚开始
                                    lineList.Add(new Lineindex(minFistart, currPtCount , curr));
                                else
                                    lineList.Add(new Lineindex(currPtCount - 1, currPtCount, curr));
                            }
                            faceS.Push(tAdjFi);
                            Fvisited[tAdjFi] = true;
                            break;
                        }   
                    }
                }
            }
            this.slicerCenter = new Vector3d(0, 0, 0);
            ComputeCenter();
            this.radius = EvaluateRadius();
            this.perimeter = EvaluatePerimeter();
        }

        /// <summary>
        ///  用来寻找面片在原来mesh上连接成的环
        /// </summary>
        /// <param name="psted">点索引序列</param>
        /// <param name="f1">第一个faceindex</param>
        /// <param name="f2">第二个faceindex</param>
        /// <param name="commonLine">存储共边的点在面上的索引</param>
        /// <param name="fof">存储两个面上的边的位置，key是f1面上的共边索引，value是f2面上的共边索引</param>
        /// <returns></returns>
        private bool FindCommonLines(List<HashSet<int>> psted, int f1, int f2,out int[] commonLine,out KeyValuePair<int,int> fof)
        {  
            fof        = (psted[f1 * 2 + 0].SetEquals(psted[f2 * 2 + 0])) ? new KeyValuePair<int, int>(0, 0) :
                         (psted[f1 * 2 + 0].SetEquals(psted[f2 * 2 + 1])) ? new KeyValuePair<int, int>(0, 1) :
                         (psted[f1 * 2 + 1].SetEquals(psted[f2 * 2 + 0])) ? new KeyValuePair<int, int>(1, 0) :
                         (psted[f1 * 2 + 1].SetEquals(psted[f2 * 2 + 1])) ? new KeyValuePair<int, int>(1, 1) : new KeyValuePair<int,int>();
            commonLine = new int[2];
            if (psted[f1 * 2 + 0].SetEquals(psted[f2 * 2 + 0])) { psted[f1 * 2 + 0].CopyTo(commonLine); return true; }
            if (psted[f1 * 2 + 0].SetEquals(psted[f2 * 2 + 1])) { psted[f1 * 2 + 0].CopyTo(commonLine); return true; }
            if (psted[f1 * 2 + 1].SetEquals(psted[f2 * 2 + 0])) { psted[f1 * 2 + 1].CopyTo(commonLine); return true; }
            if (psted[f1 * 2 + 1].SetEquals(psted[f2 * 2 + 1])) { psted[f1 * 2 + 1].CopyTo(commonLine); return true; }
             return false;
            
        }
        /// <summary>
        /// 用来判断面片是否与给定的平面相交
        /// </summary>
        /// <param name="sh">mesh</param>
        /// <param name="pl">plane</param>
        /// <param name="faceid">face index</param>
        /// <param name="jtp"></param>
        /// <param name="lineEndPoint"></param>
        /// <returns></returns>
        private bool GetFacePlaneCross(Mesh sh, Plane pl, int faceid, ref Vector3d[] jtp, ref HashSet<int>[] lineEndPoint)
        {
            lineEndPoint[0] = new HashSet<int>();
            lineEndPoint[1] = new HashSet<int>();
            int j  = 0 ;
            Vector3d[] vp ={ new Vector3d(sh.VertexPos, sh.FaceIndex[faceid * 3] * 3)
                            ,new Vector3d(sh.VertexPos, sh.FaceIndex[faceid * 3+1] * 3 )
                            ,new Vector3d(sh.VertexPos, sh.FaceIndex[faceid * 3+2] * 3 )};
            bool flag = false;
            for (int i = 0; i < 3; i++)
            {
                double temp1 = PointPlaneDistance(vp[i], pl);
                double temp2 = PointPlaneDistance(vp[(i + 1) % 3], pl);
                if (temp1 == 0 || temp2 == 0) throw new Exception("point on plane...");
                else if (temp1 * temp2 > 0) continue;
                else
                {
                    jtp[j] = (vp[i] * Math.Abs(temp2) + vp[(i + 1) % 3] * Math.Abs(temp1)) / (Math.Abs(temp1) + Math.Abs(temp2));
                    lineEndPoint[j].Add(sh.FaceIndex[faceid * 3 + i]);
                    lineEndPoint[j].Add(sh.FaceIndex[faceid * 3 +(i + 1) % 3] );
                    j++;
                    flag = true;
                }
            }
            return flag;
        }

        
        private double PointPlaneDistance(Vector3d v, Plane p)
        {
            return (v - p.pt).Dot(p.nv);
        }
        /// <summary>
        ///  计算分片半径
        /// </summary>
        private void ComputeCenter()
        {
            slicerCenter = new Vector3d(0, 0, 0);
            int nump = pointInfoList.Count;
            for (int i = 0; i < nump; i++)
            {
                slicerCenter += pointInfoList[i].p / nump;
            }
        }
        /// <summary>
        /// 接口  用来返回半径
        /// </summary>
        /// <returns></returns>
        private double EvaluateRadius()
        {
            double r = 0.0;
            for (int i = 0; i < pointInfoList.Count; i++)
            {
                r += (pointInfoList[i].p - slicerCenter).Length() / pointInfoList.Count;
            }
            return r;
        }
        public List<Lineindex> RebuildLineList()
        {
            for(int i = 0; i < lineList.Count; i++)
               lineList[i] = new Lineindex(i, (i + 1) % (lineList.Count), lineList[i].fid);
            return lineList;
        }
        private double EvaluatePerimeter()
        {
            int numl = lineList.Count;
            double pm = 0;
            for (int i = 0; i < numl; i++)
            {
                pm += (pointInfoList[lineList[i].p1].p - pointInfoList[lineList[i].p2].p).Length();
            }
            return pm;
        }        
    }
    /// <summary>
    /// 最终结果打印用
    /// </summary>
    public class SlicerRecordUniform : SlicerRecord
    {
        public new List<Vector3d> pointInfoList = null;
        public int numRadialPoint;
        public int lable;
        public SlicerRecordUniform(List<Vector3d> pointList, Vector3d center)
        {
            this.pointInfoList = pointList;
            this.numRadialPoint = pointList.Count;
            this.slicerCenter = center;
        }
        public Vector3d SlicerUniformNormal()
        {
            return (pointInfoList[0] - slicerCenter).Cross(pointInfoList[1] - slicerCenter);
        }
        
    }
    public class HalfSlicerRecord : SlicerRecord
    {
        public HalfSlicerRecord() { }
        public HalfSlicerRecord(SlicerRecord slicer, int i1, int i2)
        {
            this.slicerNormal = slicer.slicerNormal;
            if (i2 - i1 == slicer.pointInfoList.Count - 1)
            {
                this.pointInfoList = slicer.pointInfoList;
                this.lineList = slicer.lineList;
            }
            else // normal
            {
                this.lineList = slicer.lineList.GetRange(i1, i2 - i1 + 1);  // 添加从i1开始的数据，一直添加到i2
                this.pointInfoList = slicer.pointInfoList.GetRange(i1, i2 - i1 + 1);
                if (i2 == slicer.pointInfoList.Count - 1) // 即最后一位的索引
                {
                    // add slicer fid and p
                    this.pointInfoList.Add(new PointRecord(slicer.pointInfoList[0].p, slicer.lineList[0].fid));
                }
                else
                {
                    this.pointInfoList.Add(new PointRecord(slicer.pointInfoList[i2].p, slicer.lineList[i2].fid));
                }
                
            }
        }
        public PointRecord GetFirstPoint() => pointInfoList[0];
        public PointRecord GetLastPoint() => pointInfoList[pointInfoList.Count - 1];

        public SlicerRecord CombineHalfSlicer(HalfSlicerRecord inHalfSlicer)  // 和 + 不同的是可能会遇见反向的序列
        {
            if (inHalfSlicer.lineList == null || this.lineList == null) return null;
            int keyStartFI = pointInfoList[0].adjFF.ToArray()[0];
            int keyEndFI = pointInfoList[pointInfoList.Count - 1].adjFF.ToArray()[0];
            this.slicerNormal = (this.slicerNormal + inHalfSlicer.slicerNormal).Normalize();
            this.slicerCenter = 0.5 * (this.pointInfoList[0].p + this.pointInfoList[pointInfoList.Count - 1].p);
            if (keyEndFI == inHalfSlicer.pointInfoList[0].adjFF.ToArray()[0])  // head encounter
            {
                this.pointInfoList.RemoveAt(pointInfoList.Count - 1);
                this.pointInfoList.AddRange(inHalfSlicer.pointInfoList);
                this.lineList.AddRange(inHalfSlicer.lineList);
                this.lineList = base.RebuildLineList();
                this.pointInfoList.RemoveAt(pointInfoList.Count - 1);  // remove twice to make a slicerRecord
                return this;
            }
            else if (keyStartFI == inHalfSlicer.pointInfoList[0].adjFF.ToArray()[0]) // need Reverse
            {
                this.pointInfoList.RemoveAt(pointInfoList.Count - 1);
                this.pointInfoList.Reverse();
                this.lineList.Reverse();
                this.pointInfoList.AddRange(inHalfSlicer.pointInfoList);
                this.lineList.AddRange(inHalfSlicer.lineList);
                this.lineList = base.RebuildLineList();
                this.pointInfoList.RemoveAt(pointInfoList.Count - 1);
                return this;
            }
            else
            {
                throw new Exception("Slicer dont meet");
                return null;
            }
            
        }
        public static HalfSlicerRecord operator+(HalfSlicerRecord h1, HalfSlicerRecord h2)
        {
            if (h1.lineList == null || h2.lineList == null) return null;
            HalfSlicerRecord ret = new HalfSlicerRecord();
            ret.slicerNormal = (h1.slicerNormal + h2.slicerNormal).Normalize();
            ret.pointInfoList = new List<PointRecord>(h1.pointInfoList.GetRange(0,h1.pointInfoList.Count-1));
            ret.pointInfoList.AddRange(h2.pointInfoList);
            ret.lineList = new List<Lineindex>(h1.lineList);
            ret.lineList.AddRange(h2.lineList);

            return ret;
        }
    }
}