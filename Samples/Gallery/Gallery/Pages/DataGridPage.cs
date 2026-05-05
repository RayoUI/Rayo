using Rayo;
using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using System;
using System.Collections.Generic;
using System.Text;
using static Rayo.Core.UIHelpers;

namespace Gallery.Pages;

public class DataGridPage : UserControl
{
    public override VisualElement Build()
    {
        var grid = new DataGrid()
            .AddColumn(new DataGridColumn("ID", "Id", 60))
            .AddColumn(new DataGridColumn("Name", "Name", 150))
            .AddColumn(new DataGridColumn("Email", "Email", 200))
            .AddColumn(new DataGridColumn("Status", "Status", 100));

        // Add sample data
        var items = new List<object>();
        
        // Generate diverse sample data to test scrolling
        string[] statuses = { "Active", "Inactive", "Pending", "Suspended" };
        string[] firstNames = { "John", "Jane", "Bob", "Alice", "Charlie", "David", "Eva", "Frank", "Grace", "Henry", "Ivy", "Jack" };
        string[] lastNames = { "Doe", "Smith", "Johnson", "Brown", "Davis", "Miller", "Wilson", "Moore", "Taylor", "Anderson" };

        for (int i = 1; i <= 50; i++)
        {
            var firstName = firstNames[i % firstNames.Length];
            var lastName = lastNames[i % lastNames.Length];
            var status = statuses[i % statuses.Length];
            
            items.Add(new 
            { 
                Id = i, 
                Name = $"{firstName} {lastName}", 
                Email = $"{firstName.ToLower()}.{lastName.ToLower()}{i}@example.com", 
                Status = status 
            });
        }

        grid.Items(items);

        return new VStack()
            .Spacing(20)
            .Padding(new Thickness(20))
            .Children(
                Helper.CreatePageHeader("DataGrid", "Tabular data with sorting and selection"),

                Helper.CreateExampleSection("Interactive Data Grid",
                    new VStack()
                        .Spacing(10)
                        .Children(
                            grid,
                            new Label("Click on column headers to sort. Click rows to select.")
                                .FontSize(12)
                                .Foreground(ColorDefault.Secondary)
                        )
                )
            );
    }
}
