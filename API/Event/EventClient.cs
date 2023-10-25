using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.API.Event
{
    public class EventClient
    {
        private class EventAPIClientProtocol : WebSocketProtocol
        {
            private readonly EventClient m_Client;

            public EventAPIClientProtocol(EventClient client) => m_Client = client;

            protected override void OnWSMessage(string message)
            {
                JFile json = new(message);
                if (json.TryGet("id", out string? id))
                    m_Client.SetID(id!);
                else
                {
                    JNode? node = json.Get("data");
                    if (node != null && json.TryGet("type", out string? type))
                        m_Client.Receive(type!, node);
                }
            }
        }

        public class EventArgs
        {
            private readonly JNode m_Data;
            private readonly string m_EventType;

            public string EventType => m_EventType;
            public JNode Data => m_Data;

            public EventArgs(string eventType, JNode data)
            {
                m_Data = data;
                m_EventType = eventType;
            }

            public T? GetData<T>() => m_Data.Cast<T>();
        }

        private interface IEventCanalWrapper
        {
            public void Emit(JNode data);
        }

        private class EventCanalWrapper : IEventCanalWrapper
        {
            private readonly Canal m_Canal;
            public EventCanalWrapper(Canal canal) => m_Canal = canal;
            public void Emit(JNode data) => m_Canal.Trigger();
        }

        private class EventCanalWrapper<T> : IEventCanalWrapper
        {
            private readonly Canal<T> m_Canal;
            public EventCanalWrapper(Canal<T> canal) => m_Canal = canal;
            public void Emit(JNode data)
            {
                T? @event = data.Cast<T>();
                if (@event != null)
                    m_Canal.Emit(@event);
            }
        }

        private readonly TCPAsyncClient m_Client;
        private readonly Dictionary<string, IEventCanalWrapper> m_CanalManager = new();
        private string? m_ID = null;
        private readonly string m_Host;
        private readonly int m_Port;
        private readonly bool m_IsSecured;

        public EventClient(string host, int port, bool isSecured = false)
        {
            m_Host = host;
            m_Port = port;
            m_IsSecured = isSecured;
            m_Client = new(new EventAPIClientProtocol(this), URI.Build(m_IsSecured ? "wss" : "ws").Host(m_Host).Port(m_Port).Build());
        }

        public EventClient(string host, int port, string path, bool isSecured = false)
        {
            m_Host = host;
            m_Port = port;
            m_IsSecured = isSecured;
            m_Client = new(new EventAPIClientProtocol(this), URI.Build(m_IsSecured ? "wss" : "ws").Host(m_Host).Port(m_Port).Path(path).Build());
        }

        public bool Connect()
        {
            if (m_Client.Start())
            {
                while (m_ID == null)
                    Thread.Sleep(100);
                return true;
            }
            return false;
        }

        public void Disconnect() => m_Client.Disconnect();

        internal void SetID(string id) => m_ID = id;

        internal void Receive(string eventType, JNode data)
        {
            if (m_CanalManager.TryGetValue(eventType, out IEventCanalWrapper? canalWrapper))
                canalWrapper.Emit(data);
        }

        private bool SubscriptionRequest(string path, string eventType, params string[] eventsType)
        {
            if (m_ID == null)
                return false;
            URI url = URI.Build(m_IsSecured ? "https" : "http").Host(m_Host).Port(m_Port).Path(path).Build();
            List<string> events = new() { eventType };
            events.AddRange(eventsType);
            URLRequest request = new(url, Request.MethodType.POST, new JObject() { { "ws", m_ID }, { "events", events } });
            Response response = request.Send();
            return response.StatusCode == 200;
        }

        public bool Subscribe(Canal canal, string eventType, params string[] eventsType) => Subscribe("/subscribe", canal, eventType, eventsType);
        public bool Subscribe(string path, Canal canal, string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest(path, eventType, eventsType))
            {
                m_CanalManager[eventType] = new EventCanalWrapper(canal);
                foreach (string @event in eventsType)
                    m_CanalManager[@event] = new EventCanalWrapper(canal);
                return true;
            }
            return false;
        }

        public bool Subscribe<T>(Canal<T> canal, string eventType, params string[] eventsType) => Subscribe<T>("/subscribe", canal, eventType, eventsType);
        public bool Subscribe<T>(string path, Canal<T> canal, string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest(path, eventType, eventsType))
            {
                m_CanalManager[eventType] = new EventCanalWrapper<T>(canal);
                foreach (string @event in eventsType)
                    m_CanalManager[@event] = new EventCanalWrapper<T>(canal);
                return true;
            }
            return false;
        }

        public bool Unsubscribe(string eventType, params string[] eventsType) => UnsubscribeFromPath("/unsubscribe", eventType, eventsType);
        public bool UnsubscribeFromPath(string path, string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest(path, eventType, eventsType))
            {
                m_CanalManager.Remove(eventType);
                foreach (string @event in eventsType)
                    m_CanalManager.Remove(@event);
                return true;
            }
            return false;
        }
    }
}
