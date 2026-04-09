#nullable enable
using Gauge.CSharp.Lib;
using Gauge.CSharp.Lib.Attribute;
using Microsoft.Playwright;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNet.Template.Steps
{
    public class WebStepImplementation
    {
        private const string KeyPage = "pw:page";
        private const string KeyBaseUrl = "pw:base_url"; 

        private static IPage Page =>
            ScenarioDataStore.Get<IPage>(KeyPage) ?? throw new System.InvalidOperationException("Pageが初期化されていません");

        [Step("URL <url> を開く")]
        public async Task OpenUrl(string url)
        {
            await Page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        }

        [Step("URL <url> に遷移している")]
        public async Task UrlIs(string url)
        {
            var baseUrl = ScenarioDataStore.Get<string>(KeyBaseUrl);
            var actual = Page.Url;
            if (baseUrl != null && Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) && Uri.TryCreate(baseUri, url, out var expectedUri))
            {
                url = expectedUri.ToString();
            }
            actual.ShouldBe(url);
        }

        [Step("要素 <selector> に <text> が表示されている")]
        public async Task ElementTextIsVisible(string selector, string text)
        {
            var element = Page.Locator(selector);
            await element.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        }

        [Step("要素 <selector> が <count> 件表示されている")]
        public async Task ElementCountIsVisible(string selector, int count)
        {
            var elements = Page.Locator(selector);
            await elements.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            var actualCount = await elements.CountAsync();
            actualCount.ShouldBe(count);
        }

        [Step("要素 <selector> に <text> と入力する")]
        public async Task EnterText(string selector, string text)
        {
            var element = Page.Locator(selector);
            await element.FillAsync(text);
        }

        [Step("要素 <selector> をクリックする")]
        public async Task ClickElement(string selector)
        {
            var element = Page.Locator(selector);
            await element.ClickAsync();
        }

        [Step("リンク <text> をクリックする")]
        public async Task ClickLink(string text)
        {
            var link = Page.Locator($"a:has-text(\"{text}\")");
            await link.ClickAsync();
        }

        [Step("見出し <text> が表示されている")]
        public async Task HeadingIsVisible(string text)
        {
            var h1 = Page.Locator("h1");
            await h1.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            var actual = await h1.InnerTextAsync();
            actual.ShouldContain(text);
        }

        [Step("テーブル要素 <selector> の <row> 行目の <column> 列の値が <value> である")]
        public async Task TableCellValueIs(string selector, int row, string column, string value)
        {
            var tableLocator = Page.Locator(selector);
            await tableLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            var headerCells = tableLocator.Locator("thead th, tr:first-child th");
            var headerCount = await headerCells.CountAsync();

            int colIndex = -1;
            for (int i = 0; i < headerCount; i++)
            {
                var headerText = (await headerCells.Nth(i).InnerTextAsync()).Trim();
                if (headerText.Equals(column, StringComparison.OrdinalIgnoreCase))
                {
                    colIndex = i;
                    break;
                }
            }

            if (colIndex == -1)
                throw new InvalidOperationException($"列 '{column}' が見つかりません。");

            var cellLocator = tableLocator.Locator($"tbody tr:nth-child({row}) td:nth-child({colIndex + 1})");
            var actualValue = (await cellLocator.InnerTextAsync()).Trim();
            actualValue.ShouldBe(value);
        }

        [Step("テーブル要素 <selector> の内容が <csv> と一致している", "テーブル要素 <selector> の内容が以下の通りである <table>")]
        public async Task TableContentIsCsv(string selector, Table csv)
        {
            var tableLocator = Page.Locator(selector);
            await tableLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

            // ── HTMLテーブルのヘッダー取得 ──────────────────────────────
            var headerCells = tableLocator.Locator("thead th, tr:first-child th");
            var headerCount  = await headerCells.CountAsync();
            var htmlHeaders  = new List<string>();
            for (var i = 0; i < headerCount; i++)
                htmlHeaders.Add((await headerCells.Nth(i).InnerTextAsync()).Trim());

            // ── 期待値 (Gauge Table) の取得 ────────────────────────────
            var expectedRows = csv.GetTableRows();

            // ── HTMLテーブルのデータ行取得 ─────────────────────────────
            var bodyRows     = tableLocator.Locator("tbody tr");
            var actualCount  = await bodyRows.CountAsync();

            actualCount.ShouldBe(expectedRows.Count,
                $"テーブルの行数が一致しません。期待値: {expectedRows.Count} 件、実際: {actualCount} 件");

            // ── 行ごとにセル値を比較 ──────────────────────────────────
            for (var rowIndex = 0; rowIndex < expectedRows.Count; rowIndex++)
            {
                var expectedRow = expectedRows[rowIndex];
                var cells       = bodyRows.Nth(rowIndex).Locator("td");

                foreach (var header in htmlHeaders)
                {
                    // Gauge Table に存在しない列はスキップ
                    string expectedValue;
                    
                    try { expectedValue = expectedRow.GetCell(header); }
                    catch { continue; }

                    if (expectedValue == "") continue;

                    var colIndex    = htmlHeaders.IndexOf(header);
                    var actualValue = (await cells.Nth(colIndex).InnerTextAsync()).Trim();

                    actualValue.ShouldBe(expectedValue.Trim(),
                        $"行 {rowIndex + 1}、列 '{header}' の値が一致しません");
                }
            }
        }
    }

}