#nullable enable
using Gauge.CSharp.Lib;
using Gauge.CSharp.Lib.Attribute;
using RestSharp;
using Shouldly;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using DotNet.Template.Config;

namespace DotNet.Template.Steps
{
    public class WebApiStepImplementation
    {
        private const string KeyResponse = "api:response";

        [Step("<url> にGETリクエストを送る")]
        public async Task SendGet(string url)
        {
            var config = PlaywrightConfig.Load();
            var baseUrl = config.BaseUrl
                ?? throw new InvalidOperationException("base_url が設定されていません。env/default/web.properties または env/local/web.properties を確認してください。");

            var client = new RestClient(baseUrl);
            var request = new RestRequest(url, Method.Get);
            var response = await client.ExecuteAsync(request);

            ScenarioDataStore.Add(KeyResponse, response);
        }

        [Step("ステータスコードが <code> である")]
        public void StatusCodeIs(string code)
        {
            var response = ScenarioDataStore.Get<RestResponse>(KeyResponse)
                ?? throw new InvalidOperationException("レスポンスが存在しません。先にリクエストを送信してください。");

            ((int)response.StatusCode).ShouldBe(int.Parse(code));
        }

        [Step("レスポンスのJSON配列の長さが <length> である")]
        public void JsonArrayLengthIs(string length)
        {
            var response = ScenarioDataStore.Get<RestResponse>(KeyResponse)
                ?? throw new InvalidOperationException("レスポンスが存在しません。先にリクエストを送信してください。");

            var content = response.Content
                ?? throw new InvalidOperationException("レスポンスボディが空です。");

            var doc = JsonDocument.Parse(content);
            doc.RootElement.GetArrayLength().ShouldBe(int.Parse(length));
        }

        [Step("レスポンスのJSONが <json> と一致している")]
        public void JsonShouldMatch(string json)
        {
            var response = ScenarioDataStore.Get<RestResponse>(KeyResponse)
                ?? throw new InvalidOperationException("レスポンスが存在しません。先にリクエストを送信してください。");

            var content = response.Content
                ?? throw new InvalidOperationException("レスポンスボディが空です。");

            var expectedDoc = JsonDocument.Parse(json);
            var actualDoc = JsonDocument.Parse(content);

            var options = new JsonSerializerOptions { WriteIndented = false };
            var normalizedExpected = JsonSerializer.Serialize(expectedDoc.RootElement, options);
            var normalizedActual = JsonSerializer.Serialize(actualDoc.RootElement, options);

            normalizedActual.ShouldBe(normalizedExpected);
        }
    }
}
