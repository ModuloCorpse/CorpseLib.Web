namespace CorpseLib.Web.API
{
    public abstract class AHTTPEndpoint : AEndpoint
    {
        protected AHTTPEndpoint() : base(true, false) { }
        protected AHTTPEndpoint(bool needExactPath) : base(needExactPath, true, false) { }

        protected sealed override void OnClientRegistered(WebsocketReference wsReference) { }
        protected sealed override void OnClientMessage(WebsocketReference wsReference, string message) { }
        protected sealed override void OnClientUnregistered(WebsocketReference wsReference) { }
    }
}
