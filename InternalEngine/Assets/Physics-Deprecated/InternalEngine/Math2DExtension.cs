using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace InternalEngine
{
    public struct Mat22
    {
        public Vector2 Col1;
        public Vector2 Col2;

        public Mat22(float Angle)
        {
            float C = Mathf.Cos(Angle);
            float S = Mathf.Sin(Angle);

            Col1.x = C; Col2.x = -S;
            Col1.y = S; Col2.y = C;
        }

        public Mat22(Vector2 DirVector) //Dir Vector should be normalized
        {
            float C = DirVector.x;
            float S = DirVector.y;

            Col1.x = C; Col2.x = -S;
            Col1.y = S; Col2.y = C;
        }
        public Mat22(Vector2 Col1, Vector2 Col2)
        {
            this.Col1 = Col1;
            this.Col2 = Col2;
        }

        public Mat22 Transpose()
        {
            return new Mat22(new Vector2(Col1.x, Col2.x), new Vector2(Col1.y, Col2.y));
        }

        public Mat22 Invert()
	    {
            float a = Col1.x, b = Col2.x, c = Col1.y, d = Col2.y;
            Mat22 B;
            float det = a * d - b * c;
            det = 1.0f / det;
		    B.Col1.x =  det* d; B.Col2.x = -det* b;
            B.Col1.y = -det* c; B.Col2.y =  det* a;
		    return B;
	    }

        public static Vector2 operator *(Mat22 A, Vector2 B)
        {
            return new Vector2(A.Col1.x * B.x + A.Col2.x * B.y, A.Col1.y * B.x + A.Col2.y * B.y);
        }

        public static Mat22 operator +(Mat22 A, Mat22 B)
        {
            return new Mat22(A.Col1 + B.Col1, A.Col2 + B.Col2);
        }
    }

    public class Math2D
    {
        public static Vector2 Cross(Vector2 A, float B)
        {
            return new Vector2(B * A.y, -B * A.x);
        }

        public static Vector2 Cross(float A, Vector2 B)
        {
            return new Vector2(-A * B.y, A * B.x);
        }

        public static float Cross(Vector2 A, Vector2 B)
        {
            return A.x * B.y - A.y * B.x;
        }
    }
}
