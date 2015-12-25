using System;
using System.Windows.Forms;
using Updater.Utilities;

namespace Launcher
{
    public static class Program
    {
        private static string _root;
        private static bool _runSilent = false;
        private static bool _fullReload = false;
        private static bool _validate = false;

        private static void ParseArg(string[] args, ref int id)
        {
            var arg = args[id];
            switch (arg.ToLowerInvariant())
            {
                case "-silent":
                    _runSilent = true;
                    break;
                case "-reload":
                    _fullReload = true;
                    break;
                case "-validate":
                    _validate = true;
                    break;
                case "-root":
                    if (id + 1 >= args.Length)
                    {
                        MessageBox.Show("Был получен ключ -root последним аргументом.", "Launcher");
                        Application.Exit();
                    }
                    _root = args[++id];
                    break;
                case "-help":
                case "-h":
                case "-?":
                    MessageBox.Show(
                        "-silent - запуск без GUI.\r\n" +
                        "-reload - загрузить актуальные файлы, вне зависимости от текущей версии.\r\n" +
                        "-validate - сверить все файлы, даже если версия актуалбна.\r\n" +
                        "-root <путь> - путь к папке, куда нужно выкладывать файлы клиента.\r\n" +
                        "-h/-help/-? - вывести эту подсказку.",
                        "Launcher"
                    );
                    Exit();
                    break;
                default:
                    MessageBox.Show(
                        $"Был получен неизвестный ключ: {arg}.\r\n" +
                        "Для списка возможных ключей используйте с ключом -h/-help/-?.",
                        "Launcher"
                    );
                    Exit();
                    break;
            }
        }

        public static void Exit()
        {
            LauncherSettings.Default.Save();
            Environment.Exit(0);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            _root = LauncherSettings.Default.ClientPath ?? "";

            for (var i = 0; i < args.Length; i++)
                ParseArg(args, ref i);

            if (!_root.Contains(":"))
                _root = $"{Application.StartupPath}\\{_root}";

            if (_runSilent)
            {
                var upd = new Updater();
                LauncherSettings.Default.Version = upd.Update(_root, _fullReload, _validate, LauncherSettings.Default.Version).Await();
                Exit();
            }

            MessageBox.Show(
                "Пока поддерживается только с включенным -silent режимом.\r\n" +
                "Для списка возможных ключей используйте с ключом -h/-help/-?.",
                "Launcher"
            );
            Exit();
        }
    }
}
