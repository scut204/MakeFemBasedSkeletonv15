using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGeometry;

namespace IsolineEditing
{
    public static class TrickForSegment
    {
        public static int torsoCutArmIndex;
        public static Plane chest;
        public static Plane navel;
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
    }
}
