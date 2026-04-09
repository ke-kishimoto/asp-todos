#nullable enable
using System.Collections.Generic;
using System.IO;

namespace DotNet.Template.Config
{
    /// <summary>
    /// DB 接続設定を env/default/db.properties から読み込みます。
    /// env/local/db.properties が存在する場合はその値で上書きします。
    /// </summary>
    public class DbConfig
    {
        private readonly Dictionary<string, string> _props;

        /// <summary>SQL Server 接続文字列</summary>
        public string ConnectionString => Get("db_connection_string",
            "Server=localhost;Database=mydb;Integrated Security=true;TrustServerCertificate=true;");

        /// <summary>コマンドタイムアウト (秒)</summary>
        public int CommandTimeout => int.Parse(Get("db_command_timeout", "30"));

        private DbConfig(Dictionary<string, string> props)
        {
            _props = props;
        }

        /// <summary>
        /// env/default/db.properties を読み込み、env/local/db.properties が存在する場合は上書きします。
        /// </summary>
        public static DbConfig Load()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var defaultPath = Path.Combine(projectRoot, "env", "default", "db.properties");
            var localPath   = Path.Combine(projectRoot, "env", "local",   "db.properties");

            var props = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

            if (File.Exists(defaultPath)) LoadFile(defaultPath, props);
            if (File.Exists(localPath))   LoadFile(localPath,   props);

            return new DbConfig(props);
        }

        private static void LoadFile(string path, Dictionary<string, string> props)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#'))
                    continue;

                var idx = trimmed.IndexOf('=');
                if (idx < 0) continue;

                var key   = trimmed[..idx].Trim();
                var value = trimmed[(idx + 1)..].Trim();
                props[key] = value;
            }
        }

        private string Get(string key, string defaultValue) =>
            _props.TryGetValue(key, out var value) ? value : defaultValue;
    }
}
