using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class SpecificationCrudHelper
    {
        public static TabPage CreateSpecificationsTab(string connectionString, bool canEdit)
        {
            string query = @"
                SELECT s.id AS [ID], s.product_id AS [ProductID], s.material_id AS [MaterialID], s.operation_id AS [OperationID],
                       p.name AS [Продукция],
                       ISNULL(m.name, o.name) AS [Материал или операция],
                       CASE WHEN m.id IS NOT NULL THEN N'Материал' ELSE N'Операция' END AS [Тип],
                       CASE WHEN m.id IS NOT NULL THEN s.material_qty ELSE s.operation_qty END AS [Количество],
                       ISNULL(m.unit, N'операция') AS [Ед. изм.],
                       ISNULL(m.price, o.price) AS [Цена]
                FROM Specifications s
                JOIN Products p ON s.product_id = p.id
                LEFT JOIN Materials m ON s.material_id = m.id
                LEFT JOIN Operations o ON s.operation_id = o.id";

            TabPage page = new TabPage("Спецификации");
            DataGridView grid = CreateGrid();
            FlowLayoutPanel panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = canEdit ? 38 : 1 };

            if (canEdit)
            {
                Button btnAdd = new Button { Text = "Добавить", Width = 100 };
                Button btnEdit = new Button { Text = "Изменить", Width = 100 };
                Button btnDelete = new Button { Text = "Удалить", Width = 100 };
                btnAdd.Click += (s, e) => AddSpec(connectionString, grid, query);
                btnEdit.Click += (s, e) => EditSpec(connectionString, grid, query);
                btnDelete.Click += (s, e) => DeleteSpec(connectionString, grid, query);
                panel.Controls.Add(btnAdd);
                panel.Controls.Add(btnEdit);
                panel.Controls.Add(btnDelete);
            }

            page.Controls.Add(grid);
            page.Controls.Add(panel);
            LoadGrid(connectionString, grid, query);
            return page;
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
            grid.DataBindingComplete += (s, e) => HideColumns(grid);
            return grid;
        }

        private static void LoadGrid(string connectionString, DataGridView grid, string query)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(query, connection))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                grid.DataSource = table;
                HideColumns(grid);
            }
        }

        private static void HideColumns(DataGridView grid)
        {
            HideColumn(grid, "ID");
            HideColumn(grid, "ProductID");
            HideColumn(grid, "MaterialID");
            HideColumn(grid, "OperationID");
        }

        private static void HideColumn(DataGridView grid, string name)
        {
            if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
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

        private static void AddSpec(string connectionString, DataGridView grid, string query)
        {
            using (SpecificationEditForm form = new SpecificationEditForm(connectionString, "Добавление спецификации"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                SaveSpec(connectionString, "INSERT INTO Specifications (product_id, material_id, operation_id, material_qty, operation_qty) VALUES (@product, @material, @operation, @materialQty, @operationQty)", form, 0);
                LoadGrid(connectionString, grid, query);
            }
        }

        private static void EditSpec(string connectionString, DataGridView grid, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;

            object productId = grid.CurrentRow.Cells["ProductID"].Value;
            object materialId = grid.CurrentRow.Cells["MaterialID"].Value == DBNull.Value ? null : grid.CurrentRow.Cells["MaterialID"].Value;
            object operationId = grid.CurrentRow.Cells["OperationID"].Value == DBNull.Value ? null : grid.CurrentRow.Cells["OperationID"].Value;
            decimal quantity = Convert.ToDecimal(grid.CurrentRow.Cells["Количество"].Value);

            using (SpecificationEditForm form = new SpecificationEditForm(connectionString, "Изменение спецификации", productId, materialId, operationId, quantity))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                SaveSpec(connectionString, "UPDATE Specifications SET product_id=@product, material_id=@material, operation_id=@operation, material_qty=@materialQty, operation_qty=@operationQty WHERE id=@id", form, id);
                LoadGrid(connectionString, grid, query);
            }
        }

        private static void DeleteSpec(string connectionString, DataGridView grid, string query)
        {
            int id = GetSelectedId(grid);
            if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную спецификацию?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("DELETE FROM Specifications WHERE id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(connectionString, grid, query);
        }

        private static void SaveSpec(string connectionString, string sql, SpecificationEditForm form, int id)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                bool isMaterial = form.MaterialId != DBNull.Value;
                command.Parameters.AddWithValue("@product", form.ProductId);
                command.Parameters.AddWithValue("@material", form.MaterialId);
                command.Parameters.AddWithValue("@operation", form.OperationId);
                command.Parameters.AddWithValue("@materialQty", isMaterial ? form.Quantity : 0);
                command.Parameters.AddWithValue("@operationQty", isMaterial ? 0 : form.Quantity);
                if (id > 0) command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
