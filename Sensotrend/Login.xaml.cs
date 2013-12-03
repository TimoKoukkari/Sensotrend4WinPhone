using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Text;
using System.IO;

namespace Sensotrend
{
    public partial class Page1 : PhoneApplicationPage
    {
        private const string OAuthVersion = "2.0";

        // OAuth signature method
        private const string Hmacsha1SignatureType = "HMAC-SHA1";

        private const string RequestUrl = "https://asiakastesti.taltioni.fi/OAuth/RequestToken";
        private const string AUTHORIZATION_LOCATION = "https://asiakastesti.taltioni.fi/OAuth/Index";
        private const string TOKEN_LOCATION = "https://asiakastesti.taltioni.fi/OAuth/RequestToken";

        private const string OAuthConsumerKeyKey = "oauth_consumer_key";
        private const string OAuthVersionKey = "oauth_version";
        private const string OAuthSignatureMethodKey = "oauth_signature_method";
        private const string OAuthSignatureKey = "oauth_signature";
        private const string OAuthTimestampKey = "oauth_timestamp";
        private const string OAuthNonceKey = "oauth_nonce";
        private const string OAuthTokenKey = "oauth_token";
        private const string OAuthTokenSecretKey = "oauth_token_secret";
        private const string OAuthVerifierKey = "oauth_verifier";
        private const string OAuthPostBodyKey = "post_body";

        private string token;       // First time round the request token then becomes the access token!
        private string tokenSecret; // Ditto for the token's secret
        private string pin;

        private const string ConsumerKey = "testipalvelu1_OAuth"; //"jk142mxe9n9mq7n7g1psp4t1gcwdn3ut";
        private const string ConsumerSecret = "I5jrWs3+05AnaEkNtwYrhCHzXtCRmXkd";

        public Page1()
        {
            InitializeComponent();

            // Create the request to get a request token and its secret
            WebRequest request = CreateRequest("POST", AUTHORIZATION_LOCATION);
            request.BeginGetResponse(result =>
            {
                try
                {
                    HttpWebRequest req = result.AsyncState as HttpWebRequest;

                    if (req == null)
                        throw new ArgumentNullException("result", "Request parameter is null");

                    WebResponse resp = req.EndGetResponse(result);
                    Stream strm = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(strm);
                    string responseText = reader.ReadToEnd();

                    // Parse out the request token
                    ExtractTokenInfo(responseText);

                    // Create authorization url plus query using request token
                    Uri loginUrl = new Uri(AuthorizeUrl + "?" + OAuthTokenKey + "=" + token);  // miten tämä menee taltionissa? ks. Moves2Taltioni...
                    // Browser navigates to the authorization url
                    Dispatcher.BeginInvoke(() => AuthenticationBrowser.Navigate(loginUrl));

                }
                catch
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show("Unable to retrieve request token"));
                }
            }, request);
        }

        // Extracts access/request token and its secret from key/value pairs in input string
        // read from response.
        private IEnumerable<KeyValuePair<string, string>> ExtractTokenInfo(string responseText)
        {
            if (string.IsNullOrEmpty(responseText))
                return null;

            var responsePairs = (from pair in responseText.Split('&')
                                 let bits = pair.Split('=')
                                 where bits.Length == 2
                                 select new KeyValuePair<string, string>(bits[0], bits[1])).ToArray();
            token = responsePairs
                          .Where(kvp => kvp.Key == OAuthTokenKey)
                          .Select(kvp => kvp.Value).FirstOrDefault();

            tokenSecret = responsePairs
                                      .Where(kvp => kvp.Key == OAuthTokenSecretKey)
                                      .Select(kvp => kvp.Value).FirstOrDefault();

            return responsePairs;
        }

        // Creates a HTTP request complete with signed authorization header.
        // The authorization header contains all the OAuth parameters including
        // the token (request or access) and its secret, the consumer key and consumer
        // secret for the application etc.
        private WebRequest CreateRequest(string httpMethod, string requestUrl,
            IDictionary<string, string> requestParameters = null)
        {
            if (requestParameters == null)
            {
                requestParameters = new Dictionary<string, string>();
            }

            var secret = "";

            if (!string.IsNullOrEmpty(token))
            {
                requestParameters[OAuthTokenKey] = token;
                secret = tokenSecret;
            }

            if (!string.IsNullOrEmpty(pin))
            {
                requestParameters[OAuthVerifierKey] = pin;
            }

            Uri url = new Uri(requestUrl);
            string normalizedUrl = requestUrl;

            if (!string.IsNullOrEmpty(url.Query))
            {
                normalizedUrl = requestUrl.Replace(url.Query, "");
            }

            WebRequest request = WebRequest.CreateHttp(normalizedUrl);
            request.Method = httpMethod;

            // Create the authorization header: Concatanate the request parameters including our signature and 
            // add a "OAuth +" prefix
            request.Headers[HttpRequestHeader.Authorization] = GenerateAuthorizationHeader(requestParameters);

            return request;
        }

        /// <summary>
        /// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
        /// </summary>
        private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };
        private static readonly char[] HexUpperChars =
                                 new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        public static string UrlEncode(string value)
        {
            // Start with RFC 2396 escaping by calling the .NET method to do the work.
            // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
            // If it does, the escaping we do that follows it will be a no-op since the
            // characters we search for to replace can't possibly exist in the string.
            var escaped = new StringBuilder(Uri.EscapeDataString(value));
            foreach (string t in UriRfc3986CharsToEscape)
            {
                escaped.Replace(t, HexEscape(t[0]));
            }
            return escaped.ToString();
        }

        public static string HexEscape(char character)
        {
            var to = new char[3];
            int pos = 0;
            EscapeAsciiChar(character, to, ref pos);
            return new string(to);
        }

        private static void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = HexUpperChars[(ch & 240) >> 4];
            to[pos++] = HexUpperChars[ch & '\x000f'];
        }

        public static string GenerateAuthorizationHeader(IDictionary<string, string> requestParameters)
        {
            var paras = new StringBuilder();

            foreach (var param in requestParameters)
            {
                if (!param.Key.StartsWith("oauth_"))
                    continue;

                if (paras.Length > 0)
                    paras.Append(",");

                paras.Append(param.Key + "=\"" + param.Value + "\"");
            }
            return "OAuth " + paras;
        }

        private static readonly Random Random = new Random();
        public static string GenerateNonce()
        {
            // Random number between 123456 and 9999999
            return Random.Next(123456, 9999999).ToString();
        }

        public static string GenerateTimeStamp()
        {
            var now = DateTime.UtcNow;
            TimeSpan ts = now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds).ToString();
        }

    }
}