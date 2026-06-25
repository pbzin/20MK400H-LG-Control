using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LGMonitorControl {
    sealed class Profile {
        public string Name;
        public int Mode, Brightness, Contrast, Sharpness, Gamma, Temperature, Red, Green, Blue;
    }

    static class ProfileStore {
        public static readonly string DirectoryPath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string FilePath = Path.Combine(DirectoryPath, "profiles.ini");
        public static string[] Times = { "08:00", "13:00", "17:00", "22:00" };
        public static bool[] Enabled = { true, true, true, true };
        public static bool[] NightFilterEnabled = { false, false, false, false };
        public static int[] NightFilterKelvin = { 4000, 3000, 2500, 2000 };
        public static bool FilterPaused = false;
        public static Profile[] Profiles = {
            new Profile { Name = "Dia", Mode = 11, Brightness = 70, Contrast = 70, Sharpness = 50, Gamma = 30720, Temperature = 9, Red = 50, Green = 50, Blue = 50 },
            new Profile { Name = "Tarde", Mode = 11, Brightness = 60, Contrast = 65, Sharpness = 50, Gamma = 30720, Temperature = 5, Red = 50, Green = 50, Blue = 50 },
            new Profile { Name = "Noite", Mode = 11, Brightness = 35, Contrast = 60, Sharpness = 50, Gamma = 30720, Temperature = 3, Red = 50, Green = 50, Blue = 50 },
            new Profile { Name = "Madrugada", Mode = 1, Brightness = 25, Contrast = 65, Sharpness = 50, Gamma = 30720, Temperature = 3, Red = 50, Green = 50, Blue = 50 }
        };
        public static Profile Day { get { return Profiles[0]; } }
        public static Profile Night { get { return Profiles[2]; } }
        public static string DayTime { get { return Times[0]; } set { Times[0] = value; } }
        public static string NightTime { get { return Times[2]; } set { Times[2] = value; } }

        public static void Load() {
            if (!File.Exists(FilePath)) { Save(); return; }
            int loadedVersion = 0;
            foreach (string raw in File.ReadAllLines(FilePath)) {
                string line = raw.Trim(); int split = line.IndexOf('=');
                if (split < 1) continue;
                string key = line.Substring(0, split); string value = line.Substring(split + 1);
                int number;
                if (key == "Version") int.TryParse(value, out loadedVersion);
                else if (key == "FilterPaused") FilterPaused = value == "1";
                else if (key == "DayTime") Times[0] = value;
                else if (key == "NightTime") Times[2] = value;
                else if (key.StartsWith("Profile") && key.EndsWith("Time")) {
                    int idx;
                    if (int.TryParse(key.Substring(7, key.Length - 11), out idx) && idx >= 1 && idx <= Profiles.Length) Times[idx - 1] = value;
                }
                else if (key.StartsWith("Profile") && key.EndsWith("Name")) {
                    int idx;
                    if (int.TryParse(key.Substring(7, key.Length - 11), out idx) && idx >= 1 && idx <= Profiles.Length) Profiles[idx - 1].Name = value;
                }
                else if (key.StartsWith("Profile") && key.EndsWith("Enabled")) {
                    int idx, enabled;
                    if (int.TryParse(key.Substring(7, key.Length - 14), out idx) && idx >= 1 && idx <= Profiles.Length && int.TryParse(value, out enabled)) Enabled[idx - 1] = enabled != 0;
                }
                else if (key.StartsWith("Profile") && key.EndsWith("NightFilter")) {
                    int idx, enabled;
                    if (int.TryParse(key.Substring(7, key.Length - 18), out idx) && idx >= 1 && idx <= Profiles.Length && int.TryParse(value, out enabled)) NightFilterEnabled[idx - 1] = enabled != 0;
                }
                else if (key.StartsWith("Profile") && key.EndsWith("NightKelvin")) {
                    int idx, kelvin;
                    if (int.TryParse(key.Substring(7, key.Length - 18), out idx) && idx >= 1 && idx <= Profiles.Length && int.TryParse(value, out kelvin)) NightFilterKelvin[idx - 1] = kelvin;
                }
                else if (int.TryParse(value, out number)) SetNumber(key, number);
            }
            if (loadedVersion < 2) {
                foreach (Profile profile in Profiles) { profile.Temperature = MigrateTemperature(profile.Temperature); profile.Gamma = MigrateGamma(profile.Gamma); }
            }
            Save();
        }

        static int MigrateTemperature(int value) {
            if (value == 3) return 9; // versão anterior rotulava 3 como Frio
            if (value == 9) return 3; // versão anterior rotulava 9 como Quente
            if (value == 4) return 3; // primeiro formato: Quente
            if (value == 8) return 9; // primeiro formato: Frio
            return value == 5 || value == 11 ? value : 5;
        }

        static int MigrateGamma(int value) {
            if (value >= 100 && value <= 103) return 25600 + (value - 100) * 5120;
            return value;
        }

        static void SetNumber(string key, int value) {
            Profile p = null; string name = "";
            if (key.StartsWith("Day")) { p = Profiles[0]; name = key.Substring(3); }
            else if (key.StartsWith("Night")) { p = Profiles[2]; name = key.Substring(5); }
            else if (key.StartsWith("Profile")) {
                int i = 7; while (i < key.Length && char.IsDigit(key[i])) i++;
                int idx; if (int.TryParse(key.Substring(7, i - 7), out idx) && idx >= 1 && idx <= Profiles.Length) { p = Profiles[idx - 1]; name = key.Substring(i); }
            }
            if (p == null) return;
            if (name == "Mode") p.Mode = value;
            else if (name == "Brightness") p.Brightness = value;
            else if (name == "Contrast") p.Contrast = value;
            else if (name == "Sharpness") p.Sharpness = value;
            else if (name == "Gamma") p.Gamma = value;
            else if (name == "Temperature") p.Temperature = value;
            else if (name == "Red") p.Red = value;
            else if (name == "Green") p.Green = value;
            else if (name == "Blue") p.Blue = value;
        }

        public static void Save() {
            Directory.CreateDirectory(DirectoryPath);
            List<string> lines = new List<string>();
            lines.Add("Version=3");
            lines.Add("FilterPaused=" + (FilterPaused ? "1" : "0"));
            for (int i = 0; i < Profiles.Length; i++) {
                string prefix = "Profile" + (i + 1);
                lines.Add(prefix + "Name=" + Profiles[i].Name);
                lines.Add(prefix + "Time=" + Times[i]);
                lines.Add(prefix + "Enabled=" + (Enabled[i] ? "1" : "0"));
                lines.Add(prefix + "NightFilter=" + (NightFilterEnabled[i] ? "1" : "0"));
                lines.Add(prefix + "NightKelvin=" + NightFilterKelvin[i]);
                lines.Add(Line(prefix, Profiles[i]));
            }
            lines.Add("DayTime=" + Times[0]); lines.Add("NightTime=" + Times[2]);
            lines.Add(Line("Day", Profiles[0])); lines.Add(Line("Night", Profiles[2]));
            File.WriteAllLines(FilePath, lines.ToArray());
        }

        static string Line(string prefix, Profile p) {
            return prefix + "Mode=" + p.Mode + Environment.NewLine +
                   prefix + "Brightness=" + p.Brightness + Environment.NewLine +
                   prefix + "Contrast=" + p.Contrast + Environment.NewLine +
                   prefix + "Sharpness=" + p.Sharpness + Environment.NewLine +
                   prefix + "Gamma=" + p.Gamma + Environment.NewLine +
                   prefix + "Temperature=" + p.Temperature + Environment.NewLine +
                   prefix + "Red=" + p.Red + Environment.NewLine + prefix + "Green=" + p.Green + Environment.NewLine + prefix + "Blue=" + p.Blue;
        }

        public static void Apply(Profile p) {
            bool custom = p.Mode == 11;
            if (custom) { MonitorApi.Set(0x15, 11); Thread.Sleep(180); }
            // Modos predefinidos (especialmente Leitura) bloqueiam estes controles
            // no próprio firmware. Só enviamos ajustes manuais em Personalizado.
            if (custom) {
                // Primeiro Gamma e depois temperatura. Com o opcode correto 0xFE,
                // isso evita que a troca termine presa em Usuário/RGB.
                // O 20MK400H usa o opcode proprietário 0xFE para os presets.
                // O Modo 4 existe no OSD, mas não é exposto por DDC neste firmware.
                uint gamma = GammaWire(p.Gamma);
                if (gamma != uint.MaxValue) { Thread.Sleep(100); MonitorApi.Set(0xFE, gamma); }
                // Nativo é o estado intermediário necessário para sair de Usuário.
                uint temperature = TemperatureWire(p.Temperature);
                Thread.Sleep(150); MonitorApi.Set(0x14, 2); Thread.Sleep(700);
                MonitorApi.Set(0x14, temperature);
                Thread.Sleep(300); MonitorApi.Set(0x14, temperature);
                // Ganhos RGB forçam a temperatura para Usuário. Em Quente/Médio/Frio,
                // não devem ser enviados.
                if (p.Temperature == 11) {
                    Thread.Sleep(80);
                    MonitorApi.Set(0x16, (uint)p.Red); MonitorApi.Set(0x18, (uint)p.Green); MonitorApi.Set(0x1A, (uint)p.Blue);
                }
                // A troca de temperatura pode restaurar controles de imagem.
                // Por isso contraste, nitidez e brilho são sempre os últimos.
                Thread.Sleep(800); MonitorApi.Set(0x12, (uint)p.Contrast);
                Thread.Sleep(180); MonitorApi.Set(0x12, (uint)p.Contrast);
                Thread.Sleep(80); MonitorApi.Set(0x87, (uint)p.Sharpness);
                Thread.Sleep(80); MonitorApi.Set(0x10, (uint)p.Brightness);
            }
            // Temperatura e ganhos RGB fazem alguns LG retornarem automaticamente
            // ao modo Personalizado. O modo deve ser sempre o último comando.
            if (!custom) {
                Thread.Sleep(120); MonitorApi.Set(0x15, (uint)p.Mode);
                Thread.Sleep(180); MonitorApi.Set(0x10, (uint)p.Brightness);
            }
            int profileIndex = Array.IndexOf(Profiles, p);
            NightFilter.Apply(!FilterPaused && profileIndex >= 0 && NightFilterEnabled[profileIndex], profileIndex >= 0 ? NightFilterKelvin[profileIndex] : 6500);
        }

        public static uint TemperatureWire(int semanticValue) {
            // Tabela medida no scaler do LG 20MK400H (difere da tabela LG genérica).
            if (semanticValue == 3) return 5;  // Quente
            if (semanticValue == 5) return 7;  // Médio
            if (semanticValue == 9) return 8;  // Frio
            return 11;                         // Usuário
        }

        public static uint GammaWire(int semanticValue) {
            // Tabela medida e confirmada no LG 20MK400H, opcode 0xFE.
            if (semanticValue == 25600) return 0; // Modo 1
            if (semanticValue == 30720) return 1; // Modo 2
            if (semanticValue == 35840) return 2; // Modo 3
            return uint.MaxValue;                 // Modo 4: somente pelo OSD
        }

        public static Profile Current() {
            return Profiles[CurrentIndex()];
        }

        public static int CurrentIndex() {
            TimeSpan now = DateTime.Now.TimeOfDay;
            int selected = 0;
            TimeSpan selectedTime = TimeSpan.MinValue;
            for (int i = 0; i < Times.Length; i++) {
                TimeSpan t; if (!TimeSpan.TryParse(Times[i], out t)) continue;
                if (Enabled[i] && t <= now && t >= selectedTime) { selected = i; selectedTime = t; }
            }
            if (selectedTime != TimeSpan.MinValue) return selected;
            TimeSpan latest = TimeSpan.MinValue;
            for (int i = 0; i < Times.Length; i++) {
                TimeSpan t; if (Enabled[i] && TimeSpan.TryParse(Times[i], out t) && t >= latest) { selected = i; latest = t; }
            }
            return selected;
        }

        public static Profile ByKey(string key) {
            if (key.Equals("Day", StringComparison.OrdinalIgnoreCase)) return Profiles[0];
            if (key.Equals("Night", StringComparison.OrdinalIgnoreCase)) return Profiles[2];
            if (key.StartsWith("Profile", StringComparison.OrdinalIgnoreCase)) {
                int idx; if (int.TryParse(key.Substring(7), out idx) && idx >= 1 && idx <= Profiles.Length) return Profiles[idx - 1];
            }
            return Profiles[0];
            // O perfil cujo horário inicial ocorreu por último é o perfil ativo.
        }
    }

    static class MonitorApi {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct PhysicalMonitor {
            public IntPtr Handle;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Description;
        }
        delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);
        [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);
        [DllImport("dxva2.dll", SetLastError = true)] static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr monitor, out uint count);
        [DllImport("dxva2.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr monitor, uint count, [Out] PhysicalMonitor[] physical);
        [DllImport("dxva2.dll", SetLastError = true)] static extern bool DestroyPhysicalMonitors(uint count, PhysicalMonitor[] physical);
        [DllImport("dxva2.dll", SetLastError = true)] static extern bool SetVCPFeature(IntPtr monitor, byte code, uint value);

        public static int Set(byte opcode, uint value) {
            int found = 0, changed = 0;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data) {
                uint count;
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out count)) return true;
                PhysicalMonitor[] physical = new PhysicalMonitor[count];
                if (!GetPhysicalMonitorsFromHMONITOR(monitor, count, physical)) return true;
                try {
                    foreach (PhysicalMonitor item in physical) {
                        found++;
                        bool ok = false;
                        for (int attempt = 0; attempt < 3 && !ok; attempt++) {
                            ok = SetVCPFeature(item.Handle, opcode, value);
                            if (!ok) Thread.Sleep(100);
                        }
                        if (ok) changed++;
                    }
                } finally { DestroyPhysicalMonitors(count, physical); }
                return true;
            }, IntPtr.Zero);
            if (found == 0) throw new InvalidOperationException("Nenhum monitor físico foi encontrado.");
            if (changed == 0) throw new InvalidOperationException("O monitor recusou o comando 0x" + opcode.ToString("X2") + ".");
            return changed;
        }
    }

    static class NightFilter {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct GammaRamp {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)] public ushort[] Blue;
        }
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("gdi32.dll")] static extern bool SetDeviceGammaRamp(IntPtr hdc, ref GammaRamp ramp);
        [StructLayout(LayoutKind.Sequential)]
        struct ColorEffect {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)] public float[] Matrix;
        }
        [DllImport("Magnification.dll")] static extern bool MagInitialize();
        [DllImport("Magnification.dll")] static extern bool MagSetFullscreenColorEffect(ref ColorEffect effect);

        public static void Apply(bool enabled, int kelvin) {
            double green = 1.0, blue = 1.0;
            if (enabled) {
                if (kelvin <= 1500) { green = 0.36; blue = 0.05; }
                else if (kelvin <= 2000) { green = 0.52; blue = 0.12; }
                else if (kelvin <= 2500) { green = 0.65; blue = 0.24; }
                else if (kelvin <= 3000) { green = 0.76; blue = 0.39; }
                else { green = 0.88; blue = 0.66; }
            }
            ColorEffect effect = new ColorEffect { Matrix = new float[25] };
            effect.Matrix[0] = 1.0f;
            effect.Matrix[6] = (float)green;
            effect.Matrix[12] = (float)blue;
            effect.Matrix[18] = 1.0f;
            effect.Matrix[24] = 1.0f;
            if (MagInitialize() && MagSetFullscreenColorEffect(ref effect)) return;

            // Fallback para sistemas onde a API de Magnificação não está disponível.
            GammaRamp ramp = new GammaRamp { Red = new ushort[256], Green = new ushort[256], Blue = new ushort[256] };
            for (int i = 0; i < 256; i++) {
                int normal = i * 257;
                ramp.Red[i] = (ushort)normal;
                ramp.Green[i] = (ushort)Math.Min(65535, normal * green);
                ramp.Blue[i] = (ushort)Math.Min(65535, normal * blue);
            }
            IntPtr dc = GetDC(IntPtr.Zero);
            if (dc == IntPtr.Zero) return;
            try { SetDeviceGammaRamp(dc, ref ramp); } finally { ReleaseDC(IntPtr.Zero, dc); }
        }
    }

    static class StartupManager {
        const string RunPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "LGMonitorControl";

        public static bool IsEnabled() {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunPath))
                return key != null && key.GetValue(ValueName) != null;
        }

        public static void SetEnabled(bool enabled) {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunPath)) {
                if (enabled) key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\" --startup", RegistryValueKind.String);
                else key.DeleteValue(ValueName, false);
            }
        }

        public static void RefreshPathIfEnabled() {
            if (!IsEnabled()) return;
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunPath)) {
                string expected = "\"" + Application.ExecutablePath + "\" --startup";
                string current = Convert.ToString(key.GetValue(ValueName, ""));
                if (!string.Equals(current, expected, StringComparison.OrdinalIgnoreCase))
                    key.SetValue(ValueName, expected, RegistryValueKind.String);
            }
        }
    }

    public sealed class MainForm : Form {
        readonly Label status = new Label();
        readonly Label headerTitle = new Label();
        readonly Label headerSubtitle = new Label();
        readonly Color accent = Color.FromArgb(190, 0, 55);
        readonly NotifyIcon trayIcon = new NotifyIcon();
        ToolStripMenuItem pauseFilterItem;
        bool allowExit;
        DateTimePicker[] profileTime = new DateTimePicker[4];
        TextBox[] profileName = new TextBox[4];
        CheckBox[] profileEnabled = new CheckBox[4];
        CheckBox[] profileNightFilter = new CheckBox[4];
        ComboBox[] profileNightKelvin = new ComboBox[4];
        ComboBox[] profileMode = new ComboBox[4], profileGamma = new ComboBox[4], profileTemp = new ComboBox[4];
        NumericUpDown[] profileBrightness = new NumericUpDown[4], profileContrast = new NumericUpDown[4], profileSharpness = new NumericUpDown[4], profileRed = new NumericUpDown[4], profileGreen = new NumericUpDown[4], profileBlue = new NumericUpDown[4];
        DateTimePicker dayTime, nightTime;
        ComboBox dayMode, nightMode, dayGamma, nightGamma, dayTemp, nightTemp;
        NumericUpDown dayBrightness, nightBrightness, dayContrast, nightContrast, daySharpness, nightSharpness, dayRed, dayGreen, dayBlue, nightRed, nightGreen, nightBlue;
        int row;

        public MainForm(bool startHidden = false) {
            Text = "LG 20MK400H Control";
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(245, 245, 247);
            ForeColor = Color.FromArgb(35, 35, 40);
            ProfileStore.Load();
            SyncEnabledFromScheduler();
            ClientSize = new Size(690, 680);
            MinimumSize = new Size(706, 688);
            StartPosition = FormStartPosition.CenterScreen;

            headerTitle.Text = "LG 20MK400H"; headerTitle.Font = new Font("Segoe UI Semibold", 20F); headerTitle.AutoSize = true; headerTitle.Location = new Point(24, 18);
            headerSubtitle.Text = "Controle direto do monitor • protocolo write-only"; headerSubtitle.ForeColor = Color.DimGray; headerSubtitle.AutoSize = true; headerSubtitle.Location = new Point(28, 58);
            Controls.Add(headerTitle); Controls.Add(headerSubtitle);

            TabControl tabs = new TabControl { Location = new Point(22, 88), Size = new Size(646, 535), Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            TabPage profiles = new TabPage("Perfis");
            TabPage basic = new TabPage("Controles");
            TabPage color = new TabPage("Cor");
            TabPage lab = new TabPage("Laboratório");
            TabPage settings = new TabPage("Configurações");
            TabPage donate = new TabPage("Donate");
            tabs.TabPages.Add(profiles); tabs.TabPages.Add(basic); tabs.TabPages.Add(color); tabs.TabPages.Add(lab); tabs.TabPages.Add(settings); tabs.TabPages.Add(donate);
            Controls.Add(tabs);

            BuildProfiles4(profiles);
            BuildBasic(basic);
            BuildColor(color);
            BuildLab(lab);
            BuildSettings(settings);
            BuildDonate(donate);

            status.Text = "Pronto. O monitor aceita escrita, mas não informa o valor atual.";
            status.AutoEllipsis = true;
            status.ForeColor = Color.DimGray;
            status.Location = new Point(26, 640);
            status.Size = new Size(635, 24);
            status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(status);
            LinkLabel author = new LinkLabel { Text = "Desenvolvido por pbzin", AutoSize = true, Location = new Point(510, 640), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, LinkColor = accent };
            author.LinkClicked += delegate { OpenUrl("https://github.com/pbzin"); };
            Controls.Add(author); author.BringToFront();
            SetTheme(ProfileStore.CurrentIndex() >= 2);
            InitializeTray();
            FormClosing += OnFormClosing;
            if (startHidden) {
                Shown += delegate { Hide(); ShowInTaskbar = false; };
            }
        }

        void SyncEnabledFromScheduler() {
            try {
                bool[] exists = new bool[4];
                int found = 0;
                ProcessStartInfo info = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -Command \"$names=Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object TaskName -Like 'LG Monitor - Perfil *' | Select-Object -ExpandProperty TaskName; $names\"");
                info.UseShellExecute = false; info.CreateNoWindow = true; info.RedirectStandardOutput = true;
                using (Process process = Process.Start(info)) {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(5000);
                    for (int i = 0; i < 4; i++) {
                        exists[i] = output.IndexOf("LG Monitor - Perfil " + (i + 1), StringComparison.OrdinalIgnoreCase) >= 0;
                        if (exists[i]) found++;
                    }
                }
                // Se nenhuma tarefa nova existe, pode ser primeira migração; preservar o INI.
                if (found > 0) {
                    for (int i = 0; i < 4; i++) ProfileStore.Enabled[i] = exists[i];
                    ProfileStore.Save();
                } else {
                    ProcessStartInfo legacyInfo = new ProcessStartInfo("powershell.exe",
                        "-NoProfile -Command \"$names=Get-ScheduledTask -ErrorAction SilentlyContinue | Where-Object TaskName -In 'LG Monitor - Dia','LG Monitor - Noite' | Select-Object -ExpandProperty TaskName; $names\"");
                    legacyInfo.UseShellExecute = false; legacyInfo.CreateNoWindow = true; legacyInfo.RedirectStandardOutput = true;
                    using (Process legacy = Process.Start(legacyInfo)) {
                        string output = legacy.StandardOutput.ReadToEnd(); legacy.WaitForExit(5000);
                        bool day = output.IndexOf("LG Monitor - Dia", StringComparison.OrdinalIgnoreCase) >= 0;
                        bool night = output.IndexOf("LG Monitor - Noite", StringComparison.OrdinalIgnoreCase) >= 0;
                        if (day || night) {
                            ProfileStore.Enabled[0] = day; ProfileStore.Enabled[1] = false;
                            ProfileStore.Enabled[2] = night; ProfileStore.Enabled[3] = false;
                            ProfileStore.Save();
                        }
                    }
                }
            } catch {
                // Sem permissão ou serviço do Agendador indisponível: mantém profiles.ini.
            }
        }

        void InitializeTray() {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem open = new ToolStripMenuItem("Abrir LG Monitor Control");
            ToolStripMenuItem exit = new ToolStripMenuItem("Sair do aplicativo");
            pauseFilterItem = new ToolStripMenuItem();
            UpdatePauseMenuText();
            open.Click += delegate { RestoreFromTray(); };
            pauseFilterItem.Click += delegate { ToggleFilterPause(); };
            exit.Click += delegate { ExitApplication(); };
            menu.Items.Add(open); menu.Items.Add(pauseFilterItem); menu.Items.Add(new ToolStripSeparator());
            for (int i = 0; i < ProfileStore.Profiles.Length; i++) {
                int idx = i;
                ToolStripMenuItem item = new ToolStripMenuItem("Aplicar perfil " + ProfileStore.Profiles[i].Name);
                item.Click += delegate { ApplyProfile(ProfileStore.Profiles[idx]); };
                menu.Items.Add(item);
            }
            menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(exit);
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.Text = "LG 20MK400H Control";
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreFromTray(); };
        }

        void UpdatePauseMenuText() {
            if (pauseFilterItem != null)
                pauseFilterItem.Text = ProfileStore.FilterPaused ? "Continuar filtro noturno" : "Pausar filtro noturno";
        }

        void ToggleFilterPause() {
            ProfileStore.FilterPaused = !ProfileStore.FilterPaused;
            ProfileStore.Save();
            if (ProfileStore.FilterPaused) {
                NightFilter.Apply(false, 6500);
                status.Text = "Filtro noturno pausado em todos os perfis.";
            } else {
                int idx = ProfileStore.CurrentIndex();
                NightFilter.Apply(ProfileStore.NightFilterEnabled[idx], ProfileStore.NightFilterKelvin[idx]);
                status.Text = "Filtro noturno retomado conforme o perfil atual.";
            }
            UpdatePauseMenuText();
        }

        void OnFormClosing(object sender, FormClosingEventArgs e) {
            if (!allowExit && e.CloseReason == CloseReason.UserClosing) {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;
                trayIcon.ShowBalloonTip(1800, "LG Monitor Control", "O aplicativo continua ativo na bandeja.", ToolTipIcon.Info);
            }
        }

        void RestoreFromTray() {
            ShowInTaskbar = true; Show(); WindowState = FormWindowState.Normal; Activate(); BringToFront();
        }

        void ExitApplication() {
            allowExit = true; trayIcon.Visible = false; trayIcon.Dispose(); Application.Exit();
        }

        void BuildProfiles4(Control page) {
            Panel panel = new Panel { Location = new Point(0, 0), Size = new Size(625, 475), AutoScroll = true };
            page.Controls.Add(panel);
            for (int i = 0; i < ProfileStore.Profiles.Length; i++) {
                Profile p = ProfileStore.Profiles[i];
                GroupBox box = new GroupBox { Text = "Perfil " + (i + 1), Location = new Point(14, 8 + i * 150), Size = new Size(580, 142), BackColor = Color.White };
                panel.Controls.Add(box);
                profileName[i] = new TextBox { Text = p.Name, Width = 105 };
                profileEnabled[i] = new CheckBox { Text = "Ativo", Checked = ProfileStore.Enabled[i], AutoSize = true, Location = new Point(492, 22) };
                box.Controls.Add(profileEnabled[i]);
                profileNightFilter[i] = new CheckBox { Text = "Filtro", Checked = ProfileStore.NightFilterEnabled[i], AutoSize = true, Location = new Point(450, 50) };
                profileNightKelvin[i] = NewCombo(new object[] { "4000 K", "3000 K", "2500 K", "2000 K", "1500 K" });
                profileNightKelvin[i].Width = 72; profileNightKelvin[i].Location = new Point(492, 47);
                int[] filterKelvins = { 4000, 3000, 2500, 2000, 1500 };
                int filterSelection = Array.IndexOf(filterKelvins, ProfileStore.NightFilterKelvin[i]);
                profileNightKelvin[i].SelectedIndex = filterSelection < 0 ? 1 : filterSelection;
                profileNightKelvin[i].Enabled = profileNightFilter[i].Checked;
                int filterIdx = i;
                profileNightFilter[i].CheckedChanged += delegate { profileNightKelvin[filterIdx].Enabled = profileNightFilter[filterIdx].Checked; };
                box.Controls.Add(profileNightFilter[i]); box.Controls.Add(profileNightKelvin[i]);
                profileTime[i] = TimePicker(ProfileStore.Times[i]);
                profileMode[i] = ProfileMode(p.Mode); profileMode[i].Width = 125;
                profileTemp[i] = ProfileTemp(p.Temperature); profileTemp[i].Width = 105;
                profileGamma[i] = ProfileGamma(p.Gamma); profileGamma[i].Width = 92;
                profileBrightness[i] = Number(p.Brightness); profileContrast[i] = Number(p.Contrast); profileSharpness[i] = Number(p.Sharpness);
                profileRed[i] = Number(p.Red); profileGreen[i] = Number(p.Green); profileBlue[i] = Number(p.Blue);
                AddSmall(box, "Nome", profileName[i], 10, 18); AddSmall(box, "Começa", profileTime[i], 125, 18);
                AddSmall(box, "Modo", profileMode[i], 240, 18); AddSmall(box, "Temperatura", profileTemp[i], 375, 18);
                profileBrightness[i].Width = profileContrast[i].Width = profileSharpness[i].Width = profileRed[i].Width = profileGreen[i].Width = profileBlue[i].Width = 48;
                AddSmall(box, "Brilho", profileBrightness[i], 10, 72); AddSmall(box, "Contr.", profileContrast[i], 67, 72);
                AddSmall(box, "Nitidez", profileSharpness[i], 124, 72); AddSmall(box, "Gamma", profileGamma[i], 181, 72);
                AddSmall(box, "R", profileRed[i], 282, 72); AddSmall(box, "G", profileGreen[i], 339, 72); AddSmall(box, "B", profileBlue[i], 396, 72);
                int idx = i;
                Button apply = ButtonOf("Aplicar", 492, 96); apply.Size = new Size(70, 28);
                apply.Click += delegate { SaveProfileValues4(); ApplyProfile(ProfileStore.Profiles[idx]); };
                box.Controls.Add(apply);
                profileMode[i].SelectedIndexChanged += delegate { UpdateProfileAvailability(profileMode[idx], profileContrast[idx], profileSharpness[idx], profileGamma[idx], profileTemp[idx], profileRed[idx], profileGreen[idx], profileBlue[idx]); UpdateRgbAvailability(profileMode[idx], profileTemp[idx], profileRed[idx], profileGreen[idx], profileBlue[idx]); };
                profileTemp[i].SelectedIndexChanged += delegate { UpdateRgbAvailability(profileMode[idx], profileTemp[idx], profileRed[idx], profileGreen[idx], profileBlue[idx]); };
                UpdateProfileAvailability(profileMode[i], profileContrast[i], profileSharpness[i], profileGamma[i], profileTemp[i], profileRed[i], profileGreen[i], profileBlue[i]);
                UpdateRgbAvailability(profileMode[i], profileTemp[i], profileRed[i], profileGreen[i], profileBlue[i]);
            }
            Button save = ButtonOf("Salvar e agendar", 18, 485); save.Size = new Size(180, 36); save.BackColor = Color.FromArgb(190, 0, 55); save.ForeColor = Color.White;
            save.Click += delegate { SaveProfilesAndSchedule4(); };
            page.Controls.Add(save);
            CheckBox allFilters = new CheckBox { Text = "Filtro em todos", AutoSize = true, Location = new Point(215, 494) };
            allFilters.CheckedChanged += delegate { for (int i = 0; i < profileNightFilter.Length; i++) profileNightFilter[i].Checked = allFilters.Checked; };
            page.Controls.Add(allFilters);
            page.Controls.Add(new Label { Text = "Ativo = último horário iniciado.", AutoSize = true, ForeColor = Color.DimGray, Location = new Point(340, 495) });
        }

        void BuildProfiles(Control page) {
            GroupBox day = new GroupBox { Text = "☀ Perfil Dia — tema claro", Location = new Point(14, 12), Size = new Size(600, 190), BackColor = Color.White };
            GroupBox night = new GroupBox { Text = "● Perfil Noite — tema escuro", Location = new Point(14, 210), Size = new Size(600, 190), BackColor = Color.FromArgb(42, 43, 48), ForeColor = Color.White };
            page.Controls.Add(day); page.Controls.Add(night);

            dayTime = TimePicker(ProfileStore.DayTime); nightTime = TimePicker(ProfileStore.NightTime);
            dayMode = ProfileMode(ProfileStore.Day.Mode); nightMode = ProfileMode(ProfileStore.Night.Mode);
            dayGamma = ProfileGamma(ProfileStore.Day.Gamma); nightGamma = ProfileGamma(ProfileStore.Night.Gamma);
            dayTemp = ProfileTemp(ProfileStore.Day.Temperature); nightTemp = ProfileTemp(ProfileStore.Night.Temperature);
            dayBrightness = Number(ProfileStore.Day.Brightness); nightBrightness = Number(ProfileStore.Night.Brightness);
            dayContrast = Number(ProfileStore.Day.Contrast); nightContrast = Number(ProfileStore.Night.Contrast);
            daySharpness = Number(ProfileStore.Day.Sharpness); nightSharpness = Number(ProfileStore.Night.Sharpness);
            dayRed = Number(ProfileStore.Day.Red); dayGreen = Number(ProfileStore.Day.Green); dayBlue = Number(ProfileStore.Day.Blue);
            nightRed = Number(ProfileStore.Night.Red); nightGreen = Number(ProfileStore.Night.Green); nightBlue = Number(ProfileStore.Night.Blue);
            FillProfileBox(day, dayTime, dayMode, dayTemp, dayBrightness, dayContrast, daySharpness, dayGamma, dayRed, dayGreen, dayBlue);
            FillProfileBox(night, nightTime, nightMode, nightTemp, nightBrightness, nightContrast, nightSharpness, nightGamma, nightRed, nightGreen, nightBlue);
            dayMode.SelectedIndexChanged += delegate { UpdateProfileAvailability(dayMode, dayContrast, daySharpness, dayGamma, dayTemp, dayRed, dayGreen, dayBlue); };
            nightMode.SelectedIndexChanged += delegate { UpdateProfileAvailability(nightMode, nightContrast, nightSharpness, nightGamma, nightTemp, nightRed, nightGreen, nightBlue); };
            dayMode.SelectedIndexChanged += delegate { UpdateRgbAvailability(dayMode, dayTemp, dayRed, dayGreen, dayBlue); };
            nightMode.SelectedIndexChanged += delegate { UpdateRgbAvailability(nightMode, nightTemp, nightRed, nightGreen, nightBlue); };
            dayTemp.SelectedIndexChanged += delegate { UpdateRgbAvailability(dayMode, dayTemp, dayRed, dayGreen, dayBlue); };
            nightTemp.SelectedIndexChanged += delegate { UpdateRgbAvailability(nightMode, nightTemp, nightRed, nightGreen, nightBlue); };
            UpdateProfileAvailability(dayMode, dayContrast, daySharpness, dayGamma, dayTemp, dayRed, dayGreen, dayBlue);
            UpdateProfileAvailability(nightMode, nightContrast, nightSharpness, nightGamma, nightTemp, nightRed, nightGreen, nightBlue);
            UpdateRgbAvailability(dayMode, dayTemp, dayRed, dayGreen, dayBlue);
            UpdateRgbAvailability(nightMode, nightTemp, nightRed, nightGreen, nightBlue);

            Button save = ButtonOf("Salvar e agendar", 18, 420); save.Size = new Size(180, 36); save.BackColor = Color.FromArgb(190, 0, 55); save.ForeColor = Color.White;
            Button applyDay = ButtonOf("Aplicar Dia agora", 215, 420); applyDay.Size = new Size(165, 36);
            Button applyNight = ButtonOf("Aplicar Noite agora", 395, 420); applyNight.Size = new Size(180, 36);
            save.Click += delegate { SaveProfilesAndSchedule(); };
            applyDay.Click += delegate { SaveProfileValues(); ApplyProfile(ProfileStore.Day); };
            applyNight.Click += delegate { SaveProfileValues(); ApplyProfile(ProfileStore.Night); };
            page.Controls.Add(save); page.Controls.Add(applyDay); page.Controls.Add(applyNight);
            page.Controls.Add(new Label { Text = "O período noturno pode atravessar a meia-noite normalmente.", AutoSize = true, ForeColor = Color.DimGray, Location = new Point(20, 470) });
        }

        void UpdateProfileAvailability(ComboBox mode, params Control[] manualControls) {
            bool custom = mode.SelectedIndex == 0;
            foreach (Control control in manualControls) control.Enabled = custom;
        }

        void UpdateRgbAvailability(ComboBox mode, ComboBox temperature, params Control[] rgbControls) {
            // Permite preparar/salvar os ganhos em qualquer temperatura do perfil
            // Personalizado. Eles só são enviados quando Temperatura=Usuário.
            bool enabled = mode.SelectedIndex == 0;
            foreach (Control control in rgbControls) control.Enabled = enabled;
        }

        void FillProfileBox(Control box, DateTimePicker time, ComboBox mode, ComboBox temp, NumericUpDown bright, NumericUpDown contrast, NumericUpDown sharpness, ComboBox gamma, NumericUpDown red, NumericUpDown green, NumericUpDown blue) {
            AddSmall(box, "Começa", time, 14, 28); AddSmall(box, "Modo", mode, 205, 28); AddSmall(box, "Temperatura", temp, 396, 28);
            bright.Width = contrast.Width = sharpness.Width = red.Width = green.Width = blue.Width = 60; gamma.Width = 82;
            AddSmall(box, "Brilho", bright, 14, 92); AddSmall(box, "Contraste", contrast, 92, 92); AddSmall(box, "Nitidez", sharpness, 178, 92);
            AddSmall(box, "Gamma", gamma, 256, 92); AddSmall(box, "R", red, 355, 92); AddSmall(box, "G", green, 430, 92); AddSmall(box, "B", blue, 505, 92);
        }

        void AddSmall(Control parent, string label, Control control, int x, int y) {
            parent.Controls.Add(new Label { Text = label, AutoSize = true, Location = new Point(x, y) });
            control.BackColor = Color.White;
            control.ForeColor = Color.FromArgb(30, 30, 34);
            control.Location = new Point(x, y + 22); parent.Controls.Add(control);
        }

        DateTimePicker TimePicker(string value) {
            DateTimePicker picker = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 105 };
            TimeSpan parsed; if (TimeSpan.TryParse(value, out parsed)) picker.Value = DateTime.Today.Add(parsed);
            return picker;
        }

        NumericUpDown Number(int value) { return new NumericUpDown { Minimum = 0, Maximum = 100, Value = value, Width = 75 }; }

        ComboBox ProfileMode(int value) {
            ComboBox box = NewCombo(new object[] { "Personalizado", "Leitura", "Foto", "Cinema", "Daltonismo", "Jogo" }); box.Width = 165;
            int[] values = { 11, 1, 16, 17, 32, 48 }; int index = Array.IndexOf(values, value); box.SelectedIndex = index < 0 ? 0 : index; return box;
        }

        ComboBox ProfileTemp(int value) {
            ComboBox box = NewCombo(new object[] { "Quente", "Médio", "Frio", "Usuário" }); box.Width = 165;
            int[] values = { 3, 5, 9, 11 }; int index = Array.IndexOf(values, value); box.SelectedIndex = index < 0 ? 1 : index; return box;
        }

        ComboBox ProfileGamma(int value) {
            ComboBox box = NewCombo(new object[] { "Modo 1", "Modo 2", "Modo 3", "Modo 4 (manual)" }); box.Width = 145;
            int[] values = { 25600, 30720, 35840, 40960 }; int index = Array.IndexOf(values, value); box.SelectedIndex = index < 0 ? 1 : index; return box;
        }

        void SaveProfileValues4() {
            int[] modes = { 11, 1, 16, 17, 32, 48 }, temps = { 3, 5, 9, 11 }, gammas = { 25600, 30720, 35840, 40960 };
            int[] kelvins = { 4000, 3000, 2500, 2000, 1500 };
            for (int i = 0; i < ProfileStore.Profiles.Length; i++) {
                Profile p = ProfileStore.Profiles[i];
                p.Name = string.IsNullOrWhiteSpace(profileName[i].Text) ? "Perfil " + (i + 1) : profileName[i].Text.Trim();
                ProfileStore.Enabled[i] = profileEnabled[i].Checked;
                ProfileStore.NightFilterEnabled[i] = profileNightFilter[i].Checked;
                ProfileStore.NightFilterKelvin[i] = kelvins[profileNightKelvin[i].SelectedIndex];
                ProfileStore.Times[i] = profileTime[i].Value.ToString("HH:mm");
                p.Mode = modes[profileMode[i].SelectedIndex]; p.Temperature = temps[profileTemp[i].SelectedIndex]; p.Gamma = gammas[profileGamma[i].SelectedIndex];
                p.Brightness = (int)profileBrightness[i].Value; p.Contrast = (int)profileContrast[i].Value; p.Sharpness = (int)profileSharpness[i].Value;
                p.Red = (int)profileRed[i].Value; p.Green = (int)profileGreen[i].Value; p.Blue = (int)profileBlue[i].Value;
            }
            ProfileStore.Save();
        }

        void SaveProfilesAndSchedule4() {
            try {
                SaveProfileValues4();
                string script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install-lg-monitor-schedule.ps1");
                string args = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"";
                for (int i = 0; i < 4; i++) {
                    args += " -Profile" + (i + 1) + "Time \"" + ProfileStore.Times[i] + "\"";
                    args += " -Profile" + (i + 1) + "Enabled " + (ProfileStore.Enabled[i] ? "1" : "0");
                }
                ProcessStartInfo info = new ProcessStartInfo("powershell.exe", args);
                info.Verb = "runas"; info.UseShellExecute = true;
                Process process = Process.Start(info); process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("O Agendador retornou " + process.ExitCode + ".");
                int activeIndex = ProfileStore.CurrentIndex();
                ProfileStore.Apply(ProfileStore.Profiles[activeIndex]);
                SetTheme(activeIndex >= 2);
                status.ForeColor = Color.FromArgb(25, 115, 55);
                status.Text = "✓ Quatro perfis salvos e agendados.";
            } catch (Exception ex) { status.ForeColor = accent; status.Text = "Falha ao agendar: " + ex.Message; }
        }

        void SaveProfileValues() {
            int[] modes = { 11, 1, 16, 17, 32, 48 }, temps = { 3, 5, 9, 11 }, gammas = { 25600, 30720, 35840, 40960 };
            ProfileStore.DayTime = dayTime.Value.ToString("HH:mm"); ProfileStore.NightTime = nightTime.Value.ToString("HH:mm");
            ProfileStore.Day.Mode = modes[dayMode.SelectedIndex]; ProfileStore.Night.Mode = modes[nightMode.SelectedIndex];
            ProfileStore.Day.Temperature = temps[dayTemp.SelectedIndex]; ProfileStore.Night.Temperature = temps[nightTemp.SelectedIndex];
            ProfileStore.Day.Gamma = gammas[dayGamma.SelectedIndex]; ProfileStore.Night.Gamma = gammas[nightGamma.SelectedIndex];
            ProfileStore.Day.Brightness = (int)dayBrightness.Value; ProfileStore.Night.Brightness = (int)nightBrightness.Value;
            ProfileStore.Day.Contrast = (int)dayContrast.Value; ProfileStore.Night.Contrast = (int)nightContrast.Value;
            ProfileStore.Day.Sharpness = (int)daySharpness.Value; ProfileStore.Night.Sharpness = (int)nightSharpness.Value;
            ProfileStore.Day.Red = (int)dayRed.Value; ProfileStore.Day.Green = (int)dayGreen.Value; ProfileStore.Day.Blue = (int)dayBlue.Value;
            ProfileStore.Night.Red = (int)nightRed.Value; ProfileStore.Night.Green = (int)nightGreen.Value; ProfileStore.Night.Blue = (int)nightBlue.Value;
            ProfileStore.Save();
        }

        void ApplyProfile(Profile profile) {
            try { ProfileStore.Apply(profile); status.ForeColor = Color.FromArgb(25, 115, 55); status.Text = "✓ Perfil " + profile.Name + " aplicado."; SetTheme(Array.IndexOf(ProfileStore.Profiles, profile) >= 2); }
            catch (Exception ex) { status.ForeColor = accent; status.Text = "Falha: " + ex.Message; }
        }

        void SaveProfilesAndSchedule() {
            try {
                SaveProfileValues();
                string script = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "install-lg-monitor-schedule.ps1");
                ProcessStartInfo info = new ProcessStartInfo("powershell.exe", "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\" -DayTime \"" + ProfileStore.DayTime + "\" -NightTime \"" + ProfileStore.NightTime + "\"");
                info.Verb = "runas"; info.UseShellExecute = true; Process process = Process.Start(info); process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("O Agendador retornou " + process.ExitCode + ".");
                Profile active = ProfileStore.Current();
                ProfileStore.Apply(active);
                SetTheme(active == ProfileStore.Night);
                status.ForeColor = Color.FromArgb(25, 115, 55); status.Text = "✓ Perfis salvos: Dia " + ProfileStore.DayTime + " • Noite " + ProfileStore.NightTime;
            } catch (Exception ex) { status.ForeColor = accent; status.Text = "Falha ao agendar: " + ex.Message; }
        }

        void SetTheme(bool dark) {
            BackColor = dark ? Color.FromArgb(28, 29, 33) : Color.FromArgb(245, 245, 247);
            // As páginas e campos continuam claros; manter a cor-base escura evita
            // texto branco sobre ComboBox/NumericUpDown brancos.
            ForeColor = Color.FromArgb(35, 35, 40);
            headerTitle.ForeColor = dark ? Color.WhiteSmoke : Color.FromArgb(35, 35, 40);
            headerSubtitle.ForeColor = dark ? Color.Silver : Color.DimGray;
        }

        void BuildBasic(Control page) {
            ComboBox modes = NewCombo(new object[] { "Personalizado", "Leitura", "Foto", "Cinema", "Daltonismo", "Jogo" });
            int[] modeValues = { 11, 1, 16, 17, 32, 48 };
            AddComboRow(page, "Modo de imagem", modes, delegate { Apply(0x15, (uint)modeValues[modes.SelectedIndex], "Modo: " + modes.Text); });
            AddSlider(page, "Brilho", 0x10, 0, 100, 50);
            AddSlider(page, "Contraste", 0x12, 0, 100, 70);
            AddSlider(page, "Nitidez (somente Personalizado)", 0x87, 0, 100, 50);

            GroupBox presets = new GroupBox { Text = "Atalhos", Location = new Point(18, 300), Size = new Size(590, 100), Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
            Button day = ButtonOf("Dia / Personalizado", 18, 30);
            Button night = ButtonOf("Noite / Leitura", 205, 30);
            day.Click += delegate { Apply(0x15, 11, "Modo Personalizado aplicado"); };
            night.Click += delegate { Apply(0x15, 1, "Modo Leitura aplicado"); };
            presets.Controls.Add(day); presets.Controls.Add(night); page.Controls.Add(presets);
        }

        void BuildColor(Control page) {
            row = 0;
            ComboBox temperature = NewCombo(new object[] { "Quente (~5000 K)", "Médio (6500 K)", "Frio (~9300 K)", "Usuário" });
            int[] tempValues = { 3, 5, 9, 11 };
            AddComboRow(page, "Temperatura de cor", temperature, delegate { ApplyTemperature(ProfileStore.TemperatureWire(tempValues[temperature.SelectedIndex]), temperature.Text); });
            ComboBox gamma = NewCombo(new object[] { "Modo 1", "Modo 2", "Modo 3", "Modo 4 (manual)" });
            int[] gammaValues = { 25600, 30720, 35840, 40960 };
            AddComboRow(page, "Gamma", gamma, delegate {
                uint value = ProfileStore.GammaWire(gammaValues[gamma.SelectedIndex]);
                if (value == uint.MaxValue) { status.ForeColor = accent; status.Text = "Gama 4 só pode ser escolhido no menu físico deste monitor."; }
                else Apply(0xFE, value, "Gamma: " + gamma.Text);
            });
            AddSlider(page, "Vermelho", 0x16, 0, 100, 50);
            AddSlider(page, "Verde", 0x18, 0, 100, 50);
            AddSlider(page, "Azul", 0x1A, 0, 100, 50);
            Label note = new Label { Text = "Temperatura e Gamma só respondem no Personalizado. Gama 4 é somente manual.", AutoSize = true, ForeColor = Color.DimGray, Location = new Point(20, 375) };
            page.Controls.Add(note);
        }

        void BuildSettings(Control page) {
            page.Controls.Add(new Label { Text = "Comportamento ao fechar", Font = new Font("Segoe UI Semibold", 13F), AutoSize = true, Location = new Point(22, 24) });
            page.Controls.Add(new Label {
                Text = "Ao clicar no X, a janela é escondida e o aplicativo continua ativo\nna bandeja do Windows, próximo ao relógio.",
                AutoSize = true, ForeColor = Color.DimGray, Location = new Point(24, 62)
            });
            page.Controls.Add(new Label {
                Text = "• Duplo clique no ícone: reabre a janela\n• Clique direito: perfis rápidos e opção de saída\n• As tarefas Dia/Noite continuam funcionando mesmo se o aplicativo sair",
                AutoSize = true, Location = new Point(35, 120)
            });
            CheckBox startup = new CheckBox { Text = "Iniciar automaticamente com o Windows", AutoSize = true, Location = new Point(25, 198), Checked = StartupManager.IsEnabled() };
            startup.CheckedChanged += delegate {
                try {
                    StartupManager.SetEnabled(startup.Checked);
                    if (StartupManager.IsEnabled() != startup.Checked) throw new InvalidOperationException("O Windows não confirmou a alteração.");
                    status.Text = startup.Checked ? "Inicialização automática ativada pelo programa." : "Inicialização automática desativada pelo programa.";
                }
                catch (Exception ex) { startup.Checked = !startup.Checked; status.Text = "Falha na inicialização automática: " + ex.Message; }
            };
            Button pause = ButtonOf(ProfileStore.FilterPaused ? "Continuar filtro" : "Pausar filtro", 24, 240); pause.Size = new Size(190, 38);
            pause.Click += delegate { ToggleFilterPause(); pause.Text = ProfileStore.FilterPaused ? "Continuar filtro" : "Pausar filtro"; };
            Button hide = ButtonOf("Esconder na bandeja", 235, 240); hide.Size = new Size(190, 38);
            hide.Click += delegate { Hide(); ShowInTaskbar = false; };
            Button exit = ButtonOf("Sair do aplicativo", 446, 240); exit.Size = new Size(165, 38); exit.BackColor = Color.FromArgb(190, 0, 55); exit.ForeColor = Color.White;
            exit.Click += delegate { ExitApplication(); };
            page.Controls.Add(startup); page.Controls.Add(pause); page.Controls.Add(hide); page.Controls.Add(exit);
        }

        void BuildDonate(Control page) {
            ScrollableControl scrollable = page as ScrollableControl;
            if (scrollable != null) scrollable.AutoScroll = true;
            page.Controls.Add(new Label { Text = "💖 Support My Work", Font = new Font("Segoe UI Semibold", 18F), AutoSize = true, Location = new Point(185, 18) });

            Button coffee = ButtonOf("Buy Me a Coffee", 75, 65); coffee.Size = new Size(210, 38);
            Button sponsor = ButtonOf("GitHub Sponsors", 315, 65); sponsor.Size = new Size(210, 38);
            coffee.BackColor = Color.FromArgb(255, 221, 0);
            sponsor.BackColor = Color.FromArgb(234, 74, 170); sponsor.ForeColor = Color.White;
            coffee.Click += delegate { OpenUrl("https://buymeacoffee.com/pbzin"); };
            sponsor.Click += delegate { OpenUrl("https://github.com/sponsors/pbzin"); };
            page.Controls.Add(coffee); page.Controls.Add(sponsor);

            AddDonationBlock(page, "Pix ⚡", "5198a8b3-6b89-4475-aec1-5adcfcfd12cf", null, 125);
            AddDonationBlock(page, "Bitcoin", "1GkpDZDHYov7WZLs54Nv19f2KUoZPcACs2", "bitcoin-qr.png", "https://raw.githubusercontent.com/pbzin/pbzin/main/assets/bitcoin-qr.png", 205);
            AddDonationBlock(page, "Monero", "45YtYmxUeXeFdokKPG1KWtMFLByS8nwmtiJjEiZ9LfbkNaSUCvyWWAx3VmtDKKkxPJFdQLSXxodRWMt7EBu5TmA3Qi9dgwT", "monero-qr.png", "https://raw.githubusercontent.com/pbzin/pbzin/main/assets/monero-qr.png", 390);
        }

        void AddDonationBlock(Control page, string title, string address, string imageName, int y) {
            AddDonationBlock(page, title, address, imageName, null, y);
        }

        void AddDonationBlock(Control page, string title, string address, string imageName, string imageUrl, int y) {
            Label heading = new Label { Text = title, Font = new Font("Segoe UI Semibold", 12F), AutoSize = true, Location = new Point(24, y) };
            TextBox value = new TextBox { Text = address, ReadOnly = true, Location = new Point(24, y + 30), Width = imageName == null ? 540 : 370 };
            Button copy = ButtonOf("Copiar", imageName == null ? 475 : 405, y + 27); copy.Size = new Size(90, 29);
            copy.Click += delegate { try { Clipboard.SetText(address); status.Text = title + " copiado."; } catch { } };
            page.Controls.Add(heading); page.Controls.Add(value); page.Controls.Add(copy);
            if (imageName != null) {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", imageName);
                if (!File.Exists(path) && !string.IsNullOrEmpty(imageUrl)) {
                    try {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                        using (WebClient client = new WebClient()) client.DownloadFile(imageUrl, path);
                    } catch { }
                }
                PictureBox qr = new PictureBox { Location = new Point(430, y - 5), Size = new Size(135, 135), SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
                if (File.Exists(path)) {
                    using (Image source = Image.FromFile(path)) qr.Image = new Bitmap(source);
                } else {
                    qr.BackColor = Color.WhiteSmoke;
                }
                page.Controls.Add(qr);
            }
        }

        void OpenUrl(string url) {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (Exception ex) { status.Text = "Não foi possível abrir o link: " + ex.Message; }
        }

        void BuildLab(Control page) {
            row = 0;
            Label warning = new Label {
                Text = "Códigos encontrados no software LG, mas ainda não confirmados neste modelo.\nUse um controle por vez; nenhum item abaixo executa reset, energia ou troca de entrada.",
                ForeColor = Color.FromArgb(130, 75, 0), Location = new Point(18, 16), Size = new Size(580, 48)
            };
            page.Controls.Add(warning);

            ComboBox black = NewCombo(new object[] { "Baixo (1)", "Alto (2)" });
            black.Location = new Point(220, 82); page.Controls.Add(black);
            Button blackApply = ButtonOf("Aplicar Black Level", 400, 80);
            blackApply.Click += delegate { Apply(0x92, (uint)(black.SelectedIndex + 1), "Black Level experimental: " + black.Text); };
            page.Controls.Add(blackApply);
            page.Controls.Add(new Label { Text = "Nível de preto HDMI", AutoSize = true, Location = new Point(20, 86) });

            page.Controls.Add(new Label { Text = "Comando bruto", Font = new Font(Font, FontStyle.Bold), AutoSize = true, Location = new Point(20, 150) });
            NumericUpDown opcode = new NumericUpDown { Minimum = 0, Maximum = 255, Hexadecimal = true, Value = 0xFC, Location = new Point(220, 146), Width = 90 };
            NumericUpDown value = new NumericUpDown { Minimum = 0, Maximum = 65535, Value = 0, Location = new Point(320, 146), Width = 90 };
            Button raw = ButtonOf("Enviar", 430, 144);
            raw.Click += delegate { Apply((byte)opcode.Value, (uint)value.Value, "Bruto 0x" + ((byte)opcode.Value).ToString("X2") + " = " + value.Value); };
            page.Controls.Add(opcode); page.Controls.Add(value); page.Controls.Add(raw);

            TextBox reference = new TextBox {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
                Location = new Point(20, 205), Size = new Size(570, 215), BackColor = Color.White,
                Text = "Mapa extraído do OnScreen Control 9.47:\r\n" +
                       "0x10 Brilho | 0x12 Contraste | 0x15 Modo de imagem\r\n" +
                       "0x16/0x18/0x1A Ganhos RGB | 0x69 Temperatura LG\r\n" +
                       "0x6C/0x6E/0x70 Nível preto RGB | 0x72 Gamma\r\n" +
                       "0x87 Nitidez | 0x92 Black Level HDMI\r\n" +
                       "0xF1 Brilho F-Engine | 0xF2 ACE/DFC candidato\r\n" +
                       "0xF3 RCM candidato | 0xFC seletor F-Engine/Super Resolution candidato\r\n\r\n" +
                       "Super Resolution+ e DFC permanecem candidatos porque o monitor não responde a consultas GET."
            };
            page.Controls.Add(reference);
        }

        ComboBox NewCombo(object[] values) {
            ComboBox box = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 205 };
            box.Items.AddRange(values); box.SelectedIndex = 0; return box;
        }

        void AddComboRow(Control page, string name, ComboBox box, Action action) {
            int y = 24 + row * 68; row++;
            page.Controls.Add(new Label { Text = name, AutoSize = true, Location = new Point(20, y + 7) });
            box.Location = new Point(220, y); page.Controls.Add(box);
            Button apply = ButtonOf("Aplicar", 445, y - 2); apply.Click += delegate { action(); }; page.Controls.Add(apply);
        }

        void AddSlider(Control page, string name, byte opcode, int min, int max, int initial, int wireOffset = 0) {
            int y = 24 + row * 68; row++;
            Label value = new Label { Text = initial.ToString(), AutoSize = false, TextAlign = ContentAlignment.MiddleRight, Location = new Point(548, y + 4), Size = new Size(40, 25) };
            TrackBar bar = new TrackBar { Minimum = min, Maximum = max, Value = initial, TickFrequency = 10, SmallChange = 1, LargeChange = 5, Location = new Point(210, y), Size = new Size(335, 45) };
            page.Controls.Add(new Label { Text = name, AutoSize = true, Location = new Point(20, y + 9) });
            bar.Scroll += delegate { value.Text = bar.Value.ToString(); Apply(opcode, (uint)(bar.Value + wireOffset), name + ": " + bar.Value); };
            bar.MouseUp += delegate { Apply(opcode, (uint)(bar.Value + wireOffset), name + ": " + bar.Value); };
            bar.KeyUp += delegate(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right || e.KeyCode == Keys.Home || e.KeyCode == Keys.End) Apply(opcode, (uint)(bar.Value + wireOffset), name + ": " + bar.Value); };
            page.Controls.Add(bar); page.Controls.Add(value);
        }

        Button ButtonOf(string text, int x, int y) {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(165, 32), FlatStyle = FlatStyle.Flat, BackColor = Color.White };
        }

        void ApplyTemperature(uint value, string description) {
            try {
                // O scaler exige passar por Nativo para sair de Usuário.
                MonitorApi.Set(0x14, 2); Thread.Sleep(700);
                int changed = MonitorApi.Set(0x14, value);
                Thread.Sleep(300); MonitorApi.Set(0x14, value);
                status.ForeColor = Color.FromArgb(25, 115, 55);
                status.Text = "✓ Temperatura: " + description + " — enviada para " + changed + " monitor(es).";
            } catch (Exception ex) {
                status.ForeColor = accent;
                status.Text = "Falha: " + ex.Message;
            }
        }

        void Apply(byte opcode, uint value, string description) {
            try {
                int changed = MonitorApi.Set(opcode, value);
                status.ForeColor = Color.FromArgb(25, 115, 55);
                status.Text = "✓ " + description + " — enviado para " + changed + " monitor(es).";
            } catch (Exception ex) {
                status.ForeColor = accent;
                status.Text = "Falha: " + ex.Message;
            }
        }
    }

    static class Program {
        [STAThread]
        static void Main(string[] args) {
            ProfileStore.Load();
            StartupManager.RefreshPathIfEnabled();
            if (args.Length == 2 && args[0] == "--mode") {
                uint value = args[1].Equals("Reader", StringComparison.OrdinalIgnoreCase) ? 1U : 11U;
                MonitorApi.Set(0x15, value); return;
            }
            if (args.Length == 3 && args[0] == "--set") {
                byte opcode = Convert.ToByte(args[1], 16); uint value = Convert.ToUInt32(args[2]);
                MonitorApi.Set(opcode, value); return;
            }
            if (args.Length == 2 && args[0] == "--filter") {
                NightFilter.Apply(true, Convert.ToInt32(args[1])); return;
            }
            if (args.Length == 1 && args[0] == "--filter-off") {
                NightFilter.Apply(false, 6500); return;
            }
            if (args.Length == 1 && args[0] == "--startup-enable") {
                StartupManager.SetEnabled(true); Environment.ExitCode = StartupManager.IsEnabled() ? 0 : 2; return;
            }
            if (args.Length == 1 && args[0] == "--startup-disable") {
                StartupManager.SetEnabled(false); Environment.ExitCode = StartupManager.IsEnabled() ? 2 : 0; return;
            }
            if (args.Length == 2 && args[0] == "--profile") {
                Profile profile = ProfileStore.ByKey(args[1]);
                ProfileStore.Apply(profile); return;
            }
            if (args.Length == 1 && args[0] == "--auto") {
                ProfileStore.Apply(ProfileStore.Current()); return;
            }
            bool startup = args.Length == 1 && args[0] == "--startup";
            if (startup) ProfileStore.Apply(ProfileStore.Current());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(startup));
        }
    }
}
