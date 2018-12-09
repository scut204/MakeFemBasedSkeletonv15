using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MyGeometry;
using System.Drawing;
using System.Runtime.InteropServices;
using CsGL.OpenGL;


namespace IsolineEditing
{
     
     public struct SkeRecord
    {
        public List<Vector3d> nodePosList ;
        public List<Set<int>> adjVV;
        public List<KeyValuePair<int,int>> skeLine;
        public double skeLength ;
        public int numSkeNode ;

        
        public SkeRecord(List<Vector3d> nl, List<Set<int>> adjvv)
        {
            nodePosList = nl;
            adjVV = adjvv;
            numSkeNode = nl.Count;
            skeLine = null;
            skeLength = 0.0;
            skeLine = BuildSkeletonLine(nl, adjvv);
            skeLength = ComputeSkeletonLength(nl, adjvv);
            //debug 
            //double skl = CheckoutLength();
        }

        private List<KeyValuePair<int,int>> BuildSkeletonLine(List<Vector3d> nl, List<Set<int>> adjvv)
        {
            List<KeyValuePair<int, int>> ret = new List<KeyValuePair<int, int>>();
            bool[] flag = new bool[nl.Count];
            int start = 0;
            Stack<int> skeS = new Stack<int>();
            skeS.Push(start);
            while (skeS.Count != 0)
            {
                int cur = skeS.Pop();
                flag[cur] = true;
                int[] neighb = adjvv[cur].ToArray();
                for (int i = 0; i < neighb.Length; i++)
                {
                    int adjv_num = neighb[i];
                    if (flag[adjv_num]) continue;
                    else
                    {
                        skeS.Push(adjv_num);
                        ret.Add(new KeyValuePair<int, int>(cur, adjv_num));
                    }
                }
            }
            return ret;
        }
        private double ComputeSkeletonLength(List<Vector3d> nl, List<Set<int>> adjvv)
        {
            double ret = 0.0;
            bool[] flag = new bool[nl.Count];
            int start = 0;
            Stack<int> skeS = new Stack<int>();
            skeS.Push(start);
            while (skeS.Count != 0)
            {
                int cur = skeS.Pop();
                flag[cur] = true;
                int[] neighb = adjvv[cur].ToArray();
                for (int i = 0; i < neighb.Length; i++)
                {
                    int adjv_num = neighb[i];
                    if (flag[adjv_num]) continue;
                    else
                    {
                        skeS.Push(adjv_num);
                        ret = ret + (nl[cur] - nl[adjv_num]).Length();
                    }
                }
            }
            return ret;
        }
        private double CheckoutLength() //debug
         {
             double ret = 0.0;
             for (int i = 0; i < this.skeLine.Count; i++)
             {
                 ret += (nodePosList[skeLine[i].Key] - nodePosList[skeLine[i].Value]).Length();
             }
             return ret;
         }
     }
    
    public class Segmentation
    {
        public enum bodypart { Head, Torso, Arm_0, Arm_1, Leg_0, Leg_1, PartCount };
        public List<int>[] partSkelSeq = new List<int>[(int)bodypart.PartCount];
        Mesh segMesh;
        SkeRecord skeRcd;
        double interval;
        public char displayMode = 's';
        public SlicerRecord displaySlicer;
        public SlicerRecordUniform displaySlicerUniform;
        public Segmentation(Skeletonizer skel, Mesh currmesh)
        {
            segMesh = currmesh;
            int numSkSeq = 80; // 骨架的分段数量
            if (skel != null)
            {
                skeRcd = skel.ReturnSkeleton(); // get skeleton from sig08
                interval = (double)skeRcd.skeLength/numSkSeq;
                SkeletonRefining(interval);   // refine
                SegmentToPart();      //get partskelseq
            }
        }

        public void SegPostProcess()
        {
            DebugMethod.slicerUniformAll = new List<List<SlicerRecordUniform>>();
            bodypart[] bdparr = (bodypart[])Enum.GetValues(typeof(bodypart));
            for (int i = 0; i < (int)bodypart.PartCount; i++)
            {
                int n = (int)bodypart.Torso == i ? 15 : 5;
                List<SlicerRecord> slicerSeq;
                bodypart bdt = bdparr[i];
                CreateSlicerSequence(bdt, interval, out slicerSeq);
                List<SlicerRecordUniform> radialSlicer = RadialSlicerCut(slicerSeq, n);
                // display
                foreach(SlicerRecordUniform sru in radialSlicer)
                {
                    sru.lable = i;
                } 
                DebugMethod.slicerUniformAll.Add(radialSlicer);
                
                WriteToSlicFile(radialSlicer, bdt);
            }
        }
        //TODO:test unit
        private void SegmentToPart()
        {
            // find the torso ske
            
            int nodeNum = skeRcd.nodePosList.Count;
            int chest4v = 0;
            int navel3v = 0; // 肚脐眼
            for (int i = 0; i < nodeNum; i++)
            {
                if (skeRcd.adjVV[i].Count == 4) chest4v = i;
                if (skeRcd.adjVV[i].Count == 3) navel3v = i;
            }
            bool[] sflag = Enumerable.Repeat<bool>(false,nodeNum).ToArray();
            sflag[chest4v] = true;
            DFSFindTorso(skeRcd, chest4v,navel3v ,sflag, out partSkelSeq[(int)bodypart.Torso]);
            Vector3d torsoDown = (skeRcd.nodePosList[chest4v] - skeRcd.nodePosList[navel3v]).Normalize();
            DFSFindHead(skeRcd, chest4v, torsoDown, sflag, out partSkelSeq[(int)bodypart.Head]);
            DFSFindLimb(skeRcd, chest4v, sflag, out partSkelSeq[(int)bodypart.Arm_0]);
            DFSFindLimb(skeRcd, chest4v, sflag, out partSkelSeq[(int)bodypart.Arm_1]);
            DFSFindLimb(skeRcd, navel3v, sflag, out partSkelSeq[(int)bodypart.Leg_0]);
            DFSFindLimb(skeRcd, navel3v, sflag, out partSkelSeq[(int)bodypart.Leg_1]);
            bodypart[] bdt = (bodypart[])Enum.GetValues(typeof(bodypart));
        }
        private void SkeletonRefining(double interval)
        {
            SkeletonDensify(interval/5);    // 尽量紧密一点
            SkeletonSmoothing(20);
        }
        private void SkeletonSmoothing(int times)
        {
            int loop = 0;
             
            while (loop < times)
            {
                List<Vector3d> temp = new List<Vector3d>(skeRcd.nodePosList.ToArray());
                for (int i = 0; i < skeRcd.nodePosList.Count; i++)
                {
                    int adjCount = skeRcd.adjVV[i].Count;
                    if (adjCount > 1)
                    {
                        temp[i] = new Vector3d();
                        int[] adjv = skeRcd.adjVV[i].ToArray();
                        for (int j = 0; j < adjCount; j++)
                        {
                            int curradj = adjv[j];
                            temp[i] += skeRcd.nodePosList[curradj] / adjCount;
                        }
                    }
                }
                skeRcd.nodePosList = temp;
                loop++;
            }
        }
        // TODO: test unit
        private void SkeletonDensify(double interval)  // 这里使用interval是为了保持密度均一
        {
            Set<int>[] initadj = new Set<int>[skeRcd.numSkeNode];
            for (int i = 0; i < initadj.Count(); i++) initadj[i] = new Set<int>();   
            List<Set<int>> updateAdjVV = new List<Set<int>>(initadj); // 填充list
            
            bool[] flag = new bool[skeRcd.nodePosList.Count];
            int start = 0;
            Stack<int> skeS = new Stack<int>();
            skeS.Push(start);
            while (skeS.Count != 0)
            {
                int cur = skeS.Pop();
                flag[cur] = true;
                int[] neighb = skeRcd.adjVV[cur].ToArray();
                for (int i = 0; i < neighb.Length; i++)
                {
                    int adjv_num = neighb[i]; 
                    if (flag[adjv_num]) continue;
                    else
                    {
                        skeS.Push(adjv_num);

                        #region expand the skeleton fragment
                        double leng = (skeRcd.nodePosList[cur]-skeRcd.nodePosList[adjv_num]).Length();
                        int numDensity = (int)(leng / interval);
                        if (numDensity == 0) continue;
                        else
                        {
                            List<int> fragSkeindexlist = new List<int>();
                            fragSkeindexlist.Add(cur);
                            int curSkeCount = skeRcd.nodePosList.Count;  // initial index of current progress
                            for (int j = 1; j <= numDensity; j++)     // 从第二个点开始
                            {
                                fragSkeindexlist.Add(curSkeCount);
                                skeRcd.nodePosList.Add((1 - j * interval / leng) * (skeRcd.nodePosList[cur]) +
                                                     j * interval / leng * (skeRcd.nodePosList[adjv_num]));
                                updateAdjVV.Add(new Set<int>());
                                curSkeCount++;
                            }
                            fragSkeindexlist.Add(adjv_num);   // next to build the new updateadjvv

                            updateAdjVV[cur].Add(fragSkeindexlist[1]);
                            for (int j = 1; j < fragSkeindexlist.Count - 1; j++)
                            {
                                Set<int> looptemp = new Set<int>(2);
                                looptemp.Add(fragSkeindexlist[j - 1]);
                                looptemp.Add(fragSkeindexlist[j + 1]);
                                updateAdjVV[fragSkeindexlist[j]] = looptemp;
                            }
                            updateAdjVV[adjv_num].Add(fragSkeindexlist[fragSkeindexlist.Count - 2]);
                        }  
                        #endregion


                    }
                }
            } 
            skeRcd.adjVV = updateAdjVV;
            // debug
            bool s = Checkchest(updateAdjVV);
        }
        public void DisplayRefinedSkeleton()
        {
            // debug 
            //Vector3d seletpoint1 = this.skeRcd.nodePosList[0];
            //GL.glPointSize(13.0f);
            //GL.glColor3f(0,0,0);
            //GL.glBegin(GL.GL_POINTS);
            //GL.glVertex3d(seletpoint1.x, seletpoint1.y, seletpoint1.z);
            //GL.glEnd();
            Color[] parts = new Color[(int)bodypart.PartCount]
                            {Color.BlanchedAlmond,
                             Color.Brown,
                             Color.DarkGreen,
                             Color.Gray,
                             Color.LightPink,
                             Color.Red
                             // for more colors
                             };
            for(int i = 0;i<(int)bodypart.PartCount;i++)
            {
                float[] newcolor = ColorHelper.byte2float(parts[i]);
                List<int> pt = this.partSkelSeq[i];
                for(int j = 0; j < pt.Count; j++)
                {
                    Vector3d seletpoint = this.skeRcd.nodePosList[pt[j]];
                    GL.glPointSize(6.0f);
                    GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                    GL.glBegin(GL.GL_POINTS);
                    GL.glVertex3d(seletpoint.x, seletpoint.y, seletpoint.z);
                    GL.glEnd();
                }

                for (int j = 0; j < pt.Count-1; j++)
                {
                    GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                    GL.glLineWidth(2.0f);
                    GL.glBegin(GL.GL_LINES);
                    Vector3d st = this.skeRcd.nodePosList[pt[j]];
                    Vector3d ed = this.skeRcd.nodePosList[pt[j+1]];
                    GL.glVertex3d(st.x, st.y, st.z);
                    GL.glVertex3d(ed.x, ed.y, ed.z);
                    GL.glEnd();
                }
                
            }

        }
        public void DisplayCurrentSlicer()
        {
            if (this.displaySlicer == null) return;
            DisplaySlicer(displaySlicer);

        }
        private void DisplaySlicer()
        {

        }
        private void DisplaySlicer(SlicerRecord sli)
        {
            float skeletonNodeSize = 6.0f;
            float[] newcolor = ColorHelper.byte2float(Color.Gold);

            // draw center 
            GL.glPointSize(skeletonNodeSize);
            GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
            GL.glBegin(GL.GL_POINTS);
            GL.glVertex3d(sli.slicerCenter.x, sli.slicerCenter.y, sli.slicerCenter.z);
            GL.glEnd();
            newcolor = ColorHelper.byte2float(Color.Black);

            // mark skeleton node
            GL.glPointSize(skeletonNodeSize + 3);
            GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
            GL.glBegin(GL.GL_POINTS);
            GL.glVertex3d(sli.skeletonNodepos.x, sli.skeletonNodepos.y, sli.skeletonNodepos.z);
            GL.glEnd();

            // draw points
            newcolor = ColorHelper.byte2float(Color.Orange);
            for (int i = 0; i < sli.pointInfoList.Count; i++)
            {
                GL.glPointSize(skeletonNodeSize);
                GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                GL.glBegin(GL.GL_POINTS);
                GL.glVertex3d(sli.pointInfoList[i].p.x, sli.pointInfoList[i].p.y, sli.pointInfoList[i].p.z);
                GL.glEnd();
            }

            // draw lines and mark mesh triangles
            newcolor = ColorHelper.byte2float(Color.Orange);
            float[] facemark = ColorHelper.byte2float(Color.Pink);
            for (int i = 0; i < sli.lineList.Count; i++)
            {
                GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                GL.glLineWidth(2.0f);
                GL.glBegin(GL.GL_LINES);
                int p1 = sli.lineList[i].p1;
                int p2 = sli.lineList[i].p2;
                GL.glVertex3d(sli.pointInfoList[p1].p.x, sli.pointInfoList[p1].p.y, sli.pointInfoList[p1].p.z);
                GL.glVertex3d(sli.pointInfoList[p2].p.x, sli.pointInfoList[p2].p.y, sli.pointInfoList[p2].p.z);
                GL.glEnd();
            }
            //unsafe
            //{
            //    fixed (double* np = segMesh.FaceNormal)
            //    fixed (double* vp = segMesh.VertexPos)
            //    {
            //        for (int i = 0; i < sli.lineList.Count; i++)
            //        {
            //            int fi = sli.lineList[i].fid;
            //            GL.glPolygonMode(GL.GL_FRONT_AND_BACK, GL.GL_FILL);
            //            GL.glColor3f(facemark[0], facemark[1], facemark[2]);
            //            GL.glEnableClientState(GL.GL_VERTEX_ARRAY);
            //            GL.glNormal3dv(np + fi);
            //            GL.glVertex3dv(vp + segMesh.FaceIndex[fi] * 3);
            //            GL.glVertex3dv(vp + segMesh.FaceIndex[fi + 1] * 3);
            //            GL.glVertex3dv(vp + segMesh.FaceIndex[fi + 2] * 3);
            //            GL.glEnd();
            //        }
            //    }
            //}


        }
        private bool Checkchest(List<Set<int>> updateAdjVV)
        {
            for (int i = 0; i < updateAdjVV.Count; i++)
            {
                if (updateAdjVV[i].Count == 4) 
                    return true;
            }
            return false;
        }
        // end = -1 表示
        private void DFSFindTorso(SkeRecord skeRcd,int start,int end,bool[] sflag, out List<int> retl)
        {
            retl = new List<int>();
            
            bool[] sflag_internal = (bool[])sflag.Clone();
            Stack<int> SkeS = new Stack<int>();
            SkeS.Push(start);
            while (SkeS.Count != 0)
            {
                int cur = SkeS.Pop();
                retl.Add(cur);
                sflag_internal[cur] = true;
                int[] neighb = skeRcd.adjVV[cur].ToArray();
                for (int i = 0; i < neighb.Length; i++)
                {
                    int adjv_num = neighb[i];
                    if(sflag_internal[adjv_num])   continue;
                    if( skeRcd.adjVV[adjv_num].Count == 1)
                    {
                        retl.Clear();
                        retl.Add(start);    // 重新开始                
                    }
                    else if(adjv_num==end)
                    {
                        retl.Add(adjv_num);
                        for (int j = 0; j < retl.Count; j++)
                        {
                            sflag[retl[j]] = true;
                        }
                            return;
                    }
                    else { 
                           SkeS.Push(adjv_num); }                        
                }
            }  
        }
        private void DFSFindHead(SkeRecord skeRcd, int chest4v, Vector3d torsoDown, bool[] sflag, out List<int> retl)
        {
            bool[] sflag_internal = (bool[])sflag.Clone();
            int[] neighb = skeRcd.adjVV[chest4v].ToArray();
            int hi = 0; double mindot = double.MaxValue;
            for (int i = 0; i < neighb.Length; i++)
            {
                Vector3d vec_t = (skeRcd.nodePosList[chest4v] - skeRcd.nodePosList[neighb[i]]).Normalize();
                if (vec_t.Dot(torsoDown) < mindot) 
                { hi = neighb[i]; mindot = vec_t.Dot(torsoDown); }
            }
            retl = new List<int>();
            retl.Add(chest4v);
            sflag_internal[chest4v] = true;
            Stack<int> SkeS = new Stack<int>();
            SkeS.Push(hi);
            while (SkeS.Count != 0)
            {
                int cur = SkeS.Pop();
                retl.Add(cur);
                sflag_internal[cur] = true;
                int[] neighb_ = skeRcd.adjVV[cur].ToArray();
                for (int i = 0; i < neighb_.Length; i++)
                {
                    int adjv_num = neighb_[i];
                    if (sflag_internal[adjv_num]) continue;
                    if (skeRcd.adjVV[adjv_num].Count == 1)   // find end
                    {
                        retl.Add(adjv_num);
                        for (int j = 0; j < retl.Count; j++)
                        {
                            sflag[retl[j]] = true;
                        }
                        return;
                    }
                    SkeS.Push(adjv_num);
                }
            }
        }
        private void DFSFindLimb(SkeRecord skeRcd, int start, bool[] sflag, out List<int> retl)
        {

            retl = new List<int>();
            bool[] sflag_internal = (bool[])sflag.Clone();
            Stack<int> SkeS = new Stack<int>();
            SkeS.Push(start);
            while (SkeS.Count != 0)
            {
                int cur = SkeS.Pop();
                retl.Add(cur);
                sflag_internal[cur] = true;
                int[] neighb_ = skeRcd.adjVV[cur].ToArray();
                for (int i = 0; i < neighb_.Length; i++)
                {
                    int adjv_num = neighb_[i];
                    if (sflag_internal[adjv_num]) continue;
                    if (skeRcd.adjVV[adjv_num].Count == 1)   // find end
                    {
                        retl.Add(adjv_num);
                        for (int j = 0; j < retl.Count; j++)
                        {
                            sflag[retl[j]] = true;
                        }
                        return;
                    }
                    SkeS.Push(adjv_num);
                }
            }
        }
        // 把这个当作主要的处理函数
        public void CreateSlicerSequence(bodypart bdt, double interval, out List<SlicerRecord> slicerSeq) 
        {
            List<int> skeletonSeq = partSkelSeq[(int)bdt];
            #region compute interval: output pSkeLen
            double pSkeLen = 0.0;
            for (int i = 0; i < skeletonSeq.Count - 1; i++)
            {
                pSkeLen += (skeRcd.nodePosList[skeletonSeq[i + 1]] - skeRcd.nodePosList[skeletonSeq[i]]).Length();
            }
            
            #endregion 
            slicerSeq = new List<SlicerRecord>();
            Vector3d nodeNV ;
            Vector3d nodepos; 
            Plane nodePlane;
            double globalPos = 0.0;  // 整个骨架序列线段中的位置
            double skeLineAddup = 0.0;  // 单个骨架线中的位置
            int skeLineIndex = 0;  // 骨架序列索引
            // start point 
            nodeNV = skeRcd.nodePosList[skeletonSeq[1]] - skeRcd.nodePosList[skeletonSeq[0]];
            nodePlane = new Plane(skeRcd.nodePosList[skeletonSeq[0]], nodeNV);
            slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
            globalPos += interval;
            skeLineAddup = (skeRcd.nodePosList[skeletonSeq[1]] - skeRcd.nodePosList[skeletonSeq[0]]).Length();
            // medial points
            while (globalPos < pSkeLen)  // 总的不超过整个seq
            {
                int currStartIndex = skeletonSeq[skeLineIndex];
                int currEndIndex = skeletonSeq[skeLineIndex + 1];
                double currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                for (; skeLineAddup < globalPos; )
                {
                    skeLineIndex++;
                    currStartIndex = skeletonSeq[skeLineIndex];
                    currEndIndex = skeletonSeq[skeLineIndex + 1];
                    currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                    skeLineAddup += currLineLen;
                }// 循环停止的结果：skeLineAddup>=globalPos skeLineIndex指向超过globalpos的那个线段
                nodepos = (1-(skeLineAddup - globalPos) / currLineLen )*
                          (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]) +
                          skeRcd.nodePosList[currStartIndex];
                nodeNV = skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex];
                nodePlane = new Plane(nodepos, nodeNV);
                slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
                globalPos += interval; 
            }
            //end point 
            nodeNV = skeRcd.nodePosList[skeletonSeq.Count-1] - skeRcd.nodePosList[skeletonSeq.Count-2];
            nodePlane = new Plane(skeRcd.nodePosList[skeletonSeq.Last()], nodeNV);
            slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));

            SlicerSeqPruning(ref slicerSeq, bdt);

        }
        /// <summary>
        /// 与 createsequence函数搭配使用，因此在s上不需要进行分割
        /// </summary>
        /// <param name="s"></param>
        /// <param name="bdt"></param>
        private void SlicerSeqPruning(ref List<SlicerRecord> s, bodypart bdt)
        {
            switch (bdt)
            {
                case bodypart.Head : HeadSeqPrune(ref s); break;
                case bodypart.Torso: TorsoSeqPrune(ref s); break;
                case bodypart.Arm_0: ArmSeqPrune(ref s); break;
                case bodypart.Arm_1: ArmSeqPrune(ref s); break;
                case bodypart.Leg_0: LegSeqPrune(ref s); break;
                case bodypart.Leg_1: LegSeqPrune(ref s); break;
            }
        }
        // 打印的时候用
        private void LimbMidDivision(    List<SlicerRecordUniform> s ,
                                     out List<SlicerRecordUniform> su,
                                     out List<SlicerRecordUniform> sd)
        {
            int nump = s.Count;

            su = s.GetRange(0, nump / 2);
            sd = s.GetRange(nump / 2-1, nump / 2);
        }
        private void LegSeqPrune(ref List<SlicerRecord> s)
        {  // 暂时用增幅来判断，感觉有BUG
            int start;
            int outstart;
            DiffPrune(s, out start, out outstart);
            s.RemoveRange(start, outstart);
        }
        // 从胸口开始
        private void ArmSeqPrune(ref List<SlicerRecord> s)
        {
            int start;
            int outstart;
            DiffPrune(s, out start, out outstart);
            s.RemoveRange(start, outstart);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="start"></param>
        /// <param name="outstart"></param>
        /// <param name="dir"></param>
        /// dir = 1时候特征为正向最大值
        private static void DiffPrune(List<SlicerRecord> s, out int start, out int outstart, int dir = 1)
        {
            int numNd = s.Count;
            int end = numNd;
            start = 0;
            outstart = 0;
            double maxRadiusInflation = dir * double.MinValue;
            for (int i = start; i < end - 1; i++)
            {
                double radincr = s[i].radius - s[i + 1].radius;
                if (dir*(radincr - maxRadiusInflation)>0)
                {
                    maxRadiusInflation = radincr;
                    outstart = i + (dir)*1;
                }
            }
            
        }

        private void TorsoSeqPrune(ref List<SlicerRecord> s)
        {
            int start;
            int outstart;
            DiffPrune(s, out start, out outstart);
            s.RemoveRange(start, outstart);
        }

        private void HeadSeqPrune(ref List<SlicerRecord> s)
        {
            int start;
            int outstart;
            DiffPrune(s, out start, out outstart);
            s.RemoveRange(start, outstart);
        }
        /// <summary>
        /// 环切切片得到角度均匀的环片
        /// </summary>
        /// <param name="s"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        public List<SlicerRecordUniform> RadialSlicerCut(List<SlicerRecord> s,int n)
        {
            
            int[] nSeq = Enumerable.Range(0, n).ToArray();
            double[] piSeq = Enumerable.Repeat<double>(2*Math.PI,n).ToArray();
            double[] phi = piSeq.Zip(nSeq, (x, y) => x * y / n).ToArray();
            List<SlicerRecordUniform> ret = new List<SlicerRecordUniform>();
            for (int i = 0; i < s.Count; i++)
            {
                List<Vector3d> tempnp = new List<Vector3d>();
                SlicerRecord tempSR = s[i];
                Vector3d plcen = s[i].slicerCenter;
                Vector3d slicernv = s[i].slicerNormal;
                for(int k = 0 ;k < n;k++)
                {
                    Vector3d radialRay = new Vector3d(Math.Sin(phi[k]), 0, Math.Cos(phi[k])) ;
                    Vector3d radialSpinAx = new Vector3d(0, 1, 0);
                    double cost = radialSpinAx.Dot(slicernv);
                    Vector3d newRay = Vector3d.RotationRodriguesMethod(radialSpinAx.Cross(slicernv), radialRay, cost);
                    Plane halfCutPlane = new Plane(plcen,slicernv.Cross(newRay));
                    //int dir = slicernv.Dot(new Vector3d(0, 1, 0)) > 0 ? 1 : -1;
                    for (int j = 0; j < tempSR.lineList.Count; j++)
                    {
                        Vector3d p1 = tempSR.pointInfoList[tempSR.lineList[j].p1].p;
                        Vector3d p2 = tempSR.pointInfoList[tempSR.lineList[j].p2].p;
                        double tempP1Planedist = PointPlaneDistance(p1, halfCutPlane);
                        double tempP2Planedist = PointPlaneDistance(p2, halfCutPlane);
                        if (tempP2Planedist * tempP1Planedist > 0) continue;
                        Vector3d outP = (p1 * Math.Abs(tempP2Planedist) + p2 * Math.Abs(tempP1Planedist)) / (Math.Abs(tempP1Planedist) + Math.Abs(tempP2Planedist));
                        if ((outP - plcen).Dot(newRay) > 0)
                        {
                            tempnp.Add(outP);
                            break;   // test dir！
                        }
                    }
                }
                ret.Add(new SlicerRecordUniform(tempnp, plcen));
            }
            return ret;
        }
        private double PointPlaneDistance(Vector3d v, Plane p)
        {
            return (v - p.pt).Dot(p.nv);
        }
        public void WriteToSlicFile( List<SlicerRecordUniform> osl , bodypart bdt)
        {
            
            string currPath="W:\\Release\\ResultHexa\\";
            switch(bdt) 
            {
                case bodypart.Arm_0: 
                {
                        List<SlicerRecordUniform> osl1;
                        List<SlicerRecordUniform> osl2;
                    LimbMidDivision(osl,out osl1,out osl2);
                    WriteToSlicFile_single(osl1, currPath + "LeftupperArm.txt");
                    WriteToSlicFile_single(osl2, currPath + "LeftlowerArm.txt");
                    break;
                }
                case bodypart.Arm_1:
                {
                    List<SlicerRecordUniform> osl1;
                    List<SlicerRecordUniform> osl2;
                    LimbMidDivision(osl, out osl1, out osl2);
                    WriteToSlicFile_single(osl1, currPath + "RightupperArm.txt");
                    WriteToSlicFile_single(osl2, currPath + "RightlowerArm.txt");
                    break;
                }
                case bodypart.Leg_0:
                {
                    List<SlicerRecordUniform> osl1;
                    List<SlicerRecordUniform> osl2;
                    LimbMidDivision(osl, out osl1, out osl2);
                    WriteToSlicFile_single(osl1, currPath + "LeftupperLeg.txt");
                    WriteToSlicFile_single(osl2, currPath + "LeftlowerLeg.txt");
                    break;
                }
                case bodypart.Leg_1:
                {
                    List<SlicerRecordUniform> osl1;
                    List<SlicerRecordUniform> osl2;
                    LimbMidDivision(osl, out osl1, out osl2);
                    WriteToSlicFile_single(osl1, currPath + "RightupperArm.txt");
                    WriteToSlicFile_single(osl2, currPath + "RightlowerArm.txt");
                    break;
                }
                case bodypart.Head:
                {
                    WriteToSlicFile_single(osl, currPath + "Head.txt"); break;
                }
                case bodypart.Torso:
                {
                    WriteToSlicFile_single(osl, currPath + "Torso.txt"); break;
                }
            }
        }
        private void WriteToSlicFile_single(List<SlicerRecordUniform> osl, string s)
        {
            StreamWriter sw = new StreamWriter(s);
            if(osl.Count>0)
            {
                int n = osl[0].pointInfoList.Count;
                sw.WriteLine(n);
                for (int i = 0; i < osl.Count; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        sw.Write(osl[i].pointInfoList[j].x.ToString() + " ");
                        sw.Write(osl[i].pointInfoList[j].y.ToString() + " ");
                        sw.Write(osl[i].pointInfoList[j].z.ToString() + " ");
                        sw.WriteLine();
                    }
                    sw.Write(osl[i].slicerCenter.x.ToString() + " ");
                    sw.Write(osl[i].slicerCenter.y.ToString() + " ");
                    sw.Write(osl[i].slicerCenter.z.ToString() + " ");
                    sw.WriteLine();
                }
            }
            
            sw.Close();
        } 
        /// <summary>
        /// 用来将躯干与头部连接起来的方法，暂时不实现
        /// </summary>
        /// <param name="torso"></param>
        /// <param name="head"></param>
        private void ConcatenateTorsoHead(ref List<SlicerRecordUniform> torso,
                                              List<SlicerRecordUniform> head  
                                          )
        {
            
        }
        
    }
    

    
}
