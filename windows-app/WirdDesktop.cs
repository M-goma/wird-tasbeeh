using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using Timer = System.Windows.Forms.Timer;

namespace WirdDesktop
{
    [Serializable]
    public class UserSettings
    {
        public int IntervalMinutes { get; set; }
        public int DurationSeconds { get; set; }
        public string Dhikr { get; set; }
        public bool RunAtStartup { get; set; }

        public UserSettings()
        {
            IntervalMinutes = 5;
            DurationSeconds = 30;
            Dhikr = "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ";
            RunAtStartup = true;
        }
    }

    internal static class Program
    {
        private static Mutex instanceMutex;

        [STAThread]
        private static void Main()
        {
            bool created;
            instanceMutex = new Mutex(true, "Local\\WirdTasbeehDesktop", out created);
            if (!created) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WirdContext());
            GC.KeepAlive(instanceMutex);
        }
    }

    internal sealed class WirdContext : ApplicationContext
    {
        private readonly NotifyIcon tray;
        private readonly Timer clock;
        private UserSettings settings;
        private DateTime nextReminder;
        private ReminderForm reminder;
        private SettingsForm settingsForm;
        private bool paused;
        private readonly ToolStripMenuItem pauseItem;

        public WirdContext()
        {
            settings = LoadSettings();
            EnsureStartupShortcut(settings.RunAtStartup);
            pauseItem = new ToolStripMenuItem("إيقاف التذكيرات مؤقتًا");
            pauseItem.Click += delegate
            {
                paused = !paused;
                pauseItem.Text = paused ? "استئناف التذكيرات" : "إيقاف التذكيرات مؤقتًا";
                tray.ShowBalloonTip(1800, "وِرد", paused ? "تم إيقاف التذكيرات مؤقتًا" : "تم استئناف التذكيرات", ToolTipIcon.Info);
            };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("الإعدادات", null, delegate { OpenSettings(); });
            menu.Items.Add(pauseItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("خروج", null, delegate { ExitThread(); });
            tray = new NotifyIcon();
            tray.Icon = CreateTrayIcon();
            tray.Text = "وِرد — تذكير التسبيح";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;
            tray.DoubleClick += delegate { OpenSettings(); };
            nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
            clock = new Timer();
            clock.Interval = 1000;
            clock.Tick += ClockTick;
            clock.Start();
        }

        private void ClockTick(object sender, EventArgs e)
        {
            if (paused || DateTime.Now < nextReminder) return;
            nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
            ShowReminder();
        }

        private void ShowReminder()
        {
            if (reminder != null && !reminder.IsDisposed) reminder.Close();
            reminder = new ReminderForm(settings.Dhikr, settings.DurationSeconds);
            reminder.Show();
        }

        private void OpenSettings()
        {
            if (settingsForm != null && !settingsForm.IsDisposed)
            {
                settingsForm.Activate();
                return;
            }
            settingsForm = new SettingsForm(settings, SaveSettings);
            settingsForm.Show();
        }

        private void SaveSettings(UserSettings value)
        {
            settings = value;
            StoreSettings(settings);
            EnsureStartupShortcut(settings.RunAtStartup);
            nextReminder = DateTime.Now.AddMinutes(settings.IntervalMinutes);
            tray.ShowBalloonTip(1800, "وِرد", "تم حفظ الإعدادات", ToolTipIcon.Info);
        }

        protected override void ExitThreadCore()
        {
            if (reminder != null && !reminder.IsDisposed) reminder.Close();
            if (settingsForm != null && !settingsForm.IsDisposed) settingsForm.Close();
            clock.Stop();
            clock.Dispose();
            tray.Visible = false;
            tray.Dispose();
            base.ExitThreadCore();
        }

        internal static UserSettings LoadSettings()
        {
            string file = SettingsFile();
            try
            {
                if (File.Exists(file))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(UserSettings));
                    using (FileStream stream = File.OpenRead(file)) return (UserSettings)serializer.Deserialize(stream);
                }
            }
            catch { }
            UserSettings value = new UserSettings();
            StoreSettings(value);
            return value;
        }

        internal static void StoreSettings(UserSettings value)
        {
            try
            {
                string file = SettingsFile();
                Directory.CreateDirectory(Path.GetDirectoryName(file));
                XmlSerializer serializer = new XmlSerializer(typeof(UserSettings));
                using (FileStream stream = File.Create(file)) serializer.Serialize(stream, value);
            }
            catch { }
        }

        private static string SettingsFile()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Wird", "settings.xml");
        }

        internal static void EnsureStartupShortcut(bool enabled)
        {
            try
            {
                string startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcut = Path.Combine(startup, "Wird.lnk");
                if (!enabled)
                {
                    if (File.Exists(shortcut)) File.Delete(shortcut);
                    return;
                }
                string exe = Application.ExecutablePath;
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                object shell = Activator.CreateInstance(shellType);
                object link = shellType.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, shell, new object[] { shortcut });
                Type linkType = link.GetType();
                linkType.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { exe });
                linkType.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { Path.GetDirectoryName(exe) });
                linkType.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { "وِرد — تذكير التسبيح" });
                linkType.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty, null, link, new object[] { exe + ",0" });
                linkType.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, link, null);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(link);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
            }
            catch { }
        }

        private static Icon CreateTrayIcon()
        {
            Bitmap bitmap = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bitmap))
            using (SolidBrush bg = new SolidBrush(Color.FromArgb(23, 76, 61)))
            using (SolidBrush fg = new SolidBrush(Color.White))
            using (Font font = new Font("Segoe UI", 19, FontStyle.Bold, GraphicsUnit.Pixel))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(bg, 1, 1, 30, 30);
                g.DrawString("و", font, fg, new PointF(4, 2));
            }
            IntPtr handle = bitmap.GetHicon();
            Icon icon = (Icon)Icon.FromHandle(handle).Clone();
            DestroyIcon(handle);
            bitmap.Dispose();
            return icon;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }

    internal sealed class ReminderForm : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x00000080 | 0x08000000 | 0x00000008;
                return cp;
            }
        }

        public ReminderForm(string dhikr, int seconds)
        {
            Text = "وِرد";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = Color.White;
            Size = new Size(390, 190);
            Padding = new Padding(20);

            Panel accent = new Panel();
            accent.BackColor = Color.FromArgb(196, 148, 80);
            accent.Location = new Point(0, 0);
            accent.Size = new Size(390, 4);
            Controls.Add(accent);

            Label close = new Label();
            close.Text = "×";
            close.Font = new Font("Segoe UI", 17, FontStyle.Regular);
            close.ForeColor = Color.FromArgb(125, 140, 130);
            close.TextAlign = ContentAlignment.MiddleCenter;
            close.Size = new Size(30, 30);
            close.Location = new Point(340, 12);
            close.Cursor = Cursors.Hand;
            close.Click += delegate { Close(); };
            Controls.Add(close);

            Label brand = new Label();
            brand.Text = "وِرد  ·  لحظةُ ذِكر";
            brand.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            brand.ForeColor = Color.FromArgb(125, 145, 130);
            brand.Location = new Point(24, 20);
            brand.Size = new Size(290, 25);
            Controls.Add(brand);

            Label title = new Label();
            title.Text = "ألا بذكر الله تطمئن القلوب";
            title.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(33, 75, 57);
            title.TextAlign = ContentAlignment.MiddleRight;
            title.Location = new Point(22, 57);
            title.Size = new Size(342, 35);
            Controls.Add(title);

            Label zikr = new Label();
            zikr.Text = dhikr;
            zikr.Font = new Font("Segoe UI", 19, FontStyle.Bold);
            zikr.ForeColor = Color.FromArgb(83, 121, 87);
            zikr.TextAlign = ContentAlignment.MiddleRight;
            zikr.Location = new Point(22, 98);
            zikr.Size = new Size(342, 42);
            Controls.Add(zikr);

            Label note = new Label();
            note.Text = "سيختفي التذكير تلقائيًا";
            note.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            note.ForeColor = Color.FromArgb(155, 165, 155);
            note.TextAlign = ContentAlignment.MiddleRight;
            note.Location = new Point(22, 148);
            note.Size = new Size(342, 22);
            Controls.Add(note);

            Timer timer = new Timer();
            timer.Interval = Math.Max(5, seconds) * 1000;
            timer.Tick += delegate { timer.Stop(); Close(); };
            Shown += delegate
            {
                Screen screen = Screen.FromPoint(Cursor.Position);
                Rectangle area = screen.WorkingArea;
                Location = new Point(area.Right - Width - 18, area.Top + 18);
                SetWindowPos(Handle, HWND_TOPMOST, Location.X, Location.Y, 0, 0, SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                timer.Start();
            };
            FormClosed += delegate { timer.Dispose(); };
        }
    }

    internal sealed class SettingsForm : Form
    {
        private readonly NumericUpDown interval;
        private readonly NumericUpDown duration;
        private readonly ComboBox dhikr;
        private readonly CheckBox startup;
        private readonly Action<UserSettings> save;

        public SettingsForm(UserSettings current, Action<UserSettings> saveAction)
        {
            save = saveAction;
            Text = "إعدادات وِرد";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            Font = new Font("Segoe UI", 10);
            BackColor = Color.FromArgb(247, 247, 241);
            ClientSize = new Size(390, 345);

            Label title = new Label();
            title.Text = "وِرد — إعدادات التذكير";
            title.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(23, 60, 49);
            title.Location = new Point(22, 17);
            title.Size = new Size(345, 32);
            Controls.Add(title);

            AddLabel("الذِكر", 62);
            dhikr = new ComboBox();
            dhikr.DropDownStyle = ComboBoxStyle.DropDownList;
            dhikr.Items.AddRange(new object[] { "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ", "سُبْحَانَ اللَّهِ", "الْحَمْدُ لِلَّهِ", "اللَّهُ أَكْبَرُ", "لَا إِلَهَ إِلَّا اللَّهُ", "أَسْتَغْفِرُ اللَّهَ", "اللَّهُمَّ صَلِّ عَلَى مُحَمَّدٍ" });
            dhikr.Location = new Point(22, 86);
            dhikr.Size = new Size(345, 30);
            dhikr.SelectedItem = current.Dhikr;
            if (dhikr.SelectedIndex < 0) dhikr.SelectedIndex = 0;
            Controls.Add(dhikr);

            AddLabel("التذكير كل (دقيقة)", 127);
            interval = new NumericUpDown();
            interval.Minimum = 1;
            interval.Maximum = 180;
            interval.Value = Math.Max(interval.Minimum, Math.Min(interval.Maximum, current.IntervalMinutes));
            interval.Location = new Point(22, 151);
            interval.Size = new Size(345, 29);
            Controls.Add(interval);

            AddLabel("مدة ظهور التذكير (ثانية)", 191);
            duration = new NumericUpDown();
            duration.Minimum = 5;
            duration.Maximum = 600;
            duration.Increment = 5;
            duration.Value = Math.Max(duration.Minimum, Math.Min(duration.Maximum, current.DurationSeconds));
            duration.Location = new Point(22, 215);
            duration.Size = new Size(345, 29);
            Controls.Add(duration);

            startup = new CheckBox();
            startup.Text = "تشغيل وِرد مع بدء Windows";
            startup.Checked = current.RunAtStartup;
            startup.Location = new Point(22, 257);
            startup.Size = new Size(345, 26);
            Controls.Add(startup);

            Button saveButton = new Button();
            saveButton.Text = "حفظ";
            saveButton.BackColor = Color.FromArgb(23, 76, 61);
            saveButton.ForeColor = Color.White;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Location = new Point(22, 295);
            saveButton.Size = new Size(345, 34);
            saveButton.Click += delegate
            {
                UserSettings value = new UserSettings();
                value.IntervalMinutes = Decimal.ToInt32(interval.Value);
                value.DurationSeconds = Decimal.ToInt32(duration.Value);
                value.Dhikr = Convert.ToString(dhikr.SelectedItem);
                value.RunAtStartup = startup.Checked;
                save(value);
                Close();
            };
            Controls.Add(saveButton);
        }

        private void AddLabel(string text, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.ForeColor = Color.FromArgb(73, 98, 82);
            label.Location = new Point(22, y);
            label.Size = new Size(345, 21);
            Controls.Add(label);
        }
    }
}

