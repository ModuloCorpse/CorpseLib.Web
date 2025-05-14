using CorpseLib.DataNotation;
using CorpseLib.Json;
using System.Collections.Concurrent;

namespace CorpseLib.Web.API.Event
{
    public class EventEndpoint : AWebsocketEndpoint
    {
        private abstract class AEventHandler(EventEndpoint manager, string eventType)
        {
            private readonly EventEndpoint m_Manager = manager;
            private readonly HashSet<string> m_RegisteredClients = [];
            private readonly string m_EventType = eventType;

            public string EventType => m_EventType;

            public bool RegisterClient(string clientID) => m_RegisteredClients.Add(clientID);
            public bool UnregisterClient(string clientID) => m_RegisteredClients.Remove(clientID);
            public bool IsRegistered(string clientID) => m_RegisteredClients.Contains(clientID);

            protected void Emit(string type, DataObject eventData) => m_Manager.SendEvent([..m_RegisteredClients], type, eventData);
        }

        private class EventHandler(EventEndpoint manager, string eventType) : AEventHandler(manager, eventType)
        {
            public void RegisterToCanal(Canal canal) => canal.Register(Trigger);
            public void UnregisterFromCanal(Canal canal) => canal.Unregister(Trigger);
            public void Trigger() => Emit("event", new DataObject() { { "event", EventType }, { "data", new DataObject() } });
        }

        private class EventHandler<T>(EventEndpoint manager, string eventType) : AEventHandler(manager, eventType)
        {
            public void RegisterToCanal(Canal<T> canal) => canal.Register(Emit);
            public void UnregisterFromCanal(Canal<T> canal) => canal.Unregister(Emit);

            public void Emit(T? data)
            {
                if (data == null)
                    Emit("event", new DataObject() { { "event", EventType }, { "data", new DataObject() } });
                else
                    Emit("event", new DataObject() { { "event", EventType }, { "data", data } });
            }
        }

        private class JEventHandler<T>(EventEndpoint manager, string eventType) : AEventHandler(manager, eventType) where T : DataNode
        {
            public void RegisterToCanal(Canal<T> canal) => canal.Register(Emit);
            public void UnregisterFromCanal(Canal<T> canal) => canal.Unregister(Emit);

            public void Emit(DataNode? data)
            {
                if (data == null)
                    Emit("event", new DataObject() { { "event", EventType }, { "data", new DataValue() } });
                else
                    Emit("event", new DataObject() { { "event", EventType }, { "data", data } });
            }
        }

        private readonly ConcurrentDictionary<string, AEventHandler> m_Events = new();
        private readonly ConcurrentDictionary<string, WebsocketReference> m_Clients = new();

        public EventEndpoint() : base() { }
        public EventEndpoint(bool needExactPath) : base(needExactPath) { }

        protected void SendEvent(string id, string type, DataObject data)
        {
            if (m_Clients.TryGetValue(id, out WebsocketReference? client))
                client.Send(JsonParser.NetStr(new DataObject() { { "type", type }, { "data", data } }));
        }

        protected void SendEvent(string[] ids, string type, DataObject data)
        {
            string msg = JsonParser.NetStr(new DataObject() { { "type", type }, { "data", data } });
            foreach (string id in ids)
            {
                if (m_Clients.TryGetValue(id, out WebsocketReference? client))
                    client.Send(msg);
            }
        }

        protected override void OnClientRegistered(WebsocketReference wsReference)
        {
            m_Clients[wsReference.ClientID] = wsReference;
            wsReference.Send(JsonParser.NetStr(new DataObject() { { "type", "welcome" }, { "data", new DataObject() { { "id", wsReference.ClientID } } } }));
        }

        protected override void OnClientUnregistered(WebsocketReference wsReference)
        {
            m_Clients.Remove(wsReference.ClientID, out _);
            foreach (var pair in m_Events)
                pair.Value.UnregisterClient(wsReference.ClientID);
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

        protected override void OnClientMessage(WebsocketReference wsReference, string message) { }

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

        public bool RegisterJCanal<T>(string eventType, Canal<T> canal) where T : DataNode
        {
            if (!m_Events.ContainsKey(eventType))
            {
                JEventHandler<T> handler = new(this, eventType);
                handler.RegisterToCanal(canal);
                m_Events[eventType] = handler;
                return true;
            }
            return false;
        }

        public bool UnregisterJCanal<T>(string eventType, Canal<T> canal) where T : DataNode
        {
            if (m_Events.TryGetValue(eventType, out AEventHandler? handler) && handler is JEventHandler<T> eventHandler)
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

        protected WebsocketReference? GetClient(string id)
        {
            if (m_Clients.TryGetValue(id, out WebsocketReference? client))
                return client;
            return null;
        }
    }
}
