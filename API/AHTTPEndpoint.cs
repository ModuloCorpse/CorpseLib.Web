using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public class AHTTPEndpoint : AEndpoint
    {
        protected AHTTPEndpoint(string path) : base(path, false) { }
        protected AHTTPEndpoint(string path, bool needExactPath) : base(path, needExactPath) { }

        internal Response OnRequest(Request request) => request.Method switch
        {
            Request.MethodType.GET => OnGetRequest(request),
            Request.MethodType.HEAD => OnHeadRequest(request),
            Request.MethodType.POST => OnPostRequest(request),
            Request.MethodType.PUT => OnPutRequest(request),
            Request.MethodType.DELETE => OnDeleteRequest(request),
            Request.MethodType.CONNECT => OnConnectRequest(request),
            Request.MethodType.OPTIONS => OnOptionsRequest(request),
            Request.MethodType.TRACE => OnTracerequest(request),
            Request.MethodType.PATCH => OnPatchRequest(request),
            _ => new(400, "Bad Request")
        };

        protected virtual Response OnGetRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnHeadRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnPostRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnPutRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnDeleteRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnConnectRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnOptionsRequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnTracerequest(Request request) => new(405, "Method Not Allowed");
        protected virtual Response OnPatchRequest(Request request) => new(405, "Method Not Allowed");
    }
}
