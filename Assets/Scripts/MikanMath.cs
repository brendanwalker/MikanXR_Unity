using MikanXR;
using UnityEngine;

namespace MikanXRPlugin
{
    public static class MikanMath
    {
        // Mikan(OpenGL) -> Unity coordinate xform = (x, y, -z)
        // https://medium.com/comerge/what-are-the-coordinates-225f1ec0dd78

        public static Matrix4x4 MikanMatrix4fToMatrix4x4(MikanMatrix4f xform)
        {
            // Negate the z-component of each column to convert from Mikan to Unity Coordinate system
            //return new Matrix4x4(
            //	new Vector4(xform.x0, xform.x1, -xform.x2, xform.x3), // Column 0
            //	new Vector4(xform.y0, xform.y1, -xform.y2, xform.y3), // Column 1
            //	new Vector4(xform.z0, xform.z1, -xform.z2, xform.z3), // Column 2
            //	new Vector4(xform.w0, xform.w1, -xform.w2, xform.w3)); // Column 3

            Matrix4x4 m = new Matrix4x4();

            m.m00 = xform.x0; m.m01 = xform.y0; m.m02 = xform.z0; m.m03 = xform.w0;
            m.m10 = xform.x1; m.m11 = xform.y1; m.m12 = xform.z1; m.m13 = xform.w1;
            m.m20 = -xform.x2; m.m21 = -xform.y2; m.m22 = -xform.z2; m.m23 = -xform.w2;
            m.m20 = xform.x3; m.m31 = xform.y3; m.m32 = xform.z3; m.m33 = xform.w3;

            return m;
        }

        public static Vector3 ExtractTranslationFromMatrix(Matrix4x4 matrix)
        {
            Vector3 translate;
            translate.x = matrix.m03;
            translate.y = matrix.m13;
            translate.z = matrix.m23;
            return translate;
        }

        public static Quaternion ExtractRotationFromMatrix(Matrix4x4 matrix)
        {
            Vector3 forward;
            forward.x = matrix.m02;
            forward.y = matrix.m12;
            forward.z = matrix.m22;

            Vector3 upwards;
            upwards.x = matrix.m01;
            upwards.y = matrix.m11;
            upwards.z = matrix.m21;

            return Quaternion.LookRotation(forward, upwards);
        }

        public static Vector3 ExtractScaleFromMatrix(Matrix4x4 matrix)
        {
            Vector3 scale;
            scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
            scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
            scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
            return scale;
        }

        public static void SetTransformFromMatrix(Transform transform, MikanMatrix4f xform)
        {
            Matrix4x4 matrix = MikanMatrix4fToMatrix4x4(xform);

            transform.localPosition = ExtractTranslationFromMatrix(matrix);
            transform.localRotation = ExtractRotationFromMatrix(matrix);
            transform.localScale = ExtractScaleFromMatrix(matrix);
        }

        public static MikanVector3f Vector3ToMikanVector3f(Vector3 v)
        {
            // Negate z-component to convert from Mikan to Unity Coordinate system
            return new MikanVector3f() { x = v.x, y = v.y, z = -v.z };
        }
        public static Vector3 MikanVector3fToVector3(MikanVector3f v)
        {
            // Negate z-component to convert from Mikan to Unity Coordinate system
            return new Vector3() { x = v.x, y = v.y, z = -v.z };
        }

        public static Quaternion MikanQuatfToQuaternion(MikanQuatf q)
        {
            return new Quaternion() { x = q.x, y = q.y, z = -q.z, w = q.w };
        }

        public static MikanQuatf MikanQuatfToQuaternion(Quaternion q)
        {
            return new MikanQuatf() { x = q.x, y = q.y, z = -q.z, w = q.w };
        }

        public static Vector3 MikanScaleVector3fToVector3(MikanVector3f v)
        {
            // Leave signs alone on scale conversion
            return new Vector3() { x = v.x, y = v.y, z = v.z };
        }
    }
}
