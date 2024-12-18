using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public static string addString;
        public static DateTime dateFile = DateTime.Now;
        List<List<string>> filesList = new List<List<string>>();
        string headName;
        string regKey;
        string regPath = "Path";
        string regDate = "Date";
        string pathFile;
        string userPassword;
        const int derivationIterations = 1000;
        const int keySize = 256;
        int tabIndex = 0;
        bool textChanged = false;
        bool hidePassword = true;
        bool hidden = false;
        Button clickedButton;

        public FormMain()
        {
            InitializeComponent();
            headName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);
            Text = headName;
            regKey = "SOFTWARE\\" + Text;
        }

        void button1_Click(object sender, EventArgs e)
        {
            textBox1_KeyDown(this, new KeyEventArgs(Keys.Enter));
        }

        void button2_Click(object sender, EventArgs e)
        {
            hidePassword = !hidePassword;
            textBox1.PasswordChar = hidePassword ? '*' : '\0';
        }

        void button3_Click(object sender, EventArgs e)
        {
            button6_Click(this, new EventArgs());
        }

        void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && textBox1.Text.Length > 0)
            {
                userPassword = textBox1.Text;
                RegistryKey regkey = Registry.CurrentUser.OpenSubKey(regKey, true);
                if (regkey != null)
                {
                    pathFile = (string)regkey.GetValue(regPath);
                }
                if (pathFile != null)
                {
                    pathFile = decryptString(pathFile);
                    if (pathFile.Length > 0 && Directory.Exists(Path.GetDirectoryName(pathFile)))
                    {
                        string dateString = decryptString((string)regkey.GetValue(regDate, ""));
                        if (dateString.Length != 19)
                        {
                            setDate();
                        }
                        else
                        {
                            dateFile = DateTime.ParseExact(dateString, "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        }
                        textBox1.Clear();
                        textBox1.Visible = false;
                        button1.Visible = false;
                        button2.Visible = false;
                        button3.Visible = false;
                        textBox2.Visible = true;
                        tableLayoutPanel1.Visible = true;
                        button4.Visible = true;
                        button5.Visible = true;
                        button6.Visible = true;
                        button7.Visible = true;
                        if (File.Exists(pathFile) && new FileInfo(pathFile).Length > 0)
                        {
                            List<string> cacheFile = new List<string>();
                            try
                            {
                                foreach (string line in File.ReadAllLines(pathFile))
                                {
                                    if (!String.IsNullOrEmpty(line))
                                    {
                                        cacheFile.AddRange(decryptString(line).Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
                                        break;
                                    }
                                }
                            }
                            catch
                            {
                                MessageBox.Show("Не удалось прочитать файл.");
                            }
                            if (cacheFile.Count > 1)
                            {
                                for (int i = 0; i + 1 < cacheFile.Count; i += 2)
                                {
                                    if (!String.IsNullOrEmpty(cacheFile[i]) && !String.IsNullOrEmpty(cacheFile[i + 1]))
                                    {
                                        filesList.Add(new List<string>() { cacheFile[i], cacheFile[i + 1] });
                                        createButton(decryptString(cacheFile[i]));
                                    }
                                }
                            }
                            cacheFile.Clear();
                        }
                    }
                    else
                    {
                        button3.Visible = true;
                        textBox1.Size = new System.Drawing.Size(210, 22);
                        textBox1.Focus();
                    }
                }
                else
                {
                    textBox1.Clear();
                    textBox1.Visible = false;
                    button1.Visible = false;
                    button2.Visible = false;
                    button3.Visible = false;
                    Form form = new FormAdd(1);
                    form.ShowDialog(this);
                    if (!String.IsNullOrEmpty(addString) && Directory.Exists(Path.GetDirectoryName(addString)))
                    {
                        regkey = Registry.CurrentUser.CreateSubKey(regKey);
                        regkey.SetValue(regPath, encryptString(addString));
                    }
                    form.Dispose();
                    addString = null;
                    setDate();
                    Application.Restart();
                }
                regkey.Dispose();
            }
        }

        void setDate()
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey(regKey, true);
            Form form = new FormAdd(0);
            form.ShowDialog(this);
            form.Dispose();
            regkey.SetValue(regDate, encryptString(dateFile.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)));
            regkey.Dispose();
        }

        void createButton(string name)
        {
            Button myButton = new Button();
            myButton.Text = name;
            myButton.Font = new System.Drawing.Font("Arial", 9.75F, FontStyle.Regular);
            myButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            myButton.Size = new System.Drawing.Size(137, 33);
            myButton.TabIndex = tabIndex;
            myButton.Click += clickButton;
            myButton.Margin = new System.Windows.Forms.Padding(0, 0, 0, 0);
            tableLayoutPanel1.Controls.Add(myButton);
            tabIndex++;
        }

        void clickButton(object sender, EventArgs e)
        {
            textBox2.Enabled = false;
            if (textChanged && clickedButton != null)
            {
                filesList[clickedButton.TabIndex][1] = encryptString(textBox2.Text);
                writeToFile();
                clickedButton.ForeColor = System.Drawing.SystemColors.ControlText;
                textChanged = false;
            }
            if (clickedButton != (Button)sender)
            {
                clickedButton = (Button)sender;
                textBox2.TextChanged -= textBox2_TextChanged;
                textBox2.Clear();
                Text = headName + " - " + clickedButton.Text;
                textBox2.Text = decryptString(filesList[clickedButton.TabIndex][1]);
                textBox2.SelectionStart = 0;
                textBox2.ScrollToCaret();
                textBox2.Enabled = true;
                textBox2.TextChanged += textBox2_TextChanged;
            }
            else
            {
                textBox2.Enabled = true;
            }
            textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
            timer1.Stop();
            timer1.Start();
        }

        void button4_Click(object sender, EventArgs e)
        {
            Form form = new FormAdd(2);
            form.ShowDialog(this);
            if (!String.IsNullOrEmpty(addString))
            {
                filesList.Add(new List<string>() { encryptString(addString), encryptString("") });
                createButton(addString);
                writeToFile();
            }
            form.Dispose();
            addString = null;
        }

        void button5_Click(object sender, EventArgs e)
        {
            if (clickedButton != null)
            {
                if (dialogResult("Удалить " + clickedButton.Text + "?", "Подтверждение"))
                {
                    textBox2.Enabled = false;
                    textBox2.Clear();
                    timer1.Stop();
                    filesList.RemoveAt(clickedButton.TabIndex);
                    tableLayoutPanel1.Controls.Remove(clickedButton);
                    clickedButton = null;
                    textChanged = false;
                    Text = headName;
                    writeToFile();
                    tabIndex = 0;
                    foreach (Control line in tableLayoutPanel1.Controls)
                    {
                        line.TabIndex = tabIndex;
                        tabIndex++;
                    }
                }
            }
        }

        void button6_Click(object sender, EventArgs e)
        {
            if (dialogResult("Сбросить параметры?", "Подтверждение"))
            {
                RegistryKey regkey = Registry.CurrentUser.OpenSubKey(regKey, true);
                if (regkey != null)
                {
                    Registry.CurrentUser.DeleteSubKey(regKey);
                }
                regkey.Dispose();
                Application.Restart();
            }
        }

        void button7_Click(object sender, EventArgs e)
        {
            if (dialogResult("Показать путь до контейнера?", "Подтверждение"))
            {
                MessageBox.Show(pathFile);
            }
        }

        void textBox2_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
            if (hidden)
            {
                textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
                hidden = false;
            }
            if (!textChanged && clickedButton != null)
            {
                clickedButton.ForeColor = Color.Red;
            }
            textChanged = true;
        }

        void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                if (sender != null)
                {
                    ((TextBox)sender).SelectAll();
                }
            }
        }

        void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            textBox2.Select(textBox2.SelectionStart, 0);
            textBox2.ForeColor = textBox2.BackColor;
            hidden = true;
        }

        void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            timer1.Stop();
            timer1.Start();
            if (hidden)
            {
                textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
                hidden = false;
            }
        }

        bool dialogResult(string message, string title)
        {
            DialogResult dialog = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            return dialog == DialogResult.Yes;
        }

        void writeToFile()
        {
            if (dialogResult("Внести изменения в контейнер?", "Подтверждение") && pathFile != null && Directory.Exists(Path.GetDirectoryName(pathFile)))
            {
                List<string> cacheList = new List<string>();
                int count = filesList.Count;
                for (int i = 0; i < count; i++)
                {
                    cacheList.AddRange(filesList[i]);
                }
                try
                {
                    File.WriteAllText(pathFile, encryptString(String.Join(Environment.NewLine, cacheList)));
                    File.SetCreationTime(pathFile, randomDate(dateFile));
                    Thread.Sleep(50);
                    File.SetLastWriteTime(pathFile, dateFile);
                    Thread.Sleep(50);
                    File.SetLastAccessTime(pathFile, randomDate(dateFile));
                }
                catch
                {
                    MessageBox.Show("Не удалось записать файл.");
                }
                cacheList.Clear();
            }
        }

        DateTime randomDate(DateTime start)
        {
            DateTime newDate = start;
            newDate = newDate.AddMonths(new Random().Next(-12, 0));
            newDate = newDate.AddDays(new Random().Next(-30, 30));
            newDate = newDate.AddHours(new Random().Next(-24, 24));
            newDate = newDate.AddMinutes(new Random().Next(-60, 60));
            newDate = newDate.AddSeconds(new Random().Next(-60, 60));
            return newDate;
        }

        byte[] byteCombine(byte[] array1, byte[] array2)
        {
            byte[] bytes = new byte[array1.Length + array2.Length];
            Buffer.BlockCopy(array1, 0, bytes, 0, array1.Length);
            Buffer.BlockCopy(array2, 0, bytes, array1.Length, array2.Length);
            return bytes;
        }

        byte[] byteTake(byte[] array, int count)
        {
            byte[] bytes = new byte[count];
            Buffer.BlockCopy(array, 0, bytes, 0, count);
            return bytes;
        }

        byte[] byteSkip(byte[] array, int offset)
        {
            byte[] bytes = new byte[array.Length - offset];
            Buffer.BlockCopy(array, offset, bytes, 0, array.Length - offset);
            return bytes;
        }

        byte[] generate256BitsOfRandomEntropy()
        {
            byte[] bytes = new byte[32];
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(bytes);
            rngCsp.Dispose();
            return bytes;
        }

        string encryptString(string plainText)
        {
            try
            {
                byte[] saltStringBytes = generate256BitsOfRandomEntropy();
                byte[] ivStringBytes = generate256BitsOfRandomEntropy();
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(userPassword, saltStringBytes, derivationIterations);
                byte[] keyBytes = password.GetBytes(keySize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged();
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                ICryptoTransform encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes);
                keyBytes = null;
                MemoryStream memoryStream = new MemoryStream();
                CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
                cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                plainTextBytes = null;
                cryptoStream.FlushFinalBlock();
                byte[] cipherTextBytes = saltStringBytes;
                saltStringBytes = null;
                cipherTextBytes = byteCombine(cipherTextBytes, ivStringBytes);
                ivStringBytes = null;
                cipherTextBytes = byteCombine(cipherTextBytes, memoryStream.ToArray());
                password.Dispose();
                symmetricKey.Clear();
                encryptor.Dispose();
                memoryStream.Close();
                cryptoStream.Close();
                return Convert.ToBase64String(cipherTextBytes);
            }
            catch
            {
                return "";
            }
        }

        string decryptString(string cipherText)
        {
            try
            {
                byte[] cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                byte[] saltStringBytes = byteTake(cipherTextBytesWithSaltAndIv, keySize / 8);
                byte[] ivStringBytes = byteTake(byteSkip(cipherTextBytesWithSaltAndIv, keySize / 8), keySize / 8);
                byte[] cipherTextBytes = byteTake(byteSkip(cipherTextBytesWithSaltAndIv, (keySize / 8) * 2), cipherTextBytesWithSaltAndIv.Length - ((keySize / 8) * 2));
                cipherTextBytesWithSaltAndIv = null;
                Rfc2898DeriveBytes password = new Rfc2898DeriveBytes(userPassword, saltStringBytes, derivationIterations);
                saltStringBytes = null;
                byte[] keyBytes = password.GetBytes(keySize / 8);
                RijndaelManaged symmetricKey = new RijndaelManaged();
                symmetricKey.BlockSize = 256;
                symmetricKey.Mode = CipherMode.CBC;
                symmetricKey.Padding = PaddingMode.PKCS7;
                ICryptoTransform decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes);
                keyBytes = null;
                ivStringBytes = null;
                MemoryStream memoryStream = new MemoryStream(cipherTextBytes);
                CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read);
                byte[] plainTextBytes = new byte[cipherTextBytes.Length];
                cipherTextBytes = null;
                int decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                password.Dispose();
                symmetricKey.Clear();
                decryptor.Dispose();
                memoryStream.Close();
                cryptoStream.Close();
                return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
            }
            catch
            {
                return "";
            }
        }
    }
}