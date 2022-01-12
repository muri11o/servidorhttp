using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ServerHttp.Interfaces;

namespace ServerHttp.Classes
{
    class ServerHttpHandler : IServerHttp
    {
        public TcpListener SocketListener { get; private set; }
        public IPAddress IP { get; private set; }
        public int Port { get; private set; }
        private int _numberOfRequest { get; set; }
        private SortedList<string, string> _typesMime { get; set; }
        private SortedList<string, string> _sitesDirectory { get; set; }
        private readonly ILogger<ServerHttpHandler> _logger;
        public ServerHttpHandler(ILogger<ServerHttpHandler> logger)
        {
            this._logger = logger;
        }
        public void StartServer(int port = 3000)
        {
            try
            {
                this.FillMimeTypes();
                this.LoadSites();

                this.IP = IPAddress.Any;
                this.Port = port;
                this.SocketListener = new TcpListener(this.IP, this.Port);
                this.SocketListener.Start();

                _logger.LogInformation($"The server is running on the port {this.Port}.");
                PrintSites();
                _logger.LogInformation($"To access the site, type in your browser: http://[site]:{this.Port}");

                Task servidorHttpTask = Task.Run(() => WaitForRequests());
                servidorHttpTask.GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error starting server on port {this.Port}:\n{ex.Message}");
            }
        }
        private async Task WaitForRequests()
        {
            while (true)
            {
                Socket connection = await this.SocketListener.AcceptSocketAsync();
                this._numberOfRequest++;
                Task task = Task.Run(() => ProcessRequisition(connection, this._numberOfRequest));
            }
        }

        private void ProcessRequisition(Socket connection, int requestNumber)
        {
            _logger.LogInformation($"Processing request #{requestNumber}...\n");

            if (connection.Connected)
            {
                byte[] requestBytes = new byte[1024];
                connection.Receive(requestBytes, requestBytes.Length, 0);
                var requestText = Encoding.UTF8.GetString(requestBytes).Replace((char)0, ' ').Trim();

                if (requestText.Length > 0)
                {
                    _logger.LogInformation($"\n{requestText}\n");

                    string[] lines = requestText.Split("\r\n");
                    int iFirstSpace = lines[0].IndexOf(' ');
                    int iSecondSpace = lines[0].LastIndexOf(' ');

                    var httpMethod = lines[0].Substring(0, iFirstSpace);
                    var resource = lines[0].Substring(iFirstSpace + 2, iSecondSpace - (iFirstSpace + 2));

                    if (string.IsNullOrEmpty(resource))
                        resource = "index.html";

                    var httpVersion = lines[0].Substring(iSecondSpace + 1);

                    iFirstSpace = lines[1].IndexOf(' ');
                    var nameHost = lines[1].Substring(iFirstSpace + 1);

                    byte[] headerBytes = null;
                    byte[] contentBytes = null;

                    FileInfo fiFile = new FileInfo(GetPhysicalPathOfResource(nameHost, resource));

                    if (fiFile.Exists)
                    {
                        if (_typesMime.ContainsKey(fiFile.Extension.ToLower()))
                        {
                            contentBytes = File.ReadAllBytes(fiFile.FullName);
                            string typeMime = _typesMime[fiFile.Extension.ToLower()];
                            headerBytes = GenerateHeader(httpVersion, typeMime, "200", contentBytes.Length);
                        }
                        else
                        {
                            contentBytes = Encoding.UTF8.GetBytes("<h1>Erro 415 - Unsupported file type</h1>");
                            headerBytes = GenerateHeader(httpVersion, "text/html;charset=utf-8", "415", contentBytes.Length);
                        }
                    }
                    else
                    {
                        contentBytes = Encoding.UTF8.GetBytes("<h1>Erro 404 - Resource not found</h1>");
                        headerBytes = GenerateHeader(httpVersion, "text/html;charset=utf-8", "404", contentBytes.Length);
                    }

                    int sendBytes = connection.Send(headerBytes, headerBytes.Length, 0);
                    sendBytes += connection.Send(contentBytes, contentBytes.Length, 0);

                    connection.Close();

                    _logger.LogInformation($"\n{sendBytes} bytes sent in response to request #{requestNumber}.");

                }
            }

            _logger.LogInformation($"\nRequest #{requestNumber} finished.");
        }

        private byte[] GenerateHeader(string versaoHttp, string tipoMime, string codigoHttp, int qtdBytes = 0)
        {
            StringBuilder cabecalho = new StringBuilder();
            cabecalho.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
            cabecalho.Append($"Server: Servidor Http muri11o{Environment.NewLine}");
            cabecalho.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
            cabecalho.Append($"Content-Length: {qtdBytes}{Environment.NewLine}{Environment.NewLine}");

            return Encoding.UTF8.GetBytes(cabecalho.ToString());
        }

        private void FillMimeTypes()
        {
            this._typesMime = new SortedList<string, string>();
            this._typesMime.Add(".html", "text/html;charset=utf-8");
            this._typesMime.Add(".htm", "text/html;charset=utf-8");
            this._typesMime.Add(".css", "text/css");
            this._typesMime.Add(".js", "text/javascript");
            this._typesMime.Add(".png", "image/png");
            this._typesMime.Add(".jpg", "image/jpeg");
            this._typesMime.Add(".gif", "image/gif");
            this._typesMime.Add(".svg", "image/svg+xml");
            this._typesMime.Add(".webp", "image/webp");
            this._typesMime.Add(".ico", "image/ico");
            this._typesMime.Add(".woff", "font/woff");
            this._typesMime.Add(".woff2", "font/woff2");
        }

        private void LoadSites()
        {
            this._sitesDirectory = new SortedList<string, string>();
            var sites = Directory.GetDirectories(Path.Combine(GetPhysicalPathOfResource()));

            foreach (var site in sites)
            {
                var siteName = site.Substring(site.LastIndexOf('/') + 1);
                this._sitesDirectory.Add(siteName, Path.Combine(GetPhysicalPathOfResource(), siteName));
            }
        }

        private void PrintSites()
        {
            _logger.LogInformation("Site list:");
            int i = 1;
            foreach (var site in _sitesDirectory)
            {
                Console.WriteLine($"\t\t {i} - {site.Key}");
                i++;
            }
        }
        private string GetPhysicalPathOfResource()
        {
            var path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "www");

            return path;
        }

        private string GetPhysicalPathOfResource(string host, string recurso)
        {
            var diretorio = this._sitesDirectory[host.Split(":")[0]];
            var path = Path.Combine(diretorio, recurso);

            return path;
        }
    }
}