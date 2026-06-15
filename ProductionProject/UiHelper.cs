using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ProductionProject
{
    public static class UiHelper
    {
        public static readonly Color Primary = Color.FromArgb(37, 99, 235);
        public static readonly Color LightBackground = Color.FromArgb(243, 246, 251);
        public static readonly Color HeaderBackground = Color.FromArgb(239, 246, 255);
        public static readonly Color HeaderText = Color.FromArgb(30, 64, 175);

        public static void ApplyFormStyle(Form form)
        {
            form.BackColor = LightBackground;
            form.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        }

        public static Panel CreateTopPanel(string title, string subtitle)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                BackColor = Primary,
                Padding = new Padding(22, 12, 22, 10)
            };

            Label titleLabel = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                Dock = DockStyle.Top,
                Height = 32
            };

            Label subtitleLabel = new Label
            {
                Text = subtitle,
                ForeColor = Color.FromArgb(219, 234, 254),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                Dock = DockStyle.Top,
                Height = 24
            };

            panel.Controls.Add(subtitleLabel);
            panel.Controls.Add(titleLabel);
            return panel;
        }

        public static FlowLayoutPanel CreateToolbar()
        {
            return new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White,
                Padding = new Padding(8, 7, 8, 6),
                AutoScroll = false
            };
        }

        public static Button CreateButton(string text, int width)
        {
            Button button = new Button
            {
                Text = text,
                Width = width,
                Height = 28,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = HeaderText,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Margin = new Padding(3, 2, 3, 2)
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(191, 219, 254);
            return button;
        }

        public static DataGridView CreateGrid()
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
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                EnableHeadersVisualStyles = false,
                GridColor = Color.FromArgb(229, 231, 235)
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = HeaderBackground;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = HeaderText;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grid.ColumnHeadersHeight = 36;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            return grid;
        }

        public static BindingNavigator CreateNavigator(BindingSource source)
        {
            BindingNavigator navigator = new BindingNavigator(true)
            {
                Dock = DockStyle.Bottom,
                BindingSource = source,
                BackColor = Color.White,
                GripStyle = ToolStripGripStyle.Hidden
            };
            return navigator;
        }

        public static Panel CreateTablePanel(DataGridView grid, BindingSource source)
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(8)
            };
            panel.Controls.Add(grid);
            panel.Controls.Add(CreateNavigator(source));
            return panel;
        }

        public static void BindTable(DataGridView grid, BindingSource source, DataTable table)
        {
            source.DataSource = table;
            grid.DataSource = source;
        }
    }
}
