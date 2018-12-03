using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGeometry ;

namespace IsolineEditing
{
    /// <summary>
    /// 用来给即使窗口用的debug方法类
    /// </summary>
    public static class DebugMethod
    {
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
        public static List<Vector3d> GetVList(List<SlicerRecord> sl,string prop)
        {
            List<Vector3d> ret = new List<Vector3d>();
            for (int i = 0; i < sl.Count; i++)
            {
                switch (prop)
                {
                    case "slicerNormal" : ret.Add(sl[i].slicerNormal); break;
                    case "slicerCenter" : ret.Add(sl[i].slicerCenter); break; 
                }
                //ret.Add(sl[i].slicerNormal)
            }
            return ret;
        }
    }

}
