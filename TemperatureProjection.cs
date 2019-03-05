using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using MyGeometry;
using System.Diagnostics;
using System.IO;
namespace IsolineEditing
{
    public struct ColorPoint
    {
        public Vector3d pos;
        public double Tsk;
        public int faceIndex;
        public ColorPoint(double x,double y,double z,double T)
        {
            pos = new Vector3d(x, y, z);
            Tsk = T;
            faceIndex = 0;
        }
        public ColorPoint(Vector3d p, double T, int fi)
        {
            pos = p;
            Tsk = T;
            faceIndex = fi;
        }
    }
    public class TemperatureProjection
    {
        List<ColorPoint> inputcp;

        public TemperatureProjection(Mesh sh)
        {
            string path = "";
            ReadTemperatureResultFile(path);
            GenerateProjectPoints(sh);
            ComputeOriginalMeshColor(sh);      // 将产生的投射点与Mesh三点的距离作为权重
            DisplayColoredModel();
        }
        private void ReadTemperatureResultFile(string path)
        {
            StreamReader sw = new StreamReader(path);




            sw.Close();
        }
        public void DisplayColoredModel()
        {
            //throw new NotImplementedException();
        }

        private void ComputeOriginalMeshColor(Mesh sh)
        {
            //this.color = new float[faceCount * 3]; Mesh的颜色结构与面相符合
            float[] faceColor = sh.Color;   // 放到最后进行重新组合
            int vertexCount = sh.VertexCount;
            float[] vertexColor = new float[vertexCount];
            float[] colorWeight = new float[vertexCount];
            for (int i = 0; i< inputcp.Count; i++)
            {
                int j = inputcp[i].faceIndex;
                for(int k = 0; k < 3; k++)
                {
                    int vi = sh.FaceIndex[j * 3 + k];
                    Vector3d vp = new Vector3d(sh.VertexPos, sh.FaceIndex[j * 3 + k] * 3);
                    vertexColor[vi] = (float)(vertexColor[vi] * colorWeight[vi] + (1.0 / (vp - inputcp[i].pos).Length()) * inputcp[i].Tsk);
                    colorWeight[vi] += (float)(1.0 / (vp - inputcp[i].pos).Length());
                }
            }
        }

        private void GenerateProjectPoints(Mesh sh)
        {
            for (int i = 0; i < inputcp.Count; i++)
            {
                Vector3d oriSkinPos = inputcp[i].pos;
                double minDist = Double.MaxValue;
                for (int j = 0; j < sh.FaceCount; j++)
                {
                    Vector3d[] vp ={ new Vector3d(sh.VertexPos, sh.FaceIndex[j * 3] * 3)  // 前面是face的点的索引，后面*3是点的pos索引
                                    ,new Vector3d(sh.VertexPos, sh.FaceIndex[j * 3+1] * 3 )
                                    ,new Vector3d(sh.VertexPos, sh.FaceIndex[j * 3+2] * 3 )};
                    Vector3d fn = new Vector3d(sh.FaceNormal, j * 3);  // 需要测试
                    Vector3d pointOnPlane = (vp[0] - oriSkinPos).Dot(fn) * fn + oriSkinPos; // get point on plane
                    double dist = Math.Abs((vp[0] - oriSkinPos).Dot(fn));
                    if (PointInsideTriangle(pointOnPlane, vp) && dist < minDist)
                    {
                        minDist = dist;
                        inputcp[i] = new ColorPoint(pointOnPlane, inputcp[i].Tsk,j);
                    }
                }
            }
            // 到这里为止得到所有点的面索引
        }
        private bool PointInsideTriangle(Vector3d pO, Vector3d[] vp)
        {
            Vector3d[] cvTri =
            {
                (vp[0]-pO).Cross(vp[1]-pO),
                (vp[1]-pO).Cross(vp[2]-pO),
                (vp[2]-pO).Cross(vp[0]-pO),
            };
            if (
                cvTri[0].Dot(cvTri[1]) < 0 ||
                cvTri[1].Dot(cvTri[2]) < 0 ||
                cvTri[2].Dot(cvTri[0]) < 0
                ) return false;
            else return true;
        }


    }
}
