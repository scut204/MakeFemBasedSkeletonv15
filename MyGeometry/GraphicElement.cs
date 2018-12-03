using System;
using System.Collections.Generic;
using System.Text;

namespace MyGeometry
{
    public struct Plane
    {
        public Vector3d pt;
        public Vector3d nv;
        public Plane(Vector3d p,Vector3d n)
        {
            pt = p;
            nv = n.Normalize();
        }
            
    }
}
