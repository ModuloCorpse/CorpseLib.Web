using CorpseLib.Json;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.API.Event
{
    internal class SubscribeEndpoint : AHTTPEndpoint
    {
        private readonly EventManager m_Manager;

        internal SubscribeEndpoint(EventManager manager, string path) : base(path, true) => m_Manager = manager;

        protected override Response OnPostRequest(Request request)
        {
            try
            {
                JFile requestContent = new(request.Body);
                if (requestContent.TryGet("ws", out string? websocketID))
                {
                    if (!m_Manager.IsClientConnected((websocketID!)))
                        return new(404, "Not Found", string.Format("Websocket {0} does not exist", websocketID));
                    if (!requestContent.ContainsKey("events"))
                        return new(400, "Bad Request", "Missing 'events' field");
                    List<string> events = requestContent.GetList<string>("events");
                    foreach (string eventType in events)
                    {
                        if (!m_Manager.HaveEvent(eventType))
                            return new(404, "Not Found", string.Format("Event {0} does not exist", eventType));
                    }
                    m_Manager.RegisterClientToEvents(websocketID!, events);
                    return new Response(200, "Client subscribed");
                }
                else
                    return new(400, "Bad Request", "Missing 'ws' field");
            }
            catch
            {
                return new(400, "Bad Request", "Request body is not a json");
            }
        }
    }
}
