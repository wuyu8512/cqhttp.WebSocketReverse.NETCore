﻿using cqhttp.WebSocketReverse.NETCore.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace cqhttp.WebSocketReverse.NETCore.Model.Params
{
    /// <summary>
    /// 获取群信息
    /// </summary>
    internal class GetGroupInfoParams : IParams
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonPropertyName("group_id")]
        public long GroupId { get; set; }

        /// <summary>
        /// 是否不使用缓存（使用缓存可能更新不及时，但响应更快）
        /// </summary>
        [JsonPropertyName("no_cache")]
        public bool NoCache { get; set; }
    }
}
