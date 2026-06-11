using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public class OrderEditForm : Form
    {
        private readonly string connectionString;
        private ComboBox cmbCustomer;
        private DateTimePicker dtpOrderDate;

        public int CustomerId { get; private set; }
        public DateTime OrderDate { get; private set; }

        public OrderEditForm(string connectionString, string title, int customerId = 0, DateTime? orderDate = null)
        {
            this.connectionString = connectionString;
            CustomerId = customerId;
            OrderDate = orderDate ?? DateTime.Today;
            BuildForm(title);
            LoadCustomers();
        }

        private void BuildForm(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(420, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Controls.Add(new Label { Text = "Заказчик:", Location = new Point(30, 35), AutoSize = true });
            cmbCustomer = new ComboBox { Location = new Point(140, 32), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(cmbCustomer);

            Controls.Add(new Label { Text = "Дата заказа:", Location = new Point(30, 78), AutoSize = true });
            dtpOrderDate = new DateTimePicker { Location = new Point(140, 75), Width = 220, Format = DateTimePickerFormat.Short, Value = OrderDate };
            Controls.Add(dtpOrderDate);

            Button btnOk = new Button { Text = "Сохранить", Location = new Point(95, 130), Width = 100 };
            btnOk.Click += BtnOk_Click;
            Button btnCancel = new Button { Text = "Отмена", Location = new Point(210, 130), Width = 100, DialogResult = DialogResult.Cancel };
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }

        private void LoadCustomers()
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand("SELECT id, name FROM Counterparties ORDER BY name", connection))
            {
                connection.Open();
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ComboItem item = new ComboItem(Convert.ToInt32(reader["id"]), reader["name"].ToString());
                        cmbCustomer.Items.Add(item);
                        if (CustomerId > 0 && item.Id == CustomerId)
                            cmbCustomer.SelectedItem = item;
                    }
                }
            }

            if (cmbCustomer.SelectedIndex < 0 && cmbCustomer.Items.Count > 0)
                cmbCustomer.SelectedIndex = 0;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            ComboItem customer = cmbCustomer.SelectedItem as ComboItem;
            if (customer == null)
            {
                MessageBox.Show("Выберите заказчика.");
                return;
            }

            CustomerId = customer.Id;
            OrderDate = dtpOrderDate.Value.Date;
            DialogResult = DialogResult.OK;
            Close();
        }

        private class ComboItem
        {
            public int Id { get; private set; }
            public string Name { get; private set; }

            public ComboItem(int id, string name)
            {
                Id = id;
                Name = name;
            }

            public override string ToString()
            {
                return Name;
            }
        }
    }
}
