using AlohaFriend.Server.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlohaFriend.Server.Services
{
    public class RoutingService
    {
        private readonly Dictionary<string, IHandler> _routing;
        public RoutingService(
            SignUpHandler signUpHandler, 
            SignInHandler signInHandler,
            GetChatsHandler getChatsHandler,
            GetUsersHandler getUsersHandler,
            AddChatHandler addChatHandler)
        {
            _routing = new Dictionary<string, IHandler>()
            {
                { "/app/sign-up",  signUpHandler },
                { "/app/sign-in", signInHandler },
                { "/app/get-chats", getChatsHandler },
                { "/app/get-users", getUsersHandler },
                { "/app/add-chat", addChatHandler }
            };
        }

        public IHandler GetHandlerByRoute(string rawRoute)
        {
            if (_routing.ContainsKey(rawRoute))
                return _routing[rawRoute];
            else return new NotFoundHandler();
        }
    }
}
