/*
 * 
 * Author: Sincerelord2
 *
 * Modifying this code is not allowed without the permission of the author.
 * 
 * License: MIT License
 * 
 * Description:
 *      Helper to calulate object sizes in bytes.
 * 
 */

using System.Runtime.InteropServices;

namespace NetFlexor.ServiceHelpers
{
    public static class ObjectSizeCalculator
    {
        public static long GetObjectSizes(object obj)
        {
            if (!obj.GetType().IsSerializable)
            {
                throw new ArgumentException("The type must be serializable.", nameof(obj));
            }

            return Marshal.SizeOf(obj);
        }
        public static long GetObjectSize(object obj)
        {
            return GetObjectSize(obj, new HashSet<object>());
        }

        private static long GetObjectSize(object tempObj, HashSet<object> visited)
        {
            var obj = tempObj;
            if (obj == null)
            {
                return 0;
            }

            long size = 0;
            if (visited.Contains(obj))
            {
                return size;
            }

            visited.Add(obj);

            size += IntPtr.Size;
            if (obj is string str)
            {
                size += str.Length * 2;
            }
            else if (obj is Array array)
            {
                foreach (var element in array)
                {
                    size += GetObjectSize(element, visited);
                }
            }
            else if (obj is IEnumerable<object> enumerable)
            {
                foreach (var element in enumerable)
                {
                    size += GetObjectSize(element, visited);
                }
            }
            else
            {
                var fields = obj.GetType().GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                foreach (var field in fields)
                {
                    size += GetObjectSize(field.GetValue(obj), visited);
                }
            }

            return size;
        }
    }
}
