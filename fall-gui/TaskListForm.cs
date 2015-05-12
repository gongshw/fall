using fall_core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace fall_gui
{
    public partial class TaskListForm : Form, TaskListener
    {
        private List<DownloadTask> tasks = new List<DownloadTask>();

        private void bindTasks()
        {
            Action action = () =>
            {
                taskListView.Items.Clear();
                if (tasks != null)
                {
                    int i = 0;
                    List<ListViewItem> items = new List<ListViewItem>();
                    foreach (var t in tasks)
                    {
                        ListViewItem item = new ListViewItem(t.GetLocalFile(), i);
                        item.SubItems.AddRange(new string[] {
                        String.Format("[{0}/{1}]", Utils.FormatSize(t.FinishedSize), Utils.FormatSize(t.TotalSize)), 
                        String.Format("{0}/s",Utils.FormatSize((long)t.Speed)), 
                        String.Format("{0:F3}%", t.Process * 100)});
                        items.Add(item);
                        i++;
                    }
                    taskListView.Items.AddRange(items.ToArray());
                }
            };
            taskListView.Invoke(action);
        }

        public TaskListForm()
        {
            InitializeComponent();
            taskListView.View = View.Details;
            taskListView.CheckBoxes = true;
            taskListView.Columns.Add("Task Name", -2, HorizontalAlignment.Left);
            taskListView.Columns.Add("Size", -2, HorizontalAlignment.Left);
            taskListView.Columns.Add("Speed", -2, HorizontalAlignment.Left);
            taskListView.Columns.Add("Progress", -2, HorizontalAlignment.Left);
        }

        private async void buttonAdd_Click(object sender, EventArgs e)
        {
            NewTaskForm form = new NewTaskForm();
            form.ShowDialog();
            DownloadTask task = form.DownloadTask;
            if (task != null)
            {
                tasks.Add(task);
                task.BindTaskListener(this);
                this.bindTasks();
                await Task.Run(() => { task.Start(); });
            }
        }

        public void OnError(DownloadTask task, DownloadError exception)
        {
            MessageBox.Show(exception.Message);
        }

        public void OnDone(DownloadTask task)
        {
            MessageBox.Show(task.GetLocalFile() + " Done!");
        }

        public void OnProcessUpdate(DownloadTask task)
        {
            bindTasks();
        }
    }
}
