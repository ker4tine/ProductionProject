using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class ProductionOrderCrudHelper
    {
        private const string Query = @"
SELECT po.production_order_id AS [ID],
       po.customer_order_id AS [CustomerOrderID],
       po.product_id AS [ProductID],
       ISNULL(c.counterparty_name, N'Без заказчика') AS [Заказчик],
       p.product_name AS [Продукция],
       po.quantity AS [Количество],
       p.unit_name AS [Ед. изм.],
       po.production_date AS [Дата производства]
FROM ProductionOrders po
LEFT JOIN CustomerOrders co ON po.customer_order_id = co.customer_order_id
LEFT JOIN Counterparties c ON co.customer_id = c.counterparty_id
JOIN Products p ON po.product_id = p.product_id
ORDER BY po.production_date DESC, po.production_order_id DESC";

        public static TabPage CreateProductionOrdersTab(string connectionString, bool canEdit)
        {
            TabPage page = new TabPage("Производственные заказы") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            grid.DataBindingComplete += (s, e) => HideColumns(grid);
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            if (canEdit)
            {
                Button add = UiHelper.CreateButton("Добавить", 100);
                Button edit = UiHelper.CreateButton("Изменить", 100);
                Button delete = UiHelper.CreateButton("Удалить", 100);
                add.Click += (s, e) => AddOrder(connectionString, grid, source);
                edit.Click += (s, e) => EditOrder(connectionString, grid, source);
                delete.Click += (s, e) => DeleteOrder(connectionString, grid, source);
                toolbar.Controls.Add(add);
                toolbar.Controls.Add(edit);
                toolbar.Controls.Add(delete);
            }

            Button refresh = UiHelper.CreateButton("Обновить", 100);
            refresh.Click += (s, e) => LoadGrid(connectionString, grid, source);
            toolbar.Controls.Add(refresh);
            UiHelper.AddStartsWithSearch(toolbar, source, "Заказчик", "Поиск по заказчику:");

            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(UiHelper.CreateTablePanel(grid, source));
            content.Controls.Add(toolbar);
            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Производственные заказы", "Планирование выпуска продукции"));
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
                    HideColumns(grid);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки производственных заказов: " + ex.Message);
            }
        }

        private static void HideColumns(DataGridView grid)
        {
            foreach (string name in new[] { "ID", "CustomerOrderID", "ProductID" })
                if (grid.Columns.Contains(name)) grid.Columns[name].Visible = false;
        }

        private static int SelectedId(DataGridView grid)
        {
            if (grid.CurrentRow == null)
            {
                MessageBox.Show("Выберите производственный заказ.");
                return 0;
            }
            return Convert.ToInt32(grid.CurrentRow.Cells["ID"].Value);
        }

        private static void AddOrder(string cs, DataGridView grid, BindingSource source)
        {
            using (ProductionOrderEditForm form = new ProductionOrderEditForm(cs, "Добавление производственного заказа"))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                Save(cs,
                    "INSERT INTO ProductionOrders (customer_order_id, product_id, quantity, production_date) VALUES (@customerOrder,@product,@quantity,@date)",
                    form, 0);
                LoadGrid(cs, grid, source);
            }
        }

        private static void EditOrder(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedId(grid);
            if (id == 0) return;

            object customerOrderId = grid.CurrentRow.Cells["CustomerOrderID"].Value;
            if (customerOrderId == DBNull.Value) customerOrderId = null;
            int productId = Convert.ToInt32(grid.CurrentRow.Cells["ProductID"].Value);
            decimal quantity = Convert.ToDecimal(grid.CurrentRow.Cells["Количество"].Value);
            DateTime productionDate = Convert.ToDateTime(grid.CurrentRow.Cells["Дата производства"].Value);

            using (ProductionOrderEditForm form = new ProductionOrderEditForm(
                cs, "Изменение производственного заказа", customerOrderId, productId, quantity, productionDate))
            {
                if (form.ShowDialog() != DialogResult.OK) return;
                Save(cs,
                    "UPDATE ProductionOrders SET customer_order_id=@customerOrder, product_id=@product, quantity=@quantity, production_date=@date WHERE production_order_id=@id",
                    form, id);
                LoadGrid(cs, grid, source);
            }
        }

        private static void DeleteOrder(string cs, DataGridView grid, BindingSource source)
        {
            int id = SelectedId(grid);
            if (id == 0) return;
            if (MessageBox.Show("Удалить выбранный производственный заказ?", "Подтверждение", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand("DELETE FROM ProductionOrders WHERE production_order_id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
            LoadGrid(cs, grid, source);
        }

        private static void Save(string cs, string sql, ProductionOrderEditForm form, int id)
        {
            using (SqlConnection connection = new SqlConnection(cs))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@customerOrder", form.CustomerOrderId ?? DBNull.Value);
                command.Parameters.AddWithValue("@product", form.ProductId);
                command.Parameters.AddWithValue("@quantity", form.Quantity);
                command.Parameters.AddWithValue("@date", form.ProductionDate);
                if (id > 0) command.Parameters.AddWithValue("@id", id);
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
    }
}
