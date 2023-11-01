using CorpseLib.Json;
using CorpseLib.ManagedObject;
using System.Collections.Concurrent;

namespace CorpseLib.Web.API.Event
{
    public class EventEndpoint : AWebsocketEndpoint
    {
        private abstract class AEventHandler
        {
            private readonly EventEndpoint m_Manager;
            private readonly HashSet<string> m_RegisteredClients = new();
            private readonly string m_EventType;

            public string EventType => m_EventType;

            protected AEventHandler(EventEndpoint manager, string eventType)
            {
                m_Manager = manager;
                m_EventType = eventType;
            }

            public bool RegisterClient(string clientID) => m_RegisteredClients.Add(clientID);
            public bool UnregisterClient(string clientID) => m_RegisteredClients.Remove(clientID);
            public bool IsRegistered(string clientID) => m_RegisteredClients.Contains(clientID);

            protected void Emit(string type, JObject eventData) => m_Manager.SendEvent(m_RegisteredClients.ToArray(), type, eventData);
        }

        private class EventHandler : AEventHandler
        {
            public EventHandler(EventEndpoint manager, string eventType) : base(manager, eventType) { }

            public void RegisterToCanal(Canal canal) => canal.Register(Trigger);
            public void UnregisterFromCanal(Canal canal) => canal.Unregister(Trigger);

            public void Trigger() => Emit("event", new JObject() { { "event", EventType }, { "data", new JObject() } });
        }

        private class EventHandler<T> : AEventHandler
        {
            public EventHandler(EventEndpoint manager, string eventType) : base(manager, eventType) { }

            public void RegisterToCanal(Canal<T> canal) => canal.Register(Emit);
            public void UnregisterFromCanal(Canal<T> canal) => canal.Unregister(Emit);

            public void Emit(T? data)
            {
                if (data == null)
                    Emit("event", new JObject() { { "event", EventType }, { "data", new JObject() } });
                else
                    Emit("event", new JObject() { { "event", EventType }, { "data", data } });
            }
        }

        private readonly ConcurrentDictionary<string, AEventHandler> m_Events = new();
        private readonly ConcurrentDictionary<string, API.APIProtocol> m_Clients = new();

        public EventEndpoint() : base("/event") { }
        public EventEndpoint(string path) : base(path) { }
        public EventEndpoint(string path, bool needExactPath) : base(path, needExactPath) { }

        protected static void SendEvent(API.APIProtocol client, string type, JObject data)
        {
            client.Send(new JObject() { { "type", type }, { "data", data } }.ToNetworkString());
        }

        protected void SendEvent(string id, string type, JObject data)
        {
            if (m_Clients.TryGetValue(id, out API.APIProtocol? client))
                client.Send(new JObject() { { "type", type }, { "data", data } }.ToNetworkString());
        }

        protected void SendEvent(string[] ids, string type, JObject data)
        {
            string msg = new JObject() { { "type", type }, { "data", data } }.ToNetworkString();
            foreach (string id in ids)
            {
                if (m_Clients.TryGetValue(id, out API.APIProtocol? client))
                    client.Send(msg);
            }
        }

        protected override void OnClientRegistered(API.APIProtocol client, string path) => m_Clients[client.ID] = client;

        protected override void OnClientUnregistered(string id)
        {
            m_Clients.Remove(id, out API.APIProtocol? _);
            foreach (var pair in m_Events)
                pair.Value.UnregisterClient(id);
        }

        protected override void OnClientMessage(string id, string message)
        {
            if (m_Clients.TryGetValue(id, out API.APIProtocol? client))
            {
                try
                {
                    JFile json = new(message);
                    if (json.TryGet("request", out string? request))
                    {
                        if (request == "subscribe" || request == "unsubscribe")
                        {
                            if (json.TryGet("event", out string? eventType))
                            {
                                if (m_Events.TryGetValue(eventType!, out AEventHandler? handler))
                                {
                                    if (request[0] == 's')
                                    {
                                        handler.RegisterClient(id);
                                        SendEvent(client, "subscribed", new JObject() { { "event", eventType } });
                                        return;
                                    }
                                    else
                                    {
                                        handler.UnregisterClient(id);
                                        SendEvent(client, "unsubscribed", new JObject() { { "event", eventType } });
                                        return;
                                    }
                                }
                                else
                                {
                                    SendEvent(client, "error", new JObject() { { "error", string.Format("Unknown event {0}", eventType) }, { "event", eventType } });
                                    return;
                                }
                            }
                            else
                            {
                                SendEvent(client, "error", new JObject() { { "error", "No 'event' given" } });
                                return;
                            }
                        }
                        else
                        {
                            OnUnknownRequest(client!, request!, json);
                            return;
                        }
                    }
                    else
                    {
                        SendEvent(client, "error", new JObject() { { "error", "No 'request' given" } });
                        return;
                    }
                }
                catch { }
            }
        }

        protected virtual void OnUnknownRequest(API.APIProtocol client, string request, JFile json)
        {
            SendEvent(client, "error", new JObject() { { "error", string.Format("Unknown request {0}", request) } });
        }

        public bool HaveEvent(string eventType) => m_Events.ContainsKey(eventType);

        public bool RegisterCanal(string eventType, Canal canal)
        {
            if (!m_Events.ContainsKey(eventType))
            {
                EventHandler handler = new(this, eventType);
                handler.RegisterToCanal(canal);
                m_Events[eventType] = handler;
                return true;
            }
            return false;
        }

        public bool UnregisterCanal(string eventType, Canal canal)
        {
            if (m_Events.TryGetValue(eventType, out AEventHandler? handler) && handler is EventHandler eventHandler)
            {
                eventHandler.UnregisterFromCanal(canal);
                return true;
            }
            return false;
        }

        public bool RegisterCanal<T>(string eventType, Canal<T> canal)
        {
            if (!m_Events.ContainsKey(eventType))
            {
                EventHandler<T> handler = new(this, eventType);
                handler.RegisterToCanal(canal);
                m_Events[eventType] = handler;
                return true;
            }
            return false;
        }

        public bool UnregisterCanal<T>(string eventType, Canal<T> canal)
        {
            if (m_Events.TryGetValue(eventType, out AEventHandler? handler) && handler is EventHandler<T> eventHandler)
            {
                eventHandler.UnregisterFromCanal(canal);
                return true;
            }
            return false;
        }
    }
}
