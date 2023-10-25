namespace CorpseLib.Web.API.Event
{
    public class EventEndpoint : AWebsocketEndpoint
    {
        private readonly EventManager m_Manager;

        internal EventEndpoint(EventManager manager, string path): base(path) => m_Manager = manager;

        protected override void OnClientRegistered(API.APIProtocol client) => m_Manager.RegisterClient(client);

        protected override void OnClientMessage(string id, string message) { }

        protected override void OnClientUnregistered(string id) => m_Manager.UnregisterClient(id);
    }
}
