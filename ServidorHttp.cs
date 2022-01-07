using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class ServidorHttp
{
    private TcpListener Controlador { get; set; }
    private int Porta { get; set; }
    private int QtdRequests { get; set; }
    private SortedList<string, string> TiposMime { get; set; }
    private SortedList<string, string> DiretoriosHosts { get; set; }
    public ServidorHttp(int porta = 3000)
    {
        this.PopularTiposMime();
        this.PopularDiretorioHosts();
        this.Porta = porta;

        try
        {
            this.Controlador = new TcpListener(IPAddress.Parse("127.0.0.1"), this.Porta);
            this.Controlador.Start();

            Console.WriteLine($"Servidor HTTP está rodando na porta {this.Porta}.");
            Console.WriteLine($"Para acessar, digite no navegador: http://localhost:{this.Porta}");

            Task servidorHttpTask = Task.Run(() => AguardarRequests());
            servidorHttpTask.GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao iniciar o servidor na porta {this.Porta}:\n{ex.Message}");
        }
    }

    private async Task AguardarRequests()
    {
        while (true)
        {
            Socket conexao = await this.Controlador.AcceptSocketAsync();
            this.QtdRequests++;
            Task task = Task.Run(() => ProcessarRequest(conexao, this.QtdRequests));
        }
    }

    private void ProcessarRequest(Socket conexao, int numeroRequest)
    {
        Console.WriteLine($"Processando request #{numeroRequest}...\n");

        if (conexao.Connected)
        {
            byte[] bytesRequisicao = new byte[1024];
            conexao.Receive(bytesRequisicao, bytesRequisicao.Length, 0);
            var textoRequisicao = Encoding.UTF8.GetString(bytesRequisicao).Replace((char)0, ' ').Trim();
            if (textoRequisicao.Length > 0)
            {
                Console.WriteLine($"\n{textoRequisicao}\n");

                string[] linhas = textoRequisicao.Split("\r\n");
                int iPrimeiroEspaco = linhas[0].IndexOf(' ');
                int iSegundoEspaco = linhas[0].LastIndexOf(' ');

                var metodoHttp = linhas[0].Substring(0, iPrimeiroEspaco);
                var recursoBuscado = linhas[0].Substring(iPrimeiroEspaco + 2, iSegundoEspaco - (iPrimeiroEspaco + 2));

                if (string.IsNullOrEmpty(recursoBuscado))
                    recursoBuscado = "index.html";

                var versaoHttp = linhas[0].Substring(iSegundoEspaco + 1);

                iPrimeiroEspaco = linhas[1].IndexOf(' ');
                var nomeHost = linhas[1].Substring(iPrimeiroEspaco + 1);

                byte[] bytesCabecalho = null;
                byte[] bytesConteudo = null;

                FileInfo fiArquivo = new FileInfo(ObterCaminhoFisicoRecurso(nomeHost, recursoBuscado));

                if (fiArquivo.Exists)
                {
                    if (TiposMime.ContainsKey(fiArquivo.Extension.ToLower()))
                    {
                        bytesConteudo = File.ReadAllBytes(fiArquivo.FullName);
                        string tipoMime = TiposMime[fiArquivo.Extension.ToLower()];
                        bytesCabecalho = GerarCabecalho(versaoHttp, tipoMime, "200", bytesConteudo.Length);
                    }
                    else
                    {
                        bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 415 - Tipo de arquivo não suportado</h1>");
                        bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "415", bytesConteudo.Length);
                    }
                }
                else
                {
                    bytesConteudo = Encoding.UTF8.GetBytes("<h1>Erro 404 - recurso não encontrado</h1>");
                    bytesCabecalho = GerarCabecalho(versaoHttp, "text/html;charset=utf-8", "404", bytesConteudo.Length);
                }

                int bytesEnviados = conexao.Send(bytesCabecalho, bytesCabecalho.Length, 0);
                bytesEnviados += conexao.Send(bytesConteudo, bytesConteudo.Length, 0);

                conexao.Close();

                Console.WriteLine($"\n{bytesEnviados} bytes enviados em resposta à requisição #{numeroRequest}.");

            }
        }

        Console.WriteLine($"\nRequest #{numeroRequest} finalizado.");
    }

    private byte[] GerarCabecalho(string versaoHttp, string tipoMime, string codigoHttp, int qtdBytes = 0)
    {
        StringBuilder cabecalho = new StringBuilder();
        cabecalho.Append($"{versaoHttp} {codigoHttp}{Environment.NewLine}");
        cabecalho.Append($"Server: Servidor Http muri11o{Environment.NewLine}");
        cabecalho.Append($"Content-Type: {tipoMime}{Environment.NewLine}");
        cabecalho.Append($"Content-Length: {qtdBytes}{Environment.NewLine}{Environment.NewLine}");

        return Encoding.UTF8.GetBytes(cabecalho.ToString());
    }

    private void PopularTiposMime()
    {
        this.TiposMime = new SortedList<string, string>();
        this.TiposMime.Add(".html", "text/html;charset=utf-8");
        this.TiposMime.Add(".htm", "text/html;charset=utf-8");
        this.TiposMime.Add(".css", "text/css");
        this.TiposMime.Add(".js", "text/javascript");
        this.TiposMime.Add(".png", "image/png");
        this.TiposMime.Add(".jpg", "image/jpeg");
        this.TiposMime.Add(".gif", "image/gif");
        this.TiposMime.Add(".svg", "image/svg+xml");
        this.TiposMime.Add(".webp", "image/webp");
        this.TiposMime.Add(".ico", "image/ico");
        this.TiposMime.Add(".woff", "font/woff");
        this.TiposMime.Add(".woff2", "font/woff2");
    }

    private void PopularDiretorioHosts()
    {
        this.DiretoriosHosts = new SortedList<string, string>();
        this.DiretoriosHosts.Add("localhost", Path.Combine(ObterCaminhoFisicoRecurso(), "localhost"));
        this.DiretoriosHosts.Add("muri11o.com", Path.Combine(ObterCaminhoFisicoRecurso(), "muri11o.com"));
    }

    private string ObterCaminhoFisicoRecurso()
    {
        var path = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "www");

        return path;
    }

    private string ObterCaminhoFisicoRecurso(string host, string recurso)
    {
        var diretorio = this.DiretoriosHosts[host.Split(":")[0]];
        var path = Path.Combine(diretorio, recurso);

        return path;
    }


}