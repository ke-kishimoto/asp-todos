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

        /// <summary>SQL Server 接続文字列（後方互換用。db_connection_string キーを読み込みます）</summary>
        public string ConnectionString => Get("db_connection_string",
            "Server=localhost;Database=mydb;Integrated Security=true;TrustServerCertificate=true;");

        /// <summary>SQL Server 専用の接続文字列。sqlserver_connection_string キーを優先し、未設定なら ConnectionString にフォールバックします</summary>
        public string SqlServerConnectionString =>
            _props.TryGetValue("sqlserver_connection_string", out var v) && !string.IsNullOrWhiteSpace(v)
                ? v
                : ConnectionString;

        /// <summary>PostgreSQL 接続文字列</summary>
        public string PostgreSqlConnectionString => Get("postgresql_connection_string",
            "Host=localhost;Port=5432;Database=mydb;Username=postgres;Password=postgres;");

        /// <summary>コマンドタイムアウト (秒)</summary>
        public int CommandTimeout => int.Parse(Get("db_command_timeout", "30"));

        /// <summary>TestDbType に応じた接続文字列を返します</summary>
        public string GetConnectionString(TestDbType dbType) => dbType switch
        {
            TestDbType.SqlServer  => SqlServerConnectionString,
            TestDbType.PostgreSql => PostgreSqlConnectionString,
            _                     => SqlServerConnectionString,
        };

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
