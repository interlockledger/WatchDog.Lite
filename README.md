# InterlockLedger.WatchDog

## Introduction

InterlockLedger.WatchDog is a Realtime Message, Event, HTTP (Request & Response) and Exception logger and viewer for ASP.Net 6 Web Apps and APIs. It allows developers log and view messages, events, http requests made to their web application and also exception caught during runtime in their web applications, all in Realtime.
It leverages `SignalR` for real-time monitoring and `LiteDb` a Serverless MongoDB-like database with no configuration.

# ![Request & Response Viewer](https://github.com/IzyPro/WatchDog/blob/main/watchlog.png)

## General Features

- RealTime HTTP Request and Response Logger
- RealTime Exception Logger
- In-code message and event logging
- User Friendly Logger Views
- Search Option for HTTP and Exception Logs
- Filtering Option for HTTP Logs using HTTP Methods and StatusCode
- Logger View Authentication
- Auto Clear Logs Option
- In-code logger for messages and events


## What's New

- Specify folder to store log database

## Support
- .NET 6.0 and newer

## Installation

Install via .NET CLI

```bash
dotnet add package InterlockLedger.WatchDog.NET --version 1.0.3
```
Install via Package Manager

```bash
Install-Package InterlockLedger.WatchDog.NET --version 1.0.3
```



## Usage
To enable InterlockLedger.WatchDog to listen for requests, use the WatchDog middleware provided by WatchDog.

Add WatchDog Namespace in `Startup.cs`

```c#
using WatchDog;
```



### Register WatchDog service in `Startup.cs` under `ConfigureService()`

```c#
services.AddWatchDogServices();
```



### Setup AutoClear Logs `Optional`
This clears the logs after a specific duration.
```c#
services.AddWatchDogServices(opt => 
{ 
   opt.UseAutoClear = true; 
});
```



>**NOTE**
>When `UseAutoClear = true`
>Default Schedule Time is set to Weekly,  override the settings like below:


```c#
services.AddWatchDogServices(opt => 
{ 
   opt.UseAutoClear = true;
   opt.ClearTimeSchedule = WatchDogAutoClearScheduleEnum.Monthly;
});
```


### Add WatchDog middleware in the HTTP request pipeline in `Startup.cs` under `Configure()`
# ![Login page sample](https://github.com/IzyPro/WatchDog/blob/main/login.png)

>**NOTE**
>Add Authentication option like below: `Important`

This authentication information (Username and Password) will be used to access the log viewer.

```c#
app.UseWatchDog(opt => 
{ 
   opt.WatchPageUsername = "admin"; 
   opt.WatchPagePassword = "Qwerty@123"; 
 });
```


>**NOTE**
> If your project uses authentication, then `app.UseWatchDog();` should come before app.UseRouting(), app.UseAuthentication(), app.UseAuthorization(), app.UseEndpoints() in that order
<!--- >If your projects startup or program class contains app.UseMvc() or app.UseRouting() then app.UseWatchDog() should come after `Important`
>If your projects startup or program class contains app.UseEndpoints() then app.UseWatchDog() should come before `Important` -->

# ![Request and Response Sample Details](https://github.com/IzyPro/WatchDog/blob/main/requestLog.png)

#### Add list of routes you want to ignore by the logger: `Optional`
List of routes, paths or specific strings to be ignored should be a comma separated string like below.

```c#
app.UseWatchDog(opt => 
{ 
   opt.WatchPageUsername = "admin"; 
   opt.WatchPagePassword = "Qwerty@123"; 
   opt.Blacklist = "Test/testPost, weatherforecast";
 });
```

#### Activate WatchDog Exception Logger `Optional`
This is used to log in-app exceptions that occur during a particular HTTP request.
# ![Exception Sample Details](https://github.com/IzyPro/WatchDog/blob/main/exceptionLog.png)

>**NOTE**
>Add the main WatchDog Middleware turning the LogExceptions option on.


```c#
app.UseWatchDog(opt => 
{ 
   opt.WatchPageUsername = "admin"; 
   opt.WatchPagePassword = "Qwerty@123"; 
   opt.LogExceptions = true;
 });
```
### Log Messages/Events
```
WatchLogger.Log("...TestGet Started...");
```
# ![In-code log messages](https://github.com/IzyPro/WatchDog/blob/main/in-code.jpeg)


### View Logs and Exception
Start your server and head to `/watchdog` to view the logs.
>Example: https://myserver.com/watchdog or https://localhost:[your-port]/watchdog

Still confused? Check out the implementation in the [WatchDogCompleteApiNet6](https://github.com/IzyPro/WatchDog/tree/main/WatchDogCompleteApiNet6) folder.

## Contribution
Feel like something is missing? Fork the repo and send a PR.

Encountered a bug? Fork the repo and send a PR.

Alternatively, open an issue and we'll get to it as soon as we can.

## Credit
Kelechi Onyekwere -  [Github](https://github.com/Khelechy) [Twitter](https://twitter.com/khelechy1337)

Israel Ulelu - [Github](https://github.com/IzyPro) [Twitter](https://twitter.com/IzyPro_)

Rafael *Monoman* Teixeira - [Github](https://github.com/monoman)
