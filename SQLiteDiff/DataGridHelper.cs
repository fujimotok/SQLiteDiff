using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace SQLiteDiff
{
    public class DataGridHelper
    {
        private const string HIGHLIGHTCOLOR = "LightBlue";

        /// <summary>
        /// ヘッダーのスタイルをクリアする関数
        /// </summary>
        /// <param name="dataGrid"></param>
        public static void ClearHeaderStyles(System.Windows.Controls.DataGrid dataGrid)
        {
            // すべての行ヘッダーのスタイルをクリア
            foreach (var item in dataGrid.Items)
            {
                int rowIndex = dataGrid.Items.IndexOf(item);
                DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex);
                if (row != null)
                {
                    row.HeaderStyle = null;
                }
            }

            // すべての列ヘッダーのスタイルをクリア
            foreach (var column in dataGrid.Columns)
            {
                column.HeaderStyle = null;
            }
        }

        /// <summary>
        /// ヘッダーのスタイルを作成する関数
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="index"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public static Style CreateRowHeaderStyle(System.Windows.Controls.DataGrid dataGrid, int index)
        {
            Style style = new Style(typeof(DataGridRowHeader));
            style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(HIGHLIGHTCOLOR))));

            if (index >= 0)
            {
                DataGridRow row = (DataGridRow)dataGrid.ItemContainerGenerator.ContainerFromIndex(index);
                if (row != null)
                {
                    // 行ヘッダーの背景色を変更
                    row.HeaderStyle = style;
                }
            }
            return style;
        }

        /// <summary>
        /// ヘッダーのスタイルを作成する関数
        /// </summary>
        /// <param name="dataGrid"></param>
        /// <param name="index"></param>
        /// <param name="backgroundColor"></param>
        /// <returns></returns>
        public static Style CreateColumnHeaderStyle(System.Windows.Controls.DataGrid dataGrid, int index)
        {
            Style style = new Style(typeof(DataGridColumnHeader));
            style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(HIGHLIGHTCOLOR))));

            if (index >= 0)
            {
                DataGridColumn column = dataGrid.Columns[index];
                if (column != null)
                {
                    // 列ヘッダーの背景色を変更
                    column.HeaderStyle = style;
                }
            }

            return style;
        }

        /// <summary>
        /// Cellのスタイルを作成する関数
        /// </summary>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static Style CreateCellStyle(string columnName)
        {
            Style cellStyle = new Style(typeof(DataGridCell));
            cellStyle.Setters.Add(new Setter(Control.BackgroundProperty, new Binding($"[{columnName}].BackgroundColor")));
            cellStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new Binding($"[{columnName}].BackgroundColor")));
            cellStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));

            // 選択時のトリガー
            var trigger = new Trigger
            {
                Property = DataGridCell.IsSelectedProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(Control.BorderBrushProperty, Brushes.Blue));
            trigger.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(2)));
            cellStyle.Triggers.Add(trigger);
            return cellStyle;
        }

        /// <summary>
        /// DataGridTextColumnを作成する関数
        /// </summary>
        /// <param name="header"></param>
        /// <param name="columnName"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static DataGridTextColumn CreateDataGridTextColumn(string header, string columnName, DataGridLength width)
        {
            DataGridTextColumn textColumn = new DataGridTextColumn
            {
                Header = header,
                Binding = new Binding($"[{columnName}].Value"),
                Width = width,
                CellStyle = CreateCellStyle(columnName)
            };

            return textColumn;
        }
    }
}