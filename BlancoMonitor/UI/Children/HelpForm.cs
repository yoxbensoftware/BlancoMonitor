using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

/// <summary>
/// Classic desktop help viewer with TreeView navigation on the left
/// and a rich-text content panel on the right. Neon green retro style.
/// </summary>
public sealed class HelpForm : Form
{
    private readonly IHelpContentService _helpContent;
    private readonly ILocalizationService _loc;

    private readonly TreeView _treeView;
    private readonly RichTextBox _contentBox;
    private readonly TextBox _searchBox;
    private readonly Label _headerLabel;
    private readonly SplitContainer _splitter;
    private readonly Panel _topPanel;

    private IReadOnlyList<HelpTopic> _topics = [];

    public HelpForm(IHelpContentService helpContent, ILocalizationService loc)
    {
        _helpContent = helpContent;
        _loc = loc;

        Text = _loc.Get("Help.Title");
        Size = new Size(1100, 720);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(800, 500);

        // ── Top panel: header + search ──────────────────────────
        _topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = NeonTheme.Surface,
            Padding = new Padding(12, 8, 12, 8),
        };

        _headerLabel = new Label
        {
            Text = _loc.Get("Help.Header"),
            Font = NeonTheme.HeaderFont,
            ForeColor = NeonTheme.TextAccent,
            AutoSize = true,
            Location = new Point(12, 8),
        };
        _topPanel.Controls.Add(_headerLabel);

        var searchLabel = new Label
        {
            Text = _loc.Get("Help.Search"),
            Font = NeonTheme.MonoFontSmall,
            ForeColor = NeonTheme.TextDim,
            AutoSize = true,
            Location = new Point(12, 40),
        };
        _topPanel.Controls.Add(searchLabel);

        _searchBox = new TextBox
        {
            Width = 260,
            Location = new Point(searchLabel.PreferredWidth + 20, 37),
            BackColor = NeonTheme.Background,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFontSmall,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _searchBox.TextChanged += SearchBox_TextChanged;
        _topPanel.Controls.Add(_searchBox);

        // ── Splitter: tree (left) + content (right) ─────────────
        _splitter = new SplitContainer
        {
            Dock = DockStyle.Fill,
            SplitterDistance = 280,
            FixedPanel = FixedPanel.Panel1,
            SplitterWidth = 3,
            BackColor = NeonTheme.Border,
            Orientation = Orientation.Vertical,
        };

        // ── Left: TreeView ──────────────────────────────────────
        _treeView = new TreeView
        {
            Dock = DockStyle.Fill,
            BackColor = NeonTheme.Background,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFont,
            BorderStyle = BorderStyle.None,
            ShowLines = true,
            ShowPlusMinus = true,
            ShowRootLines = true,
            FullRowSelect = true,
            HideSelection = false,
            LineColor = NeonTheme.Border,
            Indent = 20,
            ItemHeight = 26,
        };
        _treeView.AfterSelect += TreeView_AfterSelect;
        _splitter.Panel1.Controls.Add(_treeView);
        _splitter.Panel1.BackColor = NeonTheme.Background;

        // ── Right: content area ─────────────────────────────────
        _contentBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = NeonTheme.Background,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFont,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            WordWrap = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Margin = Padding.Empty,
        };
        _splitter.Panel2.Controls.Add(_contentBox);
        _splitter.Panel2.BackColor = NeonTheme.Background;
        _splitter.Panel2.Padding = new Padding(8, 8, 8, 8);

        // Add controls in correct z-order: Fill first (back), then Top (front)
        Controls.Add(_splitter);
        Controls.Add(_topPanel);

        NeonTheme.Apply(this);

        LoadTopics();

        // Auto-select Quick Start node (index 1), fallback to first
        if (_treeView.Nodes.Count > 1)
        {
            _treeView.SelectedNode = _treeView.Nodes[1];
        }
        else if (_treeView.Nodes.Count > 0)
        {
            _treeView.SelectedNode = _treeView.Nodes[0];
        }
    }

    private void LoadTopics()
    {
        _topics = _helpContent.GetTopics(_loc.CurrentLanguage);
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        foreach (var topic in _topics)
        {
            var node = CreateTreeNode(topic);
            _treeView.Nodes.Add(node);
        }

        _treeView.ExpandAll();
        _treeView.EndUpdate();
    }

    private static TreeNode CreateTreeNode(HelpTopic topic)
    {
        var prefix = topic.IconHint switch
        {
            "info" => "ℹ ",
            "play" => "▶ ",
            "grid" => "⊞ ",
            "shield" => "⛨ ",
            "globe" => "⊕ ",
            "question" => "? ",
            "gear" => "⚙ ",
            _ => "  ",
        };

        var node = new TreeNode(prefix + topic.Title)
        {
            Tag = topic,
            ForeColor = topic.Children.Count > 0 ? NeonTheme.TextAccent : NeonTheme.TextPrimary,
        };

        foreach (var child in topic.Children)
        {
            node.Nodes.Add(CreateTreeNode(child));
        }

        return node;
    }

    private void TreeView_AfterSelect(object? sender, TreeViewEventArgs e)
    {
        if (e.Node?.Tag is HelpTopic topic)
        {
            DisplayTopic(topic);
        }
    }

    private void DisplayTopic(HelpTopic topic)
    {
        _contentBox.Clear();
        _contentBox.SelectionFont = NeonTheme.MonoFont;
        _contentBox.SelectionColor = NeonTheme.TextPrimary;
        _contentBox.SelectionIndent = 0;
        _contentBox.SelectionRightIndent = 0;
        _contentBox.SelectionAlignment = HorizontalAlignment.Left;

        if (string.IsNullOrEmpty(topic.Content) && topic.Children.Count > 0)
        {
            // Parent node with no own content — show summary
            AppendLine(topic.Title.ToUpperInvariant(), NeonTheme.HeaderFont, NeonTheme.TextAccent);
            AppendLine(new string('═', 50), NeonTheme.MonoFont, NeonTheme.Border);
            AppendLine("", NeonTheme.MonoFont, NeonTheme.TextPrimary);
            AppendLine(_loc.Get("Help.SelectSubtopic"), NeonTheme.MonoFont, NeonTheme.TextDim);
            return;
        }

        // Render content with syntax coloring for special markers
        var lines = topic.Content.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("═") || line.StartsWith("──"))
            {
                AppendLine(line, NeonTheme.MonoFont, NeonTheme.Border);
            }
            else if (line.TrimStart().StartsWith("✗ ") || line.TrimStart().StartsWith("✗"))
            {
                AppendLine(line, NeonTheme.MonoFont, NeonTheme.Critical);
            }
            else if (line.TrimStart().StartsWith("• ") || line.TrimStart().StartsWith("✓"))
            {
                AppendLine(line, NeonTheme.MonoFont, NeonTheme.Success);
            }
            else if (line.TrimStart().StartsWith("⛨ ") || line.TrimStart().StartsWith("⛨"))
            {
                AppendLine(line, NeonTheme.MonoFont, NeonTheme.Warning);
            }
            else if (line.TrimStart().StartsWith("Q:") || line.TrimStart().StartsWith("F:") || line.TrimStart().StartsWith("S:"))
            {
                AppendLine(line, new Font(NeonTheme.MonoFont, FontStyle.Bold), NeonTheme.TextAccent);
            }
            else if (line.TrimStart().StartsWith("A:") || line.TrimStart().StartsWith("C:"))
            {
                AppendLine(line, NeonTheme.MonoFont, NeonTheme.TextPrimary);
            }
            else if (line == line.ToUpperInvariant() && line.Trim().Length > 3 && !line.StartsWith(" "))
            {
                AppendLine(line, NeonTheme.MonoFontLarge, NeonTheme.TextAccent);
            }
            else if (line.TrimStart().StartsWith("STEP ") || line.TrimStart().StartsWith("SCHRITT ") || line.TrimStart().StartsWith("ADIM "))
            {
                AppendLine(line, new Font(NeonTheme.MonoFont, FontStyle.Bold), NeonTheme.TextAccent);
            }
            else
            {
                AppendLine(line, NeonTheme.MonoFont, NeonTheme.TextPrimary);
            }
        }

        _contentBox.SelectionStart = 0;
        _contentBox.ScrollToCaret();
    }

    private void AppendLine(string text, Font font, Color color)
    {
        _contentBox.SelectionFont = font;
        _contentBox.SelectionColor = color;
        _contentBox.SelectionIndent = 0;
        _contentBox.SelectionAlignment = HorizontalAlignment.Left;
        _contentBox.AppendText(text + "\n");
    }

    private void SearchBox_TextChanged(object? sender, EventArgs e)
    {
        var query = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            LoadTopics();
            if (_treeView.Nodes.Count > 0)
                _treeView.SelectedNode = _treeView.Nodes[0];
            return;
        }

        // Filter topics and rebuild tree
        var filtered = FilterTopics(_topics, query);
        _treeView.BeginUpdate();
        _treeView.Nodes.Clear();

        foreach (var topic in filtered)
        {
            var node = CreateTreeNode(topic);
            _treeView.Nodes.Add(node);
        }

        _treeView.ExpandAll();
        _treeView.EndUpdate();

        if (_treeView.Nodes.Count > 0)
            _treeView.SelectedNode = _treeView.Nodes[0];
    }

    private static List<HelpTopic> FilterTopics(IReadOnlyList<HelpTopic> topics, string query)
    {
        var result = new List<HelpTopic>();
        foreach (var topic in topics)
        {
            var matchesSelf = topic.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                              topic.Content.Contains(query, StringComparison.OrdinalIgnoreCase);

            var filteredChildren = FilterTopics(topic.Children, query);

            if (matchesSelf || filteredChildren.Count > 0)
            {
                result.Add(new HelpTopic
                {
                    Key = topic.Key,
                    Title = topic.Title,
                    Content = topic.Content,
                    IconHint = topic.IconHint,
                    Children = matchesSelf ? topic.Children : filteredChildren,
                });
            }
        }
        return result;
    }
}
