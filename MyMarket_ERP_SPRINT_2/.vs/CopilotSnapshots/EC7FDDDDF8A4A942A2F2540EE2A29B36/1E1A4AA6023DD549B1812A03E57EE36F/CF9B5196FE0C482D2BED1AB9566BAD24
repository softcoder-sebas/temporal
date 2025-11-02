using System.Drawing;
using System.Windows.Forms;

namespace MyMarket_ERP
{
    public static class ModernTheme
    {
        // === Colores base ===
        public static readonly Color Background = Color.FromArgb(248, 250, 252);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceAlt = Color.FromArgb(248, 250, 252);
        public static readonly Color Border = Color.FromArgb(226, 232, 240);

        // === Textos ===
        public static readonly Color TextPrimary = Color.FromArgb(15, 23, 42);
        public static readonly Color TextSecondary = Color.FromArgb(100, 116, 139);

        // === Acentos ===
        public static readonly Color Accent = Color.FromArgb(59, 130, 246);
        public static readonly Color AccentSecondary = Color.FromArgb(99, 102, 241);
        public static readonly Color AccentWarning = Color.FromArgb(245, 158, 11);
        public static readonly Color AccentSuccess = Color.FromArgb(34, 197, 94);
        public static readonly Color AccentDanger = Color.FromArgb(239, 68, 68);

        // Alias para compatibilidad: algunos archivos usan "Primary"
        public static readonly Color Primary = Accent; // <--- agregado

        // === Fuentes ===
        public static readonly Font Body = new Font("Segoe UI", 10f, FontStyle.Regular);
        public static readonly Font Heading1 = new Font("Segoe UI", 28f, FontStyle.Bold);
        public static readonly Font Heading2 = new Font("Segoe UI", 22f, FontStyle.Bold);
        public static readonly Font Heading3 = new Font("Segoe UI", 16f, FontStyle.Bold);
        public static readonly Font Caption = new Font("Segoe UI", 9f, FontStyle.Regular);  // ← AGREGAR ESTA LÍNEA

        // === Botones ===
        public static Button CreatePrimaryButton(string text)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = AccentSecondary,
                FlatStyle = FlatStyle.Flat,
                Height = 40,
                Width = 160,
                Cursor = Cursors.Hand,
                FlatAppearance =
                {
                    BorderSize = 0,
                    MouseOverBackColor = Color.FromArgb(79, 70, 229)
                }
            };
        }

        public static Button CreateSecondaryButton(string text)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = AccentSecondary,
                BackColor = Color.FromArgb(238, 242, 255),
                FlatStyle = FlatStyle.Flat,
                Height = 40,
                Width = 140,
                Cursor = Cursors.Hand,
                FlatAppearance =
                {
                    BorderSize = 0,
                    MouseOverBackColor = Color.FromArgb(224, 231, 255)
                }
            };
        }

        public static Button CreateGhostButton(string text)
        {
            return new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                ForeColor = TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Height = 40,
                Width = 140,
                Cursor = Cursors.Hand,
                FlatAppearance =
                {
                    BorderSize = 1,
                    BorderColor = Border,
                    MouseOverBackColor = SurfaceAlt
                }
            };
        }

        // === DataGridView Styling ===
        public static void StyleDataGrid(DataGridView grid)
        {
            grid.BackgroundColor = Surface;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.EnableHeadersVisualStyles = false;
            grid.GridColor = Border;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Header style
            grid.ColumnHeadersDefaultCellStyle.BackColor = SurfaceAlt;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextSecondary;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 8, 12, 8);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = SurfaceAlt;
            grid.ColumnHeadersHeight = 44;

            // Cell style
            grid.DefaultCellStyle.BackColor = Surface;
            grid.DefaultCellStyle.ForeColor = TextPrimary;
            grid.DefaultCellStyle.Font = Body;
            grid.DefaultCellStyle.Padding = new Padding(12, 4, 12, 4);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 231, 255);
            grid.DefaultCellStyle.SelectionForeColor = TextPrimary;
            grid.RowTemplate.Height = 48;

            // Alternate row style
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 252);
        }
    }
}