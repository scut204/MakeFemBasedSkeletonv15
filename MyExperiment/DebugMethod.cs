using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGeometry ;
using CsGL.OpenGL;
using System.Drawing;

namespace IsolineEditing
{
    /// <summary>
    /// 用来给即使窗口用的debug方法类
    /// </summary>
    public static class DebugMethod
    {
        public static Color[] dbColor = new Color[6]{
                             Color.BlanchedAlmond,
                             Color.Brown,
                             Color.DarkGreen,
                             Color.Gray,
                             Color.LightPink,
                             Color.Red };
    public static List<Vector3d> ps;
        public static List<List<SlicerRecord>> slicerAll;
        public static List<List<SlicerRecordUniform>> slicerUniformAll;
        public static string ToReadableString(byte[] data)
        {
            int length = data.Length;
            var sb = new StringBuilder(length);
            for (int index = 0; index < length; ++index)
            {
                char ch = (char)data[index];
                sb.Append(Char.IsControl(ch) ? '.' : ch);
            }
            return sb.ToString();
        }
        public static void ListToReadableString(List<SlicerRecord> data)
        {
            int length = data.Count;
            for (int index = 0; index < length; ++index)
            {

            }

        }
        public static List<Vector3d> GetVList(List<SlicerRecord> sl, string prop)
        {
            List<Vector3d> ret = new List<Vector3d>();
            for (int i = 0; i < sl.Count; i++)
            {
                switch (prop)
                {
                    case "slicerNormal": ret.Add(sl[i].slicerNormal); break;
                    case "slicerCenter": ret.Add(sl[i].slicerCenter); break;
                }
                //ret.Add(sl[i].slicerNormal)
            }
            return ret;
        }

        public static void DisplayVertexArray()
        {
            if (ps == null) return;
            float[] newcolor = ColorHelper.byte2float(Color.Orange);
            for (int i = 0; i < ps.Count; i++)
            {
                GL.glPointSize(6);
                GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                GL.glBegin(GL.GL_POINTS);
                GL.glVertex3d(ps[i].x, ps[i].y, ps[i].z);
                GL.glEnd();
            }
        }
        public static void DisplaySlicerArray()
        {
            if (slicerAll == null) return;
            for (int i = 0; i < slicerAll.Count; i++)
            {
                for (int j = 0; j < slicerAll[i].Count; j++)
                {
                    DisplaySlicer(slicerAll[i][j]);
                }
            }
        }
        public static void DisplaySlicerUniformArray()
        {
            if (slicerUniformAll == null) return;
            for (int i = 0; i < slicerUniformAll.Count; i++)
            {
                for (int j = 0; j < slicerUniformAll[i].Count; j++)
                {
                    DisplaySlicer(slicerUniformAll[i][j]);
                }
            }
        }
        private static void DisplaySlicer(SlicerRecord sli)
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
        }
        private static void DisplaySlicer(SlicerRecordUniform sli)
        {
            int bd = sli.lable;   
            float skeletonNodeSize = 6.0f;
            float[] newcolor = ColorHelper.byte2float(Color.Gold);

            // draw center 
            GL.glPointSize(skeletonNodeSize);
            GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
            GL.glBegin(GL.GL_POINTS);
            GL.glVertex3d(sli.slicerCenter.x, sli.slicerCenter.y, sli.slicerCenter.z);
            GL.glEnd();
            newcolor = ColorHelper.byte2float(Color.Black);

            // draw points
            newcolor = ColorHelper.byte2float(dbColor[bd]);
            for (int i = 0; i < sli.pointInfoList.Count; i++)
            {
                GL.glPointSize(skeletonNodeSize);
                GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                GL.glBegin(GL.GL_POINTS);
                GL.glVertex3d(sli.pointInfoList[i].x, sli.pointInfoList[i].y, sli.pointInfoList[i].z);
                GL.glEnd();
            }

            // draw lines and mark mesh triangles
            newcolor = ColorHelper.byte2float(dbColor[bd]);
            float[] facemark = ColorHelper.byte2float(Color.Pink);
            for (int i = 0; i < sli.numRadialPoint; i++)
            {
                GL.glColor3f(newcolor[0], newcolor[1], newcolor[2]);
                GL.glLineWidth(2.0f);
                GL.glBegin(GL.GL_LINES);
                GL.glVertex3d(sli.pointInfoList[i].x, sli.pointInfoList[i].y, sli.pointInfoList[i].z);
                GL.glVertex3d(sli.pointInfoList[(i + 1) % (sli.numRadialPoint)].x,
                              sli.pointInfoList[(i + 1) % (sli.numRadialPoint)].y, 
                              sli.pointInfoList[(i + 1) % (sli.numRadialPoint)].z);
                GL.glEnd();
            }
        }
    }
}
