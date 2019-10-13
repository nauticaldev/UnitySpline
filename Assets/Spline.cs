using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    class Spline<T> 
    {
        List<T> points;
        private T[] PointArray = new T[4];

        public Spline(List<T> list)
        {
            points = new List<T>(list);
        }

        public Spline(T[] list)
        {
            points = new List<T>(list);
        }

        private struct CatmullRom
        {
            private T v0, v1;
            private float[] t;
            private T[] points;

            public static implicit operator T(CatmullRom v) => ((dynamic)v.points[1] * 2.0f - (dynamic)v.points[2] * 2.0f + (dynamic)v.v0 + (dynamic)v.v1) * v.t[2] + ((dynamic)v.points[1] * -3.0f + (dynamic)v.points[2] * 3.0f - (dynamic)v.v0 * 2.0f - (dynamic)v.v1) * v.t[1] + (dynamic)v.v0 * v.t[0] + (dynamic)v.points[1];

            public CatmullRom(ref T[] p, float t_)
            {
                v0 = ((dynamic)p[2] - (dynamic)p[0]) * 0.5f;
                v1 = ((dynamic)p[3] - (dynamic)p[1]) * 0.5f;
                t = new float[3];
                t[2] = (t[1] = t[0] * (t[0] = t_)) * t[0];
                points = p;
            }

        }

        public void AddPoints(params T[] p) 
        {
            for (int i = 0; i < p.Length; ++i) AddPoint(p[i]);
        }
        public void AddPoints(List<T> points_)
        {
            points.AddRange(points_);
        }
        public void AddPoint(T p)
        {
            points.Add(p);
        }

        public T GetPoint(float t)
        {
            int size = points.Count;
            float p = t * (size - 1);
            int intPoint = (int)p;
            float weight = p - intPoint;
            PointArray[0] = points[intPoint == 0 ? intPoint : intPoint - 1];
            PointArray[1] = points[intPoint];
            PointArray[2] = points[intPoint > size - 2 ? size - 1 : intPoint + 1];
            PointArray[3] = points[intPoint > size - 3 ? size - 1 : intPoint + 2];
            return new CatmullRom(ref PointArray, weight);
        }

    }
}
