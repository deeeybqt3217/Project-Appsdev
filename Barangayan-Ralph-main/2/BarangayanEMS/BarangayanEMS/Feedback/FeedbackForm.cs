using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BarangayanEMS.Data;

namespace BarangayanEMS
{
    internal sealed class FeedbackForm : Form
    {
        private readonly string _userDisplayName;
        private readonly FeedbackRepository _repo = new FeedbackRepository();

        private Label _lblTitle;
        private Label _lblSubtitle;
        private Button _btnClose;

        private Label _lblType;
        private ComboBox _cmbType;
        private Panel _cmbHost;

        private Label _lblMessage;
        private TextBox _txtMessage;
        private Panel _txtHost;

        private Button _btnSubmit;

        public FeedbackForm(string userDisplayName)
        {
            _userDisplayName = string.IsNullOrWhiteSpace(userDisplayName) ? "Resident" : userNameTrim(userDisplayName);

            // Form chrome
            Text = "Feedback";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 247, 250); // light neutral background
            ShowInTaskbar = false;
            KeyPreview = true;
            Font = new Font("Segoe UI", 9f);
            Size = new Size(540, 310);
            DoubleBuffered = true;

            // Root container (acts as the card) with padding
            var root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 20, 20, 20)
            };
            // Rounded card with subtle border
            root.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, root.Width - 1, root.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 16))
                using (SolidBrush fill = new SolidBrush(root.BackColor))
                using (Pen border = new Pen(Color.FromArgb(220, 224, 235)))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            };
            root.Resize += (s, e) => root.Region = new Region(RoundedRect(new Rectangle(0, 0, root.Width, root.Height), 16));
            Controls.Add(root);

            // Header
            _lblTitle = new Label
            {
                AutoSize = true,
                Text = "Feedback System",
                Font = new Font("Segoe UI Semibold", 14.5f), // slightly larger and bold
                ForeColor = Color.FromArgb(35, 35, 35),
                Location = new Point(0, 0)
            };
            root.Controls.Add(_lblTitle);

            _btnClose = new Button
            {
                Text = "?",
                AutoSize = false,
                Size = new Size(28, 28),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(90, 90, 90),
                TabStop = false,
                Location = new Point(Width - 18 - 28, 8),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 248);
            _btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 235, 240);
            _btnClose.Click += (s, e) => Close();
            root.Controls.Add(_btnClose);

            _lblSubtitle = new Label
            {
                AutoSize = false,
                Size = new Size(Width - 36, 36),
                Text = "Share your suggestions, complaints, or feedback to help us improve our services.",
                Font = new Font("Segoe UI", 9.2f),
                ForeColor = Color.FromArgb(105, 105, 120),
                Location = new Point(0, _lblTitle.Bottom + 4)
            };
            root.Controls.Add(_lblSubtitle);

            // Type field
            _lblType = new Label
            {
                AutoSize = true,
                Text = "Feedback Type",
                Font = new Font("Segoe UI Semibold", 9.6f),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(0, _lblSubtitle.Bottom + 10)
            };
            root.Controls.Add(_lblType);

            _cmbHost = CreateSoftInputHost(new Size(Width - 36, 40), Color.White, Color.FromArgb(230, 232, 240));
            _cmbHost.Location = new Point(0, _lblType.Bottom + 6);
            root.Controls.Add(_cmbHost);

            _cmbType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(140, 144, 150), // placeholder gray
                BackColor = Color.White,
                IntegralHeight = false,
                Width = _cmbHost.Width - 24,
                Location = new Point(12, 8),
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 26
            };
            _cmbType.Items.Add("Select feedback type");
            _cmbType.Items.AddRange(new object[] { "Suggestion", "Complaint", "Bug Report", "Other" });
            _cmbType.SelectedIndex = 0;
            _cmbType.SelectedIndexChanged += (s, e) =>
            {
                _cmbType.ForeColor = _cmbType.SelectedIndex == 0 ? Color.FromArgb(140, 144, 150) : Color.FromArgb(31, 41, 55);
            };
            _cmbType.DrawItem += CmbType_DrawItem; // custom draw to remove default blue highlight
            _cmbHost.Controls.Add(_cmbType);

            // Message field
            _lblMessage = new Label
            {
                AutoSize = true,
                Text = "Your Feedback",
                Font = new Font("Segoe UI Semibold", 9.6f),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(0, _cmbHost.Bottom + 10)
            };
            root.Controls.Add(_lblMessage);

            _txtHost = CreateSoftInputHost(new Size(Width - 36, 92), Color.FromArgb(250, 251, 253), Color.FromArgb(230, 232, 240));
            _txtHost.Location = new Point(0, _lblMessage.Bottom + 6);
            root.Controls.Add(_txtHost);

            _txtMessage = new TextBox
            {
                Multiline = true,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(150, 152, 160), // softer placeholder
                BackColor = Color.FromArgb(250, 251, 253), // off-white
                Location = new Point(12, 12),
                Size = new Size(_txtHost.Width - 24, _txtHost.Height - 24),
                Text = "Share your thoughts…"
            };
            _txtMessage.GotFocus += (s, e) =>
            {
                if (_txtMessage.ForeColor.ToArgb() == Color.FromArgb(150, 152, 160).ToArgb())
                {
                    _txtMessage.Text = string.Empty;
                    _txtMessage.ForeColor = Color.FromArgb(31, 41, 55);
                }
            };
            _txtMessage.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtMessage.Text))
                {
                    _txtMessage.Text = "Share your thoughts…";
                    _txtMessage.ForeColor = Color.FromArgb(150, 152, 160);
                }
            };
            _txtHost.Controls.Add(_txtMessage);

            // Submit button
            _btnSubmit = new Button
            {
                Text = "Submit Feedback",
                AutoSize = false,
                Size = new Size(Width - 36, 40),
                Location = new Point(0, _txtHost.Bottom + 12),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(24, 24, 32),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            _btnSubmit.FlatAppearance.BorderSize = 0;
            _btnSubmit.MouseEnter += (s, e) => _btnSubmit.BackColor = Color.FromArgb(34, 34, 44);
            _btnSubmit.MouseDown += (s, e) => _btnSubmit.BackColor = Color.FromArgb(18, 18, 26);
            _btnSubmit.MouseLeave += (s, e) => _btnSubmit.BackColor = Color.FromArgb(24, 24, 32);
            _btnSubmit.Resize += (s, e) => _btnSubmit.Region = new Region(RoundedRect(new Rectangle(0, 0, _btnSubmit.Width, _btnSubmit.Height), 10));
            _btnSubmit.Click += Submit_Click;
            root.Controls.Add(_btnSubmit);

            AcceptButton = _btnSubmit;

            // Allow dragging by header area
            _lblTitle.MouseDown += Drag_MouseDown;
            _lblSubtitle.MouseDown += Drag_MouseDown;
            root.MouseDown += Drag_MouseDown;

            // Esc to close
            KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) Close(); };
        }

        private static string userNameTrim(string value) => value.Trim();

        private void Submit_Click(object sender, EventArgs e)
        {
            string type = _cmbType.SelectedIndex > 0 ? _cmbType.SelectedItem as string : null;
            string msg = (_txtMessage.ForeColor.ToArgb() == Color.FromArgb(150, 152, 160).ToArgb()) ? string.Empty : (_txtMessage.Text ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(type))
            {
                MessageBox.Show("Please select a feedback type.", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _cmbType.Focus();
                return;
            }
            if (string.IsNullOrWhiteSpace(msg))
            {
                MessageBox.Show("Please enter your feedback message.", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _txtMessage.Focus();
                return;
            }

            try
            {
                _repo.Insert(type, msg, _userDisplayName);
                MessageBox.Show("Thank you for your feedback!", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save feedback. " + ex.Message, "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Custom draw for combo box to remove blue highlight and use soft gray selection
        private void CmbType_DrawItem(object sender, DrawItemEventArgs e)
        {
            var cmb = sender as ComboBox;
            e.DrawBackground();

            if (e.Index < 0)
            {
                // Draw the selected text area when not dropped down
                string text = cmb.SelectedIndex >= 0 ? cmb.Items[cmb.SelectedIndex].ToString() : string.Empty;
                bool isPlaceholder = cmb.SelectedIndex == 0;
                Color textColor = isPlaceholder ? Color.FromArgb(140, 144, 150) : Color.FromArgb(31, 41, 55);
                using (SolidBrush bg = new SolidBrush(Color.White))
                using (SolidBrush tb = new SolidBrush(textColor))
                {
                    e.Graphics.FillRectangle(bg, e.Bounds);
                    TextRenderer.DrawText(e.Graphics, text, cmb.Font, e.Bounds, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
                }
                return;
            }

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            string itemText = cmb.Items[e.Index].ToString();
            bool placeholder = e.Index == 0;

            Color back = selected ? Color.FromArgb(240, 242, 247) : Color.White;
            Color textCol = placeholder ? Color.FromArgb(140, 144, 150) : Color.FromArgb(31, 41, 55);

            using (SolidBrush bg = new SolidBrush(back))
            using (SolidBrush tb = new SolidBrush(textCol))
            {
                e.Graphics.FillRectangle(bg, e.Bounds);
                var textRect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height);
                TextRenderer.DrawText(e.Graphics, itemText, cmb.Font, textRect, textCol, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            }
            // Skip default focus rectangle for a cleaner look
        }

        // Soft rounded input host (light background + subtle border)
        private Panel CreateSoftInputHost(Size size)
        {
            return CreateSoftInputHost(size, Color.White, Color.FromArgb(230, 232, 240));
        }

        private Panel CreateSoftInputHost(Size size, Color fillColor, Color borderColor)
        {
            var host = new Panel
            {
                Size = size,
                BackColor = Color.Transparent
            };
            host.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, host.Width - 1, host.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 12))
                using (SolidBrush fill = new SolidBrush(fillColor))
                using (Pen border = new Pen(borderColor))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            };
            host.Resize += (s, e) => host.Region = new Region(RoundedRect(new Rectangle(0, 0, host.Width, host.Height), 12));
            return host;
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Dragging support for borderless form
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private void Drag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        // Subtle drop shadow for borderless window
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                var cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }
    }
}
