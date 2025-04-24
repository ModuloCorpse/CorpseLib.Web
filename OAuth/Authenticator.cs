using CorpseLib.Encryption;
using CorpseLib.Network;
using CorpseLib.Web.Http;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace CorpseLib.Web.OAuth
{
    public class Authenticator(string[] scopes, string @public, string @private, string host, string authorizePath, string tokenPath, string redirectPath, int redirectPort)
    {
        private class AuthenticatorClientProtocol(Operation<RefreshToken> tokenOperation, URI redirectURL, string[] expectedScope, string publicKey, string privateKey, string pageContent, string host, string expectedState, string tokenPath) : HttpProtocol
        {
            private readonly Operation<RefreshToken> m_TokenOperation = tokenOperation;
            private readonly URI m_RedirectURL = redirectURL;
            private readonly string[] m_ExpectedScope = expectedScope;
            private readonly string m_PublicKey = publicKey;
            private readonly string m_PrivateKey = privateKey;
            private readonly string m_PageContent = pageContent;
            private readonly string m_Host = host;
            private readonly string m_ExpectedState = expectedState;
            private readonly string m_TokenPath = tokenPath;

            protected override void OnHTTPRequest(Request request)
            {
                if (request.Method == Request.MethodType.GET)
                {
                    if (request.Path.FullPath == m_RedirectURL.Path)
                    {
                        Send(new Response(200, "Ok", m_PageContent));
                        if (request.TryGetParameter("state", out string? state))
                        {
                            if (m_ExpectedState == state)
                            {
                                List<string> scopes = [];
                                if (request.TryGetParameter("scope", out string? scope))
                                    scopes.AddRange(scope!.Replace("%3A", ":").Split('+'));
                                if (request.TryGetParameter("code", out string? token) && m_ExpectedScope.All(item => scopes.Contains(item)) && scopes.All(item => m_ExpectedScope.Contains(item)))
                                    m_TokenOperation.SetResult(new(URI.Build("https").Host(m_Host).Port(443).Path(m_TokenPath).Build(), m_ExpectedScope, m_PublicKey, m_PrivateKey, token!, m_RedirectURL.ToString()));
                                else if (request.TryGetParameter("error", out string? error))
                                {
                                    if (request.TryGetParameter("error_description", out string? errorDescription))
                                        m_TokenOperation.SetError(error!, errorDescription!.Replace('+', ' '));
                                    else
                                        m_TokenOperation.SetError(error!, string.Empty);
                                }
                            }
                        }
                        else
                            m_TokenOperation.SetError("Bad response", "Received request didn't have the good state");
                    }
                    else
                        Send(new Response(404, "Not Found"));
                }
                else
                    Send(new Response(405, "Method Not Allowed"));
            }
        }

        [SupportedOSPlatform("windows")]
        private readonly WindowsEncryptionAlgorithm m_WindowsEncryptionAlgorithm = new([95, 239, 5, 252, 160, 29, 242, 88, 31, 3]);
        private Operation<RefreshToken> m_TokenOperation = new();
        private string m_PageContent = string.Empty;
        private readonly string[] m_Scopes = scopes;
        private readonly string m_PublicKey = @public;
        private readonly string m_PrivateKey = @private;
        private readonly string m_Host = host;
        private readonly string m_AuthorizePath = authorizePath;
        private readonly string m_TokenPath = tokenPath;
        private readonly string m_RedirectPath = redirectPath;
        private readonly int m_RedirectPort = redirectPort;

        public Authenticator(string[] scopes, string publicKey, string privateKey, string host) : this(scopes, publicKey, privateKey, host, "/oauth2/authorize", "/oauth2/token", "/oauth_authenticate", 80) { }
        public Authenticator(string[] scopes, string publicKey, string privateKey, string host, string redirectPath) : this(scopes, publicKey, privateKey, host, "/oauth2/authorize", "/oauth2/token", redirectPath, 80) { }
        public Authenticator(string[] scopes, string publicKey, string privateKey, string host, string redirectPath, int redirectPort) : this(scopes, publicKey, privateKey, host, "/oauth2/authorize", "/oauth2/token", redirectPath, redirectPort) { }
        public Authenticator(string[] scopes, string publicKey, string privateKey, string host, string authorizePath, string tokenPath) : this(scopes, publicKey, privateKey, host, authorizePath, tokenPath, "/oauth_authenticate", 80) { }
        public Authenticator(string[] scopes, string publicKey, string privateKey, string host, string authorizePath, string tokenPath, string redirectPath) : this(scopes, publicKey, privateKey, host, authorizePath, tokenPath, redirectPath, 80) {}

        public void SetPageContent(string content) => m_PageContent = content;

        public OperationResult<RefreshToken> ClientCredentials() => new(new(URI.Build("https").Host(m_Host).Port(443).Path(m_TokenPath).Build(), m_PublicKey, m_PrivateKey));

        public OperationResult<RefreshToken> AuthorizationCode(string browser = "")
        {
            m_TokenOperation = new();
            URI redirectURL = URI.Build("http").Host("localhost").Port(m_RedirectPort).Path(m_RedirectPath).Build();
            string expectedState = Guid.NewGuid().ToString();
            string scopeString = string.Join('+', m_Scopes).Replace(":", "%3A");
            URI oauthURL = URI.Build("https")
                .Host(m_Host)
                .Path(m_AuthorizePath)
                .Query(new URIQuery('&')
                {
                    { "response_type", "code" },
                    { "client_id", m_PublicKey },
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
            TCPAsyncServer httpServer = new(() => new AuthenticatorClientProtocol(m_TokenOperation, redirectURL, m_Scopes, m_PublicKey, m_PrivateKey, m_PageContent, m_Host, expectedState, m_TokenPath), redirectURL.Port);
            httpServer.Start();
            m_TokenOperation.Wait();
            httpServer.Stop();
            return m_TokenOperation.Result;
        }

        public void StoreToken(LocalVault vault, string key, RefreshToken token)
        {
            vault.Store(key, string.Format("{0}\n{1}", token.AccessToken, token.TokenRefresh));
        }

        public void SaveToken(string path, RefreshToken token)
        {
            string content = string.Format("{0}\n{1}", token.AccessToken, token.TokenRefresh);
            if (OperatingSystem.IsWindows())
            {
                EncryptedFile encryptedFile = new(path) { m_WindowsEncryptionAlgorithm };
                encryptedFile.Write(content);
            }
            else
                File.WriteAllText(path, content);
        }

        public RefreshToken? LoadToken(string path)
        {
            string content;
            if (OperatingSystem.IsWindows())
            {
                EncryptedFile encryptedFile = new(path) { m_WindowsEncryptionAlgorithm };
                content = encryptedFile.Read();
            }
            else
                content = File.ReadAllText(path);
            string[] lines = content.Split('\n');
            if (lines.Length == 2)
            {
                RefreshToken ret = new(m_Scopes, m_PrivateKey, URI.Build("https").Host(m_Host).Port(443).Path(m_TokenPath).Build(), lines[1], m_PublicKey, lines[0]);
                if (ret.Refresh())
                    return ret;
            }
            return null;
        }

        public RefreshToken? LoadToken(LocalVault vault, string key)
        {
            string content = vault.Load(key);
            string[] lines = content.Split('\n');
            if (lines.Length == 2)
            {
                RefreshToken ret = new(m_Scopes, m_PrivateKey, URI.Build("https").Host(m_Host).Port(443).Path(m_TokenPath).Build(), lines[1], m_PublicKey, lines[0]);
                if (ret.Refresh())
                    return ret;
            }
            return null;
        }

        public RefreshToken CreateToken(string access, string refresh) => new(m_Scopes, m_PrivateKey, URI.Build("https").Host(m_Host).Port(443).Path(m_TokenPath).Build(), refresh, m_PublicKey, access);
    }
}
