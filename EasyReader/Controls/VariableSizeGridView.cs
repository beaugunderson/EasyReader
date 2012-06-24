using System;

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using EasyReader.Data;

namespace EasyReader.Controls
{
    public class VariableSizeGridView : GridView
    {
        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            var dataItem = item as ReadingListDataItem;

            if (dataItem == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(dataItem.ImageUri) && !dataItem.ImageUri.Contains("LightGray"))
            {
                var rand = new Random();

                if (rand.Next(0, 100) >= 50)
                {
                    SetSize(element as UIElement, LayoutSizes.PrimaryItem);

                    //element.SetValue(WidthProperty, 500);
                    //element.SetValue(HeightProperty, 500);

                    //element.SetValue(TemplateProperty, "Standard500x500ItemTemplate");
                    //element.SetValue(ItemTemplateProperty, "Standard500x500ItemTemplate");
                }
            }    
        }

        public static void SetSize(UIElement element, Size size)
        {
            VariableSizedWrapGrid.SetColumnSpan(element, (int)size.Width);
            VariableSizedWrapGrid.SetRowSpan(element, (int)size.Height);
        }
    }

    public static class LayoutSizes
    {
        public static Size PrimaryItem = new Size(2, 2);
        public static Size RegularItem = new Size(1, 1);
    }
}