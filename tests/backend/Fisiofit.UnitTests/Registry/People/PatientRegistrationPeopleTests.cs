using Fisiofit.ModuleContracts.People;
using Fisiofit.Modules.Registry.People.Application;
using Fisiofit.Modules.Registry.People.Domain;
using Xunit;

namespace Fisiofit.UnitTests.Registry.People;

public sealed class PatientRegistrationPeopleTests
{
    [Fact]
    public void Cpf_NormalizesFormattingAndValidatesCheckDigits()
    {
        var normalized = PeopleNormalization.NormalizeCpf("111.444.777-35");

        Assert.Equal("11144477735", normalized);
        Assert.True(PeopleNormalization.IsValidCpf(normalized!));
        Assert.False(PeopleNormalization.IsValidCpf("11111111111"));
    }

    [Fact]
    public void Person_CreatesCurrentIdentityWithOnePrimaryPhoneAndOptionalCpf()
    {
        var person = Person.Create(
            "Pessoa Fictícia",
            new DateOnly(1990, 1, 1),
            null,
            new PhoneNumber("999", "000", "000000"));

        var phone = Assert.Single(person.ContactPoints);
        Assert.Equal(PersonRecordState.Current, person.RecordState);
        Assert.Null(person.CpfNormalized);
        Assert.True(phone.IsPrimary);
        Assert.Equal("+999000000000", phone.NormalizedValue);
    }

    [Theory]
    [InlineData("11144477735", true)]
    [InlineData("12345678900", false)]
    [InlineData("00000000000", false)]
    public void CpfValidation_HandlesCanonicalExamples(string value, bool expected) =>
        Assert.Equal(expected, PeopleNormalization.IsValidCpf(value));
}
