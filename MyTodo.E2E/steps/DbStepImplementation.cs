#nullable enable
using Gauge.CSharp.Lib;
using Gauge.CSharp.Lib.Attribute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Data;
using DotNet.Template.Config;
using DotNet.Template.Helpers;

namespace DotNet.Template.Steps
{
    /// <summary>
    /// Gauge のステップ実装クラス。
    /// データベース操作のステップを定義しています。
    /// </summary>
    public class DbStepImplementation
    {
        [Step("テーブル <tableName> のデータを全て削除する")]
        public void DeleteAllDataFromTable(string tableName)
        {
            DatabaseHelper.TruncateTableAsync(tableName).GetAwaiter().GetResult();
        }

        [Step("SQL <sql> を実行する")]
        public void ExecuteSql(string sql)
        {
            DatabaseHelper.ExecuteSqlAsync(sql).GetAwaiter().GetResult();
        }

        [Step("CSVファイル <csvPath> の内容をテーブル <tableName> に投入する")]
        public void InsertDataFromCsvToTable(string csvPath, string tableName)
        {
            DatabaseHelper.InsertFromCsvAsync(tableName: tableName, relativePath: csvPath).GetAwaiter().GetResult();
        }


        [Step("テーブル <tableName> に <csv> の内容を投入する", "テーブル <tableName> に以下の内容を投入する <csv>")]
        public void InsertDataFromCsvToTable(string tableName, Table csv)
        {
            var rows = csv.GetTableRows();
            if (rows.Count == 0) return;

            var columns = csv.GetColumnNames();
            var dataTable = new DataTable();
            foreach (var column in columns)
                dataTable.Columns.Add(column);

            foreach (var row in rows)
            {
                var dataRow = dataTable.NewRow();
                foreach (var column in columns)
                {
                    var value = row.GetCell(column);
                    dataRow[column] = string.IsNullOrEmpty(value) ? DBNull.Value : value;
                }
                dataTable.Rows.Add(dataRow);
            }

            DatabaseHelper.InsertDataTableAsync(tableName, dataTable).GetAwaiter().GetResult();
        }

        [Step("テーブル <tableName> の内容が <csv> と一致している", "テーブル <tableName> の内容が以下の通りである <table>")]
        public void TableContentIsCsv(string tableName, Table csv)
        {
            var expectedRows = csv.GetTableRows();
            if (expectedRows.Count == 0) return;

            // Gauge Table の列名一覧を取得
            // 1列目を ORDER BY キーとして使用し、結果を決定的な順序にする
            var columns    = csv.GetColumnNames();
            var orderByCol = columns.Count > 0 ? columns[0] : null;

            var actualRows = DatabaseHelper
                .QueryTableRowsAsync(tableName, orderByCol)
                .GetAwaiter().GetResult();

            // 行数チェック
            actualRows.Count.ShouldBe(expectedRows.Count,
                $"テーブル '{tableName}' の行数が一致しません。期待値: {expectedRows.Count} 件、実際: {actualRows.Count} 件");

            // 行ごとに、Gauge Table に存在する列だけ比較する
            for (var i = 0; i < expectedRows.Count; i++)
            {
                var expectedRow = expectedRows[i];
                var actualRow   = actualRows[i];

                foreach (var col in columns)
                {
                    var expectedValue = expectedRow.GetCell(col)?.Trim() ?? string.Empty;
                    actualRow.TryGetValue(col, out var rawActual);
                    var actualValue = rawActual?.Trim() ?? string.Empty;

                    actualValue.ShouldBe(expectedValue,
                        $"行 {i + 1}、列 '{col}' の値が一致しません");
                }
            }
        }

        [Step("テーブル <tableName> の条件 <condition> のレコードの内容が以下の通りである <table>")]
        public void TableContentIs(string tableName, string condition, Table expectedTable)
        {
            // 列名は "Column" と "Value" のみ許容
            var columnNames = expectedTable.GetColumnNames();
            if (columnNames.Count != 2
                || !columnNames.Contains("Column")
                || !columnNames.Contains("Value"))
            {
                throw new ArgumentException(
                    $"期待値テーブルの列名は \"Column\" と \"Value\" のみ許容されますが、実際の列名: [{string.Join(", ", columnNames)}] です。");
            }

            // 条件付きでデータ取得
            var rows = DatabaseHelper
                .QueryWithConditionAsync(tableName, condition)
                .GetAwaiter().GetResult();

            // 1 レコードのみであることを検証
            rows.Count.ShouldBe(1,
                $"テーブル '{tableName}' の条件 '{condition}' に該当するレコードが {rows.Count} 件です。、1 件であることが期待されます。");

            var actualRow     = rows[0];
            var expectedRows  = expectedTable.GetTableRows();

            // Gauge Table の各行 (Column / Value) で検証
            foreach (var expectedRow in expectedRows)
            {
                var col           = expectedRow.GetCell("Column").Trim();
                var expectedValue = expectedRow.GetCell("Value").Trim();

                if (!actualRow.TryGetValue(col, out var rawActual))
                    throw new ArgumentException(
                        $"テーブル '{tableName}' に列 '{col}' は存在しません。");

                rawActual.Trim().ShouldBe(expectedValue, $"列 '{col}' の値が一致しません");
            }
        }

        [Step("ストアドプロシージャ <procName> を実行する")]
        public void ExecuteStoredProcedure(string procName)
        {
            DatabaseHelper.ExecuteStoredProcedureAsync(procName).GetAwaiter().GetResult();
        }

        [Step("ストアドプロシージャ <procName> を以下の引数で実行する <paramTable>")]
        public void ExecuteStoredProcedureWithParams(string procName, Table paramTable)
        {
            var parameters = new Dictionary<string, object?>();
            foreach (var row in paramTable.GetTableRows())
            {
                var paramName  = row.GetCell("Parameter").Trim();
                var paramValue = row.GetCell("Value").Trim();
                parameters[paramName] = string.IsNullOrEmpty(paramValue) ? null : paramValue;
            }

            DatabaseHelper.ExecuteStoredProcedureAsync(procName, parameters).GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------
        // DB 種別指定ステップ（SQL Server / PostgreSQL 両対応）
        // 使用例: "SqlServer" のテーブル "todos" のデータを全て削除する
        //         "PostgreSql" でSQL "DELETE FROM todos" を実行する
        // -------------------------------------------------------------------

        [Step("<dbType> のテーブル <tableName> のデータを全て削除する")]
        public void TruncateTableWithDbType(string dbType, string tableName)
        {
            DatabaseHelper.TruncateTableAsync(tableName, ParseDbType(dbType)).GetAwaiter().GetResult();
        }

        [Step("<dbType> のテーブル <tableName> に以下の内容を投入する <csv>")]
        public void InsertDataWithDbType(string dbType, string tableName, Table csv)
        {
            var rows    = csv.GetTableRows();
            if (rows.Count == 0) return;

            var columns   = csv.GetColumnNames();
            var dataTable = new DataTable();
            foreach (var column in columns)
                dataTable.Columns.Add(column);

            foreach (var row in rows)
            {
                var dataRow = dataTable.NewRow();
                foreach (var column in columns)
                {
                    var value = row.GetCell(column);
                    dataRow[column] = string.IsNullOrEmpty(value) ? DBNull.Value : value;
                }
                dataTable.Rows.Add(dataRow);
            }

            DatabaseHelper.InsertDataTableAsync(tableName, dataTable, ParseDbType(dbType)).GetAwaiter().GetResult();
        }

        [Step("<dbType> のテーブル <tableName> の内容が以下の通りである <table>")]
        public void TableContentWithDbType(string dbType, string tableName, Table csv)
        {
            var expectedRows = csv.GetTableRows();
            if (expectedRows.Count == 0) return;

            var columns    = csv.GetColumnNames();
            var orderByCol = columns.Count > 0 ? columns[0] : null;

            var actualRows = DatabaseHelper
                .QueryTableRowsAsync(tableName, orderByCol, ParseDbType(dbType))
                .GetAwaiter().GetResult();

            actualRows.Count.ShouldBe(expectedRows.Count,
                $"[{dbType}] テーブル '{tableName}' の行数が一致しません。期待値: {expectedRows.Count} 件、実際: {actualRows.Count} 件");

            for (var i = 0; i < expectedRows.Count; i++)
            {
                var expectedRow = expectedRows[i];
                var actualRow   = actualRows[i];

                foreach (var col in columns)
                {
                    var expectedValue = expectedRow.GetCell(col)?.Trim() ?? string.Empty;
                    actualRow.TryGetValue(col, out var rawActual);
                    var actualValue = rawActual?.Trim() ?? string.Empty;

                    actualValue.ShouldBe(expectedValue,
                        $"[{dbType}] 行 {i + 1}、列 '{col}' の値が一致しません");
                }
            }
        }

        [Step("<dbType> のテーブル <tableName> の条件 <condition> のレコードの内容が以下の通りである <table>")]
        public void TableConditionWithDbType(string dbType, string tableName, string condition, Table expectedTable)
        {
            var columnNames = expectedTable.GetColumnNames();
            if (columnNames.Count != 2
                || !columnNames.Contains("Column")
                || !columnNames.Contains("Value"))
            {
                throw new ArgumentException(
                    $"期待値テーブルの列名は \"Column\" と \"Value\" のみ許容されますが、実際の列名: [{string.Join(", ", columnNames)}] です。");
            }

            var rows = DatabaseHelper
                .QueryWithConditionAsync(tableName, condition, ParseDbType(dbType))
                .GetAwaiter().GetResult();

            rows.Count.ShouldBe(1,
                $"[{dbType}] テーブル '{tableName}' の条件 '{condition}' に該当するレコードが {rows.Count} 件です。1 件であることが期待されます。");

            var actualRow    = rows[0];
            var expectedRows = expectedTable.GetTableRows();

            foreach (var expectedRow in expectedRows)
            {
                var col           = expectedRow.GetCell("Column").Trim();
                var expectedValue = expectedRow.GetCell("Value").Trim();

                if (!actualRow.TryGetValue(col, out var rawActual))
                    throw new ArgumentException(
                        $"[{dbType}] テーブル '{tableName}' に列 '{col}' は存在しません。");

                rawActual.Trim().ShouldBe(expectedValue, $"[{dbType}] 列 '{col}' の値が一致しません");
            }
        }

        [Step("<dbType> でSQL <sql> を実行する")]
        public void ExecuteSqlWithDbType(string dbType, string sql)
        {
            DatabaseHelper.ExecuteSqlAsync(sql, ParseDbType(dbType)).GetAwaiter().GetResult();
        }

        [Step("<dbType> でストアドプロシージャ <procName> を実行する")]
        public void ExecuteStoredProcedureWithDbType(string dbType, string procName)
        {
            DatabaseHelper.ExecuteStoredProcedureAsync(procName, ParseDbType(dbType)).GetAwaiter().GetResult();
        }

        [Step("<dbType> でストアドプロシージャ <procName> を以下の引数で実行する <paramTable>")]
        public void ExecuteStoredProcedureWithParamsAndDbType(string dbType, string procName, Table paramTable)
        {
            var parameters = new Dictionary<string, object?>();
            foreach (var row in paramTable.GetTableRows())
            {
                var paramName  = row.GetCell("Parameter").Trim();
                var paramValue = row.GetCell("Value").Trim();
                parameters[paramName] = string.IsNullOrEmpty(paramValue) ? null : paramValue;
            }

            DatabaseHelper.ExecuteStoredProcedureAsync(procName, parameters, ParseDbType(dbType)).GetAwaiter().GetResult();
        }

        // -------------------------------------------------------------------

        private static TestDbType ParseDbType(string value) => value.Trim().ToLowerInvariant() switch
        {
            "sqlserver" => TestDbType.SqlServer,
            "postgresql" or "postgres" => TestDbType.PostgreSql,
            _ => throw new ArgumentException(
                     $"不明な DB 種別: \"{value}\"。\"SqlServer\" または \"PostgreSql\" を指定してください。")
        };
    }
}
