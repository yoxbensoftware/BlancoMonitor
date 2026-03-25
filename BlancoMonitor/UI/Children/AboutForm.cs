using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Controls;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class AboutForm : Form
{
    private bool _policyAccepted;
    private readonly CheckBox _policyCheckBox;
    private readonly Button _closeBtn;
    private readonly ILocalizationService _loc;
    private readonly IVersionProvider _version;

    public bool PolicyAccepted => _policyAccepted;

    public AboutForm(ILocalizationService loc, IVersionProvider version)
    {
        _loc = loc;
        _version = version;

        Text = _loc.Get("About.Title");
        Size = new Size(700, 740);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Icon = SystemIcons.Shield;

        // ── Scrollable content panel ────────────────────────────
        var contentPanel = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(ClientSize.Width, ClientSize.Height - 60),
            AutoScroll = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        };

        int y = BuildContent(contentPanel);

        var spacer = new Panel
        {
            Location = new Point(0, y + 10),
            Size = new Size(1, 1),
            BackColor = Color.Transparent,
        };
        contentPanel.Controls.Add(spacer);
        Controls.Add(contentPanel);

        // ── Bottom bar ──────────────────────────────────────────
        var bottomPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = NeonTheme.Surface,
        };

        var bottomSep = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = NeonTheme.Border,
        };
        bottomPanel.Controls.Add(bottomSep);

        _policyCheckBox = new CheckBox
        {
            Text = _loc.Get("About.PolicyToggle"),
            Font = NeonTheme.MonoFontSmall,
            ForeColor = NeonTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(20, 18),
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
        };
        _policyCheckBox.FlatAppearance.BorderColor = NeonTheme.Border;
        _policyCheckBox.CheckedChanged += (_, _) =>
        {
            _policyAccepted = _policyCheckBox.Checked;
            _closeBtn.Text = _policyAccepted ? _loc.Get("Btn.AcceptClose") : _loc.Get("Btn.Close");
            _closeBtn.ForeColor = _policyAccepted ? NeonTheme.Success : NeonTheme.TextPrimary;
        };

        _closeBtn = NeonTheme.CreateButton(_loc.Get("Btn.Close"), 160, 32);
        _closeBtn.Location = new Point(ClientSize.Width - 180, 12);
        _closeBtn.Anchor = AnchorStyles.Right | AnchorStyles.Top;
        _closeBtn.Click += (_, _) =>
        {
            DialogResult = _policyAccepted ? DialogResult.OK : DialogResult.Cancel;
            Close();
        };

        bottomPanel.Controls.AddRange([_policyCheckBox, _closeBtn]);
        Controls.Add(bottomPanel);

        NeonTheme.Apply(this);
        bottomPanel.BackColor = NeonTheme.Surface;
        _policyCheckBox.BackColor = NeonTheme.Surface;
    }

    private int BuildContent(Panel parent)
    {
        const int margin = 28;
        const int sectionGap = 18;
        int contentWidth = parent.ClientSize.Width - margin * 2 - 20;
        int y = 15;

        // ── Logo + ASCII Banner side by side ────────────────────
        var logo = new BlancoLogo
        {
            CurrentSize = BlancoLogo.LogoSize.Medium,
            Location = new Point(margin, y),
        };
        parent.Controls.Add(logo);

        var banner = new Label
        {
            Text = @"  ██████╗ ██╗      █████╗ ███╗   ██╗ ██████╗ ██████╗
  ██╔══██╗██║     ██╔══██╗████╗  ██║██╔════╝██╔═══██╗
  ██████╔╝██║     ███████║██╔██╗ ██║██║     ██║   ██║
  ██╔══██╗██║     ██╔══██╗██║╚██╗██║██║     ██║   ██║
  ██████╔╝███████╗██║  ██║██║ ╚████║╚██████╗╚██████╔╝
  ╚═════╝ ╚══════╝╚═╝  ╚═╝╚═╝  ╚═══╝ ╚═════╝ ╚═════╝",
            Font = new Font("Consolas", 7f),
            ForeColor = NeonTheme.TextDim,
            AutoSize = true,
            Location = new Point(margin + 90, y + 5),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(banner);
        y += Math.Max(logo.Height, banner.PreferredHeight) + 8;

        // ── Title + Version ─────────────────────────────────────
        var title = new Label
        {
            Text = "⬡  BLANCO MONITOR",
            Font = new Font("Consolas", 16f, FontStyle.Bold),
            ForeColor = NeonTheme.TextAccent,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(title);
        y += title.PreferredHeight + 2;

        var version = new Label
        {
            Text = _loc.Get("About.Version", _version.DisplayVersion),
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.TextDim,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(version);
        y += version.PreferredHeight + 4;

        var tagline = new Label
        {
            Text = _loc.Get("About.Tagline"),
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.TextPrimary,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(tagline);
        y += tagline.PreferredHeight + sectionGap;

        y = AddSeparator(parent, y, contentWidth, margin);

        // ── WHAT THIS TOOL DOES ─────────────────────────────────
        y = AddSectionHeader(parent, _loc.Get("About.WhatDoes"), NeonTheme.Success, margin, y);
        y = AddLocalizedBullets(parent, margin, y, contentWidth, NeonTheme.TextPrimary, "About.Does", 7);
        y += sectionGap;

        // ── WHAT THIS TOOL DOES NOT DO ──────────────────────────
        y = AddSectionHeader(parent, _loc.Get("About.WhatDoesNot"), NeonTheme.Critical, margin, y);
        y = AddLocalizedBullets(parent, margin, y, contentWidth, NeonTheme.TextPrimary, "About.DoesNot", 9);
        y += sectionGap;

        y = AddSeparator(parent, y, contentWidth, margin);

        // ── TECHNICAL SAFEGUARDS ────────────────────────────────
        y = AddSectionHeader(parent, _loc.Get("About.Safeguards"), NeonTheme.Warning, margin, y);
        y = AddSafeguardBlock(parent, margin, y, contentWidth);
        y += sectionGap;

        y = AddSeparator(parent, y, contentWidth, margin);

        // ── HOW IT WORKS ────────────────────────────────────────
        y = AddSectionHeader(parent, _loc.Get("About.HowItWorks"), NeonTheme.TextAccent, margin, y);
        var howItWorks = new Label
        {
            Text = _loc.Get("About.HowItWorks.Body"),
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.TextPrimary,
            AutoSize = false,
            Size = new Size(contentWidth, 160),
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(howItWorks);
        y += howItWorks.Height + sectionGap;

        y = AddSeparator(parent, y, contentWidth, margin);

        // ── TECHNOLOGY ──────────────────────────────────────────
        y = AddSectionHeader(parent, _loc.Get("About.Technology"), NeonTheme.TextDim, margin, y);
        var tech = new Label
        {
            Text = "Runtime     C# / .NET 10 / WinForms\n" +
                   "Storage     SQLite (WAL mode, local only)\n" +
                   "Network     System.Net.Http.HttpClient\n" +
                   "Reports     HTML / JSON / CSV\n" +
                   "Design      Clean Architecture\n" +
                   "License     Internal use only",
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.TextDim,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(tech);
        y += tech.PreferredHeight + sectionGap;

        y = AddSeparator(parent, y, contentWidth, margin);

        // ── USAGE POLICY ────────────────────────────────────────
        y = AddSectionHeader(parent, _loc.Get("About.UsagePolicy"), NeonTheme.Warning, margin, y);
        var policy = new Label
        {
            Text = _loc.Get("About.Policy.Body"),
            Font = NeonTheme.MonoFont,
            ForeColor = NeonTheme.TextPrimary,
            AutoSize = false,
            Size = new Size(contentWidth, 185),
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(policy);
        y += policy.Height + sectionGap;

        // ── Footer ──────────────────────────────────────────────
        y = AddSeparator(parent, y, contentWidth, margin);
        var footer = new Label
        {
            Text = $"© {DateTime.Now.Year} Oz  —  BlancoMonitor  —  Built with care",
            Font = NeonTheme.MonoFontSmall,
            ForeColor = NeonTheme.TextDim,
            AutoSize = true,
            Location = new Point(margin, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(footer);
        y += footer.PreferredHeight + 15;

        return y;
    }

    private int AddLocalizedBullets(Panel parent, int x, int y, int width, Color color, string keyPrefix, int count)
    {
        for (int i = 1; i <= count; i++)
        {
            var bullet = new Label
            {
                Text = $"  ›  {_loc.Get($"{keyPrefix}.{i}")}",
                Font = NeonTheme.MonoFontSmall,
                ForeColor = color,
                AutoSize = false,
                Size = new Size(width - 10, 18),
                Location = new Point(x + 10, y),
                BackColor = Color.Transparent,
            };
            parent.Controls.Add(bullet);
            y += 19;
        }
        return y;
    }

    private int AddSafeguardBlock(Panel parent, int x, int y, int width)
    {
        var safeguardKeys = new[]
        {
            "RateLimit", "Scope", "Whitelist", "Methods", "Pacing", "UserAgent"
        };

        foreach (var key in safeguardKeys)
        {
            var nameLabel = new Label
            {
                Text = $"  ● {_loc.Get($"About.Safeguard.{key}.Name")}",
                Font = new Font("Consolas", 9.5f, FontStyle.Bold),
                ForeColor = NeonTheme.Success,
                AutoSize = true,
                Location = new Point(x + 10, y),
                BackColor = Color.Transparent,
            };
            parent.Controls.Add(nameLabel);
            y += nameLabel.PreferredHeight + 1;

            var detailLabel = new Label
            {
                Text = $"    {_loc.Get($"About.Safeguard.{key}.Detail")}",
                Font = NeonTheme.MonoFontSmall,
                ForeColor = NeonTheme.TextDim,
                AutoSize = false,
                Size = new Size(width - 30, 34),
                Location = new Point(x + 10, y),
                BackColor = Color.Transparent,
            };
            parent.Controls.Add(detailLabel);
            y += detailLabel.Height + 6;
        }

        return y;
    }

    private static int AddSectionHeader(Panel parent, string text, Color color, int x, int y)
    {
        var label = new Label
        {
            Text = text,
            Font = new Font("Consolas", 11f, FontStyle.Bold),
            ForeColor = color,
            AutoSize = true,
            Location = new Point(x, y),
            BackColor = Color.Transparent,
        };
        parent.Controls.Add(label);
        return y + label.PreferredHeight + 8;
    }

    private static int AddSeparator(Panel parent, int y, int width, int margin)
    {
        var sep = new Panel
        {
            Location = new Point(margin, y),
            Size = new Size(width, 1),
            BackColor = NeonTheme.Border,
        };
        parent.Controls.Add(sep);
        return y + 12;
    }
}
