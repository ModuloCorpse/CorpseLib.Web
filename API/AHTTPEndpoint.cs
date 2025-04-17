namespace CorpseLib.Web.API
{
    public abstract class AHTTPEndpoint : AEndpoint
    {
        protected AHTTPEndpoint(string path) : base(path, true, false) { }
        protected AHTTPEndpoint(Http.Path path) : base(path, true, false) { }
        protected AHTTPEndpoint(string path, bool needExactPath) : base(path, needExactPath, true, false) { }
        protected AHTTPEndpoint(Http.Path path, bool needExactPath) : base(path, needExactPath, true, false) { }

        protected sealed override void OnClientRegistered(API.APIProtocol client, Http.Path path) { }
        protected sealed override void OnClientMessage(string id, string message) { }
        protected sealed override void OnClientUnregistered(string id) { }
    }
}
