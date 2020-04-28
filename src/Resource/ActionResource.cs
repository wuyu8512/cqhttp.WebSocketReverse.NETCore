using cqhttp.WebSocketReverse.NETCore.Resource;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cqhttp.WebSocketReverse.NETCore.Resource
{
    public static class ActionResource
    {
        /// <summary>
        /// 回调等待暂存
        /// </summary>
        public static readonly ConcurrentDictionary<string, TaskCompletionSource<ResponseResource>> Operations
            = new ConcurrentDictionary<string, TaskCompletionSource<ResponseResource>>();
    }
}
