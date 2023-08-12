using CorpseLib.Network;
using CorpseLib.Web.Http;
using System.Diagnostics;

namespace CorpseLib.Web.OAuth
{
    public class Authenticator
    {
        private readonly string m_Host;
        private readonly string m_AuthorizePath = "/oauth2/authorize";
        private readonly string m_TokenPath = "/oauth2/token";
        private readonly string m_RedirectPath = "/oauth_authenticate";
        private readonly int m_RedirectPort = 80;

        public Authenticator(string host) => m_Host = host;

        public Authenticator(string host, string redirectPath)
        {
            m_Host = host;
            m_RedirectPath = redirectPath;
        }

        public Authenticator(string host, string redirectPath, int redirectPort)
        {
            m_Host = host;
            m_RedirectPath = redirectPath;
            m_RedirectPort = redirectPort;
        }

        public Authenticator(string host, string authorizePath, string tokenPath)
        {
            m_Host = host;
            m_AuthorizePath = authorizePath;
            m_TokenPath = tokenPath;
        }

        public Authenticator(string host, string authorizePath, string tokenPath, string redirectPath)
        {
            m_Host = host;
            m_AuthorizePath = authorizePath;
            m_TokenPath = tokenPath;
            m_RedirectPath = redirectPath;
        }

        public Authenticator(string host, string authorizePath, string tokenPath, string redirectPath, int redirectPort)
        {
            m_Host = host;
            m_AuthorizePath = authorizePath;
            m_TokenPath = tokenPath;
            m_RedirectPath = redirectPath;
            m_RedirectPort = redirectPort;
        }

        public OperationResult<RefreshToken> AuthorizationCode(string[] expectedScope, string publicKey, string privateKey, string browser = "")
        {
            URI redirectURL = URI.Build("http").Host("localhost").Port(m_RedirectPort).Path(m_RedirectPath).Build();
            string expectedState = Guid.NewGuid().ToString();
            string scopeString = string.Join('+', expectedScope).Replace(":", "%3A");
            URI oauthURL = URI.Build("https")
                .Host(m_Host)
                .Path(m_AuthorizePath)
                .Query("response_type", "code")
                .Query("client_id", publicKey)
                .Query("redirect_uri", redirectURL.ToString())
                .Query("scope", scopeString)
                .Query("state", expectedState)
                .Build();
            Process myProcess = new();
            myProcess.StartInfo.UseShellExecute = true;
            if (string.IsNullOrWhiteSpace(browser))
                myProcess.StartInfo.FileName = oauthURL.ToString();
            else
            {
                myProcess.StartInfo.FileName = browser;
                myProcess.StartInfo.Arguments = oauthURL.ToString();
            }
            myProcess.Start();
            TCPSyncServer httpServer = new(() => new HttpProtocol(), redirectURL.Port);
            httpServer.Listen();
            TCPSyncClient client = httpServer.Accept();
            List<object> messages = client.Read();
            httpServer.Stop();
            foreach (object message in messages)
            {
                if (message is Request request && request.Path == redirectURL.Path)
                {
                    if (request.TryGetParameter("state", out string? state))
                    {
                        if (expectedState == state)
                        {
                            List<string> scopes = new();
                            if (request.TryGetParameter("scope", out string? scope))
                                scopes.AddRange(scope!.Replace("%3A", ":").Split('+'));
                            if (request.TryGetParameter("code", out string? token) && expectedScope.All(item => scopes.Contains(item)) && scopes.All(item => expectedScope.Contains(item)))
                                return new(new(URI.Build("https").Host(m_Host).Path(m_TokenPath).Build(), expectedScope, publicKey, privateKey, token!, redirectURL.ToString()));
                            else if (request.TryGetParameter("error", out string? error))
                            {
                                if (request.TryGetParameter("error_description", out string? errorDescription))
                                    return new(error!, errorDescription!.Replace('+', ' '));
                                else
                                    return new(error!, string.Empty);
                            }
                        }
                    }
                    return new("Bad response", "Received request didn't have the good state");
                }
            }
            return new("Bad response", "Didn't receive requests from client");
        }
    }
}
