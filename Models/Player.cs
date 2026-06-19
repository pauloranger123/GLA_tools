using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GLAtools.Models
{
    // Representa um membro da alianca. Implementa INotifyPropertyChanged
    // para que a interface (XAML) atualize automaticamente quando os
    // valores mudarem (ex: quando o lider digita a doacao).
    public class Player : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
