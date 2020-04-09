using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets
{
    public struct Spline<T>
    {
        public List<T> points;
        public T[] PointsArray;
        public float [] ArcLengthsCache;
        public int ArcLengthDivisions;
        public bool Invalid;

        public delegate float DistanceFunctionType(T a, T b);
        public DistanceFunctionType DistanceFunction;
        bool Closed;

        static bool HasMethod()
        {
            var type = typeof(T);
            return type.GetMethod("Distance") != null;
        } 
        public sealed class Lambda<T>
        {
            public static Func<T, T> Cast = x => x;
        }
        public Spline(List<T> list,DistanceFunctionType df, bool closed = false)
        {
            Closed = closed;
            points = new List<T>(list);
            PointsArray = new T[4];
            ArcLengthDivisions = 200;
            ArcLengthsCache = null;
            Invalid = false;
            var Distance = typeof(T).GetMethod("Distance");
            DistanceFunction = df;//( Distance != null ? (DistanceFunctionType)Delegate.CreateDelegate(typeof(DistanceFunctionType),Distance) : (a, b)=>(float)((dynamic) a - (dynamic)b) );
        }
        public Spline(List<T> list, bool closed = false)
        {
            Closed = closed;
            points = new List<T>(list);
            PointsArray = new T[4];
            ArcLengthDivisions = 200;
            ArcLengthsCache = null;
            Invalid = false;
            var Distance = typeof(T).GetMethod("Distance");
            DistanceFunction = ( Distance != null ? (DistanceFunctionType)Delegate.CreateDelegate(typeof(DistanceFunctionType),Distance) : (a, b)=>(float)((dynamic) a - (dynamic)b) );
        }

        public Spline(T[] list, bool closed = false)
        {
            Closed = closed;
            points = new List<T>(list);
            PointsArray = new T[4];
            ArcLengthDivisions = 200;
            ArcLengthsCache = null;
            Invalid = false;
            var Distance = typeof(T).GetMethod("Distance");
            DistanceFunction = ( Distance != null ? (DistanceFunctionType)Delegate.CreateDelegate(typeof(DistanceFunctionType),Distance) : (a, b)=>(float)((dynamic) a - (dynamic)b) );

        }

        private readonly ref struct CatmullRom
        {
            private readonly T v0, v1;
            private readonly float[] t;
            private readonly Span<T> points;
            public static implicit operator T(CatmullRom v) => ((dynamic)v.points[1] * 2.0f - (dynamic)v.points[2] * 2.0f + (dynamic)v.v0 + (dynamic)v.v1) * v.t[2] + ((dynamic)v.points[1] * -3.0f + (dynamic)v.points[2] * 3.0f - (dynamic)v.v0 * 2.0f - (dynamic)v.v1) * v.t[1] + (dynamic)v.points[1] + (dynamic)v.v0 * v.t[0];

            public CatmullRom(Span<T> p, float t_)
            {
                v0 = ((dynamic)p[2] - (dynamic)p[0]) * 0.5f;
                v1 = ((dynamic)p[3] - (dynamic)p[1]) * 0.5f;
                t = new float[3];
                t[2] = (t[1] = t_ * (t[0] = t_)) * t_;
                points = p;
            }
        }

        public float GetLength()
        {
            var lengths = GetLengths();
            return lengths[lengths.Length-1];
        }
        public float[] GetLengths(int divisions)
        {
            if (ArcLengthsCache != null && (ArcLengthsCache.Length == divisions + 1) && !Invalid)
            {
                return ArcLengthsCache;
            }
            Invalid = false;

            ArcLengthsCache = new float[divisions+1];
            T current, last = points[0];
            float sum = 0.0f;
            ArcLengthsCache[0] = 0.0f;
            for (int p=1; p <= divisions; ++p)
            {
                current = GetPoint((float) p / (float) divisions);
                sum += DistanceFunction(current, last);
                ArcLengthsCache[p] = sum;
                last = current;
            }

            return ArcLengthsCache;
        }

        public float[] GetLengths()
        {
            return GetLengths(ArcLengthDivisions);
        }

        public void UpdateArcLengths()
        {
            Invalid = true;
            GetLengths();
        }

        public T GetUniformTangent(float u)
        {
            return GetTangent(GetUtoTMapping(u));
        }

        public T GetTangent(float t)
        {
            float dt     = 1.0f / 1000.0f;
            float t0     = t - dt;
            float t1     = t + dt;

            T A = GetPoint(t0);
            T B = GetPoint(t1);
            return (dynamic) B - (dynamic)A;
        }


        public float GetUtoTMapping(float u, float distance = -1.0f)
        {
            float[] arcLengths = GetLengths();
            int end = arcLengths.Length, low = 0, high = end - 1, i;
            float comparison, targetArcLength = distance == -1.0f ? u * arcLengths[end - 1] : distance;
            
            while (low <= high)
            {
                i = low + (high - low) / 2;
                comparison = arcLengths[i] - targetArcLength;
                if (comparison < 0.0f)
                {
                    low = i + 1;
                }else if (comparison > 0.0f)
                {
                    high = i - 1;
                }
                else
                {
                    high = i;
                    break;
                }
            }
            i = high;
            if (Math.Abs(arcLengths[i] - targetArcLength) == 0)
            {
                return (float)i / (float)(end - 1);
            }

            float lengthBefore = arcLengths[i];
            float lengthAfter = arcLengths[i + 1];
            float segmentFraction = (targetArcLength - lengthBefore) / (lengthAfter - lengthBefore);

            return ((float) i + segmentFraction) / ((float) end - 1.0f);

        }
        public void AddPoints(params T[] p)
        {
            Invalid = true;
            for (int i = 0; i < p.Length; ++i) AddPoint(p[i]);
        }
        public void AddPoints(List<T> points_)
        {
            Invalid = true;
            points.AddRange(points_);
        }
        public void AddPoint(T p)
        {
            Invalid = true;
            points.Add(p);
        }

        public T GetUniformPoint(float u)
        {
            return GetPoint(GetUtoTMapping(u));
        }

        private T GetPointClosed(float t)
        {
            int size = points.Count;
            float p = t * size;
            int intPoint = (int)p;
            float weight = p - intPoint;
            intPoint += intPoint > 0 ? 0 : (int)(( Mathf.Abs( intPoint ) / size ) + 1) * size; 
            PointsArray[0] = points[(intPoint - 1) % size];
            PointsArray[1] = points[intPoint % size];
            PointsArray[2] = points[ (intPoint + 1) % size];
            PointsArray[3] = points[ (intPoint + 2) % size];
            return new CatmullRom(PointsArray, weight);
        }

        private T GetPointOpen(float t)
        {
            int size = points.Count;
            float p = t * (size - 1);
            int intPoint = (int)p;
            float weight = p - intPoint;
            if (weight == 0.0f && intPoint == size - 1)
            {
                intPoint = size - 2;
                weight = 1;
            }
            PointsArray[0] = points[intPoint == 0 ? intPoint : intPoint - 1];
            PointsArray[1] = points[intPoint];
            PointsArray[2] = points[intPoint > size - 2 ? size - 1 : intPoint + 1];
            PointsArray[3] = points[intPoint > size - 3 ? size - 1 : intPoint + 2];
            return new CatmullRom(PointsArray, weight);
        }
        public T GetPoint(float t)
        {
            return Closed ? GetPointClosed(t) : GetPointOpen(t);
        }
    }
}
