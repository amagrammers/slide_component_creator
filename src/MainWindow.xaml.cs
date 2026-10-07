using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace SlideComponentCreator;

public partial class MainWindow : Window
{
    private static readonly string[] Presets =
        { "#00D5FF", "#2F6FDE", "#E5484D", "#30A46C", "#F5A524", "#8E4EC6", "#FFFFFF", "#111827" };

    private bool _loading = true;
    private TextBox _activeColorBox;

    public MainWindow()
    {
        InitializeComponent();
        _activeColorBox = StrokeBox;

        foreach (var hex in Presets)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var btn = new Button
            {
                Width = 22, Height = 22, Margin = new Thickness(0, 0, 4, 0),
                Background = new SolidColorBrush(color), ToolTip = hex,
            };
            btn.Click += (_, _) => _activeColorBox.Text = hex;
            PresetPanel.Children.Add(btn);
        }

        CategoryList.ItemsSource = ShapeCatalog.All;
        _loading = false;
        CategoryList.SelectedIndex = 0;
    }

    private ShapeDef? CurrentShape => ShapeCombo.SelectedItem as ShapeDef;

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList.SelectedItem is not Category c) return;
        SubCategoryList.ItemsSource = c.SubCategories;
        SubCategoryList.SelectedIndex = 0;
    }

    private void SubCategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SubCategoryList.SelectedItem is not SubCategory s) return;
        ShapeCombo.ItemsSource = s.Shapes;
        ShapeCombo.SelectedIndex = 0;
    }

    private void ShapeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentShape is not { } shape) return;
        _loading = true;
        WidthBox.Text = shape.DefaultWidth.ToString(CultureInfo.InvariantCulture);
        HeightBox.Text = shape.DefaultHeight.ToString(CultureInfo.InvariantCulture);
        AdjustSlider.Value = shape.DefaultAdjust;
        AdjustSlider.IsEnabled = shape.HasAdjust;
        EnclosureCombo.IsEnabled = shape.Enclosable;
        SidesPanel.Visibility = shape.BuildSided != null ? Visibility.Visible : Visibility.Collapsed;
        AdjustLabel.Text = shape.AdjustLabel + " ";
        _loading = false;
        UpdatePreview();
    }

    private void Setting_Changed(object sender, TextChangedEventArgs e) => UpdatePreview();

    private void Setting_Slider(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();

    private void Setting_Selection(object sender, SelectionChangedEventArgs e) => UpdatePreview();

    private void ColorBox_Focus(object sender, KeyboardFocusChangedEventArgs e) => _activeColorBox = (TextBox)sender;

    private void PreviewBg_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (PreviewBackground == null) return;
        PreviewBackground.Background = PreviewBgCombo.SelectedIndex switch
        {
            0 => new SolidColorBrush(Color.FromRgb(0x0B, 0x12, 0x20)),
            1 => Brushes.Black,
            2 => Brushes.White,
            _ => (Brush)FindResource("Checker"),
        };
    }

    private static bool TryColor(string text, out Color color)
    {
        try
        {
            color = (Color)ColorConverter.ConvertFromString(text.Trim());
            return true;
        }
        catch
        {
            color = default;
            return false;
        }
    }

    private static bool TrySize(string text, out double v) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out v) && v >= 8 && v <= 4000;

    private static void SetSwatch(Border swatch, string text)
    {
        if (TryColor(text, out var c)) swatch.Background = new SolidColorBrush(c);
    }

    private Brush? BuildFill()
    {
        TryColor(Fill1Box.Text, out var c1);
        TryColor(Fill2Box.Text, out var c2);
        switch (FillModeCombo.SelectedIndex)
        {
            case 1:
                return new SolidColorBrush(c1);
            case 2:
            {
                // 角度0°=左→右、90°=上→下。バウンディングボックスの端から端までを勾配の長さにする
                double rad = AngleSlider.Value * Math.PI / 180;
                double cos = Math.Cos(rad), sin = Math.Sin(rad);
                double half = (Math.Abs(cos) + Math.Abs(sin)) / 2;
                double range = RangeSlider.Value / 100;

                Color Scaled(Color c, double opacity) =>
                    Color.FromArgb((byte)Math.Round(c.A * opacity / 100), c.R, c.G, c.B);

                var brush = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5 - cos * half, 0.5 - sin * half),
                    EndPoint = new Point(0.5 + cos * half, 0.5 + sin * half),
                };
                brush.GradientStops.Add(new GradientStop(Scaled(c1, StartOpacitySlider.Value), 0));
                brush.GradientStops.Add(new GradientStop(Scaled(c2, EndOpacitySlider.Value), range));
                if (range < 1)
                {
                    // 同じ位置に透明の停止点を置いて、範囲の外側を完全に透過させる
                    brush.GradientStops.Add(new GradientStop(Scaled(c2, 0), range));
                    brush.GradientStops.Add(new GradientStop(Scaled(c2, 0), 1));
                }
                return brush;
            }
            default:
                return null;
        }
    }

    private void UpdatePreview()
    {
        if (_loading || CurrentShape is not { } shape) return;
        if (!TrySize(WidthBox.Text, out var w) || !TrySize(HeightBox.Text, out var h)) return;

        SetSwatch(Fill1Swatch, Fill1Box.Text);
        SetSwatch(Fill2Swatch, Fill2Box.Text);
        SetSwatch(StrokeSwatch, StrokeBox.Text);

        ShapePath.Fill = BuildFill();

        double strokeW = StrokeWidthSlider.Value;
        if (TryColor(StrokeBox.Text, out var sc) && strokeW > 0)
        {
            ShapePath.Stroke = new SolidColorBrush(sc);
            ShapePath.StrokeThickness = strokeW;
        }
        else
        {
            ShapePath.Stroke = null;
            ShapePath.StrokeThickness = 0;
        }

        double glowR = GlowRadiusSlider.Value;
        bool glowOn = glowR > 0 && GlowStrengthSlider.Value > 0 && TryColor(StrokeBox.Text, out _);
        if (glowOn)
        {
            TryColor(StrokeBox.Text, out var gc);
            Glow.Color = gc;
            Glow.BlurRadius = glowR;
            Glow.Opacity = GlowStrengthSlider.Value;
        }
        else
        {
            Glow.BlurRadius = 0;
            Glow.Opacity = 0;
        }

        // 線の太さと発光がはみ出す分だけ出力範囲を広げる
        double pad = Math.Ceiling(strokeW / 2 + (glowOn ? glowR : 0)) + 4;
        Inner.Width = w;
        Inner.Height = h;
        Canvas1.Width = w + pad * 2;
        Canvas1.Height = h + pad * 2;

        var sides = new[] { SideTop.SelectedIndex, SideRight.SelectedIndex, SideBottom.SelectedIndex, SideLeft.SelectedIndex };
        ShapePath.Data = ShapeCatalog.Enclose(shape, w, h, AdjustSlider.Value,
            shape.Enclosable ? EnclosureCombo.SelectedIndex : 0, sides);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentShape is not { } shape) return;
        var dlg = new SaveFileDialog
        {
            Filter = "PNG 画像 (*.png)|*.png",
            FileName = shape.Name + ".png",
        };
        if (dlg.ShowDialog(this) != true) return;

        int scale = ScaleCombo.SelectedIndex + 1;
        double w = Canvas1.Width, h = Canvas1.Height;

        var dv = new DrawingVisual();
        using (var dc = dv.RenderOpen())
        {
            if (TransparentCheck.IsChecked != true)
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, w * scale, h * scale));
            dc.PushTransform(new ScaleTransform(scale, scale));
            dc.DrawRectangle(new VisualBrush(Canvas1) { Stretch = Stretch.None }, null, new Rect(0, 0, w, h));
            dc.Pop();
        }

        var bmp = new RenderTargetBitmap((int)Math.Ceiling(w * scale), (int)Math.Ceiling(h * scale), 96, 96, PixelFormats.Pbgra32);
        bmp.Render(dv);

        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(bmp));
        using var fs = File.Create(dlg.FileName);
        enc.Save(fs);
        MessageBox.Show(this, "保存しました:\n" + dlg.FileName, "保存完了");
    }
}
