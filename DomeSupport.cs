using System;
using Microsoft.AspNet.SignalR.Client;

namespace Dome{

    /// <summary>
    /// Dome.Support .NET(C#) Library
    /// -> Using SignalR 2.2.0
    /// </summary>
    public class DomeSupport{
        /// <summary>
        /// Hub Instance
        /// </summary>
        private IHubProxy HubProxy { get; set; }

        /// <summary>
        /// DomeClientId (Based on Device Id and Push Token)
        /// </summary>
        private string DomeClientId { get; set; }

        /// <summary>
        /// Initialization of Dome
        /// </summary>
        /// <param name="apiid">Customer API-ID</param>
        /// <param name="apikey">Customer API-KEY</param>
        /// <param name="deviceId">Client Unique Device Id</param>
        /// <param name="pushToken">Client Push Token(if enabled)</param>
        /// <exception cref="ArgumentNullException">The value of 'apiid' cannot be null. </exception>
        /// <exception cref="ArgumentNullException">The value of 'apikey' cannot be null. </exception>
        /// <exception cref="Exception">A delegate callback throws an exception. </exception>
        /// <exception cref="ArgumentException">An element with the same key already exists in the <see cref="T:System.Collections.Generic.IDictionary`2" />.</exception>
        /// <exception cref="NotSupportedException">Security Headers setting error</exception>
        /// <exception cref="ObjectDisposedException">Could not start SignalR connection to server</exception>
        /// <exception cref="AggregateException">The <see cref="T:System.Threading.Tasks.Task" /> was canceled -or- an exception was thrown during the execution of the <see cref="T:System.Threading.Tasks.Task" />. If the task was canceled, the <see cref="T:System.AggregateException" /> contains an <see cref="T:System.OperationCanceledException" /> in its <see cref="P:System.AggregateException.InnerExceptions" /> collection.</exception>
        public DomeSupport(string apiid, string apikey, string deviceId, string pushToken){
            if (apiid == null) throw new ArgumentNullException("apiid");
            if (apikey == null) throw new ArgumentNullException("apikey");
            if (deviceId == null) throw new ArgumentNullException("deviceId");

            var hubConnection = new HubConnection("https://dome.support/signalr", false){
                TraceLevel = TraceLevels.All,
                TraceWriter = Console.Out
            };
            HubProxy = hubConnection.CreateHubProxy("chat");
            try{
                hubConnection.Headers.Add("X-DOME-APIID", apiid);
                hubConnection.Headers.Add("X-DOME-APIKEY", apikey);
            }
            catch (NotSupportedException notSupportedException){
                throw new NotSupportedException("Security headers setting error", notSupportedException);
            }

            //Start SignalR connection to Hub
            try{
                hubConnection.Start().ContinueWith(task => {
                    if (!task.IsFaulted) HubProxy.Invoke("HelloMyNameIs", deviceId, pushToken);
                }).Wait();
            }
            catch (ObjectDisposedException objectDisposedException){
                throw new ObjectDisposedException("Could not start SignalR connection to server", objectDisposedException);
            }

            // Server Request Methods
            HubProxy.On("HelloMyNameIsResponse", domeClientId =>{
                // Server Response with Associated DomeClientId on class init
                if (domeClientId == null) throw new ArgumentNullException("domeClientId");
                DomeClientId = domeClientId;
            });


            HubProxy.On("SupportResponse", message =>{
                // Server Response with new Message from Support service
                if (OnMessage != null)
                    OnMessage(message);
            });
        }

        public void SendMessage(string message){
            HubProxy.Invoke("MessageFromClient", DomeClientId, message);
        }

        public event Action<string> OnMessage;
    }
}