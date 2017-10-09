using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using EPiServer;
using EPiServer.ServiceLocation;
using Geta.ImageOptimization.Configuration;
using Geta.ImageOptimization.Interfaces;
using Geta.ImageOptimization.Messaging;

namespace Geta.ImageOptimization.Implementations
{
    [ServiceConfiguration(typeof(ISmushItProxy))]
    public class SmushItProxy : ISmushItProxy
    {
        private readonly WebClient _webClient = new WebClient();
        private readonly JavaScriptSerializer _javaScriptSerializer = new JavaScriptSerializer();

        public SmushItResponse ProcessImage(SmushItRequest smushItRequest)
        {
            string jsonResponse;

            string endpoint = this.BuildUrl(smushItRequest.ImageUrl);

            try
            {
                jsonResponse = this._webClient.DownloadString(endpoint);
            }
            catch (WebException exception)
            {
                throw new WebException(exception.Message);
            }

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                return this._javaScriptSerializer.Deserialize<SmushItResponse>(jsonResponse);
            }

            return new SmushItResponse { Src = smushItRequest.ImageUrl };
        }

        private string BuildUrl(string imageUrl)
        {
            string endpoint = ImageOptimizationSettings.Instance.ImageOptimizationApi ?? "http://api.resmush.it/ws.php";

            endpoint = UriSupport.AddQueryString(endpoint, "img", HttpUtility.UrlEncode(imageUrl));

            return endpoint;
        }
    }
}