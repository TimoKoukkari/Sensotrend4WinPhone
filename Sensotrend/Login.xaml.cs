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
using System.Xml;
using System.Security.Cryptography;
using Sensotrend.TaltioniService;
using Newtonsoft.Json;

namespace Sensotrend
{
    public partial class Page1 : PhoneApplicationPage
    {
        //private const string OAuthVersion = "2.0";

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
                
            AuthenticationBrowser.Navigate(loginUrl);

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

            string uriStr = e.Uri.AbsoluteUri;
            if (uriStr.Contains("?code="))
            {
                AuthenticationBrowser.Visibility = Visibility.Collapsed;

                int authCodeStart = uriStr.IndexOf("=") + 1;
                authCode = uriStr.Substring(authCodeStart);

                if (!string.IsNullOrEmpty(authCode))
                {
                    RetrieveAccessToken();
                }

                if (string.IsNullOrEmpty(authCode))
                {
                    Dispatcher.BeginInvoke(() => MessageBox.Show("Authorization denied by user"));
                }

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

                            accessToken = ExtractTokenInfo(responseText);

                            App.accessToken = accessToken;

                            Dispatcher.BeginInvoke(() =>
                            {
                                MessageBox.Show("Access granted, token: " + accessToken);
                                NavigationService.GoBack();

                            });
                        }
                    }
                    catch
                    {
                        Dispatcher.BeginInvoke(() => 
                        {
                            MessageBox.Show("Unable to retrieve Access Token");
                            NavigationService.GoBack();
                        });
                        
                    }
                }, req);
            }, request);
            
        }

        // Extracts access/request token and its secret from key/value pairs in input string
        // read from response.
        private string ExtractTokenInfo(string responseText)
        {

            if (string.IsNullOrEmpty(responseText))
                return null;

            Dictionary<string, string> tokenInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);

            string token = tokenInfo["access_token"];

            return token;
        }

        private WebRequest CreateTokenRequest()
        {           
            Uri requestUrl = new Uri(REQUEST_TOKEN_URI);

            WebRequest request = WebRequest.CreateHttp(requestUrl);
            request.Method = "POST";
           
            IDictionary<string, string> requestParameters = new Dictionary<string, string>();
            requestParameters["USER_NAME"] = CLIENT_ID;
            requestParameters["PASSWORD"] = APPLICATION_ID;
            // Create the authorization header. Does Taltioni require some sort of user name & password here?
            request.Headers[HttpRequestHeader.Authorization] = GenerateAuthorizationHeader(requestParameters);

            return request;
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
        private void testAccessToken()
        {

            Guid requestId = Guid.NewGuid(); 
            string timestamp = XmlConvert.ToString(DateTime.UtcNow, XmlDateTimeSerializationMode.Utc); 
            SHA256Managed hashAlgorithm = new SHA256Managed(); 
            string toHash = requestId.ToString() + ";" + timestamp + ";" + APPLICATION_ID + ";" + accessToken + ";" + SharedSecret; 
            byte[] authCode = hashAlgorithm.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toHash));

            bool filterByApplicationId = true;
            DateTime? startDate = null; 
            DateTime? endDate = null;
            using (var client = new TaltioniAPIClient())
            {
                client.
                var data = client.GetHealthRecordItems(accessToken, 
                    APPLICATION_ID, authCode, ref requestId, 
                    timestamp, startDate, endDate, 
                    new string[] { "Weight" }, 
                    filterByApplicationId);
                var observations = data.Observations;
            }
        }
     
  */

        /* The token request response from Taltioni is in JSON and the format is:
        {
             "access_token":"33369431943e4fadb2629bb66a8dafa4",
             "token_type":"taltioni_token"
        }
         http://json2csharp.com/  converted it to the folowing class:       
        */

        private class TokenInfo
        {
            [JsonProperty ("access_token")]
            public string access_token { get; set; }
            [JsonProperty("token_type")]
            public string token_type { get; set; }
        }
    
    }

}