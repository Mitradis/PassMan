using System;
using System.IO;
using System.Windows.Forms;

namespace PassMan
{
    public partial class FormMessage : Form
    {
        public FormMessage()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(textBox1.Text) && !checkIllegal(textBox1.Text) && !File.Exists(FormMain.pathFiles + textBox1.Text))
            {
                FormMain.newfilename = textBox1.Text;
                DialogResult = DialogResult.OK;
            }
            else
            {
                DialogResult = DialogResult.Cancel;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(this, new EventArgs());
            }
        }

        private bool checkIllegal(string name)
        {
            char[] Illegal = Path.GetInvalidFileNameChars();
            foreach (char c in Illegal)
            {
                if (name.Contains(c.ToString()))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
