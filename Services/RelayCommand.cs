using System.Windows.Input;

namespace GLAtools.Services
{
    // Implementacao minima de ICommand, usada para conectar botoes dentro de
    // DataTemplates a acoes (metodos) sem precisar de eventos roteados.
    // E mais confiavel que capturar Click via bubbling porque o Command e
    // resolvido diretamente pelo binding, independente da posicao do botao
    // na arvore visual (mesmo dentro de ScrollViewers aninhados).
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
