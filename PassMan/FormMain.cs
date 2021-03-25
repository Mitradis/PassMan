using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace PassMan
{
    public partial class FormMain : Form
    {
        public static string newfilename = null;
        public static string pathFiles = null;
        string registryPath = @"SOFTWARE\PassMan";
        string registryKey = "Path";
        string userPassword = null;
        string currentFile = null;
        string previousFile = null;
        const int derivationIterations = 1000;
        const int keySize = 256;
        int ylocation = 0;
        int count = 0;
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
                textBox1.Clear();
                textBox1.Visible = false;
                textBox1.Enabled = false;
                panel1.Enabled = true;
                button1.Enabled = true;
                button2.Enabled = true;
                Size = new Size(780, 400);
                Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - this.Width) / 2, (Screen.PrimaryScreen.WorkingArea.Height - this.Height) / 2);
                RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true);
                if (key != null)
                {
                    pathFiles = (string)key.GetValue(registryKey);
                }
                if (pathFiles != null)
                {
                    pathFiles = decryptString(pathFiles);
                }
                else
                {
                    DialogResult result = folderBrowserDialog1.ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        pathFiles = pathAddSlash(folderBrowserDialog1.SelectedPath);
                        key = Registry.LocalMachine.CreateSubKey(registryPath);
                        key.SetValue(registryKey, encryptString(pathFiles));
                    }
                }
                key.Close();
                if (Directory.Exists(pathFiles))
                {
                    foreach (string line in Directory.GetFiles(pathFiles))
                    {
                        createButton(Path.GetFileName(line));
                    }
                }
            }
        }

        private void createButton(string name)
        {
            count++;
            Button myButton = new Button();
            myButton.Text = name;
            myButton.Font = new System.Drawing.Font("Arial", 9.75F, FontStyle.Regular);
            myButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            myButton.Location = new System.Drawing.Point(0, ylocation);
            myButton.Size = new System.Drawing.Size(120, 32);
            myButton.UseVisualStyleBackColor = true;
            myButton.TabIndex = 2 + count;
            myButton.Click += newButton;
            panel1.Controls.Add(myButton);
            ylocation += 35;
        }

        private void newButton(object sender, EventArgs e)
        {
            if (changedText)
            {
                clickedButton.ForeColor = System.Drawing.SystemColors.ControlText;
            }
            timer1.Stop();
            timer1.Start();
            textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
            clickedButton = (Button)sender;
            currentFile = clickedButton.Text;
            if (previousFile != null && changedText)
            {
                List<string> writeList = new List<string>();
                writeList.Add(encryptString("PassMan Test OK"));
                foreach (string line in textBox2.Lines)
                {
                    if (!String.IsNullOrEmpty(line))
                    {
                        writeList.Add(encryptString(line));
                    }
                }
                writeToFile(pathFiles + previousFile, writeList);
                writeList.Clear();
                changedText = false;
            }
            if (currentFile != previousFile && File.Exists(pathFiles + currentFile))
            {
                textBox2.TextChanged -= textBox2_TextChanged;
                textBox2.Clear();
                List<string> cacheFile = new List<string>(File.ReadAllLines(pathFiles + currentFile));
                if (cacheFile.Count > 0 && decryptString(cacheFile[0]) == "PassMan Test OK")
                {
                    List<string> cacheList = new List<string>();
                    foreach (string line in cacheFile)
                    {
                        cacheList.Add(decryptString(line));
                    }
                    cacheList.RemoveAt(0);
                    textBox2.AppendText(String.Join(Environment.NewLine, cacheList));
                    textBox2.SelectionStart = 0;
                    textBox2.ScrollToCaret();
                    textBox2.Enabled = true;
                }
                else
                {
                    textBox2.AppendText("Файл пуст или неверный пароль.");
                    textBox2.Enabled = false;
                }
                textBox2.TextChanged += textBox2_TextChanged;
            }
            previousFile = currentFile;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (!changedText)
            {
                clickedButton.ForeColor = Color.Red;
            }
            changedText = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            newfilename = null;
            var form = new FormMessage();
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                writeToFile(pathFiles + newfilename, new List<string>() { encryptString("PassMan Test OK") });
                createButton(newfilename);
            }
            form.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dialog = MessageBox.Show("Сбросить параметры текущей папки?", "Подтверждение", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true);
                if (key.GetValue(registryKey) != null)
                {
                    key.DeleteValue(registryKey);
                }
                key.Close();
                Environment.Exit(0);
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                if (sender != null)
                    ((TextBox)sender).SelectAll();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Stop();
            textBox2.ForeColor = textBox2.BackColor;
        }

        private void textBox2_MouseClick(object sender, MouseEventArgs e)
        {
            timer1.Stop();
            timer1.Start();
            textBox2.ForeColor = System.Drawing.SystemColors.WindowText;
        }

        private void writeToFile(string path, List<string> list)
        {
            try
            {
                File.WriteAllLines(path, list, new UTF8Encoding(false));
            }
            catch
            {
                MessageBox.Show("Ошибка записи в файл:" + path);
            }
        }

        private static string pathAddSlash(string path)
        {
            if (!path.EndsWith("/") && !path.EndsWith(@"\"))
            {
                if (path.Contains("/"))
                {
                    path += "/";
                }
                else if (path.Contains(@"\"))
                {
                    path += @"\";
                }
            }
            return path;
        }

        private byte[] generate256BitsOfRandomEntropy()
        {
            var randomBytes = new byte[32];
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        private string encryptString(string plainText)
        {
            try
            {
                var saltStringBytes = generate256BitsOfRandomEntropy();
                var ivStringBytes = generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(userPassword, saltStringBytes, derivationIterations))
                {
                    var keyBytes = password.GetBytes(keySize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
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
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(keySize / 8).ToArray();
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(keySize / 8).Take(keySize / 8).ToArray();
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((keySize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((keySize / 8) * 2)).ToArray();
                using (var password = new Rfc2898DeriveBytes(userPassword, saltStringBytes, derivationIterations))
                {
                    var keyBytes = password.GetBytes(keySize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
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
                return "";
            }
        }
    }
}