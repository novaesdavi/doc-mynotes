# Weather MCP - Logging e Testes

Este repositório é um servidor MCP (Model Context Protocol) que expõe duas ferramentas: `get_forecast` e `get_alerts`.

Resumo do problema

-   O MCP usa `stdout`/`stdin` para trocar mensagens JSON-RPC com o host (ex: Claude Desktop). Qualquer texto adicional enviado para `stdout` (como logs) corrompe o fluxo JSON e causa erros do tipo "Unexpected token ..." no host.

O que foi feito

-   Os logs foram instrumentados para NÃO escrever em `stdout`. Em vez disso:
    -   Foi adicionado `FileLoggerProvider` que grava logs num ficheiro (padrão `logs/weather.log`).
    -   Adicionado `HttpLoggingHandler` para registar requisições HTTP externas (método, URI, status, tempo e erros).
    -   `WeatherTools` foi instrumentado para aceitar um `ILogger` opcional e registrar início/fim/erros.
    -   Há um modo de teste local (`WEATHER_LOCAL_TEST`) que chama `GetForecast` e gera logs de exemplo.

Arquivos relevantes

-   `Program.cs` — configura serviços, logging e o modo de teste local.
-   `WeatherTools.cs` — as ferramentas MCP instrumentadas com logs.
-   `HttpLoggingHandler.cs` — `DelegatingHandler` que faz log de requisições HTTP.
-   `FileLoggerProvider.cs` — provedor simples que grava logs em ficheiro.
-   `ErrorConsoleLoggerProvider.cs` — (opcional) provedor que grava em `stderr` (para casos de debug).

Como testar localmente

1.  No terminal, entre na pasta do projeto:

```bash
cd "C:/Users/dnova/repos/doc-mynotes/ExerciciosCurso/MCP Learning/weather"
```

2.  Executar um teste local que chama `GetForecast` e grava logs em `logs/weather.log`:

```bash
WEATHER_LOCAL_TEST=1 WEATHER_LOG_FILE=logs/weather.log dotnet run
```

3.  Ver ficheiro de log (após execução):

```bash
cat logs/weather.log
```

Variáveis de ambiente

-   `WEATHER_LOCAL_TEST=1` — ativa o modo de teste local (chama `GetForecast`).
-   `WEATHER_LOG_FILE` — caminho do ficheiro de log (padrão `logs/weather.log`).

Por que isto resolve o erro "Unexpected token"

-   Ao manter `stdout` limpo (somente JSON-RPC do MCP) e escrever logs em ficheiro ou `stderr`, o host não tentará interpretar texto de log como JSON.

Sugestões adicionais

-   Para produção, considere usar Serilog com `Serilog.Sinks.File` e rotação de ficheiros. Posso adicionar essa configuração se desejar.
-   Evite usar `Console.WriteLine` para mensagens de diagnóstico no código MCP; use `ILogger` ou escreva para `stderr`/ficheiro.

Se quiser que eu atualize o formato de log para JSON estruturado ou adicione rotação/compactação, diga-me qual opção prefere e eu implemento.

Utilizar o inspect para obter detalhes melhores para log:  
[modelcontextprotocol/inspector: Visual testing tool for MCP servers](https://github.com/modelcontextprotocol/inspector)

Comando pra tentar subir algum serviço: npx @modelcontextprotocol/inspector

npx @modelcontextprotocol/inspector dotnet run --configuration Debug