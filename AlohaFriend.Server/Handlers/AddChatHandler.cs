using AlohaFriend.Server.Contexts;
using AlohaFriend.Server.Models;
using AlohaFriend.Server.Services;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AlohaFriend.Server.Handlers
{
    public class AddChatHandler : IHandler
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SessionManagementService _sessionManagementService;
        public AddChatHandler(ApplicationDbContext dbContext,
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

                var title = registrationModel["Title"].ToString();
                ChatRoomType chatRoomType;
                Enum.TryParse(registrationModel["ChatRoomType"].ToString(), out chatRoomType);
                var selUsers = registrationModel["SelUsers"].ToString();

                string result = new string(selUsers.Where(p => char.IsDigit(p)).ToArray());

                List<int> usersId = new List<int>();

                foreach (var item in result)
                {
                    usersId.Add(int.Parse(item.ToString()));
                }

                _dbContext.ChatRooms.Add(new Models.ChatRoom()
                {
                    Title = title,
                    ChatRoomType = chatRoomType
                });
                _dbContext.SaveChanges();

                int chatRoomId = _dbContext.ChatRooms.FirstOrDefault(p => p.Title == title).Id;

                foreach (var item in usersId)
                {
                    _dbContext.ApplicationUserChatRooms.Add(new Models.ApplicationUserChatRoomJunction()
                    {
                        ChatRoomId = chatRoomId,
                        ApplicationUserId = item,
                        ApplicationUserStatus = 0
                    });
                }
                _dbContext.SaveChanges();

                response.StatusCode = 200;
                response.OutputStream.Write(Encoding.UTF8.GetBytes("OK!"));
                response.Close();
            }
        }
    }
}
