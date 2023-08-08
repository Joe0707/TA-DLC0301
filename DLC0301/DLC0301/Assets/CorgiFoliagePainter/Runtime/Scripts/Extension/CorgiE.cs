// if you need to disable unsafe code, simply comment out this define here and uncheck "allow unsafe code" from the assembly definition file 
#define CORGI_ALLOW_UNSAFE_CODE 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEditor;

namespace CorgiFoliagePainter.Extensions
{
    public static class CorgiE 
    {
        public static Vector3 GetPositionFromMatrix(this Matrix4x4 matrix)
        {
            // added in Unity 2021.2
#if UNITY_2021_2_OR_NEWER
            return matrix.GetPosition();
#endif

            var column3 = matrix.GetColumn(3);
            return new Vector3(column3.x, column3.y, column3.z);
        }

        public static Quaternion GetRotationFromMatrix(this Matrix4x4 matrix)
        {
            return matrix.rotation;
        }

        public static Vector3 GetScaleFromMatrix(this Matrix4x4 matrix)
        {
            return matrix.lossyScale;
        }

        public static Quaternion GetQuaternionSafe(this Quaternion quaternion)
        {
            var quatAsVec = new float4(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
            
            if (math.lengthsq(quatAsVec) < 0.00001f || math.any(math.isinf(quatAsVec)) || math.any(math.isnan(quatAsVec)))
            {
                return Quaternion.identity;
            }

            return quaternion;
        }

        public static void Shuffle<T>(this T[] array)
        {
            var random = new System.Random();
            var n = array.Length;
            while (n > 1)
            {
                var k = random.Next(n--);
                var temp = array[n];

                array[n] = array[k];
                array[k] = temp;
            }
        }
    
#if CORGI_ALLOW_UNSAFE_CODE
        public static unsafe void CopyToFast<T>(this NativeArray<T> nativeArray, T[] array, int length) where T : struct
        {
            var byteLength = length * UnsafeUtility.SizeOf(typeof(T));
            var managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            var nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
        }

        public static unsafe void CopyFromFast<T>(this NativeArray<T> nativeArray, T[] array, int length) where T : struct
        {
            var byteLength = length * UnsafeUtility.SizeOf(typeof(T));
            var managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            var nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
        }

        public static unsafe void CopyFromFast<T>(this NativeArray<T> nativeArray, T[] array, int length, int offsetFrom) where T : struct
        {
            var byteLength = length * UnsafeUtility.SizeOf(typeof(T));
            var managedBuffer = UnsafeUtility.AddressOf(ref array[offsetFrom]);
            var nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
        }
#else
    public static void CopyToFast<T>(this NativeArray<T> nativeArray, T[] managedArray, int length) where T : struct
        {
            for (var i = 0; i < length; ++i)
            {
                managedArray[i] = nativeArray[i];
            }
        }

        public static void CopyFromFast<T>(this NativeArray<T> nativeArray, T[] managedArray, int length) where T : struct
        {
            for (var i = 0; i < length; ++i)
            {
                nativeArray[i] = managedArray[i];
            }
        }

        public static void CopyFromFast<T>(this NativeArray<T> nativeArray, T[] managedArray, int length, int offsetFrom) where T : struct
        {
            for (var i = 0; i < length; ++i)
            {
                nativeArray[i] = managedArray[i + offsetFrom];
            }
        }
#endif
    }
}
