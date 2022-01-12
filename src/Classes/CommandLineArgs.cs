using CommandLine;

namespace ServerHttp.Classes
{
    public class CommandLineArgs
    {
        [Option('p', "porta", Default = 3000, Required = false, HelpText = "Port number the service will listen on")]
        public int port { get; set; }
    }
}