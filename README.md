Welcome to Dome.

This is .NET library of our service. 

Requrements:
 - SignalR.Client >= 2.0.0
 
 Usage:
 
      var dome = new DomeSupport("API-ID","API-KEY","Device Unique Id","Push Token");
      dome.OnMessage += Dome_OnMessage;
      dome.SendMessage("Hello, i have some problem.");
