namespace BlancoMonitor.UI.Theme;

using BlancoMonitor.UI.Utilities;

public static class NeonTheme
{
    // Core colors
    public static readonly Color Background = Color.FromArgb(5, 5, 5);
    public static readonly Color BackgroundAlt = Color.FromArgb(10, 18, 10);
    public static readonly Color Surface = Color.FromArgb(12, 25, 12);
    public static readonly Color SurfaceHover = Color.FromArgb(0, 40, 0);

    // Text colors
    public static readonly Color TextPrimary = Color.FromArgb(0, 255, 65);
    public static readonly Color TextAccent = Color.FromArgb(57, 255, 20);
    public static readonly Color TextDim = Color.FromArgb(0, 140, 40);

    // Status colors
    public static readonly Color Warning = Color.FromArgb(255, 191, 0);
    public static readonly Color Critical = Color.FromArgb(255, 50, 50);
    public static readonly Color Success = Color.FromArgb(0, 255, 65);

    // Log colors
    public static readonly Color LogCyan = Color.FromArgb(0, 210, 255);
    public static readonly Color LogWhite = Color.FromArgb(200, 220, 200);

    // Border / grid
    public static readonly Color Border = Color.FromArgb(0, 80, 0);
    public static readonly Color GridLine = Color.FromArgb(0, 50, 0);
    public static readonly Color Selection = Color.FromArgb(0, 60, 0);

    // Fonts
    public static readonly Font MonoFont = new("Consolas", 10f, FontStyle.Regular);
    public static readonly Font MonoFontSmall = new("Consolas", 9f, FontStyle.Regular);
    public static readonly Font MonoFontLarge = new("Consolas", 12f, FontStyle.Regular);
    public static readonly Font HeaderFont = new("Consolas", 14f, FontStyle.Bold);

    public static void Apply(Form form)
    {
        form.BackColor = Background;
        form.ForeColor = TextPrimary;
        form.Font = MonoFont;

        ApplyToControls(form.Controls);
    }

    public static void ApplyToControls(Control.ControlCollection controls)
    {
        foreach (Control control in controls)
        {
            ApplyToControl(control);
            if (control.HasChildren)
                ApplyToControls(control.Controls);
        }
    }

    public static void ApplyToControl(Control control)
    {
        control.ForeColor = TextPrimary;
        control.Font = MonoFont;

        switch (control)
        {
            case Button btn:
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderColor = Border;
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = SurfaceHover;
                btn.FlatAppearance.MouseDownBackColor = Surface;
                btn.BackColor = Surface;
                btn.Cursor = Cursors.Hand;
                break;

            case TextBox txt:
                txt.BorderStyle = BorderStyle.FixedSingle;
                txt.BackColor = Surface;
                break;

            case ListBox lst:
                lst.BorderStyle = BorderStyle.FixedSingle;
                lst.BackColor = Surface;
                break;

            case RichTextBox rtb:
                rtb.BorderStyle = BorderStyle.None;
                rtb.BackColor = Background;
                break;

            case DataGridView dgv:
                StyleDataGridView(dgv);
                break;

            case TabControl tab:
                tab.BackColor = Background;
                tab.DrawMode = TabDrawMode.OwnerDrawFixed;
                tab.DrawItem += TabControl_DrawItem;
                break;

            case GroupBox grp:
                grp.BackColor = Background;
                grp.ForeColor = TextAccent;
                break;

            case Label lbl:
                lbl.BackColor = Color.Transparent;
                break;

            case MenuStrip menu:
                menu.BackColor = Background;
                menu.ForeColor = TextPrimary;
                menu.Renderer = new NeonMenuRenderer();
                break;

            case StatusStrip status:
                status.BackColor = Surface;
                status.ForeColor = TextDim;
                status.Renderer = new NeonMenuRenderer();
                break;

            case ProgressBar pb:
                pb.BackColor = Background;
                pb.ForeColor = TextPrimary;
                break;

            case ComboBox cmb:
                cmb.FlatStyle = FlatStyle.Flat;
                cmb.BackColor = Surface;
                break;

            case NumericUpDown nud:
                nud.BackColor = Surface;
                nud.BorderStyle = BorderStyle.FixedSingle;
                break;

            case CheckBox chk:
                chk.BackColor = Color.Transparent;
                chk.FlatStyle = FlatStyle.Flat;
                chk.FlatAppearance.BorderColor = Border;
                chk.FlatAppearance.CheckedBackColor = Surface;
                break;

            case Panel:
                // Preserve panel's explicitly set BackColor (Surface, Background, etc.)
                break;

            default:
                control.BackColor = Background;
                break;
        }
    }

    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = Background;
        dgv.GridColor = GridLine;
        dgv.BorderStyle = BorderStyle.None;
        dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dgv.EnableHeadersVisualStyles = false;
        dgv.RowHeadersVisible = false;
        dgv.AllowUserToAddRows = false;
        dgv.AllowUserToResizeRows = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        dgv.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Background,
            ForeColor = TextPrimary,
            SelectionBackColor = Selection,
            SelectionForeColor = TextAccent,
            Font = MonoFont,
            Padding = new Padding(4, 2, 4, 2),
        };

        dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextAccent,
            Font = MonoFont,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(4),
        };

        dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;

        // Suppress DataGridView error sounds
        dgv.DataError += static (_, e) => e.ThrowException = false;

        // Double-click on any URL-looking cell opens it in the browser
        dgv.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var value = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
            if (BrowserHelper.IsUrl(value))
                BrowserHelper.OpenUrl(value!);
        };

        // Change cursor to hand when hovering over URL cells
        dgv.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var value = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
            dgv.Cursor = BrowserHelper.IsUrl(value) ? Cursors.Hand : Cursors.Default;
        };
        dgv.CellMouseLeave += (_, _) => dgv.Cursor = Cursors.Default;
    }

    private static void TabControl_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not TabControl tab) return;

        var tabPage = tab.TabPages[e.Index];
        var isSelected = e.Index == tab.SelectedIndex;

        using var bgBrush = new SolidBrush(isSelected ? Surface : Background);
        using var fgBrush = new SolidBrush(isSelected ? TextAccent : TextDim);

        e.Graphics.FillRectangle(bgBrush, e.Bounds);
        e.Graphics.DrawString(tabPage.Text, MonoFont, fgBrush, e.Bounds,
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    public static Button CreateButton(string text, int width = 120, int height = 32)
    {
        var btn = new Button
        {
            Text = text,
            Size = new Size(width, height),
            FlatStyle = FlatStyle.Flat,
            BackColor = Surface,
            ForeColor = TextPrimary,
            Font = MonoFont,
            Cursor = Cursors.Hand,
        };
        btn.FlatAppearance.BorderColor = Border;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = SurfaceHover;
        return btn;
    }

    public static Label CreateLabel(string text, bool isHeader = false)
    {
        return new Label
        {
            Text = text,
            ForeColor = isHeader ? TextAccent : TextPrimary,
            Font = isHeader ? HeaderFont : MonoFont,
            AutoSize = true,
            BackColor = Color.Transparent,
        };
    }

    private sealed class NeonMenuRenderer : ToolStripProfessionalRenderer
    {
        public NeonMenuRenderer() : base(new NeonColorTable()) { }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Selected ? TextAccent : TextPrimary;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (e.Item.Selected || e.Item.Pressed)
            {
                using var brush = new SolidBrush(SurfaceHover);
                e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }
            else
            {
                using var brush = new SolidBrush(Background);
                e.Graphics.FillRectangle(brush, new Rectangle(Point.Empty, e.Item.Size));
            }
        }
    }

    private sealed class NeonColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder => Border;
        public override Color MenuItemBorder => Border;
        public override Color MenuItemSelected => SurfaceHover;
        public override Color MenuItemSelectedGradientBegin => SurfaceHover;
        public override Color MenuItemSelectedGradientEnd => SurfaceHover;
        public override Color MenuItemPressedGradientBegin => Surface;
        public override Color MenuItemPressedGradientEnd => Surface;
        public override Color MenuStripGradientBegin => Background;
        public override Color MenuStripGradientEnd => Background;
        public override Color ToolStripDropDownBackground => Background;
        public override Color ImageMarginGradientBegin => Background;
        public override Color ImageMarginGradientEnd => Background;
        public override Color ImageMarginGradientMiddle => Background;
        public override Color SeparatorDark => Border;
        public override Color SeparatorLight => Border;
        public override Color StatusStripGradientBegin => Surface;
        public override Color StatusStripGradientEnd => Surface;
    }
}
