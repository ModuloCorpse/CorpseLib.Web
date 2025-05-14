using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public abstract class AWebsocketEndpoint : AEndpoint
    {
        protected AWebsocketEndpoint() : base(false, false, true) { }
        protected AWebsocketEndpoint(bool needExactPath) : base(needExactPath, false, true) { }

        protected sealed override Response OnGetRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnHeadRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnPostRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnPutRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnDeleteRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnConnectRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnOptionsRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnTraceRequest(Request request) => new(405, "Method Not Allowed");
        protected sealed override Response OnPatchRequest(Request request) => new(405, "Method Not Allowed");
    }
}
