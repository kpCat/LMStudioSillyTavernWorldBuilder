namespace LMStudioSillyTavernWorldBuilder;

internal static class PlayListViewHelper
{
    public static void FillList(
        ListView listView,
        IEnumerable<(string Id, string Name, string Description)> rows,
        (string Id, string Name, string Description)? emptyRow = null)
    {
        EnsureColumns(listView);
        listView.Items.Clear();
        var materializedRows = rows.ToList();
        if (materializedRows.Count == 0)
        {
            var row = emptyRow ?? ("empty", "Пусто", "Нет данных");
            AddRow(listView, row);
            return;
        }

        foreach (var row in materializedRows)
        {
            AddRow(listView, row);
        }
    }

    public static void EnsureColumns(ListView listView)
    {
        if (listView.Columns.Count > 0)
        {
            return;
        }

        listView.Columns.Add("Id", 120);
        listView.Columns.Add("Название", 140);
        listView.Columns.Add("Описание/Значение", 180);
    }

    private static void AddRow(ListView listView, (string Id, string Name, string Description) row)
    {
        var item = new ListViewItem(row.Id);
        item.SubItems.Add(row.Name);
        item.SubItems.Add(row.Description);
        listView.Items.Add(item);
    }
}
