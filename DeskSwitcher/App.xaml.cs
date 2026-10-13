using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;
using System.Runtime.InteropServices;

namespace DeskSwitcher
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		private const string MutexName = "DeskSwitcher-single-instance";
		private const string ShowEventName = "DeskSwitcher-show-window";

		private Mutex? _mutex;
		private bool _owned;
		private EventWaitHandle? _showEvent;
		private RegisteredWaitHandle? _showWait;
		private bool _errorShown;

		public App()
		{
			DispatcherUnhandledException += (_, e) =>
			{
				Logger.Error("Необработанное исключение в UI-потоке", e.Exception);
				ShowErrorOnce(e.Exception);
				e.Handled = true;
			};

			AppDomain.CurrentDomain.UnhandledException += (_, e) =>
				Logger.Error("Критическая ошибка, программа завершается", e.ExceptionObject as Exception);

			TaskScheduler.UnobservedTaskException += (_, e) =>
			{
				Logger.Error("Необработанное исключение в задаче", e.Exception);
				e.SetObserved();
			};
		}
		private void ShowErrorOnce(Exception ex)
		{
			if (_errorShown) return;     // дальше пишем только в журнал
			_errorShown = true;

			MessageBox.Show(
				$"{ex.Message}\n\nПодробности в журнале:\n{Logger.FilePath}\n\n" +
				"Следующие ошибки будут только записаны в журнал.",
				"DeskSwitcher", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			_mutex = new Mutex(true, MutexName, out _owned);
			_showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);

			if (!_owned)
			{
				AllowSetForegroundWindow(-1);   // ASFW_ANY: первая копия сможет выйти на передний план
				_showEvent.Set();               // просим её показать окно
				Shutdown();
				return;
			}
			Logger.Info($"Запуск. Windows {Environment.OSVersion.Version}, .NET {Environment.Version}");
			base.OnStartup(e);

			// Ждём сигналы от повторных запусков. Отдельный поток для этого не нужен.
			_showWait = ThreadPool.RegisterWaitForSingleObject(
				_showEvent,
				(_, _) => Dispatcher.InvokeAsync(ShowMainWindow),
				null, Timeout.Infinite, executeOnlyOnce: false);
		}
		private void ShowMainWindow()
		{
			if (MainWindow is DeskSwitcher.MainWindow w)
			{
				w.ShowFromTray();         //	показываем окошко
				w.ShowAlreadyRunning();   //	показываем всплывашку
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			_showWait?.Unregister(null);
			_showEvent?.Dispose();
			if (_owned) _mutex?.ReleaseMutex();
			_mutex?.Dispose();
			base.OnExit(e);
		}
		[DllImport("user32.dll")]
		private static extern bool AllowSetForegroundWindow(int dwProcessId);
	}

}
