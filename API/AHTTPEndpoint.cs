namespace CorpseLib.Web.API
{
    public abstract class AHTTPEndpoint : AEndpoint
    {
        protected AHTTPEndpoint(string path) : base(path, true, false) { }
        protected AHTTPEndpoint(Http.Path path) : base(path, true, false) { }
        protected AHTTPEndpoint(string path, bool needExactPath) : base(path, needExactPath, true, false) { }
        protected AHTTPEndpoint(Http.Path path, bool needExactPath) : base(path, needExactPath, true, false) { }

        protected sealed override void OnClientRegistered(WebsocketReference wsReference) { }
        protected sealed override void OnClientMessage(WebsocketReference wsReference, string message) { }
        protected sealed override void OnClientUnregistered(WebsocketReference wsReference) { }
    }
}
