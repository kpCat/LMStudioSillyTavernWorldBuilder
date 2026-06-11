using System.Windows.Forms;

namespace LMStudioSillyTavernWorldBuilder.Tests;

public sealed class PlayListViewHelperTests
{
    [Fact]
    public void FillList_AddsColumnsAndPlaceholderForEmptyRows()
    {
        using var listView = new ListView { View = View.Details };

        PlayListViewHelper.FillList(listView, Array.Empty<(string Id, string Name, string Description)>(), ("empty", "Пусто", "Нет данных"));

        Assert.Equal(3, listView.Columns.Count);
        var item = Assert.Single(listView.Items.Cast<ListViewItem>());
        Assert.Equal("empty", item.Text);
        Assert.Equal("Пусто", item.SubItems[1].Text);
        Assert.Equal("Нет данных", item.SubItems[2].Text);
    }

    [Fact]
    public void FillList_KeepsExistingColumnsAndAddsRows()
    {
        using var listView = new ListView { View = View.Details };
        listView.Columns.Add("Custom", 80);

        PlayListViewHelper.FillList(listView, new[] { ("scene_start", "Начало", "Текст") });

        Assert.Single(listView.Columns.Cast<ColumnHeader>());
        var item = Assert.Single(listView.Items.Cast<ListViewItem>());
        Assert.Equal("scene_start", item.Text);
        Assert.Equal("Начало", item.SubItems[1].Text);
        Assert.Equal("Текст", item.SubItems[2].Text);
    }
}
