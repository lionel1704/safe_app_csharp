#pragma warning disable CS0649

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SafeApp.Utilities {
  public class FfiException : Exception {
    public readonly int ErrorCode;

    public FfiException(int code, string description) : base($"Error Code: {code}. Description: {description}") {
      ErrorCode = code;
    }
  }

  public struct FfiResult {
    public int ErrorCode;
    [MarshalAs(UnmanagedType.LPStr)] public string Description;

    public FfiException ToException() {
      return new FfiException(ErrorCode, Description);
    }
  }

  internal class BindingUtils {
    public static void CompleteTask<T>(TaskCompletionSource<T> tcs, ref FfiResult result, T arg) {
      if (result.ErrorCode != 0) {
        tcs.SetException(result.ToException());
      } else {
        tcs.SetResult(arg);
      }
    }

    public static void CompleteTask<T>(IntPtr userData, ref FfiResult result, T arg) {
      var tcs = FromHandlePtr<TaskCompletionSource<T>>(userData);
      CompleteTask(tcs, ref result, arg);
    }

    public static void CompleteTask(IntPtr userData, ref FfiResult result) {
      CompleteTask(userData, ref result, true);
    }

    public static IntPtr CopyFromByteArray(byte[] array) {
      var size = Marshal.SizeOf(array[0]) * array.Length;
      var ptr = Marshal.AllocHGlobal(size);
      Marshal.Copy(array, 0, ptr, array.Length);

      return ptr;
    }

    public static IntPtr CopyFromObjectArray<T>(T[] array) {
      var size = Marshal.SizeOf(array[0]) * array.Length;
      var ptr = Marshal.AllocHGlobal(size);
      for (var i = 0; i < array.Length; ++i) {
        Marshal.StructureToPtr(array[i], IntPtr.Add(ptr, Marshal.SizeOf<T>() * i), false);
      }

      return ptr;
    }

    public static byte[] CopyToByteArray(IntPtr ptr, IntPtr len) {
      var array = new byte[(int)len];
      Marshal.Copy(ptr, array, 0, (int)len);

      return array;
    }

    public static T[] CopyToObjectArray<T>(IntPtr ptr, IntPtr len) {
      var array = new T[(int)len];
      for (var i = 0; i < (int)len; ++i) {
        array[i] = Marshal.PtrToStructure<T>(IntPtr.Add(ptr, Marshal.SizeOf<T>() * i));
      }

      return array;
    }

    public static void FreeArray(ref IntPtr ptr, ref IntPtr len) {
      if (ptr != IntPtr.Zero) {
        Marshal.FreeHGlobal(ptr);
      }

      ptr = IntPtr.Zero;
      len = IntPtr.Zero;
    }

    public static T FromHandlePtr<T>(IntPtr ptr, bool free = true) {
      var handle = GCHandle.FromIntPtr(ptr);
      var result = (T)handle.Target;

      if (free) {
        handle.Free();
      }

      return result;
    }

    public static (Task<T>, IntPtr) PrepareTask<T>() {
      var tcs = new TaskCompletionSource<T>();
      var userData = ToHandlePtr(tcs);

      return (tcs.Task, userData);
    }

    public static (Task, IntPtr) PrepareTask() {
      return PrepareTask<bool>();
    }

    public static IntPtr ToHandlePtr<T>(T obj) {
      return GCHandle.ToIntPtr(GCHandle.Alloc(obj));
    }
  }
}