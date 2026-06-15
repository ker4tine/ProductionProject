using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class NoteCrudHelper
    {
        private const string NotesPageUrl = "http://localhost:8080/notes";
        private const string NotesJsonUrl = "http://localhost:8080/api/notes";

        public static TabPage CreateNotesTab(string connectionString, int currentUserId, bool canEdit)
        {
            string query = @"
                SELECT n.id AS [ID], n.title AS [Заголовок], u.login AS [Пользователь], n.content AS [Содержание], FORMAT(n.created_at, 'dd.MM.yyyy') AS [Дата]
                FROM Notes n
                JOIN Users u ON n.user_id = u.id
                ORDER BY n.created_at DESC, n.id DESC";

            TabPage page = new TabPage("Заметки");
            Panel contentPanel = new Panel { Dock = DockStyle.Fill };
            DataGridView grid = CreateGrid();
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38 };

            if (canEdit)
            {
                Button btnAdd = new Button { Text = "Добавить", Width = 100 };
                Button btnEdit = new Button { Text = "Изменить", Width = 100 };
                Button btnDelete = new Button { Text = "Удалить", Width = 100 };

                btnAdd.Click += (s, e) => AddNote(connectionString, currentUserId, grid, query);
                btnEdit.Click += (s, e) => EditNote(connectionString, grid, query);
                btnDelete.Click += (s, e) => DeleteNote(connectionString, grid, query);

                panel.Controls.Add(btnAdd);
                panel.Controls.Add(btnEdit);
                panel.Controls.Add(btnDelete);
            }

            Button btnRefresh = new Button { Text = "Обновить", Width = 100 };
            Button btnOpenPage = new Button { Text = "Открыть страницу", Width = 140 };
            Button btnOpenJson = new Button { Text = "Открыть JSON", Width = 120 };

            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, query);
            btnOpenPage.Click += (s, e) => OpenUrl(NotesPageUrl);
            btnOpenJson.Click += (s, e) => OpenUrl(NotesJsonUrl);

            panel.Controls.Add(btnRefresh);
            panel.Controls.Add(btnOpenPage);
            panel.Controls.Add(btnOpenJson);

            contentPanel.Controls.Add(grid);
            contentPanel.Controls.Add(panel);
            page.Controls.Add(contentPanel);
            LoadGrid(connectionString, grid, query);
            return page;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть ссылку: " + ex.Message);
            }
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = System.Drawing.Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            grid.DataBindingComplete += (s, e) => HideIdColumn(grid);
            return grid;
        }

        private static void LoadGrid(string connectionString, DataGridView grid, string query)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    grid.DataSource = table;
                    HideIdColumn(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заметок: " + ex.Message);
            }
        }

        private static void HideIdColumn(DataGridView grid)
        {
            if (grid.Columns.Contains("ID")) grid.Columns["ID"].Visible = false;
        }

        private static int GetSelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите строку.");
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddNote(string connectionString, int currentUserId, DataGridView grid, string query)
        {
            using (NoteEditForm form = new NoteEditForm("Добавление заметки"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("INSERT INTO Notes (user_id, title, content, created_at) VALUES (@user, @title, @content, GETDATE())", connection))
                {
                    command.Parameters.AddWithValue("@user", currentUserId);
                    command.Parameters.AddWithValue("@title", form.NoteTitle);
                    command.Parameters.AddWithValue("@content", form.NoteContent);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, query);
            }
        }

        private static void EditNote(string connectionString, DataGridView grid, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;

            string title = Convert.ToString(grid.CurrentRow.Cells["Заголовок"].Value);
            string content = Convert.ToString(grid.CurrentRow.Cells["Содержание"].Value);

            using (NoteEditForm form = new NoteEditForm("Изменение заметки", title, content))
            {
                if (form.ShowDialog() != DialogResult.OK) return;

                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand("UPDATE Notes SET title=@title, content=@content WHERE id=@id", connection))
                {
                    command.Parameters.AddWithValue("@title", form.NoteTitle);
                    command.Parameters.AddWithValue("@content", form.NoteContent);
                    command.Parameters.AddWithValue("@id", id);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
                LoadGrid(connectionString, grid, query);
            }
        }

        private static void DeleteNote(string connectionString, DataGridView grid, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную заметку?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("DELETE FROM Notes WHERE id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(connectionString, grid, query);
        }
    }
}
