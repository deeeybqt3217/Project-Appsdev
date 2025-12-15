using System;
using System.Drawing;
using System.Windows.Forms;

namespace BarangayanEMS
{
    internal sealed class SubmitBlotterForm : Form
    {
        private Panel _root;
        private Panel _contentScrollHost;
        private Panel _footerBar;
        private Button _btnSubmit;
        private Button _btnCancel;

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
            content.Controls.Add(CreateLabeledControl("Report Type *", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 }, ref y));
            content.Controls.Add(CreateLabeledControl("Priority Level *", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 }, ref y));

            content.Controls.Add(CreateLabeledControl("Complainant Name *", new TextBox { Width = 360 }, ref y));
            content.Controls.Add(CreateLabeledControl("Respondent Name *", new TextBox { Width = 360 }, ref y));

            var dtpIncident = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMM dd, yyyy",
                Width = 180
            };
            content.Controls.Add(CreateLabeledControl("Incident Date *", dtpIncident, ref y));

            content.Controls.Add(CreateLabeledControl("Barangay *", new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 240 }, ref y));
            content.Controls.Add(CreateLabeledControl("Incident Location *", new TextBox { Width = 360 }, ref y));

            var txtDescription = new TextBox
            {
                Multiline = true,
                Width = 360,
                Height = 120,
                ScrollBars = ScrollBars.Vertical
            };
            content.Controls.Add(CreateLabeledControl("Description *", txtDescription, ref y));

            content.Controls.Add(CreateLabeledControl("Witnesses (Optional)", new TextBox { Width = 360 }, ref y));

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
            _btnCancel.Click += (s, e) => Close();

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
            // Hook to your validation and persistence
            MessageBox.Show("Blotter report submitted.", "Submit", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Close();
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