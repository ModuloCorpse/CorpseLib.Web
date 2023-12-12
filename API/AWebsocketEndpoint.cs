namespace CorpseLib.Web.API
{
    public abstract class AWebsocketEndpoint : AEndpoint
    {
        protected AWebsocketEndpoint(string path) : base(new(path), false) { }
        protected AWebsocketEndpoint(Http.Path path) : base(path, false) { }
        protected AWebsocketEndpoint(string path, bool needExactPath) : base(new(path), needExactPath) { }
        protected AWebsocketEndpoint(Http.Path path, bool needExactPath) : base(path, needExactPath) { }

        internal void RegisterClient(API.APIProtocol client, Http.Path path) => OnClientRegistered(client, path);
        protected abstract void OnClientRegistered(API.APIProtocol client, Http.Path path);
        internal void ClientMessage(string id, string message) => OnClientMessage(id, message);
        protected abstract void OnClientMessage(string id, string message);
        internal void ClientUnregistered(string id) => OnClientUnregistered(id);
        protected abstract void OnClientUnregistered(string id);
    }
}
