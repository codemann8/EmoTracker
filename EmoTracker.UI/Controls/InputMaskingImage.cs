using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using EmoTracker.UI.Media.Utility;
using System.Linq;

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

        // Guard against re-entrant hit testing from InputHitTest
        private bool mInSiblingCheck = false;

        public bool HitTest(Avalonia.Point point)
        {
            if (!UseAlphaHitTest)
                return Bounds.Contains(point);

            // Opaque pixel — always catch
            if (HitTestAlphaMask(point))
                return true;

            // During the sibling check, we're re-entered via InputHitTest.
            // Report false so the hit test can find what's underneath us.
            if (mInSiblingCheck)
                return false;

            // Transparent pixel — only pass through if there's another interactive
            // element underneath that could receive this click (e.g. overlapping items
            // in a Container or CanvasPanel). If nothing is underneath, catch the click
            // so the item remains fully clickable on its bounding box.
            return !HasInteractiveSiblingUnderneath(point);
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

        /// <summary>
        /// Checks whether there is another interactive visual (e.g. another item)
        /// at the given local point that sits behind this image in the Z-order.
        /// This allows transparent areas to pass clicks through when overlapping
        /// other items, while still catching clicks when nothing is underneath.
        /// </summary>
        private bool HasInteractiveSiblingUnderneath(Avalonia.Point localPoint)
        {
            try
            {
                var topLevel = this.GetVisualRoot() as IInputElement;
                if (topLevel == null) return false;

                var rootVisual = topLevel as Visual;
                if (rootVisual == null) return false;

                var screenPoint = this.TranslatePoint(localPoint, rootVisual);
                if (screenPoint == null) return false;

                mInSiblingCheck = true;
                IInputElement? hit;
                try
                {
                    hit = topLevel.InputHitTest(screenPoint.Value);
                }
                finally
                {
                    mInSiblingCheck = false;
                }

                if (hit == null) return false;

                var hitVisual = hit as Visual;
                if (hitVisual == null) return false;

                // Only consider it "something underneath" if the hit lands inside
                // a DIFFERENT TrackableItemControl. Hitting a background panel,
                // layout grid, map image, etc. does NOT count.
                return IsInDifferentTrackableItem(hitVisual);
            }
            catch
            {
                mInSiblingCheck = false;
                return false;
            }
        }

        /// <summary>
        /// Returns true if the given visual belongs to a TrackableItemControl
        /// that is different from the one containing this InputMaskingImage.
        /// </summary>
        private bool IsInDifferentTrackableItem(Visual visual)
        {
            // Find our own TrackableItemControl ancestor
            var ourItem = this.FindAncestorOfType<UserControl>();

            // Walk up from the hit visual looking for any TrackableItemControl
            Visual? current = visual;
            while (current != null)
            {
                if (current is UserControl uc && uc.GetType().Name == "TrackableItemControl")
                {
                    // Found a TrackableItemControl — is it ours or a different one?
                    return uc != ourItem;
                }
                current = current.GetVisualParent() as Visual;
            }

            // Hit didn't land in any TrackableItemControl — not a sibling item
            return false;
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
