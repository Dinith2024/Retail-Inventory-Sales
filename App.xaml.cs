using System.Windows;
namespace RetailInventorySales
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Surface any unhandled exception in a friendly dialog rather
            // than letting the application crash silently - this keeps a
            // demo running smoothly even if something unexpected happens
            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show(
                    $"An unexpected error occurred:\n\n{GetInnermostException(args.Exception).Message}",
                    "Retail Inventory & Sales",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                args.Handled = true;
            };

            MainWindow = new MainWindow();
            MainWindow.Show();
        }

        private static Exception GetInnermostException(Exception exception)
        {
            while (exception.InnerException != null)
            {
                exception = exception.InnerException;
            }

            return exception;
        }
    }
}