using System.Windows;

namespace SQLiteDiff
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(args.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            base.OnStartup(e);
        }
    }
}
