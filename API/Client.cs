using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.API
{
    public class Client
    {
        public class APIProtocol : WebSocketProtocol
        {
            private readonly Client m_Client;

            public APIProtocol(Client client) => m_Client = client;

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

        private readonly TCPAsyncClient m_Client;
        private readonly Dictionary<string, Canal<APIEvent>> m_CanalManager = new();
        //private readonly CanalManager<string> m_CanalManager = new();
        private string? m_ID = null;
        private readonly string m_Host;
        private readonly int m_Port;
        private readonly bool m_IsSecured;

        public Client(string host, int port, bool isSecured = false)
        {
            m_Host = host;
            m_Port = port;
            m_IsSecured = isSecured;
            m_Client = new(new APIProtocol(this), URI.Build((m_IsSecured) ? "wss" : "ws").Host(m_Host).Port(m_Port).Build());
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
            if (m_CanalManager.TryGetValue(eventType, out Canal<APIEvent>? canal))
                canal.Emit(new(eventType, data));
        }

        private bool SubscriptionRequest(string path, string eventType, params string[] eventsType)
        {
            if (m_ID == null)
                return false;
            URI url = URI.Build((m_IsSecured) ? "https" : "http").Host(m_Host).Port(m_Port).Path(path).Build();
            List<string> events = new() { eventType };
            events.AddRange(eventsType);
            URLRequest request = new(url, Request.MethodType.POST, new JObject() { { "ws", m_ID }, { "events", events } });
            Response response = request.Send();
            return response.StatusCode == 200;
        }

        private bool NewCanal(string eventType)
        {
            if (!m_CanalManager.ContainsKey(eventType))
            {
                Canal<APIEvent> newCanal = new();
                m_CanalManager[eventType] = new();
                return true;
            }
            return false;
        }

        public bool Subscribe(string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest("/subscribe", eventType, eventsType))
            {
                m_CanalManager[eventType] = new();
                foreach (string @event in eventsType)
                    m_CanalManager[@event] = new();
                return true;
            }
            return false;
        }

        public bool Unsubscribe(string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest("/unsubscribe", eventType, eventsType))
            {
                m_CanalManager.Remove(eventType);
                foreach (string @event in eventsType)
                    m_CanalManager.Remove(@event);
                return true;
            }
            return false;
        }

        public bool Register(string eventType, Action<APIEvent?> listener)
        {
            if (m_CanalManager.TryGetValue(eventType, out Canal<APIEvent>? canal))
            {
                canal.Register(listener);
                return true;
            }
            return false;
        }

        public bool Unregister(string eventType, Action<APIEvent?> listener)
        {
            if (m_CanalManager.TryGetValue(eventType, out Canal<APIEvent>? canal))
            {
                canal.Unregister(listener);
                return true;
            }
            return false;
        }
    }
}
