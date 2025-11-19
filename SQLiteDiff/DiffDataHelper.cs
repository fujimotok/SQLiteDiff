using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Controls;

namespace SQLiteDiff
{
    using RowData = Dictionary<string, CellData>;

    internal class DiffDataHelper
    {
        private const string STATUS = "_status_";

        private enum RowStatus
        {
            None,
            Added,
            Deleted,
            Modified,
            Blank,
        }

        private enum CellStatus
        {
            None,
            Modified
        }

        /// <summary>
        /// 改行コードの可視化
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string VisualizeNewLine(string text)
        {
            const string RETURN_SYMBOL = "↵";
            const string CARRIAGE_SYMBOL = "⇠";
            const string LINEFEED_SYMBOL = "⇣";

            return text.Replace("\r\n", $"{RETURN_SYMBOL}")
                       .Replace("\r", $"{CARRIAGE_SYMBOL}")
                       .Replace("\n", $"{LINEFEED_SYMBOL}")
                       .Replace($"{RETURN_SYMBOL}", $"{RETURN_SYMBOL}\n")
                       .Replace($"{CARRIAGE_SYMBOL}", $"{CARRIAGE_SYMBOL}\n")
                       .Replace($"{LINEFEED_SYMBOL}", $"{LINEFEED_SYMBOL}\n");
        }

        /// <summary>
        /// データベースの差分をとり、DataGrid1,2に反映する
        /// </summary>
        /// <param name="dataTable1"></param>
        /// <param name="dataTable2"></param>
        /// <param name="primaryKeyColumnName"></param>
        /// <param name="dataGrid1"></param>
        /// <param name="dataGrid2"></param>
        public static void DiffProcess(
            DataTable dataTable1,
            DataTable dataTable2,
            string primaryKeyColumnName,
            System.Windows.Controls.DataGrid dataGrid1,
            System.Windows.Controls.DataGrid dataGrid2,
            bool isDiffContext1Line)
        {
            var dataList1 = ConvertDataTableToList(dataTable1);
            var dataList2 = ConvertDataTableToList(dataTable2);

            // 主キーの結合したデータリストを取得
            HashSet<object> combinedKeys = GetCombinedKeys(dataList1, dataList2, primaryKeyColumnName);

            // データリストに存在しない主キーがある場合、nullで埋める
            FillMissingKeys(dataList1, combinedKeys, primaryKeyColumnName);
            FillMissingKeys(dataList2, combinedKeys, primaryKeyColumnName);

            // 差分を集計して背景色を設定
            AnnotateDataList(dataList1, dataList2, primaryKeyColumnName);

            if (isDiffContext1Line)
            {
                dataList1 = dataList1.Where(row => (string)(row[STATUS].Value) != "").ToList();
                dataList2 = dataList2.Where(row => (string)(row[STATUS].Value) != "").ToList();
            }

            // 画面に反映
            dataGrid1.ItemsSource = dataList1;
            BindDataToDataGrid(dataList1, dataGrid1);
            dataGrid2.ItemsSource = dataList2;
            BindDataToDataGrid(dataList2, dataGrid2);
        }

        /// <summary>
        /// Primary Keyの和集合を取得する
        /// 比較前後のデータ数を揃えるために使用する
        /// </summary>
        /// <param name="dataList1"></param>
        /// <param name="dataList2"></param>
        /// <param name="primaryKeyColumnName"></param>
        /// <returns></returns>
        private static HashSet<object> GetCombinedKeys(
            List<RowData> dataList1,
            List<RowData> dataList2,
            string primaryKeyColumnName)
        {
            // 各テーブルの主キーをHashSetに格納
            HashSet<object> table1PrimaryKeys = new HashSet<object>(dataList1.Select(row => row[primaryKeyColumnName].Value));

            HashSet<object> table2PrimaryKeys = new HashSet<object>(dataList2.Select(row => row[primaryKeyColumnName].Value));

            // 和集合を作成
            HashSet<object> combinedKeys = new HashSet<object>(table1PrimaryKeys.Union(table2PrimaryKeys));
            return combinedKeys;
        }

        /// <summary>
        /// dataTableをList<RowData>に変換する
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        private static List<RowData> ConvertDataTableToList(DataTable dataTable)
        {
            List<RowData> list = new List<RowData>();

            foreach (DataRow row in dataTable.Rows)
            {
                RowData dictionary = new RowData();

                // Status 列を最初に追加
                dictionary[STATUS] = new CellData
                {
                    Value = GetStatusText(RowStatus.None),
                    BackgroundColor = GetBackgroundColor(RowStatus.None)
                };

                foreach (DataColumn column in dataTable.Columns)
                {
                    object value = row[column] != DBNull.Value ? row[column] : null;

                    dictionary[column.ColumnName] = new CellData
                    {
                        Value = value,
                        BackgroundColor = GetBackgroundColor(RowStatus.None),
                    };
                }

                list.Add(dictionary);
            }

            return list;
        }

        /// <summary>
        /// データリストに存在しない主キーがある場合、nullで埋める
        /// </summary>
        /// <param name="dataList"></param>
        /// <param name="combinedKeys"></param>
        /// <param name="primaryKeyColumnName"></param>
        private static void FillMissingKeys(
            List<RowData> dataList,
            HashSet<object> combinedKeys,
            string primaryKeyColumnName)
        {
            // dataListに存在しない主キーを絞り込む
            var dataListKeys = dataList.Select(row => row[primaryKeyColumnName].Value);
            var missingKeys = combinedKeys.Where(key => dataListKeys.Contains(key) == false);

            // 存在しない場合、新しい行を作成して追加
            foreach (object key in missingKeys)
            {
                RowData newRow = new RowData();

                // sampleRowから列名を取得して新しい行を作成
                var sampleRow = dataList.FirstOrDefault() ?? new RowData();
                foreach (var column in sampleRow)
                {
                    // key列には主キーの値を設定し、他の列はnullで埋める
                    var val = (column.Key == primaryKeyColumnName) ? key : null;

                    newRow[column.Key] = new CellData
                    {
                        Value = val,
                        BackgroundColor = GetBackgroundColor(RowStatus.None)
                    };
                }

                dataList.Add(newRow);
            }

            // 比較前後のデータをそろえるために、主キーでソート
            dataList.Sort((row1, row2) =>
            {
                object key1 = row1[primaryKeyColumnName].Value;
                object key2 = row2[primaryKeyColumnName].Value;
                return Comparer<object>.Default.Compare(key1, key2);
            });
        }

        /// <summary>
        /// 2つのデータリストを比較し、差分に応じて背景色を設定する
        /// </summary>
        /// <param name="dataList1"></param>
        /// <param name="dataList2"></param>
        /// <param name="primaryKeyColumnName"></param>
        private static void AnnotateDataList(
            List<RowData> dataList1,
            List<RowData> dataList2,
            string primaryKeyColumnName)
        {
            // 前提としてデータの数がそろっていて、主キーでソートされていること
            var dataSet = dataList1.Zip(dataList2, (a, b) => (a, b));
            foreach (var row in dataSet)
            {
                var diffStatus = AreRowsEqual(row.a, row.b, primaryKeyColumnName);
                if (diffStatus == RowStatus.Modified)
                {
                    // 背景色を設定
                    foreach (string column in row.a.Keys)
                    {
                        if (!row.a[column].Value.Equals(row.b[column].Value))
                        {
                            row.a[column].BackgroundColor = GetBackgroundColor(CellStatus.Modified);
                            row.b[column].BackgroundColor = GetBackgroundColor(CellStatus.Modified);
                        }
                        else
                        {
                            row.a[column].BackgroundColor = GetBackgroundColor(RowStatus.Modified);
                            row.b[column].BackgroundColor = GetBackgroundColor(RowStatus.Modified);
                        }
                    }
                    row.a[STATUS].Value = GetStatusText(RowStatus.Modified);
                    row.b[STATUS].Value = GetStatusText(RowStatus.Modified);
                }

                if (diffStatus == RowStatus.Added)
                {
                    // dataList2の行が追加された場合
                    foreach (string column in row.b.Keys)
                    {
                        row.a[column].BackgroundColor = GetBackgroundColor(RowStatus.Added);
                        row.b[column].BackgroundColor = GetBackgroundColor(RowStatus.Added);
                    }
                    row.a[STATUS].Value = GetStatusText(RowStatus.Added);
                    row.b[STATUS].Value = GetStatusText(RowStatus.Added);
                }

                if (diffStatus == RowStatus.Deleted)
                {
                    // dataList1の行が削除された場合
                    foreach (string column in row.a.Keys)
                    {
                        row.a[column].BackgroundColor = GetBackgroundColor(RowStatus.Deleted);
                        row.b[column].BackgroundColor = GetBackgroundColor(RowStatus.Deleted);
                    }
                    row.a[STATUS].Value = GetStatusText(RowStatus.Deleted);
                    row.b[STATUS].Value = GetStatusText(RowStatus.Deleted);

                }
            }
        }

        /// <summary>
        /// List<RowData> を DataGrid にバインドするメソッド
        /// </summary>
        /// <param name="data"></param>
        /// <param name="dataGrid"></param>
        private static void BindDataToDataGrid(
            List<RowData> data,
            System.Windows.Controls.DataGrid dataGrid)
        {
            // 列をクリア
            dataGrid.Columns.Clear();

            // ステータス列を先頭に追加
            var statusColumn = DataGridHelper.CreateDataGridTextColumn("!", STATUS, 50);
            dataGrid.Columns.Add(statusColumn);

            // 列名を抽出
            HashSet<string> columnNames = new HashSet<string>();
            foreach (var row in data)
            {
                foreach (var columnName in row.Keys)
                {
                    columnNames.Add(columnName);
                }
            }

            // 列を動的に生成
            foreach (string columnName in columnNames)
            {
                if (columnName == STATUS) continue; // Status列はすでに作成済みなのでスキップ

                var dataGridLengthStar = new DataGridLength(1, DataGridLengthUnitType.Star);
                var dataGridColumn = DataGridHelper.CreateDataGridTextColumn(columnName, columnName, dataGridLengthStar);
                dataGrid.Columns.Add(dataGridColumn);
            }

            // DataGrid にデータソースを設定
            dataGrid.ItemsSource = data;
        }

        /// <summary>
        /// 2つのRowDataが等しいかどうかを判定するメソッド
        /// </summary>
        /// <param name="row1"></param>
        /// <param name="row2"></param>
        /// <param name="primaryKeyColumnName"></param>
        /// <returns></returns>
        private static RowStatus AreRowsEqual(
            RowData row1,
            RowData row2,
            string primaryKeyColumnName)
        {
            // row1がキーのみのRowの場合、row2に存在しないのでAdded
            if (row1.Where(kvp => kvp.Key != primaryKeyColumnName).Select(kvp => kvp.Value).All(cell => cell.Value == null))
            {
                foreach (string column in row2.Keys)
                {
                    row2[column].BackgroundColor = GetBackgroundColor(RowStatus.Added);
                }
                row2[STATUS].Value = GetStatusText(RowStatus.Added);
                return RowStatus.Added;
            }

            // row2がキーのみのRowの場合、row1に存在しないのでDeleted
            if (row2.Where(kvp => kvp.Key != primaryKeyColumnName).Select(kvp => kvp.Value).All(cell => cell.Value == null))
            {
                foreach (string column in row1.Keys)
                {
                    row1[column].BackgroundColor = GetBackgroundColor(RowStatus.Deleted);
                }
                row1[STATUS].Value = GetStatusText(RowStatus.Deleted);
                return RowStatus.Deleted;
            }

            // 両方に存在する場合、値を比較
            foreach (string column in row1.Keys)
            {
                var cell1 = row1[column].Value?.ToString() ?? string.Empty;
                var cell2 = row2[column].Value?.ToString() ?? string.Empty;
                if (cell1 != cell2)
                {
                    return RowStatus.Modified;
                }
            }
            return RowStatus.None;
        }

        /// <summary>
        /// ステータスに基づいて背景色を決定するメソッド
        /// </summary>
        /// <param name="rowStatus"></param>
        /// <returns></returns>
        private static string GetBackgroundColor(RowStatus rowStatus)
        {
            // TODO: コンフィグ可能にする
            switch (rowStatus)
            {
                case RowStatus.Added: return "LightGreen";
                case RowStatus.Deleted: return "LightCoral";
                case RowStatus.Modified: return "LightYellow";
                case RowStatus.Blank: return "LightGray";
                default: return "White";
            }
        }

        /// <summary>
        /// ステータスに基づいて背景色を決定するメソッド
        /// </summary>
        /// <param name="cellStatus"></param>
        /// <returns></returns>
        private static string GetBackgroundColor(CellStatus cellStatus)
        {
            // TODO: コンフィグ可能にする
            switch (cellStatus)
            {
                case CellStatus.Modified: return "Orange";
                default: return "White";
            }
        }

        /// <summary>
        /// ステータスに基づいて文字を決定するメソッド
        /// </summary>
        /// <param name="rowStatus"></param>
        /// <returns></returns>
        private static string GetStatusText(RowStatus rowStatus)
        {
            switch (rowStatus)
            {
                case RowStatus.Added: return "A";
                case RowStatus.Deleted: return "D";
                case RowStatus.Modified: return "M";
                default: return "";
            }
        }
    }
}
