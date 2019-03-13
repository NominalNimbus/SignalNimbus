using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using DebugService.Classes;
using Xceed.Wpf.Toolkit;

namespace DebugService.Converters
{
    public class CodeParameterToEditorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            var valueBinding = new Binding("Value") { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
            FrameworkElement editor;

            if (value is IntParam)
            {
                editor = new IntegerUpDown
                {
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Snow,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    Minimum = ((IntParam)value).MinValue,
                    Maximum = ((IntParam)value).MaxValue,
                };
                editor.SetBinding(IntegerUpDown.ValueProperty, valueBinding);
                return editor;
            }
            if (value is DoubleParam)
            {
                editor = new DoubleUpDown
                {
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Snow,
                    Minimum = ((DoubleParam)value).MinValue,
                    Maximum = ((DoubleParam)value).MaxValue,
                };
                editor.SetBinding(DoubleUpDown.ValueProperty, valueBinding);
                return editor;
            }
            if (value is StringParam && ((StringParam)value).AllowedValues.Count == 0)
            {
                editor = new TextBox
                {
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Snow,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                };
                editor.SetBinding(TextBox.TextProperty, valueBinding);
                return editor;
            }

            if (value is StringParam && ((StringParam)value).AllowedValues.Count > 0)
            {
                editor = new ComboBox
                {
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    ItemsSource = ((StringParam)value).AllowedValues,
                    HorizontalContentAlignment = HorizontalAlignment.Right
                };
                editor.SetBinding(Selector.SelectedValueProperty, valueBinding);
                return editor;
            }

            if (value is ColorParam)
            {
                editor = new ColorPicker
                {
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Snow,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0)
                };
                var binding = new Binding("Value")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };
                editor.SetBinding(ColorPicker.SelectedColorProperty, binding);
                return editor;
            }

            if (value is SeriesParam)
            {
                editor = new Grid
                {
                    Background = Brushes.Transparent
                };

                ((Grid)editor).RowDefinitions.Add(new RowDefinition());
                ((Grid)editor).RowDefinitions.Add(new RowDefinition());
                ((Grid)editor).ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                ((Grid)editor).ColumnDefinitions.Add(new ColumnDefinition());

                // Color

                var colorEditor = new ColorPicker
                {
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(0, 0, 0, 3)

                };

                var colorEditorBinding = new Binding("Color")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                colorEditor.SetBinding(ColorPicker.SelectedColorProperty, colorEditorBinding);

                // Thickness

                var thicknessEditor = new IntegerUpDown
                {
                    Background = Brushes.Transparent,
                    Foreground = Brushes.Snow,
                    BorderThickness = new Thickness(0),
                    BorderBrush = Brushes.Transparent,
                    Minimum = 1,
                    Maximum = 10
                };

                var thicknessEditorBinding = new Binding("Thickness")
                {
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                };

                thicknessEditor.SetBinding(IntegerUpDown.ValueProperty, thicknessEditorBinding);

                Grid.SetRow(colorEditor, 0);
                Grid.SetRow(thicknessEditor, 1);
                Grid.SetColumn(colorEditor, 1);
                Grid.SetColumn(thicknessEditor, 1);

                var text1 = new TextBlock { Text = "Color: ", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 3, 0) };
                var text2 = new TextBlock { Text = "Thickness: ", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 3, 0) };

                Grid.SetRow(text1, 0);
                Grid.SetRow(text2, 1);
                Grid.SetColumn(text1, 0);
                Grid.SetColumn(text2, 0);

                ((Grid)editor).Children.Add(colorEditor);
                ((Grid)editor).Children.Add(thicknessEditor);
                ((Grid)editor).Children.Add(text1);
                ((Grid)editor).Children.Add(text2);

                return editor;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
