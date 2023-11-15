namespace RisuDeepL;
using EmbedIO;
using EmbedIO.WebSockets;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

internal class TranslateServer {
    private readonly WebServer server;
    private readonly WSModule socket;

    public delegate Task TranslateRequestedAsyncHandler(string text, string from, string to);
    

    public TranslateServer(string prefix, string path) {
        server = new((opt) => opt.WithUrlPrefix(prefix));
        socket = new(this, path);
        server.WithModule(socket);

        UriBuilder builder =  new (new Uri(prefix));
        builder.Scheme = builder.Scheme == "https" ? "wss" : "ws";
        URL = new Uri(builder.Uri, path).ToString();
    }

    public string URL { get; }

    public TranslateRequestedAsyncHandler TranslateRequestedAsync { get; set; }
        = (text, from, to) => Task.CompletedTask;
    
    public void Start()
        => server.Start();

    public Task SetTranslateResponseAsync(IEnumerable<string> results)
        => socket.SetTranslateResponseAsync(results);

    private class WSModule : WebSocketModule {

        public WSModule(TranslateServer server, string path) : base(path, true)
            => Server = server;

        public TranslateServer Server { get; }

        protected override async Task OnMessageReceivedAsync(IWebSocketContext context, byte[] buffer, IWebSocketReceiveResult result) {
            try {
                var data = JsonSerializer.Deserialize<RequestData>(buffer);
                await Server.TranslateRequestedAsync(data.Text, data.From, data.To);
            } catch { }
        }

        public Task SetTranslateResponseAsync(IEnumerable<string> results)
            => BroadcastAsync(JsonSerializer.Serialize(results));
    }

    private struct RequestData {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }
    }
}
