#nullable enable
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNet.Template.Config;

namespace DotNet.Template.Helpers
{
    /// <summary>
    /// DB テストデータ管理ユーティリティ。
    /// SQL ファイルの実行と CSV ファイルからのデータ投入をサポートします。
    /// ファイルパスはプロジェクトルートからの相対パス、または絶対パスで指定できます。
    /// </summary>
    public static class DatabaseHelper
    {
        // GO はバッチ区切り (大文字小文字・前後空白を無視)
        private static readonly Regex GoBatchSeparator =
            new(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        /// <summary>
        /// SQL ファイルを実行します。
        /// T-SQL の GO バッチ区切りに対応しています。
        /// </summary>
        /// <param name="relativePath">プロジェクトルートからの相対パス (例: testdata/sql/seed.sql)</param>
        public static async Task ExecuteSqlFileAsync(string relativePath)
        {
            var fullPath = ResolvePath(relativePath);
            var sql = await File.ReadAllTextAsync(fullPath);
            await ExecuteSqlAsync(sql);
        }

        /// <summary>
        /// SQL 文字列を直接実行します。
        /// T-SQL の GO バッチ区切りに対応しています。
        /// </summary>
        /// <param name="sql">実行する SQL 文字列</param>
        public static async Task ExecuteSqlAsync(string sql)
        {
            var config = DbConfig.Load();

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            // GO で分割して各バッチを順番に実行する
            var batches = GoBatchSeparator.Split(sql);
            foreach (var batch in batches)
            {
                var trimmed = batch.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                await using var cmd = new SqlCommand(trimmed, conn)
                {
                    CommandTimeout = config.CommandTimeout
                };
                await cmd.ExecuteNonQueryAsync();
            }
        }

        /// <summary>
        /// CSV ファイルからデータを読み込み、指定のテーブルに一括挿入します。
        /// CSV の1行目をヘッダー (列名) として扱います。
        /// </summary>
        /// <param name="tableName">挿入先のテーブル名</param>
        /// <param name="relativePath">プロジェクトルートからの相対パス (例: testdata/csv/todos.csv)</param>
        public static async Task InsertFromCsvAsync(string tableName, string relativePath)
        {
            var fullPath = ResolvePath(relativePath);
            var dataTable = ReadCsvToDataTable(fullPath);
            await BulkInsertAsync(tableName, dataTable);
        }

        /// <summary>
        /// 指定の DataTable を対象テーブルに一括挿入します。
        /// </summary>
        /// <param name="tableName">挿入先テーブル名</param>
        /// <param name="dataTable">挿入するデータ</param>
        public static Task InsertDataTableAsync(string tableName, DataTable dataTable) =>
            BulkInsertAsync(tableName, dataTable);

        /// <summary>
        /// 指定テーブルの全データを削除します。
        /// テストデータの初期化に使用します。
        /// </summary>
        /// <param name="tableName">削除対象のテーブル名</param>
        public static async Task TruncateTableAsync(string tableName)
        {
            // テーブル名を角括弧で囲み SQL インジェクションを防ぐ
            var safeName = EscapeTableName(tableName);
            await ExecuteSqlAsync($"TRUNCATE TABLE {safeName};");
        }

        /// <summary>
        /// 指定テーブルの全データを取得します。
        /// 主キー列を1列目と仮定し、Dictionary&lt;string, Dictionary&lt;string, string&gt;&gt; の形式で返します。
        /// 例: { "1" => { "Title" => "Sample Todo", "Done" => "false" }, ... }
        /// </summary>
        /// <param name="tableName">クエリ対象のテーブル名</param>
        /// <returns>テーブルデータの辞書</returns>
        /// 主キー列が複数ある場合や、1列目が主キーでない場合は適宜修正してください。
        public static async Task<Dictionary<string, Dictionary<string, string>>> QueryTableAsync(string tableName)
        {
            var config = DbConfig.Load();
            var safeName = EscapeTableName(tableName);
            var result = new Dictionary<string, Dictionary<string, string>>();

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            var query = $"SELECT * FROM {safeName};";
            await using var cmd = new SqlCommand(query, conn)
            {
                CommandTimeout = config.CommandTimeout
            };
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var key = reader[0].ToString() ?? throw new Exception("主キー列の値が null です");
                var rowDict = new Dictionary<string, string>();
                for (var i = 1; i < reader.FieldCount; i++)
                {
                    var columnName = reader.GetName(i);
                    var value = reader[i]?.ToString() ?? "";
                    rowDict[columnName] = value;
                }
                result[key] = rowDict;
            }

            return result;
        }

        /// <summary>
        /// 指定テーブルの全データを取得し、行のリストとして返します。
        /// 各行は「列名 → 値(文字列)」の辞書です。
        /// </summary>
        /// <param name="tableName">クエリ対象のテーブル名</param>
        /// <param name="orderByColumn">
        /// ORDER BY に使う列名。null の場合は ORDER BY なし。
        /// Gauge Table の最初の列名を渡すと決定的な順序になります。
        /// </param>
        public static async Task<List<Dictionary<string, string>>> QueryTableRowsAsync(
            string tableName, string? orderByColumn = null)
        {
            var config   = DbConfig.Load();
            var safeName = EscapeTableName(tableName);
            var result   = new List<Dictionary<string, string>>();

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            var orderBy = orderByColumn != null
                ? $" ORDER BY [{orderByColumn.Trim('[', ']')}]"
                : string.Empty;

            await using var cmd = new SqlCommand($"SELECT * FROM {safeName}{orderBy};", conn)
            {
                CommandTimeout = config.CommandTimeout
            };
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader[i]?.ToString() ?? string.Empty;
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// WHERE 句を指定してテーブルからデータを取得します。
        /// </summary>
        /// <param name="tableName">クエリ対象のテーブル名</param>
        /// <param name="condition">WHERE 句の条件式 (例: "Id = 1" または "Status = 'Active'")</param>
        /// <returns>取得した行のリスト。各行は列名→値の辞書</returns>
        public static async Task<List<Dictionary<string, string>>> QueryWithConditionAsync(
            string tableName, string condition)
        {
            var config   = DbConfig.Load();
            var safeName = EscapeTableName(tableName);
            var result   = new List<Dictionary<string, string>>();

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            // condition はステップ定義者が記述する文字列のため、そのまま埋め込む
            // （テストコードでのみ使用し、外部入力は渡さないことを前提とする）
            var query = $"SELECT * FROM {safeName} WHERE {condition};";
            await using var cmd = new SqlCommand(query, conn)
            {
                CommandTimeout = config.CommandTimeout
            };
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader[i]?.ToString() ?? string.Empty;
                result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// 引数なしのストアドプロシージャを実行します。
        /// </summary>
        /// <param name="procName">実行するストアドプロシージャの名前</param>
        public static async Task ExecuteStoredProcedureAsync(string procName)
        {
            var config = DbConfig.Load();
            var safeName = EscapeTableName(procName);

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(safeName, conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = config.CommandTimeout
            };
            await cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// パラメータ付きのストアドプロシージャを実行します。
        /// </summary>
        /// <param name="procName">実行するストアドプロシージャの名前</param>
        /// <param name="parameters">ストアドプロシージャに渡すパラメータの辞書 (例: { "UserId" => 123, "IsActive" => true })</param>
        public static async Task ExecuteStoredProcedureAsync(string procName, Dictionary<string, object?> parameters)
        {
            var config = DbConfig.Load();
            var safeName = EscapeTableName(procName);

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(safeName, conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = config.CommandTimeout
            };

            foreach (var param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync();
        }

        // -------------------------------------------------------------------

        private static DataTable ReadCsvToDataTable(string fullPath)
        {
            var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,   // 列数が不足している行はスキップ
            };

            using var reader = new StreamReader(fullPath);
            using var csv = new CsvReader(reader, csvConfig);

            var dataTable = new DataTable();
            bool headersAdded = false;

            while (csv.Read())
            {
                if (!headersAdded)
                {
                    csv.ReadHeader();
                    foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
                        dataTable.Columns.Add(header);
                    headersAdded = true;
                    continue;
                }

                var row = dataTable.NewRow();
                foreach (DataColumn col in dataTable.Columns)
                {
                    var raw = csv.GetField(col.ColumnName);
                    row[col.ColumnName] = string.IsNullOrEmpty(raw) ? DBNull.Value : (object)raw;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        private static async Task BulkInsertAsync(string tableName, DataTable dataTable)
        {
            var config = DbConfig.Load();
            var safeName = EscapeTableName(tableName);

            await using var conn = new SqlConnection(config.ConnectionString);
            await conn.OpenAsync();

            using var bulk = new SqlBulkCopy(conn)
            {
                DestinationTableName = safeName,
                BulkCopyTimeout      = config.CommandTimeout,
            };

            // CSV のヘッダー名とテーブルの列名をマッピング
            foreach (DataColumn col in dataTable.Columns)
                bulk.ColumnMappings.Add(col.ColumnName, col.ColumnName);

            await bulk.WriteToServerAsync(dataTable);
        }

        /// <summary>テーブル名をスキーマ付きで角括弧エスケープする</summary>
        private static string EscapeTableName(string tableName)
        {
            // "dbo.TableName" や "TableName" の形式に対応
            var parts = tableName.Split('.');
            var escaped = new List<string>();
            foreach (var part in parts)
                escaped.Add($"[{part.Trim('[', ']')}]");
            return string.Join(".", escaped);
        }

        /// <summary>相対パスをプロジェクトルート基準の絶対パスに解決する</summary>
        private static string ResolvePath(string path) =>
            Path.IsPathRooted(path)
                ? path
                : Path.Combine(Directory.GetCurrentDirectory(), path);
    }
}
