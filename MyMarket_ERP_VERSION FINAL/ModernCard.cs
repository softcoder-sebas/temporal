using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

// Alias para evitar la ambigüedad con System.Threading.Timer
using WinFormsTimer = System.Windows.Forms.Timer;

namespace MyMarket_ERP
{
    /// <summary>
    /// Panel con diseño de tarjeta y una animación suave de hover.
    /// </summary>
    public class ModernCard : Panel
    {
        private readonly Label _title;
        private readonly Label _value;
        private readonly Label _meta;
        private readonly Label _icon;
        private readonly TableLayoutPanel _contentLayout;
        private readonly TableLayoutPanel _textLayout;

        private readonly WinFormsTimer _animTimer;
        private float _targetGlow;
        private float _currentGlow;

        private static readonly ToolTip SharedToolTip = CreateSharedToolTip();

        private Color _accentColor = ModernTheme.AccentSecondary;
        private string _iconToolTipText = string.Empty;
        private string _iconGlyph = string.Empty;

        public ModernCard()
        {
            DoubleBuffered = true;
            BackColor = ModernTheme.Surface;
            Padding = new Padding(20);
            Margin = new Padding(8);
            Size = new Size(220, 120);
            BorderStyle = BorderStyle.None;

            _contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            Controls.Add(_contentLayout);

            _textLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            _textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _contentLayout.Controls.Add(_textLayout, 0, 0);

            _icon = new Label
            {
                Width = 44,
                Height = 44,
                Margin = new Padding(16, 4, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false,
                AccessibleRole = AccessibleRole.Graphic,
                TabStop = false,
                Font = new Font("Segoe MDL2 Assets", 28f, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = ModernTheme.AccentSecondary,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false
            };
            _contentLayout.Controls.Add(_icon, 1, 0);

            _title = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = ModernTheme.Body,
                ForeColor = ModernTheme.TextSecondary,
                Text = "Título",
                AutoEllipsis = true
            };
            _textLayout.Controls.Add(_title, 0, 0);

            _value = new Label
            {
                Dock = DockStyle.Top,
                Height = 46,
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ForeColor = ModernTheme.TextPrimary,
                Text = "0",
                AutoEllipsis = true
            };
            _textLayout.Controls.Add(_value, 0, 1);

            _meta = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = ModernTheme.TextSecondary,
                Text = "",
                AutoEllipsis = true
            };
            _textLayout.Controls.Add(_meta, 0, 2);

            Cursor = Cursors.Hand;

            _animTimer = new WinFormsTimer { Interval = 16 };
            _animTimer.Tick += (_, __) => AnimateGlow();

            MouseEnter += (_, __) => StartGlow(1f);
            MouseLeave += (_, __) => StartGlow(0f);
        }

        [Category("ModernCard")]
        [Description("Texto de título que se muestra en la tarjeta.")]
        [DefaultValue("Título")]
        public string Title
        {
            get => _title.Text;
            set => _title.Text = value;
        }

        [Category("ModernCard")]
        [Description("Valor principal grande de la tarjeta.")]
        [DefaultValue("0")]
        public string Value
        {
            get => _value.Text;
            set => _value.Text = value;
        }

        [Category("ModernCard")]
        [Description("Texto secundario o meta información.")]
        [DefaultValue("")]
        public string Meta
        {
            get => _meta.Text;
            set => _meta.Text = value;
        }

        [Category("ModernCard")]
        [Description("Glifo (Segoe MDL2 Assets) que se muestra como icono en la tarjeta.")]
        [DefaultValue("")]
        public string IconGlyph
        {
            get => _iconGlyph;
            set
            {
                _iconGlyph = value ?? string.Empty;
                if (string.IsNullOrWhiteSpace(_iconGlyph))
                {
                    _icon.Visible = false;
                    _icon.Text = string.Empty;
                }
                else
                {
                    _icon.Visible = true;
                    _icon.Text = _iconGlyph;
                    _icon.ForeColor = _accentColor;
                }
            }
        }

        [Category("ModernCard")]
        [Description("Color de acento para el icono.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color AccentColor
        {
            get => _accentColor;
            set
            {
                if (_accentColor == value)
                    return;
                _accentColor = value;
                if (!string.IsNullOrWhiteSpace(_iconGlyph))
                {
                    _icon.ForeColor = _accentColor;
                }
            }
        }

        [Category("ModernCard")]
        [Description("Texto descriptivo del icono para tooltip y accesibilidad.")]
        [DefaultValue("")]
        public string IconToolTip
        {
            get => _iconToolTipText;
            set
            {
                _iconToolTipText = value ?? string.Empty;
                SharedToolTip.SetToolTip(_icon, string.IsNullOrWhiteSpace(_iconToolTipText) ? null : _iconToolTipText);
                _icon.AccessibleDescription = _iconToolTipText;
            }
        }

        private void StartGlow(float target)
        {
            _targetGlow = target;
            if (!_animTimer.Enabled)
                _animTimer.Start();
        }

        private void AnimateGlow()
        {
            _currentGlow = Lerp(_currentGlow, _targetGlow, 0.15f);
            if (Math.Abs(_currentGlow - _targetGlow) < 0.01f)
            {
                _currentGlow = _targetGlow;
                _animTimer.Stop();
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using GraphicsPath path = RoundRect(ClientRectangle, 18);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);

            if (_currentGlow > 0f)
            {
                int alpha = (int)(40 * _currentGlow);
                using var glow = new Pen(Color.FromArgb(alpha, ModernTheme.AccentSecondary), 2f);
                e.Graphics.DrawPath(glow, path);
            }

            using var borderPen = new Pen(ModernTheme.Border);
            e.Graphics.DrawPath(borderPen, path);
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SharedToolTip.SetToolTip(_icon, null);
            }
            base.Dispose(disposing);
        }

        private static float Lerp(float start, float end, float amount)
            => start + (end - start) * amount;

        private static GraphicsPath RoundRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static ToolTip CreateSharedToolTip()
        {
            return new ToolTip
            {
                AutomaticDelay = 200,
                ReshowDelay = 100,
                InitialDelay = 200,
                ShowAlways = true
            };
        }
    }
}
