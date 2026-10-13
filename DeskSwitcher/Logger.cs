using System.IO;
using System.Text;

namespace DeskSwitcher;

public static class Logger
{
	private const long MaxBytes = 512 * 1024;      // больше — старый журнал уходит в .old
	private static readonly object _lock = new();

	public static string Folder { get; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeskSwitcher");

	public static string FilePath { get; } = Path.Combine(Folder, "error.log");

	public static void Info(string message) => Write("INFO", message, null);
	public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

	private static void Write(string level, string message, Exception? ex)
	{
		try
		{
			lock (_lock)
			{
				Directory.CreateDirectory(Folder);

				var info = new FileInfo(FilePath);
				if (info.Exists && info.Length > MaxBytes) File.Move(FilePath, FilePath + ".old", overwrite: true);

				var sb = new StringBuilder();
				sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" [").Append(level).Append("] ").AppendLine(message);
				if (ex != null) sb.AppendLine(ex.ToString());
				sb.AppendLine();

				File.AppendAllText(FilePath, sb.ToString(), Encoding.UTF8);
			}
		}
		catch
		{
			// журнал не должен ронять программу
		}
	}
}