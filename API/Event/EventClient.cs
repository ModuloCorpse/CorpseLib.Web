using CorpseLib.DataNotation;
using CorpseLib.Json;
using CorpseLib.Network;

namespace CorpseLib.Web.API.Event
{
    public class EventClient : WebSocketProtocol
    {
        private class EventAPIClientProtocol(EventClient client) : WebSocketProtocol
        {
            private readonly EventClient m_Client = client;

            protected override void OnWSMessage(string message)
            {
                DataObject json = JsonParser.Parse(message);
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
                            DataNode? node = json.Get("data");
                            if (node != null && json.TryGet("event", out string? @event))
                                m_Client.Receive(@event!, node);
                            break;
                        }
                    }
                }
            }
        }

        public class EventArgs(string eventType, DataNode data)
        {
            private readonly DataNode m_Data = data;
            private readonly string m_EventType = eventType;

            public string EventType => m_EventType;
            public DataNode Data => m_Data;

            public T? GetData<T>()
            {
                DataHelper.Cast(m_Data, out T? ret);
                return ret;
            }
        }

        private interface IEventCanalWrapper
        {
            public void Emit(DataNode data);
        }

        private class EventCanalWrapper(Canal canal) : IEventCanalWrapper
        {
            private readonly Canal m_Canal = canal;

            public void Emit(DataNode data) => m_Canal.Trigger();
        }

        private class EventCanalWrapper<T>(Canal<T> canal) : IEventCanalWrapper
        {
            private readonly Canal<T> m_Canal = canal;

            public void Emit(DataNode data)
            {
                if (DataHelper.Cast(data, out T? @event) && @event != null)
                    m_Canal.Emit(@event);
            }
        }

        private readonly Dictionary<string, IEventCanalWrapper> m_AwaitingWrapper = [];
        private readonly Dictionary<string, IEventCanalWrapper> m_CanalManager = [];

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
            DataObject json = JsonParser.Parse(message);
            if (json.TryGet("type", out string? type) && json.TryGet("data", out DataObject? data))
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
                        DataNode? node = data!.Get("data");
                        if (node != null && data!.TryGet("event", out string? @event))
                            Receive(@event!, node);
                        break;
                    }
                }
            }
        }

        internal void Receive(string eventType, DataNode data)
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
            Send(JsonParser.NetStr(new DataObject() { { "request", "subscribe" }, { "event", eventType } }));
        }

        public void Subscribe(Canal canal, string eventType) => Subscribe(eventType, new EventCanalWrapper(canal));
        public void Subscribe<T>(Canal<T> canal, string eventType) => Subscribe(eventType, new EventCanalWrapper<T>(canal));

        private void Unsubsribed(string eventType) => m_CanalManager.Remove(eventType);
        public void Unubscribe(string eventType) => Send(JsonParser.NetStr(new DataObject() { { "request", "unsubscribe" }, { "event", eventType } }));
    }
}
