using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;

namespace Dome{
    /// <summary>
    /// Dome.Support .NET(C#) Library
    /// -> Using SignalR 2.2.0
    /// </summary>
    public class DomeSupport{
        /// <summary>
        /// Hub Instance
        /// </summary>
        private IHubProxy HubProxy { get; }  
        /// <summary>
        /// Hub Connection 
        /// </summary>
        private HubConnection HubConnection { get; }     
        /// <summary>
        /// DomeClientId (Based on Device Id and Push Token)
        /// </summary>
        private string DomeClientId { get; set; }   
        private string ApiId { get; }     
        private string ApiKey { get; }  
        /// <summary>
        /// API Endpoint configuration
        /// </summary>
        private const string DomeApiEndpoint = "https://dome.support/api";

        /// <summary>
        /// Dispose Hub Connection on class destruction
        /// </summary>
        ~DomeSupport(){
            HubConnection.Stop();
            HubConnection.Dispose();
        }

        /// <summary>
        /// Message conversation container
        /// </summary>
        public List<TicketConversationLog> Conversation = new List<TicketConversationLog>();

        public class TicketConversationLog{
            public enum MessageType{
                FromUser,
                FromOperator
            }

            public Guid Id { get; set; }
            public string ClientId { get; set; }
            public DateTime? DateTime { get; set; }
            public MessageType Type { get; set; }
            public string MessageText { get; set; }
            public string OperatorId { get; set; }
            public string TicketId { get; set; }
        }

        /// <summary>
        /// Initialization of Dome
        /// </summary>
        /// <param name="apiId">Customer API-ID</param>
        /// <param name="apiKey">Customer API-KEY</param>
        /// <param name="deviceId">Client Unique Device Id</param>
        /// <param name="pushToken">Client Push Token(if enabled)</param>
        /// <exception cref="ArgumentNullException">The value of 'apiid' cannot be null. </exception>
        /// <exception cref="ArgumentNullException">The value of 'apikey' cannot be null. </exception>
        /// <exception cref="Exception">A delegate callback throws an exception. </exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</exception>
        /// <exception cref="NotSupportedException">Security Headers setting error</exception>
        /// <exception cref="ObjectDisposedException">Could not start SignalR connection to server</exception>
        public DomeSupport(string apiId, string apiKey, string deviceId, string pushToken){
            if (apiId == null) throw new ArgumentNullException("apiId");
            if (apiKey == null) throw new ArgumentNullException("apiKey");
            if (deviceId == null) throw new ArgumentNullException("deviceId");

            ApiId = apiId;
            ApiKey = apiKey;

            // Getting DomeClientId, based on DeviceId + PushToken
            DomeClientId = ApiHttpPostCall<string>("client", new NameValueCollection{{"deviceId", deviceId}, {"pushToken", pushToken}});
            // Get last conversation log from server
            Conversation = ApiHttpGetCall<List<TicketConversationLog>>("messages", new NameValueCollection { { "domeClientId", DomeClientId } });

            // Setting up SignalR
            HubConnection = new HubConnection("https://dome.support/signalr", false){
#if DEBUG
                TraceLevel = TraceLevels.All, 
                TraceWriter = Console.Out
#endif  
            };

            HubProxy = HubConnection.CreateHubProxy("chat");

            try{
                HubConnection.Headers.Add("X-DOME-APIID", apiId);
                HubConnection.Headers.Add("X-DOME-APIKEY", apiKey);
                HubConnection.Headers.Add("X-DOME-CLIENT", DomeClientId);
            }
            catch (NotSupportedException notSupportedException){
                throw new NotSupportedException("Security headers setting error", notSupportedException);
            }

            //Start SignalR connection to Hub
            try{
                HubConnection.Start();
            }
            catch (ObjectDisposedException objectDisposedException){
                throw new ObjectDisposedException("Could not start SignalR connection to server", objectDisposedException);
            }


            HubProxy.On("SupportResponse", message =>{
                // Server Response with new Message from Support service
                var deserialized = JsonConvert.DeserializeObject<TicketConversationLog>(message);
                if (OnMessage != null)
                    OnMessage(deserialized);
            });
        }

        /// <summary>
        /// Send Message to Support via SignalR 
        /// </summary>
        /// <param name="message">Message from Customer</param>
        public string SendMessage(string message){
            return ApiHttpPostCall<string>("messages", new NameValueCollection{{"domeClientId", DomeClientId}, {"text", message}});
        }

        private T ApiHttpPostCall<T>(string method, NameValueCollection parameters){
            using (var wc = new WebClient()){
                // Set security headers for API request 
                ApplySecurityHeaders(wc);
                // Make POST request with parameters
                var response = wc.UploadValues(string.Format("{0}/{1}", DomeApiEndpoint, method), parameters);
                // Byte to string
                var data = Encoding.Default.GetString(response);
                // Deserialize response into class
                return JsonConvert.DeserializeObject<T>(data);
            }
        }

        private T ApiHttpGetCall<T>(string method, NameValueCollection parameters){
            using (var wc = new WebClient()){
                // Set security headers for API request 
                ApplySecurityHeaders(wc);
                // Make POST request with parameters
                var response = wc.DownloadString(string.Format("{0}/{1}?{2}", DomeApiEndpoint, method, ToQueryString(parameters)));
                // Deserialize response into class
                return JsonConvert.DeserializeObject<T>(response);
            }
        }

        private static string ToQueryString(NameValueCollection parameters){
            var items = parameters.AllKeys.SelectMany(parameters.GetValues, (k, v) => k + "=" + HttpUtility.UrlEncode(v)).ToArray();
            return string.Join("&", items);
        }


        private void ApplySecurityHeaders(WebClient webClient){
            webClient.Headers.Add("X-DOME-APIID", ApiId);
            webClient.Headers.Add("X-DOME-APIKEY", ApiKey);
        }

        /// <summary>
        /// Event Delegate, Triggering, when new message from Support received
        /// </summary>
        public event Action<TicketConversationLog> OnMessage;
    }
}