using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class UserTabHelper
    {
        public static TabPage CreateUsersTab(string connectionString)
        {
            string query = "SELECT u.id AS [ID], u.login AS [Логин], u.full_name AS [ФИО], r.name AS [Роль], CASE WHEN u.is_blocked = 1 THEN N'Да' ELSE N'Нет' END AS [Заблокирован], u.failed_attempts AS [Ошибок входа] FROM Users u JOIN Roles r ON u.role_id = r.id";

            TabPage page = new TabPage("Пользователи") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideIdColumn(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            Button btnAdd = UiHelper.CreateButton("Добавить", 100);
            Button btnBlock = UiHelper.CreateButton("Заблокировать", 130);
            Button btnUnblock = UiHelper.CreateButton("Разблокировать", 140);
            Button btnReset = UiHelper.CreateButton("Сбросить ошибки", 150);
            Button btnRefresh = UiHelper.CreateButton("Обновить", 100);

            btnAdd.Click += (s, e) => AddUser(connectionString, grid, source, query);
            btnBlock.Click += (s, e) => SetSelectedUserBlocked(connectionString, grid, source, true, query);
            btnUnblock.Click += (s, e) => SetSelectedUserBlocked(connectionString, grid, source, false, query);
            btnReset.Click += (s, e) => ResetSelectedUserAttempts(connectionString, grid, source, query);
            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, source, query);

            toolbar.Controls.Add(btnAdd);
            toolbar.Controls.Add(btnBlock);
            toolbar.Controls.Add(btnUnblock);
            toolbar.Controls.Add(btnReset);
            toolbar.Controls.Add(btnRefresh);

            Panel tablePanel = UiHelper.CreateTablePanel(grid, source);
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(tablePanel);
            content.Controls.Add(toolbar);

            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Пользователи", "Управление учетными записями и блокировками"));
            LoadGrid(connectionString, grid, source, query);
            return page;
        }

        private static void LoadGrid(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    HideIdColumn(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message);
            }
        }

        private static void AddUser(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            using (UserEditForm form = new UserEditForm())
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("INSERT INTO Users (login, password_hash, full_name, role_id) VALUES (@login, @password, @name, @role)", connection))
                {
                    command.Parameters.AddWithValue("@login", form.UserLogin);
                    command.Parameters.AddWithValue("@password", form.UserPassword);
                    command.Parameters.AddWithValue("@name", form.FullName);
                    command.Parameters.AddWithValue("@role", form.RoleId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, source, query);
            }
        }

        private static int GetSelectedUserId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите пользователя.");
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void SetSelectedUserBlocked(string connectionString, DataGridView grid, BindingSource source, bool blocked, string query)
        {
            int id = GetSelectedUserId(grid);
            if (id == 0) return;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("UPDATE Users SET is_blocked = @blocked WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@blocked", blocked);
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(connectionString, grid, source, query);
        }

        private static void ResetSelectedUserAttempts(string connectionString, DataGridView grid, BindingSource source, string query)
        {
            int id = GetSelectedUserId(grid);
            if (id == 0) return;
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("UPDATE Users SET failed_attempts = 0, is_blocked = 0 WHERE id = @id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(connectionString, grid, source, query);
        }

        private static void HideIdColumn(DataGridView grid)
        {
            if (grid.Columns.Contains("ID"))
                grid.Columns["ID"].Visible = false;
        }
    }
}
