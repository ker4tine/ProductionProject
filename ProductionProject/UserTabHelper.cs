using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class UserTabHelper
    {
        private const string Query = @"
SELECT u.user_id AS [ID], u.user_login AS [Логин], u.full_name AS [ФИО],
       r.role_name AS [Роль],
       CASE WHEN u.is_blocked = 1 THEN N'Да' ELSE N'Нет' END AS [Заблокирован],
       u.failed_attempts AS [Ошибок входа]
FROM Users u
JOIN Roles r ON u.role_id = r.role_id";

        public static TabPage CreateUsersTab(string connectionString)
        {
            TabPage page = new TabPage("Пользователи") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideIdColumn(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            Button add = UiHelper.CreateButton("Добавить", 100);
            Button block = UiHelper.CreateButton("Заблокировать", 130);
            Button unblock = UiHelper.CreateButton("Разблокировать", 140);
            Button reset = UiHelper.CreateButton("Сбросить ошибки", 150);
            Button refresh = UiHelper.CreateButton("Обновить", 100);

            add.Click += (s, e) => AddUser(connectionString, grid, source);
            block.Click += (s, e) => SetBlocked(connectionString, grid, source, true);
            unblock.Click += (s, e) => SetBlocked(connectionString, grid, source, false);
            reset.Click += (s, e) => ResetAttempts(connectionString, grid, source);
            refresh.Click += (s, e) => LoadGrid(connectionString, grid, source);

            toolbar.Controls.Add(add);
            toolbar.Controls.Add(block);
            toolbar.Controls.Add(unblock);
            toolbar.Controls.Add(reset);
            toolbar.Controls.Add(refresh);
            UiHelper.AddStartsWithSearch(toolbar, source, "Логин", "Поиск по логину:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Пользователи", "Управление учетными записями и блокировками"));
            LoadGrid(connectionString, grid, source);
            return page;
        }

        private static void LoadGrid(string cs, DataGridView grid, BindingSource source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlDataAdapter adapter = new SqlDataAdapter(Query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    HideIdColumn(grid);
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки пользователей: " + ex.Message); }
        }

        private static void AddUser(string cs, DataGridView grid, BindingSource source)
        {
            using (UserEditForm form = new UserEditForm(cs))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                using (SqlConnection connection = new SqlConnection(cs))
                using (SqlCommand command = new SqlCommand("INSERT INTO Users (user_login, password_hash, full_name, role_id) VALUES (@login,@password,@name,@role)", connection))
                {
                    command.Parameters.AddWithValue("@login", form.UserLogin);
                    command.Parameters.AddWithValue("@password", form.UserPassword);
                    command.Parameters.AddWithValue("@name", form.FullName);
                    command.Parameters.AddWithValue("@role", form.RoleId);
                    connection.Open(); command.ExecuteNonQuery();
                }
                LoadGrid(cs, grid, source);
            }
        }

        private static int SelectedUserId(DataGridView grid)
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Выберите пользователя."); return 0; }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void SetBlocked(string cs, DataGridView grid, BindingSource source, bool blocked)
        {
            int id = SelectedUserId(grid); if (id == 0) return;
            Execute(cs, "UPDATE Users SET is_blocked=@blocked WHERE user_id=@id", command =>
            {
                command.Parameters.AddWithValue("@blocked", blocked);
                command.Parameters.AddWithValue("@id", id);
            });
            LoadGrid(cs, grid, source);
        }

        private static void ResetAttempts(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedUserId(grid); if (id == 0) return;
            Execute(cs, "UPDATE Users SET failed_attempts=0, is_blocked=0 WHERE user_id=@id",
                command => command.Parameters.AddWithValue("@id", id));
            LoadGrid(cs, grid, source);
        }

        private static void Execute(string cs, string sql, Action<SqlCommand> configure)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                configure(command);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void HideIdColumn(DataGridView grid)
        {
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
        }
    }
}
