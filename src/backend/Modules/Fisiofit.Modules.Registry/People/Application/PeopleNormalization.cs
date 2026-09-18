using System.Text;

namespace Fisiofit.Modules.Registry.People.Application;

internal static class PeopleNormalization
{
    public static string NormalizeName(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).Normalize(NormalizationForm.FormC);

    public static string? NormalizeCpf(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return string.Concat(value.Where(char.IsAsciiDigit));
    }

    public static string Digits(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsAsciiDigit));

    public static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
        {
            return false;
        }

        return CheckDigit(cpf, 9) == cpf[9] - '0' && CheckDigit(cpf, 10) == cpf[10] - '0';
    }

    private static int CheckDigit(string cpf, int length)
    {
        var sum = 0;
        for (var index = 0; index < length; index++)
        {
            sum += (cpf[index] - '0') * (length + 1 - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
