using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GLAtools.Models;
using GLAtools.Services;

namespace GLAtools
{
    public partial class NewGoalWindow : Window
    {
        // Resultado exposto para a janela principal ler depois do ShowDialog()
        public AllianceGoal? CreatedGoal { get; private set; }
        private readonly List<Player> _players;

        // Evita reentrada infinita: ao reformatar o texto dentro do proprio
        // handler de TextChanged, isso disparia TextChanged de novo. Essa
        // flag garante que so reagimos a mudancas feitas pelo usuario, nao
        // as que nosso proprio codigo de formatacao provoca.
        private bool _isFormattingBerries = false;

        public NewGoalWindow(List<Player> players)
        {
            InitializeComponent();
            _players = players;
            DeadlinePicker.SelectedDate = DateTime.Now.AddDays(7);
        }

        private void UseBerriesCheck_Changed(object sender, RoutedEventArgs e)
            => BerriesInput.IsEnabled = UseBerriesCheck.IsChecked == true;

        private void UseGemsCheck_Changed(object sender, RoutedEventArgs e)
            => GemsInput.IsEnabled = UseGemsCheck.IsChecked == true;

        // Berries: reformata o texto com pontos de milhar (ex: 100000 -> 100.000)
        // a cada digitacao, mantendo o cursor numa posicao sensata.
        private void BerriesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormattingBerries) return;

            var textBox = (TextBox)sender;
            int caretPos = textBox.CaretIndex;
            int digitsBeforeCaretBefore = CountDigitsBefore(textBox.Text, caretPos);

            NumberFormatHelper.TryParse(textBox.Text, out long value);
            string formatted = value == 0 && NumberFormatHelper.StripFormatting(textBox.Text).Length == 0
                ? string.Empty
                : NumberFormatHelper.FormatThousands(value);

            _isFormattingBerries = true;
            textBox.Text = formatted;

            // Reposiciona o cursor contando a mesma quantidade de DIGITOS
            // (ignorando pontos) que havia antes da posicao original do cursor.
            // Isso evita que o cursor "salte" para o inicio/fim ao digitar no meio.
            int newCaretPos = PositionAfterNDigits(formatted, digitsBeforeCaretBefore);
            textBox.CaretIndex = newCaretPos;
            _isFormattingBerries = false;
        }

        private static int CountDigitsBefore(string text, int position)
        {
            int count = 0;
            for (int i = 0; i < position && i < text.Length; i++)
            {
                if (char.IsDigit(text[i])) count++;
            }
            return count;
        }

        private static int PositionAfterNDigits(string text, int n)
        {
            int digitsSeen = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (digitsSeen == n) return i;
                if (char.IsDigit(text[i])) digitsSeen++;
            }
            return text.Length;
        }

        // Gemas: bloqueia qualquer caractere que nao seja digito, garantindo
        // que o valor seja SEMPRE um numero inteiro puro, sem ponto/virgula.
        private void GemsInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string title = TitleInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                ShowError("Digite um título para a meta.");
                return;
            }

            bool useBerries = UseBerriesCheck.IsChecked == true;
            bool useGems = UseGemsCheck.IsChecked == true;

            if (!useBerries && !useGems)
            {
                ShowError("Selecione pelo menos um recurso (Berries ou Gemas).");
                return;
            }

            long berriesTarget = 0;
            long gemsTarget = 0;

            if (useBerries && !NumberFormatHelper.TryParse(BerriesInput.Text, out berriesTarget))
            {
                ShowError("Valor de Berries inválido. Digite apenas números.");
                return;
            }

            if (useGems && !long.TryParse(GemsInput.Text.Trim(), out gemsTarget))
            {
                ShowError("Valor de Gemas inválido. Digite apenas números.");
                return;
            }

            if (DeadlinePicker.SelectedDate == null)
            {
                ShowError("Selecione uma data limite.");
                return;
            }

            var goal = new AllianceGoal
            {
                Title = title,
                BerriesTarget = useBerries ? berriesTarget : 0,
                GemsTarget = useGems ? gemsTarget : 0,
                Deadline = DeadlinePicker.SelectedDate.Value
            };

            // Cria automaticamente uma linha de doacao (zerada) para cada player existente
            foreach (var player in _players)
            {
                goal.Donations.Add(new PlayerDonation
                {
                    PlayerId = player.Id,
                    PlayerName = player.Name,
                    BerriesTarget = goal.BerriesTarget,
                    GemsTarget = goal.GemsTarget,
                    ParentGoal = goal
                });
            }

            CreatedGoal = goal;
            DialogResult = true;
            Close();
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
