using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading;

namespace DeskSwitcher
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{


		public App()
		{
			DispatcherUnhandledException += (_, e) =>
			{
				MessageBox.Show(e.Exception.Message, "DeskSwitcher", MessageBoxButton.OK, MessageBoxImage.Error);
				e.Handled = true;
			};
		}

		private Mutex? _mutex;
		private bool _owned;

		protected override void OnStartup(StartupEventArgs e)
		{
			_mutex = new Mutex(true, "DeskSwitcher-single-instance", out _owned);
			if (!_owned) { Shutdown(); return; }
			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			if (_owned) _mutex?.ReleaseMutex();
			_mutex?.Dispose();
			base.OnExit(e);
		}
		


	}

}
