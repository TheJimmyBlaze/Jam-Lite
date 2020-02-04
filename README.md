# JamLite
**Simple TCPIP based network library designed to simplify implementation of TCPIP application development in .NET.**  
  
Jam Lite is written in .NET targeting the .NET framework, but works fine for .NET core also.  
  
Creating a networking application using Jam Lite is simple and straight forward.  
Both Jam Lite Client and Server classes have a comprehensive set of events, and are flexible enough to suit many scenarios.  
And the Jam Lite Packages used to transmit data between client and server are maluable and error resistance.  
Packages must be defined with a globally unique ID number for the purposes of serialization and deserialization of objects,  
and allows for flexible organisation of these ID numbers.  
   
Jam Lite is derived from an originally much more complex and feature rich library: JamLib.  
All of the additional fluff of JamLib has been stripped away from the implementation of Jam Lite,  
with a focus on portability and ease of use.  
JamLib relied on strict inheritence rules to allow for interpretation and deserialization of messages,  
but this has been refactored in to an easy to understand set of events.  
 
Jam Lite is thread safe and makes use of many asyncronous opperation to allow for fast and light weight communication.  
Both the Jam Lite Servers and Clients implement IDisposable and can be wrapped in a 'using' statement to ensure they are disposed of elegantly.  
Jam Lite works best in conjunction with an IoC container and the Dependency Injection design pattern,  
but is free to be implemented in which ever way is appropriate for you application.  
