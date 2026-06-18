using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class SpecificationCrudHelper
    {
        private const string Query = @"
SELECT s.specification_id AS [ID], s.product_id AS [ProductID],
       s.material_id AS [MaterialID], s.operation_id AS [OperationID],
       p.product_name AS [Продукция],
       ISNULL(m.material_name, o.operation_name) AS [Материал или операция],
       CASE WHEN m.material_id IS NOT NULL THEN N'Материал' ELSE N'Операция' END AS [Тип],
       CASE WHEN m.material_id IS NOT NULL THEN s.material_qty ELSE s.operation_qty END AS [Количество],
       ISNULL(m.unit_name, N'операция') AS [Ед. изм.],
       ISNULL(m.material_price, o.operation_price) AS [Цена]
FROM Specifications s
JOIN Products p ON s.product_id = p.product_id
LEFT JOIN Materials m ON s.material_id = m.material_id
LEFT JOIN Operations o ON s.operation_id = o.operation_id";

        public static TabPage CreateSpecificationsTab(string connectionString, bool canEdit)
        {
            TabPage page = new TabPage("Спецификации") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideColumns(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button add = UiHelper.CreateButton("Добавить", 100);
                Button edit = UiHelper.CreateButton("Изменить", 100);
                Button delete = UiHelper.CreateButton("Удалить", 100);
                add.Click += (s, e) => AddSpec(connectionString, grid, source);
                edit.Click += (s, e) => EditSpec(connectionString, grid, source);
                delete.Click += (s, e) => DeleteSpec(connectionString, grid, source);
                toolbar.Controls.Add(add);
                toolbar.Controls.Add(edit);
                toolbar.Controls.Add(delete);
            }

            Button refresh = UiHelper.CreateButton("Обновить", 100);
            refresh.Click += (s, e) => LoadGrid(connectionString, grid, source);
            toolbar.Controls.Add(refresh);
            UiHelper.AddStartsWithSearch(toolbar, source, "Продукция", "Поиск по продукции:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Спецификации", "Состав продукции: материалы и технологические операции"));
            LoadGrid(connectionString, grid, source);
            return page;
        }

        private static void LoadGrid(string connectionString, DataGridView grid, BindingSource source)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                using (SqlDataAdapter adapter = new SqlDataAdapter(Query, connection))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    UiHelper.BindTable(grid, source, table);
                    HideColumns(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки спецификаций: " + ex.Message);
            }
        }

        private static void HideColumns(DataGridView grid)
        {
            foreach (string name in new[] { "ID", "ProductID", "MaterialID", "OperationID" })
                if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
        }

        private static int SelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null) { MessageBox.Show("Выберите строку."); return 0; }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddSpec(string cs, DataGridView grid, BindingSource source)
        {
            using (SpecificationEditForm form = new SpecificationEditForm(cs, "Добавление спецификации"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                Save(cs, "INSERT INTO Specifications (product_id, material_id, operation_id, material_qty, operation_qty) VALUES (@product,@material,@operation,@materialQty,@operationQty)", form, 0);
                LoadGrid(cs, grid, source);
            }
        }

        private static void EditSpec(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedId(grid); if (id == 0) return;
            object product = grid.CurrentRow.Cells["ProductID"].Value;
            object material = grid.CurrentRow.Cells["MaterialID"].Value == DBNull.Value ? null : grid.CurrentRow.Cells["MaterialID"].Value;
            object operation = grid.CurrentRow.Cells["OperationID"].Value == DBNull.Value ? null : grid.CurrentRow.Cells["OperationID"].Value;
            decimal quantity = Convert.ToDecimal(grid.CurrentRow.Cells["Количество"].Value);
            using (SpecificationEditForm form = new SpecificationEditForm(cs, "Изменение спецификации", product, material, operation, quantity))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                Save(cs, "UPDATE Specifications SET product_id=@product, material_id=@material, operation_id=@operation, material_qty=@materialQty, operation_qty=@operationQty WHERE specification_id=@id", form, id);
                LoadGrid(cs, grid, source);
            }
        }

        private static void DeleteSpec(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedId(grid); if (id == 0) return;
            if (MessageBox.Show("Удалить выбранную спецификацию?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("DELETE FROM Specifications WHERE specification_id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open(); command.ExecuteNonQuery();
            }
            LoadGrid(cs, grid, source);
        }

        private static void Save(string cs, string sql, SpecificationEditForm form, int id)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                bool isMaterial = form.MaterialId != DBNull.Value;
                command.Parameters.AddWithValue("@product", form.ProductId);
                command.Parameters.AddWithValue("@material", form.MaterialId);
                command.Parameters.AddWithValue("@operation", form.OperationId);
                command.Parameters.AddWithValue("@materialQty", isMaterial ? form.Quantity : 0);
                command.Parameters.AddWithValue("@operationQty", isMaterial ? 0 : form.Quantity);
                if (id > 0) command.Parameters.AddWithValue("@id", id);
                connection.Open(); command.ExecuteNonQuery();
            }
        }
    }
}
