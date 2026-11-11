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
        public int ActiveFromMinutes { get; set; }
        public int ActiveUntilMinutes { get; set; }
        public bool DarkMode { get; set; }

        public UserSettings()
        {
            IntervalMinutes = 5;
            DurationSeconds = 30;
            Dhikr = "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ";
            RunAtStartup = true;
            ActiveFromMinutes = 480;
            ActiveUntilMinutes = 1320;
            DarkMode = false;
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
        private int dhikrRotationIndex = -1;
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
            if (!IsWithinActiveHours(DateTime.Now)) return;
            ShowReminder();
        }

        private bool IsWithinActiveHours(DateTime now)
        {
            int current = now.Hour * 60 + now.Minute;
            int from = settings.ActiveFromMinutes;
            int until = settings.ActiveUntilMinutes;
            return from <= until ? current >= from && current < until : current >= from || current < until;
        }

        private void ShowReminder()
        {
            if (reminder != null && !reminder.IsDisposed) reminder.Close();
            settings.Dhikr = NextDhikr(settings.Dhikr);
            StoreSettings(settings);
            reminder = new ReminderForm(settings.Dhikr, settings.DurationSeconds, settings.DarkMode);
            reminder.Show();
        }

        private string NextDhikr(string current)
        {
            string[] adhkar = { "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ", "سُبْحَانَ اللَّهِ", "الْحَمْدُ لِلَّهِ", "اللَّهُ أَكْبَرُ", "لَا إِلَهَ إِلَّا اللَّهُ", "أَسْتَغْفِرُ اللَّهَ", "لَا حَوْلَ وَلَا قُوَّةَ إِلَّا بِاللَّهِ", "سُبْحَانَ اللَّهِ وَالْحَمْدُ لِلَّهِ وَلَا إِلَهَ إِلَّا اللَّهُ وَاللَّهُ أَكْبَرُ", "اللَّهُمَّ صَلِّ عَلَى مُحَمَّدٍ", "سُبْحَانَ اللَّهِ الْعَظِيمِ وَبِحَمْدِهِ", "لَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ، وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ", "أَسْتَغْفِرُ اللَّهَ الْعَظِيمَ وَأَتُوبُ إِلَيْهِ", "حَسْبِيَ اللَّهُ لَا إِلَهَ إِلَّا هُوَ، عَلَيْهِ تَوَكَّلْتُ وَهُوَ رَبُّ الْعَرْشِ الْعَظِيمِ", "اللَّهُمَّ أَعِنِّي عَلَى ذِكْرِكَ وَشُكْرِكَ وَحُسْنِ عِبَادَتِكَ" };
            int index = Array.IndexOf(adhkar, current);
            if (index >= 0) dhikrRotationIndex = index;
            else if (dhikrRotationIndex == -1) { dhikrRotationIndex = -2; return current; }
            else if (dhikrRotationIndex == -2) dhikrRotationIndex = -1;
            dhikrRotationIndex = (dhikrRotationIndex + 1) % adhkar.Length;
            return adhkar[dhikrRotationIndex];
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
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic link = shell.CreateShortcut(shortcut);
                link.TargetPath = exe;
                link.WorkingDirectory = Path.GetDirectoryName(exe);
                link.Description = "وِرد — تذكير التسبيح";
                link.IconLocation = exe + ",0";
                link.Save();
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject((object)link);
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject((object)shell);
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

        public ReminderForm(string dhikr, int seconds, bool darkMode)
        {
            Text = "وِرد";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            BackColor = darkMode ? Color.FromArgb(28, 39, 33) : Color.White;
            Size = new Size(360, 150);
            Padding = new Padding(14);

            Panel accent = new Panel();
            accent.BackColor = Color.FromArgb(196, 148, 80);
            accent.Location = new Point(0, 0);
            accent.Size = new Size(360, 4);
            Controls.Add(accent);

            Label close = new Label();
            close.Text = "×";
            close.Font = new Font("Segoe UI", 17, FontStyle.Regular);
            close.ForeColor = darkMode ? Color.FromArgb(170, 187, 174) : Color.FromArgb(125, 140, 130);
            close.TextAlign = ContentAlignment.MiddleCenter;
            close.Size = new Size(26, 26);
            close.Location = new Point(319, 8);
            close.Cursor = Cursors.Hand;
            close.Click += delegate { Close(); };
            Controls.Add(close);

            Label brand = new Label();
            brand.Text = "وِرد  ·  لحظةُ ذِكر";
            brand.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            brand.ForeColor = darkMode ? Color.FromArgb(170, 187, 174) : Color.FromArgb(125, 145, 130);
            brand.Location = new Point(18, 14);
            brand.Size = new Size(260, 22);
            Controls.Add(brand);

            Label title = new Label();
            title.Text = "ألا بذكر الله تطمئن القلوب";
            title.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            title.ForeColor = darkMode ? Color.FromArgb(220, 235, 222) : Color.FromArgb(33, 75, 57);
            title.TextAlign = ContentAlignment.MiddleRight;
            title.Location = new Point(18, 43);
            title.Size = new Size(320, 29);
            Controls.Add(title);

            Label zikr = new Label();
            zikr.Text = dhikr;
            zikr.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            zikr.ForeColor = darkMode ? Color.FromArgb(177, 211, 181) : Color.FromArgb(83, 121, 87);
            zikr.TextAlign = ContentAlignment.MiddleRight;
            zikr.Location = new Point(18, 76);
            zikr.Size = new Size(324, 34);
            zikr.AutoEllipsis = false;
            Size measuredText = TextRenderer.MeasureText(dhikr, zikr.Font, new Size(324, 1000), TextFormatFlags.WordBreak | TextFormatFlags.RightToLeft);
            zikr.Height = Math.Max(34, measuredText.Height + 4);
            Controls.Add(zikr);

            Label note = new Label();
            note.Text = "سيختفي التذكير تلقائيًا";
            note.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            note.ForeColor = darkMode ? Color.FromArgb(164, 178, 166) : Color.FromArgb(155, 165, 155);
            note.TextAlign = ContentAlignment.MiddleRight;
            note.Location = new Point(18, zikr.Bottom + 4);
            note.Size = new Size(324, 20);
            Controls.Add(note);
            Height = note.Bottom + 12;

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
        private readonly CheckBox darkMode;
        private readonly DateTimePicker activeFrom;
        private readonly DateTimePicker activeUntil;
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
            ClientSize = new Size(390, 455);

            Label title = new Label();
            title.Text = "وِرد — إعدادات التذكير";
            title.Font = new Font("Segoe UI", 15, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(23, 60, 49);
            title.Location = new Point(22, 17);
            title.Size = new Size(345, 32);
            Controls.Add(title);

            AddLabel("اختر ذكرًا أو اكتب ذكرًا خاصًا", 60);
            dhikr = new ComboBox();
            dhikr.DropDownStyle = ComboBoxStyle.DropDown;
            dhikr.Items.AddRange(new object[] { "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ", "سُبْحَانَ اللَّهِ", "الْحَمْدُ لِلَّهِ", "اللَّهُ أَكْبَرُ", "لَا إِلَهَ إِلَّا اللَّهُ", "أَسْتَغْفِرُ اللَّهَ", "لَا حَوْلَ وَلَا قُوَّةَ إِلَّا بِاللَّهِ", "سُبْحَانَ اللَّهِ وَالْحَمْدُ لِلَّهِ وَلَا إِلَهَ إِلَّا اللَّهُ وَاللَّهُ أَكْبَرُ", "اللَّهُمَّ صَلِّ عَلَى مُحَمَّدٍ", "سُبْحَانَ اللَّهِ الْعَظِيمِ وَبِحَمْدِهِ", "لَا إِلَهَ إِلَّا اللَّهُ وَحْدَهُ لَا شَرِيكَ لَهُ، لَهُ الْمُلْكُ وَلَهُ الْحَمْدُ، وَهُوَ عَلَى كُلِّ شَيْءٍ قَدِيرٌ", "أَسْتَغْفِرُ اللَّهَ الْعَظِيمَ وَأَتُوبُ إِلَيْهِ", "حَسْبِيَ اللَّهُ لَا إِلَهَ إِلَّا هُوَ، عَلَيْهِ تَوَكَّلْتُ وَهُوَ رَبُّ الْعَرْشِ الْعَظِيمِ", "اللَّهُمَّ أَعِنِّي عَلَى ذِكْرِكَ وَشُكْرِكَ وَحُسْنِ عِبَادَتِكَ" });
            dhikr.Text = current.Dhikr;
            if (!dhikr.Items.Contains(current.Dhikr)) dhikr.Items.Add(current.Dhikr);
            dhikr.Location = new Point(22, 86);
            dhikr.Size = new Size(345, 30);
            Controls.Add(dhikr);

            AddLabel("التذكير كل (دقيقة)", 122);
            interval = new NumericUpDown();
            interval.Minimum = 1;
            interval.Maximum = 180;
            interval.Value = Math.Max(interval.Minimum, Math.Min(interval.Maximum, current.IntervalMinutes));
            interval.Location = new Point(22, 146);
            interval.Size = new Size(345, 29);
            Controls.Add(interval);

            AddLabel("مدة ظهور التذكير (ثانية)", 184);
            duration = new NumericUpDown();
            duration.Minimum = 5;
            duration.Maximum = 600;
            duration.Increment = 5;
            duration.Value = Math.Max(duration.Minimum, Math.Min(duration.Maximum, current.DurationSeconds));
            duration.Location = new Point(22, 208);
            duration.Size = new Size(345, 29);
            Controls.Add(duration);

            startup = new CheckBox();
            AddLabel("وقت تشغيل التذكير", 246);
            activeFrom = CreateTimePicker(current.ActiveFromMinutes, new Point(22, 270));
            activeUntil = CreateTimePicker(current.ActiveUntilMinutes, new Point(205, 270));
            Controls.Add(activeFrom);
            Controls.Add(activeUntil);

            startup.Text = "تشغيل وِرد مع بدء Windows";
            startup.Checked = current.RunAtStartup;
            startup.Location = new Point(22, 309);
            startup.Size = new Size(345, 26);
            Controls.Add(startup);

            darkMode = new CheckBox();
            darkMode.Text = "الوضع الداكن";
            darkMode.Checked = current.DarkMode;
            darkMode.Location = new Point(22, 340);
            darkMode.Size = new Size(345, 26);
            Controls.Add(darkMode);

            Button saveButton = new Button();
            saveButton.Text = "حفظ";
            saveButton.BackColor = Color.FromArgb(23, 76, 61);
            saveButton.ForeColor = Color.White;
            saveButton.FlatStyle = FlatStyle.Flat;
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Location = new Point(22, 390);
            saveButton.Size = new Size(345, 34);
            saveButton.Click += delegate
            {
                UserSettings value = new UserSettings();
                value.IntervalMinutes = Decimal.ToInt32(interval.Value);
                value.DurationSeconds = Decimal.ToInt32(duration.Value);
                value.Dhikr = String.IsNullOrWhiteSpace(dhikr.Text) ? "سُبْحَانَ اللَّهِ وَبِحَمْدِهِ" : dhikr.Text.Trim();
                value.RunAtStartup = startup.Checked;
                value.DarkMode = darkMode.Checked;
                value.ActiveFromMinutes = activeFrom.Value.Hour * 60 + activeFrom.Value.Minute;
                value.ActiveUntilMinutes = activeUntil.Value.Hour * 60 + activeUntil.Value.Minute;
                save(value);
                Close();
            };
            Controls.Add(saveButton);
            ApplyTheme(this, current.DarkMode);
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

        private DateTimePicker CreateTimePicker(int minutes, Point location)
        {
            DateTimePicker picker = new DateTimePicker();
            picker.Format = DateTimePickerFormat.Custom;
            picker.CustomFormat = "HH:mm";
            picker.ShowUpDown = true;
            picker.Value = DateTime.Today.AddMinutes(Math.Max(0, Math.Min(1439, minutes)));
            picker.Location = location;
            picker.Size = new Size(162, 29);
            return picker;
        }

        private void ApplyTheme(Control parent, bool dark)
        {
            Color background = dark ? Color.FromArgb(28, 39, 33) : Color.FromArgb(247, 247, 241);
            Color foreground = dark ? Color.FromArgb(224, 235, 226) : Color.FromArgb(73, 98, 82);
            BackColor = background;
            foreach (Control control in parent.Controls)
            {
                control.ForeColor = foreground;
                if (control is TextBox || control is ComboBox || control is NumericUpDown || control is DateTimePicker)
                {
                    control.BackColor = dark ? Color.FromArgb(20, 31, 25) : Color.White;
                }
                else if (control is Button)
                {
                    control.BackColor = dark ? Color.FromArgb(49, 92, 67) : Color.FromArgb(23, 76, 61);
                    control.ForeColor = Color.White;
                }
                else if (control is Label)
                {
                    control.BackColor = background;
                }
            }
        }
    }
}