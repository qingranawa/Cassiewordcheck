using System.Windows;
using System.Windows.Controls;
using CassieWordCheck.Models;
using CassieWordCheck.Services;

namespace CassieWordCheck.Views;

public partial class DiffWindow : Window
{
    private readonly WordList _leftWordlist;
    private readonly LocalizationService _localization;
    private WordList? _rightWordlist;
    private WordListDiff? _diffResult;

    public DiffWindow(WordList leftWordlist, LocalizationService localization)
    {
        InitializeComponent();
        _leftWordlist = leftWordlist;
        _localization = localization;
        this.EnableDarkTitleBar();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        Title = _localization["diff.title"];
        TitleLabel.Text = _localization["diff.title"];
        LoadRightButton.Content = _localization["diff.load"];
        LeftSectionLabel.Text = _localization["diff.left_only"];
        RightSectionLabel.Text = _localization["diff.right_only"];
        ExportButton.Content = _localization["diff.export"];
        StatusLabel.Text = _localization["diff.prompt"];

        // 如果当前词库为空，显示提示
        if (_leftWordlist.WordCount == 0)
        {
            StatusLabel.Text = _localization["wordlist_browser.empty"];
            LoadRightButton.IsEnabled = false;
        }
    }

    private void OnLoadRightWordlist(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = _localization["diff.select_file"],
            Filter = "词库文件 (*.txt;*.csv;*.xlsx)|*.txt;*.csv;*.xlsx|文本文件 (*.txt)|*.txt|CSV 文件 (*.csv)|*.csv|Excel 文件 (*.xlsx)|*.xlsx",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            _rightWordlist = new WordList();
            var count = _rightWordlist.LoadFromFile(dialog.FileName);
            if (count == 0)
            {
                StatusLabel.Text = _localization["diff.empty_file"];
                return;
            }

            // 计算差异
            _diffResult = _leftWordlist.DiffWith(_rightWordlist);

            // 更新统计概览
            LeftCountLabel.Text = $"{_localization["diff.left"]}: {_diffResult.LeftOnlyCount}";
            RightOnlyCountLabel.Text = $"{_localization["diff.right_only_count"]}: {_diffResult.RightOnlyCount}";
            CommonCountLabel.Text = $"{_localization["diff.common"]}: {_diffResult.CommonCount}";

            // 更新列表
            LeftOnlyList.ItemsSource = _diffResult.LeftOnly.OrderBy(w => w).ToList();
            RightOnlyList.ItemsSource = _diffResult.RightOnly.OrderBy(w => w).ToList();

            // 启用导出
            ExportButton.IsEnabled = true;

            // 状态提示
            StatusLabel.Text = string.Format(
                _localization["diff.done"],
                Path.GetFileName(dialog.FileName),
                _diffResult.LeftOnlyCount,
                _diffResult.RightOnlyCount,
                _diffResult.CommonCount);
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{_localization["diff.error"]}: {ex.Message}";
        }
    }

    private void OnExportDiff(object sender, RoutedEventArgs e)
    {
        if (_diffResult is null) return;

        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = _localization["diff.export_title"],
            Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
            DefaultExt = ".txt",
            FileName = $"wordlist_diff_{DateTime.Now:yyyyMMdd_HHmmss}.txt",
        };

        if (saveDialog.ShowDialog() != true) return;

        try
        {
            var lines = new List<string>
            {
                $"=== 词库差异报告 ===",
                $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"",
                $"左词库: {_diffResult.LeftLabel} ({_leftWordlist.WordCount} 词)",
                $"右词库: {_diffResult.RightLabel} ({(_rightWordlist?.WordCount ?? 0)} 词)",
                $"",
                $"--- 仅左词库存在: {_diffResult.LeftOnlyCount} 词 ---",
            };

            foreach (var word in _diffResult.LeftOnly.OrderBy(w => w))
                lines.Add(word);

            lines.Add("");
            lines.Add($"--- 仅右词库存在: {_diffResult.RightOnlyCount} 词 ---");
            foreach (var word in _diffResult.RightOnly.OrderBy(w => w))
                lines.Add(word);

            lines.Add("");
            lines.Add($"--- 共有: {_diffResult.CommonCount} 词 ---");
            lines.Add($"(共 {_diffResult.CommonCount} 个单词在两词库中均存在)");

            File.WriteAllLines(saveDialog.FileName, lines);
            StatusLabel.Text = _localization["diff.exported"];
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"{_localization["diff.error"]}: {ex.Message}";
        }
    }
}
