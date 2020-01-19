using System.Net;

namespace AlohaFriend.Server.Handlers
{
    public interface IHandler
    {
        void Handle(HttpListenerRequest request, HttpListenerResponse response);
    }
}
