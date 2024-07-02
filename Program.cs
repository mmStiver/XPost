using DiscuitSharp.Core;
using System.Net;
using DiscuitSharp.Core.Content;
using Microsoft.Extensions.Configuration;
using DiscuitSharp.Core.Group;
using System.Threading.Channels;
using System.Collections.Generic;

namespace XPost;

public class Program {
    Channel<Post> msgQueue = Channel.CreateBounded<Post>(new(5), (Post) => Console.WriteLine($"Unable to Post to {Post.CommunityName}"));
    IRuntimeSettings runtimeSettings;
    DiscuitClient Client;
    internal Program(IRuntimeSettings runtimeSettings, DiscuitClient client)
    {
        this.runtimeSettings = runtimeSettings;
        Client = client;
    }
     
    public static async Task Main(string[] args)
    {
        bool interactive = args.Any(s => s.Equals("-i"));
        var builder = new ConfigurationBuilder()
                    .AddJsonFile("local.settings.json", optional: true);
         var settingArgs = ApplicationSettings.Parse(Environment.GetCommandLineArgs());
        var localSettings = ApplicationSettings.Parse(builder.Build());

        ApplicationSettings defaults = new ApplicationSettings() { 
            Strict = true
            };


        ApplicationSettings userSettings = localSettings.Cascade(settingArgs).Cascade(defaults);
        if (interactive || string.IsNullOrWhiteSpace(userSettings.UserName))
        {
            userSettings.UserName = PromptForString(nameof(userSettings.UserName), true);
        }
        if (interactive || string.IsNullOrWhiteSpace(userSettings.Password))
        {
            userSettings.Password = PromptForString(nameof(userSettings.Password), true);
        }
        if (interactive || string.IsNullOrWhiteSpace(userSettings.Type))
        {
            userSettings.Type = PromptForString(nameof(userSettings.Type), true);
        }
        if (interactive || string.IsNullOrWhiteSpace(userSettings.Title))
        {
            userSettings.Title = PromptForString(nameof(userSettings.Title), true);
        }
        if (interactive || string.IsNullOrWhiteSpace(userSettings.Body))
        {
            userSettings.Body = PromptForString(nameof(userSettings.Body), true);
        }
        if (interactive || localSettings.Communities == null)
        {
            userSettings.Communities = PromptForStringList(nameof(userSettings.Communities), true).ToList();
        }
        IRuntimeSettings? runtimeSettings = userSettings.Cast();
        if(runtimeSettings != null)
        {
            var client = InitializeClient();
            var pgm = new Program(runtimeSettings, client);
            var processing = Task.Run(async () => { await pgm.ProcessPosts(); });
            await pgm.Run();
            await processing;
        }
        else
        {
            Console.WriteLine("Configuration failed");
        }
    }

    async Task Run()
    {
        await this.Client.GetInitial();
        await this.Client.Authenticate(new DiscuitSharp.Core.Auth.Credentials(runtimeSettings.UserName, runtimeSettings.Password));
        List<Community> matchedComms = new();
        foreach(var communitySearch in this.runtimeSettings.Communities)
        {
            var communities = await Client.GetCommunities(communitySearch, DiscuitSharp.Core.Group.QueryParams.All);
            if(communities is null || communities.Count() == 0)
            {
                Console.WriteLine($"{communitySearch} could not be found");
                continue;
            }

            if (runtimeSettings.Strict)
            {
                var first = communities.Where(c => c.Name == communitySearch).SingleOrDefault();
                if (first is null) { 
                    Console.WriteLine($"{communitySearch} could not be found");
                    continue;
                }
                Console.WriteLine($"Community {first.Name} found!");

                matchedComms.Add(first);
            }else  {
                Console.WriteLine($"{communities.Count()} Communities found: {String.Join(",", communities.Select(c=>c.Name).ToArray())} found!");
                matchedComms.AddRange(communities);
            }
        }

        await SendPost(matchedComms);
        msgQueue.Writer.Complete();
        }

    private static DiscuitClient InitializeClient()
    {
        var baseAddress = new Uri("https://discuit.net/api/");
        var cookieContainer = new CookieContainer();
        var Handler = new HttpClientHandler() { CookieContainer = cookieContainer };
        var Client = new HttpClient(Handler)
        {
            BaseAddress = baseAddress
        };
        return new DiscuitClient(Client);
    }

    static string? PromptForString(string argument, bool required = true)
    {
        string? input = string.Empty;
        start:
        {
            Console.Write("Input " + argument + ": ");
            input = Console.ReadLine();
            if(required && string.IsNullOrEmpty(input))
            {
                Console.WriteLine($"{argument} is required");
                goto start;
            }
        } 
        return input;
    }
    static string[]? PromptForStringList(string argument, bool required = true)
    {
        string? input = string.Empty;
    start:
        {
            Console.Write("Input as many " + argument + " (seperated by ,): ");
            input = Console.ReadLine();
            if (required && string.IsNullOrEmpty(input))
            {
                Console.WriteLine($"{argument} is required");
                goto start;
            }
        }
        return input.Split(",");
    }
    
    public async Task SendPost(List<Community> matchedComms)
    {

        List<Post> posts = new();
        foreach (var c in matchedComms)
        {
            Post post = runtimeSettings.Type switch
            {
                "text" => new TextPost(runtimeSettings.Title, c!.Name, runtimeSettings.Body),
                "link" => new LinkPost(runtimeSettings.Title, c!.Name, new Link(runtimeSettings.Body)),
                "image" => new ImagePost(runtimeSettings.Title, c!.Name, new DiscuitSharp.Core.Media.Image() { Url = runtimeSettings .Body}),
                _ => throw new InvalidOperationException()
            };
            await this.msgQueue.Writer.WriteAsync(post);
        }
    }


    async Task ProcessPosts(CancellationToken token = default)
    {
        while (await this.msgQueue.Reader.WaitToReadAsync(token))
        {
            if (this.msgQueue.Reader.TryRead(out var msg))
            {
                Console.Write($"Creating new Post.");
                for(var count = 0; count < 5; count++) { 
                    Console.Write($".");
                    await Task.Delay(200);
                }
                if (msg is TextPost txt)
                    await Client.Create(txt);
                if (msg is LinkPost lnk)
                    await Client.Create(lnk);
                if (msg is ImagePost img)
                    await Client.Create(img);
                Console.Write($"Done!");
            }
            await Task.Delay(10000);
        }
    }
}