using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    [DefaultProperty(nameof(TitleText))]
    [DefaultEvent(nameof(Click))]
    public class SidebarButton : UserControl
    {
        private bool _active;
        private bool _collapsed;

        private Color _bgNormal = Color.FromArgb(248, 249, 251);
        private Color _bgHover = Color.FromArgb(236, 238, 241);
        private Color _bgActive = Color.FromArgb(33, 37, 41);
        private Color _fgNormal = Color.FromArgb(55, 65, 81);
        private Color _fgActive = Color.White;
        private Color _accent = Color.FromArgb(0, 122, 204);

        private string _iconGlyph = "";
        private string _titleText = "";

        // MODIFICADO: Aumentamos el padding para mover el texto más a la derecha
        private readonly Padding _expandedPadding = new Padding(16, 0, 12, 0);
        private readonly Padding _collapsedPadding = new Padding(0);

        private Panel? _contentPanel;

        [Browsable(false)] public Panel ActiveIndicator { get; private set; }
        [Browsable(false)] public Label IconLabel { get; private set; }
        [Browsable(false)] public Label TitleLabel { get; private set; }

        [Category("Sidebar")]
        public Color BgNormal { get => _bgNormal; set { _bgNormal = value; if (!_active) BackColor = _bgNormal; } }
        [Category("Sidebar")]
        public Color BgHover { get => _bgHover; set { _bgHover = value; } }
        [Category("Sidebar")]
        public Color BgActive { get => _bgActive; set { _bgActive = value; if (_active) BackColor = _bgActive; } }
        [Category("Sidebar")]
        public Color FgNormal { get => _fgNormal; set { _fgNormal = value; if (!_active) ApplyFore(_fgNormal); } }
        [Category("Sidebar")]
        public Color FgActive { get => _fgActive; set { _fgActive = value; if (_active) ApplyFore(_fgActive); } }
        [Category("Sidebar")]
        public Color Accent { get => _accent; set { _accent = value; if (ActiveIndicator != null) ActiveIndicator.BackColor = _accent; } }

        [Category("Sidebar")]
        public string IconGlyph
        {
            get => _iconGlyph;
            set { _iconGlyph = value ?? ""; if (IconLabel != null) IconLabel.Text = _iconGlyph; }
        }

        [Category("Sidebar")]
        public string TitleText
        {
            get => _titleText;
            set { _titleText = value ?? ""; if (TitleLabel != null) TitleLabel.Text = _titleText; }
        }

        public SidebarButton()
        {
            Height = 40;
            Margin = new Padding(0);
            Cursor = Cursors.Hand;
            BackColor = _bgNormal;
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            ActiveIndicator = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = _accent,
                Visible = false
            };
            Controls.Add(ActiveIndicator);

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = _expandedPadding
            };
            Controls.Add(_contentPanel);

            IconLabel = new Label
            {
                Dock = DockStyle.Left,
                Width = 32,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe MDL2 Assets", 16f),
                ForeColor = _fgNormal,
                Text = _iconGlyph,
                AutoSize = false,
                Margin = new Padding(0)
            };
            _contentPanel.Controls.Add(IconLabel);

            TitleLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = _fgNormal,
                Text = _titleText,
                AutoSize = false,
                AutoEllipsis = true,
                Margin = new Padding(0),
      //mod: mas a la derecha
                Padding = new Padding(48, 0, 0, 0)
            };
            _contentPanel.Controls.Add(TitleLabel);

            WireChildren(this);
        }

        private void WireChildren(Control root)
        {
            foreach (Control child in root.Controls)
            {
                Hook(child);
                if (child.HasChildren) WireChildren(child);
            }
        }

        private void Hook(Control c)
        {
            c.Click += Child_Click;
            c.MouseEnter += (s, e) => { if (!_active) BackColor = _bgHover; };
            c.MouseLeave += (s, e) => { if (!_active) BackColor = _bgNormal; };
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            Hook(e.Control);
            WireChildren(e.Control);
        }

        private void Child_Click(object sender, EventArgs e) => OnClick(e);

        private void ApplyFore(Color c)
        {
            TitleLabel.ForeColor = c;
            IconLabel.ForeColor = c;
            ForeColor = c;
        }

        public void SetActive(bool active)
        {
            _active = active;
            ActiveIndicator.Visible = active;
            BackColor = active ? _bgActive : _bgNormal;
            ApplyFore(active ? _fgActive : _fgNormal);
        }

        public void SetCollapsed(bool collapsed)
        {
            _collapsed = collapsed;

            TitleLabel.Visible = !collapsed;

            if (_contentPanel != null)
            {
                _contentPanel.Padding = collapsed ? _collapsedPadding : _expandedPadding;

                IconLabel.Visible = true;
                IconLabel.Dock = collapsed ? DockStyle.Fill : DockStyle.Left;
                IconLabel.TextAlign = ContentAlignment.MiddleCenter;
                if (!collapsed) IconLabel.Width = 32;
            }

            PerformLayout();
            Invalidate();
            Update();
        }
    }
}