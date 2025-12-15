using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace BarangayanEMS
{
    internal sealed class BlotterReportForm : Form
    {
        private readonly List<BlotterReport> _items = new List<BlotterReport>();
        private readonly BindingSource _binding = new BindingSource();
        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;
        private BlotterReport _selected;

        internal BlotterReportForm()
        {
            Text = "Blotter Report Status";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Size = new Size(980, 620);
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;

            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24)
            };
            Controls.Add(content);

            Panel header = new Panel { Dock = DockStyle.Top, Height = 92, BackColor = Color.White };
            content.Controls.Add(header);
            header.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Republika ng Pilipinas\r\nBarangayan E-Management System",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(90, 90, 90),
                Location = new Point(0, 0)
            });
            header.Controls.Add(new Label
            {
                AutoSize = true,
                Text = "Blotter Report Status",
                Font = new Font("Segoe UI Semibold", 20f),
                ForeColor = Color.FromArgb(48, 48, 48),
                Location = new Point(0, 50)
            });
            content.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });

            Panel actionsRow = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = Color.White };
            content.Controls.Add(actionsRow);
            FlowLayoutPanel actionBar = new FlowLayoutPanel
            {
                Location = new Point(0, 0), Size = new Size(600, 46), AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false
            };
            actionsRow.Controls.Add(actionBar);

            Button btnNew = CreateActionButton("+ New Report", Color.FromArgb(56, 178, 89));
            btnNew.Click += (s, e) => NewReport();
            actionBar.Controls.Add(btnNew);

            Button btnEdit = CreateActionButton("Edit Report", Color.FromArgb(44, 124, 228));
            btnEdit.Click += (s, e) => EditSelected();
            actionBar.Controls.Add(btnEdit);

            Button btnView = CreateActionButton("View Details", Color.FromArgb(245, 158, 11));
            btnView.Click += (s, e) => ViewSelected();
            actionBar.Controls.Add(btnView);

            Button btnDelete = CreateActionButton("Delete", Color.FromArgb(224, 64, 64));
            btnDelete.Click += (s, e) => DeleteSelected();
            actionBar.Controls.Add(btnDelete);

            _txtSearch = new TextBox { Anchor = AnchorStyles.Top | AnchorStyles.Right, Width = 260, Height = 30, Font = new Font("Segoe UI", 9f), ForeColor = Color.Gray, Text = "Search" };
            actionsRow.Controls.Add(_txtSearch);
            _txtSearch.Location = new Point(actionsRow.Width - _txtSearch.Width, 8);
            actionsRow.Resize += (s, e) => _txtSearch.Location = new Point(actionsRow.Width - _txtSearch.Width, 8);
            _txtSearch.GotFocus += (s, e) => { if (_txtSearch.Text == "Search") { _txtSearch.Text = string.Empty; _txtSearch.ForeColor = Color.Black; } };
            _txtSearch.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtSearch.Text)) { _txtSearch.Text = "Search"; _txtSearch.ForeColor = Color.Gray; } };
            _txtSearch.TextChanged += (s, e) => { if (_txtSearch.Focused || _txtSearch.Text != "Search") ApplyFilter(_txtSearch.Text); };

            content.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });

            Panel gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            content.Controls.Add(gridHost);

            _grid = new DataGridView
            {
                Parent = gridHost,
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                MultiSelect = false
            };
            _grid.RowTemplate.Height = 40;
            _grid.DefaultCellStyle.Font = new Font("Segoe UI", 10f);
            _grid.DefaultCellStyle.Padding = new Padding(6, 8, 6, 8);
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            _grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(17, 24, 39);
            _grid.EnableHeadersVisualStyles = false;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10f);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 250);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 65, 81);
            _grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 8, 6, 8);

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlotterReport.CaseNo), HeaderText = "Case No.", Width = 100 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlotterReport.IncidentType), HeaderText = "Incident Type", Width = 170 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlotterReport.Complainant), HeaderText = "Complainant", Width = 170 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlotterReport.DateReported), HeaderText = "Date Reported", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlotterReport.Status), HeaderText = "Status", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(BlotterReport.Actions), HeaderText = "Actions", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            _grid.SelectionChanged += (s, e) => UpdateSelection();
            _grid.CellDoubleClick += (s, e) => EditSelected();

            Seed();
        }

        private Button CreateActionButton(string text, Color bg)
        {
            return new Button { Text = text, AutoSize = true, Height = 36, BackColor = bg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Margin = new Padding(0, 0, 12, 0) };
        }

        private void Seed()
        {
            var rng = new Random();
            _items.Clear();
            string[] types = { "Theft", "Physical Injury", "Vandalism", "Harassment" };
            for (int i = 1; i <= 25; i++)
            {
                _items.Add(new BlotterReport
                {
                    CaseNo = $"BL-{(1000 + i):D4}",
                    IncidentType = types[rng.Next(types.Length)],
                    Complainant = $"Complainant {rng.Next(20, 100)}",
                    DateReported = DateTime.Today.AddDays(-rng.Next(0, 30)),
                    Status = (i % 5 == 0) ? "Under Investigation" : (i % 7 == 0) ? "For Pickup" : "Pending",
                    Actions = "View Details, Edit Status"
                });
            }
            _binding.DataSource = _items;
            _grid.DataSource = _binding;
            UpdateSelection();
        }

        private void ApplyFilter(string q)
        {
            _binding.DataSource = string.IsNullOrWhiteSpace(q)
                ? _items
                : _items.Where(x => (x.CaseNo ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                 || (x.IncidentType ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                                 || (x.Complainant ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void UpdateSelection()
        {
            _selected = _grid.SelectedRows.Count > 0 ? _grid.SelectedRows[0].DataBoundItem as BlotterReport : null;
        }

        private void NewReport()
        {
            using (var f = new SubmitBlotterForm())
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }

        private void EditSelected()
        {
            if (_selected == null) return;
            using (var f = new SubmitBlotterForm())
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }

        private void ViewSelected()
        {
            if (_selected == null) return;
            MessageBox.Show($"Case: {_selected.CaseNo}\nType: {_selected.IncidentType}\nComplainant: {_selected.Complainant}\nDate: {_selected.DateReported:yyyy-MM-dd}\nStatus: {_selected.Status}", "Blotter Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void DeleteSelected()
        {
            if (_selected == null) return;
            _items.Remove(_selected);
            ApplyFilter(_txtSearch.Text);
        }
    }

    internal sealed class BlotterReport
    {
        public string CaseNo { get; set; }
        public string IncidentType { get; set; }
        public string Complainant { get; set; }
        public DateTime DateReported { get; set; }
        public string Status { get; set; }
        public string Actions { get; set; }
    }
}
