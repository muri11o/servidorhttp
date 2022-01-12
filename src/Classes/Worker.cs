using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServerHttp.Interfaces;

namespace ServerHttp.Classes
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServerHttp _serverHttp;
        private readonly int _port;
        public Worker(ILogger<Worker> logger, IConfiguration configuration, IServerHttp serverHttp, CommandLineArgs arg)
        {
            this._logger = logger;
            this._configuration = configuration;
            this._serverHttp = serverHttp;
            this._port = arg.port;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Task servidorHttpTask = Task.Run(() => _serverHttp.StartServer(this._port));
                await servidorHttpTask;
            }

        }
    }
}