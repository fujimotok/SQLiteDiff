using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SQLiteDiff
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string NOSELECT = "<No Select>";

        private DatabaseHelper _dbHelper = new DatabaseHelper();
        private List<(string tableName, string primaryKeyName)> _tableAndPKList;
        private ScrollViewer _dataGridScrollViewer1;
        private ScrollViewer _dataGridScrollViewer2;
        private bool _isSyncingScroll;
        private bool _isSyncingSelect;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            // 初期化
            InitializeComponent();
            TableComboBox.ItemsSource = new List<string>() { NOSELECT };
            TableComboBox.SelectedIndex = 0;

            // 起動引数を取得し、データベースパスをセット
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 3)
            {
                Database1PathTextBox.Text = args[1];
                Database2PathTextBox.Text = args[2];
            }
        }

        /// <summary>
        /// DatabasePathTextBoxのPreviewMouseDownイベントハンドラ
        /// ファイルパスの入力をファイルダイアログで行う
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DatabasePathTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "SQLite Database Files (*.db;*.sqlite)|*.db;*.sqlite|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                textBox.Text = openFileDialog.FileName;
            }
        }

        /// <summary>
        /// Compareボタンクリックイベントハンドラ
        /// テーブル一覧を作成し、ComboBoxに表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            // データベース1のパスを取得
            string db1Path = Database1PathTextBox.Text;
            // データベース2のパスを取得
            string db2Path = Database2PathTextBox.Text;

            // テーブルリストを格納するリスト
            List<(string tableName, string primaryKeyName)> tableList1 = _dbHelper.GetTableList(db1Path);
            List<(string tableName, string primaryKeyName)> tableList2 = _dbHelper.GetTableList(db2Path);

            // 未選択にさせるための項目を先頭に追加
            _tableAndPKList = new List<(string tableName, string primaryKeyName)>() { (NOSELECT, "") };
            foreach (var tableInfo in tableList1)
            {
                _tableAndPKList.Add(tableInfo); // テーブル名のみを追加
            }

            var tableNames = new HashSet<string>(_tableAndPKList.ConvertAll(t => t.tableName));

            // テーブルリストをComboBoxに表示する
            TableComboBox.ItemsSource = tableNames;
            TableComboBox.SelectedIndex = 0;

            MessageBox.Show($"Opening DBs success !");
        }

        /// <summary>
        /// TebleComboBoxの選択変更イベントハンドラ
        /// 差分を集計してDataGridにデータを表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 選択されたテーブル名を取得。Nullまたは未選択の場合は処理を終了
            string selectedTable = TableComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedTable) || selectedTable == NOSELECT)
            {
                DataGrid1.ItemsSource = null;
                DataGrid2.ItemsSource = null;
                return;
            }

            // 各データベースからテーブルデータをDataTableで取得、リストに変換
            string db1Path = Database1PathTextBox.Text;
            string db2Path = Database2PathTextBox.Text;
            DataTable dataTable1 = _dbHelper.LoadTableData(db1Path, selectedTable);
            DataTable dataTable2 = _dbHelper.LoadTableData(db2Path, selectedTable);

            // 主キー名を取得し、結合した主キーのリストを取得
            string primaryKeyColumnName = _tableAndPKList.Find(x => x.tableName == selectedTable).primaryKeyName;
            
            // データベースの差分をとり、DataGrid1,2に反映する
            DiffDataHelper.DiffProcess(dataTable1, dataTable2, primaryKeyColumnName, DataGrid1, DataGrid2);
        }

        /// <summary>
        /// DataGridのレイアウト更新イベントハンドラ
        /// 列幅を同期する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_LayoutUpdated(object sender, EventArgs e)
        {
            // DataGrid1 の列幅を DataGrid2 に適用
            for (int i = 0; i < DataGrid1.Columns.Count && i < DataGrid2.Columns.Count; i++)
            {
                DataGrid2.Columns[i].Width = DataGrid1.Columns[i].Width;
            }
        }

        /// <summary>
        /// DataGridの列表示順序変更イベントハンドラ
        /// 表示順序を同期する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_ColumnDisplayIndexChanged(object sender, DataGridColumnEventArgs e)
        {
            var sourceDataGrid = sender as System.Windows.Controls.DataGrid;
            var targetDataGrid = (sourceDataGrid == DataGrid1) ? DataGrid2 : DataGrid1;

            // 列の表示順序を同期
            if (e.Column != null)
            {
                targetDataGrid.Columns[e.Column.DisplayIndex].DisplayIndex = e.Column.DisplayIndex;
            }
        }

        /// <summary>
        /// DataGridのセル選択変更イベントハンドラ
        /// 行ヘッダと列ヘッダのスタイルを変更して強調表示する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_SelectedCellsChanged(object sender,  SelectedCellsChangedEventArgs e)
        {
            if (_isSyncingSelect) return;
            _isSyncingSelect = true;

            var sourceDataGrid = sender as System.Windows.Controls.DataGrid;
            var targetDataGrid = (sourceDataGrid == DataGrid1) ? DataGrid2 : DataGrid1;

            DataGridHelper.ClearHeaderStyles(sourceDataGrid);
            DataGridHelper.ClearHeaderStyles(targetDataGrid);

            if (sourceDataGrid.SelectedCells.Count <= 0)
            {
                _isSyncingSelect = false;
                return;
            }

            // 選択されたセルの情報を取得
            var selectedCell = sourceDataGrid.SelectedCells[0];
            int rowIndex = sourceDataGrid.Items.IndexOf(selectedCell.Item);
            int columnIndex = selectedCell.Column.DisplayIndex;

            // 行ヘッダーのスタイルを変更
            DataGridHelper.CreateRowHeaderStyle(sourceDataGrid, rowIndex);
            DataGridHelper.CreateColumnHeaderStyle(sourceDataGrid, columnIndex);
            DataGridHelper.CreateRowHeaderStyle(targetDataGrid, rowIndex);
            DataGridHelper.CreateColumnHeaderStyle(targetDataGrid, columnIndex);

            // 相手のDataGridでも同じセルを選択状態にする
            targetDataGrid.SelectedCells.Clear();
            targetDataGrid.SelectedCells.Add(new DataGridCellInfo(
                targetDataGrid.Items[rowIndex],
                targetDataGrid.Columns[columnIndex]
            ));

            // 選択セルの情報を表示
            SelectedColumnLabel.Content = $"Column: {selectedCell.Column.Header.ToString()}";
            SelectedCellValue1.Text = DataGridHelper.GetCellValue(DataGrid1.SelectedCells[0]);
            SelectedCellValue2.Text = DataGridHelper.GetCellValue(DataGrid2.SelectedCells[0]);

            _isSyncingSelect = false;
        }

        /// <summary>
        /// DataGridのLoadedイベントハンドラ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            var grid = sender as System.Windows.Controls.DataGrid;
            if (grid == null) return;

            var sv = FindVisualChild<ScrollViewer>(grid);
            if (grid == DataGrid1)
            {
                if (_dataGridScrollViewer1 != null)
                {
                    _dataGridScrollViewer1.ScrollChanged -= InternalScrollViewer_ScrollChanged;
                }
                _dataGridScrollViewer1 = sv;
                if (_dataGridScrollViewer1 != null)
                {
                    _dataGridScrollViewer1.ScrollChanged += InternalScrollViewer_ScrollChanged;
                }
            }
            else if (grid == DataGrid2)
            {
                if (_dataGridScrollViewer2 != null)
                {
                    _dataGridScrollViewer2.ScrollChanged -= InternalScrollViewer_ScrollChanged;
                }
                _dataGridScrollViewer2 = sv;
                if (_dataGridScrollViewer2 != null)
                {
                    _dataGridScrollViewer2.ScrollChanged += InternalScrollViewer_ScrollChanged;
                }
            }
        }

        /// <summary>
        /// VisualTree を探索して指定型の子要素を返すユーティリティ
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// DataGridのスクロール同期処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InternalScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncingScroll) return;

            try
            {
                _isSyncingScroll = true;
                var source = sender as ScrollViewer;
                if (source == null) return;

                // どちらが発生源かを判定して相手にオフセットを設定
                if (source == _dataGridScrollViewer1 && _dataGridScrollViewer2 != null)
                {
                    if (e.HorizontalChange != 0)
                    {
                        _dataGridScrollViewer2.ScrollToHorizontalOffset(source.HorizontalOffset);
                    }
                    if (e.VerticalChange != 0)
                    {
                        _dataGridScrollViewer2.ScrollToVerticalOffset(source.VerticalOffset);
                    }
                }
                else if (source == _dataGridScrollViewer2 && _dataGridScrollViewer1 != null)
                {
                    if (e.HorizontalChange != 0)
                    {
                        _dataGridScrollViewer1.ScrollToHorizontalOffset(source.HorizontalOffset);
                    }
                    if (e.VerticalChange != 0)
                    {
                        _dataGridScrollViewer1.ScrollToVerticalOffset(source.VerticalOffset);
                    }
                }
            }
            finally
            {
                _isSyncingScroll = false;
            }
        }
    }
}
