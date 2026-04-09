using Gauge.CSharp.Lib.Attribute;
using Gauge.CSharp.Lib;
using Microsoft.Playwright;
using System.Threading.Tasks;
using DotNet.Template.Config;

namespace DotNet.Template.Hooks
{
    public class SetupAndTeardown 
    {
        private const string KeyPage = "pw:page";
        private const string KeyBrowser = "pw:browser";
        private const string KeyPlaywright = "pw:playwright";
        private const string KeyContext = "pw:context";
        private const string KeyBaseUrl = "pw:base_url";

        [BeforeScenario]
        public async Task Setup()
        {
            var config = PlaywrightConfig.Load();
            var pw = await Playwright.CreateAsync();

            var browserType = config.Browser.ToLowerInvariant() switch
            {
                "firefox" => pw.Firefox,
                "webkit"  => pw.Webkit,
                _         => pw.Chromium,
            };

            var browser = await browserType.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = config.Headless,
                SlowMo   = config.SlowMo,
            });

            var ctxOptions = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = config.ViewportWidth, Height = config.ViewportHeight },
            };
            if (config.BaseUrl != null)
                ctxOptions.BaseURL = config.BaseUrl;

            var ctx = await browser.NewContextAsync(ctxOptions);
            var page = await ctx.NewPageAsync();

            ScenarioDataStore.Add(KeyPlaywright, pw);
            ScenarioDataStore.Add(KeyBrowser, browser);
            ScenarioDataStore.Add(KeyContext, ctx);
            ScenarioDataStore.Add(KeyPage, page);
            if (config.BaseUrl != null) {
                ScenarioDataStore.Add(KeyBaseUrl, config.BaseUrl);
            }
        }

        [AfterScenario]
        public async Task Teardown()
        {
            var page = ScenarioDataStore.Get<IPage>(KeyPage);
            var ctx = ScenarioDataStore.Get<IBrowserContext>(KeyContext);
            var browser = ScenarioDataStore.Get<IBrowser>(KeyBrowser);
            var pw = ScenarioDataStore.Get<IPlaywright>(KeyPlaywright);

            if (page != null) await page.CloseAsync();
            if (ctx != null) await ctx.CloseAsync();
            if (browser != null) await browser.CloseAsync();
            pw?.Dispose();
        }

    }
}