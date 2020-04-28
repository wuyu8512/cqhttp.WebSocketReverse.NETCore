using System;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using cqhttp.WebSocketReverse.NETCore.Model;
using cqhttp.WebSocketReverse.NETCore.Content;
using cqhttp.WebSocketReverse.NETCore.Resource;
using cqhttp.WebSocketReverse.NETCore.Enumeration;
using cqhttp.WebSocketReverse.NETCore.Model.Event;
using cqhttp.WebSocketReverse.NETCore.Model.Response;

namespace cqhttp.WebSocketReverse.NETCore
{
    public class CqHttpParse
    {
        public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs;
        public event AsyncEventHandler<ConnectEventArgs> OnConnected;
        public event AsyncEventHandler<CqHttpMessageEventArgs> OnPrivateMessage;
        public event AsyncEventHandler<CqHttpMessageEventArgs> OnGroupMessage;
        public event AsyncEventHandler<CqHttpMessageEventArgs> OnDiscussMessage;
        public event AsyncEventHandler<CqHttpMessageEventArgs> OnMessage;
        public event AsyncEventHandler<CqGroupNoticEventArgs> OnGroupNotic;
        public event AsyncEventHandler<CqFriendAddEventArgs> OnFriendAdd;
        public event AsyncEventHandler<CqFriendRequestEventArgs> OnFriendRequest;
        public event AsyncEventHandler<CqGroupRequestEventArgs> OnGroupRequest;
        public event AsyncEventHandler<StatusEventArgs> OnStatus;
        public event AsyncEventHandler<ResponseEventArgs> OnResponse;
        public event AsyncEventHandler<ResponseEventArgs> OnErrorResponse;

        private readonly Action<string, ResponseResource> SetResult;
        public CqHttpParse(Action<string, ResponseResource> setResult)
        {
            this.SetResult = setResult;
        }

        public async Task<ResponseResource> ParseResponse(Source source, JsonElement element)
        {
            string status = "", echo = "";
            var response = JsonSerializer.Deserialize<CqHttpContent>(element.GetRawText());
            if (element.TryGetProperty("retcode", out JsonElement je_rc) && je_rc.ValueKind == JsonValueKind.Number && je_rc.TryGetInt32(out int retcode))
            {
                if (element.TryGetProperty("status", out JsonElement je_status)) { status = je_status.GetString(); }
                if (element.TryGetProperty("echo", out JsonElement je_echo)) { echo = je_echo.GetString(); }
                ResponseResource data = new ResponseResource() { Retcode = retcode };
                switch (status)
                {
                    case "ok":
                    case "async":
                        if (element.TryGetProperty("data", out JsonElement je_data) == false) { break; }
                        if (je_echo.ValueKind != JsonValueKind.String) { break; }
                        if (je_data.ValueKind == JsonValueKind.Null) { break; }
                        if (OnResponse != null)
                            await OnResponse(source.SelfId, new ResponseEventArgs(source, response));
                        await ResponseParse(echo, je_data, data);
                        break;
                    case "failed":
                        if (retcode < 1) { break; }
                        if (OnErrorResponse != null)
                            await OnErrorResponse(source.SelfId, new ResponseEventArgs(source, response));
                        data.IsFailed = true;
                        this.SetResult(echo, data);
                        break;
                }
                return data;
            }
            return null;
        }
        public async Task<ResponseResource> ResponseParse(string echo, JsonElement jData, ResponseResource data)
        {
            return await Task.Run(() =>
            {
                if (echo.StartsWith('~') == false) { return data; }
                if (echo.Contains('@') == false) { return data; }
                data.IsInVaild = false;
                string[] ctx = echo.TrimStart('~').Split('@');
                RequestType task = (RequestType)System.Enum.Parse(typeof(RequestType), ctx[0]);

                switch (task)
                {
                    case RequestType.SendMsg:
                        if (jData.TryGetProperty("message_id", out JsonElement je_mid) == false) { break; }
                        if (je_mid.ValueKind != JsonValueKind.Number) { break; }
                        data.MessageId = je_mid.GetInt32();
                        break;
                    case RequestType.GetLoginInfo:
                        data.LoginInfo = JsonSerializer.Deserialize<LoginInfo>(jData.GetRawText());
                        break;
                    case RequestType.GetStrangerInfo:
                        data.QQInfo = JsonSerializer.Deserialize<QQInfo>(jData.GetRawText());
                        break;
                    case RequestType.GetGroupInfo:
                        data.GroupList = new List<Group>();
                        data.GroupList.Add(JsonSerializer.Deserialize<Group>(jData.GetRawText()));
                        break;
                    case RequestType.GetGroupList:
                        data.GroupList = JsonSerializer.Deserialize<IList<Group>>(jData.GetRawText());
                        break;
                    case RequestType.GetGroupMemberInfo:
                        data.GroupMemberList = new List<GroupMemberInfo>();
                        data.GroupMemberList.Add(JsonSerializer.Deserialize<GroupMemberInfo>(jData.GetRawText()));
                        break;
                    case RequestType.GetGroupMemberList:
                        data.GroupMemberList = JsonSerializer.Deserialize<IList<GroupMemberInfo>>(jData.GetRawText());
                        break;
                    case RequestType.GetCookies:
                    case RequestType.GetCredentials:
                        data.Credentials = JsonSerializer.Deserialize<Credentials>(jData.GetRawText());
                        break;
                    case RequestType.GetCsrfToken:
                        if (jData.TryGetProperty("token", out JsonElement je_token)) 
                        {
                            if(je_token.TryGetInt32(out int token))
                            {
                                data.Credentials = new Credentials() { CsrfToken = token };
                            }
                        }
                        break;
                    case RequestType.GetRecord:
                    case RequestType.GetImage:
                        data.File = JsonSerializer.Deserialize<FileInfo>(jData.GetRawText());
                        break;
                    case RequestType.CanSendImage:
                        if (jData.TryGetProperty("yes", out JsonElement je_yes_image))
                        {
                            if (je_yes_image.ValueKind == JsonValueKind.True || je_yes_image.ValueKind == JsonValueKind.False)
                            {
                                data.CanSendImage = je_yes_image.GetBoolean();
                            }
                        }
                        break;
                    case RequestType.CanSendRecord:
                        if (jData.TryGetProperty("yes", out JsonElement je_yes_record))
                        {
                            if (je_yes_record.ValueKind == JsonValueKind.True || je_yes_record.ValueKind == JsonValueKind.False)
                            {
                                data.CanSendRecord = je_yes_record.GetBoolean();
                            }
                        }
                        break;
                    case RequestType.GetStatus:
                        data.Status = JsonSerializer.Deserialize<CqHttpStatus>(jData.GetRawText());
                        break;
                    case RequestType.GetVersionInfo:
                        data.Version = JsonSerializer.Deserialize<CqHttpVersion>(jData.GetRawText());
                        break;
                    case RequestType.GetFriendList:
                        data.FriendGroupList = JsonSerializer.Deserialize<IList<FriendGroup>>(jData.GetRawText());
                        break;
                    case RequestType.GetVipInfo:
                        data.QQInfo = JsonSerializer.Deserialize<QQInfo>(jData.GetRawText());
                        break;
                    case RequestType.DeleteMsg:
                    case RequestType.SendLike:
                    case RequestType.SetGroupKick:
                    case RequestType.SetGroupBan:
                    case RequestType.SetGroupAnonymousBan:
                    case RequestType.SetGroupWholeBan:
                    case RequestType.SetGroupAdmin:
                    case RequestType.SetGroupAnonymous:
                    case RequestType.SetGroupCard:
                    case RequestType.SetGroupLeave:
                    case RequestType.SetGroupSpecialTitle:
                    case RequestType.SetDiscussLeave:
                    case RequestType.SetFriendAddRequest:
                    case RequestType.SetGroupAddRequest:
                    case RequestType.SetRestart:
                    case RequestType.SetRestartPlugin:
                    case RequestType.CleanDataDir:
                    case RequestType.CleanPluginLog:
                    case RequestType.CheckUpdate:
                    case RequestType.HandleQuickOperation:
                        break;
                }
                this.SetResult(echo, data);
                return data;
            });
        }
        public async Task Parse(object selfId, MessageEventArgs pack)
        {
            using (var docs = JsonDocument.Parse(pack.Message))
            {
                await Task.Run(async () =>
                {
                    DateTime receivedDate = DateTime.Now;
                    if (long.TryParse(selfId as string, out long sid) == false) { return; }
                    if (docs.RootElement.TryGetProperty("time", out JsonElement je_time))
                    {
                        if (je_time.ValueKind == JsonValueKind.Number)
                        {
                            if (je_time.TryGetInt64(out long timestamp)) 
                            {
                                receivedDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).AddHours(8).DateTime;
                            }
                        }
                    }
                    Source source = new Source(sid, receivedDate, pack);
                    if (docs.RootElement.TryGetProperty("post_type", out JsonElement je_ptype)==false)
                    {
                        await ParseResponse(source, docs.RootElement);
                        return;
                    }
                    if (je_ptype.ValueKind != JsonValueKind.String) { return; }
                    switch (je_ptype.GetString())
                    {
                        case "meta_event":
                            MetaEvent(docs.RootElement, source);
                            break;
                        case "message":
                            MessageEvent(docs.RootElement, source);
                            break;
                        case "notice":
                            NoticEvent(docs.RootElement, source);
                            break;
                        case "request":
                            RequestEvent(docs.RootElement, source);
                            break;
                    }
                });
            }
        }
        private async void StatusEvent(JsonElement element, Source source)
        {
    
            if (OnStatus != null)
            {
                StatusEventArgs sea = new StatusEventArgs(source, await ParseResponse(source, element));
                if (OnStatus != null)
                    await OnStatus(source.SelfId, sea);
            }
        }
        private async void RequestEvent(JsonElement element, Source source)
        {
            if (element.TryGetProperty("request_type", out JsonElement type))
            {
                long userid;
                string flag, comment;
                if (element.TryGetProperty("user_id", out JsonElement je_uid)) { userid = je_uid.GetInt64(); } else { return; }
                if (element.TryGetProperty("flag", out JsonElement je_flag)) { flag = je_flag.GetString(); } else { return; }
                if (element.TryGetProperty("comment", out JsonElement je_cm)) { comment = je_cm.GetString(); } else { return; }
                switch (type.GetString())
                {
                    case "friend":
                        if (OnFriendRequest != null)
                            await OnFriendRequest(source.SelfId, new CqFriendRequestEventArgs(source,userid, flag, comment));
                        break;
                    case "group":
                        long groupid = 0;
                        string subtype = "";
                        if (element.TryGetProperty("sub_type", out JsonElement je_st)) { subtype = je_st.GetString(); }
                        if (element.TryGetProperty("group_id", out JsonElement je_gid)) { groupid = je_st.GetInt64(); }
                        if (OnGroupRequest != null)
                            await OnGroupRequest(source.SelfId, new CqGroupRequestEventArgs(source, userid, groupid, subtype, flag, comment));
                        break;
                }
            }
        }
        private async void NoticEvent(JsonElement element, Source source)
        {
            if (element.TryGetProperty("notice_type", out JsonElement type))
            {
                long userid, groupid = 0, operatorid = 0, duration = 0, fileid = 0, filesize = 0, filebusid = 0;
                string subtype ="", filename = "";
                GroupNotic gnotic = GroupNotic.Upload;
                File file = null;
                if (element.TryGetProperty("user_id", out JsonElement je_uid)) { userid = je_uid.GetInt64(); } else { return; }
                if (element.TryGetProperty("operator_id", out JsonElement je_oi)) { operatorid = je_oi.GetInt64(); }
                if (element.TryGetProperty("group_id", out JsonElement je_di)) { groupid = je_di.GetInt64(); }
                if (element.TryGetProperty("duration", out JsonElement je_du)) { duration = je_du.GetInt64(); }
                if (element.TryGetProperty("sub_type", out JsonElement je_st)) { subtype = je_st.GetString(); }
                if (element.TryGetProperty("file", out JsonElement je_file))
                {
                    if (je_file.GetRawText() != "null")
                    {
                        if (je_file.TryGetProperty("id", out JsonElement je_fid)) { fileid = je_fid.GetInt64(); }
                        if (je_file.TryGetProperty("name", out JsonElement je_fn)) { filename = je_fn.GetString(); }
                        if (je_file.TryGetProperty("size", out JsonElement je_fs)) { filesize = je_fs.GetInt64(); }
                        if (je_file.TryGetProperty("busid", out JsonElement je_fb)) { filebusid = je_fb.GetInt64(); }
                        file = new File()
                        {
                            Id = fileid,
                            Name = filename,
                            Size = filesize,
                            Busid = filebusid
                        };
                    }
                }
                switch (subtype)
                {
                    case "set":
                        gnotic = GroupNotic.Admin | GroupNotic.Set;
                        break;
                    case "unset":
                        gnotic = GroupNotic.Admin | GroupNotic.UnSet;
                        break;
                    case "leave":
                        gnotic = GroupNotic.Decrease | GroupNotic.Leave;
                        break;
                    case "kick":
                        gnotic = GroupNotic.Decrease | GroupNotic.Kick;
                        break;
                    case "kick_me":
                        gnotic = GroupNotic.Decrease | GroupNotic.KickMe;
                        break;
                    case "approve":
                        gnotic = GroupNotic.Increase | GroupNotic.Approve;
                        break;
                    case "invite":
                        gnotic = GroupNotic.Increase | GroupNotic.Invite;
                        break;
                    case "ban":
                        gnotic = GroupNotic.Ban;
                        break;
                    case "lift_ban":
                        gnotic = GroupNotic.Lift_Ban;
                        break;
                }
                switch (type.GetString())
                {
                    case "group_upload":
                    case "group_admin":
                    case "group_decrease":
                    case "group_increase":
                    case "group_ban":
                        if (OnGroupNotic !=null)
                            await OnGroupNotic(source.SelfId, new CqGroupNoticEventArgs(source, groupid, userid, operatorid, duration, file, gnotic));
                        break;
                    case "friend_add":
                        if (OnFriendAdd != null)
                            await OnFriendAdd(source.SelfId, new CqFriendAddEventArgs(source, userid));
                        break;
                }
            }
        }
        private async void MessageEvent(JsonElement element, Source source)
        {
            if (element.TryGetProperty("message_type", out JsonElement type))
            {
                int age = 0;
                long targetid = 0, user_id = 0, anonymousId = 0;
                string subtype = "", nickname = "", card = "", sex = "", area = "", role = "", title = "", flag = "", name = "", level = "";
                InGroupSex gsex = InGroupSex.Unknown;
                GroupRole gro = GroupRole.Member;

                if (element.TryGetProperty("sender", out JsonElement je_sender) == false) { return; }
                if (element.TryGetProperty("sub_type", out JsonElement je_st)){ subtype = je_st.GetString(); }
                if (element.TryGetProperty("discuss_id", out JsonElement je_di)) { targetid = je_di.GetInt64(); }
                if (element.TryGetProperty("group_id", out JsonElement je_gi)) { targetid = je_gi.GetInt64();}
                if (je_sender.TryGetProperty("user_id", out JsonElement je_uid)) { user_id = je_uid.GetInt64(); }
                if (je_sender.TryGetProperty("age", out JsonElement je_age)) { age = je_age.GetInt32(); }
                if (je_sender.TryGetProperty("nickname", out JsonElement je_name)) { nickname = je_name.GetString(); }

                if (je_sender.TryGetProperty("level", out JsonElement je_level)) { level = je_level.GetString(); }
                if (je_sender.TryGetProperty("card", out JsonElement je_card)) { card = je_card.GetString(); }
                if (je_sender.TryGetProperty("sex", out JsonElement je_sex)) { sex = je_sex.GetString(); }
                if (je_sender.TryGetProperty("area", out JsonElement je_area)) { area = je_area.GetString(); }
                if (je_sender.TryGetProperty("role", out JsonElement je_role)) { role = je_role.GetString(); }
                if (je_sender.TryGetProperty("title", out JsonElement je_title)) { title = je_title.GetString(); }

                switch (sex)
                {
                    case "male":
                        gsex = InGroupSex.Male;
                        break;
                    case "female":
                        gsex = InGroupSex.Male;
                        break;
                    case "unknown":
                        gsex = InGroupSex.Unknown;
                        break;
                }
                switch (role)
                {
                    case "owner":
                        gro = GroupRole.Owner;
                        break;
                    case "admin":
                        gro = GroupRole.Admin;
                        break;
                    case "member":
                        gro = GroupRole.Member;
                        break;
                }
                CqHttpSender sender = new CqHttpSender()
                {
                    UserId = user_id,
                    Age = age,
                    Level = level,
                    NickName = nickname,
                    Card = card,
                    Sex = gsex,
                    Area = area,
                    Role = gro,
                    Title = title
                };
                Anonymous anonymous = new Anonymous()
                {
                    Id = anonymousId,
                    Flag = flag,
                    Name = name
                };
                CqHttpMessageEventArgs ea = new CqHttpMessageEventArgs(
                    message: element.GetProperty("message").GetString(),
                    rawMessage: element.GetProperty("raw_message").GetString(),
                    subType: subtype,
                    messageId: element.GetProperty("message_id").GetInt32(),
                    targetId: targetid,
                    fontId: element.GetProperty("font").GetInt64(),
                    sender: sender,
                    anonymous: anonymous,
                    source: source
                 );
                if (OnMessage != null)
                {
                    await OnMessage(source.SelfId, ea);
                }
                switch (type.GetString())
                { 
                    case "private":
                        if (OnPrivateMessage!=null)
                            await OnPrivateMessage(source.SelfId, ea);
                        break;
                    case "group":
                        if (OnGroupMessage != null)
                            await OnGroupMessage(source.SelfId, ea);
                        break;
                    case "discuss":
                        if (OnDiscussMessage != null)
                            await OnDiscussMessage(source.SelfId, ea);
                        break;
                }
            }
        }
        private async void MetaEvent(JsonElement element, Source source)
        {
            if (element.TryGetProperty("meta_event_type", out JsonElement meta))
            {
                if (meta.GetString() == "heartbeat")
                {
                    if (element.TryGetProperty("status", out JsonElement status))
                    {
                        StatusEvent(status, source);
                    }
                }
                if (meta.GetString() == "lifecycle")
                {
                    if (element.TryGetProperty("sub_type", out JsonElement subType))
                    {
                        switch (subType.GetString())
                        {
                            case "enable":
                                break;
                            case "disable":
                                break;
                            case "connect":
                                if (OnConnected != null)
                                    await OnConnected(source.SelfId, new  ConnectEventArgs(source));
                                break;
                        }
                    }
                }
            }
        }
    }
}
