using System;
using System.Diagnostics;
using System.Windows.Forms;
using Updater.Utilities;

namespace Launcher
{
    public static class Program
    {
        public static void Message(string text, string caption)
        {
            if (LauncherSettings.Default.ConsoleOutput)
#if !DEBUG
                Console.WriteLine(text);
#else
                Debug.WriteLine(text);
#endif
            else
                MessageBox.Show(text, caption);
        }

        public static void Exit()
        {
            LauncherSettings.Default.Save();
            Environment.Exit(0);
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread] public static void Main(string[] args)
        {
            _root = LauncherSettings.Default.ClientPath ?? "";

            for (var i = 0; i < args.Length; i++)
                ParseArg(args, ref i);

            if (!_root.Contains(":"))
                _root = $"{Application.StartupPath}\\{_root}";

            //if (_runSilent)
            //{
            var upd = new Updater();
            LauncherSettings.Default.Version =
                upd.Update(_root, _fullReload, _validate, LauncherSettings.Default.Version)
                    .Await();
            Exit();
            //}
            //
            //Message(
            //    "Пока поддерживается только с включенным -silent режимом.\r\n" +
            //    "Для списка возможных ключей используйте с ключом -h/-help/-?.",
            //    "Launcher"
            //);
            //Exit();
        }

        private static string _root;
        private static bool _runSilent;
        private static bool _fullReload;
        private static bool _validate;

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
                        Message("Был получен ключ -root последним аргументом.", "Launcher");
                        Application.Exit();
                    }
                    _root = args[++id];
                    break;
                case "-help":
                case "-h":
                case "-?":
                    Message(
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
                    Message(
                        $"Был получен неизвестный ключ: {arg}.\r\n" +
                        "Для списка возможных ключей используйте с ключом -h/-help/-?.",
                        "Launcher"
                    );
                    Exit();
                    break;
            }
        }
    }
}