using Rayo.Controls;
using Rayo.Core;
using Rayo.Layout;
using System;
using System.Collections.Generic;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Gallery.Pages;

public class DialogPage : UserControl
{
    public override VisualElement Build()
    {
        return new VStack()
            .Spacing(20)
            .Padding(new Rayo.Thickness(top: 20))
            //.Alignment(Alignment.Center)
            //.VerticalAlignment(VerticalAlignment.Stretch)
            //.HorizontalAlignment(HorizontalAlignment.Stretch)
            //.JustifyContent(JustifyContent.Center)
            .Children(
                new Button()
                    .Text("Show")
                    .Width(150)
                    .Height(50)
                    .OnTapped(() =>
                    {
                        Dialog.Show(
                            "About Rayo",
                            "Version 1.0\n\nBuilt with Rayo Framework\n.NET 10 + OpenGL"
                        );
                    })
            );
    }
}
