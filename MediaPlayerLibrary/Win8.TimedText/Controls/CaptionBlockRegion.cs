using System;
using System.Collections.Generic;
using System.Linq;
#if SILVERLIGHT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
#else
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#endif

namespace Microsoft.TimedText
{
    public sealed class CaptionBlockRegion : Control
    {
        private TimeSpan _mediaPosition;
        private IMarkerManager<TimedTextElement> _captionManager;
        private IDictionary<CaptionElement, UIElement> _activeElements;

        private Grid LayoutRoot;
        private Border CaptionsBorder;
        private Grid CaptionsRoot;
        private StackPanel CaptionsPanel;

        private bool isTemplateApplied;

#if SILVERLIGHT
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            LayoutRoot = base.GetTemplateChild("LayoutRoot") as Grid;
            CaptionsBorder = base.GetTemplateChild("CaptionsBorder") as Border;
            CaptionsRoot = base.GetTemplateChild("CaptionsRoot") as Grid;
            CaptionsPanel = base.GetTemplateChild("captionsPanel") as StackPanel;

            isTemplateApplied = true;

            UpdateSize();
        }

        #region CaptionRegion
        public static readonly DependencyProperty CaptionRegionProperty = DependencyProperty.Register("CaptionRegion", typeof(CaptionRegion), typeof(CaptionBlockRegion), new PropertyMetadata(null, OnCaptionRegionPropertyChanged));

        public CaptionRegion CaptionRegion
        {
            get { return (CaptionRegion)GetValue(CaptionRegionProperty); }
            set { SetValue(CaptionRegionProperty, value); }
        }

        private static void OnCaptionRegionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var captionBlockRegion = d as CaptionBlockRegion;
            captionBlockRegion.IfNotNull(i => i.OnCaptionRegionChanged());
        }

        private void OnCaptionRegionChanged()
        {
            ApplyRegionStyles();
        }
        #endregion

        const int MaxOverflow = 1080;

        public CaptionBlockRegion()
        {
            this.DefaultStyleKey = typeof(CaptionBlockRegion);

            _activeElements = new Dictionary<CaptionElement, UIElement>();

            this.Loaded += (s, e) => RedrawActiveCaptions();
            this.SizeChanged += (s, e) => UpdateSize();
        }

        public IMarkerManager<TimedTextElement> CaptionManager
        {
            get { return _captionManager; }
            set
            {
                if (_captionManager != null)
                {
                    _captionManager.MarkerLeft -= _captionManager_MarkerLeft;
                    _captionManager.MarkerReached -= _captionManager_MarkerReached;
                }
                _captionManager = value;
                if (_captionManager != null)
                {
                    _captionManager.MarkerLeft += _captionManager_MarkerLeft;
                    _captionManager.MarkerReached += _captionManager_MarkerReached;
                }
            }
        }

        void _captionManager_MarkerLeft(IMarkerManager<TimedTextElement> sender, TimedTextElement marker)
        {
            _mediaPosition = marker.End;
            HideCaption(marker);
        }

        void _captionManager_MarkerReached(IMarkerManager<TimedTextElement> sender, TimedTextElement marker)
        {
            _mediaPosition = marker.Begin;
            ShowCaption(marker);
        }

        public void UpdateAnimations(TimeSpan mediaPosition)
        {
            _mediaPosition = mediaPosition;

#if HACK_XAMLTYPEINFO
            var children = CaptionRegion.Children as MediaMarkerCollection<TimedTextElement>;
#else
            var children = CaptionRegion.Children;
#endif
            children.WhereActiveAtPosition(mediaPosition)
                                  .Where(i => i.HasAnimations)
                                  .ForEach(HideCaption)
                                  .ForEach(ShowCaption);
        }

        private void ShowCaption(TimedTextElement timedTextElement)
        {
            var caption = timedTextElement as CaptionElement;
            if (caption != null)
            {
                caption.CalculateCurrentStyle(_mediaPosition);
                var uiElement = RenderElement(caption);
                if (uiElement != null)
                {
                    if (_activeElements.ContainsKey(caption))
                    {
                        HideCaption(timedTextElement);
                    }
                    _activeElements.Add(caption, uiElement);
                    CaptionsPanel.Children.Clear();
                    _activeElements.OrderBy(i => i.Key.Index)
                                   .ForEach(i => CaptionsPanel.Children.Add(i.Value));
                }
            }
        }

        private void HideCaption(TimedTextElement timedTextElement)
        {
            var caption = timedTextElement as CaptionElement;

            if (caption != null && _activeElements.ContainsKey(caption))
            {
                CaptionsPanel.Children.Remove(_activeElements[caption]);
                _activeElements.Remove(caption);
            }
        }

        private void RedrawActiveCaptions()
        {
            var activeCaptions = _activeElements.Keys.ToList();
            activeCaptions.ForEach(HideCaption);
            activeCaptions.ForEach(ShowCaption);
        }


        private void UpdateSize()
        {
            if (isTemplateApplied)
            {
                var width = this.GetEffectiveWidth();
                var height = this.GetEffectiveHeight();

                if (width != 0 && height != 0 && CaptionRegion != null)
                {
                    Origin origin = CaptionRegion.CurrentStyle.Origin;
                    Extent extent = CaptionRegion.CurrentStyle.Extent;

                    double regionHeight = extent.Height.ToPixelLength(height);
                    double regionWidth = extent.Width.ToPixelLength(width);

                    CaptionsBorder.Width = regionWidth < 0 ? width : regionWidth;
                    CaptionsBorder.Height = regionHeight < 0 ? height : regionHeight;
                    CaptionsBorder.VerticalAlignment = regionHeight < 0
                        ? VerticalAlignment.Bottom
                        : VerticalAlignment.Top;

                    CaptionsBorder.Margin = new Thickness
                    {
                        Left = origin.Left.ToPixelLength(width),
                        Top = origin.Top.ToPixelLength(height)
                    };

                    ApplyRegionStyles();
                    RedrawActiveCaptions();
                }
            }
        }

        const int BRUSH_CACHE_CAPACITY = 50;

        // captions will usually use a handful of colors, we'll cache them to reduce resource usage.
        static Dictionary<Color, CachedBrush> cachedBrushes = new Dictionary<Color, CachedBrush>();

        class CachedBrush
        {
            public CachedBrush(Brush Brush)
            {
                this.Brush = Brush;
                LastUse = DateTime.Now;
            }

            public Brush Brush { get; private set; }
            public DateTime LastUse { get; set; }
        }

        static Brush GetCachedBrush(Color src)
        {
            if (cachedBrushes.ContainsKey(src))
            {
                var result = cachedBrushes[src];
                result.LastUse = DateTime.Now;
                return result.Brush;
            }
            else
            {
                Brush brush = new SolidColorBrush(src);
                if (cachedBrushes.Count >= BRUSH_CACHE_CAPACITY)
                {
                    var oldestUsedBrush = cachedBrushes.OrderBy(b => b.Value.LastUse).First();
                    cachedBrushes.Remove(oldestUsedBrush.Key);
                }
                cachedBrushes.Add(src, new CachedBrush(brush));
                return brush;
            }
        }

        private void ApplyRegionStyles()
        {
            if (isTemplateApplied)
            {
                var effectiveSize = this.GetEffectiveSize();
                var fontSize = CaptionRegion.CurrentStyle.FontSize.ToPixelLength(effectiveSize.Height);

                Canvas.SetZIndex(this, CaptionRegion.CurrentStyle.ZIndex);
                FontSize = fontSize > 0 ? fontSize : FontSize;
#if SILVERLIGHT
                FontFamily = CaptionRegion.CurrentStyle.FontFamily;
#else
                FontFamily = CaptionRegion.CurrentStyle.FontFamily.WindowsFontFamily;
#endif
                Foreground = GetCachedBrush(CaptionRegion.CurrentStyle.Color);
                CaptionsBorder.Background = GetCachedBrush(CaptionRegion.CurrentStyle.BackgroundColor);
                CaptionsBorder.Padding = CaptionRegion.CurrentStyle.Padding.ToThickness(effectiveSize);
                LayoutRoot.Visibility = CaptionRegion.CurrentStyle.Display == Visibility.Collapsed
                                        ? Visibility.Collapsed
                                        : CaptionRegion.CurrentStyle.Visibility;

                switch (CaptionRegion.CurrentStyle.DisplayAlign)
                {
                    case DisplayAlign.Center:
                        CaptionsPanel.VerticalAlignment = VerticalAlignment.Center;
                        break;
                    case DisplayAlign.Before:
                        CaptionsPanel.VerticalAlignment = VerticalAlignment.Top;
                        break;
                    case DisplayAlign.After:
                        CaptionsPanel.VerticalAlignment = VerticalAlignment.Bottom;
                        break;
                }

                if (CaptionRegion.CurrentStyle.Overflow == Overflow.Visible)
                {
                    // we're going to do this with margins since no default Silverlight panels support overflow and alignment. Could also be done with a custom panel.
                    switch (CaptionRegion.CurrentStyle.DisplayAlign)
                    {
                        case DisplayAlign.Center:
                            CaptionsPanel.Margin = new Thickness(0, -MaxOverflow, 0, -MaxOverflow);
                            break;
                        case DisplayAlign.Before:
                            CaptionsPanel.Margin = new Thickness(0, 0, 0, -MaxOverflow);
                            break;
                        case DisplayAlign.After:
                            CaptionsPanel.Margin = new Thickness(0, -MaxOverflow, 0, 0);
                            break;
                    }
                    switch (CaptionRegion.CurrentStyle.TextAlign)
                    {
                        case TextAlignment.Center:
                            CaptionsPanel.Margin = new Thickness(-MaxOverflow, CaptionsPanel.Margin.Top, -MaxOverflow, CaptionsPanel.Margin.Bottom);
                            break;
                        case TextAlignment.Right:
                            CaptionsPanel.Margin = new Thickness(-MaxOverflow, CaptionsPanel.Margin.Top, 0, CaptionsPanel.Margin.Bottom);
                            break;
                        default:
                            CaptionsPanel.Margin = new Thickness(0, CaptionsPanel.Margin.Top, -MaxOverflow, CaptionsPanel.Margin.Bottom);
                            break;
                    }
                }
                else
                {
                    CaptionsPanel.Margin = new Thickness();
                }
            }
        }

        private UIElement RenderElement(CaptionElement element)
        {
            StackPanel parent = null;

            try
            {
                parent = new StackPanel
                {
                    Background = GetCachedBrush(element.CurrentStyle.BackgroundColor),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // set position and size (origin and extent)
                var width = this.GetEffectiveWidth();
                var height = this.GetEffectiveHeight();
                var origin = element.CurrentStyle.Origin;
                //var extent = CaptionRegion.CurrentStyle.Extent;
                //var extentWidth = extent.Width.ToPixelLength(width);
                //var extentHeight = extent.Height.ToPixelLength(height);
                parent.Margin = new Thickness
                {
                    Left = origin.Left.ToPixelLength(width),
                    Top = origin.Top.ToPixelLength(height)
                };
                //if (extentWidth > 0) parent.Width = extentWidth;
                //if (extentHeight > 0) parent.Height = extentHeight;

                var textAlignment = element.CurrentStyle.TextAlign;

                double offset = 0;
                StackPanel p = NewPanel(parent, ref offset, element, textAlignment);

                RenderElementRecurse(parent, ref p, element, ref offset, textAlignment);

#if !WINDOWS_PHONE && !SILVERLIGHT3 && NOTUSED
                foreach (StackPanel stack in parent.Children)
                {
                    double baseline = 0;
                    foreach (Border b in stack.Children)
                    {
                        TextBlock tb = b.Child as TextBlock;
                        if (tb != null && tb.BaselineOffset > baseline)
                        {
                            baseline = tb.ActualHeight - tb.BaselineOffset;
                        }
                    }
                    foreach (Border b in stack.Children)
                    {
                        TextBlock tb = b.Child as TextBlock;
                        if (tb != null)
                        {
                            tb.Margin = new Thickness(0, 0, 0, baseline - (tb.ActualHeight - tb.BaselineOffset));
                        }
                    }
                }
#endif
            }
            catch (Exception)
            {
                //TODO: Respond to errors
            }

            return parent;
        }

        private void RenderElementRecurse(StackPanel parent, ref StackPanel p, CaptionElement element, ref double offset, TextAlignment align)
        {
            if (element.IsActiveAtPosition(_mediaPosition) && element.CurrentStyle.Display == Visibility.Visible)
            {
                if (element.CaptionElementType == TimedTextElementType.Text)
                {
                    var text = element.Content != null ? element.Content.ToString() : string.Empty;
                    offset = WrapElement(parent, ref p, offset, text, element, align);
                }
                else if (element.CaptionElementType == TimedTextElementType.Container)
                {
#if HACK_XAMLTYPEINFO
                    var children = element.Children as MediaMarkerCollection<TimedTextElement>;
#else
                    var children = element.Children;
#endif

                    foreach (CaptionElement child in children)
                    {
                        RenderElementRecurse(parent, ref p, child, ref offset, align);
                    }
                }
                else if (element.CaptionElementType == TimedTextElementType.LineBreak)
                {
                    p = NewPanel(parent, ref offset, element, align);
                }
            }
        }

        private double WrapElement(StackPanel parent, ref StackPanel p, double offset, string text, CaptionElement element, TextAlignment align, bool directionApplied = false)
        {
            if (text == null || text == "") return offset;

            var effectiveSize = this.GetEffectiveSize();
            var style = element.CurrentStyle;
            var panelSize = style.Extent.ToPixelSize(effectiveSize);
            double panelWidth = panelSize.Width;
            double panelHeight = panelSize.Height;

            if (style.Direction == FlowDirection.RightToLeft && !directionApplied)
            {
                text = new string(text.ToCharArray().Reverse().ToArray());
            }

            double height = style.FontSize.Unit == LengthUnit.PixelProportional || style.FontSize.Unit == LengthUnit.Cell ? effectiveSize.Height : panelHeight;
            TextBlock textblock = GetStyledTextblock(style, panelWidth, height, false);
            SetContent(textblock, text);

            Border border = new Border();
            border.Background = GetCachedBrush(style.BackgroundColor);
            FrameworkElement contentElement;

            double outlineWidth = style.OutlineWidth.ToPixelLength(effectiveSize.Height);
            if (outlineWidth > 0)
            {
                Grid cnv = new Grid();

                // do outline image up and to left
                TextBlock tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = -outlineWidth, Y = -outlineWidth };
                // do outline image left
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = -outlineWidth, Y = 0 };

                // do outline image down and to right
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = outlineWidth, Y = outlineWidth };
                // do outline image to right
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = outlineWidth, Y = 0 };

                // do outline image up and to right
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = outlineWidth, Y = -outlineWidth };
                // do outline image up
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = 0, Y = -outlineWidth };

                // do outline image down and to left
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = -outlineWidth, Y = outlineWidth };
                // do outline image down
                tb2 = GetStyledTextblock(style, panelWidth, height, true);
                SetContent(tb2, text);
                cnv.Children.Add(tb2);
                tb2.RenderTransform = new TranslateTransform() { X = 0, Y = outlineWidth };

                // add the main text
                cnv.Children.Add(textblock);

                // add the border
                contentElement = cnv;
            }
            else
            {
                contentElement = textblock;
            }

            border.Child = contentElement;
            p.Children.Add(border);

            string head = text;
            string tail = string.Empty;
            double elementWidth = textblock.GetEffectiveWidth();
            if (offset + elementWidth > panelSize.Width && style.WrapOption == TextWrapping.Wrap)
            {
                if (text.Length > 0 && text.IndexOf(' ') < 0)
                {
                    if (offset != 0 && elementWidth < panelSize.Width)
                    {
                        p.Children.Remove(border);
                        p = NewPanel(parent, ref offset, element, align);
                        return WrapElement(parent, ref p, 0, text, element, align, true);
                    }
                    int idx = text.Length - 1;
                    head = text.Substring(0, idx);
                    tail = text.Substring(idx);
                    SetAllContent(contentElement, head);
                    while (offset + textblock.GetEffectiveWidth() > panelSize.Width)
                    {
                        idx--;
                        head = text.Substring(0, idx);
                        tail = text.Substring(idx);
                        SetAllContent(contentElement, head);
                        p.UpdateLayout();
                    }
                    p = NewPanel(parent, ref offset, element, align);
                    return WrapElement(parent, ref p, offset, tail, element, align, true);
                }
                while (offset + textblock.GetEffectiveWidth() > panelSize.Width)
                {
                    int idx = head.LastIndexOf(' ');
                    if (idx < 0)
                    {
                        SetAllContent(contentElement, text);
                        return 0;
                    }
                    else
                    {
                        tail = text.Substring(idx + 1);
                        head = text.Substring(0, idx);
                    }
                    SetAllContent(contentElement, head);
                }
                p = NewPanel(parent, ref offset, element, align);
                return WrapElement(parent, ref p, offset, tail, element, align, true);
            }
            else
            {
                offset += elementWidth;
                return offset;
            }
        }

        private TextBlock GetStyledTextblock(TimedTextStyle style, double width, double height, bool fOutline)
        {
            TextBlock textblock = new TextBlock();
            //textblock.Width = width;
            textblock.FontStyle = style.FontStyle;
            textblock.FontWeight = FontWeightConverter.Convert(style.FontWeight);
            textblock.VerticalAlignment = VerticalAlignment.Bottom;
#if SILVERLIGHT
            textblock.FontFamily = style.FontFamily;
#else
            textblock.FontFamily = style.FontFamily.WindowsFontFamily;
#endif
            if (!double.IsNaN(height) && height != 0)
            {
                textblock.FontSize = Math.Round(style.FontSize.ToPixelLength(height));
            }
            textblock.Foreground = GetCachedBrush(fOutline ? style.OutlineColor : style.Color);

            textblock.Opacity = style.Visibility == Visibility.Visible
                                    ? style.Opacity
                                    : 0;
            //textblock.TextWrapping = style.WrapOption;
            textblock.TextAlignment = style.TextAlign;
            return textblock;
        }

        private StackPanel NewPanel(StackPanel parent, ref double offset, CaptionElement element, TextAlignment align)
        {
            StackPanel p = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            switch (align)
            {
                case TextAlignment.Center:
                    p.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
                case TextAlignment.Right:
                    p.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                case TextAlignment.Left:
                    p.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case TextAlignment.Justify:
                    p.HorizontalAlignment = HorizontalAlignment.Stretch;
                    break;
            }
            parent.Children.Add(p);
            offset = 0;
            return p;
        }

        private void SetAllContent(FrameworkElement contentElement, string text)
        {
            if (contentElement is TextBlock)
            {
                SetContent((TextBlock)contentElement, text);
            }
            else if (contentElement is Panel)
            {
                foreach (var textblock in ((Panel)contentElement).Children.OfType<TextBlock>())
                {
                    SetContent(textblock, text);
                }
            }
        }

        private void SetContent(TextBlock textblock, string text)
        {
            textblock.Text = text;
            textblock.UpdateLayout();
        }
    }
}
