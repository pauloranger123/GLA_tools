using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GLAtools.Converters
{
    // Converte um bool (meta batida ou nao) na cor de destaque do texto/borda.
    public class GoalMetToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool met = value is bool b && b;
            return met
                ? new SolidColorBrush(Color.FromRgb(0x6B, 0xA5, 0x4A))   // verde envelhecido
                : new SolidColorBrush(Color.FromRgb(0xC9, 0xA8, 0x76)); // dourado padrao
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Converte bool em Visibility (Collapsed quando false), usado para
    // esconder a linha de Berries ou Gemas quando a meta nao usa aquele recurso.
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool flag = value is bool b && b;
            return flag ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Mostra "Expirado" em vermelho quando a meta passou do prazo,
    // ou "X dias restantes" caso contrario.
    public class DaysRemainingToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int days)
            {
                return days <= 0 ? "Prazo encerrado" : $"{days} dia(s) restante(s)";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    // Mostra um icone de cadeado fechado quando a meta esta bloqueada
    // para edicao, ou aberto quando esta liberada para editar.
    public class LockIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isLocked = value is bool b && b;
            return isLocked ? "🔒" : "🔓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
