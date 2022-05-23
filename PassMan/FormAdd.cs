using System.Windows.Forms;

namespace PassMan
{
    public partial class FormAdd : Form
    {
        bool isName = false;

        public FormAdd(int action)
        {
            InitializeComponent();
            isName = action > 0;
            if (action == 0)
            {
                label1.Text = "Дата файла";
                dateTimePicker1.Enabled = true;
                dateTimePicker1.Visible = true;
                dateTimePicker1.Value = FormMain.dateFile;
            }
            else if (action == 1)
            {
                label1.Text = "Путь к файлу";
            }
            else if (action == 2)
            {
                label1.Text = "Имя вкладки";
            }
            textBox1.Enabled = !dateTimePicker1.Enabled;
            textBox1.Visible = !dateTimePicker1.Visible;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (isName)
            {
                FormMain.addString = textBox1.Text;
            }
            else
            {
                FormMain.dateFile = dateTimePicker1.Value;
            }
        }
    }
}
