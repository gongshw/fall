using fall_core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fall_gui
{
    public partial class NewTaskForm : Form
    {

        private DownloadTask _task;

        public DownloadTask DownloadTask { get { return _task; } }

        public NewTaskForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DownloadTaskAnalyzer factory = new DownloadTaskAnalyzer();
            DownloadTask task = factory.create(textBox1.Text, SaveFilePathText.Text);
            if (task != null)
            {
                this._task = task;
                this.Close();
            }
            else
            {
                MessageBox.Show("不能处理这个url!");
            }
        }

        async private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string fileName = await Task.Run(() =>
            {
                FileLink link = new FileLink(textBox1.Text);
                return link.FileName;
            });
            SaveFilePathText.Text = Directory.GetCurrentDirectory() + '\\' + fileName;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SaveFileDialog.FileName = Utils.GetFileNameFromUri(textBox1.Text);
            if (SaveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SaveFilePathText.Text = SaveFileDialog.FileName;
            }
        }

        private void NewTaskForm_Load(object sender, EventArgs e)
        {
            SaveFilePathText.Text = Directory.GetCurrentDirectory() + '\\';
        }
    }
}
