using CorpseLib.Json;
using CorpseLib.Network;
using CorpseLib.Web.Http;

namespace CorpseLib.Web.OAuth
{
    public class RefreshToken
    {
        public delegate void RefreshEventHandler(RefreshToken refreshedToken);

        private readonly string[] m_Scopes;
        private readonly string m_PublicKey;
        private readonly string m_Secret;
        private readonly URI m_OAuthURL;
        private string m_RefreshToken = string.Empty;
        private string m_AccessToken = string.Empty;

        public event RefreshEventHandler? Refreshed;

        //TODO Make it internal
        public RefreshToken(URI url, string[] scopes, string publicKey, string secret, string token, string redirectURI)
        {
            m_OAuthURL = url;
            m_Scopes = scopes;
            m_PublicKey = publicKey;
            m_Secret = secret;
            GetAccessToken(string.Format("client_id={0}&client_secret={1}&code={2}&grant_type=authorization_code&redirect_uri={3}", m_PublicKey, m_Secret, token, redirectURI));
        }

        public string AccessToken => m_AccessToken;
        public string ClientID => m_PublicKey;

        private void GetAccessToken(string request)
        {
            URLRequest oauthRequest = new(m_OAuthURL, Request.MethodType.POST, request);
            oauthRequest.AddContentType(MIME.APPLICATION.X_WWW_FORM_URLENCODED);
            Response oauthResponse = oauthRequest.Send();
            string responseJsonStr = oauthResponse.Body;
            if (string.IsNullOrWhiteSpace(responseJsonStr))
                return;
            JFile responseJson = new(responseJsonStr);
            List<string> scope = responseJson.GetList<string>("scope");
            if (responseJson.TryGet("access_token", out string? access_token) &&
                responseJson.TryGet("refresh_token", out string? refresh_token) &&
                responseJson.TryGet("token_type", out string? token_type) && token_type! == "bearer" &&
                m_Scopes.All(item => scope.Contains(item)) && scope.All(item => m_Scopes.Contains(item)))
            {
                m_RefreshToken = refresh_token!;
                m_AccessToken = access_token!;
            }
        }

        public void Refresh()
        {
            GetAccessToken(string.Format("grant_type=refresh_token&refresh_token={0}&client_id={1}&client_secret={2}", m_RefreshToken, m_PublicKey, m_Secret));
            Refreshed?.Invoke(this);
        }
    }
}
