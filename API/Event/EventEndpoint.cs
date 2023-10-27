using CorpseLib.Json;
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

            protected void Emit(JObject eventData)
            {
                string msg = eventData.ToNetworkString();
                foreach (string websocketID in m_RegisteredClients)
                {
                    if (m_Manager.Clients.TryGetValue(websocketID, out API.APIProtocol? client))
                        client.Send(msg);
                }
            }
        }

        private class EventHandler : AEventHandler
        {
            public EventHandler(EventEndpoint manager, string eventType) : base(manager, eventType) { }

            public void RegisterToCanal(Canal canal) => canal.Register(Trigger);
            public void UnregisterFromCanal(Canal canal) => canal.Unregister(Trigger);

            public void Trigger() => Emit(new JObject() { { "type", "event" }, { "event", EventType }, { "data", new JObject() } });
        }

        private class EventHandler<T> : AEventHandler
        {
            public EventHandler(EventEndpoint manager, string eventType) : base(manager, eventType) { }

            public void RegisterToCanal(Canal<T> canal) => canal.Register(Emit);
            public void UnregisterFromCanal(Canal<T> canal) => canal.Unregister(Emit);

            public void Emit(T? data)
            {
                if (data == null)
                    Emit(new JObject() { { "type", "event" }, { "event", EventType }, { "data", new JObject() } });
                else
                    Emit(new JObject() { { "type", "event" }, { "event", EventType }, { "data", data } });
            }
        }

        private readonly ConcurrentDictionary<string, AEventHandler> m_Events = new();
        private readonly ConcurrentDictionary<string, API.APIProtocol> m_Clients = new();

        public EventEndpoint() : base("/event") { }
        public EventEndpoint(string path) : base(path) { }

        internal ConcurrentDictionary<string, API.APIProtocol> Clients => m_Clients;

        internal bool IsClientRegisteredToEvent(string id, string eventType)
        {
            if (m_Events.TryGetValue(eventType, out AEventHandler? value))
                return value.IsRegistered(id);
            return false;
        }

        internal bool IsClientConnected(string id) => m_Clients.ContainsKey(id);

        protected override void OnClientRegistered(API.APIProtocol client) => m_Clients[client.ID] = client;

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
                                        client.Send(new JObject() { { "type", "subscribed" }, { "event", eventType } }.ToNetworkString());
                                        return;
                                    }
                                    else
                                    {
                                        handler.UnregisterClient(id);
                                        client.Send(new JObject() { { "type", "unsubscribed" }, { "event", eventType } }.ToNetworkString());
                                        return;
                                    }
                                }
                                else
                                {
                                    client.Send(new JObject() { { "type", "error" }, { "error", string.Format("Unknown event {0}", eventType) }, { "event", eventType } }.ToNetworkString());
                                    return;
                                }
                            }
                            else
                            {
                                client.Send(new JObject() { { "type", "error" }, { "error", "No 'event' given" } }.ToNetworkString());
                                return;
                            }
                        }
                        else
                        {
                            client.Send(new JObject() { { "type", "error" }, { "error", string.Format("Unknown request {0}", request) } }.ToNetworkString());
                            return;
                        }
                    }
                    else
                    {
                        client.Send(new JObject() { { "type", "error" }, { "error", "No 'request' given" } }.ToNetworkString());
                        return;
                    }
                }
                catch { }
            }
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
