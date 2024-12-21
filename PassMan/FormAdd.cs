using System.IO;
using System.Windows.Forms;

namespace PassMan
{
    public partial class FormAdd : Form
    {
        public FormAdd(int action)
        {
            InitializeComponent();
            textBox1.Visible = action > 0;
            if (action == 0)
            {
                label1.Text = "Дата файла:";
                dateTimePicker1.Value = FormMain.dateFile;
                button1.Size = new System.Drawing.Size(240, 24);
                button1.Text = "Принять";
                button2.Visible = false;
            }
            else if (action == 1)
            {
                label1.Text = "Путь к файлу:";
                textBox1.TextChanged += textBox1_TextChanged;
            }
            else if (action == 2)
            {
                label1.Text = "Имя вкладки:";
            }
        }

        void FormAdd_Shown(object sender, System.EventArgs e)
        {
            dateTimePicker1.Visible = !textBox1.Visible;
        }

        void button1_Click(object sender, System.EventArgs e)
        {
            if (textBox1.Visible)
            {
                FormMain.addString = textBox1.Text;
            }
            else
            {
                FormMain.dateFile = dateTimePicker1.Value;
            }
        }

        void textBox1_TextChanged(object sender, System.EventArgs e)
        {
            if (Directory.Exists(textBox1.Text))
            {
                textBox1.TextChanged -= textBox1_TextChanged;
                textBox1.Text = textBox1.Text + (textBox1.Text.Contains("\\") ? "\\" : "/");
                textBox1.Select(textBox1.Text.Length, 0);
                textBox1.TextChanged += textBox1_TextChanged;
            }
        }
    }
}
