using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using WebSocketServer.Middleware;

namespace WebSocketServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebSocketServerConnectionManager();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseWebSockets();

            app.UseWebsocketServer();

            app.Run(async context =>
            {
                Console.WriteLine("Hello from the 3rd request delegate");
                await context.Response.WriteAsync("Hello from the 3rd request delegate");
            });
        }

        private static void WriteRequestParam(HttpContext context)
        {
            Console.WriteLine("Request Method " + context.Request.Method);
            Console.WriteLine("Request Protocol " + context.Request.Protocol);

            foreach (var (key, value) in context.Request.Headers)
            {
                Console.WriteLine("--> " + key + " : " + value);
            }
        }
        
    }
}