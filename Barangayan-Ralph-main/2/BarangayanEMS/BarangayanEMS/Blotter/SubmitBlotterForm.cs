using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BarangayanEMS.Data;

namespace BarangayanEMS
{
    internal sealed class SubmitBlotterForm : Form
    {
        private Panel _root;
        private Panel _contentScrollHost;
        private Panel _footerBar;
        private Button _btnSubmit;
        private Button _btnCancel;

        // Field refs
        private ComboBox _cmbReportType;
        private ComboBox _cmbPriority;
        private TextBox _txtComplainant;
        private TextBox _txtRespondent;
        private DateTimePicker _dtpIncident;
        private ComboBox _cmbBarangay;
        private TextBox _txtLocation;
        private TextBox _txtDescription;
        private TextBox _txtWitnesses;

        internal SubmitBlotterForm()
        {
            Text = "Submit Blotter Report";
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);
            BackColor = Color.White;
            Size = new Size(560, 720);
            MinimizeBox = false;
            MaximizeBox = false;
            DoubleBuffered = true;

            // Root container
            _root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 16, 20, 16)
            };
            Controls.Add(_root);

            // Sticky footer
            _footerBar = BuildFooter();
            _footerBar.Dock = DockStyle.Bottom;
            _footerBar.Height = 60;
            _root.Controls.Add(_footerBar);

            // Scrollable content
            _contentScrollHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true
            };
            _root.Controls.Add(_contentScrollHost);

            var content = BuildFormContent();
            _contentScrollHost.Controls.Add(content);
        }

        private Panel BuildFormContent()
        {
            var content = new Panel
            {
                BackColor = Color.White,
                Location = new Point(0, 0),
                Width = _contentScrollHost.ClientSize.Width - 20,
                Height = 1400,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            int y = 0;

            content.Controls.Add(CreateHeading("Submit Blotter Report", new Font("Segoe UI Semibold", 18f), ref y));
            content.Controls.Add(CreateSubheading("Fill out the form below to submit a blotter report to the barangay authorities.", ref y));

            // Fields
            _cmbReportType = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            _cmbReportType.Items.AddRange(new object[]
            {
                "Theft",
                "Physical Injury",
                "Domestic Dispute",
                "Noise Complaint",
                "Vandalism",
                "Threat / Harassment",
                "Property Damage",
                "Other"
            });
            content.Controls.Add(CreateLabeledControl("Report Type *", _cmbReportType, ref y));

            _cmbPriority = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            _cmbPriority.Items.AddRange(new object[] { "Low", "Medium", "High", "Urgent" });
            content.Controls.Add(CreateLabeledControl("Priority Level *", _cmbPriority, ref y));

            _txtComplainant = new TextBox { Width = 360 };
            content.Controls.Add(CreateLabeledControl("Complainant Name *", _txtComplainant, ref y));

            _txtRespondent = new TextBox { Width = 360 };
            content.Controls.Add(CreateLabeledControl("Respondent Name *", _txtRespondent, ref y));

            _dtpIncident = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMM dd, yyyy",
                Width = 180
            };
            content.Controls.Add(CreateLabeledControl("Incident Date *", _dtpIncident, ref y));

            _cmbBarangay = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 };
            _cmbBarangay.Items.AddRange(CebuCityBarangays());
            content.Controls.Add(CreateLabeledControl("Barangay *", _cmbBarangay, ref y));

            _txtLocation = new TextBox { Width = 360 };
            content.Controls.Add(CreateLabeledControl("Incident Location *", _txtLocation, ref y));

            _txtDescription = new TextBox
            {
                Multiline = true,
                Width = 360,
                Height = 120,
                ScrollBars = ScrollBars.Vertical
            };
            content.Controls.Add(CreateLabeledControl("Description *", _txtDescription, ref y));

            _txtWitnesses = new TextBox { Width = 360 };
            content.Controls.Add(CreateLabeledControl("Witnesses (Optional)", _txtWitnesses, ref y));

            _contentScrollHost.Resize += (s, e) =>
            {
                content.Width = _contentScrollHost.ClientSize.Width - 20;
            };

            return content;
        }

        private Panel BuildFooter()
        {
            var footer = new Panel
            {
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(12, 10, 12, 10)
            };

            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            footer.Controls.Add(rightFlow);

            _btnCancel = new Button
            {
                Text = "Cancel",
                AutoSize = true,
                Height = 34,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(55, 65, 81),
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 10, 0)
            };
            _btnCancel.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            _btnCancel.FlatAppearance.BorderSize = 1;
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            _btnSubmit = new Button
            {
                Text = "Submit Report",
                AutoSize = true,
                Height = 34,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0)
            };
            _btnSubmit.FlatAppearance.BorderSize = 0;
            _btnSubmit.Click += HandleSubmit;

            rightFlow.Controls.Add(_btnCancel);
            rightFlow.Controls.Add(_btnSubmit);

            return footer;
        }

        private void HandleSubmit(object sender, EventArgs e)
        {
            // Validate required
            string[] errors = new[]
            {
                _cmbReportType.SelectedItem == null ? "Select a Report Type." : null,
                _cmbPriority.SelectedItem == null ? "Select a Priority Level." : null,
                string.IsNullOrWhiteSpace(_txtComplainant.Text) ? "Enter Complainant Name." : null,
                string.IsNullOrWhiteSpace(_txtRespondent.Text) ? "Enter Respondent Name." : null,
                _cmbBarangay.SelectedItem == null ? "Select a Barangay." : null,
                string.IsNullOrWhiteSpace(_txtLocation.Text) ? "Enter Incident Location." : null,
                string.IsNullOrWhiteSpace(_txtDescription.Text) ? "Enter Description." : null,
            }.Where(x => x != null).ToArray();

            if (errors.Length > 0)
            {
                MessageBox.Show(string.Join("\n", errors), "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var repo = new BlotterRepository();
            var rec = new BlotterRepository.BlotterRecord
            {
                ReportType = _cmbReportType.SelectedItem.ToString(),
                PriorityLevel = _cmbPriority.SelectedItem.ToString(),
                Barangay = _cmbBarangay.SelectedItem.ToString(),
                Complainant = _txtComplainant.Text.Trim(),
                Respondent = _txtRespondent.Text.Trim(),
                IncidentDate = _dtpIncident.Value.Date,
                IncidentLocation = _txtLocation.Text.Trim(),
                Description = _txtDescription.Text.Trim(),
                Witnesses = _txtWitnesses.Text.Trim(),
                Status = "Pending",
                DateReported = DateTime.Today
            };

            try
            {
                string caseNo = repo.Insert(rec);
                MessageBox.Show("Blotter report submitted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                // Reset form fields
                _cmbReportType.SelectedIndex = -1;
                _cmbPriority.SelectedIndex = -1;
                _txtComplainant.Text = string.Empty;
                _txtRespondent.Text = string.Empty;
                _dtpIncident.Value = DateTime.Today;
                _cmbBarangay.SelectedIndex = -1;
                _txtLocation.Text = string.Empty;
                _txtDescription.Text = string.Empty;
                _txtWitnesses.Text = string.Empty;
                // Signal parent to refresh
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to submit report.\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string[] CebuCityBarangays()
        {
            // Partial list for brevity; can be expanded as needed
            return new[]
            {
                "Lahug",
                "Mabolo",
                "Guadalupe",
                "Talamban",
                "Banilad",
                "Apas",
                "Busay",
                "Kamputhaw",
                "Capitol Site",
                "Tisa",
                "Labangon",
                "Sawang Calero",
                "Pahina Central",
                "Sambag I",
                "Sambag II",
                "Tinago",
                "Carreta",
                "Hipodromo",
                "Pari-an",
                "San Nicolas Proper",
                "Inayawan",
                "Pardo",
                "Bulacao",
                "Basak San Nicolas",
                "Basak Pardo",
                "Mambaling",
                "Kinasang-an",
                "Punta Princesa",
                "Calamba",
                "Cogon Ramos",
                "Buhisan",
                "To-ong",
                "Pamutan",
                "Sinsin",
            };
        }

        private Control CreateHeading(string text, Font font, ref int y)
        {
            var lbl = new Label
            {
                AutoSize = true,
                Text = text,
                Font = font,
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(0, y)
            };
            y = lbl.Bottom + 6;
            return lbl;
        }

        private Control CreateSubheading(string text, ref int y)
        {
            var lbl = new Label
            {
                AutoSize = true,
                Text = text,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(0, y)
            };
            y = lbl.Bottom + 16;
            return lbl;
        }

        private Control CreateLabeledControl(string label, Control input, ref int y)
        {
            var host = new Panel
            {
                BackColor = Color.Transparent,
                Location = new Point(0, y),
                Size = new Size(500, input.Height + 32)
            };

            var lbl = new Label
            {
                AutoSize = true,
                Text = label,
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(0, 0)
            };
            host.Controls.Add(lbl);

            input.Location = new Point(0, lbl.Bottom + 6);
            host.Controls.Add(input);

            y = host.Bottom + 12;
            return host;
        }
    }
}