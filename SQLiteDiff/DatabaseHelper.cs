using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Windows;

namespace SQLiteDiff
{
    public class DatabaseHelper
    {
        public List<(string tableName, string primaryKeyName)> GetTableList(string dbPath)
        {
            List<(string tableName, string primaryKeyName)> tableList = new List<(string tableName, string primaryKeyName)>();

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    // テーブル名を取得
                    DataTable schema = connection.GetSchema("Tables");
                    foreach (DataRow row in schema.Rows)
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                        string primaryKeyName = GetPrimaryKeyColumnName(connection, tableName);
                        tableList.Add((tableName, primaryKeyName));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening database: {ex.Message}");
            }

            return tableList;
        }

        private string GetPrimaryKeyColumnName(SQLiteConnection connection, string tableName)
        {
            string primaryKeyColumnName = null;

            try
            {
                using (SQLiteCommand command = new SQLiteCommand($"PRAGMA table_info('{tableName}')", connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int pkValue = Convert.ToInt32(reader["pk"]);
                            if (pkValue == 1)
                            {
                                primaryKeyColumnName = reader["name"].ToString();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting primary key: {ex.Message}");
            }

            return primaryKeyColumnName;
        }

        public DataTable LoadTableData(string dbPath, string tableName)
        {
            DataTable dataTable = new DataTable();

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    connection.Open();

                    string query = $"SELECT * FROM `{tableName}`";
                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table data: {ex.Message}");
            }

            return dataTable;
        }
        
        public bool HasDifferences(string db1Path, string db2Path, string tableName)
        {
            var columns = GetColumnNames(db1Path, tableName);

            using (var connA = new SQLiteConnection($"Data Source={db1Path};Version=3;"))
            using (var connB = new SQLiteConnection($"Data Source={db2Path};Version=3;"))
            {
                connA.Open();
                connB.Open();

                string sql = $"SELECT {string.Join(",", columns)} FROM {tableName} ORDER BY {string.Join(",", columns)}";

                try
                {
                    using (var cmdA = new SQLiteCommand(sql, connA))
                    using (var cmdB = new SQLiteCommand(sql, connB))
                    using (var readerA = cmdA.ExecuteReader())
                    using (var readerB = cmdB.ExecuteReader())
                    {
                        while (true)
                        {
                            bool hasNextA = readerA.Read();
                            bool hasNextB = readerB.Read();

                            if (hasNextA == false && hasNextB == false)
                            {
                                return false; // 両方終わり → 完全一致
                            }
                            else if (hasNextA == false || hasNextB == false)
                            {
                                return true; // 片方だけ終わり → 差分検出
                            }

                            string rowA = string.Join("|", Enumerable.Range(0, readerA.FieldCount)
                                                                     .Select(i => readerA.GetValue(i)?.ToString() ?? "NULL"));
                            string rowB = string.Join("|", Enumerable.Range(0, readerB.FieldCount)
                                                                     .Select(i => readerB.GetValue(i)?.ToString() ?? "NULL"));

                            if (rowA != rowB)
                            {
                                return true; // 差分検出
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error comparing tables: {ex.Message}");
                    return false; // エラー時は差分なしとみなす
                }
            }
        }

        private List<string> GetColumnNames(string dbPath, string tableName)
        {
            var columns = new List<string>();

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand($"PRAGMA table_info({tableName})", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(reader["name"].ToString());
                    }
                }
            }

            return columns;
        }
    }
}
