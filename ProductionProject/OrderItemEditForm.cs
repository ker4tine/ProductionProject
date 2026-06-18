using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class OrderItemEditForm : Form
    {
        private readonly string connectionString;
        private ComboBox cmbOrder;
        private ComboBox cmbProduct;
        private NumericUpDown nudQuantity;

        public int OrderId { get; private set; }
        public int ProductId { get; private set; }
        public decimal Quantity { get; private set; }

        public OrderItemEditForm(string connectionString, string title, int orderId = 0, int productId = 0, decimal quantity = 1)
        {
            this.connectionString = connectionString;
            OrderId = orderId;
            ProductId = productId;
            Quantity = quantity <= 0 ? 1 : quantity;
            BuildForm(title);
            LoadOrders();
            LoadProducts();
        }

        private void BuildForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(460, 270);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Заказ:", Location = new Point(30, 35), AutoSize = true });
            cmbOrder = new ComboBox { Location = new Point(140, 32), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbOrder);

            Controls.Add(new Label { Text = "Продукция:", Location = new Point(30, 78), AutoSize = true });
            cmbProduct = new ComboBox { Location = new Point(140, 75), Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbProduct);

            Controls.Add(new Label { Text = "Количество:", Location = new Point(30, 121), AutoSize = true });
            nudQuantity = new NumericUpDown { Location = new Point(140, 118), Width = 260, Minimum = 1, Maximum = 100000, DecimalPlaces = 2, Value = Quantity };
            Controls.Add(nudQuantity);

            Button ok = new Button { Text = "Сохранить", Location = new Point(115, 180), Width = 100 };
            ok.Click += BtnOk_Click;
            Controls.Add(ok);
            Controls.Add(new Button { Text = "Отмена", Location = new Point(230, 180), Width = 100, DialogResult = DialogResult.Cancel });
        }

        private void LoadOrders()
        {
            const string sql = "SELECT co.customer_order_id, c.counterparty_name + N' от ' + CONVERT(nvarchar(10), co.order_date, 104) AS display_name FROM CustomerOrders co JOIN Counterparties c ON co.customer_id = c.counterparty_id ORDER BY co.order_date DESC, co.customer_order_id DESC";
            BindCombo(cmbOrder, sql, "display_name", "customer_order_id", OrderId);
        }

        private void LoadProducts()
        {
            const string sql = "SELECT product_id, product_name FROM Products ORDER BY product_name";
            BindCombo(cmbProduct, sql, "product_name", "product_id", ProductId);
        }

        private void BindCombo(ComboBox combo, string sql, string displayMember, string valueMember, int selectedValue)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connection))
            {
                DataTable table = new DataTable();
                adapter.Fill(table);
                combo.DisplayMember = displayMember;
                combo.ValueMember = valueMember;
                combo.DataSource = table;
                if (selectedValue > 0) combo.SelectedValue = selectedValue;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cmbOrder.SelectedValue == null || cmbProduct.SelectedValue == null)
            {
                MessageBox.Show("Выберите заказ и продукцию.");
                return;
            }

            OrderId = Convert.ToInt32(cmbOrder.SelectedValue);
            ProductId = Convert.ToInt32(cmbProduct.SelectedValue);
            Quantity = nudQuantity.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
