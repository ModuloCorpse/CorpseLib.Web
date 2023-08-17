using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;
using System.Collections.Concurrent;

namespace CorpseLib.Web.API
{
    public class API
    {
        public delegate Response MethodHandler(Request request);

        public class APIProtocol : HttpProtocol
        {
            private readonly API m_API;
            private readonly string m_ID;

            public APIProtocol(API api, string id)
            {
                m_API = api;
                m_ID = id;
            }

            protected override void OnHTTPRequest(Request request) => Send(m_API.HandleAPIRequest(request));

            protected override void OnWSOpen(Request _) => m_API.RegisterClient(m_ID, this);

            protected override void OnWSClose(int status, string message) => m_API.UnregisterClient(m_ID);
        }

        private readonly Dictionary<string, Canal<APIEvent>> m_CanalManager = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> m_Events = new();
        private readonly ConcurrentDictionary<string, APIProtocol> m_Clients = new();
        private readonly ConcurrentDictionary<string, Dictionary<Request.MethodType, MethodHandler>> m_APIEndpoints = new();
        private readonly ConcurrentDictionary<string, Dictionary<Request.MethodType, MethodHandler>> m_Endpoints = new();
        private readonly TCPAsyncServer m_AsyncServer;

        public bool IsRunning => m_AsyncServer.IsRunning();

        public API(int port)
        {
            AddAPIEndpoint("/subscribe", Request.MethodType.POST, OnSubscribeRequest);
            AddAPIEndpoint("/unsubscribe", Request.MethodType.POST, OnUnsubscribeRequest);
            m_AsyncServer = new(() => new APIProtocol(this, Guid.NewGuid().ToString().Replace("-", "")), port);
        }

        public void Start() => m_AsyncServer.Start();
        public void Stop() => m_AsyncServer.Stop();

        ~API() => m_AsyncServer.Stop();

        private void AddAPIEndpoint(string path, Request.MethodType methodType, MethodHandler methodHandler)
        {
            if (!m_APIEndpoints.ContainsKey(path))
                m_APIEndpoints[path] = new();
            m_APIEndpoints[path][methodType] = methodHandler;
        }

        public void AddEndpoint(string path, Request.MethodType methodType, MethodHandler methodHandler)
        {
            if (path.Length > 0 && path[0] != '/')
                path = "/" + path;
            if (!m_Endpoints.ContainsKey(path))
                m_Endpoints[path] = new();
            m_Endpoints[path][methodType] = methodHandler;
        }

        internal void RegisterClient(string id, APIProtocol protocol)
        {
            m_Clients[id] = protocol;
            protocol.Send(new JObject() { { "id", id } }.ToNetworkString());
        }

        internal void UnregisterClient(string id)
        {
            m_Clients.Remove(id, out APIProtocol? _);
            foreach (var pair in m_Events)
                pair.Value.Remove(id);
        }

        internal Response HandleAPIRequest(Request request)
        {
            if (m_APIEndpoints.TryGetValue(request.Path, out Dictionary<Request.MethodType, MethodHandler>? apiMethods))
                return (apiMethods.TryGetValue(request.Method, out MethodHandler? methodHandler)) ? methodHandler(request) : new(405, "Method Not Allowed");
            else if (m_Endpoints.TryGetValue(request.Path, out Dictionary<Request.MethodType, MethodHandler>? methods))
                return (methods.TryGetValue(request.Method, out MethodHandler? methodHandler)) ? methodHandler(request) : new(405, "Method Not Allowed");
            else
                return new(404, "Not Found", string.Format("Endpoint {0} does not exist", request.Path));
        }

        public bool NewEvent(string eventType)
        {
            if (!m_CanalManager.ContainsKey(eventType))
            {
                Canal<APIEvent> newCanal = new();
                m_CanalManager[eventType] = newCanal;
                m_Events[eventType] = new();
                newCanal.Register((APIEvent? @event) => SendEvent(@event!.Endpoint, @event!.Data));
                return true;
            }
            return false;
        }

        private void SendEvent(string eventType, object? data)
        {
            if (m_Events.TryGetValue(eventType, out HashSet<string>? hashSet))
            {
                string msg = new JObject() {
                    { "type", eventType },
                    { "data", data }
                }.ToNetworkString();
                foreach (string websocketID in hashSet)
                {
                    if (m_Clients.TryGetValue(websocketID, out APIProtocol? client))
                        client.Send(msg);
                }
            }
        }

        public bool Emit(string eventType, object arg)
        {
            if (m_CanalManager.TryGetValue(eventType, out Canal<APIEvent>? canal))
            {
                canal.Emit(new(eventType, arg));
                return true;
            }
            return false;
        }

        private Response OnSubscribeRequest(Request request)
        {
            try
            {
                JFile requestContent = new(request.Body);
                if (requestContent.TryGet("ws", out string? websocketID))
                {
                    if (!m_Clients.ContainsKey((websocketID!)))
                        return new(404, "Not Found", string.Format("Websocket {0} does not exist", websocketID));
                    if (!requestContent.ContainsKey("events"))
                        return new(400, "Bad Request", "Missing 'events' field");
                    List<string> events = requestContent.GetList<string>("events");
                    foreach (string eventType in events)
                    {
                        if (!m_Events.ContainsKey(eventType))
                            return new(404, "Not Found", string.Format("Event {0} does not exist", eventType));
                    }
                    foreach (string eventType in events)
                        m_Events[eventType].Add(websocketID!);
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

        private Response OnUnsubscribeRequest(Request request)
        {
            try
            {
                JFile requestContent = new(request.Body);
                if (requestContent.TryGet("ws", out string? websocketID))
                {
                    if (!m_Clients.ContainsKey(websocketID!))
                        return new(404, "Not Found", string.Format("Websocket {0} does not exist", websocketID));
                    if (!requestContent.ContainsKey("events"))
                        return new(400, "Bad Request", "Missing 'events' field");
                    List<string> events = requestContent.GetList<string>("events");
                    foreach (string eventType in events)
                    {
                        if (m_Events.TryGetValue(eventType, out HashSet<string>? value))
                        {
                            if (!value.Contains(websocketID!))
                                return new(400, "Bad Request", string.Format("Websocket {0} is not registered to event {1}", websocketID, eventType));
                        }
                        else
                            return new(404, "Not Found", string.Format("Event {0} does not exist", eventType));
                    }
                    foreach (string eventType in events)
                        m_Events[eventType].Remove(websocketID!);
                    return new Response(200, "Client unsubscribed");
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
