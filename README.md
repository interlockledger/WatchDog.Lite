# InterlockLedger.WatchDog

## Introduction

InterlockLedger.WatchDog is a Realtime Message, Event, HTTP (Request & Response) and Exception logger and viewer for ASP.Net 6 Web Apps and APIs. It allows developers log and view messages, events, http requests made to their web application and also exception caught during runtime in their web applications, all in Realtime.
It leverages `SignalR` for real-time monitoring and `LiteDb` a Serverless MongoDB-like database with no configuration.

# ![Request & Response Viewer](https://github.com/interlockledger/InterlockLedger.WatchDog/blob/main/README/watchlog.png)

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
- Separate extension method to map endpoints for better integration with complex ASP.NET apps
- Diagnostic logging when configuring middleware

## Support
- .NET 6.0 and newer

## Installation

Install via .NET CLI

```bash
dotnet add package InterlockLedger.WatchDog --version 2.0.2
```
Install via Package Manager

```bash
Install-Package InterlockLedger.WatchDog --version 2.0.2
```


## Usage
To enable InterlockLedger.WatchDog to listen for requests, use the WatchDog middleware provided by WatchDog.

### Add InterlockLedger.WatchDog Namespace to your startup code file

```c#
using InterlockLedger.WatchDog;
```


### Register WatchDog service in the services configuration part of the initialization

```c#
services.AddWatchDogServices();
```

#### `Optional` Change the folder where to create the database for the logs 

```c#
services.AddWatchDogServices(opt => 
{ 
   opt.DatabaseFolder = "c:\\temp\\watchdog"; // default uses Environment.SpecialFolder.LocalApplicationData and executing assembly name
});
```

#### `Optional` Setup AutoClear Logs 
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

### Add WatchDog middleware in the HTTP request pipeline, with required credentials to enforce

>**NOTE**
>Add Authentication option like below: `Important`

This authentication information (Username and Password) will be used to access the log viewer, unless you specify a role to check first

```c#
app.UseWatchDog(opt => 
{ 
   opt.WatchPageUsername = "admin"; 
   opt.WatchPagePassword = "Qwerty@123"; 
 });
```
>**NOTE**
> If your projects startup or program class contains app.UseMvc() or app.UseRouting() then app.UseWatchDog() should come after `Important`

#### `Optional` Specify a role that the authenticated user must have to avoid asking the credentials

```c#
app.UseWatchDog(opt => 
{ 
   opt.WatchPageUsername = "admin"; 
   opt.WatchPagePassword = "Qwerty@123"; 
   opt.RequiredRole = "LogViewer";
 });
```

#### `Optional` Add list of routes you want to ignore when logging requests
List of routes, paths or specific strings to be ignored should be a comma separated string like below.

```c#
app.UseWatchDog(opt => 
{ 
   opt.WatchPageUsername = "admin"; 
   opt.WatchPagePassword = "Qwerty@123"; 
   opt.Blacklist = "Test/testPost,weatherforecast";
 });
```

#### `Optional` Activate WatchDog Exception Logger
This is used to log in-app exceptions that occur during the processing of a particular HTTP request.

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

### Map the WatchDog Services/UI in the endpoints
This will make the LogViewer available at /watchdog

```c#
app.UseEndpoints(endpoints => {
    endpoints.MapControllers();
    // map other needed things ...
    endpoints.MapWatchDog();
});
```

### Log Messages/Events
```
WatchLogger.Log("...TestGet Started...");
```

### View Logs and Exception
Start your server and head to `/watchdog` to view the logs.
>Example: https://myserver.com/watchdog or https://localhost:[your-port]/watchdog


Still confused? Check out the implementation in the [WatchDogCompleteApiNet6](https://github.com/interlockledger/InterlockLedger.WatchDog/tree/main/WatchDogCompleteApiNet6) folder.


## Example Screens
# ![Login page](https://github.com/interlockledger/InterlockLedger.WatchDog/blob/main/README/login.png)
# ![Request and Response Details](https://github.com/interlockledger/InterlockLedger.WatchDog/blob/main/README/requestLog.png)
# ![Exception Details](https://github.com/interlockledger/InterlockLedger.WatchDog/blob/main/README/exceptionLog.png)
# ![In-code log messages](https://github.com/interlockledger/InterlockLedger.WatchDog/blob/main/README/watchlog-incode.png)

## Contribution
Feel like something is missing? Fork the repo and send a PR.

Encountered a bug? Fork the repo and send a PR.

Alternatively, open an issue and we'll get to it as soon as we can.

## Credit

### Original WatchDog.NET
Kelechi Onyekwere -  [Github](https://github.com/Khelechy) [Twitter](https://twitter.com/khelechy1337)

Israel Ulelu - [Github](https://github.com/interlockledger) [Twitter](https://twitter.com/IzyPro_)

### InterlockLedger.WatchDog
Rafael *Monoman* Teixeira - [Github](https://github.com/monoman)
