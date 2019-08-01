using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using ABCat.Shared.Plugins.DataSets;
using ABCat.Shared.Plugins.UI;
using Component.Infrastructure;
using Component.Infrastructure.Factory;
using JetBrains.Annotations;
using Xceed.Wpf.DataGrid;

namespace ABCat.Plugins.RecordsList.WpfToolKit
{
    [PerCallComponentInfo("1.0")]
    [UsedImplicitly]
    public partial class RecordsListWpfToolkitUc : IRecordsListPlugin
    {
        public RecordsListWpfToolkitUc()
        {
            InitializeComponent();
            Grid.MouseDoubleClick += GridMouseDoubleClick;
        }

        public Config Config { get; set; }
        public event EventHandler<ItemDoubleClickRowEventArgs> ItemDoubleClick;

        public FrameworkElement Control => this;

        public IEnumerable<IAudioBook> Data
        {
            get => (IEnumerable<IAudioBook>) Grid.ItemsSource;
            set
            {
                var currentRecords = SelectedItems.Select(item => item.Key).ToHashSet();
                if (Grid.ItemsSource != null) StoreLayout();
                Grid.ItemsSource = value;
                RestoreLayout();
                if (currentRecords.Any())
                    Grid.SelectedItem = value.FirstOrDefault(item => item.Key == currentRecords.First());
            }
        }

        public IReadOnlyCollection<IAudioBook> SelectedItems => Grid.SelectedItems.Cast<IAudioBook>().ToArray();

        public void Dispose()
        {
            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void FixComponentConfig()
        {
        }

        public void RestoreLayout()
        {
            if (Config.TryLoadLayout("RecordsListWpfToolkutUc_grid_Layout", out var layout))
            {
                SetGridColumnsLayoutFromXml(Grid, Context.I.DefaultEncoding.GetString(layout));
            }
        }

        public void StoreLayout()
        {
            var layout = GetXmlFromGridColumnsLayout(Grid);
            Config.SaveLayout("RecordsListWpfToolkutUc_grid_Layout", Context.I.DefaultEncoding.GetBytes(layout));
        }

        public event EventHandler Disposed;

        private string GetXmlFromGridColumnsLayout(DataGridControl grid)
        {
            var builder = new StringBuilder();

            builder.Append("<grid>\r");

            foreach (Column column in grid.Columns)
            {
                if (column.Visible)
                {
                    string sort;

                    if (column.SortDirection == SortDirection.Ascending) sort = "Ascending";
                    else if (column.SortDirection == SortDirection.Descending) sort = "Descending";
                    else sort = "None";

                    builder.AppendFormat(
                        "<column name='{0}' width='{1}' visibleIndex='{2}' sort='{3}' sortIndex='{4}'/>",
                        column.FieldName, Math.Ceiling(column.Width), column.VisiblePosition, sort, column.SortIndex);

                    builder.Append("\r");
                }
            }

            foreach (var dataGridGroupDescription in grid.Items.GroupDescriptions.Cast<DataGridGroupDescription>())
            {
                builder.AppendFormat("<dataGridGroupDescription propertyName='{0}'/>",
                    dataGridGroupDescription.PropertyName);
                builder.Append("\r");
            }

            builder.Append("</grid>");

            return builder.ToString();
        }

        private void GridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ItemDoubleClick?.Invoke(this, new ItemDoubleClickRowEventArgs((IAudioBook) Grid.CurrentItem));
        }

        private void SetGridColumnsLayoutFromXml(DataGridControl grid, string xmlFragment)
        {
            grid.BeginInit();
            var sortedColumns = new SortedList();
            var sortDirections = new SortedList();

            var reader = new XmlTextReader(xmlFragment, XmlNodeType.Element, null);

            if (grid.Items.GroupDescriptions != null)
            {
                grid.Items.GroupDescriptions.Clear();

                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "column")
                    {
                        var str = reader.GetAttribute("name");

                        var column = grid.Columns[str];

                        //column.Title = str;

                        if (column != null && column.Visible)
                        {
                            column.Width = int.Parse(reader.GetAttribute("width"));
                            column.VisiblePosition = int.Parse(reader.GetAttribute("visibleIndex"));

                            if (reader.GetAttribute("sort") == "Ascending" ||
                                reader.GetAttribute("sort") == "Descending")
                            {
                                var sortIndex = int.Parse(reader.GetAttribute("sortIndex"));
                                sortedColumns.Add(sortIndex, column);
                                sortDirections.Add(sortIndex, reader.GetAttribute("sort"));
                            }
                        }
                    }

                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "dataGridGroupDescription")
                    {
                        var str = reader.GetAttribute("propertyName");
                        var gd = new DataGridGroupDescription(str);
                        grid.Items.GroupDescriptions.Add(gd);
                    }
                }
            }

            reader.Close();

            grid.Items.SortDescriptions.Clear();

            for (var i = 0; i < sortedColumns.Count; i++)
            {
                var ascending = sortDirections.GetByIndex(i).ToString() == "Ascending";
                var column = (Column) sortedColumns.GetByIndex(i);
                var str = column.FieldName;
                if (ascending) grid.Items.SortDescriptions.Add(new SortDescription(str, ListSortDirection.Ascending));
                else grid.Items.SortDescriptions.Add(new SortDescription(str, ListSortDirection.Descending));
            }

            grid.EndInit();
        }
    }
}