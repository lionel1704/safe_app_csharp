using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using JetBrains.Annotations;
using SafeApp.AppBindings;
using SafeApp.Utilities;

// ReSharper disable ConvertToLocalFunction

namespace SafeApp.MData {
  [PublicAPI]
  public static class MData {
    private static readonly IAppBindings AppBindings = AppResolver.Current;

    public static Task<(List<byte>, ulong)> GetValueAsync(MDataInfo info, List<byte> key) {
      var tcs = new TaskCompletionSource<(List<byte>, ulong)>();
      var infoPtr = Helpers.StructToPtr(info);
      var keyPtr = key.ToIntPtr();
      Action<FfiResult, IntPtr, IntPtr, ulong> callback = (result, dataPtr, dataLen, entryVersion) => {
        if (result.ErrorCode != 0) {
          tcs.SetException(result.ToException());
          return;
        }

        var data = dataPtr.ToList<byte>(dataLen);
        tcs.SetResult((data, entryVersion));
      };

      AppBindings.MDataGetValue(Session.AppPtr, infoPtr, keyPtr, (IntPtr)key.Count, callback);
      Marshal.FreeHGlobal(keyPtr);
      Marshal.FreeHGlobal(infoPtr);

      return tcs.Task;
    }

    public static Task<NativeHandle> ListEntriesAsync(MDataInfo info) {
      var tcs = new TaskCompletionSource<NativeHandle>();
      var infoPtr = Helpers.StructToPtr(info);
      Action<FfiResult, ulong> callback = (result, mDataEntriesHandle) => {
        if (result.ErrorCode != 0) {
          tcs.SetException(result.ToException());
          return;
        }

        tcs.SetResult(new NativeHandle(mDataEntriesHandle, MDataEntries.FreeAsync));
      };

      AppBindings.MDataListEntries(Session.AppPtr, infoPtr, callback);
      Marshal.FreeHGlobal(infoPtr);

      return tcs.Task;
    }

    public static Task<List<List<byte>>> ListKeysAsync(MDataInfo mDataInfo) {
      var tcs = new TaskCompletionSource<List<List<byte>>>();
      var infoPtr = Helpers.StructToPtr(mDataInfo);
      Action<FfiResult, List<MDataKeyFfi>> callback = (result, mDataKeys) => {
        if (result.ErrorCode != 0) {
          tcs.SetException(result.ToException());
          return;
        }

        tcs.SetResult(mDataKeys.Select(k => k.DataPtr.ToList<byte>(k.Len)).ToList());
      };

      AppBindings.MDataListKeys(Session.AppPtr, infoPtr, callback);
      Marshal.FreeHGlobal(infoPtr);
      return tcs.Task;
    }

    public static Task MutateEntriesAsync(MDataInfo mDataInfo, NativeHandle entryActionsH) {
      var tcs = new TaskCompletionSource<object>();
      var infoPtr = Helpers.StructToPtr(mDataInfo);
      Action<FfiResult> callback = result => {
        if (result.ErrorCode != 0) {
          tcs.SetException(result.ToException());
          return;
        }

        tcs.SetResult(null);
      };

      AppBindings.MDataMutateEntries(Session.AppPtr, infoPtr, entryActionsH, callback);
      Marshal.FreeHGlobal(infoPtr);
      return tcs.Task;
    }

    public static Task PutAsync(MDataInfo mDataInfo, NativeHandle permissionsH, NativeHandle entriesH) {
      var tcs = new TaskCompletionSource<object>();
      Action<FfiResult> callback = result => {
        if (result.ErrorCode != 0) {
          tcs.SetException(result.ToException());
          return;
        }

        tcs.SetResult(null);
      };
      var infoPtr = Helpers.StructToPtr(mDataInfo);
      AppBindings.MDataPut(Session.AppPtr, infoPtr, permissionsH, entriesH, callback);

      return tcs.Task;
    }
  }
}
