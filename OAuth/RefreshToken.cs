using CorpseLib.DataNotation;
using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.OAuth
{
    public class RefreshToken : Token
    {
        public delegate void RefreshEventHandler(Token refreshedToken);

        private readonly string[] m_Scopes;
        private readonly string m_Secret;
        private readonly URI m_OAuthURL;
        private string m_RefreshToken = string.Empty;

        public event RefreshEventHandler? Refreshed;

        internal string[] Scopes => m_Scopes;
        internal string Secret => m_Secret;
        internal URI OAuthURL => m_OAuthURL;
        internal string TokenRefresh => m_RefreshToken;

        internal RefreshToken(string[] scopes, string secret, URI oauthURL, string refreshToken, string clientID, string accessToken) : base(clientID, accessToken)
        {
            m_Scopes = scopes;
            m_Secret = secret;
            m_OAuthURL = oauthURL;
            m_RefreshToken = refreshToken;
        }

        internal RefreshToken(URI url, string[] scopes, string publicKey, string secret, string token, string redirectURI) : base(publicKey)
        {
            m_OAuthURL = url;
            m_Scopes = scopes;
            m_Secret = secret;
            GetAccessToken(string.Format("client_id={0}&client_secret={1}&code={2}&grant_type=authorization_code&redirect_uri={3}", ClientID, m_Secret, token, redirectURI));
        }

        private bool GetAccessToken(string request)
        {
            URLRequest oauthRequest = new(m_OAuthURL, Request.MethodType.POST, request);
            oauthRequest.AddContentType(MIME.APPLICATION.X_WWW_FORM_URLENCODED);
            Response oauthResponse = oauthRequest.Send();
            string responseJsonStr = oauthResponse.Body;
            if (string.IsNullOrWhiteSpace(responseJsonStr))
                return false;
            DataObject responseJson = JsonParser.Parse(responseJsonStr);
            List<string> scope = responseJson.GetList<string>("scope");
            if (responseJson.TryGet("access_token", out string? access_token) &&
                responseJson.TryGet("refresh_token", out string? refresh_token) &&
                responseJson.TryGet("token_type", out string? token_type) && token_type! == "bearer" &&
                m_Scopes.All(scope.Contains) && scope.All(m_Scopes.Contains))
            {
                m_RefreshToken = refresh_token!;
                SetAccessToken(access_token!);
                return true;
            }
            return false;
        }

        public bool Refresh()
        {
            if (GetAccessToken(string.Format("grant_type=refresh_token&refresh_token={0}&client_id={1}&client_secret={2}", m_RefreshToken, ClientID, m_Secret)))
            {
                Refreshed?.Invoke(this);
                return true;
            }
            return false;
        }
    }
}
