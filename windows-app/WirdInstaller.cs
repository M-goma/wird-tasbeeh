using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class WirdInstaller
{
    private const string ProductName = "وِرد — تذكير التسبيح";
    private const string RelativeInstallPath = "Programs\\Wird";

    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            if (args.Length > 0 && String.Equals(args[0], "/uninstall", StringComparison.OrdinalIgnoreCase))
                Uninstall();
            else
                Install();
        }
        catch (Exception ex)
        {
            MessageBox.Show("تعذّر إكمال العملية.\r\n" + ex.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void Install()
    {
        string installDir = InstallDirectory();
        Directory.CreateDirectory(installDir);
        string appPath = Path.Combine(installDir, "Wird.exe");
        using (Stream input = Assembly.GetExecutingAssembly().GetManifestResourceStream("WirdDesktopPayload.exe"))
        {
            if (input == null) throw new InvalidOperationException("ملف البرنامج غير موجود داخل أداة التثبيت.");
            using (FileStream output = File.Create(appPath)) input.CopyTo(output);
        }

        string installerPath = Path.Combine(installDir, "Wird-Setup.exe");
        string runningInstaller = Assembly.GetExecutingAssembly().Location;
        if (!String.Equals(Path.GetFullPath(runningInstaller), Path.GetFullPath(installerPath), StringComparison.OrdinalIgnoreCase))
            File.Copy(runningInstaller, installerPath, true);

        string startMenu = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Wird.lnk");
        string desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "وِرد.lnk");
        string startup = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Wird.lnk");
        CreateShortcut(startMenu, appPath);
        CreateShortcut(desktop, appPath);
        CreateShortcut(startup, appPath);

        using (RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\WirdTasbeeh"))
        {
            key.SetValue("DisplayName", ProductName);
            key.SetValue("DisplayVersion", "1.0.0");
            key.SetValue("Publisher", "Wird");
            key.SetValue("InstallLocation", installDir);
            key.SetValue("DisplayIcon", appPath + ",0");
            key.SetValue("UninstallString", "\"" + installerPath + "\" /uninstall");
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }

        Process.Start(appPath);
        MessageBox.Show("تم تثبيت وِرد. يعمل الآن في الخلفية، وستظهر التذكيرات فوق نوافذ Windows حسب إعداداتك.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void Uninstall()
    {
        string installDir = InstallDirectory();
        try
        {
            foreach (Process process in Process.GetProcessesByName("Wird"))
            {
                try { process.Kill(); process.WaitForExit(3000); }
                catch { }
                process.Dispose();
            }
        }
        catch { }

        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Wird.lnk"));
        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "وِرد.lnk"));
        DeleteShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Wird.lnk"));
        try { Registry.CurrentUser.DeleteSubKeyTree("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\WirdTasbeeh", false); }
        catch { }

        string command = "/c ping 127.0.0.1 -n 3 > nul & rmdir /s /q \"" + installDir + "\"";
        ProcessStartInfo cleanup = new ProcessStartInfo("cmd.exe", command);
        cleanup.CreateNoWindow = true;
        cleanup.WindowStyle = ProcessWindowStyle.Hidden;
        cleanup.UseShellExecute = false;
        Process.Start(cleanup);
        MessageBox.Show("تمت إزالة وِرد. إعداداتك محفوظة على الجهاز.", ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string InstallDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), RelativeInstallPath);
    }

    private static void CreateShortcut(string path, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        if (shellType == null) return;
        object shell = Activator.CreateInstance(shellType);
        object shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { path });
        Type shortcutType = shortcut.GetType();
        shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { target });
        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { Path.GetDirectoryName(target) });
        shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { ProductName });
        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { target + ",0" });
        shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        Marshal.FinalReleaseComObject(shortcut);
        Marshal.FinalReleaseComObject(shell);
    }

    private static void DeleteShortcut(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}

