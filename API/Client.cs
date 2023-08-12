using CorpseLib.Canal;
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
        private readonly CanalManager<string> m_CanalManager = new();
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

        internal void Receive(string eventType, JNode data) => m_CanalManager.Emit(eventType, data);

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

        public bool Subscribe(string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest("/subscribe", eventType, eventsType))
            {
                m_CanalManager.NewCanal<JNode>(eventType);
                foreach (string @event in eventsType)
                    m_CanalManager.NewCanal<JNode>(@event);
                return true;
            }
            return false;
        }

        public bool Unsubscribe(string eventType, params string[] eventsType)
        {
            if (SubscriptionRequest("/unsubscribe", eventType, eventsType))
            {
                m_CanalManager.DeleteCanal(eventType);
                foreach (string @event in eventsType)
                    m_CanalManager.DeleteCanal(@event);
                return true;
            }
            return false;
        }

        public bool Register(string eventType, Action<string, object?> listener) => m_CanalManager.Register<JNode>(eventType, listener);
        public bool Unregister(string eventType, Action<string, object?> listener) => m_CanalManager.Unregister<JNode>(eventType, listener);
    }
}
