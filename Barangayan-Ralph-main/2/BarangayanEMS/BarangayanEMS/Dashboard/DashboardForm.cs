using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Reflection;
using System.IO;
using BarangayanEMS;    
using BarangayanEMS.Data; // added for repository access

namespace BarangayanEMS
{
    public partial class DashboardForm : Form
    {
        // ---------- PAGE ENUM ----------
        private enum DashboardPage
        {
            Dashboard,
            Services,
            Requirements,
            Feedback,
            About
        }

        // ---------- NAVIGATION STATE ----------
        private readonly Dictionary<Panel, DashboardPage> _navMap =
            new Dictionary<Panel, DashboardPage>();

        private Panel _activeNavPanel;
        private DashboardPage _activePage = DashboardPage.Dashboard;

        // Host where pages will slide in/out
        private Panel _contentHost;

        // Pages (created only here, not in Designer)
        private Panel _pageDashboard;
        private Panel _pageServices;
        private Panel _pageRequirements;
        private Panel _pageFeedback;
        private Panel _pageAbout;
        private readonly string _userDisplayName;

        // Keep a reference to the dashboard welcome label to enforce static behavior
        private Label _contentWelcomeLabel;

        // ---------- SLIDE ANIMATION ----------
        private Timer _slideTimer;
        private Control _slideFrom;
        private Control _slideTo;
        private int _slideStep;
        private const int SlideSteps = 18;
        private bool _isSliding; // guard to prevent overlapping animations

        // Sidebar accent color used for active state
        private static readonly Color NavAccent = Color.FromArgb(77, 109, 242);

        protected override CreateParams CreateParams
        {
            get 
            {
                // Reduce flicker by enabling composited painting for child controls
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }

        private static void EnableDoubleBuffer(Control control)
        {
            if (control == null) return;
            try
            {
                // Use reflection to set the protected DoubleBuffered property on arbitrary controls
                typeof(Control).InvokeMember("DoubleBuffered",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                    null, control, new object[] { true });
            }
            catch { /* ignore */ }
        }

        public DashboardForm(string userName = "Juan")
        {
            InitializeComponent();        // Designer builds base layout
            _userDisplayName = string.IsNullOrWhiteSpace(userName) ? "Resident" : userName.Trim();

            // Set header and make it static (non-hovering, non-clickable)
            UpdateHeaderGreeting();
            MakeHeaderStatic();           

            ResolveCoreControls();        
            BuildPages(_userDisplayName); 
            SetupNavigation();            
            HookServicesNavFallback();    
            SetupSearchBox();              
            SetupSlideTimer();            

            ShowPage(DashboardPage.Dashboard, immediate: true);
        }

        // Keep header purely informational (no hover/click/background changes)
        private void MakeHeaderStatic()
        {
            // Replace any interactive header label with a fresh, non-interactive one
            Control[] found = this.Controls.Find("lblMainTitle", true);
            var old = (found.Length > 0) ? found[0] as Label : null;
            if (old == null) return;

            Label replacement = new Label
            {
                AutoSize = old.AutoSize,
                Text = old.Text,
                Font = old.Font,
                ForeColor = old.ForeColor,
                BackColor = Color.Transparent,
                Location = old.Location,
                Anchor = old.Anchor,
                Cursor = Cursors.Default,
                TabStop = false,
                UseMnemonic = false
            };

            // Remove possible event handlers affecting hover/click
            old.MouseEnter -= lblSystem_Click;
            old.MouseLeave -= lblRepublic_Click;
            old.Click -= lblSystem_Click;

            Control parent = old.Parent ?? this;
            int index = (parent is null) ? -1 : parent.Controls.GetChildIndex(old, false);
            parent.Controls.Remove(old);
            parent.Controls.Add(replacement);
            if (index >= 0)
            {
                parent.Controls.SetChildIndex(replacement, index);
            }

            // Ensure no hover effects can be attached going forward
            replacement.Enabled = true;             // keep readable style
            replacement.Cursor = Cursors.Default;   // no hand cursor
        }

        // =========================================================
        //  HEADER TEXT
        // =========================================================
        private void UpdateHeaderGreeting()
        {
            if (!string.IsNullOrWhiteSpace(_userDisplayName) && lblMainTitle != null)
            {
                // Set once; do not change dynamically later                lblMainTitle.Text = $"Welcome back, {_userDisplayName}";
                lblMainTitle.Cursor = Cursors.Default;
                lblMainTitle.Enabled = true;
            }
        }

        // =========================================================
        //  FIND EXISTING DESIGNER CONTROLS BY NAME
        // =========================================================
        private void ResolveCoreControls()
        {
            _contentHost = FindPanel("pnlContentHost");

            // basic fallback (should not happen if Designer is correct)
            if (_contentHost == null)
            {
                _contentHost = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };
                this.Controls.Add(_contentHost);
            }

            EnableDoubleBuffer(_contentHost);
        }

        private Panel FindPanel(string name)
        {
            Control[] found = this.Controls.Find(name, true);
            return (found.Length > 0) ? found[0] as Panel : null;
        }

        private TextBox FindTextBox(string name)
        {
            Control[] found = this.Controls.Find(name, true);
            return (found.Length > 0) ? found[0] as TextBox : null;
        }

        // =========================================================
        //  BUILD DASHBOARD PAGES (ONLY IN THIS FILE)
        // =========================================================
        private void BuildPages(string userName)
        {
            _contentHost.Controls.Clear();

            // ----- DASHBOARD PAGE -----
            _pageDashboard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 247, 255)
            };
            EnableDoubleBuffer(_pageDashboard);

            // Big welcome text inside the content area (constant, non-hovering)
            _contentWelcomeLabel = new Label
            {
                AutoSize = true,
                Text = $"Welcome back, {userName}",
                Font = new Font("Segoe UI Semibold", 22f),
                ForeColor = Color.FromArgb(32, 32, 32),
                Location = new Point(24, 24),
                Cursor = Cursors.Default
            };
            // Ensure no accidental interactivity
            _contentWelcomeLabel.Enabled = true;
            _contentWelcomeLabel.MouseEnter -= CardChild_ClickForward;
            _contentWelcomeLabel.MouseLeave -= CardChild_ClickForward;
            _contentWelcomeLabel.Click -= CardChild_ClickForward;

            Label lblSub = new Label
            {
                AutoSize = true,
                Text = "Sign in to access your barangay services",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.DimGray,
                Location = new Point(26, 60),
                Cursor = Cursors.Default
            };

            _pageDashboard.Controls.Add(_contentWelcomeLabel);
            _pageDashboard.Controls.Add(lblSub);

            // ===== METRIC CARDS (TOP ROW) =====
            _pageDashboard.Controls.Add(CreateMetricCard(
                "👥 Total Population",
                "204,500",
                Color.FromArgb(77, 109, 242),
                new Point(32, 110)
            ));

            _pageDashboard.Controls.Add(CreateMetricCard(
                "📣 Announcements",
                "5",
                Color.FromArgb(0, 168, 214),
                new Point(330, 110)
            ));

            _pageDashboard.Controls.Add(CreateMetricCard(
                "🕒 Pending Requests",
                "0",
                Color.FromArgb(130, 84, 245),
                new Point(628, 110)
            ));

            // ===== LARGE CARDS (SECOND ROW) =====
            Panel cardDoc = new Panel
            {
                Size = new Size(268, 190),
                Location = new Point(32, 260),
                BackColor = Color.FromArgb(63, 153, 89),
                Cursor = Cursors.Hand
            };
            cardDoc.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, cardDoc.Width - 1, cardDoc.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 22))
                using (SolidBrush brush = new SolidBrush(cardDoc.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
            };
            cardDoc.Click += cardDocumentRequests_Click;

            Label docTitle = new Label
            {
                AutoSize = true,
                Text = "📄 Document Requests",
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(22, 18)
            };
            docTitle.Click += CardChild_ClickForward;

            Label docValue = new Label
            {
                AutoSize = true,
                Text = "0",
                Font = new Font("Segoe UI Semibold", 32f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(26, 60)
            };
            docValue.Click += CardChild_ClickForward;

            Label docSub = new Label
            {
                AutoSize = true,
                Text = "Total processed",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent,
                Location = new Point(26, 140)
            };
            docSub.Click += CardChild_ClickForward;

            cardDoc.Controls.Add(docTitle);
            cardDoc.Controls.Add(docValue);
            cardDoc.Controls.Add(docSub);
            _pageDashboard.Controls.Add(cardDoc);

            // BLOTTER REPORTS
            Panel cardBlotter = new Panel
            {
                Size = new Size(268, 190),
                Location = new Point(320, 260),
                BackColor = Color.FromArgb(160, 105, 60),
                Cursor = Cursors.Hand
            };
            bool blotterHovered = false;
            bool blotterPressed = false;
            cardBlotter.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, cardBlotter.Width - 1, cardBlotter.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 22))
                using (SolidBrush brush = new SolidBrush(
                    blotterPressed ? Color.FromArgb(140, 92, 50) :
                    blotterHovered ? Color.FromArgb(170, 115, 70) :
                    cardBlotter.BackColor))
                {
                    e.Graphics.FillPath(brush, path);
                }
                if (blotterHovered)
                {
                    using (Pen shadow = new Pen(Color.FromArgb(60, 0, 0, 0), 6))
                    {
                        e.Graphics.DrawPath(shadow, RoundedRect(new Rectangle(4, 4, rect.Width - 8, rect.Height - 8), 22));
                    }
                }
            };
            cardBlotter.MouseEnter += (s, e) => { blotterHovered = true; cardBlotter.Invalidate(); };
            cardBlotter.MouseLeave += (s, e) => { blotterHovered = false; blotterPressed = false; cardBlotter.Invalidate(); };
            cardBlotter.MouseDown += (s, e) => { blotterPressed = true; cardBlotter.Invalidate(); };
            cardBlotter.MouseUp += (s, e) => { blotterPressed = false; cardBlotter.Invalidate(); };
            cardBlotter.Click += cardBlotterReports_Click;

            Label blotterTitle = new Label
            {
                AutoSize = true,
                Text = "🛡 Blotter Reports",
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(22, 18),
                Cursor = Cursors.Hand
            };
            blotterTitle.Click += CardChild_ClickForward;

            Label blotterCount = new Label
            {
                AutoSize = true,
                Text = "0",
                Font = new Font("Segoe UI Semibold", 32f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(26, 60),
                Cursor = Cursors.Hand
            };
            blotterCount.Click += CardChild_ClickForward;

            Label blotterValue = new Label
            {
                AutoSize = true,
                Text = "1 pending",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.WhiteSmoke,
                BackColor = Color.Transparent,
                Location = new Point(26, 140),
                Cursor = Cursors.Hand
            };
            blotterValue.Click += CardChild_ClickForward;

            cardBlotter.Controls.Add(blotterTitle);
            cardBlotter.Controls.Add(blotterCount);
            cardBlotter.Controls.Add(blotterValue);
            _pageDashboard.Controls.Add(cardBlotter);

            // ===== LATEST UPDATES SECTION (RIGHT SIDE) =====
            int updatesX = 620;
            int updatesY = 230;

            Label lblUpdates = new Label
            {
                AutoSize = true,
                Text = "Latest Updates",
                Font = new Font("Segoe UI Semibold", 12f),
                ForeColor = Color.FromArgb(32, 32, 32),
                Location = new Point(updatesX, updatesY)
            };
            _pageDashboard.Controls.Add(lblUpdates);

            _pageDashboard.Controls.Add(CreateUpdateCard(
                "Community event scheduled",
                "A clean up drive is scheduled this Saturday at the main park area.",
                "2 days ago",
                new Point(updatesX, updatesY + 28)));

            _pageDashboard.Controls.Add(CreateUpdateCard(
                "Road maintenance on Main St.",
                "Expect temporary closures and detours on Main Street next week.",
                "5 days ago",
                new Point(updatesX, updatesY + 98)));

            _pageDashboard.Controls.Add(CreateUpdateCard(
                "Vaccination drive this week",
                "A vaccination drive will be held in the barangay hall for eligible residents.",
                "1 week ago",
                new Point(updatesX, updatesY + 168)));

            // ----- OTHER PAGES (PLACEHOLDERS) -----
            _pageServices = CreateServicesPage();
            _pageRequirements = CreateRequirementsPage();
            _pageFeedback = CreateFeedbackPage(userName);
            _pageAbout = CreatePlaceholderPage("About Barangayan EMS", Color.FromArgb(139, 92, 246));

            EnableDoubleBuffer(_pageServices);
            EnableDoubleBuffer(_pageRequirements);
            EnableDoubleBuffer(_pageFeedback);
            EnableDoubleBuffer(_pageAbout);

            _contentHost.Controls.Add(_pageDashboard);
            _contentHost.Controls.Add(_pageServices);
            _contentHost.Controls.Add(_pageRequirements);
            _contentHost.Controls.Add(_pageFeedback);
            _contentHost.Controls.Add(_pageAbout);
        }

        private Panel CreateFeedbackPage(string userName)
        {
            var repo = new FeedbackRepository(); // ensures table exists

            Panel page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 247, 255),
                Visible = false
            };
            EnableDoubleBuffer(page);

            // Card container
            Panel card = new Panel
            {
                Size = new Size(560, 320),
                Location = new Point(280, 130),
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            EnableDoubleBuffer(card);
            int cardCornerRadius = 18;
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, cardCornerRadius))
                using (SolidBrush fill = new SolidBrush(Color.White))
                using (Pen border = new Pen(Color.FromArgb(228, 231, 255)))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            };
            card.Resize += (s, e) =>
            {
                card.Region = new Region(RoundedRect(new Rectangle(0, 0, card.Width, card.Height), cardCornerRadius));
            };
            // apply region once
            card.Region = new Region(RoundedRect(new Rectangle(0, 0, card.Width, card.Height), cardCornerRadius));
            page.Controls.Add(card);

            int y = 0;
            Label lblHeader = new Label
            {
                AutoSize = true,
                Text = "Feedback System",
                Font = new Font("Segoe UI Semibold", 14f),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(0, y)
            };
            card.Controls.Add(lblHeader);
            y = lblHeader.Bottom + 4;

            Label lblDesc = new Label
            {
                AutoSize = false,
                Size = new Size(card.Width - card.Padding.Horizontal, 36),
                Text = "Share your suggestions, complaints, or feedback to help us improve our services.",
                Font = new Font("Segoe UI", 9.6f),
                ForeColor = Color.FromArgb(90, 90, 90),
                Location = new Point(0, y)
            };
            card.Controls.Add(lblDesc);
            y = lblDesc.Bottom + 12;

            // Inside card: Feedback Type
            Label lblType = new Label
            {
                AutoSize = true,
                Text = "Feedback Type",
                Font = new Font("Segoe UI Semibold", 9.8f),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(0, y),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblType);
            y = lblType.Bottom + 6;

            // Rounded host for ComboBox (to simulate clean input)
            Panel typeHost = new Panel
            {
                Size = new Size(card.Width - card.Padding.Horizontal, 40),
                Location = new Point(0, y),
                BackColor = Color.White
            };
            typeHost.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, typeHost.Width - 1, typeHost.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (SolidBrush fill = new SolidBrush(Color.White))
                using (Pen border = new Pen(Color.FromArgb(228, 231, 255)))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            };
            typeHost.Resize += (s, e) => typeHost.Region = new Region(RoundedRect(new Rectangle(0, 0, typeHost.Width, typeHost.Height), 10));
            card.Controls.Add(typeHost);

            ComboBox cmbType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(12, 8),
                Width = typeHost.Width - 24,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.Gray
            };
            cmbType.Items.AddRange(new object[] { "Select feedback type", "Suggestion", "Complaint", "Bug Report", "Other" });
            cmbType.SelectedIndex = 0;
            cmbType.SelectedIndexChanged += (s, e2) =>
            {
                cmbType.ForeColor = (cmbType.SelectedIndex == 0) ? Color.Gray : Color.FromArgb(31, 41, 55);
            };
            typeHost.Controls.Add(cmbType);
            y = typeHost.Bottom + 14;

            // Your Feedback
            Label lblMsg = new Label
            {
                AutoSize = true,
                Text = "Your Feedback",
                Font = new Font("Segoe UI Semibold", 9.8f),
                ForeColor = Color.FromArgb(55, 65, 81),
                Location = new Point(0, y),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblMsg);
            y = lblMsg.Bottom + 6;

            // Rounded host for TextBox
            Panel msgHost = new Panel
            {
                Size = new Size(card.Width - card.Padding.Horizontal, 96),
                Location = new Point(0, y),
                BackColor = Color.White
            };
            msgHost.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, msgHost.Width - 1, msgHost.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 10))
                using (SolidBrush fill = new SolidBrush(Color.White))
                using (Pen border = new Pen(Color.FromArgb(228, 231, 255)))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            };
            msgHost.Resize += (s, e) => msgHost.Region = new Region(RoundedRect(new Rectangle(0, 0, msgHost.Width, msgHost.Height), 10));
            card.Controls.Add(msgHost);

            TextBox txtMsg = new TextBox
            {
                Multiline = true,
                BorderStyle = BorderStyle.None,
                Location = new Point(12, 10),
                Size = new Size(msgHost.Width - 24, msgHost.Height - 20),
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.Gray,
                Text = "Share your thoughts…"
            };
            txtMsg.GotFocus += (s, e) =>
            {
                if (txtMsg.ForeColor == Color.Gray)
                {
                    txtMsg.Text = string.Empty;
                    txtMsg.ForeColor = Color.FromArgb(31, 41, 55);
                }
            };
            txtMsg.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtMsg.Text))
                {
                    txtMsg.Text = "Share your thoughts…";
                    txtMsg.ForeColor = Color.Gray;
                }
            };
            msgHost.Controls.Add(txtMsg);
            y = msgHost.Bottom + 16;

            Button btnSubmit = new Button
            {
                Text = "Submit Feedback",
                AutoSize = false,
                Size = new Size(card.Width - card.Padding.Horizontal, 40),
                Location = new Point(0, y),
                BackColor = Color.FromArgb(24, 24, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.MouseEnter += (s, e) => btnSubmit.BackColor = Color.FromArgb(34, 34, 44);
            btnSubmit.MouseDown += (s, e) => btnSubmit.BackColor = Color.FromArgb(18, 18, 26);
            btnSubmit.MouseLeave += (s, e) => btnSubmit.BackColor = Color.FromArgb(24, 24, 32);
            btnSubmit.Resize += (s, e) => btnSubmit.Region = new Region(RoundedRect(new Rectangle(0, 0, btnSubmit.Width, btnSubmit.Height), 10));
            card.Controls.Add(btnSubmit);

            btnSubmit.Click += (s, e) =>
            {
                string type = (cmbType.SelectedIndex > 0) ? (cmbType.SelectedItem as string) : null;
                string msg = (txtMsg.ForeColor == Color.Gray) ? string.Empty : (txtMsg.Text ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(type))
                {
                    MessageBox.Show("Please select a feedback type.", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (string.IsNullOrWhiteSpace(msg))
                {
                    MessageBox.Show("Please enter your feedback message.", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                try
                {
                    repo.Insert(type, msg, userName);
                    MessageBox.Show("Thank you for your feedback!", "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cmbType.SelectedIndex = 0;
                    txtMsg.Text = "Share your thoughts…";
                    txtMsg.ForeColor = Color.Gray;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to save feedback. " + ex.Message, "Feedback", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            return page;
        }

        // =========================================================
        //  MODAL FORM FOR FEEDBACK SUBMISSION
        // =========================================================
        private void ShowFeedbackModal(string userName)
        {
            using (var f = new FeedbackForm(userName))
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }

        // ===== Requirements Page =====
        private Panel CreateRequirementsPage()
        {
            Panel page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 247, 255),
                Visible = false
            };
            EnableDoubleBuffer(page);

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = "Requirements",
                Font = new Font("Segoe UI Semibold", 22f),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(28, 26)
            };
            page.Controls.Add(lblTitle);

            // Grid layout similar to the reference
            TableLayoutPanel grid = new TableLayoutPanel
            {
                Location = new Point(28, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
                ColumnCount = 3,
                RowCount = 2,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            page.Controls.Add(grid);
            page.Resize += (s, e) =>
            {
                grid.Size = new Size(Math.Max(300, page.Width - 56), Math.Max(220, page.Height - 120));
            };
            grid.Size = new Size(Math.Max(300, page.Width - 56), Math.Max(220, page.Height - 120));

            // Cards data
            var cards = new[]
            {
                new { Title = "Barangay Clearance", Accent = Color.FromArgb(77, 109, 242), Items = new[]{
                    "Valid government-issued ID",
                    "Cedula or Community Tax Certificate",
                    "Barangay residency proof",
                    "Processing fee: ₱30.00" }},
                new { Title = "Business Permit", Accent = Color.FromArgb(130, 84, 245), Items = new[]{
                    "DTI/SEC registration",
                    "Barangay clearance",
                    "Location clearance",
                    "Fire safety inspection certificate",
                    "Processing fee: ₱500.00" }},
                new { Title = "Certificate of Indigency", Accent = Color.FromArgb(0, 168, 214), Items = new[]{
                    "Valid government-issued ID",
                    "Proof of low income/unemployment",
                    "Barangay residency proof",
                    "Processing fee: ₱20.00" }},
                new { Title = "Building Permit", Accent = Color.FromArgb(16, 185, 129), Items = new[]{
                    "Lot title or Tax Declaration",
                    "Building plans (signed by architect)",
                    "Structural design plans",
                    "Barangay clearance",
                    "Processing fee: ₱1,000.00" }},
                new { Title = "Complaint Certificate", Accent = Color.FromArgb(245, 158, 11), Items = new[]{
                    "Valid government-issued ID",
                    "Incident report documentation",
                    "Witness affidavits (if applicable)",
                    "Processing fee: ₱50.00" }},
                new { Title = "Community Tax Certificate", Accent = Color.FromArgb(139, 92, 246), Items = new[]{
                    "Valid government-issued ID",
                    "Proof of income (if employed)",
                    "Barangay residency proof",
                    "Processing fee: ₱5.00 - ₱50.00" }}
            };

            int idx = 0;
            foreach (var c in cards)
            {
                var card = CreateRequirementCard(c.Title, c.Items, c.Accent);
                grid.Controls.Add(card, idx % 3, idx / 3);
                idx++;
            }

            return page;
        }

        private Panel CreateRequirementCard(string title, string[] items, Color accent)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(18),
                BackColor = Color.Transparent
            };
            EnableDoubleBuffer(card);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 18))
                using (SolidBrush fill = new SolidBrush(Color.White))
                using (Pen border = new Pen(Color.FromArgb(228, 231, 255)))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            };

            // Accent focus border (hover)
            bool hovered = false;
            card.MouseEnter += (s, e) => { hovered = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { hovered = false; card.Invalidate(); };
            card.Paint += (s, e) =>
            {
                if (!hovered) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(2, 2, card.Width - 5, card.Height - 5);
                using (GraphicsPath path = RoundedRect(rect, 18))
                using (Pen accentPen = new Pen(accent, 2))
                {
                    e.Graphics.DrawPath(accentPen, path);
                }
            };

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = title,
                Font = new Font("Segoe UI Semibold", 12.5f),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(20, 16),
                BackColor = Color.Transparent
            };
            card.Controls.Add(lblTitle);

            // Bullet list
            int y = lblTitle.Bottom + 10;
            foreach (var item in items)
            {
                Label bullet = new Label
                {
                    AutoSize = true,
                    Text = "• " + item,
                    Font = new Font("Segoe UI", 9.6f),
                    ForeColor = Color.FromArgb(80, 80, 80),
                    Location = new Point(22, y),
                    BackColor = Color.Transparent
                };
                card.Controls.Add(bullet);
                y = bullet.Bottom + 6;
            }

            // Padding by setting a minimum size based on content
            card.Resize += (s, e) =>
            {
                int minH = Math.Max(140, y + 16);
                card.MinimumSize = new Size(0, minH);
            };

            return card;
        }

        // =========================================================
        //  CARD CREATION HELPERS
        // =========================================================
        private Panel CreateMetricCard(string title, string value, Color color, Point location)
        {
            Panel card = new Panel
            {
                Size = new Size(260, 120),
                Location = location,
                BackColor = Color.Transparent
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 18))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                           rect,
                           color,
                           ControlPaint.Light(color, 0.1f),
                           0f))
                {
                    e.Graphics.FillPath(brush, path);
                }
            };

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = title,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.White,
                Location = new Point(18, 16),
                BackColor = Color.Transparent
            };

            Label lblValue = new Label
            {
                AutoSize = true,
                Text = value,
                Font = new Font("Segoe UI Semibold", 22f),
                ForeColor = Color.White,
                Location = new Point(18, 46),
                BackColor = Color.Transparent
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            return card;
        }

        private Panel CreateUpdateCard(string title, string body, string age, Point location)
        {
            Panel card = new Panel
            {
                Size = new Size(330, 64),
                Location = location,
                BackColor = Color.White
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                using (GraphicsPath path = RoundedRect(rect, 14))
                using (SolidBrush brush = new SolidBrush(Color.White))
                using (Pen pen = new Pen(Color.FromArgb(228, 231, 255)))
                {
                    e.Graphics.FillPath(brush, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = title,
                Font = new Font("Segoe UI Semibold", 10.5f),
                ForeColor = Color.FromArgb(32, 32, 32),
                Location = new Point(18, 8),
                BackColor = Color.Transparent
            };

            Label lblBody = new Label
            {
                AutoSize = false,
                Text = body,
                Font = new Font("Segoe UI", 8.9f),
                ForeColor = Color.DimGray,
                Location = new Point(18, 26),
                Size = new Size(230, 32),
                BackColor = Color.Transparent
            };

            Label lblAge = new Label
            {
                AutoSize = true,
                Text = age,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                Location = new Point(card.Width - 80, 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblBody);
            card.Controls.Add(lblAge);
            return card;
        }

        private Panel CreateServicesPage()
        {
            Panel page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 247, 255),
                Visible = false
            };

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = "Services",
                Font = new Font("Segoe UI Semibold", 24f),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(28, 26)
            };

            Label lblSubtitle = new Label
            {
                AutoSize = true,
                Text = "Access barangay services in one place.",
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = Color.DimGray,
                Location = new Point(30, 66)
            };

            FlowLayoutPanel grid = new FlowLayoutPanel
            {
                Location = new Point(32, 110),
                Size = new Size(page.Width - 64, page.Height - 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
                AutoScroll = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            var serviceCards = new (string Title, string Description, Color Accent, string Icon)[]
            {
                ("Online Payments", "Pay barangay dues securely online.", Color.FromArgb(99, 123, 255), "\U0001F4B3"),
                ("Business Permit Services", "Apply for or renew permits digitally.", Color.FromArgb(252, 156, 199), "\U0001F9FE"),
                ("Emergency Services", "Quick access to emergency contacts.", Color.FromArgb(255, 115, 116), "\U0001F691"),
                ("Report Issues", "Submit concerns and incidents anytime.", Color.FromArgb(255, 174, 124), "\u26A0"),
                ("Barangay ID Application", "Request or renew your barangay ID.", Color.FromArgb(139, 199, 120), "\U0001F194"),
                ("Emergency Hotline", "Reach barangay hotline instantly.", Color.FromArgb(255, 153, 141), "\U0001F4DE"),
                ("Online Appointments / Scheduling", "Book appointments without visiting the hall.", Color.FromArgb(101, 119, 255), "\U0001F4C5")
            };

            foreach (var info in serviceCards)
            {
                grid.Controls.Add(CreateServiceCard(info.Title, info.Description, info.Accent, info.Icon));
            }

            page.Controls.Add(lblTitle);
            page.Controls.Add(lblSubtitle);
            page.Controls.Add(grid);

            page.Resize += (s, e) =>
            {
                grid.Size = new Size(Math.Max(220, page.Width - 64), Math.Max(220, page.Height - 140));
            };

            return page;
        }

        private Panel CreateServiceCard(string title, string description, Color accentColor, string icon)
        {
            Panel card = new Panel
            {
                Size = new Size(232, 130),
                Margin = new Padding(0, 0, 28, 28),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Tag = title
            };
            EnableDoubleBuffer(card);

            bool hovered = false;
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                Rectangle cardRect = new Rectangle(0, 0, card.Width - 12, card.Height - 12);
                // Adjust shadow rect to avoid clipping artifacts on certain DPI/scales
                Rectangle shadowRect = new Rectangle(cardRect.X + 3, cardRect.Y + 3, cardRect.Width, cardRect.Height);

                using (GraphicsPath shadowPath = RoundedRect(shadowRect, 18))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(hovered ? 70 : 45, 15, 23, 42)))
                {
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }

                using (GraphicsPath cardPath = RoundedRect(cardRect, 18))
                using (SolidBrush fillBrush = new SolidBrush(Color.White))
                using (Pen borderPen = new Pen(Color.FromArgb(hovered ? 210 : 235, 235, 245)))
                {
                    e.Graphics.FillPath(fillBrush, cardPath);
                    e.Graphics.DrawPath(borderPen, cardPath);
                }
            };

            card.MouseEnter += (s, e) => { hovered = true; card.Invalidate(); };
            card.MouseLeave += (s, e) => { hovered = false; card.Invalidate(); };
            card.Click += ServiceCard_Click;

            Panel iconBadge = new Panel
            {
                Size = new Size(46, 46),
                Location = new Point(24, 22),
                BackColor = Color.Transparent
            };
            EnableDoubleBuffer(iconBadge);

            iconBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                using (SolidBrush brush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillEllipse(brush, 0, 0, iconBadge.Width - 1, iconBadge.Height - 1);
                }
                // Draw emoji/icon centered within the circle with per-icon baseline tweak
                using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                using (var textBrush = new SolidBrush(Color.White))
                using (var font = new Font("Segoe UI Emoji", 16f, FontStyle.Regular, GraphicsUnit.Point))
                {
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                    var rect = new RectangleF(0, GetIconYOffset(icon), iconBadge.Width, iconBadge.Height);
                    e.Graphics.DrawString(icon, font, textBrush, rect, sf);
                }
            };

            // Remove separate label and forward clicks via badge
            iconBadge.Click += CardChild_ClickForward;

            Label lblName = new Label
            {
                AutoSize = false,
                Text = title,
                Font = new Font("Segoe UI Semibold", 11f),
                ForeColor = Color.FromArgb(35, 35, 35),
                Location = new Point(24, 76),
                Size = new Size(card.Width - 48, 22),
                BackColor = Color.Transparent
            };
            lblName.Click += CardChild_ClickForward;

            Label lblDesc = new Label
            {
                AutoSize = false,
                Text = description,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DimGray,
                Location = new Point(24, 100),
                Size = new Size(card.Width - 48, 24),
                BackColor = Color.Transparent
            };
            lblDesc.Click += CardChild_ClickForward;

            card.Controls.Add(iconBadge);
            card.Controls.Add(lblName);
            card.Controls.Add(lblDesc);
            return card;
        }

        private Panel CreatePlaceholderPage(string title, Color accent)
        {
            Panel page = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(246, 247, 255),
                Visible = false
            };

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = title,
                Font = new Font("Segoe UI Semibold", 20f),
                ForeColor = accent,
                Location = new Point(24, 24)
            };

            Label lblBody = new Label
            {
                AutoSize = true,
                Text = "Page content coming soon. This is a placeholder section.",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.DimGray,
                Location = new Point(26, 60)
            };

            page.Controls.Add(lblTitle);
            page.Controls.Add(lblBody);
            return page;
        }

        private GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        // Small per-icon vertical tweak so glyphs with different font baselines look visually centered
        private static float GetIconYOffset(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon)) return 0f;
            switch (icon.Trim())
            {
                case "\u26A0": // Warning sign (Report Issues)
                    return -2.0f;
                case "\U0001F4B3": // Credit card (Online Payments)
                    return -1.8f;
                default:
                    return -0.6f; // subtle lift for other emoji to appear visually centered
            }
        }

        // =========================================================
        //  NAVIGATION: WIRE SIDEBAR PANELS TO PAGES
        // =========================================================
        // Helper: climb up to the registered nav panel from any clicked child
        private Panel GetNavPanelFromSender(object sender)
        {
            Control c = sender as Control;
            while (c != null)
            {
                var p = c as Panel;
                if (p != null && _navMap.ContainsKey(p))
                    return p;
                c = c.Parent;
            }
            return null;
        }

        private void SetupNavigation()
        {
            RegisterNavPanel("navDashboard", DashboardPage.Dashboard);
            RegisterNavPanel("navServices", DashboardPage.Services);
            RegisterNavPanel("navRequirements", DashboardPage.Requirements);
            RegisterNavPanel("navFeedback", DashboardPage.Feedback);
            RegisterNavPanel("navAbout", DashboardPage.About);
        }

        private void RegisterNavPanel(string panelName, DashboardPage page)
        {
            Panel nav = FindPanel(panelName);
            if (nav == null) return;
            RegisterNavPanel(nav, page);
        }

        // Overload to register a panel instance directly
        private void RegisterNavPanel(Panel nav, DashboardPage page)
        {
            if (nav == null) return;

            if (_navMap.ContainsKey(nav)) return;
            _navMap.Add(nav, page);

            nav.Cursor = Cursors.Hand;
            EnableDoubleBuffer(nav);
            nav.Padding = new Padding(12, 8, 12, 8);
            nav.BackColor = Color.Transparent;
            nav.Tag = false; // active flag
            nav.Paint += (s, e) => PaintNavItem(nav, e);

            // Core handlers on the panel itself
            nav.MouseEnter += (s, e) => Nav_MouseEnter(nav);
            nav.MouseLeave += (s, e) => Nav_MouseLeave(nav);
            nav.Click += Nav_Click;

            // Make all descendants forward hover/click into the parent panel reliably
            foreach (var child in EnumerateDescendants(nav))
            {
                child.Cursor = Cursors.Hand;
                child.MouseEnter += (s, e) => Nav_MouseEnter(nav);
                child.MouseLeave += (s, e) => Nav_MouseLeave(nav);
                child.Click += Nav_Click;
            }

            if (_activeNavPanel == null && page == DashboardPage.Dashboard)
            {
                SetNavActive(nav, true);
                _activeNavPanel = nav;
            }
        }

        private void Nav_Click(object sender, EventArgs e)
        {
            // Instant navigation; avoid blocking when a previous slide is in progress
            Panel nav = GetNavPanelFromSender(sender);
            if (nav == null) return;

            DashboardPage target = _navMap[nav];

            if (_activeNavPanel != null && _activeNavPanel != nav)
                SetNavActive(_activeNavPanel, false);

            SetNavActive(nav, true);
            _activeNavPanel = nav;
            _activePage = target;

            ShowPage(target, immediate: true);
        }

        // =========================================================
        //  SEARCH BOX PLACEHOLDER
        // =========================================================
        private void SetupSearchBox()
        {
            TextBox txtSearch = FindTextBox("txtSearch");
            if (txtSearch == null) return;

            const string placeholder = "Search";
            txtSearch.ForeColor = Color.Gray;

            txtSearch.GotFocus += (s, e) =>
            {
                if (txtSearch.Text == placeholder)
                {
                    txtSearch.Text = "";
                    txtSearch.ForeColor = Color.FromArgb(32, 32, 32);
                }
            };

            txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    txtSearch.Text = placeholder;
                    txtSearch.ForeColor = Color.Gray;
                }
            };

            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = placeholder;
                txtSearch.ForeColor = Color.Gray;
            }
        }

        // =========================================================
        //  SLIDE ANIMATION BETWEEN PAGES
        // =========================================================
        private void SetupSlideTimer()
        {
            _slideTimer = new Timer { Interval = 16 };
            _slideTimer.Tick += SlideTimer_Tick;
        }

        private void ShowPage(DashboardPage page, bool immediate)
        {
            Control newPage = PageFromEnum(page);
            if (newPage == null || _contentHost == null) return;

            _activePage = page;

            // Sync sidebar visual active state to the page we are showing
            SyncActiveNavToPage(page);

            if (immediate)
            {
                foreach (Control c in _contentHost.Controls)
                    c.Visible = (c == newPage);

                newPage.Location = new Point(0, 0);
                _slideFrom = newPage;
                _slideTo = null;
                _isSliding = false;
                return;
            }

            StartSlideAnimation(newPage);
        }

        private void SyncActiveNavToPage(DashboardPage page)
        {
            // Find the nav panel mapped to the page and set active state visually
            Panel targetPanel = _navMap.FirstOrDefault(kv => kv.Value == page).Key;
            if (targetPanel != null && targetPanel != _activeNavPanel)
            {
                SetNavActive(_activeNavPanel, false);
                SetNavActive(targetPanel, true);
                _activeNavPanel = targetPanel;
            }
        }

        private Control PageFromEnum(DashboardPage page)
        {
            switch (page)
            {
                case DashboardPage.Dashboard: return _pageDashboard;
                case DashboardPage.Services: return _pageServices;
                case DashboardPage.Requirements: return _pageRequirements;
                case DashboardPage.Feedback: return _pageFeedback;
                case DashboardPage.About: return _pageAbout;
                default: return _pageDashboard;
            }
        }

        private void StartSlideAnimation(Control newPage)
        {
            if (_slideTimer == null || _contentHost == null)
                return;

            if (_isSliding)
                return; // do not start overlapping animations

            if (_slideTimer.Enabled)
                _slideTimer.Stop();

            Control current = null;
            foreach (Control c in _contentHost.Controls)
            {
                if (c.Visible) { current = c; break; }
            }

            _slideFrom = current;
            _slideTo = newPage;
            _slideStep = 0;
            _isSliding = true;

            _slideTo.Visible = true;
            _slideTo.BringToFront();
            _slideTo.Location = new Point(_contentHost.Width, 0);

            if (_slideFrom != null)
                _slideFrom.BringToFront();

            _slideTimer.Start();
        }

        private void SlideTimer_Tick(object sender, EventArgs e)
        {
            if (_slideFrom == null || _slideTo == null || _contentHost == null)
            {
                _slideTimer.Stop();
                _isSliding = false;
                return;
            }

            _slideStep++;
            float t = _slideStep / (float)SlideSteps;
            if (t > 1f) t = 1f;

            int offset = (int)(_contentHost.Width * (1f - t));

            _slideTo.Location = new Point(offset, 0);
            _slideFrom.Location = new Point(offset - _contentHost.Width, 0);

            if (_slideStep >= SlideSteps)
            {
                _slideTimer.Stop();
                _slideTo.Location = new Point(0, 0);

                // Make only the target page visible to avoid paint artifacts
                foreach (Control c in _contentHost.Controls)
                    c.Visible = (c == _slideTo);

                _slideFrom = _slideTo;
                _slideTo = null;
                _isSliding = false;
            }
        }

        // =========================================================
        //  CARD CLICK HANDLERS
        // =========================================================
        private void cardDocumentRequests_Click(object sender, EventArgs e)
        {
            using (var f = new DocumentRequestForm())
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }

        private void OpenSubmitBlotterForm()
        {
            using (var f = new SubmitBlotterForm())
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }

        private void cardBlotterReports_Click(object sender, EventArgs e)
        {
            using (var f = new BlotterReportForm())
            {
                f.StartPosition = FormStartPosition.CenterParent;
                f.ShowDialog(this);
            }
        }

        private void ServiceCard_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                "This feature will be available soon.",
                "Coming Soon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void NavigateService(string serviceName)
        {
            MessageBox.Show(
                "This feature will be available soon.",
                "Coming Soon",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "Do you want to log out?",
                "Log out",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            var loginForm = Application.OpenForms.OfType<LoginForm>().FirstOrDefault();
            if (loginForm == null)
            {
                loginForm = new LoginForm();
            }

            loginForm.Show();
            loginForm.BringToFront();
            loginForm.Focus();

            Close();
        }

        private void CardChild_ClickForward(object sender, EventArgs e)
        {
            if (sender is Control child && child.Parent != null)
            {
                this.InvokeOnClick(child.Parent, EventArgs.Empty);
            }
        }

        // =========================================================
        //  NAVIGATION VISUAL HELPERS
        // =========================================================
        private void Nav_MouseEnter(Panel nav)
        {
            if (nav == _activeNavPanel) return;
            nav.BackColor = Color.FromArgb(245, 248, 255);
            nav.Invalidate();
        }

        private void Nav_MouseLeave(Panel nav)
        {
            if (nav == _activeNavPanel) return;
            nav.BackColor = Color.Transparent;
            nav.Invalidate();
        }

        private void SetNavActive(Panel nav, bool active)
        {
            if (nav == null) return;
            nav.Tag = active;
            nav.BackColor = Color.Transparent; // painter draws when active
            ApplyNavTextColors(nav, active);
            nav.Invalidate();
        }

        private void ApplyNavTextColors(Panel nav, bool active)
        {
            if (nav == null) return;
            foreach (Control child in nav.Controls)
            {
                child.ForeColor = active ? NavAccent : Color.FromArgb(90, 90, 90);
            }
        }

        private void PaintNavItem(Panel nav, PaintEventArgs e)
        {
            if (nav == null) return;
            bool isActive = nav.Tag as bool? == true;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (isActive)
            {
                Rectangle rect = new Rectangle(6, 4, nav.Width - 12, nav.Height - 8);
                // shadow
                using (GraphicsPath shadowPath = RoundedRect(new Rectangle(rect.X + 2, rect.Y + 2, rect.Width, rect.Height), 12))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(40, 17, 24, 39)))
                {
                    e.Graphics.FillPath(shadowBrush, shadowPath);
                }
                // highlight container
                using (GraphicsPath path = RoundedRect(rect, 12))
                using (SolidBrush fill = new SolidBrush(Color.White))
                using (Pen border = new Pen(Color.FromArgb(230, 236, 255)))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(border, path);
                }
            }
            // inactive: transparent background, no drawing
        }

        // =========================================================
        //  DESIGNER-EVENT STUBS (required by Designer)
        // =========================================================
        private void lblSystem_Click(object sender, EventArgs e)
        {
            // optional: show "About system"
        }

        private void lblRepublic_Click(object sender, EventArgs e)
        {
            // optional: link to website
        }

        private void navServices_Paint(object sender, PaintEventArgs e)
        {
            // no custom painting; keep empty to satisfy Designer
        }

        private void cardPending_Paint(object sender, PaintEventArgs e)
        {
            // legacy stub; not used with runtime metric cards
        }

        private void lblDocValue_Click(object sender, EventArgs e)
        {
            // legacy stub
        }

        private void cardBlotterReports_Paint(object sender, PaintEventArgs e)
        {
            // legacy stub
        }

        // Fallback hook: finds any control that looks like "Services" and wires it
        private static IEnumerable<Control> EnumerateDescendants(Control root)
        {
            foreach (Control child in root.Controls)
            {
                yield return child;
                foreach (var grandChild in EnumerateDescendants(child))
                    yield return grandChild;
            }
        }

        private void HookServicesNavFallback()
        {
            // Try to locate a sidebar panel that visually represents "Services" when named controls are missing.
            // Find a panel whose child label text contains "Services".
            var allPanels = this.Controls.OfType<Panel>().Concat(EnumerateDescendants(this).OfType<Panel>()).ToList();
            foreach (var p in allPanels)
            {
                // Skip already registered
                if (_navMap.ContainsKey(p)) continue;

                // Look for a label child with text containing "Services"
                var label = p.Controls.OfType<Label>().FirstOrDefault(l =>
                    !string.IsNullOrWhiteSpace(l.Text) && l.Text.IndexOf("Services", StringComparison.OrdinalIgnoreCase) >= 0);
                if (label != null)
                {
                    RegisterNavPanel(p, DashboardPage.Services);
                    break;
                }
            }
        }
    }

    internal sealed class DocumentRequestForm : Form
    {
        private readonly List<DocumentRequest> _allRequests = new List<DocumentRequest>();
        private readonly BindingSource _bindingSource = new BindingSource();
        private readonly DataGridView _grid;
        private readonly TextBox _txtSearch;
        private readonly Panel _statusPanel;
        private readonly ComboBox _cmbStatus;
        private DocumentRequest _selectedRequest;
        private readonly DocumentRequestRepository _repo = new DocumentRequestRepository();
        private string _pendingSelectId; // remember a request id to select after binding

        private static readonly string[] StatusOptions =
        {
            "Pending",
            "Approved",
            "Rejected",
            "For Pickup",
            "Completed"
        };

        internal DocumentRequestForm()
        {
            Text = "Document Request Status";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Size = new Size(980, 620);
            Font = new Font("Segoe UI", 9f);
            DoubleBuffered = true;

            // Root with consistent padding
            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24)
            };
            Controls.Add(content);

            // ---------------------------------
            // Header section (gov label + title)
            // ---------------------------------
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.White
            };
            content.Controls.Add(header);

            Label lblGov = new Label
            {
                AutoSize = true,
                Text = "Republika ng Pilipinas\r\nBarangayan E-Management System",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(90, 90, 90),
                Location = new Point(0, 0)
            };
            header.Controls.Add(lblGov);

            Label lblTitle = new Label
            {
                AutoSize = true,
                Text = "Document Request Status",
                Font = new Font("Segoe UI Semibold", 20f),
                ForeColor = Color.FromArgb(48, 48, 48),
                Location = new Point(0, 50)
            };
            header.Controls.Add(lblTitle);

            // spacing panel under header
            Panel headerSpacer = new Panel { Dock = DockStyle.Top, Height = 8 }; // small gap
            content.Controls.Add(headerSpacer);

            // ---------------------------------
            // Actions + Search bar row
            // ---------------------------------
            Panel actionsRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.White
            };
            content.Controls.Add(actionsRow);

            FlowLayoutPanel actionBar = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Size = new Size(600, 46),
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            actionsRow.Controls.Add(actionBar);

            Button btnNew = CreateActionButton("+ New Request", Color.FromArgb(56, 178, 89));
            btnNew.Click += (s, e) =>
            {
                using (var entry = new DocumentRequestEntryForm())
                {
                    var result = entry.ShowDialog(this);
                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(entry.SavedRequestId))
                    {
                        _pendingSelectId = entry.SavedRequestId; // defer selecting until bound
                        RefreshRequests();
                        return;
                    }
                }
                RefreshRequests();
            };
            actionBar.Controls.Add(btnNew);

            // Edit should open the editable form
            Button btnEdit = CreateActionButton("Edit Status", Color.FromArgb(44, 124, 228));
            btnEdit.Click += (s, e) => OpenPreviewEdit();
            actionBar.Controls.Add(btnEdit);

            // View should show details in a message box
            Button btnView = CreateActionButton("View Status", Color.FromArgb(245, 158, 11));
            btnView.Click += (s, e) =>
            {
                if (_selectedRequest == null)
                {
                    ShowSelectPrompt();
                    return;
                }

                string details =
                    $"Request ID: {_selectedRequest.RequestId}\n" +
                    $"Type: {_selectedRequest.Type}\n" +
                    $"Requester: {_selectedRequest.RequesterName}\n" +
                    $"Date Filed: {_selectedRequest.DateFiled:yyyy-MM-dd}\n" +
                    $"Status: {_selectedRequest.Status}";

                MessageBox.Show(details, "Request Details", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            actionBar.Controls.Add(btnView);

            Button btnDelete = CreateActionButton("Delete", Color.FromArgb(224, 64, 64));
            btnDelete.Click += (s, e) => DeleteSelected();
            actionBar.Controls.Add(btnDelete);

            // Search aligned to the right of the row
            _txtSearch = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Width = 260,
                Height = 30,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Gray,
                Text = "Search"
            };
            actionsRow.Controls.Add(_txtSearch);
            _txtSearch.Location = new Point(actionsRow.Width - _txtSearch.Width, 8);
            actionsRow.Resize += (s, e) =>
            {
                _txtSearch.Location = new Point(actionsRow.Width - _txtSearch.Width, 8);
            };

            _txtSearch.GotFocus += (s, e) =>
            {
                if (_txtSearch.Text == "Search")
                {
                    _txtSearch.Text = string.Empty;
                    _txtSearch.ForeColor = Color.Black;
                }
            };
            _txtSearch.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(_txtSearch.Text))
                {
                    _txtSearch.Text = "Search";
                    _txtSearch.ForeColor = Color.Gray;
                }
            };
            _txtSearch.TextChanged += (s, e) => ApplyFilter(_txtSearch.Text);

            // spacer under actions
            Panel actionsSpacer = new Panel { Dock = DockStyle.Top, Height = 10 };
            content.Controls.Add(actionsSpacer);

            // ---------------------------------
            // Data grid section
            // ---------------------------------
            Panel gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            content.Controls.Add(gridHost);

            _grid = new DataGridView
            {
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
            gridHost.Controls.Add(_grid);

            // Improve readability
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

            // Columns with balanced widths
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DocumentRequest.RequestId),
                HeaderText = "Request ID",
                Width = 110
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DocumentRequest.Type),
                HeaderText = "Type",
                Width = 190
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DocumentRequest.RequesterName),
                HeaderText = "Requester Name",
                Width = 180
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DocumentRequest.DateFiled),
                HeaderText = "Date Filed",
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd" },
                Width = 120
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DocumentRequest.Status),
                HeaderText = "Status",
                Width = 130
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(DocumentRequest.Actions),
                HeaderText = "Actions",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            _grid.SelectionChanged += (s, e) => UpdateSelection();
            _grid.CellDoubleClick += (s, e) => OpenPreviewEdit();

            // Status floating panel
            _statusPanel = new Panel
            {
                Size = new Size(260, 150),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            Label lblStatusTitle = new Label
            {
                AutoSize = true,
                Text = "Update Request Status",
                Font = new Font("Segoe UI Semibold", 10f),
                Location = new Point(12, 12)
            };
            _statusPanel.Controls.Add(lblStatusTitle);

            _cmbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(15, 50),
                Width = 220
            };
            _cmbStatus.Items.AddRange(StatusOptions);
            _statusPanel.Controls.Add(_cmbStatus);

            Button btnSave = new Button
            {
                Text = "Save Changes",
                Size = new Size(230, 36),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(12, 100)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveChanges();
            _statusPanel.Controls.Add(btnSave);

            gridHost.Controls.Add(_statusPanel);
            _statusPanel.Location = new Point(gridHost.Width - _statusPanel.Width - 6, 6);
            gridHost.Resize += (s, e) =>
            {
                _statusPanel.Location = new Point(gridHost.Width - _statusPanel.Width - 6, 6);
            };

            // Ensure first load populates reliably
            Shown += (s, e) => RefreshRequests();
        }

        private Button CreateActionButton(String text, Color bgColor)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                Height = 36,
                BackColor = bgColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 12, 0)
            };
        }

        private void RefreshRequests()
        {
            _allRequests.Clear();
            foreach (var r in _repo.GetAll())
            {
                _allRequests.Add(new DocumentRequest
                {
                    RequestId = r.RequestId,
                    Type = r.Type,
                    RequesterName = r.RequesterName,
                    DateFiled = r.DateFiled,
                    Status = r.Status,
                    Actions = "View Details, Edit Status",
                    ContactNumber = r.ContactNumber,
                    Purpose = r.Purpose,
                    PickupDate = r.PickupDate,
                    Copies = r.Copies,
                    AdditionalRequirements = r.AdditionalRequirements
                });
            }

            _bindingSource.DataSource = _allRequests.ToList();
            _grid.DataSource = _bindingSource;
            UpdateSelection();

            // If we have a record to select (e.g., just created/edited), do it after binding completes
            if (!string.IsNullOrWhiteSpace(_pendingSelectId))
            {
                BeginInvoke(new Action(() =>
                {
                    SelectRowByRequestId(_pendingSelectId);
                    _pendingSelectId = null;
                }));
            }
        }

        private void SelectRowByRequestId(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;
            for (int i = 0; i < _grid.Rows.Count; i++)
            {
                var row = _grid.Rows[i];
                var item = row.DataBoundItem as DocumentRequest;
                if (item != null && string.Equals(item.RequestId, requestId, StringComparison.OrdinalIgnoreCase))
                {
                    _grid.ClearSelection();
                    row.Selected = true;
                    if (i >= 0) _grid.FirstDisplayedScrollingRowIndex = i;
                    UpdateSelection();
                    break;
                }
            }
        }

        private void ApplyFilter(string query)
        {
            IEnumerable<DocumentRequest> data = _allRequests;
            if (!string.IsNullOrWhiteSpace(query) && query != "Search")
            {
                string q = query.Trim().ToLowerInvariant();
                data = data.Where(d => (d.RequesterName ?? string.Empty).ToLowerInvariant().Contains(q)
                                     || (d.RequestId ?? string.Empty).ToLowerInvariant().Contains(q));
            }
            _bindingSource.DataSource = data.ToList();
        }

        private void ToggleStatusPanel()
        {
            _statusPanel.Visible = !_statusPanel.Visible;
            if (_statusPanel.Visible)
            {
                _cmbStatus.Focus();
            }
        }

        private void UpdateSelection()
        {
            if (_grid.SelectedRows.Count == 0)
            {
                _selectedRequest = null;
                _cmbStatus.SelectedIndex = -1;
                return;
            }

            _selectedRequest = _grid.SelectedRows[0].DataBoundItem as DocumentRequest;
            if (_selectedRequest == null)
            {
                _cmbStatus.SelectedIndex = -1;
                return;
            }

            int statusIndex = Array.IndexOf(StatusOptions, _selectedRequest.Status);
            _cmbStatus.SelectedIndex = (statusIndex >= 0) ? statusIndex : -1;
        }

        private void SaveChanges()
        {
            if (_selectedRequest == null) { ToggleStatusPanel(); return; }

            string newStatus = _cmbStatus.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(newStatus) || newStatus == _selectedRequest.Status)
            {
                ToggleStatusPanel();
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Confirm change status to '{newStatus}' for request {_selectedRequest.RequestId}?",
                "Confirm Status Change",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _repo.UpdateStatus(_selectedRequest.RequestId, newStatus);
                _selectedRequest.Status = newStatus;
                _bindingSource.ResetBindings(false);
                ToggleStatusPanel();
            }
        }

        private void DeleteSelected()
        {
            if (_selectedRequest == null)
            {
                ShowSelectPrompt();
                return;
            }

            DialogResult result = MessageBox.Show(
                $"Are you sure you want to delete request {_selectedRequest.RequestId}?",
                "Delete Request",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _repo.Delete(_selectedRequest.RequestId);
                RefreshRequests();
            }
        }

        private void OpenPreviewEdit()
        {
            if (_selectedRequest == null)
            {
                ShowSelectPrompt();
                return;
            }

            var record = _repo.GetByRequestId(_selectedRequest.RequestId);
            if (record == null)
            {
                MessageBox.Show("Record not found.", "Document Requests", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var entry = new DocumentRequestEntryForm(record))
            {
                var res = entry.ShowDialog(this);
                _pendingSelectId = res == DialogResult.OK ? (entry.SavedRequestId ?? record.RequestId) : null;
                RefreshRequests();
            }
        }

        private void ShowSelectPrompt()
        {
            MessageBox.Show(
                "Select a request first.",
                "Document Requests",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private sealed class DocumentRequest
        {
            internal DocumentRequest() { }

            public string RequestId { get; set; }
            public string Type { get; set; }
            public string RequesterName { get; set; }
            public DateTime DateFiled { get; set; }
            public string Status { get; set; }
            public string Actions { get; set; }

            // extra fields for preview/edit
            public string ContactNumber { get; set; }
            public string Purpose { get; set; }
            public DateTime? PickupDate { get; set; }
            public int Copies { get; set; }
            public string AdditionalRequirements { get; set; }
        }
    }

    internal sealed class DocumentRequestEntryForm : Form
    {
        private const int CardWidth = 420;
        private readonly ComboBox _cmbDocumentType;
        private readonly TextBox _txtApplicantName;
        private readonly TextBox _txtContactNumber;
        private readonly TextBox _txtPurpose;
        private readonly DateTimePicker _dtpPickupDate;
        private readonly NumericUpDown _numCopies;
        private readonly TextBox _txtAdditionalRequirements;
        private readonly Button _btnSubmit;
        private readonly Button _btnSaveDraft;

        private readonly DocumentRequestRepository _repo = new DocumentRequestRepository();
        private readonly DocumentRequestRepository.DocumentRequestRecord _editingRecord;

        public string SavedRequestId { get; private set; }

        internal DocumentRequestEntryForm()
        {
            Text = "Document Request";
            Font = new Font("Segoe UI", 9f);
            Size = new Size(480, 650);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(236, 239, 244);
            Padding = new Padding(16);

            Panel card = new Panel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 28, 24)
            };
            Controls.Add(card);

            int y = 0;
            card.Controls.Add(CreateHeading("Document Request", new Font("Segoe UI Semibold", 16f), ref y));
            card.Controls.Add(CreateHeading("Request official barangay documents and track their processing status.", new Font("Segoe UI", 9f), ref y, Color.DimGray, 12));

            card.Controls.Add(CreateFieldLabel("Document Type *", ref y));
            _cmbDocumentType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = CardWidth,
                Location = new Point(0, y)
            };
            _cmbDocumentType.Items.AddRange(new object[]
            {
                "Barangay Clearance",
                "Barangay Certificate",
                "Business Permit",
                "Residency Certificate",
                "Permit to Construct"
            });
            card.Controls.Add(_cmbDocumentType);
            y = _cmbDocumentType.Bottom + 18;

            int columnSpacing = 16;
            int leftWidth = 220;
            int rightWidth = CardWidth - leftWidth - columnSpacing;

            card.Controls.Add(CreateFieldLabel("Applicant Name *", ref y));
            _txtApplicantName = new TextBox { Width = leftWidth, Location = new Point(0, y) };
            ApplyPlaceholder(_txtApplicantName, "Enter full name");
            card.Controls.Add(_txtApplicantName);

            Label lblContact = new Label
            {
                Text = "Contact Number *",
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(55, 65, 81),
                AutoSize = true,
                Location = new Point(leftWidth + columnSpacing, y - 22)
            };
            card.Controls.Add(lblContact);

            _txtContactNumber = new TextBox
            {
                Width = rightWidth,
                Location = new Point(leftWidth + columnSpacing, y)
            };
            ApplyPlaceholder(_txtContactNumber, "+63 XXX XXX XXXX");
            card.Controls.Add(_txtContactNumber);
            y = _txtApplicantName.Bottom + 20;

            card.Controls.Add(CreateFieldLabel("Purpose *", ref y));
            _txtPurpose = new TextBox { Width = CardWidth, Location = new Point(0, y) };
            ApplyPlaceholder(_txtPurpose, "Purpose of the document");
            card.Controls.Add(_txtPurpose);
            y = _txtPurpose.Bottom + 20;

            card.Controls.Add(CreateFieldLabel("Preferred Pickup Date *", ref y));
            _dtpPickupDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMM dd, yyyy",
                Width = leftWidth,
                Location = new Point(0, y)
            };
            card.Controls.Add(_dtpPickupDate);

            Label lblCopies = new Label
            {
                Text = "Number of Copies",
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(55, 65, 81),
                AutoSize = true,
                Location = new Point(leftWidth + columnSpacing, y - 22)
            };
            card.Controls.Add(lblCopies);

            _numCopies = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 20,
                Value = 1,
                Width = 70,
                Location = new Point(leftWidth + columnSpacing, y)
            };
            card.Controls.Add(_numCopies);
            y = _dtpPickupDate.Bottom + 20;

            card.Controls.Add(CreateFieldLabel("Additional Requirements", ref y));
            _txtAdditionalRequirements = new TextBox
            {
                Multiline = true,
                Width = CardWidth,
                Height = 110,
                Location = new Point(0, y),
                ScrollBars = ScrollBars.Vertical
            };
            ApplyPlaceholder(_txtAdditionalRequirements, "Any special requirements or notes...");
            card.Controls.Add(_txtAdditionalRequirements);
            y = _txtAdditionalRequirements.Bottom + 26;

            Panel buttonRow = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(CardWidth, 40)
            };
            card.Controls.Add(buttonRow);

            _btnSubmit = new Button
            {
                Text = "Submit Request",
                Size = new Size(215, 36),
                BackColor = Color.FromArgb(34, 197, 94),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSubmit.FlatAppearance.BorderSize = 0;
            _btnSubmit.Click += Submit_Click;
            buttonRow.Controls.Add(_btnSubmit);

            _btnSaveDraft = new Button
            {
                Text = "Cancel",
                Size = new Size(150, 36),
                Location = new Point(230, 0),
                BackColor = Color.FromArgb(229, 231, 235),
                ForeColor = Color.FromArgb(55, 65, 81),
                FlatStyle = FlatStyle.Flat
            };
            _btnSaveDraft.FlatAppearance.BorderSize = 0;
            _btnSaveDraft.Click += (s, e) => Close();
            buttonRow.Controls.Add(_btnSaveDraft);
            y = buttonRow.Bottom + 24;

            Panel infoPanel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(CardWidth, 130),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };
            card.Controls.Add(infoPanel);

            Label infoTitle = new Label
            {
                Text = "Processing Information",
                Font = new Font("Segoe UI Semibold", 10f),
                ForeColor = Color.FromArgb(31, 41, 55),
                AutoSize = true,
                Location = new Point(12, 10)
            };
            infoPanel.Controls.Add(infoTitle);

            Label infoDetails = new Label
            {
                AutoSize = false,
                Location = new Point(12, 35),
                Size = new Size(infoPanel.Width - 24, 80),
                Text = "• Processing time: 3-5 business days\r\n• You will receive SMS/email updates\r\n• Bring valid ID when picking up\r\n• Processing fees apply",
                ForeColor = Color.FromArgb(75, 85, 99)
            };
            infoPanel.Controls.Add(infoDetails);

            AcceptButton = _btnSubmit;
        }

        internal DocumentRequestEntryForm(DocumentRequestRepository.DocumentRequestRecord editing)
            : this()
        {
            _editingRecord = editing;
            Text = "Edit Document Request";
            _btnSubmit.Text = "Save Changes";

            // prefill
            _cmbDocumentType.SelectedItem = editing.Type;
            _txtApplicantName.Text = editing.RequesterName;
            _txtApplicantName.ForeColor = Color.FromArgb(31, 41, 55);
            _txtContactNumber.Text = editing.ContactNumber ?? string.Empty;
            _txtContactNumber.ForeColor = Color.FromArgb(31, 41, 55);
            _txtPurpose.Text = editing.Purpose ?? string.Empty;
            _txtPurpose.ForeColor = Color.FromArgb(31, 41, 55);
            _dtpPickupDate.Value = (editing.PickupDate ?? DateTime.Today);
            _numCopies.Value = Math.Max(1, editing.Copies);
            _txtAdditionalRequirements.Text = editing.AdditionalRequirements ?? string.Empty;
            _txtAdditionalRequirements.ForeColor = Color.FromArgb(31, 41, 55);
        }

        private Label CreateHeading(string text, Font font, ref int y, Color? color = null, int margin = 8)
        {
            Label label = new Label
            {
                AutoSize = true,
                Text = text,
                Font = font,
                ForeColor = color ?? Color.FromArgb(30, 30, 30),
                Location = new Point(0, y)
            };
            y = label.Bottom + margin;
            return label;
        }

        private Label CreateFieldLabel(string text, ref int y)
        {
            Label label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(55, 65, 81),
                AutoSize = true,
                Location = new Point(0, y)
            };
            y = label.Bottom + 4;
            return label;
        }

        private void ApplyPlaceholder(TextBox textBox, string placeholder)
        {
            textBox.Tag = placeholder;
            textBox.ForeColor = Color.Gray;
            textBox.Text = placeholder;

            textBox.GotFocus += (s, e) =>
            {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = string.Empty;
                    textBox.ForeColor = Color.FromArgb(31, 41, 55);
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    textBox.Text = placeholder;
                    textBox.ForeColor = Color.Gray;
                }
            };
        }

        private string ReadInput(TextBox textBox)
        {
            string placeholder = textBox.Tag as string;
            string value = textBox.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(placeholder) && value == placeholder && textBox.ForeColor == Color.Gray)
            {
                return string.Empty;
            }

            return value;
        }

        private void Submit_Click(object sender, EventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            if (_editingRecord == null)
            {
                var rec = new DocumentRequestRepository.DocumentRequestRecord
                {
                    Type = _cmbDocumentType.SelectedItem?.ToString(),
                    RequesterName = ReadInput(_txtApplicantName),
                    DateFiled = DateTime.Today,
                    Status = "Pending",
                    ContactNumber = ReadInput(_txtContactNumber),
                    Purpose = ReadInput(_txtPurpose),
                    PickupDate = _dtpPickupDate.Value.Date,
                    Copies = (int)_numCopies.Value,
                    AdditionalRequirements = ReadInput(_txtAdditionalRequirements)
                };
                string newId = _repo.Insert(rec);
                SavedRequestId = newId;

                MessageBox.Show(
                    "Document request submitted successfully.",
                    "Success",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;

                // Reset fields for a clean state before closing
                ResetFormFields();
            }
            else
            {
                _editingRecord.Type = _cmbDocumentType.SelectedItem?.ToString();
                _editingRecord.RequesterName = ReadInput(_txtApplicantName);
                _editingRecord.ContactNumber = ReadInput(_txtContactNumber);
                _editingRecord.Purpose = ReadInput(_txtPurpose);
                _editingRecord.PickupDate = _dtpPickupDate.Value.Date;
                _editingRecord.Copies = (int)_numCopies.Value;
                _editingRecord.AdditionalRequirements = ReadInput(_txtAdditionalRequirements);

                _repo.UpdateDetails(_editingRecord);
                SavedRequestId = _editingRecord.RequestId;

                MessageBox.Show(
                    "Changes saved.",
                    "Document Request",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;

                ResetFormFields();
            }

            Close();
        }

        private void ResetFormFields()
        {
            _cmbDocumentType.SelectedIndex = -1;
            _txtApplicantName.Text = string.Empty;
            _txtContactNumber.Text = string.Empty;
            _txtPurpose.Text = string.Empty;
            _dtpPickupDate.Value = DateTime.Today;
            _numCopies.Value = 1;
            _txtAdditionalRequirements.Text = string.Empty;
        }

        private bool ValidateForm()
        {
            if (_cmbDocumentType.SelectedIndex < 0)
            {
                ShowValidation("Please select a document type.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ReadInput(_txtApplicantName)))
            {
                ShowValidation("Applicant name is required.");
                return false;
            }

            string contact = ReadInput(_txtContactNumber);
            if (string.IsNullOrWhiteSpace(contact) || contact.Count(char.IsDigit) < 9)
            {
                ShowValidation("Provide a valid contact number.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(ReadInput(_txtPurpose)))
            {
                ShowValidation("Please describe the purpose of the document.");
                return false;
            }

            return true;
        }

        private static void ShowValidation(string message)
        {
            MessageBox.Show(
                message,
                "Incomplete Form",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
