using static CorpseLib.Web.API.API;
using Path = CorpseLib.Web.Http.Path;

namespace CorpseLib.Web.API
{
    public class WebsocketReference(APIProtocol client, Path path)
    {
        private readonly APIProtocol m_Client = client;
        private readonly Path m_Path = path;

        public Path Path => m_Path;
        public string ClientID => m_Client.ID;

        public void Disconnect() => m_Client.Disconnect();
        public void Reconnect() => m_Client.Reconnect();
        public void Send(object msg) => m_Client.Send(msg);
    }
}
