﻿using AdaptiveShell.LiveTiles.Manifest;
using AdaptiveShell.LiveTiles.Models;
using AdaptiveShell.LiveTiles.Models.BaseElements;
using AdaptiveShell.LiveTiles.Models.Enums;
using AdaptiveShell.LiveTiles.Renderers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.StartScreen;

namespace AdaptiveShell.LiveTiles.UserControls {
    public enum DeviceFamily {
        Desktop,
        Mobile
    }

    public sealed partial class LiveTile : UserControl {
        private LiveTileUpdater _tileUpdater;
        private BadgeUpdater _badgeUpdater;

        public LiveTile() {
            this.InitializeComponent();

            this.VerticalAlignment = VerticalAlignment.Top;
            this.HorizontalAlignment = HorizontalAlignment.Left;

            this.Initialize();
        }

        private async void Initialize() {
            this._tileUpdater = new LiveTileUpdater(this);
            this._badgeUpdater = new BadgeUpdater(this);

            this.UpdateTileSize();

            // TODO - automatically look for their Package.appxmanifest, and initialize VisualElements data from there.

            await this.UpdateAsync();

            // Initialize the default tile
            this.Show(null, false);
        }

        #region Platform

        private static readonly DependencyProperty PlatformProperty;

        #endregion

        #region TileSize

        private static readonly DependencyProperty TileSizeProperty = DependencyProperty.Register(
            "TileSize", typeof(TileSize), typeof(LiveTile), new PropertyMetadata(TileSize.Medium, OnTileSizeChanged));

        private static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            (d as LiveTile).OnTileSizeChanged(e);
        }

        private void OnTileSizeChanged(DependencyPropertyChangedEventArgs e) {
            this.UpdateTileSize();

            //re-render, since different binding might be used
            this.Reshow();
        }

        private void UpdateTileSize() {
            switch (this.TileSize) {
                case TileSize.Small:
                    this.TilePixelSize = this.TileDensity.Small;
                    break;

                case TileSize.Medium:
                    this.TilePixelSize = this.TileDensity.Medium;
                    break;

                case TileSize.Wide:
                    this.TilePixelSize = this.TileDensity.Wide;
                    break;

                case TileSize.Large:
                    this.TilePixelSize = this.TileDensity.Large;
                    break;

                default:
                    throw new NotImplementedException(this.TileSize.ToString());
            }

            this.UpdateBranding();
        }

        public TileSize TileSize {
            get => (TileSize)this.GetValue(TileSizeProperty);
            set => this.SetValue(TileSizeProperty, value);
        }

        #endregion

        #region TileDensity

        private static readonly DependencyProperty TileDensityProperty = DependencyProperty.Register("TileDensity",
            typeof(TileDensity), typeof(LiveTile),
            new PropertyMetadata(TileDensity.Desktop(), OnTileDensityChanged));

        private static void OnTileDensityChanged(Object sender, DependencyPropertyChangedEventArgs e) {
            (sender as LiveTile).OnTileDensityChanged(e);
        }

        private void OnTileDensityChanged(DependencyPropertyChangedEventArgs e) {
            if (this.TileDensity == null)
                throw new NullReferenceException("TileDensity cannot be null");

            // It automatically scales
            this.UpdateTileSize();
        }

        public TileDensity TileDensity {
            get => this.GetValue(TileDensityProperty) as TileDensity;
            set => this.SetValue(TileDensityProperty, value);
        }

        #endregion

        private static readonly DependencyProperty TilePixelSizeProperty = DependencyProperty.Register("TilePixelSize",
            typeof(Size), typeof(LiveTile), new PropertyMetadata(default(Size)));

        private Size TilePixelSize {
            get => (Size)this.GetValue(TilePixelSizeProperty);
            set => this.SetValue(TilePixelSizeProperty, value);
        }

        private static readonly DependencyProperty IsAnimationEnabledProperty =
            DependencyProperty.Register("IsAnimationEnabled", typeof(Boolean), typeof(LiveTile),
                new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether animation is enabled. If true, when new tile updates appear, they will animate onto the tile. Otherwise, they will just instantly appear.
        /// </summary>
        public Boolean IsAnimationEnabled {
            get => (Boolean)this.GetValue(IsAnimationEnabledProperty);
            set => this.SetValue(IsAnimationEnabledProperty, value);
        }

        private Template[] GetValidTemplateValues() {
            switch (this.TileSize) {
                case TileSize.Small:
                    return new Template[] { Models.Enums.Template.TileSmall };

                case TileSize.Medium:
                    return new Template[] { Models.Enums.Template.TileMedium };

                case TileSize.Wide:
                    return new Template[] { Models.Enums.Template.TileWide };

                case TileSize.Large:
                    return new Template[] { Models.Enums.Template.TileLarge };

                default:
                    return new Template[0];
            }
        }

        private Package _package;
        private Object _notificationData;

        private Uri GetLogo() {
            switch (this.TileSize) {
                case TileSize.Small:
                    return this.VisualElements.Square71x71Logo ?? this.VisualElements.Square150x150Logo;

                case TileSize.Medium:
                    return this.VisualElements.Square150x150Logo;

                case TileSize.Wide:
                    return this.VisualElements.Wide310x150Logo;

                case TileSize.Large:
                    return this.VisualElements.Square310x310Logo ?? this.VisualElements.Square150x150Logo;

                default:
                    throw new NotImplementedException();
            }
        }

        private Uri GetCornerLogo() {
            return this.VisualElements.Square44x44Logo;
        }

        private static async Task<String> GetMrtUri(Package package, String path) {
            return await package.GetMrtUri(path);
        }

        /// <summary>
        /// Returns a tile updater for updating the content of this preview tile.
        /// </summary>
        /// <returns></returns>
        public LiveTileUpdater CreateTileUpdater() {
            return this._tileUpdater;
        }

        /// <summary>
        /// Returns a badge updater for updating the badge for this preview tile.
        /// </summary>
        /// <returns></returns>
        public BadgeUpdater CreateBadgeUpdater() {
            return this._badgeUpdater;
        }

        private void ResetDisplayProperties() {
            // Update display name back to its original tile-based one (clearing any display name set by a notification)
            this._customDisplayName = null;
            this.UpdateDisplayName();

            // Update branding to default
            this._notificationBranding = null;
            this._hasNotificationForCurrentSize = false;
            this.UpdateBranding();
        }

        private void ShowDefault(Boolean animate) {
            // Show logo
            this.ShowElement(new Border() {
                Background = new SolidColorBrush(this.VisualElements.BackgroundColor),
                Child = new Image() {
                    Source = this.GenerateBitmapImage(this.GetLogo()),
                    Stretch = Stretch.UniformToFill
                }
            }, animate);
        }

        private BitmapImage GenerateBitmapImage(Uri uri) {
            if (uri == null)
                return null;

            return ImageHelper.GetBitmap(uri.OriginalString);
        }


        private Branding? _notificationBranding;
        private Boolean _hasNotificationForCurrentSize;

        private void UpdateBranding() {
            // On Small tiles, margin is only 4px, so branding needs to move too
            switch (this.TileSize) {
                case TileSize.Small:
                    this.Branding.Margin = new Thickness(0, 0, 2, -4);
                    break;

                default:
                    this.Branding.Margin = new Thickness(0, 0, 6, 0);
                    break;
            }

            Boolean showName;
            Boolean showCornerLogo;

            // If the notification overrided branding
            if (this._notificationBranding != null) {
                Branding brandingValue = this._notificationBranding.Value;

                switch (brandingValue) {
                    case Models.Enums.Branding.None:
                        showName = false;
                        showCornerLogo = false;
                        break;

                    case Models.Enums.Branding.Name:
                        showName = true;
                        showCornerLogo = false;
                        break;

                    case Models.Enums.Branding.Logo:
                        showName = false;
                        showCornerLogo = true;
                        break;

                    default: //NameAndLogo
                        showName = true;
                        showCornerLogo = true;
                        break;
                }

                // Small can never show display name (but it can apparently display logo)
                if (this.TileSize == TileSize.Small)
                    showName = false;
            }

            // Otherwise, use behaviors from VisualElements
            else {
                // Default behavior is to never show corner logo (if notification displayed, default branding inherits ShowName from basic tile properties, never shows corner logo)
                showCornerLogo = false;

                switch (this.TileSize) {
                    case TileSize.Small:
                        showName = false; // name never shown on small
                        break;

                    case TileSize.Medium:
                        showName = this.VisualElements.ShowNameOnSquare150x150Logo;
                        break;

                    case TileSize.Wide:
                        showName = this.VisualElements.ShowNameOnWide310x150Logo;
                        break;

                    case TileSize.Large:
                        showName = this.VisualElements.ShowNameOnSquare310x310Logo;
                        break;

                    default:
                        throw new NotImplementedException(this.TileSize.ToString());
                }
            }

            // Decide to show the name
            this.TextBlockDisplayName.Visibility = showName ? Visibility.Visible : Visibility.Collapsed;

            // Decide to show corner logo
            this.CornerLogo.Visibility = showCornerLogo ? Visibility.Visible : Visibility.Collapsed;


            // Decide if branding is shown...
            if (this.HasBadge() || showName || showCornerLogo)
                this.Branding.Visibility = Visibility.Visible;
            else
                this.Branding.Visibility = Visibility.Collapsed;
        }

        private Boolean HasBadge() {
            return this.badgeValueControl.Value.HasBadge();
        }

        private void Reshow() {
            this.Show(this._notificationData, false);
        }

        internal void Show(Object tile, Boolean animate) {
            // Ensure valid object passed in
            if (tile != null && !(tile is ITile)) {
                throw new InvalidOperationException("tile must be of type ITile");
            }

            // If animations are disabled, set animate to false
            if (!this.IsAnimationEnabled)
                animate = false;

            // Store the current tile data, so that when the size changes, we can re-render using the data
            this._notificationData = tile;

            // If we're already animating, we'll wait till current animation is done, and then render/animate the new content
            if (animate && this._isAnimating) {
                this._isWaitingToShow = true;
                return;
            }

            // Otherwise we'll render/animate now
            this._isWaitingToShow = false;

            // Reset things that a previous notification might have changed, like DisplayName
            this.ResetDisplayProperties();


            // If nothing to display, revert to default
            if (tile == null) {
                this.ShowDefault(animate);
                return;
            }

            // Find the first matching template for the current size, or if none, revert to default
            if (tile is ITile) {
                var xmlTile = tile as ITile;

                AdaptiveBinding binding = xmlTile.Visual.Bindings.FirstOrDefault(m => this.GetValidTemplateValues().Contains(m.Template));
                if (binding == null) {
                    this.ShowDefault(animate);
                    return;
                }

                this._hasNotificationForCurrentSize = true;


                // Custom display name from visual level
                if (xmlTile.Visual.DisplayName != null) {
                    this._customDisplayName = xmlTile.Visual.DisplayName;
                    this.UpdateDisplayName();
                }

                // Custom display name from binding level (which overrides visual)
                if (binding.DisplayName != null) {
                    this._customDisplayName = binding.DisplayName;
                    this.UpdateDisplayName();
                }



                // Update branding from notification
                this._notificationBranding = xmlTile.Visual.Branding; // first attempt to use branding specified in the visual

                if (binding.Branding != null) this._notificationBranding = binding.Branding; // and then branding has a chance to override it

                this.UpdateBranding();


                // Generate the actual notification content
                var notificationContent = new LiveTileNotification() {
                    RequestedTheme = ElementTheme.Dark
                };
                notificationContent.InitializeFromXml(
                    tileSize: this.TileSize,
                    //tilePixelSize: TilePixelSize,
                    binding: binding,
                    visualElements: this.VisualElements,
                    isBrandingVisible: this.Branding.Visibility == Visibility.Visible);

                // And then show it
                this.ShowElement(notificationContent, animate);
            }
        }

        private Boolean _isAnimating;
        private Boolean _isWaitingToShow;

        /// <summary>
        /// Animates the tile to show this new content
        /// </summary>
        /// <param name="el"></param>
        private void ShowElement(UIElement el, Boolean animate) {
            UIElement previous = this.canvas.Children.LastOrDefault();

            // If we're not animating, or no previous content, clear children and instantly show new content
            if (!animate || previous == null) {
                this.canvas.Children.Clear();
                this.canvas.Children.Add(el);
                return;
            }

            // Otherwise, clear all but previous
            while (this.canvas.Children.Count > 1) this.canvas.Children.RemoveAt(0);

            // And then add our new
            this.canvas.Children.Add(el);


            //Storyboard s = CreateAnimationForAlreadySeenNotification(el);
            Storyboard s = this.CreateAnimationForNewNotification(previous, el);

            s.Completed += delegate {
                this._isAnimating = false;

                // Remove our previous
                this.canvas.Children.Remove(previous);

                if (this._isWaitingToShow) {
                    this.Show(this._notificationData, animate);
                }
            };

            this._isAnimating = true;
            s.Begin();
        }

        private Storyboard CreateAnimationForNewNotification(UIElement oldNotification, UIElement newNotification) {
            var s = new Storyboard();

            var duration = TimeSpan.FromSeconds(0.5);
            var halfTime = TimeSpan.FromSeconds(duration.TotalSeconds / 2);


            // Assign a scale transform to the old notification
            oldNotification.Projection = new PlaneProjection();

            // Make the height decrease
            var a = new DoubleAnimation() {
                Duration = halfTime,
                To = 90,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseIn }
            };
            Storyboard.SetTarget(a, oldNotification.Projection);
            Storyboard.SetTargetProperty(a, "RotationX");
            s.Children.Add(a);





            // For new notification...
            newNotification.Projection = new PlaneProjection() {
                RotationX = -90
            };

            // Make the height increase
            a = new DoubleAnimation() {
                Duration = halfTime,
                BeginTime = halfTime,
                To = 0,
                EasingFunction = new ExponentialEase() { EasingMode = EasingMode.EaseOut }
            };
            Storyboard.SetTarget(a, newNotification.Projection);
            Storyboard.SetTargetProperty(a, "RotationX");
            s.Children.Add(a);



            return s;
        }

        private Storyboard CreateAnimationForAlreadySeenNotification(UIElement alreadySeenNotification) {
            var s = new Storyboard();

            var animateNewElement = new DoubleAnimation() {
                Duration = TimeSpan.FromSeconds(1),
                From = this.TilePixelSize.Height,
                To = 0
            };

            alreadySeenNotification.RenderTransform = new TranslateTransform() {
                Y = animateNewElement.From.Value
            };

            Storyboard.SetTarget(animateNewElement, alreadySeenNotification.RenderTransform);
            Storyboard.SetTargetProperty(animateNewElement, "Y");

            s.Children.Add(animateNewElement);

            return s;
        }

        private String _customDisplayName = null;

        private static readonly DependencyProperty DisplayNameProperty =
            DependencyProperty.Register("DisplayName", typeof(String), typeof(LiveTile), new PropertyMetadata(""));

        /// <summary>
        /// Gets or sets a name that is associated with and displayed on the preview tile. This name is displayed on the tile and in the tile's tooltip. UpdateAsync must be called after changing this in order to commit the changes to the UI of the tile.
        /// </summary>
        public String DisplayName {
            get => this.GetValue(DisplayNameProperty) as String;
            set => this.SetValue(DisplayNameProperty, value);
        }

        private void UpdateDisplayName() {
            if (this._customDisplayName != null)
                this.TextBlockDisplayName.Text = this._customDisplayName;

            else
                this.TextBlockDisplayName.Text = this.DisplayName;
        }

        private void UpdateVisualElements() {
            this.CornerLogo.Source = this.GenerateBitmapImage(this.GetCornerLogo());

            // Re-render notification since background color might have changed
            this.Reshow();
        }

        /// <summary>
        /// Gets an object through which you can get or set the preview tile's background color, tile images, and showing/hiding the display name.
        /// </summary>
        public LiveTileVisualElements VisualElements { get; private set; } = new LiveTileVisualElements();

        /// <summary>
        /// Commits the changes made to tile properties like DisplayName and VisualElements, causing the UI of the tile to reflect the changes.
        /// </summary>
        /// <returns></returns>
        public IAsyncAction UpdateAsync() {
            // This method is async to be more similar to SecondaryTile API, even though it doesn't do anything requiring awaits.

            return this.UpdateAsyncHelper().AsAsyncAction();
        }

        /// <summary>
        /// Make it async even though it's not, so it's more similar to secondary tile API
        /// </summary>
        /// <returns></returns>
        private Task UpdateAsyncHelper() {
            this.UpdateDisplayName();

            this.UpdateVisualElements();

            return Task.CompletedTask;
        }

        internal void SetBadge(BadgeValue value) {
            this.badgeValueControl.Value = value;

            // This might have affected branding height, so notification needs to re-display to adjust for the available size
            this.Reshow();
        }
    }
}
