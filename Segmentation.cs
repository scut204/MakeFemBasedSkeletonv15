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
            int numSkSeq = 160; // 骨架的分段数量
            if (skel != null)
            {
                skeRcd = skel.ReturnSkeleton(); // get skeleton from sig08
                interval = (double)skeRcd.skeLength / numSkSeq;
                SkeletonRefining(interval);   // refine
                SegmentToPart();      //get partskelseq 是关于骨架的操作
            }
        }

        public void SegPostProcess()
        {
            DebugMethod.slicerUniformAll = new List<List<SlicerRecordUniform>>();
            DebugMethod.slicerAll = new List<List<SlicerRecord>>();
            bodypart[] bdparr = (bodypart[])Enum.GetValues(typeof(bodypart));
            List<SlicerRecord>[] bodySlicerGroup = new List<SlicerRecord>[(int)bodypart.PartCount];
            for (int i = 0; i < (int)bodypart.PartCount; i++)
            {
                List<SlicerRecord> slicerSeq;
                bodypart bdt = bdparr[i];
                CreateSlicerSequence(bdt, interval, out slicerSeq);
                bodySlicerGroup[i] = slicerSeq;
            }
            AddtionCreateSlicer(bodySlicerGroup);
            for (int i = 0; i < (int)bodypart.PartCount; i++)
            {
                List<SlicerRecord> slicerSeq = bodySlicerGroup[i];
                int n = (int)bodypart.Torso == i ? 30 :    //  密集 30 10 10 18
                        (int)bodypart.Arm_0 == i ? 10 :  
                        (int)bodypart.Arm_1 == i ? 10 : 18;
                bodypart bdt = bdparr[i];
                List<SlicerRecordUniform> radialSlicer = RadialSlicerCut(slicerSeq, n);
                // TrickForSegment.PostEliminateChestGap(radialSlicer, bdparr[i]);
                // display
                foreach (SlicerRecordUniform sru in radialSlicer) sru.lable = i;
                DebugMethod.slicerUniformAll.Add(radialSlicer);
                //DebugMethod.slicerAll.Add(slicerSeq);
                WriteToSlicFile(radialSlicer, bdt);
            }
        }
        private void AddtionCreateSlicer(List<SlicerRecord>[] bodySlicerGroup)  // 增加大腿与手臂
        {
            SlicerRecord bottomTorso = bodySlicerGroup[(int)bodypart.Torso].Last();
            SlicerRecord leg0top = bodySlicerGroup[(int)bodypart.Leg_0].First();
            SlicerRecord leg1top = bodySlicerGroup[(int)bodypart.Leg_1].First();
            
            SlicerRecord chestshoul = TrickForSegment.chestshoul;
            TrickForSegment.BuildSeperateLimbSlicer(leg0top, leg1top, out SlicerRecord nLeg0, out SlicerRecord nLeg1);

            // Arm 
            SlicerRecord arm0top = bodySlicerGroup[(int)bodypart.Arm_0].First();
            SlicerRecord arm1top = bodySlicerGroup[(int)bodypart.Arm_1].First();
            
            //for(int i =0;i< bodySlicerGroup[(int)bodypart.Torso].Count; i++)
            //{
                TrickForSegment.BuildTorsoWithArmCutting(bodySlicerGroup[(int)bodypart.Torso], ref arm0top);
                TrickForSegment.BuildTorsoWithArmCutting(bodySlicerGroup[(int)bodypart.Torso], ref arm1top);
            bodySlicerGroup[(int)bodypart.Arm_0][0] = arm0top;
            bodySlicerGroup[(int)bodypart.Arm_1][0] = arm1top;
            //TrickForSegment.BuildTorsoSlicerCutByArm(torsoI, arm1top, out torsoI);
            //bodySlicerGroup[(int)bodypart.Torso][i] = torsoI;
            //}
            //TrickForSegment.BuildNewArmTopslicer(chestshoul, arm0top, out SlicerRecord nArm0);
            //TrickForSegment.BuildNewArmTopslicer(chestshoul, arm1top, out SlicerRecord nArm1);
            //GetMostMatchedArmSlicer(ref chestshoul,bodySlicerGroup[(int)bodypart.Arm_0][1],ref nArm0);
            //GetMostMatchedArmSlicer(ref chestshoul,bodySlicerGroup[(int)bodypart.Arm_1][1],ref nArm1);


            bodySlicerGroup[(int)bodypart.Head][0] = new SlicerRecord(bodySlicerGroup[(int)bodypart.Torso][0]); // 将躯干的slicer置换到头部
            bodySlicerGroup[(int)bodypart.Head][0].slicerNormal = -bodySlicerGroup[(int)bodypart.Head][0].slicerNormal;
            bodySlicerGroup[(int)bodypart.Leg_0][0] = nLeg0;
            bodySlicerGroup[(int)bodypart.Leg_1][0] = nLeg1;

            // add torso slicer in the bottom of torso 
            TrickForSegment.BuildCombinedTorsoSlicer(leg0top, leg1top, out SlicerRecord torsoRoot);
            List<SlicerRecord> Torsoseq = new List<SlicerRecord>();
            Vector3d rootCentre = torsoRoot.slicerCenter;
            int currLoopTimes = (int)(Math.Floor((rootCentre - bottomTorso.slicerCenter).Length() / interval));
            double currInterval = (rootCentre - bottomTorso.slicerCenter).Length() / currLoopTimes;
            for(int i = 1; i < currLoopTimes; i++)
            {
                Vector3d refSkeNode = rootCentre + ((double)i / currLoopTimes) * (bottomTorso.slicerCenter - rootCentre);
                Vector3d refNodeNormal0 = ((double)i / currLoopTimes) * bottomTorso.slicerNormal +
                                         (1-(double)i / currLoopTimes) * leg0top.slicerNormal;
                Vector3d refNodeNormal1 = ((double)i / currLoopTimes) * bottomTorso.slicerNormal +
                                         (1-(double)i / currLoopTimes) * leg1top.slicerNormal;
                Plane nodePlane0 = new Plane(refSkeNode, refNodeNormal0);
                Plane nodePlane1 = new Plane(refSkeNode, refNodeNormal1);
                SlicerRecord t0 = new SlicerRecord(segMesh, nodePlane1);
                SlicerRecord t1 = new SlicerRecord(segMesh, nodePlane0);
                TrickForSegment.BuildCombinedTorsoSlicer(t0, t1, out SlicerRecord torsoIntp);
                Torsoseq.Add(torsoIntp);
            }
            Torsoseq.Reverse();
            Torsoseq.Add(torsoRoot);
            bodySlicerGroup[(int)bodypart.Torso].AddRange(Torsoseq);
        }

        // 被上面函数引用，用来计算手臂的slicer
        private void GetMostMatchedArmSlicer(ref SlicerRecord chestshoul, SlicerRecord Aend, ref SlicerRecord nArm0)
        {
            double target = Math.Abs(Aend.radius - nArm0.radius);
            SlicerRecord ret = null;
            int currLoopTimes = 100;
            Vector3d rootCentre = Aend.slicerCenter;
            double currInterval = (rootCentre - nArm0.slicerCenter).Length() / currLoopTimes;
            for (int i = 0; i < currLoopTimes; i++)
            {
                Vector3d refSkeNode = rootCentre + ((double)i / currLoopTimes) * (nArm0.slicerCenter - rootCentre);
                Vector3d refNodeNormal0 = ((double)i / currLoopTimes) * Aend.slicerNormal +
                                         (1 - (double)i / currLoopTimes) * nArm0.slicerNormal;
                Plane nodePlane0 = new Plane(refSkeNode, refNodeNormal0);
                SlicerRecord t1 = new SlicerRecord(segMesh, nodePlane0);
                TrickForSegment.BuildNewArmTopslicer(chestshoul, t1, out SlicerRecord torsoIntp);
                if(torsoIntp != null && Math.Abs(Aend.radius - torsoIntp.radius) < target)
                {
                    target = Math.Abs(Aend.radius - torsoIntp.radius);
                    ret = torsoIntp;
                }
            }
            if(ret != null)
            {
                nArm0 = ret;
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
            slicerSeq = new List<SlicerRecord>();
            if (bdt == bodypart.Arm_0 || bdt == bodypart.Arm_1)
            {
                CreateArmSlicerSequence(bdt, interval, slicerSeq);
                return;
            }
            List<int> skeletonSeq = partSkelSeq[(int)bdt];
            #region compute interval: output pSkeLen
            double pSkeLen = 0.0;
            for (int i = 0; i < skeletonSeq.Count - 1; i++)
            {
                pSkeLen += (skeRcd.nodePosList[skeletonSeq[i + 1]] - skeRcd.nodePosList[skeletonSeq[i]]).Length();
            }
            interval = pSkeLen / (Math.Floor(pSkeLen / interval)) + interval / 200;
            #endregion 
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
            // 总的不超过整个seq
            do
            {
                int currStartIndex = skeletonSeq[skeLineIndex];
                int currEndIndex = skeletonSeq[skeLineIndex + 1];
                double currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                for (; skeLineAddup < globalPos;)
                {
                    skeLineIndex++;
                    currStartIndex = skeletonSeq[skeLineIndex];
                    currEndIndex = skeletonSeq[skeLineIndex + 1];
                    currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                    skeLineAddup += currLineLen;
                }// 循环停止的结果：skeLineAddup>=globalPos skeLineIndex指向超过globalpos的那个线段
                nodepos = (1 - (skeLineAddup - globalPos) / currLineLen) *
                          (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]) +
                          skeRcd.nodePosList[currStartIndex];
                nodeNV = skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex];
                nodePlane = new Plane(nodepos, nodeNV);
                slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
                globalPos += interval;
            } while (globalPos  < pSkeLen);
                //end point 
            nodeNV = skeRcd.nodePosList[skeletonSeq[skeletonSeq.Count-1]] - skeRcd.nodePosList[skeletonSeq[skeletonSeq.Count-2]];
            nodePlane = new Plane(skeRcd.nodePosList[skeletonSeq.Last()], nodeNV);
            slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
            SlicerSeqPruning(ref slicerSeq, bdt);
        }
        /// <summary>
        /// 这个函数是用来对手臂上的分片进行特殊处理的，是CreateSlicerSequence的分支，后面还需要对躯干与手臂进行交叉
        /// </summary>
        /// <param name="bdt"></param>
        /// <param name="interval"></param>
        /// <param name="slicerSeq"></param>
        private void CreateArmSlicerSequence(bodypart bdt,double interval, List<SlicerRecord> slicerSeq)
        {
            List<int> skeletonSeq = partSkelSeq[(int)bdt];
            Vector3d torso2ArmDir = skeRcd.nodePosList[skeletonSeq[1]] - skeRcd.nodePosList[skeletonSeq[0]];  // 得到初始的法向量

            Vector3d armEndDir    = skeRcd.nodePosList[skeletonSeq[skeletonSeq.Count - 1]] - skeRcd.nodePosList[skeletonSeq[skeletonSeq.Count -2]];  // 得到末端的法向量
            #region get the length of skeleton and compute two intervals of relative slicer sknode.
            double pSkeLen = 0.0;
            for (int i = 0; i < skeletonSeq.Count - 1; i++)
            {
                pSkeLen += (skeRcd.nodePosList[skeletonSeq[i + 1]] - skeRcd.nodePosList[skeletonSeq[i]]).Length();
            }
            interval = pSkeLen / (Math.Floor(pSkeLen / interval)) + interval / 200;
            double oxterInterval = interval / 5;  // 寻找腋窝的interval 细化
            #endregion
            //Vector3d nodeNV;
            Vector3d nodepos;
            Plane nodePlane;
            List<KeyValuePair<int,int>> skeEndIndex = new List<KeyValuePair<int, int>>();
            // 因为起点的slicer并不需要所以直接舍弃 
            double globalPos = oxterInterval;  // 整个骨架序列线段中的位置
            double skeLineAddup = 0.0;  // 单个骨架线中的位置
            int skeLineIndex = 0;  // 骨架序列索引
            do
            {
                int currStartIndex = 0;
                int currEndIndex = 1;
                double currLineLen = (skeRcd.nodePosList[1] - skeRcd.nodePosList[0]).Length();
                for (; skeLineAddup < globalPos; skeLineIndex++)
                {
                    currStartIndex = skeletonSeq[skeLineIndex];
                    currEndIndex = skeletonSeq[skeLineIndex + 1];
                    currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                    skeLineAddup += currLineLen;
                }// 循环停止的结果：skeLineAddup>=globalPos skeLineIndex指向超过globalpos的那个线段
                if (skeEndIndex.Count == 0  || currEndIndex != skeEndIndex.Last().Key)
                {
                    skeEndIndex.Add(new KeyValuePair<int, int>(currEndIndex,skeLineIndex-1));
                    nodepos = (1 - (skeLineAddup - globalPos) / currLineLen) *
                              (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]) +
                              skeRcd.nodePosList[currStartIndex];
                    //nodeNV = skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]; 这里注释了
                    nodePlane = new Plane(nodepos, torso2ArmDir);
                    slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
                }
                globalPos += oxterInterval;
            } while (globalPos < pSkeLen);
            // find oxter slicer 
            Diff2rdPrune(slicerSeq, out int outstart);  // outstart 就是腋窝的index

            // 进行手臂的分片生成
            SlicerRecord temp = slicerSeq[outstart];
            slicerSeq.Clear();
            slicerSeq.Add(temp);
            double armLength = 0.0;
            for (int i = skeEndIndex[outstart].Value; i < skeletonSeq.Count - 1; i++)
            {
                armLength += (skeRcd.nodePosList[skeletonSeq[i + 1]] - skeRcd.nodePosList[skeletonSeq[i]]).Length();
            }
            double foreArmLength = armLength /2;
            interval = armLength / (Math.Floor(armLength / interval)) + interval / 200;
            Vector3d nodeNV;
            globalPos = interval;  // 整个骨架序列线段中的位置
            skeLineAddup = 0.0;  // 单个骨架线中的位置
            skeLineIndex = skeEndIndex[outstart].Value;  // 骨架序列索引
            do
            {
                int currStartIndex = skeletonSeq[skeLineIndex];
                int currEndIndex = skeletonSeq[skeLineIndex + 1];
                double currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                for (; skeLineAddup < globalPos; skeLineIndex++)
                {
                    currStartIndex = skeletonSeq[skeLineIndex];
                    currEndIndex = skeletonSeq[skeLineIndex + 1];
                    currLineLen = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Length();
                    skeLineAddup += currLineLen;
                }// 循环停止的结果：skeLineAddup>=globalPos skeLineIndex指向超过globalpos的那个线段
                nodepos = (1 - (skeLineAddup - globalPos) / currLineLen) *
                          (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]) +
                          skeRcd.nodePosList[currStartIndex];
                if (skeLineAddup < foreArmLength)
                {
                    nodeNV = (skeLineAddup * (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]) +
                            (foreArmLength - skeLineAddup) * torso2ArmDir) / (foreArmLength); //这里注释了
                }
                else nodeNV = (skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Normalize();

                nodePlane = new Plane(nodepos, nodeNV);
                slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
                globalPos += interval * (1 / nodeNV.Normalize().Dot((skeRcd.nodePosList[currEndIndex] - skeRcd.nodePosList[currStartIndex]).Normalize()));
            } while (globalPos < armLength);
            // final cut slicer 
            nodeNV = skeRcd.nodePosList[skeletonSeq[skeletonSeq.Count - 1]] - skeRcd.nodePosList[skeletonSeq[skeletonSeq.Count - 2]];
            nodePlane = new Plane(skeRcd.nodePosList[skeletonSeq.Last()], nodeNV);
            slicerSeq.Add(new SlicerRecord(segMesh, nodePlane));
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
                case bodypart.Head :/* HeadSeqPrune(ref s);*/ break;
                case bodypart.Torso:
                    {
                        int ct;
                        TorsoSeqPrune(s, out ct);
                        TrickForSegment.chest = new Plane(s[ct].skeletonNodepos, s[ct].slicerNormal);
                        TrickForSegment.navel = new Plane(s.Last().skeletonNodepos, s.Last().slicerNormal);
                        break;
                    }
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
            int mid = (int)Math.Ceiling((double)nump / 2);
            su = s.GetRange(0, mid);
            sd = s.GetRange(mid - 1, nump - mid + 1);
        }
        private void LegSeqPrune(ref List<SlicerRecord> s)
        {  // 暂时用增幅来判断，感觉有BUG
            int start;
            int outstart;
            DiffPrune(s, out start, out outstart);
            s.RemoveRange(start, outstart-1);
        }
        // 从胸口开始
        private void ArmSeqPrune(ref List<SlicerRecord> s)
        {
            int start;
            int outstart;
            DiffPrune(s, out start, out outstart);
            s.RemoveRange(start, outstart-1);
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
        private static void Diff2rdPrune(List<SlicerRecord> s, out int outstart)
        {
            int numNd = s.Count;
            int end = numNd;
            int start = 1;
            outstart = 1;
            double maxRadiusInflation = double.MinValue;   // 二次偏导最大值
            for (int i = start; i < end - 1; i++)
            {
                //if((s[i + 1].radius- s[i].radius)* (s[i].radius - s[i - 1].radius) < 0)
                //{
                //    outstart = i;
                //    return;
                //}
                double radincr =  (s[i + 1].radius * s[i-1].radius) / (s[i].radius * s[i].radius);
                if ((radincr - maxRadiusInflation) > 0)
                {
                    maxRadiusInflation = radincr;
                    start = i;
                }
            }
            // 这里找到增大的半径
            for(int i = start;i<end - 1; i++)
            {
                if(s[i+1].radius -s[i].radius >0)
                {
                    outstart = i;return;
                }
            }
        }

        private void TorsoSeqPrune(List<SlicerRecord> s, out int outstart)
        {
            int start;
            //int outstart;
            DiffPrune(s, out start, out outstart);
            outstart = outstart - 1;
            TrickForSegment.chestshoul = s[outstart]; // 这里是chestshoul 用来指定胸部位置
            //s.RemoveRange(start, outstart);
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

                Vector3d radialSpinAx = new Vector3d(0, 0, 1);
                double cost = radialSpinAx.Dot(slicernv);  // cost is cos(t)
                //List<Vector3d> debugNormal = new List<Vector3d>();
                for (int k = 0 ;k < n;k++)
                {
                    double maxDist = double.MinValue;
                    
                    Vector3d radialRay = new Vector3d(Math.Cos(phi[k]), Math.Sin(phi[k]), 0) ;
                    Vector3d radialPlaneNormal = new Vector3d(Math.Cos(phi[k]), 0, -Math.Sin(phi[k]));
                    Vector3d newRay = Vector3d.RotationRodriguesMethod(radialSpinAx.Cross(slicernv), radialRay, cost).Normalize();
                    Plane halfCutPlane = new Plane(plcen, newRay.Cross(slicernv));
                    //debugNormal.Add(newRay);
                    //int dir = slicernv.Dot(new Vector3d(0, 1, 0)) > 0 ? 1 : -1;
                    // 这个阶段是为了找到隔断的点
                    Vector3d maxOutP = new Vector3d();
                    for (int j = 0; j < tempSR.lineList.Count; j++)
                    {
                        Vector3d p1 = tempSR.pointInfoList[tempSR.lineList[j].p1].p;
                        Vector3d p2 = tempSR.pointInfoList[tempSR.lineList[j].p2].p;
                        double tempP1Planedist = PointPlaneDistance(p1, halfCutPlane);
                        double tempP2Planedist = PointPlaneDistance(p2, halfCutPlane);
                        if (tempP2Planedist * tempP1Planedist > 0) continue;  // 检测交叉
                        Vector3d outP = (p1 * Math.Abs(tempP2Planedist) + p2 * Math.Abs(tempP1Planedist)) / (Math.Abs(tempP1Planedist) + Math.Abs(tempP2Planedist));
                        if ((outP - plcen).Dot(newRay) < 0) continue;   // 检测射线方向
                        if ((outP - plcen).Length() > maxDist)
                        {
                            maxDist = (outP - plcen).Length();
                            maxOutP = outP;
                        }
                    }
                    if(maxOutP != (Vector3d) new Vector3d())
                        tempnp.Add(maxOutP);
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
                    WriteToSlicFile_single(osl1, currPath + "RightupperLeg.txt");
                    WriteToSlicFile_single(osl2, currPath + "RightlowerLeg.txt");
                    break;
                }
                case bodypart.Head:
                {
                    osl.Reverse();
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
                for (int i = osl.Count-1; i >= 0; i--)   // reverse to make from bottom to top
                {
                    for (int j = n -1; j >= 0; j--) // reverse to make the detJ > 0
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
