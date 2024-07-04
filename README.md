A simple cli tool to create posts on Discuit without a web browser. All on the terminal. 

Configuration is multi-layered, since you can specify defaults (like username and password) by including a local json appsettings file with the exe, or set env variables on your local machine. Additionally, arguments can be passed directly to the program, overriding defaults. If a required parameter is not specified in either the local settings, or as a command line argument, you can manually enter it on each run. If you want to ignore all settings, and just enter in values directly, you would set it as interactive mode (-i).

Posting to "Discuit" in strict mode will create one post to community Discuit, while in posting to "Discuit" in not strict search will match 9 communities (DiscuitDev, DiscuitMeta, etc). The number of posts is capped at 5, any additional posts queued after 5 will be dropped.

### Build:

As a .Net application, it is a quick build. Only major pre-requisite to run is having the dotnet runtime : [.NET 8.0.x](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) Runtime. Building the project requires the sdk to run the dotnet cli tool.

Creating the application is as simple as pulling the code, and running the publish command:

`dotnet publish -c Release -r [platform]`

where -r is the target, (e.g. linux-x64, win-x64  osx-x64)

##Linux

dotnet publish -c Release -r linux-x64 -o /path/to/linux/output

##Windows

dotnet publish -c Release -r win-x64 -o /path/to/windows/output

##macOS Target

dotnet publish -c Release -r osx-x64 -o /path/to/macos/output


### How To Use
Specify a settings file:

 local.settings.json
```
"AppSettings": {
  "UserName": "mmstiver",
  "Type": "text"
}
```
Run the program:
```
|>.\xpost.exe --strict -c "programming" "dotnet" -t "XPost - A CLI for cross posting To Discuit"
|>Input Password: 
|>Input Body: As first step for a planned post scheduler,...
|>Input as many Communities (seperated by ,): programming,dotnet
|>Community programming found!
|>Community dotnet found!
|> Creating new Post......Done!
|> Creating new Post......Done!
```


Creating posts are delayed between each request to avoid rate limiting. I am not responsible for getting banned from using this tool to spam.
