using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public partial class Form1 : Form
    {
        private readonly string connectionString = @"Data Source=WIN-BBJB9MMFFR1\SQLEXPRESS;Initial Catalog=PracticeDB;Integrated Security=True;TrustServerCertificate=True";

        private TextBox txtLogin;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblMessage;

        private int failedAttempts = 0;
        private int currentUserId;
        private string currentLogin;
        private string currentRole;

        public Form1()
        {
            InitializeComponent();
            BuildLoginForm();
        }

        private void BuildLoginForm()
        {
            Controls.Clear();
            Text = "Авторизация";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(360, 260);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Label lblTitle = new Label
            {
                Text = "Вход в систему",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(105, 20)
            };

            Label lblLogin = new Label { Text = "Логин:", Location = new Point(40, 70), AutoSize = true };
            txtLogin = new TextBox { Location = new Point(130, 67), Width = 160 };

            Label lblPassword = new Label { Text = "Пароль:", Location = new Point(40, 105), AutoSize = true };
            txtPassword = new TextBox { Location = new Point(130, 102), Width = 160, UseSystemPasswordChar = true };

            btnLogin = new Button { Text = "Войти", Location = new Point(130, 140), Width = 100 };
            btnLogin.Click += BtnLogin_Click;

            lblMessage = new Label
            {
                Text = "",
                ForeColor = Color.Red,
                Location = new Point(40, 180),
                Size = new Size(270, 40)
            };

            Controls.Add(lblTitle);
            Controls.Add(lblLogin);
            Controls.Add(txtLogin);
            Controls.Add(lblPassword);
            Controls.Add(txtPassword);
            Controls.Add(btnLogin);
            Controls.Add(lblMessage);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (failedAttempts >= 2)
            {
                using (CaptchaForm captcha = new CaptchaForm())
                {
                    if (captcha.ShowDialog() != DialogResult.OK)
                    {
                        lblMessage.Text = "Капча введена неверно.";
                        return;
                    }
                }
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                string sql = @"
                    SELECT u.id, u.login, r.name AS role_name, u.is_blocked
                    FROM Users u
                    JOIN Roles r ON u.role_id = r.id
                    WHERE u.login = @login AND u.password_hash = @password";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@login", txtLogin.Text.Trim());
                    command.Parameters.AddWithValue("@password", txtPassword.Text.Trim());

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            failedAttempts++;
                            lblMessage.Text = "Неверный логин или пароль.";
                            return;
                        }

                        if ((bool)reader["is_blocked"])
                        {
                            lblMessage.Text = "Пользователь заблокирован.";
                            return;
                        }

                        currentUserId = Convert.ToInt32(reader["id"]);
                        currentLogin = reader["login"].ToString();
                        currentRole = reader["role_name"].ToString();
                    }
                }
            }

            BuildMainForm();
        }

        private void BuildMainForm()
        {
            Controls.Clear();
            Text = "Производственный учет";
            Size = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            Label lblUser = new Label
            {
                Text = "Пользователь: " + currentLogin + " | Роль: " + currentRole,
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.MiddleLeft
            };

            TabControl tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateTableTab("Заказы", "SELECT * FROM CustomerOrders", "CustomerOrders"));
            tabs.TabPages.Add(CreateTableTab("Позиции заказов", "SELECT * FROM CustomerOrderItems", "CustomerOrderItems"));
            tabs.TabPages.Add(CreateTableTab("Продукция", "SELECT * FROM Products", "Products"));
            tabs.TabPages.Add(CreateTableTab("Материалы", "SELECT * FROM Materials", "Materials"));
            tabs.TabPages.Add(CreateTableTab("Операции", "SELECT * FROM Operations", "Operations"));
            tabs.TabPages.Add(CreateTableTab("Спецификации", "SELECT * FROM Specifications", "Specifications"));
            tabs.TabPages.Add(CreateTableTab("Заметки", "SELECT n.id, n.title + N' - ' + u.login AS title_user, n.content, FORMAT(n.created_at, 'dd.MM.yyyy') AS formatted_date FROM Notes n JOIN Users u ON n.user_id = u.id", "Notes"));
            tabs.TabPages.Add(CreateCostTab());

            if (currentRole == "Администратор")
            {
                tabs.TabPages.Add(CreateTableTab("Пользователи", "SELECT u.id, u.login, u.full_name, r.name AS role_name, u.is_blocked, u.failed_attempts FROM Users u JOIN Roles r ON u.role_id = r.id", "Users"));
            }

            Controls.Add(tabs);
            Controls.Add(lblUser);
        }

        private TabPage CreateTableTab(string title, string query, string tableName)
        {
            TabPage page = new TabPage(title);
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false
            };

            Button refreshButton = new Button
            {
                Text = "Обновить",
                Dock = DockStyle.Top,
                Height = 32
            };

            refreshButton.Click += (s, e) => LoadGrid(grid, query);
            page.Controls.Add(grid);
            page.Controls.Add(refreshButton);
            LoadGrid(grid, query);
            return page;
        }

        private TabPage CreateCostTab()
        {
            string query = @"
                SELECT 
                    co.id AS order_id,
                    SUM(
                        coi.quantity * (
                            ISNULL(s.material_qty, 0) * ISNULL(m.price, 0) +
                            ISNULL(s.operation_qty, 0) * ISNULL(o.price, 0)
                        )
                    ) AS total_price
                FROM CustomerOrders co
                JOIN CustomerOrderItems coi ON co.id = coi.order_id
                JOIN Specifications s ON coi.product_id = s.product_id
                LEFT JOIN Materials m ON s.material_id = m.id
                LEFT JOIN Operations o ON s.operation_id = o.id
                GROUP BY co.id";

            return CreateTableTab("Расчет стоимости", query, "Cost");
        }

        private void LoadGrid(DataGridView grid, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    grid.DataSource = table;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }
    }
}
