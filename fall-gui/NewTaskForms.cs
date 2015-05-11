using fall_core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fall_gui
{
    public partial class NewTaskForm : Form, TaskListener
    {
        public NewTaskForm()
        {
            InitializeComponent();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            DownloadTaskAnalyzer factory = new DownloadTaskAnalyzer();
            DownloadTask task = factory.create(textBox1.Text, SaveFilePathText.Text);
            if (task != null)
            {
                task.BindTaskListener(this);
                await Task.Run(() =>
                {
                    task.Start();
                });
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

        public void OnError(DownloadTask task, DownloadError exception)
        {
            MessageBox.Show(exception.Message);
        }

        public void OnDone(DownloadTask task)
        {
            MessageBox.Show("done");
        }

        public void OnProcessUpdate(DownloadTask task)
        {
            Action<double> actionDelegate = (x) => { progressBar.Value = (int)x; };
            progressBar.Invoke(actionDelegate, task.GetProcess() * 100);
        }
    }
}
