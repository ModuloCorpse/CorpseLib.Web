using CorpseLib.Json;
using CorpseLib.Network;

namespace CorpseLib.Web.API.Event
{
    public class EventClient : WebSocketProtocol
    {
        private class EventAPIClientProtocol : WebSocketProtocol
        {
            private readonly EventClient m_Client;

            public EventAPIClientProtocol(EventClient client) => m_Client = client;

            protected override void OnWSMessage(string message)
            {
                JFile json = new(message);
                if (json.TryGet("type", out string? type))
                {
                    switch (type)
                    {
                        case "subscribed":
                        {
                            if (json.TryGet("event", out string? @event))
                                m_Client.Subsribed(@event!);
                            break;
                        }
                        case "unsubscribed":
                        {
                            if (json.TryGet("event", out string? @event))
                                m_Client.Unsubsribed(@event!);
                            break;
                        }
                        case "event":
                        {
                            JNode? node = json.Get("data");
                            if (node != null && json.TryGet("event", out string? @event))
                                m_Client.Receive(@event!, node);
                            break;
                        }
                    }
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

        private readonly Dictionary<string, IEventCanalWrapper> m_AwaitingWrapper = new();
        private readonly Dictionary<string, IEventCanalWrapper> m_CanalManager = new();

        public static EventClient NewClient(string host, int port, bool isSecured = false)
        {
            EventClient eventClient = new();
            TCPAsyncClient client = new(eventClient, URI.Build(isSecured ? "wss" : "ws").Host(host).Port(port).Build());
            client.Start();
            return eventClient;
        }

        public static EventClient NewClient(string host, int port, string path, bool isSecured = false)
        {
            EventClient eventClient = new();
            TCPAsyncClient client = new(eventClient, URI.Build(isSecured ? "wss" : "ws").Host(host).Port(port).Path(path).Build());
            client.Start();
            return eventClient;
        }

        protected override void OnWSMessage(string message)
        {
            JFile json = new(message);
            if (json.TryGet("type", out string? type) && json.TryGet("data", out JObject? data))
            {
                switch (type)
                {
                    case "error":
                    {
                        if (data!.TryGet("event", out string? @event))
                            m_AwaitingWrapper.Remove(@event!);
                        break;
                    }
                    case "subscribed":
                    {
                        if (data!.TryGet("event", out string? @event))
                            Subsribed(@event!);
                        break;
                    }
                    case "unsubscribed":
                    {
                        if (data!.TryGet("event", out string? @event))
                            Unsubsribed(@event!);
                        break;
                    }
                    case "event":
                    {
                        JNode? node = data!.Get("data");
                        if (node != null && data!.TryGet("event", out string? @event))
                            Receive(@event!, node);
                        break;
                    }
                }
            }
        }

        internal void Receive(string eventType, JNode data)
        {
            if (m_CanalManager.TryGetValue(eventType, out IEventCanalWrapper? canalWrapper))
                canalWrapper.Emit(data);
        }

        internal void Subsribed(string eventType)
        {
            if (m_AwaitingWrapper.TryGetValue(eventType, out IEventCanalWrapper? wrapper))
            {
                m_CanalManager[eventType] = wrapper;
                m_AwaitingWrapper.Remove(eventType);
            }
        }

        private void Subscribe(string eventType, IEventCanalWrapper wrapper)
        {
            m_AwaitingWrapper[eventType] = wrapper;
            Send(new JObject() { { "request", "subscribe" }, { "event", eventType } }.ToNetworkString());
        }

        public void Subscribe(Canal canal, string eventType) => Subscribe(eventType, new EventCanalWrapper(canal));
        public void Subscribe<T>(Canal<T> canal, string eventType) => Subscribe(eventType, new EventCanalWrapper<T>(canal));

        private void Unsubsribed(string eventType) => m_CanalManager.Remove(eventType);
        public void Unubscribe(string eventType) => Send(new JObject() { { "request", "unsubscribe" }, { "event", eventType } }.ToNetworkString());
    }
}
