using Microsoft.Extensions.Configuration;

namespace XPost
{
    internal interface IRuntimeSettings{
        public bool Strict { get; }
        public string Title { get; }
        public string Body { get; }
        public string Type { get; }
        public string UserName { get; }
        public string Password { get; }
        public List<string> Communities { get; }
    }
    public interface ICastable<T> where T : class
    {
        T? Cast();
        bool TryAs(out T output);
    }
    internal class ApplicationSettings : IRuntimeSettings, ICastable<IRuntimeSettings>
    {
        public bool? Strict {get; set; }
        public string? Title  {get; set; }
        public string? Body  {get; set; } 
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? Type { get; set; }
        public List<string>? Communities {get; set; }

        public ApplicationSettings() { }

        public static ApplicationSettings Parse(string[] args)
        {
            ApplicationSettings @this = new ApplicationSettings();
            for (int i = 1; i < args.Length; i++)
                {
                start:
                var arg = args[i];
                switch (arg)
                {
                    case "--strict":
                        @this.Strict = true;
                        break;
                    case "-c":
                    case "--communities":
                            @this.Communities = new();
                        do
                        {
                            i += 1;
                            arg = args[i];
                            @this.Communities.Add(arg);
                        } while (!arg.StartsWith("-"));
                        @this.Communities.Remove(arg);
                        goto start;
                    case "-t":
                    case "--title":
                        i += 1;
                        @this.Title = args[i];
                        break;
                    case "-m":
                        i += 1;
                        @this.Body = args[i];
                        break;
                    case "--type":
                    case "-y":
                        i += 1;
                        @this.Body = args[i];
                        break;
                    case "-u":
                        i += 1;
                        @this.UserName= args[i];
                        break;
                    case "-p":
                        i += 1;
                        @this.Password = args[i];
                        break;
                    default:
                        break;
                }

            }
            return @this;
        }
        public static ApplicationSettings Parse(IConfiguration config)
        {
            var communities = config["communities"]?.Split(',');
            ApplicationSettings @this = new ApplicationSettings()
            {
                Body = config["AppSettings:Body"],
                Password = config["AppSettings:Password"],
                UserName = config["AppSettings:UserName"],
                Title = config["AppSettings:Title"],
                Type = config["AppSettings:Type"],
                Communities = communities?.ToList(),
                Strict = (Boolean.TryParse(config["strict"], out bool strict))?strict : null
            };
            

            
            return @this;
        }

        public string? PromptForString(string argument, bool required = true)
        {
            string? input = string.Empty;
        start:
            {
                Console.Write("Input " + argument + ": ");
                input = Console.ReadLine();
                if (required && string.IsNullOrEmpty(input))
                {
                    Console.WriteLine($"{argument} is required");
                    goto start;
                }
            }
            return input;
        }

        public ApplicationSettings Cascade(ApplicationSettings settings)
        {
            var merge = new ApplicationSettings();
            merge.Strict = (this.Strict != null) ? this.Strict : settings.Strict;
            merge.Title = 
                (this.Title != null)? this.Title : settings.Title;
            merge.Body =
                (this.Body != null) ? this.Body : settings.Body;
            merge.Type =
                (this.Type != null) ? this.Type : settings.Type;
            merge.UserName =
                            (this.UserName != null) ? this.UserName : settings.UserName;
            merge.Password =
                            (this.Password != null) ? this.Password : settings.Password;
            merge.Communities =
                            (this.Communities!= null) ? this.Communities: settings.Communities;
            return merge;
        }

        public IRuntimeSettings? Cast()
        {
            if( Strict is null 
               || Title is null   
                || Body    is null 
                || UserName is null 
                || Password is null 
                || Type is null
                || Communities is null){
                return null;
            }

            return this;
        }

        public bool TryAs(out IRuntimeSettings? output)
        {
            output = (IRuntimeSettings?)this;
            return this.Cast() is null;
        }

        #region IRuntimeSettings
        bool IRuntimeSettings.Strict { get => Strict ?? false; }
        String IRuntimeSettings.Title => Title ?? String.Empty;
        String IRuntimeSettings.Body => Body ?? String.Empty;
        String IRuntimeSettings.UserName => UserName ?? String.Empty;
        String IRuntimeSettings.Password => Password ?? String.Empty;
        String IRuntimeSettings.Type => Type ?? String.Empty;

        List<string> IRuntimeSettings.Communities => this.Communities ?? Enumerable.Empty<string>().ToList();

        
        #endregion
    }
}
