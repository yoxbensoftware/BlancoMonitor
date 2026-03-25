using BlancoMonitor.Domain.Entities;
using BlancoMonitor.Domain.Enums;
using BlancoMonitor.Domain.Interfaces;
using BlancoMonitor.UI.Theme;

namespace BlancoMonitor.UI.Children;

public sealed class ScenarioManagerForm : Form
{
    private readonly IBlancoDatabase _database;
    private readonly IAppLogger _logger;
    private readonly DataGridView _scenarioGrid;
    private readonly ListBox _stepsList;
    private readonly TextBox _nameBox;
    private readonly ComboBox _actionCombo;
    private readonly TextBox _selectorBox;
    private readonly TextBox _valueBox;
    private readonly Button _addScenarioButton;
    private readonly Button _addStepButton;
    private readonly Button _removeStepButton;

    private Guid? _selectedScenarioId;
    private readonly List<ScenarioStep> _currentSteps = [];

    public ScenarioManagerForm(IBlancoDatabase database, IAppLogger logger)
    {
        _database = database;
        _logger = logger;

        Text = "Scenario Management";
        Size = new Size(1100, 700);
        StartPosition = FormStartPosition.Manual;
        Location = new Point(40, 35);
        MinimumSize = new Size(900, 600);

        var titleLabel = NeonTheme.CreateLabel("▸ SCENARIO MANAGEMENT", isHeader: true);
        titleLabel.Location = new Point(20, 15);

        // Scenario list (left side)
        var listLabel = NeonTheme.CreateLabel("SCENARIOS", isHeader: false);
        listLabel.ForeColor = NeonTheme.TextAccent;
        listLabel.Location = new Point(20, 50);

        _scenarioGrid = new DataGridView
        {
            Location = new Point(20, 75),
            Size = new Size(500, 320),
        };
        NeonTheme.StyleDataGridView(_scenarioGrid);
        _scenarioGrid.Columns.AddRange(
        [
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "NAME", FillWeight = 40 },
            new DataGridViewTextBoxColumn { Name = "Steps", HeaderText = "STEPS", FillWeight = 15 },
            new DataGridViewTextBoxColumn { Name = "Keywords", HeaderText = "KEYWORDS", FillWeight = 25 },
            new DataGridViewTextBoxColumn { Name = "Created", HeaderText = "CREATED", FillWeight = 20 },
        ]);
        _scenarioGrid.SelectionChanged += ScenarioGrid_SelectionChanged;

        // New scenario input
        var inputPanel = new Panel
        {
            Location = new Point(20, 410),
            Size = new Size(500, 60),
            BackColor = NeonTheme.Surface,
        };

        var nameLabel = new Label { Text = "Name:", Location = new Point(10, 18), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent };
        _nameBox = new TextBox
        {
            Location = new Point(80, 16),
            Size = new Size(270, 24),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
        };

        _addScenarioButton = NeonTheme.CreateButton("+ CREATE", 120, 28);
        _addScenarioButton.Location = new Point(365, 14);
        _addScenarioButton.Click += AddScenario_Click;

        inputPanel.Controls.AddRange([nameLabel, _nameBox, _addScenarioButton]);

        // Step editor (right side)
        var stepLabel = NeonTheme.CreateLabel("STEPS", isHeader: false);
        stepLabel.ForeColor = NeonTheme.TextAccent;
        stepLabel.Location = new Point(540, 50);

        _stepsList = new ListBox
        {
            Location = new Point(540, 75),
            Size = new Size(530, 220),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            Font = NeonTheme.MonoFont,
            BorderStyle = BorderStyle.FixedSingle,
        };

        // Step input
        var stepInputPanel = new Panel
        {
            Location = new Point(540, 310),
            Size = new Size(530, 130),
            BackColor = NeonTheme.Surface,
        };

        var actLabel = new Label { Text = "Action:", Location = new Point(10, 15), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _actionCombo = new ComboBox
        {
            Location = new Point(120, 12),
            Size = new Size(180, 24),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
        };
        foreach (var action in Enum.GetNames<ScenarioActionType>())
            _actionCombo.Items.Add(action);
        _actionCombo.SelectedIndex = 0;

        var selLabel = new Label { Text = "Selector:", Location = new Point(10, 50), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _selectorBox = new TextBox
        {
            Location = new Point(120, 48),
            Size = new Size(280, 24),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "CSS selector or URL",
        };

        var valLabel = new Label { Text = "Value:", Location = new Point(10, 85), AutoSize = true, ForeColor = NeonTheme.TextDim, BackColor = Color.Transparent, Font = NeonTheme.MonoFont };
        _valueBox = new TextBox
        {
            Location = new Point(120, 83),
            Size = new Size(200, 24),
            BackColor = NeonTheme.Surface,
            ForeColor = NeonTheme.TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Input value / keyword",
        };

        _addStepButton = NeonTheme.CreateButton("+ STEP", 90, 28);
        _addStepButton.Location = new Point(420, 80);
        _addStepButton.Click += AddStep_Click;

        _removeStepButton = NeonTheme.CreateButton("- STEP", 90, 28);
        _removeStepButton.Location = new Point(420, 45);
        _removeStepButton.Click += RemoveStep_Click;

        stepInputPanel.Controls.AddRange([actLabel, _actionCombo, selLabel, _selectorBox, valLabel, _valueBox, _addStepButton, _removeStepButton]);

        // Save steps button
        var saveStepsBtn = NeonTheme.CreateButton("💾 SAVE SCENARIO", 180, 38);
        saveStepsBtn.Location = new Point(540, 455);
        saveStepsBtn.Click += SaveScenario_Click;

        Controls.AddRange([titleLabel, listLabel, _scenarioGrid, inputPanel, stepLabel, _stepsList, stepInputPanel, saveStepsBtn]);
        NeonTheme.Apply(this);

        Load += async (_, _) => await LoadScenarios();
    }

    private async Task LoadScenarios()
    {
        try
        {
            var scenarios = await _database.GetScenariosAsync();
            _scenarioGrid.Rows.Clear();
            foreach (var s in scenarios)
            {
                _scenarioGrid.Rows.Add(s.Name, s.StepsJson?.Length > 2 ? "Yes" : "—", s.Description ?? "—", s.CreatedAt.ToString("MM-dd HH:mm"));
            }
            _scenarioGrid.Tag = scenarios;
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to load scenarios", ex);
        }
    }

    private void ScenarioGrid_SelectionChanged(object? sender, EventArgs e)
    {
        if (_scenarioGrid.Tag is not List<Domain.Entities.Scenario> scenarios) return;
        if (_scenarioGrid.CurrentRow is null) return;

        var idx = _scenarioGrid.CurrentRow.Index;
        if (idx < 0 || idx >= scenarios.Count) return;

        var scenario = scenarios[idx];
        _selectedScenarioId = scenario.Id;
        _nameBox.Text = scenario.Name;

        _currentSteps.Clear();
        _stepsList.Items.Clear();

        // Parse steps from JSON if available
        if (!string.IsNullOrEmpty(scenario.StepsJson) && scenario.StepsJson != "[]")
        {
            try
            {
                var steps = System.Text.Json.JsonSerializer.Deserialize<List<ScenarioStep>>(scenario.StepsJson);
                if (steps is not null)
                {
                    _currentSteps.AddRange(steps);
                    foreach (var step in _currentSteps)
                        _stepsList.Items.Add($"{step.Order}. {step.ActionType} → {step.Selector ?? step.Value ?? "—"}");
                }
            }
            catch { /* ignore parse errors */ }
        }
    }

    private async void AddScenario_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            _logger.Warning("Scenario name is required");
            return;
        }

        // Get a UrlSetId — use the first available UrlSet, or create a placeholder
        Guid urlSetId;
        try
        {
            var urlSets = await _database.GetUrlSetsAsync();
            if (urlSets.Count == 0)
            {
                // Create a default UrlSet for scenarios
                var defaultSet = new UrlSet
                {
                    Id = Guid.NewGuid(),
                    Name = "Default Scenario Set",
                    CreatedAt = DateTime.UtcNow,
                };
                await _database.InsertUrlSetAsync(defaultSet);
                urlSetId = defaultSet.Id;
            }
            else
            {
                urlSetId = urlSets[0].Id;
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to prepare UrlSet for scenario", ex);
            return;
        }

        var scenario = new Domain.Entities.Scenario
        {
            Id = Guid.NewGuid(),
            Name = _nameBox.Text.Trim(),
            UrlSetId = urlSetId,
            StepsJson = "[]",
            CreatedAt = DateTime.UtcNow,
        };

        try
        {
            await _database.InsertScenarioAsync(scenario);
            _nameBox.Clear();
            await LoadScenarios();
            _logger.Info($"Scenario '{scenario.Name}' created");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to create scenario", ex);
        }
    }

    private void AddStep_Click(object? sender, EventArgs e)
    {
        if (!Enum.TryParse<ScenarioActionType>(_actionCombo.SelectedItem?.ToString(), out var action))
            return;

        var step = new ScenarioStep
        {
            Order = _currentSteps.Count + 1,
            ActionType = action,
            Selector = string.IsNullOrWhiteSpace(_selectorBox.Text) ? null : _selectorBox.Text.Trim(),
            Value = string.IsNullOrWhiteSpace(_valueBox.Text) ? null : _valueBox.Text.Trim(),
        };

        _currentSteps.Add(step);
        _stepsList.Items.Add($"{step.Order}. {step.ActionType} → {step.Selector ?? step.Value ?? "—"}");
        _selectorBox.Clear();
        _valueBox.Clear();
    }

    private void RemoveStep_Click(object? sender, EventArgs e)
    {
        if (_stepsList.SelectedIndex < 0) return;
        var idx = _stepsList.SelectedIndex;
        _currentSteps.RemoveAt(idx);
        _stepsList.Items.RemoveAt(idx);

        // Re-number
        for (int i = 0; i < _currentSteps.Count; i++)
        {
            _currentSteps[i].Order = i + 1;
            _stepsList.Items[i] = $"{_currentSteps[i].Order}. {_currentSteps[i].ActionType} → {_currentSteps[i].Selector ?? _currentSteps[i].Value ?? "—"}";
        }
    }

    private async void SaveScenario_Click(object? sender, EventArgs e)
    {
        if (_selectedScenarioId is null)
        {
            _logger.Warning("Select a scenario first");
            return;
        }

        if (_scenarioGrid.Tag is not List<Domain.Entities.Scenario> scenarios)
            return;

        var scenario = scenarios.FirstOrDefault(s => s.Id == _selectedScenarioId);
        if (scenario is null) return;

        // Update name if changed
        if (!string.IsNullOrWhiteSpace(_nameBox.Text))
            scenario.Name = _nameBox.Text.Trim();

        // Serialize steps to JSON
        scenario.StepsJson = System.Text.Json.JsonSerializer.Serialize(_currentSteps);
        scenario.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _database.UpdateScenarioAsync(scenario);
            await LoadScenarios();
            _logger.Info($"Scenario '{scenario.Name}' saved ({_currentSteps.Count} steps)");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save scenario", ex);
        }
    }
}
