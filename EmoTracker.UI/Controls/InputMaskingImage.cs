using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using EmoTracker.UI.Media.Utility;

namespace EmoTracker.UI.Controls
{
    public class InputMaskingImage : Image, ICustomHitTest
    {
        public static readonly StyledProperty<bool> UseAlphaHitTestProperty =
            AvaloniaProperty.Register<InputMaskingImage, bool>(nameof(UseAlphaHitTest), defaultValue: true);

        public bool UseAlphaHitTest
        {
            get => GetValue(UseAlphaHitTestProperty);
            set => SetValue(UseAlphaHitTestProperty, value);
        }

        public bool HitTest(Avalonia.Point point)
        {
            if (!UseAlphaHitTest)
                return Bounds.Contains(point);
            return HitTestAlphaMask(point);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            if (UseAlphaHitTest && !HitTestAlphaMask(e.GetPosition(this)))
                return;
            base.OnPointerMoved(e);
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (UseAlphaHitTest && !HitTestAlphaMask(e.GetPosition(this)))
                return;
            base.OnPointerPressed(e);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (UseAlphaHitTest && !HitTestAlphaMask(e.GetPosition(this)))
                return;
            base.OnPointerReleased(e);
        }

        private bool HitTestAlphaMask(Avalonia.Point point)
        {
            try
            {
                if (Source == null) return false;

                var maskEntry = IconUtility.GetAlphaMask(Source);
                if (maskEntry == null) return true;

                var (mask, maskW, maskH) = maskEntry.Value;

                int px = System.Math.Min((int)(point.X / Bounds.Width  * maskW), maskW - 1);
                int py = System.Math.Min((int)(point.Y / Bounds.Height * maskH), maskH - 1);

                if (px < 0 || py < 0) return false;
                return mask[py * maskW + px];
            }
            catch
            {
                return false;
            }
        }
    }
}
