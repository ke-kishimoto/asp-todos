#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace DotNet.Template.Config
{
    /// <summary>
    /// Playwright 設定を env/default/web.properties から読み込みます。
    /// env/local/web.properties が存在する場合はその値で上書きします。
    /// </summary>
    public class PlaywrightConfig
    {
        private readonly Dictionary<string, string> _props;

        /// <summary>使用するブラウザ (chromium / firefox / webkit)</summary>
        public string Browser => Get("browser", "chromium");

        /// <summary>ヘッドレスモードで実行するか</summary>
        public bool Headless => bool.Parse(Get("headless", "false"));

        /// <summary>各操作の遅延時間 (ミリ秒)</summary>
        public float SlowMo => float.Parse(Get("slow_mo", "0"));

        /// <summary>ビューポート幅 (ピクセル)</summary>
        public int ViewportWidth => int.Parse(Get("viewport_width", "1280"));

        /// <summary>ビューポート高さ (ピクセル)</summary>
        public int ViewportHeight => int.Parse(Get("viewport_height", "720"));

        /// <summary>ベース URL (設定されていない場合は null)</summary>
        public string? BaseUrl => GetOrNull("base_url");

        private PlaywrightConfig(Dictionary<string, string> props)
        {
            _props = props;
        }

        /// <summary>
        /// env/default/web.properties を読み込み、env/local/web.properties が存在する場合は上書きします。
        /// </summary>
        public static PlaywrightConfig Load()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var defaultPath = Path.Combine(projectRoot, "env", "default", "web.properties");
            var localPath = Path.Combine(projectRoot, "env", "local", "web.properties");

            var props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(defaultPath))
                LoadFile(defaultPath, props);

            if (File.Exists(localPath))
                LoadFile(localPath, props);

            return new PlaywrightConfig(props);
        }

        private static void LoadFile(string path, Dictionary<string, string> props)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var idx = trimmed.IndexOf('=');
                if (idx < 0)
                    continue;

                var key = trimmed[..idx].Trim();
                var value = trimmed[(idx + 1)..].Trim();
                props[key] = value;
            }
        }

        private string Get(string key, string defaultValue) =>
            _props.TryGetValue(key, out var value) ? value : defaultValue;

        private string? GetOrNull(string key) =>
            _props.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value) ? value : null;
    }
}
