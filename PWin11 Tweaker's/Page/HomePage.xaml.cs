using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace PWin11_Tweaker_s
{
    public sealed partial class HomePage : Microsoft.UI.Xaml.Controls.Page
    {
        public HomePage()
        {
            this.InitializeComponent();
            LoadAnimations();
            StartGradientAnimation();
        }

        private void LoadAnimations()
        {
            if (WelcomeText != null && WelcomeTransform != null)
            {
                // Анимация для заголовка
                Storyboard welcomeStoryboard = new Storyboard();
                DoubleAnimation welcomeAnimation = new DoubleAnimation
                {
                    From = 50,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.5),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(welcomeAnimation, WelcomeTransform);
                Storyboard.SetTargetProperty(welcomeAnimation, "Y");

                DoubleAnimation welcomeOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.5)
                };
                Storyboard.SetTarget(welcomeOpacityAnimation, WelcomeText);
                Storyboard.SetTargetProperty(welcomeOpacityAnimation, "Opacity");

                welcomeStoryboard.Children.Add(welcomeAnimation);
                welcomeStoryboard.Children.Add(welcomeOpacityAnimation);
                welcomeStoryboard.Begin();
                System.Diagnostics.Debug.WriteLine("HomePage: Анимация заголовка запущена.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("HomePage: Элементы WelcomeText или WelcomeTransform не найдены.");
            }

            if (AboutText != null && AboutTransform != null)
            {
                // Анимация для описания
                Storyboard aboutStoryboard = new Storyboard();
                DoubleAnimation aboutAnimation = new DoubleAnimation
                {
                    From = 50,
                    To = 0,
                    Duration = TimeSpan.FromSeconds(0.7),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                Storyboard.SetTarget(aboutAnimation, AboutTransform);
                Storyboard.SetTargetProperty(aboutAnimation, "Y");

                DoubleAnimation aboutOpacityAnimation = new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    Duration = TimeSpan.FromSeconds(0.7)
                };
                Storyboard.SetTarget(aboutOpacityAnimation, AboutText);
                Storyboard.SetTargetProperty(aboutOpacityAnimation, "Opacity");

                aboutStoryboard.Children.Add(aboutAnimation);
                aboutStoryboard.Children.Add(aboutOpacityAnimation);
                aboutStoryboard.Begin();
                System.Diagnostics.Debug.WriteLine("HomePage: Анимация описания запущена.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("HomePage: Элементы AboutText или AboutTransform не найдены.");
            }
        }

        private void StartGradientAnimation()
        {
            if (WelcomeText != null)
            {
                var gradientBrush = WelcomeText.Foreground as LinearGradientBrush;
                if (gradientBrush != null)
                {
                    DoubleAnimation animation = new DoubleAnimation
                    {
                        From = 0,
                        To = 1,
                        Duration = TimeSpan.FromSeconds(2),
                        AutoReverse = true,
                        RepeatBehavior = RepeatBehavior.Forever
                    };
                    Storyboard.SetTarget(animation, gradientBrush);
                    Storyboard.SetTargetProperty(animation, "(LinearGradientBrush.GradientStops)[1].Offset");
                    var storyboard = new Storyboard();
                    storyboard.Children.Add(animation);
                    storyboard.Begin();
                    System.Diagnostics.Debug.WriteLine("HomePage: Анимация градиента запущена.");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("HomePage: Градиентный кисть не найдена.");
                }
            }
        }

        private void GoToPrivacyButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Начало навигации...");
            if (this.Frame != null)
            {
                System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Frame найден, переходим на PrivacyPage.");
                this.Frame.Navigate(typeof(PrivacyPage));

                if (this.Frame.Parent is NavigationView navView)
                {
                    System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: NavigationView найден, обновляем выбранный пункт.");
                    foreach (var item in navView.MenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "PrivacyPage")
                        {
                            navView.SelectedItem = navItem;
                            System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Пункт PrivacyPage выбран.");
                            break;
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: NavigationView не найден.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("GoToPrivacyButton_Click: Frame не найден.");
            }
        }

        private void GoToPerformanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(PerformancePage));
                if (this.Frame.Parent is NavigationView navView)
                {
                    foreach (var item in navView.MenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "PerformancePage")
                        {
                            navView.SelectedItem = navItem;
                            break;
                        }
                    }
                }
            }
        }

        private void GoToExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Frame != null)
            {
                this.Frame.Navigate(typeof(ExplorerPage));
                if (this.Frame.Parent is NavigationView navView)
                {
                    foreach (var item in navView.MenuItems)
                    {
                        if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "ExplorerPage")
                        {
                            navView.SelectedItem = navItem;
                            break;
                        }
                    }
                }
            }
        }
    }
}