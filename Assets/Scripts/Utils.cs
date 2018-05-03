using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils {

    public static float[] getBezierFloats(float[] baseVals, int size)
    {
        float h = 1.0f / (float)(size - 1);
        float h_2 = h * h;

        //          A0                   B0              C0       D0
        // t^3(-p0+3p1-3p2+p3) + t^2(3p0-6p1+3p2) + t(-3p0+3p1) + p0
        float A0 = baseVals[0] * -1.0f + baseVals[1] * 3.0f + baseVals[2] * -3.0f + baseVals[3];
        float B0 = baseVals[0] * 3.0f + baseVals[1] * -6.0f + baseVals[2] * 3.0f;
        float C0 = baseVals[0] * -3.0f + baseVals[1] * 3.0f;

        //      A1            B1               C1
        // t^2(3A0h) + t(3A0h^2+2B0h) + (A0h^3+B0h^2+C0h)
        float A1 = A0 * 3.0f * h;
        float B1 = A0 * 3.0f * h_2 + B0 * 2.0f * h;
        float C1 = A0 * h * h_2 + B0 * h_2 + C0 * h;

        //    A2          B2
        // t(2A1h) + (A1h^2+B1h)
        float A2 = A1 * 2.0f * h;
        float B2 = A1 * h_2 + B1 * h;

        //  A3
        // (A2h)
        float A3 = A2 * h;


        // D1 = C1
        float D1 = C1;

        // D2 = B2
        float D2 = B2;

        // D3 = A3
        float D3 = A3;

        float[] vals = new float[size];
        vals[0] = baseVals[0];

        for (int i = 1; i < size; i++)
        {
            vals[i] = vals[i - 1] + D1;
            D1 += D2;
            D2 += D3;
        }

        return vals;
    }


    public static Vector3[] getBezierPoints(Vector3[] basePts, int size)
    {
        float h = 1.0f / (float)(size - 1);
        float h_2 = h * h;

        //          A0                   B0              C0       D0
        // t^3(-p0+3p1-3p2+p3) + t^2(3p0-6p1+3p2) + t(-3p0+3p1) + p0
        Vector3 A0 = basePts[0] * -1.0f + basePts[1] * 3.0f + basePts[2] * -3.0f + basePts[3];
        Vector3 B0 = basePts[0] * 3.0f + basePts[1] * -6.0f + basePts[2] * 3.0f;
        Vector3 C0 = basePts[0] * -3.0f + basePts[1] * 3.0f;

        //      A1            B1               C1
        // t^2(3A0h) + t(3A0h^2+2B0h) + (A0h^3+B0h^2+C0h)
        Vector3 A1 = A0 * 3.0f * h;
        Vector3 B1 = A0 * 3.0f * h_2 + B0 * 2.0f * h;
        Vector3 C1 = A0 * h * h_2 + B0 * h_2 + C0 * h;

        //    A2          B2
        // t(2A1h) + (A1h^2+B1h)
        Vector3 A2 = A1 * 2.0f * h;
        Vector3 B2 = A1 * h_2 + B1 * h;

        //  A3
        // (A2h)
        Vector3 A3 = A2 * h;


        // D1 = C1
        Vector3 D1 = C1;

        // D2 = B2
        Vector3 D2 = B2;

        // D3 = A3
        Vector3 D3 = A3;

        Vector3[] pts = new Vector3[size];
        pts[0] = basePts[0];

        for (int i = 1; i < size; i++)
        {
            pts[i] = pts[i - 1] + D1;
            D1 += D2;
            D2 += D3;
        }

        return pts;
    }

    public static Vector3[] getBezierPointTangents(Vector3[] basePts, int size)
    {
       
        float h = 1.0f / (float)(size - 1);
        float h_2 = h * h;

        //          A0                   B0              C0    
        // t^2(-3p0+9p1-9p2+3p3) + t(6p0-12p1+6p2) + (-3p0+3p1)
        Vector3 A0 = basePts[0] * -3.0f + basePts[1] * 9.0f + basePts[2] * -9.0f + basePts[3] * 3.0f;
        Vector3 B0 = basePts[0] * 6.0f + basePts[1] * -12.0f + basePts[2] * 6.0f;
        Vector3 C0 = basePts[0] * -3.0f + basePts[1] * 3.0f;

        //      A1        B1
        // t(2A0h) + (A0h^2+B0h)
        Vector3 A1 = A0 * 2.0f * h;
        Vector3 B1 = A0 * h_2 + B0 * h;

        //  A2 
        // A1h
        Vector3 A2 = A1 *  h;


        // D1 = C1
        Vector3 D1 = B1;

        // D2 = B2
        Vector3 D2 = A2;


        Vector3[] pts = new Vector3[size];
        pts[0] = C0;

        for (int i = 1; i < size; i++)
        {
            pts[i] = pts[i - 1] + D1;
            D1 += D2;
        }

        return pts;
    }


    public static void getYZSphericalCoordinates(Transform baseTrans, Transform targetTrans, out float y, out float z)
    {
        Vector3 toNode = targetTrans.position - baseTrans.position;
        toNode.Normalize();

        Vector3 projX = Vector3.Dot(toNode, baseTrans.right) * baseTrans.right;
        Vector3 projZ = Vector3.Dot(toNode, baseTrans.forward) * baseTrans.forward;


        Vector3 projZX = projX + projZ;
        projZX.Normalize();
        y = Mathf.Acos(Vector3.Dot(baseTrans.right, projZX)) * 180f / Mathf.PI;
        if (Vector3.Dot(Vector3.Cross(projZX, baseTrans.right), baseTrans.up) > 0f) y = -y;

        Quaternion firstRotation = Quaternion.Euler(0f, y, 0f);
        Vector3 t_right = firstRotation * baseTrans.right;
        t_right.Normalize();
        Vector3 t_up = firstRotation * baseTrans.up;
        t_up.Normalize();
        Vector3 t_forward = firstRotation * baseTrans.forward;
        t_forward.Normalize();

        projX = Vector3.Dot(toNode, t_right) * t_right;
        Vector3 projY = Vector3.Dot(toNode, t_up) * t_up;


        Vector3 projXY = projX + projY;
        projXY.Normalize();
        z = Mathf.Acos(Vector3.Dot(t_right, projXY)) * 180f / Mathf.PI;
        if (Vector3.Dot(Vector3.Cross(t_right, projXY), t_forward) < 0f) z = -z;
    }
}
