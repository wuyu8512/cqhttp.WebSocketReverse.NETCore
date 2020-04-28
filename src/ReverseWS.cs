using Fleck;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace cqhttp.WebSocketReverse.NETCore
{
    public class WSBaseEventArgs : EventArgs
    {
        public Connection Connection { get; set; }
    }
    public class PongEventArgs : WSBaseEventArgs
    {
        public byte[] Echo { get; set; }
        public PongEventArgs(byte[] echo, Connection connection)
        {
            Echo = echo;
            base.Connection = connection;
        }
    }
    public class ErrorEventArgs : WSBaseEventArgs
    {
        public Exception Exception { get; set; }
        public ErrorEventArgs(Exception ex, Connection connection)
        {
            Exception = ex;
            base.Connection = connection;
        }
    }
    public sealed class AuthorizationEventArgs : WSBaseEventArgs
    {
        public string SelfId { get; private set; }
        public string AuthCode { get; private set; }
        public string Role { get; private set; }
        public bool Pass { get; private set; } = false;
        public void Allow() { this.Pass = true; }
        public AuthorizationEventArgs(string selfId, string AuthCode, string Role, Connection connection)
        {
            this.SelfId = selfId;
            this.AuthCode = AuthCode;
            this.Role = Role;
            base.Connection = connection;
        }
    }
    public sealed class ConnectionEventArgs : WSBaseEventArgs
    {
        public string Role { get; private set; }
        public ConnectionEventArgs(string Role, Connection connection)
        {
            this.Role = Role;
            base.Connection = connection;
        }
    }
    public sealed class MessageEventArgs : WSBaseEventArgs
    {
        public string Message { get; private set; }
        public string Role { get; private set; }
        public ConnectionData ConnectionData { get; private set; }
        public MessageEventArgs(string Message, string Role, ConnectionData ConnectionData, Connection connection, Func<string, Task> send)
        {
            this.Message = Message;
            this.Role = Role;
            this.ConnectionData = ConnectionData;
            base.Connection = connection;
        }
    }
    public sealed class Connection
    {
        public Guid Id => WebSocketConnectionInfo.Id;
        public IWebSocketConnectionInfo WebSocketConnectionInfo { get; set; }
        public Func<string, Task> Send { get; set; }
    }
    public sealed class ConnectionData
    {
        public string SelfId { get; private set; }
        public ConcurrentDictionary<string, Connection> RoleAndConnections = new ConcurrentDictionary<string, Connection>();
        public DateTime CreateDateTime { get; private set; }
        public ConnectionData(string selfId, string role, Connection connection)
        {
            RoleAndConnections.TryAdd(role, connection);
            this.CreateDateTime = DateTime.Now;
            this.SelfId = selfId;
        }
    }
    public class ReverseWS : IDisposable
    {
        public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e) where TEventArgs : EventArgs;
        private readonly WebSocketServer Server = null;
        public bool isHardAuth = false;
        public ConcurrentDictionary<string, ConnectionData> ConnectionBinding = new ConcurrentDictionary<string, ConnectionData>();
        public event AsyncEventHandler<AuthorizationEventArgs> OnAuthorization;
        public event AsyncEventHandler<ConnectionEventArgs> OnOpenConnection;
        public event AsyncEventHandler<ConnectionEventArgs> OnCloseConnection;
        public event AsyncEventHandler<MessageEventArgs> OnReceiveMessage;
        public event AsyncEventHandler<WSBaseEventArgs> OnPong;
        public event AsyncEventHandler<ErrorEventArgs> OnError;
        public ReverseWS(string location)
        {
            Server = new WebSocketServer(location);
            Server.Start(socket =>
            {
                socket.OnPong = async (rcnb) =>
                {
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out string selfId) == false) { return; }
                    if (OnPong == null) { return; }
                    if (ConnectionBinding.TryGetValue(selfId, out ConnectionData data))
                    {
                        if (isHardAuth) if (data.RoleAndConnections.Any(a => a.Value.Id == socket.ConnectionInfo.Id) == false) { return; }
                        var connection = new Connection { Send = socket.Send, WebSocketConnectionInfo = socket.ConnectionInfo };
                        await OnPong(selfId, new WSBaseEventArgs() { Connection = connection });
                    }
                };
                socket.OnOpen = async () =>
                {
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out string selfId) == false) { return; }
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role", out string type) == false) { return; }
                    var connection = new Connection { Send = socket.Send, WebSocketConnectionInfo = socket.ConnectionInfo };
                    if (socket.ConnectionInfo.Headers.TryGetValue("Authorization", out string auth) == true)
                    {
                        var ea = new AuthorizationEventArgs(selfId, auth, type, connection);
                        if (OnAuthorization != null) { await OnAuthorization(selfId, ea); }
                        if (ea.Pass == false && OnAuthorization != null) { return; }
                    }
                    await socket.SendPing(new byte[] { 1, 2, 5 });
                    var cinfo = new ConnectionData(selfId, type, connection);
                    ConnectionBinding.AddOrUpdate(selfId, cinfo, (n, b) =>
                    {
                        b.RoleAndConnections.AddOrUpdate(type, connection, (n, b) =>
                        {
                            b.Send = connection.Send;
                            b.WebSocketConnectionInfo = connection.WebSocketConnectionInfo;
                            return b;
                        });
                        return b;
                    });
                    if (OnOpenConnection == null) { return; }
                    await OnOpenConnection(selfId, new ConnectionEventArgs(type, connection));
                };
                socket.OnClose = async () =>
                {
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out string selfId) == false) { return; }
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role", out string type) == false) { return; }
                    foreach (var cb in ConnectionBinding)
                    {
                        if (cb.Value.RoleAndConnections.Any(f => f.Value.Id == socket.ConnectionInfo.Id))
                        {
                            cb.Value.RoleAndConnections.TryRemove(type, out Connection dump);
                            if (OnCloseConnection == null) { return; }
                            await OnCloseConnection(selfId, new ConnectionEventArgs(type, dump));
                        }
                    }
                };
                socket.OnMessage = async (message) =>
                {
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Self-ID", out string selfId) == false) { return; }
                    if (socket.ConnectionInfo.Headers.TryGetValue("X-Client-Role", out string type) == false) { return; }
                    var connection = new Connection { Send = socket.Send, WebSocketConnectionInfo = socket.ConnectionInfo };
                    if (ConnectionBinding.TryGetValue(selfId, out ConnectionData data))
                    {
                        if (isHardAuth) if (data.RoleAndConnections.Any(a => a.Value.Id == socket.ConnectionInfo.Id) == false) { return; }
                        if (OnReceiveMessage == null) { return; }
                        try
                        {
                            await OnReceiveMessage(selfId, new MessageEventArgs(message, type, data, connection, socket.Send));
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            if (OnError == null) { return; }
                            await OnError(selfId, new ErrorEventArgs(ex, connection));
                        }
                    }
                };
            });
        }
        ~ReverseWS()
        {
            Dispose();
        }
        public void Dispose()
        {
            Server.Dispose();
            ConnectionBinding.Clear();
        }
    }
}
