﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using PortableConfiguration;
using System.Runtime.InteropServices;
using System.Drawing;
using llc = LowLevelControls;

namespace PrintScreen
{
    class Program
    {
        static llc.KeyboardHook kbdHook = new llc.KeyboardHook();
        static Config config = new Config(Path.Combine(AssemblyDirectory, "config"));
        static volatile bool loop = true;
        static bool prscDown = false;
        static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        
        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public static void SetConsoleWindowVisibility(bool visible)
        {
            IntPtr hWnd = FindWindow(null, Console.Title);
            if (hWnd != IntPtr.Zero)
            {
                if (visible) ShowWindow(hWnd, 1); //1 = SW_SHOWNORMAL           
                else ShowWindow(hWnd, 0); //0 = SW_HIDE               
            }
        }

        static NotifyIcon notifyIcon = new NotifyIcon();
        static bool Visible = true;

        static void Main(string[] args)
        {
            //---------
            notifyIcon.DoubleClick += (s, e) =>
            {
                Visible = !Visible;
                SetConsoleWindowVisibility(Visible);
            };
            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = Application.ProductName;

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Exit", null, (s, e) => { Application.Exit(); });
            notifyIcon.ContextMenuStrip = contextMenu;

            Console.WriteLine("Running!");

            // Standard message loop to catch click-events on notify icon
            // Code after this method will be running only after Application.Exit()

            //---------
            Console.WriteLine("PrintScreen (made by priyankt3i, j3soon)");
            Console.WriteLine("1. Press PrintScreen to save the entire screen.");
            Console.WriteLine("2. Press Alt+PrintScreen to save the current window.");
            Console.WriteLine("3. Press Ctrl+C to exit.");
            string todayFolder = DateTime.Now.ToString("MM-dd-yyyy").ToString();
            config.DefaultConfigEvent += () => { 
                config["dir"] = @"%UserProfile%\Desktop\Screenshots\"+todayFolder+"\\"; 
                };
            config.Load();
            config.Save();
            Console.WriteLine("4. The captured screens will be saved in: " + config["dir"]);
            kbdHook.KeyDownEvent += kbdHook_KeyDownEvent;
            kbdHook.KeyUpEvent += kbdHook_KeyUpEvent;
            kbdHook.InstallGlobalHook();
            Console.CancelKeyPress += Console_CancelKeyPress;
            while (loop)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }
            kbdHook.UninstallGlobalHook();
            config.Save();

            Application.Run(); 
            notifyIcon.Visible = false;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            loop = false;
        }

        private static bool kbdHook_KeyDownEvent(llc.KeyboardHook sender, uint vkCode, bool injected)
        {
            if (vkCode == (uint)Keys.PrintScreen && !prscDown)
            {
                if (llc.Keyboard.IsKeyDown((int)Keys.Menu))
                    Console.WriteLine("Saved Current Window - " + PrintWindow());
                else
                    Console.WriteLine("Saved Current Screen - " + PrintScreen());
                prscDown = true;
                return true;
            }
            return false;
        }

        private static bool kbdHook_KeyUpEvent(llc.KeyboardHook sender, uint vkCode, bool injected)
        {
            if (vkCode == (uint)Keys.PrintScreen)
                prscDown = false;
            return false;
        }

        static String GetScreenshotName()
        {
            String path = Environment.ExpandEnvironmentVariables(config["dir"]);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, DateTime.Now.ToString("yyyyMMdd_HH-mm-ss"));
            int num = 0;
            for (; File.Exists(path + (num == 0 ? "" : ("_" + num)) + ".png"); num++) ;
            return path + (num == 0 ? "" : ("_" + num)) + ".png";
        }

        static String PrintScreen()
        {
            var count =  llc.Screen.GetScreenCount();
            llc.Natives.RECT rect = llc.Screen.GetVirtualScreenRect();
            Rectangle bounds = new Rectangle
            {
                X = rect.left,
                Y = rect.top,
                Width = rect.right - rect.left,
                Height = rect.bottom - rect.top
            };
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            // Since the program is single-threaded, race conditions won't happen
            // For multi-threaded program, use the following with try-catch can guarantee that no file is overwritten:
            // File.Open(path, FileMode.CreateNew)
            String name = GetScreenshotName();
            bmp.Save(name, ImageFormat.Png);
            return name;
        }

        static String PrintWindow()
        {
            llc.Natives.RECT rect = llc.Window.GetWindowBounds(llc.Window.GetForegroundWindow());
            Rectangle bounds = new Rectangle
            {
                X = rect.left,
                Y = rect.top,
                Width = rect.right - rect.left,
                Height = rect.bottom - rect.top
            };
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bmp))
                g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            String name = GetScreenshotName();
            bmp.Save(name, ImageFormat.Png);
            return name;
        }
        
    }
}