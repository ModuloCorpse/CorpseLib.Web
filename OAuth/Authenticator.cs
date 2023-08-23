using CorpseLib.Network;
using CorpseLib.Web.Http;
using System.Diagnostics;

namespace CorpseLib.Web.OAuth
{
    public class Authenticator
    {
        private TaskCompletionSource<OperationResult<RefreshToken>> m_TokenTask = new TaskCompletionSource<OperationResult<RefreshToken>>();
        private string m_PageContent = string.Empty;
        private readonly string m_Host;
        private readonly string m_AuthorizePath = "/oauth2/authorize";
        private readonly string m_TokenPath = "/oauth2/token";
        private readonly string m_RedirectPath = "/oauth_authenticate";
        private readonly int m_RedirectPort = 80;

        private class AuthenticatorClientProtocol : HttpProtocol
        {
            private readonly Authenticator m_Authenticator;
            private readonly URI m_RedirectURL;
            private readonly string[] m_ExpectedScope;
            private readonly string m_PublicKey;
            private readonly string m_PrivateKey;
            private readonly string m_PageContent;
            private readonly string m_Host;
            private readonly string m_ExpectedState;
            private readonly string m_TokenPath;

            public AuthenticatorClientProtocol(Authenticator authenticator, URI redirectURL, string[] expectedScope, string publicKey, string privateKey, string pageContent, string host, string expectedState, string tokenPath)
            {
                m_Authenticator = authenticator;
                m_RedirectURL = redirectURL;
                m_ExpectedScope = expectedScope;
                m_PublicKey = publicKey;
                m_PrivateKey = privateKey;
                m_PageContent = pageContent;
                m_Host = host;
                m_ExpectedState = expectedState;
                m_TokenPath = tokenPath;
            }

            protected override void OnHTTPRequest(Request request)
            {
                if (request.Method == Request.MethodType.GET)
                {
                    if (request.Path == m_RedirectURL.Path)
                    {
                        Send(new Response(200, "Ok", m_PageContent));
                        if (request.TryGetParameter("state", out string? state))
                        {
                            if (m_ExpectedState == state)
                            {
                                List<string> scopes = new();
                                if (request.TryGetParameter("scope", out string? scope))
                                    scopes.AddRange(scope!.Replace("%3A", ":").Split('+'));
                                if (request.TryGetParameter("code", out string? token) && m_ExpectedScope.All(item => scopes.Contains(item)) && scopes.All(item => m_ExpectedScope.Contains(item)))
                                    m_Authenticator.SetRefreshToken(new(new(URI.Build("https").Host(m_Host).Port(443).Path(m_TokenPath).Build(), m_ExpectedScope, m_PublicKey, m_PrivateKey, token!, m_RedirectURL.ToString())));
                                else if (request.TryGetParameter("error", out string? error))
                                {
                                    if (request.TryGetParameter("error_description", out string? errorDescription))
                                        m_Authenticator.SetRefreshToken(new(error!, errorDescription!.Replace('+', ' ')));
                                    else
                                        m_Authenticator.SetRefreshToken(new(error!, string.Empty));
                                }
                            }
                        }
                        else
                            m_Authenticator.SetRefreshToken(new("Bad response", "Received request didn't have the good state"));
                    }
                    else
                        Send(new Response(404, "Not Found"));
                }
                else
                    Send(new Response(405, "Method Not Allowed"));
            }
        }

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

        public void SetPageContent(string content) => m_PageContent = content;

        internal void SetRefreshToken(OperationResult<RefreshToken> result) => m_TokenTask.SetResult(result);

        public OperationResult<RefreshToken> AuthorizationCode(string[] expectedScope, string publicKey, string privateKey, string browser = "")
        {
            m_TokenTask = new TaskCompletionSource<OperationResult<RefreshToken>>();
            URI redirectURL = URI.Build("http").Host("localhost").Port(m_RedirectPort).Path(m_RedirectPath).Build();
            string expectedState = Guid.NewGuid().ToString();
            string scopeString = string.Join('+', expectedScope).Replace(":", "%3A");
            URI oauthURL = URI.Build("https")
                .Host(m_Host)
                .Path(m_AuthorizePath)
                .Query(new URIQuery('&')
                {
                    { "response_type", "code" },
                    { "client_id", publicKey },
                    { "redirect_uri", redirectURL.ToString() },
                    { "scope", scopeString },
                    { "state", expectedState }
                })
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
            TCPAsyncServer httpServer = new(() => new AuthenticatorClientProtocol(this, redirectURL, expectedScope, publicKey, privateKey, m_PageContent, m_Host, expectedState, m_TokenPath), redirectURL.Port);
            httpServer.Start();
            m_TokenTask.Task.Wait();
            httpServer.Stop();
            return m_TokenTask.Task.Result;
        }
    }
}
