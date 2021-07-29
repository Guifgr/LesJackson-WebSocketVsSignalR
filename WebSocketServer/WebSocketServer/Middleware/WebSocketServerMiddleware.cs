using System;
using System.Linq;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerMiddleware
    {
        private readonly RequestDelegate _next;
        public readonly WebSocketServerConnectionManager _Manager;

        public WebSocketServerMiddleware(RequestDelegate next, WebSocketServerConnectionManager manager)
        {
            _next = next;
            _Manager = manager;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine("WebSocket connectado");

                var connId = _Manager.AddSocket(webSocket);
                await SendConnIdAsync(webSocket, connId);

                await ReceiveMessage(webSocket, async (result, buffer) =>
                {
                    switch (result.MessageType)
                    {
                        case WebSocketMessageType.Text:
                            Console.WriteLine("Message Received");
                            Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                            await RouteJsonMessageAsync(Encoding.UTF8.GetString(buffer, 0, result.Count));
                            return;
                        case WebSocketMessageType.Close:
                            var id = _Manager.GetAllSockets().FirstOrDefault(s => s.Value == webSocket).Key;
                            
                            Console.WriteLine("Received  close message");
                            _Manager.GetAllSockets().TryRemove(id, out WebSocket sock);
                            await sock.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription,
                                CancellationToken.None);
                            return;
                    }
                });
            }
            else
            {
                Console.WriteLine("Hello from the 2rd request delegate");
                await _next(context);
            }
        }

        private async Task SendConnIdAsync(WebSocket socket, string connId)
        {
            var buffer = Encoding.UTF8.GetBytes("ConnID: " + connId);
            await socket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }
        
        private async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                    cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);
            }
        }

        public async Task RouteJsonMessageAsync(string message)
        {
            var routeObj = JsonConvert.DeserializeObject<dynamic>(message);
            if (routeObj != null && Guid.TryParse(routeObj.To.ToString(), out Guid guidOutput))
            {
                Console.WriteLine("Targeted");
                var sock = _Manager.GetAllSockets().FirstOrDefault(s => s.Key == routeObj.To.ToString());
                if (sock.Value != null)
                {
                    if (sock.Value.State == WebSocketState.Open){}

                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                else
                {
                    Console.WriteLine("Invalid Recipient");   
                }

            }
            else
            {
                Console.WriteLine("Broadcast");
                foreach (var sock in _Manager.GetAllSockets())
                {
                    if (sock.Value.State == WebSocketState.Open)
                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                            WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
    }
}