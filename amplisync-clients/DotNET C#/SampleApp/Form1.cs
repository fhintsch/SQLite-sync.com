using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using SampleApp.Properties;
using SQLiteSyncCOMCsharp;
using System.Collections.Specialized;

namespace SampleApp
{
    public partial class Form1 : Form
    {
        private string dbPath;
        private string connString;
        private string wsUrl;
        private string subscriber;
        private DataTable tables;
        private bool doSetup;
        private Settings appSettings = Settings.Default;

        public Form1()
        {
        InitializeComponent();
            dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SQLiteDemo.db");
            connString = $"Data Source={dbPath}";
            textBox4.Text = dbPath;
            wsUrl = appSettings.Server;
            textBox3.Text = wsUrl;
            subscriber = appSettings.Subscriber;
            textBox1.Text = subscriber;
        }

        private void SetupTableView()
        {
            SQLiteSyncCOMClient sqlite = new SQLiteSyncCOMClient(connString, wsUrl);
            tables = sqlite.GetTables();
            var tabControl = tabControl1;
            tabControl.SuspendLayout();
            int i = 0;
            for (; i < tables.Rows.Count; i++)
            {
                TabPage tabPage;
                if (i >= tabControl.TabPages.Count)
                {
                    tabPage = new TabPage();
                    tabControl.Controls.Add(tabPage);
                    InitializeTabpage(tabPage, i);
                }
                else
                {
                    tabPage = tabControl.TabPages[i];                  
                }
                tabPage.Text = tables.Rows[i]["tbl_Name"].ToString();
            }
            int unusedPages = tabControl.TabPages.Count - i;
            
            for (int j = 0; j < unusedPages; j++)
            {
                TabPage page = tabControl.TabPages[i];
                tabControl.TabPages.Remove(page);
            }
            //this.Controls.Add(tabControl);
            tabControl.ResumeLayout(false);
        }

        private void InitializeTabpage(TabPage tabPage, int i)
        {
            tabPage.SuspendLayout();
            tabPage.Location = new System.Drawing.Point(4, 22);
            tabPage.Name = tabPage.GetType().Name + i.ToString();
            tabPage.Padding = new System.Windows.Forms.Padding(3);
            tabPage.Size = new System.Drawing.Size(706, 400);
            tabPage.TabIndex = i;
            tabPage.UseVisualStyleBackColor = true;
            var dataGridView = new DataGridView();
            tabPage.Controls.Add(dataGridView);
            InitializeGridView(dataGridView, i);
            tabPage.ResumeLayout();
        }

        private void InitializeGridView(DataGridView dataGridView, int i)
        {
            ((System.ComponentModel.ISupportInitialize)(dataGridView)).BeginInit();
            dataGridView.AllowUserToAddRows = false;
            dataGridView.AllowUserToDeleteRows = false;
            dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            dataGridView.Location = new System.Drawing.Point(0, 0);
            dataGridView.MultiSelect = false;
            dataGridView.Name = dataGridView.GetType().Name + i.ToString();
            dataGridView.ReadOnly = true;
            dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            dataGridView.Size = new System.Drawing.Size(706, 358);
            dataGridView.TabIndex = 0;
            ((System.ComponentModel.ISupportInitialize)(dataGridView)).EndInit();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            SQLiteSyncCOMClient sqlite = new SQLiteSyncCOMClient(connString, wsUrl);
            sqlite.ReinitializeDatabase(subscriber);
            if (doSetup)
            {
                doSetup = false;
                SetupTableView();
            }
            LoadData();
            this.Cursor = Cursors.Default;
            MessageBox.Show("Reinitialization done!");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            SQLiteSyncCOMClient sqlite = new SQLiteSyncCOMClient(connString, wsUrl);
            sqlite.SendAndReceiveChanges(subscriber);
            if (doSetup)
            {
                doSetup = false;
                SetupTableView();
            }
            LoadData();
            this.Cursor = Cursors.Default;
            MessageBox.Show("Send and receive changes done!");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            SQLiteSyncCOMClient sqlite = new SQLiteSyncCOMClient(connString, wsUrl);
            var response = sqlite.AddTable(textBox2.Text);
            this.Cursor = Cursors.Default;
            if (response.ResponseStatus == RestSharp.ResponseStatus.Error)
            {
                MessageBox.Show(response.ErrorMessage);
            }
            else
            {
                MessageBox.Show($"Table {textBox2.Text} added.");
                sqlite.ReinitializeDatabase(textBox1.Text);
                MessageBox.Show("Reinitialization done!");
                sqlite.SendAndReceiveChanges(textBox1.Text);
                SetupTableView();
                LoadData();
                MessageBox.Show("Send and receive changes done!");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                SQLiteSyncCOMClient sqlite = new SQLiteSyncCOMClient(connString, wsUrl);
                var response = sqlite.TestConnection();
                if (response.ResponseStatus == RestSharp.ResponseStatus.Error)
                {
                    MessageBox.Show(response.ErrorMessage);
                }
                else
                {
                    SetupTableView();
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Cannot load Data: " + ex.Message);
            }
        }

        private void LoadData()
        {
            using (SQLiteConnection conn = new SQLiteConnection(this.connString))
            {
                using (SQLiteCommand cmd = new SQLiteCommand())
                {
                    cmd.Connection = conn;
                    conn.Open();

                    SQLiteHelper sh = new SQLiteHelper(cmd);
                    for (int i = 0; i < tables.Rows.Count; i++)
                    {
                        TabPage tabPage = (TabPage)tabControl1.Controls[i];
                        DataGridView dataGridView = (DataGridView)tabPage.Controls[0];
                        dataGridView.DataSource = sh.Select($"Select * from {tabPage.Text};");
                    }

                    conn.Close();
                }
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            doSetup = true;
            wsUrl = textBox3.Text;
            appSettings.Server = wsUrl;
            appSettings.Save();
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            subscriber = textBox1.Text;
            appSettings.Subscriber = subscriber;
            appSettings.Save();
        }
    }
}
