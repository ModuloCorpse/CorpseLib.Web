using CorpseLib.Json;
using System.Collections.Concurrent;

namespace CorpseLib.Web.API.Event
{
    public class EventEndpoint : AWebsocketEndpoint
    {
        private abstract class AEventHandler
        {
            private readonly EventEndpoint m_Manager;
            private readonly HashSet<string> m_RegisteredClients = [];
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

        private class EventHandler(EventEndpoint manager, string eventType) : AEventHandler(manager, eventType)
        {
            public void RegisterToCanal(Canal canal) => canal.Register(Trigger);
            public void UnregisterFromCanal(Canal canal) => canal.Unregister(Trigger);
            public void Trigger() => Emit("event", new JObject() { { "event", EventType }, { "data", new JObject() } });
        }

        private class EventHandler<T>(EventEndpoint manager, string eventType) : AEventHandler(manager, eventType)
        {
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
        public EventEndpoint(Http.Path path) : base(path) { }
        public EventEndpoint(string path, bool needExactPath) : base(path, needExactPath) { }
        public EventEndpoint(Http.Path path, bool needExactPath) : base(path, needExactPath) { }

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

        protected override void OnClientRegistered(API.APIProtocol client, Http.Path path)
        {
            m_Clients[client.ID] = client;
            SendEvent(client, "welcome", new() { { "id", client.ID } });
        }

        protected override void OnClientUnregistered(string id)
        {
            m_Clients.Remove(id, out API.APIProtocol? _);
            foreach (var pair in m_Events)
                pair.Value.UnregisterClient(id);
        }

        internal OperationResult RegisterClient(string id, string eventType)
        {
            if (m_Clients.ContainsKey(id))
            {
                if (m_Events.TryGetValue(eventType!, out AEventHandler? handler))
                {
                    handler.RegisterClient(id);
                    return new();
                }
                else
                    return OnRegisterToUnknownEvent(id, eventType);
            }
            else
                return new("Unknown websocket", string.Format("Websocket {0} does not exist", id));
        }

        protected virtual OperationResult OnRegisterToUnknownEvent(string id, string eventType) => new("Unknown event", string.Format("Unknown event {0}", eventType));

        internal OperationResult UnregisterClient(string id, string eventType)
        {
            if (m_Events.TryGetValue(eventType!, out AEventHandler? handler))
            {
                handler.UnregisterClient(id);
                return new();
            }
            else
                return OnUnregisterToUnknownEvent(id, eventType);
        }

        protected virtual OperationResult OnUnregisterToUnknownEvent(string id, string eventType) => new("Unknown event", string.Format("Unknown event {0}", eventType));

        protected override void OnClientMessage(string id, string message) { }

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

        protected API.APIProtocol? GetClient(string id)
        {
            if (m_Clients.TryGetValue(id, out API.APIProtocol? client))
                return client;
            return null;
        }
    }
}
