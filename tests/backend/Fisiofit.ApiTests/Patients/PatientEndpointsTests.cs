using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Fisiofit.ApiTests.Patients;

[Collection(PatientApiCollection.Name)]
public sealed class PatientEndpointsTests(PatientApiFixture fixture)
{
    private static readonly Guid ActorId = Guid.Parse("0199ffee-0000-7000-8000-000000000301");

    [Fact]
    public async Task PostAndGet_SucceedWithLocationAndNoStore()
    {
        using var client = AuthorizedClient();
        var post = await client.PostAsJsonAsync(
            "/api/v1/patients",
            Request(),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        Assert.NotNull(post.Headers.Location);
        var created = await JsonDocument.ParseAsync(
            await post.Content.ReadAsStreamAsync(CancellationToken.None),
            cancellationToken: CancellationToken.None);
        var patientId = created.RootElement.GetProperty("patientId").GetGuid();
        Assert.Equal($"/api/v1/patients/{patientId:D}", post.Headers.Location!.OriginalString);
        Assert.Equal("ACTIVE", created.RootElement.GetProperty("status").GetString());

        var get = await client.GetAsync(post.Headers.Location, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Contains("no-store", get.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        var details = await JsonDocument.ParseAsync(
            await get.Content.ReadAsStreamAsync(CancellationToken.None),
            cancellationToken: CancellationToken.None);
        Assert.Equal("Pessoa Fictícia da API", details.RootElement.GetProperty("fullName").GetString());
        Assert.Equal("ABSENT", details.RootElement.GetProperty("cpf").GetProperty("status").GetString());
        Assert.False(details.RootElement.GetProperty("cpf").TryGetProperty("masked", out _));
    }

    [Fact]
    public async Task Post_RequiresAuthenticationPermissionsUnitScopeAndIdempotencyKey()
    {
        using var anonymous = fixture.CreateClient();
        anonymous.DefaultRequestHeaders.Add("Idempotency-Key", UniqueKey("anonymous"));
        var unauthorized = await anonymous.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);

        using var noPermissions = fixture.CreateClient();
        Authenticate(noPermissions, permissions: string.Empty, units: fixture.ActiveUnitId.ToString("D"));
        noPermissions.DefaultRequestHeaders.Add("Idempotency-Key", UniqueKey("no-permission"));
        var forbidden = await noPermissions.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);

        using var wrongUnit = fixture.CreateClient();
        Authenticate(wrongUnit, Permissions(), Guid.CreateVersion7().ToString("D"));
        wrongUnit.DefaultRequestHeaders.Add("Idempotency-Key", UniqueKey("wrong-unit"));
        var scopeDenied = await wrongUnit.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);

        using var missingKey = AuthorizedClient(addIdempotencyKey: false);
        var keyRequired = await missingKey.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal("UNAUTHORIZED", await Code(unauthorized));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, scopeDenied.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, keyRequired.StatusCode);
        Assert.Equal("IDEMPOTENCY_KEY_REQUIRED", await Code(keyRequired));
    }

    [Fact]
    public async Task Post_RejectsMinorNonSelfPayerAndInvalidRequest()
    {
        using var minorClient = AuthorizedClient();
        var minor = await minorClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { BirthDate = new DateOnly(2015, 1, 1) },
            CancellationToken.None);

        using var payerClient = AuthorizedClient();
        var payer = await payerClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { PayerMode = "OTHER" },
            CancellationToken.None);

        using var invalidClient = AuthorizedClient();
        var invalid = await invalidClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { FullName = " " },
            CancellationToken.None);

        using var malformedClient = AuthorizedClient();
        var malformed = await malformedClient.PostAsync(
            "/api/v1/patients",
            new StringContent("{", Encoding.UTF8, "application/json"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, minor.StatusCode);
        Assert.Equal("MINOR_REQUIRES_GUARDIAN_FLOW", await Code(minor));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, payer.StatusCode);
        Assert.Equal("NON_SELF_PAYER_REQUIRES_RESPONSIBLE_PAYER_FLOW", await Code(payer));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await Code(invalid));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal("VALIDATION_ERROR", await Code(malformed));
    }

    [Fact]
    public async Task Post_MapsUnitNotFoundAndInactive()
    {
        var missingUnit = Guid.CreateVersion7();
        using var missingClient = fixture.CreateClient();
        Authenticate(missingClient, Permissions(), missingUnit.ToString("D"));
        missingClient.DefaultRequestHeaders.Add("Idempotency-Key", UniqueKey("missing-unit"));
        var missing = await missingClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { PrimaryUnitId = missingUnit },
            CancellationToken.None);

        using var inactiveClient = fixture.CreateClient();
        Authenticate(inactiveClient, Permissions(), fixture.InactiveUnitId.ToString("D"));
        inactiveClient.DefaultRequestHeaders.Add("Idempotency-Key", UniqueKey("inactive-unit"));
        var inactive = await inactiveClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { PrimaryUnitId = fixture.InactiveUnitId },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("RESOURCE_NOT_FOUND", await Code(missing));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, inactive.StatusCode);
        Assert.Equal("UNIT_INACTIVE", await Code(inactive));
    }

    [Fact]
    public async Task Post_ReplaysAndRejectsMismatchedPayload()
    {
        var key = UniqueKey("replay");
        using var client = AuthorizedClient(key);
        var first = await client.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);
        var replay = await client.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);
        var mismatch = await client.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { FullName = "Outra Pessoa Fictícia" },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, replay.StatusCode);
        Assert.True(replay.Headers.TryGetValues("Idempotency-Replayed", out var values));
        Assert.Equal("true", Assert.Single(values));
        Assert.Equal(await first.Content.ReadAsStringAsync(CancellationToken.None), await replay.Content.ReadAsStringAsync(CancellationToken.None));
        Assert.Equal(HttpStatusCode.Conflict, mismatch.StatusCode);
        Assert.Equal("IDEMPOTENCY_CONFLICT", await Code(mismatch));
    }

    [Fact]
    public async Task Post_MapsCpfDuplicateWithoutExposingConflictingPerson()
    {
        const string fictitiousCpf = "111.444.777-35";
        using var firstClient = AuthorizedClient();
        var first = await firstClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { Cpf = fictitiousCpf },
            CancellationToken.None);

        using var secondClient = AuthorizedClient();
        var duplicate = await secondClient.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { FullName = "Outra Identidade Fictícia", Cpf = fictitiousCpf },
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("CPF_ALREADY_REGISTERED", await Code(duplicate));
        var body = await duplicate.Content.ReadAsStringAsync(CancellationToken.None);
        Assert.DoesNotContain("111", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUnknown_ReturnsCanonicalNotFound()
    {
        using var client = AuthorizedClient();

        var response = await client.GetAsync($"/api/v1/patients/{Guid.CreateVersion7():D}", CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("RESOURCE_NOT_FOUND", await Code(response));
    }

    [Fact]
    public async Task Get_EnforcesAuthenticationPermissionAndUnitScope()
    {
        using var creator = AuthorizedClient();
        var created = await creator.PostAsJsonAsync("/api/v1/patients", Request(), CancellationToken.None);
        var location = Assert.IsType<Uri>(created.Headers.Location);

        using var anonymous = fixture.CreateClient();
        var unauthorized = await anonymous.GetAsync(location, CancellationToken.None);

        using var noPermission = fixture.CreateClient();
        Authenticate(noPermission, "people.person.read", fixture.ActiveUnitId.ToString("D"));
        var forbidden = await noPermission.GetAsync(location, CancellationToken.None);

        using var wrongUnit = fixture.CreateClient();
        Authenticate(wrongUnit, "patients.profile.read,people.person.read", Guid.CreateVersion7().ToString("D"));
        var scopeDenied = await wrongUnit.GetAsync(location, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, scopeDenied.StatusCode);
    }

    [Fact]
    public async Task Search_ReturnsMinimizedMaskedPageAndNoStore()
    {
        const string fictitiousCpf = "529.982.247-25";
        using var client = AuthorizedClient();
        var created = await client.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { FullName = "Paciente Busca API Única", Cpf = fictitiousCpf },
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var response = await client.GetAsync(
            "/api/v1/patients?search=52998224725&page=1&pageSize=25&sort=name",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(CancellationToken.None),
            cancellationToken: CancellationToken.None);
        var item = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("Paciente Busca API Única", item.GetProperty("fullName").GetString());
        Assert.Equal("***.***.***-25", item.GetProperty("cpf").GetProperty("masked").GetString());
        Assert.Equal("**0000", item.GetProperty("primaryPhone").GetProperty("maskedNumber").GetString());
        Assert.False(item.TryGetProperty("personId", out _));
        Assert.False(item.TryGetProperty("birthDate", out _));
        Assert.Equal(1, document.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_UsesDefaultsSupportsNameAndPhoneAndReturnsEmptyPage()
    {
        using var client = AuthorizedClient();
        var uniqueName = $"Paciente Busca {Guid.CreateVersion7():N}";
        var created = await client.PostAsJsonAsync(
            "/api/v1/patients",
            Request() with { FullName = uniqueName },
            CancellationToken.None);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        var name = await client.GetAsync($"/api/v1/patients?search={uniqueName}", CancellationToken.None);
        var phone = await client.GetAsync("/api/v1/patients?search=%2B999000000000", CancellationToken.None);
        var beyond = await client.GetAsync($"/api/v1/patients?search={uniqueName}&page=2&pageSize=1", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, name.StatusCode);
        Assert.Equal(HttpStatusCode.OK, phone.StatusCode);
        var nameDocument = await JsonDocument.ParseAsync(await name.Content.ReadAsStreamAsync(CancellationToken.None));
        Assert.Equal(1, nameDocument.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(25, nameDocument.RootElement.GetProperty("pageSize").GetInt32());
        var beyondDocument = await JsonDocument.ParseAsync(await beyond.Content.ReadAsStreamAsync(CancellationToken.None));
        Assert.Empty(beyondDocument.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(1, beyondDocument.RootElement.GetProperty("totalCount").GetInt32());
    }

    [Fact]
    public async Task Search_RejectsInvalidParametersAndEnforcesAuthenticationAndScope()
    {
        using var authorized = AuthorizedClient();
        var invalidSearch = await authorized.GetAsync("/api/v1/patients?search=ab", CancellationToken.None);
        var invalidPage = await authorized.GetAsync("/api/v1/patients?page=0", CancellationToken.None);
        var invalidStatus = await authorized.GetAsync("/api/v1/patients?administrativeStatus=UNKNOWN", CancellationToken.None);
        var invalidSort = await authorized.GetAsync("/api/v1/patients?sort=createdAt", CancellationToken.None);
        var unknown = await authorized.GetAsync("/api/v1/patients?name=Paciente", CancellationToken.None);

        using var anonymous = fixture.CreateClient();
        var unauthorized = await anonymous.GetAsync("/api/v1/patients", CancellationToken.None);

        using var noScope = fixture.CreateClient();
        Authenticate(noScope, "patients.profile.read,people.person.read", string.Empty);
        var noScopeResponse = await noScope.GetAsync("/api/v1/patients", CancellationToken.None);

        using var wrongScope = fixture.CreateClient();
        Authenticate(wrongScope, "patients.profile.read,people.person.read", Guid.CreateVersion7().ToString("D"));
        var scopeResponse = await wrongScope.GetAsync(
            $"/api/v1/patients?primaryUnitId={fixture.ActiveUnitId:D}",
            CancellationToken.None);

        Assert.All([invalidSearch, invalidPage, invalidStatus, invalidSort, unknown],
            response => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode));
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, noScopeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, scopeResponse.StatusCode);
    }

    private HttpClient AuthorizedClient(string? idempotencyKey = null, bool addIdempotencyKey = true)
    {
        var client = fixture.CreateClient();
        Authenticate(client, Permissions(), $"{fixture.ActiveUnitId:D},{fixture.InactiveUnitId:D}");
        if (addIdempotencyKey)
        {
            client.DefaultRequestHeaders.Add("Idempotency-Key", idempotencyKey ?? UniqueKey("api"));
        }

        return client;
    }

    private static void Authenticate(HttpClient client, string permissions, string units)
    {
        client.DefaultRequestHeaders.Add("X-Test-Actor", ActorId.ToString("D"));
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        client.DefaultRequestHeaders.Add("X-Test-Units", units);
    }

    private static string Permissions() =>
        "patients.profile.create,patients.profile.read,people.person.create,people.person.read";

    private static string UniqueKey(string prefix) => $"{prefix}-{Guid.CreateVersion7():N}";

    private PatientRequest Request() => new(
        "Pessoa Fictícia da API",
        new DateOnly(1990, 1, 1),
        new PhoneRequest("999", "000", "000000"),
        null,
        fixture.ActiveUnitId,
        new DateOnly(2026, 9, 18),
        "SELF");

    private static async Task<string?> Code(HttpResponseMessage response)
    {
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(CancellationToken.None),
            cancellationToken: CancellationToken.None);
        return document.RootElement.GetProperty("code").GetString();
    }

    private sealed record PatientRequest(
        string FullName,
        DateOnly BirthDate,
        PhoneRequest Phone,
        string? Cpf,
        Guid PrimaryUnitId,
        DateOnly RelationshipStartedOn,
        string PayerMode);

    private sealed record PhoneRequest(string CountryCode, string AreaCode, string Number);
}
