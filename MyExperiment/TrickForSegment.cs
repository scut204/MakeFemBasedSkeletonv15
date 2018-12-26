using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGeometry;

namespace IsolineEditing
{
    public static class Utility
    {

    }
    
    public static class TrickForSegment
    {
        public static int torsoCutArmIndex;
        public static Plane chest;
        public static Plane navel;
        public static SlicerRecord leg0top = new SlicerRecord();
        public static SlicerRecord leg1top = new SlicerRecord();
        public static SlicerRecord arm0top = new SlicerRecord();
        public static SlicerRecord arm1top = new SlicerRecord();
        public static SlicerRecord chestshoul = new SlicerRecord();

        public static SlicerRecordUniform ProjectOntoSlicer(Plane targetPlane, SlicerRecordUniform source, Vector3d projectRay)
        {
            List<Vector3d> lv = new List<Vector3d>();
            Vector3d sc = new Vector3d();
            projectRay = projectRay.Normalize();
            double cost = - projectRay.Dot(targetPlane.nv); // Geometry may tell more;
            for(int i = 0; i < source.numRadialPoint; i++)
            {
                Vector3d posP = source.pointInfoList[i];
                double moveRate = PointPlaneDistance(posP, targetPlane);
                posP += projectRay * moveRate / cost;
                lv.Add(posP);
            }
            for (int i = 0; i < lv.Count; i++) sc += lv[i] / lv.Count;
            return new SlicerRecordUniform(lv, sc);
        }
        public static void PostEliminateChestGap(List<SlicerRecordUniform> lsu, Segmentation.bodypart bdt)
        {
            SlicerRecordUniform lsu0 = lsu[0];
            Vector3d slRay = lsu[0].SlicerUniformNormal();
            if (bdt == Segmentation.bodypart.Arm_0 || bdt == Segmentation.bodypart.Arm_1)
            {
                lsu.Insert(0, ProjectOntoSlicer(chest, lsu0, slRay));
            }
            if(bdt == Segmentation.bodypart.Leg_0 || bdt == Segmentation.bodypart.Leg_1)
            {
                lsu.Insert(0, ProjectOntoSlicer(navel, lsu0, navel.nv));
            }
        }
        private static double PointPlaneDistance(Vector3d v, Plane p) => (v - p.pt).Dot(p.nv);
        public static void BuildSeperateLimbSlicer( SlicerRecord s1,
                                                    SlicerRecord s2,
                                                    out SlicerRecord outS1,
                                                    out SlicerRecord outS2)
        {
            if (CreateTwoHalfSlicer(s1, s2, -(s1.slicerNormal + s2.slicerNormal).Normalize(),
                        out HalfSlicerRecord s1h, out HalfSlicerRecord s2h))
            {
                outS1 = new SlicerRecord(s1h);
                outS2 = new SlicerRecord(s2h);
            }
            else
            {
                outS1 = null;
                outS2 = null;
            }
        }
        public static void BuildCombinedTorsoSlicer(SlicerRecord s1,
                                                    SlicerRecord s2,
                                                    out SlicerRecord outS)
        {
            if (CreateTwoHalfSlicer(s1, s2, -(s1.slicerNormal + s2.slicerNormal).Normalize(),
                                    out HalfSlicerRecord s1h, out HalfSlicerRecord s2h))
            {
                outS = s1h.CombineHalfSlicer(s2h);
            }
            else outS = null;
        }
        public static void BuildNewArmTopslicer(SlicerRecord ts,
                                                SlicerRecord arms,
                                                out SlicerRecord outS)
        {
            if(CreateTwoHalfSlicer(ts,arms,-(ts.slicerNormal + arms.slicerNormal).Normalize(),
                                   out HalfSlicerRecord s1h, out HalfSlicerRecord armh))
            {
                outS = new SlicerRecord(armh);
            }
            else
            {
                outS = null;
            }
        }
        /// <summary>
        /// 创建半平面
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <param name="intsecList"></param>
        /// <returns></returns>
        public static bool CreateTwoHalfSlicer( SlicerRecord s1,
                                                SlicerRecord s2,
                                                Vector3d guider,
                                                out HalfSlicerRecord s1c,
                                                out HalfSlicerRecord s2c)
        {
            s1c = null;
            s2c = null;
            List<int> s1MeshList = new List<int>(s1.lineList.Select(l => l.fid));
            List<int> s2MeshList = new List<int>(s2.lineList.Select(l => l.fid));
            List<int> crossFiList = new List<int>(s1MeshList.Intersect(s2MeshList));
            if (crossFiList.Count < 2) return false;
            List<int> crossFiListRet = new List<int>();
            List<Vector3d> intsecList = new List<Vector3d>();
            for (int i = 0; i < crossFiList.Count; i++)
            {
                int s1i = s1.lineList.FindIndex(linelist => linelist.fid == crossFiList[i]);
                int s2i = s2.lineList.FindIndex(linelist => linelist.fid == crossFiList[i]);
                Vector3d p1 = s1.pointInfoList[s1.lineList[s1i].p1].p;
                Vector3d p2 = s1.pointInfoList[s1.lineList[s1i].p2].p;
                Vector3d p3 = s2.pointInfoList[s2.lineList[s2i].p1].p;
                Vector3d p4 = s2.pointInfoList[s2.lineList[s2i].p2].p;
                if (CrossPointOfTwoLines(p1, p2, p3, p4, out Vector3d crossP))
                {
                    intsecList.Add(crossP);
                    crossFiListRet.Add(crossFiList[i]);
                }
            }
            s1c = OutHalfSlicer(s1, guider, crossFiListRet);
            s2c = OutHalfSlicer(s2, guider, crossFiListRet);
            if (s1c == null || s2c == null) return false;
            // s1c 处理
            ModifyTerminalPoint(s1c, crossFiListRet, intsecList);
            ModifyTerminalPoint(s2c, crossFiListRet, intsecList);
            return true;
        }

        private static void ModifyTerminalPoint(HalfSlicerRecord s1c, List<int> crossFiListRet, List<Vector3d> intsecList)
        {
            if (s1c.lineList[0].fid == crossFiListRet[0]) // 起始点为开头
            {
                s1c.pointInfoList[0] = new SlicerRecord.PointRecord(intsecList[0], crossFiListRet[0]);
                s1c.pointInfoList[s1c.pointInfoList.Count - 1] = new SlicerRecord.PointRecord(intsecList[1], crossFiListRet[1]);
            }
            else
            {
                s1c.pointInfoList[0] = new SlicerRecord.PointRecord(intsecList[1], crossFiListRet[1]);
                s1c.pointInfoList[s1c.pointInfoList.Count - 1] = new SlicerRecord.PointRecord(intsecList[0], crossFiListRet[0]);
            }
        }

        // 建立需要的halfslicer
        private static HalfSlicerRecord OutHalfSlicer(SlicerRecord slicer,Vector3d guider,List<int> commonFaceI)
        {
            if (commonFaceI.Count != 2) return null;

            int cnt = slicer.lineList.Count;
            int start = -1;
            int end = -1;
            Vector3d midmidp = new Vector3d();
            List<int> tempstart = new List<int>();
            for (int i = 0; i < cnt; i++)  // 确定start与end两个交叉面的
            {
                
                int fid = slicer.lineList[i].fid;
                if (commonFaceI.Contains(fid))
                {
                    tempstart.Add(i);
                    midmidp += 0.25 * (slicer.pointInfoList[i].p + slicer.pointInfoList[(i + 1)%cnt].p);
                }
            }
            start = tempstart[0];
            end = tempstart[1];

            HalfSlicerRecord hfslicer1;
            HalfSlicerRecord hfslicer2;
            if (start == 0)
            {
                hfslicer1 = new HalfSlicerRecord(slicer, 0, end);
                hfslicer2 = new HalfSlicerRecord(slicer, end, cnt-1);
            }
            else
            {
                hfslicer1 = new HalfSlicerRecord(slicer, 0, start);
                hfslicer2 = new HalfSlicerRecord(slicer, end, cnt-1);
                hfslicer1 = hfslicer2 + hfslicer1;
                hfslicer2 = new HalfSlicerRecord(slicer, start, end);
            }
            Vector3d h1AverageP = hfslicer1.pointInfoList[hfslicer1.pointInfoList.Count / 2].p;
            Vector3d h2AverageP = hfslicer2.pointInfoList[hfslicer2.pointInfoList.Count / 2].p;
            return guider.Dot(h1AverageP - midmidp) > guider.Dot(h2AverageP - midmidp) ? hfslicer1 : hfslicer2;
        }
        private static bool CrossPointOfTwoLines(Vector3d a1, Vector3d a2, Vector3d b1, Vector3d b2, out Vector3d crossP)
        {
            // simple return wait for modify
            crossP = new Vector3d();
            double x1 = a1.x, x2 = a2.x, x3 = b1.x, x4 = b2.x;
            double y1 = a1.y, y2 = a2.y, y3 = b1.y, y4 = b2.y;
            double z1 = a1.z, z2 = a2.z, z3 = b1.z, z4 = b2.z;
            if ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4) == 0.0) return false;
            double t = ((x1 - x3) * (y3 - y4) - (y1 - y3) * (x3 - x4)) / 
                       ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            double u = - ((x1 - x2) * (y1 - y3) - (y1 - y2) * (x1 - x3)) /
                         ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            if (t > 1.0 || t < 0.0 || u > 1.0 || u < 0.0) return false;
            else
            {
                crossP = new Vector3d(x1 + t * (x2 - x1),
                                      y1 + t * (y2 - y1),
                                      z1 + t * (z2 - z1));
                return true;
            }
        }
        
    }
}
