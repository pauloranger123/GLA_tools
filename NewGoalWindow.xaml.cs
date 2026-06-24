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
            UpdateFieldLabels();
        }

        private void UseBerriesCheck_Changed(object sender, RoutedEventArgs e)
            => BerriesInput.IsEnabled = UseBerriesCheck.IsChecked == true;

        private void UseGemsCheck_Changed(object sender, RoutedEventArgs e)
            => GemsInput.IsEnabled = UseGemsCheck.IsChecked == true;

        // Atualiza os rotulos acima dos campos para deixar claro o que o
        // numero digitado representa em cada modo (por membro vs total).
        private void DivisionMode_Changed(object sender, RoutedEventArgs e)
        {
            UpdateFieldLabels();
        }

        private void UpdateFieldLabels()
        {
            // Esse metodo pode ser chamado pelo evento Checked do RadioButton
            // ANTES do XAML terminar de inicializar todos os elementos da
            // janela (o parser dispara Checked no momento em que processa
            // IsChecked="True" no XAML, e elementos declarados DEPOIS dele
            // no arquivo -- como TotalSplitRadio, BerriesLabel, GemsLabel --
            // ainda nao existem nesse momento). Por isso protegemos contra
            // TODOS os elementos usados aqui, nao so o primeiro.
            if (TotalSplitRadio == null || BerriesLabel == null || GemsLabel == null) return;

            bool isTotalSplit = TotalSplitRadio.IsChecked == true;

            if (isTotalSplit)
            {
                int memberCount = _players.Count;
                BerriesLabel.Text = $"Total de Berries a dividir entre {memberCount} membro(s)";
                GemsLabel.Text = $"Total de Gemas a dividir entre {memberCount} membro(s)";
            }
            else
            {
                BerriesLabel.Text = "Berries que cada membro deve doar";
                GemsLabel.Text = "Gemas que cada membro deve doar";
            }
        }

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

            if (_players.Count == 0)
            {
                ShowError("A aliança não tem nenhum membro cadastrado ainda.");
                return;
            }

            bool isTotalSplit = TotalSplitRadio.IsChecked == true;

            var goal = new AllianceGoal
            {
                Title = title,
                DivisionMode = isTotalSplit ? GoalDivisionMode.TotalSplit : GoalDivisionMode.PerMember,
                Deadline = DeadlinePicker.SelectedDate.Value
            };

            if (isTotalSplit)
            {
                // Modo "valor total": guarda o total como referencia fixa;
                // o valor por membro (BerriesTarget/GemsTarget) e calculado
                // a seguir, depois que as Donations forem criadas, ja que
                // RecalculateSplit() precisa da lista de Donations preenchida
                // para saber por quantos membros dividir.
                goal.BerriesTotalTarget = useBerries ? berriesTarget : 0;
                goal.GemsTotalTarget = useGems ? gemsTarget : 0;
            }
            else
            {
                // Modo "por membro": o valor digitado e o valor final direto,
                // sem necessidade de calculo de divisao.
                goal.BerriesTarget = useBerries ? berriesTarget : 0;
                goal.GemsTarget = useGems ? gemsTarget : 0;
            }

            // Cria automaticamente uma linha de doacao (zerada) para cada player existente.
            // No modo TotalSplit, o BerriesTarget/GemsTarget aqui ainda nao e o valor
            // final -- sera substituido pelo RecalculateSplit() chamado abaixo.
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

            if (isTotalSplit)
            {
                goal.RecalculateSplit();
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