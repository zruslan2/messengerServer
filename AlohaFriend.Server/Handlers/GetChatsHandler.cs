using AlohaFriend.Server.Contexts;
using AlohaFriend.Server.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace AlohaFriend.Server.Handlers
{
    public class GetChatsHandler : IHandler
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SessionManagementService _sessionManagementService;
        public GetChatsHandler(ApplicationDbContext dbContext,
            SessionManagementService sessionManagementService)
        {
            _dbContext = dbContext;
            _sessionManagementService = sessionManagementService;
        }
        public void Handle(HttpListenerRequest request, HttpListenerResponse response)
        {
            using (var ms = new MemoryStream())
            {
                if (request.HttpMethod == "OPTIONS")
                {
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                    response.AddHeader("Access-Control-Max-Age", "1728000");
                }
                response.AppendHeader("Access-Control-Allow-Origin", "*");

                request.InputStream.CopyTo(ms);
                var content = Encoding.UTF8.GetString(ms.ToArray());
                var registrationModel = JObject.Parse(content);

                var session = registrationModel["session"].ToString();

                var user = _sessionManagementService.GetUserBySession(session);
                var chatIds = _dbContext.ApplicationUserChatRooms.Where(p => p.ApplicationUserId == user).ToList();
                List<string> Chats = new List<string>();
                foreach (var id in chatIds)
                {
                    Chats.Add(_dbContext.ChatRooms.FirstOrDefault(p => p.Id == id.ChatRoomId).Title);
                }
                response.StatusCode = 200;

                var json = JsonConvert.SerializeObject(Chats);
                response.OutputStream.Write(Encoding.UTF8.GetBytes(json));
                response.Close();                
            }
        }
    }
}
