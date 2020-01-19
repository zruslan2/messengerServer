using AlohaFriend.Server.Contexts;
using AlohaFriend.Server.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace AlohaFriend.Server.Handlers
{
    public class shortUser
    {
        public int Id { get; set; }
        public string session { get; set; }
        public string phoneNumber { get; set; }
        public shortUser(int id, string ses, string pn)
        {
            this.Id = id;
            this.session = ses;
            this.phoneNumber = pn;
        }
    }
    public class SignInHandler : IHandler
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly SessionManagementService _sessionManagementService;
        public SignInHandler(ApplicationDbContext dbContext, 
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

                var phoneNumber = registrationModel["phoneNumber"].ToString();
                var password = registrationModel["password"].ToString();

                var user = _dbContext.ApplicationUsers
                    .SingleOrDefault(p => p.PhoneNumber == phoneNumber && p.PasswordHash == password);
                
                if (user == null)
                {
                    response.StatusCode = 404;
 
                    response.OutputStream.Write(Encoding.UTF8.GetBytes("NOT FOUND!"));
                    response.Close();
                    return;
                }

                var session = _sessionManagementService.AddSession(user.Id);
                
                shortUser su = new shortUser(user.Id, session, user.PhoneNumber);
                var json = JsonConvert.SerializeObject(su);

                response.StatusCode = 200;

                response.OutputStream.Write(Encoding.UTF8.GetBytes(json));
                response.Close();
            }
        }
    }
}
