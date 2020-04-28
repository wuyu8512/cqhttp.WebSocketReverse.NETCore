namespace ConsoleApp
{
    using cqhttp.WebSocketReverse.NETCore;
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer wss = new WebSocketServer("ws://0.0.0.0:8889");

            CqHttpParse parse = new CqHttpParse(CqHttpApi.SetResult);
            CqHttpApi.Timeout = TimeSpan.FromSeconds(10);

            wss.OnAuthorizationAsync += async (s, e) =>
            {
                await Task.Run(() =>
                {
                    Debug.WriteLine(e.Connection.WebSocketConnectionInfo.ClientIpAddress);
                    e.Allow();
                });
            };
            wss.OnReceiveMessageAsync += async (s, e) =>
            {
                await parse.Parse(s, e);
            };
            parse.OnPrivateMessageAsync += async (n, b) =>
            {
                await b.Source.Replay(b.Message);
            };

            Console.ReadLine();
        }
    }
}
