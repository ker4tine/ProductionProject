using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public partial class Form1 : Form
    {
        private string connectionString = "Server=.\\SQLEXPRESS;Database=ProductionDB;Trusted_Connection=True;";
        private SqlConnection connection;
        private TabControl mainTabControl;
        private DataGridView currentDataGridView;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Информационная система управления производством";
            this.Size = new System.Drawing.Size(1200, 700);

            mainTabControl = new TabControl();
            mainTabControl.Dock = DockStyle.Fill;
            mainTabControl.Name = "mainTabControl";

            mainTabControl.TabPages.Add(CreateTabPage("Заказчики", "Counterparties"));
            mainTabControl.TabPages.Add(CreateTabPage("Материалы", "Materials"));
            mainTabControl.TabPages.Add(CreateTabPage("Операции", "Operations"));
            mainTabControl.TabPages.Add(CreateTabPage("Продукция", "Products"));
            mainTabControl.TabPages.Add(CreateTabPage("Заказы", "CustomerOrders"));
            mainTabControl.TabPages.Add(CreateTabPage("Спецификации", "Specifications"));

            mainTabControl.SelectedIndexChanged += MainTabControl_SelectedIndexChanged;

            this.Controls.Add(mainTabControl);
            connection = new SqlConnection(connectionString);
        }

        private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if (tabControl != null && tabControl.SelectedIndex >= 0)
            {
                TabPage tabPage = tabControl.TabPages[tabControl.SelectedIndex];
                foreach (Control ctrl in tabPage.Controls)
                {
                    if (ctrl is DataGridView)
                    {
                        currentDataGridView = (DataGridView)ctrl;
                        break;
                    }
                }
            }
        }

        private TabPage CreateTabPage(string title, string tableName)
        {
            TabPage tabPage = new TabPage(title);
            tabPage.Name = "tabPage" + tableName;

            DataGridView dataGridView = new DataGridView();
            dataGridView.Dock = DockStyle.Fill;
            dataGridView.Name = "dataGridView" + tableName;
            dataGridView.ReadOnly = false;
            dataGridView.AllowUserToAddRows = true;
            dataGridView.AllowUserToDeleteRows = true;
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            Button btnLoad = new Button();
            btnLoad.Text = "Загрузить данные";
            btnLoad.Size = new System.Drawing.Size(120, 30);
            btnLoad.Location = new System.Drawing.Point(10, 10);
            btnLoad.Tag = tableName;
            btnLoad.Name = "btnLoad" + tableName;
            btnLoad.Click += BtnLoad_Click;

            Button btnSave = new Button();
            btnSave.Text = "Сохранить изменения";
            btnSave.Size = new System.Drawing.Size(120, 30);
            btnSave.Location = new System.Drawing.Point(140, 10);
            btnSave.Tag = tableName;
            btnSave.Name = "btnSave" + tableName;
            btnSave.Click += BtnSave_Click;

            Panel panel = new Panel();
            panel.Dock = DockStyle.Top;
            panel.Height = 50;
            panel.Controls.Add(btnLoad);
            panel.Controls.Add(btnSave);

            tabPage.Controls.Add(dataGridView);
            tabPage.Controls.Add(panel);

            return tabPage;
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string tableName = btn.Tag.ToString();
            LoadData(tableName);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            string tableName = btn.Tag.ToString();
            SaveData(tableName);
        }

        private void LoadData(string tableName)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                string query = $"SELECT * FROM {tableName}";
                SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

                DataGridView dataGridView = FindDataGridViewByTableName(tableName);

                if (dataGridView != null)
                {
                    dataGridView.DataSource = dataTable;
                    MessageBox.Show($"Данные из таблицы {tableName} загружены успешно!", "Информация",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"Не удалось найти таблицу для отображения {tableName}", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private DataGridView FindDataGridViewByTableName(string tableName)
        {
            string dataGridViewName = "dataGridView" + tableName;

            if (mainTabControl != null && mainTabControl.SelectedIndex >= 0)
            {
                TabPage tabPage = mainTabControl.TabPages[mainTabControl.SelectedIndex];
                foreach (Control ctrl in tabPage.Controls)
                {
                    if (ctrl is DataGridView && ctrl.Name == dataGridViewName)
                    {
                        return (DataGridView)ctrl;
                    }
                }
            }

            foreach (TabPage tabPage in mainTabControl.TabPages)
            {
                foreach (Control ctrl in tabPage.Controls)
                {
                    if (ctrl is DataGridView && ctrl.Name == dataGridViewName)
                    {
                        return (DataGridView)ctrl;
                    }
                }
            }

            return null;
        }

        private void SaveData(string tableName)
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                DataGridView dataGridView = FindDataGridViewByTableName(tableName);

                if (dataGridView == null || dataGridView.DataSource == null)
                {
                    MessageBox.Show("Сначала загрузите данные!", "Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DataTable dataTable = (DataTable)dataGridView.DataSource;
                SqlDataAdapter adapter = new SqlDataAdapter($"SELECT * FROM {tableName}", connection);
                SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                adapter.Update(dataTable);

                MessageBox.Show($"Изменения в таблице {tableName} сохранены успешно!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения данных: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TestConnection();
        }

        private void TestConnection()
        {
            try
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();
                MessageBox.Show("Подключение к базе данных установлено успешно!", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к БД: {ex.Message}\n\nПроверьте:\n1. Запущен ли SQL Server\n2. Существует ли база данных ProductionDB\n3. Правильность строки подключения", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (connection.State == ConnectionState.Open)
                    connection.Close();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (connection != null && connection.State == ConnectionState.Open)
            {
                connection.Close();
            }
            base.OnFormClosing(e);
        }
    }
}