using AlohaFriend.Server.Contexts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AlohaFriend.Server.Handlers
{
    public class GetUsersHandler : IHandler
    {
        private readonly ApplicationDbContext _dbContext;
        public GetUsersHandler(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
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

                var users = _dbContext.ApplicationUsers.Select(p => p).ToList();

                var json = JsonConvert.SerializeObject(users);
                response.OutputStream.Write(Encoding.UTF8.GetBytes(json));
                response.Close();
            }
        }
    }
}
