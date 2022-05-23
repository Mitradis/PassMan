using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace PassMan
{
    public partial class FormMain : Form
    {
        public static string addString = null;
        public static DateTime dateFile = DateTime.Now;
        List<List<string>> filesList = new List<List<string>>();
        string regKey = @"SOFTWARE\PassMan";
        string regPath = "Path";
        string regDate = "Date";
        string pathFile = null;
        string userPassword = null;
        const int derivationIterations = 1000;
        const int keySize = 256;
        int tabIndex = 0;
        int currentTab = -1;
        int previousTab = -1;
        bool changedText = false;
        Button clickedButton = null;

        public FormMain()
        {
            InitializeComponent();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox1.Text.Length > 0)
            {
                userPassword = textBox1.Text;
                Visible = false;
                textBox1.Clear();
                textBox1.Visible = false;
                textBox1.Enabled = false;
                Size = new Size(778, 400);
                Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - Height) / 2);
                button1.Enabled = true;
                button2.Enabled = true;
                button3.Enabled = true;
                button4.Enabled = true;
                string dateString = null;
                RegistryKey regkey = Registry.CurrentUser.OpenSubKey(regKey, true);
                if (regkey != null)
                {
                    pathFile = (string)regkey.GetValue(regPath);
                    dateString = (string)regkey.GetValue(regDate);
                }
                if (pathFile != null && dateString != null)
                {
                    pathFile = decryptString(pathFile);
                    dateString = decryptString(dateString);
                    if (dateString != null && dateString.Length == 19)
                    {
                        dateFile = DateTime.ParseExact(dateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    regkey = Registry.CurrentUser.CreateSubKey(regKey);
                    Form form = new FormAdd(0);
                    form.ShowDialog(this);
                    form.Dispose();
                    regkey.SetValue(regDate, encryptString(dateFile.ToString("dd.MM.yyyy HH:mm:ss")));
                    form = new FormAdd(1);
                    if (form.ShowDialog(this) == DialogResult.OK && !String.IsNullOrEmpty(addString))
                    {
                        pathFile = addString;
                        if (!File.Exists(pathFile) || (File.Exists(pathFile) && new FileInfo(pathFile).Length == 0))
                        {
                            prepareToWrite();
                        }
                        if (File.Exists(pathFile) && new FileInfo(pathFile).Length > 0)
                        {
                            regkey.SetValue(regPath, encryptString(pathFile));
                        }
                    }
                    form.Dispose();
                }
                regkey.Close();
                if (File.Exists(pathFile) && new FileInfo(pathFile).Length > 0)
                {
                    List<string> cacheFile = new List<string>();
                    cacheFile.AddRange(File.ReadAllLines(pathFile));
                    if (cacheFile.Count > 1 && decryptString(cacheFile[0]) == "PassMan file test string.")
                    {
                        string head = decryptString(cacheFile[1]);
                        if (!String.IsNullOrEmpty(head))
                        {
                            bool start = false;
                            string[] tabsStart = head.Split(new string[] { "|" }, StringSplitOptions.None);
                            for (int i = 2; i < cacheFile.Count; i++)
                            {
                                if (!String.IsNullOrEmpty(cacheFile[i]))
                                {
                                    if (tabsStart.Length > tabIndex && tabsStart[tabIndex] == i.ToString())
                                    {
                                        string line = decryptString(cacheFile[i]);
                                        if (!String.IsNullOrEmpty(line))
                                        {
                                            filesList.Add(new List<string>() { line });
                                            createButton(line);
                                            start = true;
                                        }
                                    }
                                    else if (start)
                                    {
                                        filesList[filesList.Count - 1].Add(cacheFile[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    button3.ForeColor = Color.Red;
                    button4.ForeColor = Color.Red;
                }
                Visible = true;
            }
        }

        private void createButton(string name)
        {
            Button myButton = new Button();
            myButton.Text = name;
            myButton.Font = new System.Drawing.Font("Arial", 9.75F, FontStyle.Regular);
            myButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            myButton.Size = new System.Drawing.Size(137, 32);
            myButton.Dock = DockStyle.Top;
            myButton.TabIndex = tabIndex;
            myButton.Click += newButton;
            panel1.Controls.Add(myButton);
            tabIndex++;
        }

        private void newButton(object sender, EventArgs e)
        {
            if (changedText && clickedButton != null)
            {
                clickedButton.ForeColor = System.Drawing.SystemColors.ControlText;
            }
            timer1.Stop();
            timer1.Start();
            textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
            clickedButton = (Button)sender;
            currentTab = clickedButton.TabIndex;
            Text = "PassMan - " + clickedButton.Text;
            if (previousTab != -1 && changedText)
            {
                string temp = filesList[previousTab][0];
                filesList[previousTab].Clear();
                filesList[previousTab].Add(temp);
                foreach (string line in textBox2.Lines)
                {
                    if (!String.IsNullOrEmpty(line))
                    {
                        filesList[previousTab].Add(encryptString(line));
                    }
                }
                prepareToWrite();
            }
            if (currentTab != previousTab)
            {
                textBox2.TextChanged -= textBox2_TextChanged;
                textBox2.Clear();
                List<string> cacheList = new List<string>();
                for (int i = 1; i < filesList[currentTab].Count; i++)
                {
                    cacheList.Add(decryptString(filesList[currentTab][i]));
                }
                textBox2.AppendText(String.Join(Environment.NewLine, cacheList));
                textBox2.SelectionStart = 0;
                textBox2.ScrollToCaret();
                textBox2.Enabled = true;
                textBox2.TextChanged += textBox2_TextChanged;
                cacheList = null;
            }
            previousTab = currentTab;
        }

        private void prepareToWrite()
        {
            int lines = 1;
            bool start = false;
            string tabsStart = "";
            List<string> cacheList = new List<string>();
            cacheList.Add(encryptString("PassMan file test string."));
            cacheList.Add("-1");
            for (int i = 0; i < filesList.Count; i++)
            {
                start = true;
                for (int j = 0; j < filesList[i].Count; j++)
                {
                    lines++;
                    if (start)
                    {
                        cacheList.Add(encryptString(filesList[i][j]));
                        tabsStart += (tabsStart.Length == 0) ? lines.ToString() : ("|" + lines.ToString());
                        start = false;
                    }
                    else
                    {
                        cacheList.Add(filesList[i][j]);
                    }
                }
            }
            cacheList[1] = encryptString((tabsStart.Length == 0) ? "-1" : tabsStart);
            writeToFile(cacheList);
            changedText = false;
            cacheList = null;
            tabsStart = null;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
            textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
            if (!changedText && clickedButton != null)
            {
                clickedButton.ForeColor = Color.Red;
            }
            changedText = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form form = new FormAdd(2);
            if (form.ShowDialog(this) == DialogResult.OK && !String.IsNullOrEmpty(addString))
            {
                filesList.Add(new List<string>() { addString });
                createButton(addString);
                prepareToWrite();
            }
            form.Dispose();
            addString = null;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (currentTab != -1)
            {
                DialogResult dialog = MessageBox.Show("Удалить " + clickedButton.Text + "?", "Подтверждение", MessageBoxButtons.YesNo);
                if (dialog == DialogResult.Yes)
                {
                    timer1.Stop();
                    filesList.RemoveAt(currentTab);
                    currentTab = -1;
                    previousTab = -1;
                    panel1.Controls.Remove(clickedButton);
                    clickedButton = null;
                    textBox2.Enabled = false;
                    textBox2.Clear();
                    Text = "PassMan";
                    prepareToWrite();
                    tabIndex = 0;
                    foreach (Control line in panel1.Controls)
                    {
                        line.TabIndex = tabIndex;
                        tabIndex++;
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Сбросить параметры?", "Подтверждение", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                RegistryKey regkey = Registry.CurrentUser.OpenSubKey(regKey, true);
                if (regkey != null)
                {
                    Registry.CurrentUser.DeleteSubKey(regKey);
                }
                regkey.Close();
                Application.Exit();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Показать путь до контейнера?", "Подтверждение", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                MessageBox.Show(pathFile);
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                if (sender != null)
                {
                    ((TextBox)sender).SelectAll();
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            textBox2.Select(textBox2.SelectionStart, 0);
            textBox2.ForeColor = textBox2.BackColor;
        }

        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            timer1.Stop();
            timer1.Start();
            textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void writeToFile(List<string> list)
        {
            if (pathFile != null && Directory.Exists(Path.GetDirectoryName(pathFile)))
            {
                try
                {
                    File.WriteAllLines(pathFile, list, new UTF8Encoding(false));
                    File.SetCreationTime(pathFile, dateFile);
                    Directory.SetCreationTime(Path.GetDirectoryName(pathFile), dateFile);
                    Thread.Sleep(50);
                    File.SetLastWriteTime(pathFile, dateFile);
                    Directory.SetLastWriteTime(Path.GetDirectoryName(pathFile), dateFile);
                    Thread.Sleep(50);
                    File.SetLastAccessTime(pathFile, dateFile);
                    Directory.SetLastAccessTime(Path.GetDirectoryName(pathFile), dateFile);
                }
                catch
                {
                    MessageBox.Show("Не удалось записать файл.");
                }
            }
        }

        private byte[] byteCombine(byte[] array1, byte[] array2)
        {
            byte[] bytes = new byte[array1.Length + array2.Length];
            Buffer.BlockCopy(array1, 0, bytes, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, bytes, array1.Length, array2.Length);
            return bytes;
        }

        private byte[] byteTake(byte[] array, int count)
        {
            byte[] bytes = new byte[count];
            Buffer.BlockCopy(array, 0, bytes, 0, count);
            return bytes;
        }

        private byte[] byteSkip(byte[] array, int offset)
        {
            byte[] bytes = new byte[array.Length - offset];
            Buffer.BlockCopy(array, offset, bytes, 0, array.Length - offset);
            return bytes;
        }

        private byte[] generate256BitsOfRandomEntropy()
        {
            byte[] bytes = new byte[32];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(bytes);
            }
            return bytes;
        }

        private string encryptString(string plainText)
        {
            try
            {
                byte[] saltStringBytes = generate256BitsOfRandomEntropy();
                byte[] ivStringBytes = generate256BitsOfRandomEntropy();
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(userPassword, saltStringBytes, derivationIterations))
                {
                    byte[] keyBytes = password.GetBytes(keySize / 8);
                    using (RijndaelManaged symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    byte[] cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = byteCombine(cipherTextBytes, ivStringBytes);
                                    cipherTextBytes = byteCombine(cipherTextBytes, memoryStream.ToArray());
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return "";
            }
        }

        private string decryptString(string cipherText)
        {
            try
            {
                byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                byte[] saltStringBytes = byteTake(cipherTextBytesWithSaltAndIv, keySize / 8);
                byte[] ivStringBytes = byteTake(byteSkip(cipherTextBytesWithSaltAndIv, keySize / 8), keySize / 8);
                byte[] cipherTextBytes = byteTake(byteSkip(cipherTextBytesWithSaltAndIv, (keySize / 8) * 2), cipherTextBytesWithSaltAndIv.Length - ((keySize / 8) * 2));
                using (Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(userPassword, saltStringBytes, derivationIterations))
                {
                    byte[] keyBytes = password.GetBytes(keySize / 8);
                    using (RijndaelManaged symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (MemoryStream memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                                    int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}