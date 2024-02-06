using CorpseLib.Web.Http;

namespace CorpseLib.Web.API.Event
{
    public class EventRegisterEndpoint(string path, EventEndpoint eventEndpoint) : AHTTPEndpoint(path, false)
    {
        private readonly EventEndpoint m_EventEndpoint = eventEndpoint;

        protected override Response OnPostRequest(Request request)
        {
            string id = request.Body;
            if (!string.IsNullOrEmpty(id))
            {
                OperationResult result = m_EventEndpoint.RegisterClient(id, request.Path.Paths[^1]);
                if (result)
                    return new(200, "Ok", "Websocket registered");
                return new(400, "Bad Request", result.Description);
            }
            return new(400, "Bad Request", "No websocket given");
        }
    }
}
