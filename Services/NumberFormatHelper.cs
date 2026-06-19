using System.Globalization;
using System.Text;

namespace GLAtools.Services
{
    // Centraliza a logica de formatar/converter numeros de Berries no padrao
    // usado pelo jogo: ponto como separador de milhar (100.000), sem decimais.
    // Gemas NAO usa essa formatacao -- e sempre numero inteiro puro.
    public static class NumberFormatHelper
    {
        // Cultura fixa (pt-BR usa ponto como separador de milhar e virgula
        // como decimal) -- usamos isso so para o separador de milhar; nunca
        // exibimos decimais, ja que berries sao sempre valores inteiros.
        private static readonly CultureInfo Culture = new CultureInfo("pt-BR");

        // Converte um numero (long) para texto formatado: 100000 -> "100.000"
        public static string FormatThousands(long value)
        {
            return value.ToString("#,##0", Culture);
        }

        // Remove toda formatacao (pontos) de um texto, deixando so os digitos.
        // Usado antes de fazer o parse para long.
        public static string StripFormatting(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (char.IsDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }

        // Tenta converter um texto (formatado ou nao) para long.
        // Retorna true e o valor se for um numero valido; false caso contrario
        // (inclui string vazia, que retorna false com value = 0).
        public static bool TryParse(string text, out long value)
        {
            string digitsOnly = StripFormatting(text);
            if (string.IsNullOrEmpty(digitsOnly))
            {
                value = 0;
                return false;
            }
            return long.TryParse(digitsOnly, out value);
        }
    }
}
