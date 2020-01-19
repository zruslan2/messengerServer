using AlohaFriend.Server.Contexts;
using AlohaFriend.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using AlohaFriend.Server.Services;
using AlohaFriend.Server.Handlers;
using Fleck;
using Newtonsoft.Json.Linq;

namespace AlohaFriend.Server
{
    class Program
    {
        static async Task RunHttpServer(ServiceProvider serviceProvider)
        {
            var worker = Task.Run(() =>
            {
                var bindingAddress = "http://localhost:15100/";
                var httpServer = new HttpListener();
                httpServer.Prefixes.Add(bindingAddress);
                httpServer.Start();

                while (true)
                {
                    var connectionContext = httpServer.GetContext();
                    var request = connectionContext.Request;

                    var routingService = serviceProvider.GetService<RoutingService>();
                    var handler = routingService.GetHandlerByRoute(request.RawUrl);

                    handler.Handle(connectionContext.Request, connectionContext.Response);
                }
            });

            await worker;
        }

        static async Task OnConnectionOpened(
            IWebSocketConnection connection,
            ICollection<IWebSocketConnection> templeConnections)
        {
            templeConnections.Add(connection);

            await connection.Send("OK");
        }

        static async Task OnMessage(
            IWebSocketConnection connection,
            SessionManagementService sessionManagement,
            WebSocketConnectionManager webSocketConnectionManager,
            ApplicationDbContext applicationDbContext,
            string message)
        {
            var json = JObject.Parse(message);
            if(json["type"].ToString()== "message")
            {
                var sessionId = json["sessionId"].ToString();
                var chatName = json["chatName"].ToString();

                var chatId = applicationDbContext.ChatRooms.FirstOrDefault(p => p.Title == chatName).Id;

                var userId = sessionManagement.GetUserBySession(sessionId);
                var user = applicationDbContext.ApplicationUsers.Find(userId);

                applicationDbContext.ChatMessages.Add(new ChatMessage()
                {
                    Body = message,
                    SenderApplicationUserId = (int)userId,
                    ChatRoomId = chatId
                });
                applicationDbContext.SaveChanges();

                webSocketConnectionManager.AddSocketConnection(sessionId, connection);

                foreach (var activeConnection in webSocketConnectionManager.GetAllActiveConnections())
                {
                    await activeConnection.Send(message);
                }
            }
            else if(json["type"].ToString() == "REQ")
            {
                var sessionId = json["sessionId"].ToString();
                var chatName = json["chatName"].ToString();

                var chatId = applicationDbContext.ChatRooms.FirstOrDefault(p => p.Title == chatName).Id;

                var chatMessages = applicationDbContext.ChatMessages.Where(p => p.ChatRoomId == chatId).ToList();

                webSocketConnectionManager.AddSocketConnection(sessionId, connection);

                var webSocket = webSocketConnectionManager.GetConnectionBySession(sessionId);

                foreach (var mes in chatMessages)
                {
                    webSocket.Send(mes.Body);
                }
            }
        }

        static async Task Main(string[] args)
        {
            var serviceProvider = new ServiceCollection()
                .AddDbContext<ApplicationDbContext>(options => options
                    .UseSqlServer("Server=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=chats;Data Source=RUSLAN-PC\\RU"))
                .AddSingleton(typeof(RoutingService))
                .AddTransient(typeof(SignUpHandler))
                .AddTransient(typeof(SignInHandler))
                .AddTransient(typeof(GetChatsHandler))
                .AddTransient(typeof(GetUsersHandler))
                .AddTransient(typeof(AddChatHandler))
                .AddSingleton(typeof(SessionManagementService))
                .AddSingleton(typeof(WebSocketConnectionManager))
                .BuildServiceProvider();

            var dbContext = serviceProvider.GetService<ApplicationDbContext>();

            dbContext.Database.EnsureCreated();

            var chat = dbContext.ChatRooms.FirstOrDefault(p => p.Title == "Общий чат");

            if(chat==null)
            {
                var mainChat = dbContext.ChatRooms.Add(new ChatRoom()
                {
                    Title = "Общий чат",
                    ChatRoomType = ChatRoomType.GroupChat
                });

                dbContext.SaveChanges();
            }           

            var templeConnections = new List<IWebSocketConnection>();
            var bindingAddress = "ws://127.0.0.1:15200";
            var sessionManagement = serviceProvider.GetService<SessionManagementService>();
            var webSocketConnectionManager = serviceProvider.GetService<WebSocketConnectionManager>();

            var wrapper = new WebSocketServer(bindingAddress);
            wrapper.Start(socket =>
            {
                socket.OnOpen = async () => await OnConnectionOpened(socket, templeConnections);
                socket.OnClose = () => Console.WriteLine("Close!");
                socket.OnMessage = async message => await OnMessage(socket, sessionManagement, webSocketConnectionManager, dbContext, message);
            });

            var httpServerWorker = RunHttpServer(serviceProvider);
            await httpServerWorker;
            Console.ReadLine();

            wrapper.Dispose();
        }
    }
}
