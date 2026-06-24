using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GLAtools.Services
{
    // Attached Property reutilizavel: aplicada a um TextBox via
    // "behaviors:DigitsOnlyBehavior.IsEnabled=True" no XAML, restringe o
    // campo a aceitar somente digitos. Diferente de EventSetter/Click direto,
    // uma Attached Property nao depende de um code-behind ligado ao XAML
    // especifico onde e usada -- funciona em qualquer DataTemplate, mesmo
    // os compartilhados globalmente sem x:Class proprio (como o
    // GoalCardTemplate.xaml), porque a logica vive aqui, numa classe normal.
    public static class DigitsOnlyBehavior
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(DigitsOnlyBehavior),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            if ((bool)e.NewValue)
                textBox.PreviewTextInput += OnPreviewTextInput;
            else
                textBox.PreviewTextInput -= OnPreviewTextInput;
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }
    }
}
