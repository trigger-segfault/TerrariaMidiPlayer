using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using TerrariaMidiPlayer;
using TerrariaMidiPlayer.Windows;

namespace TerrariaMidiPlayer {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		private void OnAppStartup(object sender, StartupEventArgs e) {
			// Catch exceptions not in a UI thread
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(OnAppDomainUnhandledException);
		}
		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			if (ErrorMessageBox.Show(e.Exception))
				Environment.Exit(0);
			e.Handled = true;
		}
		private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e) {
			Dispatcher.Invoke(() => {
				if (ErrorMessageBox.Show(e.ExceptionObject))
					Environment.Exit(0);
			});
		}
	}
}
