namespace CorpseLib.Web.API
{
    public class APIEvent
    {
        private readonly object m_Data;
        private readonly string m_Endpoint;

        public string Endpoint => m_Endpoint;
        public object Data => m_Data;

        public APIEvent(string endpoint, object data)
        {
            m_Data = data;
            m_Endpoint = endpoint;
        }
    }
}
