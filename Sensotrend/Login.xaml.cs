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
using System.Text.RegularExpressions;

namespace Sensotrend
{
    public partial class Page1 : PhoneApplicationPage
    {
        private const string OAuthVersion = "2.0";

        // OAuth signature method
        //private const string Hmacsha1SignatureType = "HMAC-SHA1";

        //private const string RequestUrl = "https://asiakastesti.taltioni.fi/OAuth/RequestToken";
        //private const string AUTHORIZATION_LOCATION = "https://asiakastesti.taltioni.fi/OAuth/Index";
        //private const string TOKEN_LOCATION = "https://asiakastesti.taltioni.fi/OAuth/RequestToken";

        //private const string AUTHORIZATION_URI = "https://asiakastesti.taltioni.fi/Authorize";
        //private const string REQUEST_TOKEN_URI = "https://asiakastesti.taltioni.fi/RequestToken";

        private const string AUTHORIZATION_URI = "https://asiakastesti.taltioni.fi/OAuth/Index";
        private const string REQUEST_TOKEN_URI = "https://asiakastesti.taltioni.fi/OAuth/RequestToken";
        //private const string REDIRECT_URI = "http://localhost:8080/Moves2Taltioni/Authentication";
        private const string REDIRECT_URI = "http://www.ideallearning.fi";

        private const string CLIENT_ID =  "testipalvelu1_OAuth";
         
        private const string GRANT_TYPE_KEY = "grant_type";
        private const string AUTH_CODE_KEY = "code";
        private const string CLIENT_ID_KEY =  "client_id";
        private const string RESPONSE_TYPE_KEY = "response_type";
        private const string ACCESS_TOKEN_KEY = "access_token";
        private const string REDIRECT_URI_KEY = "redirect_uri";

        private const string APPLICATION_ID = "jk142mxe9n9mq7n7g1psp4t1gcwdn3ut";
        private const string GRANT_TYPE_AUTH_CODE = "authorization_code";

        private string authCode;
        private string accessToken;

        /*
         
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
        */

        public Page1()
        {
            InitializeComponent();

            // Create authorization url for Taltioni:
            Uri loginUrl = new Uri(AUTHORIZATION_URI + "?response_type=code" +
                "&client_id=" + CLIENT_ID +
                "&redirect_uri=" + REDIRECT_URI);
                
            // Browser navigates to the authorization url
           // Dispatcher.BeginInvoke(() => AuthenticationBrowser.Navigate(loginUrl));
            //AuthenticationBrowser.Visibility = Visibility.Visible;
            AuthenticationBrowser.Navigate(loginUrl);

            /* Twitter specific stuff, commented out:
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
            */
        }

        private void BrowserNavigating(object sender, NavigatingEventArgs e)
        {
            if (AuthenticationBrowser.Visibility == Visibility.Visible)
            {
                AuthenticationBrowser.Visibility = Visibility.Collapsed;
            }
        }

        private void BrowserLoadCompleted(object sender, NavigationEventArgs e)
        {
            // If we have come back from the twitter authorize page
            //if (e.Uri.AbsoluteUri.ToLower().Replace("https://", "http://") == AUTHORIZATION_URI)
            //Taltioni:
            string uriStr = e.Uri.AbsoluteUri;
            if (uriStr.Contains("?code="))
            {
                AuthenticationBrowser.Visibility = Visibility.Collapsed;

                int authCodeStart = uriStr.IndexOf("=") + 1;
                authCode = uriStr.Substring(authCodeStart);

                // TO-DO: find the code parameter from uriStr and save it to authCode

                if (!string.IsNullOrEmpty(authCode))
                {
                    RetrieveAccessToken();
                }

                /*
                // Parse the verifier pin from the authorize page's XML content
                var htmlString = AuthenticationBrowser.SaveToString();

                // Find the code element and extract the pin from it
                var pinFinder = new Regex(@"<code>([A-Za-z0-9_]+)</code>", RegexOptions.IgnoreCase);

                var match = pinFinder.Match(htmlString);

                if (match.Success)
                {
                    authCode = match.Groups[1].Value;

                    // If we've got the verifier pin then next get the access token
                    if (!string.IsNullOrEmpty(authCode))
                    {
                        RetrieveAccessToken();
                    }
                }
                */

                if (string.IsNullOrEmpty(authCode))
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show("Authorization denied by user"));
                }

                // Make sure pin is reset to null
                authCode = null;
            }
            else
            {
                if (AuthenticationBrowser.Visibility == Visibility.Collapsed)
                {
                    AuthenticationBrowser.Visibility = Visibility.Visible;
                }
            }
        }

        // When we have got the verifier pin we can request the access token.
        // Sends a POST to oauth/access_token, extracts the access token from the response
        // and displays the user id and screen name from the response as well.
        public void RetrieveAccessToken()
        {
            var request = CreateTokenRequest();
           
            var body = GRANT_TYPE_KEY + "=" + GRANT_TYPE_AUTH_CODE +
            "&" + AUTH_CODE_KEY + "=" + authCode +
            "&" + REDIRECT_URI_KEY + "=" + REDIRECT_URI +
            "&" + CLIENT_ID_KEY + "=" + CLIENT_ID;

            request.BeginGetRequestStream(reqresult =>
            {
                var req = reqresult.AsyncState as HttpWebRequest;
                using (var strm = req.EndGetRequestStream(reqresult))
                using (var writer = new StreamWriter(strm))
                {
                    writer.Write(body);
                }

                req.BeginGetResponse(result =>
                {
                    try
                    {
                        var req2 = result.AsyncState as HttpWebRequest;

                        if (req2 == null)
                            throw new ArgumentNullException("result", "Request is null");

                        using (var resp = req2.EndGetResponse(result))
                        using (var strm = resp.GetResponseStream())
                        using (var reader = new StreamReader(strm))
                        {
                            var responseText = reader.ReadToEnd();

                            var userInfo = ExtractTokenInfo(responseText);

                            Dispatcher.BeginInvoke(() =>
                            {
                                MessageBox.Show("Access granted");


                            });
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(() => MessageBox.Show("Unable to retrieve Access Token"));
                    }
                }, req);
            }, request);
        }

        // Extracts access/request token and its secret from key/value pairs in input string
        // read from response.
        private IEnumerable<KeyValuePair<string, string>> ExtractTokenInfo(string responseText)
        {
            /* The response from Taltioni is in JSON and the format is:
             {
               "access_token":"33369431943e4fadb2629bb66a8dafa4",
               "token_type":"taltioni_token"
             }
             
  
             */
             
            if (string.IsNullOrEmpty(responseText))
                return null;

            string cleanedResponse = responseText.Replace("\"", "");

            var responsePairs = (from pair in cleanedResponse.Split(',')
                                 let bits = pair.Split(':')
                                 where bits.Length == 2
                                 select new KeyValuePair<string, string>(bits[0], bits[1])).ToArray();
            accessToken = responsePairs
                          .Where(kvp => kvp.Key == ACCESS_TOKEN_KEY)
                          .Select(kvp => kvp.Value).FirstOrDefault();

/*            tokenSecret = responsePairs
                                      .Where(kvp => kvp.Key == OAuthTokenSecretKey)
                                      .Select(kvp => kvp.Value).FirstOrDefault();*/

            return responsePairs;
        }

        // Creates a HTTP request complete with signed authorization header.
        // The authorization header contains all the OAuth parameters including
        // the token (request or access) and its secret, the consumer key and consumer
        // secret for the application etc.
        private WebRequest CreateTokenRequest()
        {
            
            /*
            if (requestParameters == null)
            {
                requestParameters = new Dictionary<string, string>();
            }

            //var secret = "";

            
            if (!string.IsNullOrEmpty(token))
            {
                requestParameters[OAuthTokenKey] = token;
                secret = tokenSecret;
            }
             */

            /*
            if (!string.IsNullOrEmpty(authCode))
            {
                requestParameters[AUTH_CODE_KEY] = authCode;
                requestParameters[GRANT_TYPE_KEY] = "authorization_code";
                requestParameters[CLIENT_ID_KEY] = CLIENT_ID;
            }
            */
            // Format of the request for Taltioni:
            //https://asiakastesti.taltioni.fi/RequestToken?grant_type=authorization_code&code=<code>&client_id=CLIENT_ID
            Uri requestUrl = new Uri(REQUEST_TOKEN_URI);
            /*+
                "?" + GRANT_TYPE_KEY + "=" + GRANT_TYPE_AUTH_CODE +
                "&" + AUTH_CODE_KEY + "=" + authCode +
                "&" + REDIRECT_URI_KEY + "=" + REDIRECT_URI +
                "&" + CLIENT_ID_KEY + "=" + CLIENT_ID); 
             */

            /*
            string normalizedUrl = requestUrl;

            if (!string.IsNullOrEmpty(url.Query))
            {
                normalizedUrl = requestUrl.Replace(url.Query, "");
            }
            */

            WebRequest request = WebRequest.CreateHttp(requestUrl);
            request.Method = "POST";
           
            IDictionary<string, string> requestParameters = new Dictionary<string, string>();
            requestParameters["USER_NAME"] = CLIENT_ID;
            requestParameters["PASSWORD"] = APPLICATION_ID;
            // Create the authorization header. Does Taltioni require some sort of user name & password here?
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

            paras.Append(requestParameters["USER_NAME"]);
            paras.Append(":");
            paras.Append(requestParameters["PASSWORD"]);

            byte[] utf8 = System.Text.Encoding.UTF8.GetBytes (paras.ToString());
            
            return "Basic " + Convert.ToBase64String(utf8);
        }

        /*
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
        }*/

    }
}