using AnyConsole;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateIISLogs.Utility
{
    public static class C
    {
        private static object SyncLock = new object();
        private static ExtendedConsole _Console = new ExtendedConsole();
        private static ConsoleDataContext _DataContext = new ConsoleDataContext();

        public static void Init()
        {
            _Console.Configure(config =>
            {
                config.SetStaticRow("Header", RowLocation.Top, Color.White, Color.DarkRed);
                config.SetStaticRow("SubHeader", RowLocation.Top, 1, Color.White, Color.FromArgb(30, 30, 30));
                config.SetStaticRow("Footer", RowLocation.Bottom, Color.White, Color.DarkBlue);
                config.SetUpdateInterval(TimeSpan.FromMilliseconds(500));
                config.SetLogHistoryContainer(RowLocation.Bottom, 10);
                config.SetMaxHistoryLines(10);
                config.RegisterComponent<ProgressComponent>("ProgressComponent");
                config.SetDataContext(_DataContext);
            });

            _Console.WriteRow("Header", "Generate IIS Logs v1.0", ColumnLocation.Left);
            _Console.WriteRow("Header", Component.DateTimeUtc, ColumnLocation.Right, componentParameter: "MMMM dd yyyy hh:mm:ss tt");
            //_Console.WriteRow("SubHeader", Component.NetworkTransfer, ColumnLocation.Left);
            //_Console.WriteRow("SubHeader", Component.MemoryUsed, ColumnLocation.Right);
            _Console.WriteRow("Footer", Component.Custom, "ProgressComponent", ColumnLocation.Right);
            _Console.Options.RenderOptions = RenderOptions.HideCursor;
            _Console.Start();
        }

        public static ConsoleDataContext DataContext => _DataContext;
        public static IExtendedConsole Console => _Console;

        public static void WriteBottomLeft(string str)
        {
            _Console.WriteRow("Footer", str, ColumnLocation.Left);
        }

        public static void WriteBottomRight(string str)
        {
            _Console.WriteRow("Footer", str, ColumnLocation.Right);
        }

        public static void History(string str)
        {
            _Console.WriteLine(str);
        }
    }
}
