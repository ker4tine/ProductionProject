using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class CostTabHelper
    {
        public static TabPage CreateCostTab(string connectionString)
        {
            string query = @"
                SELECT c.name AS [Заказчик], p.name AS [Продукция], coi.quantity AS [Количество],
                       SUM(coi.quantity * (ISNULL(s.material_qty, 0) * ISNULL(m.price, 0) + ISNULL(s.operation_qty, 0) * ISNULL(o.price, 0))) AS [Полная стоимость]
                FROM CustomerOrders co
                JOIN Counterparties c ON co.customer_id = c.id
                JOIN CustomerOrderItems coi ON co.id = coi.order_id
                JOIN Products p ON coi.product_id = p.id
                JOIN Specifications s ON coi.product_id = s.product_id
                LEFT JOIN Materials m ON s.material_id = m.id
                LEFT JOIN Operations o ON s.operation_id = o.id
                GROUP BY c.name, p.name, coi.quantity";

            TabPage page = new TabPage("Расчет стоимости") { BackColor = UiHelper.LightBackground };
            BindingSource source = new BindingSource();
            DataGridView grid = UiHelper.CreateGrid();
            FlowLayoutPanel toolbar = UiHelper.CreateToolbar();

            Button btnRefresh = UiHelper.CreateButton("Обновить", 100);
            btnRefresh.Click += (s, e) => LoadGrid(connectionString, grid, source, query);
            toolbar.Controls.Add(btnRefresh);

            Panel tablePanel = UiHelper.CreateTablePanel(grid, source);
            Panel content = new Panel { Dock = DockStyle.Fill, BackColor = UiHelper.LightBackground, Padding = new Padding(16) };
            content.Controls.Add(tablePanel);
            content.Controls.Add(toolbar);

            page.Controls.Add(content);
            page.Controls.Add(UiHelper.CreateTopPanel("Расчет стоимости", "Итоговая стоимость заказов по спецификациям, материалам и операциям"));
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки расчета стоимости: " + ex.Message);
            }
        }
    }
}
