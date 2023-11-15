namespace RisuDeepL;

using EmbedIO;
using EmbedIO.WebSockets;

using Microsoft.Web.WebView2.Core;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public const string DEEPLURL = "https://www.deepl.com/translator";
    public const string RPCURL = "https://www2.deepl.com/jsonrpc?method=LMT_handle_jobs";

    private const string ServerURL = "http://localhost:5858/";
    private const string ServerPath = "/translate";

    private readonly TranslateServer server;
    private string lastText = string.Empty;

    public MainWindow() {
        InitializeComponent();
        webView.EnsureCoreWebView2Async();

        server = new TranslateServer(GetURL(), ServerPath);
        server.TranslateRequestedAsync = TranslateAsync;
        txtURL.Text = server.URL;
        txtURL.Background = Brushes.Orange;
    }

    private void WebView_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e) {
        webView.CoreWebView2.IsMuted = true;
        webView.CoreWebView2.WebResourceResponseReceived += WebView_WebResourceResponseReceived;
        webView.CoreWebView2.Navigate(DEEPLURL);
    }

    private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e) {
        txtURL.Background = Brushes.LightGreen;
        server.Start();
    }

    private async void WebView_WebResourceResponseReceived(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs e) {
        if (e.Request.Method != "POST")
            return;
        if (e.Request.Uri != RPCURL)
            return;
        if (e.Response.StatusCode != 200)
            return;

        string js = """document.querySelector("div[aria-labelledby=translation-target-heading]").innerText""";
        string result = JsonSerializer.Deserialize<string>(await webView.CoreWebView2.ExecuteScriptAsync(js));
        _ = server.SetTranslateResponseAsync(new string[] { result });

        /*
        using Stream stream = await e.Response.GetContentAsync();
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        buffer.Position = 0;

        try {
            DEEPLRPC rpc = await JsonSerializer.DeserializeAsync<DEEPLRPC>(buffer);
            IEnumerable<string> results = rpc.Result.Translations.SelectMany((v) => v.Beams)
                .Select((v) => string.Join(string.Empty, v.Sentences.Select((s) => s.Text)));
            _ = server.SetTranslateResponseAsync(results);
        } catch { }
        */
    }

    private async Task TranslateAsync(string text, string from, string to) {
        await Dispatcher.InvokeAsync(async () => {
            if (lastText == text) return;
            lastText = text;
            string js =
                    $"""
                    document.getSelection().selectAllChildren(document.querySelector("div[aria-labelledby=translation-source-heading]"));
                    document.execCommand("delete");
                    document.execCommand("insertText", null, {HttpUtility.JavaScriptStringEncode(text, true)});
                    """;
            await webView.CoreWebView2.ExecuteScriptAsync(js);
        });
    }

    private static string GetURL() {
        string[] args = Environment.GetCommandLineArgs();
        if (2 <= args.Length) {
            if (Uri.TryCreate(args[1], UriKind.Absolute, out Uri? url)) {
                if (url.Scheme == "http" || url.Scheme == "https") {
                    return url.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped);
                }
            }
        }
        return ServerURL; 
    }

    private struct DEEPLRPC {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("result")]
        public RPCResult Result { get; set; }

        public struct RPCResult {
            [JsonPropertyName("translations")]
            public Translation[] Translations { get; set; }

            [JsonPropertyName("target_lang")]
            public string TargetLang { get; set; }

            [JsonPropertyName("source_lang")]
            public string SourceLang { get; set; }
        }

        public struct Translation {
            [JsonPropertyName("beams")]
            public Beam[] Beams { get; set; }

            //[JsonPropertyName("quality")]
            //public string Quality { get; set; }
        }

        public struct Beam {
            [JsonPropertyName("sentences")]
            public Sentence[] Sentences { get; set; }

            //[JsonPropertyName("num_symbols")]
            //public int NumSymbols { get; set; }
        }

        public struct Sentence {
            [JsonPropertyName("text")]
            public string Text { get; set; }

            //[JsonPropertyName("ids")]
            //public long[] Ids { get; set; }
        }
    }
}
