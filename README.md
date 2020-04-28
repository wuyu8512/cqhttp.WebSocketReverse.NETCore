## cqhttp.WebSocketReverse.NETCore
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/cqbef/cqhttp.WebSocketReverse.NETCore/PublicSDK?style=for-the-badge)
![NugetVersion](https://img.shields.io/nuget/v/cqhttp.WebSocketReverse.NETCore?style=for-the-badge)
![NugetDownload](https://img.shields.io/nuget/dt/cqhttp.WebSocketReverse.NETCore?style=for-the-badge)

基於CQHTTP插件的反向WebSocket通道提供服务端SDK层级的封装

#### 应用范例

```cs

ReverseWS rws = new ReverseWS("ws://0.0.0.0:8889");
CqHttpParse parse = new CqHttpParse(CqHttpApi.SetResult);
CqHttpApi.Timeout = TimeSpan.FromSeconds(10);

rws.OnAuthorization += async (s, e) =>
{
	await Task.Run(() =>
	{
		Debug.WriteLine(e.Connection.WebSocketConnectionInfo.ClientIpAddress);
		e.Allow();
	});
};
rws.OnReceiveMessage += async (s, e) =>
{
	await parse.Parse(s, e);
};
parse.OnPrivateMessage += async (n, b) =>
{
	var vipinfo = await b.Source.GetVipInfo();
	if(vipinfo.VipLevel == "普通用户")
	{
		await b.Source.Replay(b.Message);
	}else
	{
		long qqId = 123456789;
		int messageId = await b.Source.SendPrivateMessage(qqId,$"{b.Sender.NickName}({b.UserId})对你说:{b.Message}");
		if(messageId>0)await b.Source.Replay("已传达消息到主人");
	}
};

```
