using System.Security.Cryptography;
using System.Text;
using Fisiofit.ModuleContracts.People;

namespace Fisiofit.Modules.Registry.Patients.Application;

internal static class PatientRequestCanonicalizer
{
    public static string NormalizeName(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Normalize(NormalizationForm.FormC);

    public static string Digits(string? value) =>
        string.Concat((value ?? string.Empty).Where(char.IsAsciiDigit));

    public static string? NormalizeCpf(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Digits(value);

    public static string NormalizePayerMode(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    public static string RequestHash(
        string fullName,
        DateOnly birthDate,
        PatientRegistrationPhone phone,
        string? cpf,
        Guid unitId,
        DateOnly relationshipStartedOn,
        string payerMode) =>
        Hash(
            fullName,
            birthDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            phone.CountryCode,
            phone.AreaCode,
            phone.Number,
            cpf ?? "<null>",
            unitId.ToString("D"),
            relationshipStartedOn.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            payerMode);

    public static string PeopleHash(
        string fullName,
        DateOnly birthDate,
        PatientRegistrationPhone phone,
        string? cpf) =>
        Hash(
            fullName,
            birthDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            phone.CountryCode,
            phone.AreaCode,
            phone.Number,
            cpf ?? "<null>");

    public static bool IsValidCpf(string cpf)
    {
        if (cpf.Length != 11 || cpf.Distinct().Count() == 1)
        {
            return false;
        }

        return CheckDigit(cpf, 9) == cpf[9] - '0' && CheckDigit(cpf, 10) == cpf[10] - '0';
    }

    private static string Hash(params string[] values)
    {
        var canonical = string.Concat(values.Select(value => $"{Encoding.UTF8.GetByteCount(value)}:{value};"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
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
