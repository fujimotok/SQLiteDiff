using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
    }
}
