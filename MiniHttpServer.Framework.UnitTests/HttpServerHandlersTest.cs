using HttpServer.Framework.Handlers;
using MiniHttpServer.Services.Authentication;
using System.Net;
using System.Net.Sockets;

namespace MiniTemplateEngine
{
    [TestClass]
    [DoNotParallelize]
    public sealed class HttpServerHandlersTest
    {
        private string originalDirectory = null!;
        private string testRoot = null!;
        private HttpListener listener = null!;
        private HttpClient client = null!;

        [TestInitialize]
        public void SetUp()
        {
            originalDirectory = Directory.GetCurrentDirectory();
            testRoot = Path.Combine(Path.GetTempPath(), "grail-http-tests-" + Guid.NewGuid());
            string staticDir = Path.Combine(testRoot, "static", "semestrovaya");
            Directory.CreateDirectory(staticDir);
            File.WriteAllText(Path.Combine(staticDir, "index.html"), "<h1>Тайный Грааль</h1>");
            File.WriteAllText(Path.Combine(staticDir, "404.html"), "<h1>404</h1>");
            File.WriteAllText(Path.Combine(staticDir, "Login.html"), "<h1>Вход</h1>");
            File.WriteAllText(Path.Combine(staticDir, "admin1.html"), "<h1>Дашборд</h1>");
            File.WriteAllText(Path.Combine(staticDir, "style.css"), "body { background: white; }");
            Directory.SetCurrentDirectory(testRoot);

            int port = GetFreePort();
            string address = $"http://127.0.0.1:{port}/";
            listener = new HttpListener();
            listener.Prefixes.Add(address);
            listener.Start();

            client = new HttpClient(new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false
            })
            {
                BaseAddress = new Uri(address),
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        [TestCleanup]
        public void CleanUp()
        {
            client?.Dispose();
            listener?.Close();
            if (originalDirectory != null) Directory.SetCurrentDirectory(originalDirectory);
            if (testRoot != null && Directory.Exists(testRoot)) Directory.Delete(testRoot, true);
        }

        private static int GetFreePort()
        {
            using var socket = new TcpListener(IPAddress.Loopback, 0);
            socket.Start();
            return ((IPEndPoint)socket.LocalEndpoint).Port;
        }

        private async Task<HttpResponseMessage> SendAsync(Handler handler,
            Func<HttpClient, Task<HttpResponseMessage>> send)
        {
            Task<HttpResponseMessage> responseTask = send(client);
            var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await handler.HandleRequest(context).WaitAsync(TimeSpan.FromSeconds(5));
            return await responseTask;
        }

        [TestMethod]
        public async Task Static_Root_ReturnsHomePage()
        {
            using var response = await SendAsync(new StaticFileHandler(), c => c.GetAsync("/"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("<h1>Тайный Грааль</h1>", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task Static_Css_ReturnsCorrectContentType()
        {
            using var response = await SendAsync(new StaticFileHandler(),
                c => c.GetAsync("/semestrovaya/style.css"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("text/css", response.Content.Headers.ContentType?.MediaType);
        }

        [TestMethod]
        public async Task Static_UrlWithoutExtension_ReturnsHtml()
        {
            using var response = await SendAsync(new StaticFileHandler(),
                c => c.GetAsync("/semestrovaya/index"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("<h1>Тайный Грааль</h1>", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task Static_MissingFile_Returns404()
        {
            using var response = await SendAsync(new StaticFileHandler(),
                c => c.GetAsync("/semestrovaya/missing.html"));

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task NotFound_ReturnsProject404Page()
        {
            using var response = await SendAsync(new NotFoundHandler(), c => c.GetAsync("/missing"));

            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
            Assert.AreEqual("<h1>404</h1>", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task Controller_LoginPage_Returns200()
        {
            var handler = new ControllerHandler(new UserSessionService());
            using var response = await SendAsync(handler, c => c.GetAsync("/adminLogin"));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("<h1>Вход</h1>", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task Controller_InvalidLoginFormat_Returns400()
        {
            var handler = new ControllerHandler(new UserSessionService());
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["login"] = "admin!",
                ["password"] = "TestPassword123"
            });
            using var response = await SendAsync(handler, c => c.PostAsync("/adminLogin/login", form));

            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task Controller_AdminWithoutSession_RedirectsToLogin()
        {
            var handler = new ControllerHandler(new UserSessionService());
            using var response = await SendAsync(handler, c => c.GetAsync("/admin"));

            Assert.AreEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.AreEqual(new Uri(client.BaseAddress!, "/adminLogin"),
                new Uri(client.BaseAddress!, response.Headers.Location!.OriginalString));
        }

        [TestMethod]
        public async Task Controller_AdminWithSession_ReturnsDashboard()
        {
            var sessions = new UserSessionService();
            string sessionId = sessions.CreateSession(1);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/admin");
            request.Headers.Add("Cookie", $"sessionId={sessionId}");
            using var response = await SendAsync(new ControllerHandler(sessions), c => c.SendAsync(request));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("<h1>Дашборд</h1>", await response.Content.ReadAsStringAsync());
        }
    }
}
