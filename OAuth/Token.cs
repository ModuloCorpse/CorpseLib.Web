namespace CorpseLib.Web.OAuth
{
    public class Token
    {
        private readonly string m_ClientID;
        private string m_AccessToken = string.Empty;

        protected Token(string clientID) => m_ClientID = clientID;
        public Token(string clientID, string accessToken) : this(clientID) => m_AccessToken = accessToken;

        protected void SetAccessToken(string accessToken) => m_AccessToken = accessToken;

        public string AccessToken => m_AccessToken;
        public string ClientID => m_ClientID;
    }
}
