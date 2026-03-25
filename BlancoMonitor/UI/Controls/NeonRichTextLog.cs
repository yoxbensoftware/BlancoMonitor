using System.Collections.Concurrent;
using BlancoMonitor.UI.Theme;
using BlancoMonitor.UI.Utilities;

namespace BlancoMonitor.UI.Controls;

public sealed class NeonRichTextLog : RichTextBox
{
    private readonly int _maxLines;
    private readonly ConcurrentQueue<(string Message, Color Color)> _pendingMessages = new();
    private readonly System.Windows.Forms.Timer _flushTimer;

    public NeonRichTextLog(int maxLines = 5000)
    {
        _maxLines = maxLines;
        ReadOnly = true;
        BorderStyle = BorderStyle.None;
        BackColor = NeonTheme.Background;
        ForeColor = NeonTheme.TextPrimary;
        Font = NeonTheme.MonoFontSmall;
        WordWrap = false;
        ScrollBars = RichTextBoxScrollBars.Both;
        DetectUrls = true;
        Cursor = Cursors.IBeam;

        LinkClicked += (_, e) => BrowserHelper.OpenUrl(e.LinkText);

        _flushTimer = new System.Windows.Forms.Timer { Interval = 150 };
        _flushTimer.Tick += (_, _) => FlushPending();
        _flushTimer.Start();
    }

    public void AppendLog(string message, Color? color = null)
    {
        if (IsDisposed || !IsHandleCreated)
            return;

        _pendingMessages.Enqueue((message, color ?? NeonTheme.TextPrimary));
    }

    private void FlushPending()
    {
        if (_pendingMessages.IsEmpty || IsDisposed || !IsHandleCreated)
            return;

        if (InvokeRequired)
        {
            try { BeginInvoke(FlushPending); }
            catch (ObjectDisposedException) { }
            return;
        }

        // Drain all pending messages in one UI update
        const int maxPerFlush = 50;
        var count = 0;

        SuspendLayout();
        try
        {
            while (count < maxPerFlush && _pendingMessages.TryDequeue(out var entry))
            {
                if (Lines.Length > _maxLines)
                {
                    SelectionStart = 0;
                    SelectionLength = GetFirstCharIndexFromLine(Lines.Length / 2);
                    SelectedText = string.Empty;
                }

                SelectionStart = TextLength;
                SelectionLength = 0;
                SelectionColor = entry.Color;
                AppendText(entry.Message + Environment.NewLine);
                count++;
            }

            // Discard excess if queue is still very large (back-pressure)
            if (_pendingMessages.Count > 200)
            {
                while (_pendingMessages.Count > 50)
                    _pendingMessages.TryDequeue(out _);
            }

            ScrollToCaret();
        }
        finally
        {
            ResumeLayout();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _flushTimer.Stop();
            _flushTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}
