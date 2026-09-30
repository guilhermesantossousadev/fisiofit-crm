using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Fisiofit.Modules.Registry.Patients.Domain;
using Fisiofit.Modules.Registry.Patients.Infrastructure;
using Fisiofit.Modules.Registry.People.Domain;
using Fisiofit.Modules.Registry.People.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fisiofit.ApiTests.Patients;

[Collection(PatientApiCollection.Name)]
public sealed class GuardianLinkEndpointsTests(PatientApiFixture fixture)
{
    private static readonly Guid ActorId = Guid.Parse("0199ffee-0000-7000-8000-000000000302");

    [Fact]
    public async Task Create_PersistsApprovedResultWithoutIdempotencyKey()
    {
        var seed = await SeedAsync();
        using var client = Client(seed.UnitId, ManagePermissions());
        var from = Today().AddDays(1);

        var response = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], from, null, true);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        var result = await Json(response);
        var linkId = result.RootElement.GetProperty("guardianLinkId").GetGuid();
        Assert.Equal(seed.PatientId, result.RootElement.GetProperty("patientId").GetGuid());
        Assert.Equal(seed.GuardianIds[0], result.RootElement.GetProperty("guardianPersonId").GetGuid());
        Assert.Equal(from, DateOnly.Parse(result.RootElement.GetProperty("effectiveFrom").GetString()!));
        Assert.Equal(JsonValueKind.Null, result.RootElement.GetProperty("effectiveTo").ValueKind);
        Assert.True(result.RootElement.GetProperty("isPrimary").GetBoolean());
        Assert.Equal(1, result.RootElement.GetProperty("version").GetInt32());
        var etag = result.RootElement.GetProperty("etag").GetString();
        Assert.Equal(etag, response.Headers.ETag?.ToString());
        AssertStrongEtag(etag);
        Assert.False(result.RootElement.TryGetProperty("cpf", out _));
        Assert.False(result.RootElement.TryGetProperty("birthDate", out _));

        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        var persisted = await db.GuardianLinks.SingleAsync(x => x.Id == linkId);
        Assert.Equal(from, persisted.EffectiveFrom);
        Assert.Equal(1, persisted.Version);
    }

    [Fact]
    public async Task Create_MapsValidationNotFoundAndPersonLifecycleErrors()
    {
        var seed = await SeedAsync();
        using var client = Client(seed.UnitId, ManagePermissions());
        var tomorrow = Today().AddDays(1);
        var invalidRoute = await client.PostAsJsonAsync("/api/v1/patients/not-a-uuid/guardians", new GuardianRequest(seed.GuardianIds[0], tomorrow, null, false));
        var unknownPatient = await CreateAsync(client, Guid.CreateVersion7(), seed.GuardianIds[0], tomorrow, null, false);
        var emptyGuardian = await CreateAsync(client, seed.PatientId, Guid.Empty, tomorrow, null, false);
        var unknownGuardian = await CreateAsync(client, seed.PatientId, Guid.CreateVersion7(), tomorrow, null, false);
        var inverted = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], tomorrow, tomorrow, false);
        var retroactive = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], Today().AddDays(-1), null, false);
        var malformed = await client.PostAsync($"/api/v1/patients/{seed.PatientId:D}/guardians", new StringContent("{", Encoding.UTF8, "application/json"));
        await MarkPersonInactiveAsync(seed.GuardianIds[1]);
        var nonCurrent = await CreateAsync(client, seed.PatientId, seed.GuardianIds[1], tomorrow, null, false);

        await AssertProblem(invalidRoute, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(unknownPatient, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertProblem(emptyGuardian, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(unknownGuardian, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertProblem(inverted, HttpStatusCode.UnprocessableEntity, "BUSINESS_RULE_VIOLATION");
        await AssertProblem(retroactive, HttpStatusCode.UnprocessableEntity, "RETROACTIVE_RELATIONSHIP_NOT_SUPPORTED");
        await AssertProblem(malformed, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(nonCurrent, HttpStatusCode.UnprocessableEntity, "BUSINESS_RULE_VIOLATION");
    }

    [Fact]
    public async Task Create_EnforcesOverlapPrimaryAndAllowsAdjacentAndZeroPrimary()
    {
        var seed = await SeedAsync();
        using var client = Client(seed.UnitId, ManagePermissions());
        var from = Today().AddDays(2);
        var until = from.AddDays(4);
        var first = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], from, until, false);
        var second = await CreateAsync(client, seed.PatientId, seed.GuardianIds[1], from.AddDays(1), until.AddDays(1), false);
        var sameOverlap = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], from.AddDays(1), null, false);
        var primary = await CreateAsync(client, seed.PatientId, seed.GuardianIds[2], from, until, true);
        var primaryOverlap = await CreateAsync(client, seed.PatientId, seed.GuardianIds[3], from.AddDays(1), until, true);
        var adjacent = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], until, until.AddDays(2), false);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        await AssertProblem(sameOverlap, HttpStatusCode.Conflict, "CONFLICT");
        Assert.Equal(HttpStatusCode.Created, primary.StatusCode);
        await AssertProblem(primaryOverlap, HttpStatusCode.Conflict, "CONFLICT");
        Assert.Equal(HttpStatusCode.Created, adjacent.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsMinimizedHistoryPagingAndHalfOpenEffectiveOn()
    {
        var seed = await SeedAsync();
        var historicalFrom = new DateOnly(2030, 1, 1);
        var futureFrom = new DateOnly(2030, 2, 1);
        await AddLinkAsync(seed.PatientId, seed.GuardianIds[0], historicalFrom, new DateOnly(2030, 1, 10), false);
        await AddLinkAsync(seed.PatientId, seed.GuardianIds[1], futureFrom, null, true);
        using var client = Client(seed.UnitId, ReadPermissions());

        var all = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&page=1&pageSize=1&sort=-effectiveFrom");
        var before = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&effectiveOn=2029-12-31");
        var atStart = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&effectiveOn=2030-01-01");
        var inside = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&effectiveOn=2030-01-09");
        var atEnd = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&effectiveOn=2030-01-10");
        var future = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&effectiveOn=2030-02-01");

        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        Assert.Contains("no-store", all.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        var page = await Json(all);
        Assert.Equal(2, page.RootElement.GetProperty("totalCount").GetInt32());
        var item = Assert.Single(page.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal("GUARDIAN", item.GetProperty("kind").GetString());
        Assert.Equal("FUTURE", item.GetProperty("temporalState").GetString());
        AssertStrongEtag(item.GetProperty("etag").GetString());
        Assert.Equal("Guardião Fictício 2", item.GetProperty("guardian").GetProperty("displayName").GetString());
        Assert.False(item.TryGetProperty("cpf", out _));
        Assert.False(item.TryGetProperty("phone", out _));
        Assert.False(item.TryGetProperty("birthDate", out _));
        Assert.Empty((await Json(before)).RootElement.GetProperty("items").EnumerateArray());
        Assert.Single((await Json(atStart)).RootElement.GetProperty("items").EnumerateArray());
        Assert.Single((await Json(inside)).RootElement.GetProperty("items").EnumerateArray());
        Assert.Empty((await Json(atEnd)).RootElement.GetProperty("items").EnumerateArray());
        Assert.Single((await Json(future)).RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task List_MapsEmptyInvalidAndNotFoundRequests()
    {
        var seed = await SeedAsync();
        using var client = Client(seed.UnitId, ReadPermissions());
        var empty = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN");
        var missingKind = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships");
        var otherKind = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=EMERGENCY_CONTACT");
        var invalidDate = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&effectiveOn=nope");
        var invalidPage = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&page=0");
        var unknownQuery = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN&other=1");
        var invalidRoute = await client.GetAsync("/api/v1/patients/not-a-uuid/relationships?kind=GUARDIAN");
        var missing = await client.GetAsync($"/api/v1/patients/{Guid.CreateVersion7():D}/relationships?kind=GUARDIAN");

        Assert.Equal(HttpStatusCode.OK, empty.StatusCode);
        Assert.Empty((await Json(empty)).RootElement.GetProperty("items").EnumerateArray());
        await AssertProblem(missingKind, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(otherKind, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(invalidDate, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(invalidPage, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(unknownQuery, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(invalidRoute, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(missing, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task End_UpdatesOnlyTheLinkAndPreservesHistory()
    {
        var seed = await SeedAsync();
        using var client = Client(seed.UnitId, ManagePermissions());
        var created = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], Today(), null, true);
        var createBody = await Json(created);
        var linkId = createBody.RootElement.GetProperty("guardianLinkId").GetGuid();
        var oldEtag = createBody.RootElement.GetProperty("etag").GetString()!;
        var effectiveTo = Today().AddDays(3);
        var ended = await EndAsync(client, seed.PatientId, linkId, effectiveTo, oldEtag);

        Assert.Equal(HttpStatusCode.OK, ended.StatusCode);
        var body = await Json(ended);
        Assert.Equal(seed.GuardianIds[0], body.RootElement.GetProperty("guardianPersonId").GetGuid());
        Assert.Equal(effectiveTo, DateOnly.Parse(body.RootElement.GetProperty("effectiveTo").GetString()!));
        Assert.Equal(2, body.RootElement.GetProperty("version").GetInt32());
        Assert.Equal(body.RootElement.GetProperty("etag").GetString(), ended.Headers.ETag?.ToString());
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        var persisted = await db.GuardianLinks.SingleAsync(x => x.Id == linkId);
        Assert.Equal(effectiveTo, persisted.EffectiveTo);
        Assert.Equal(2, persisted.Version);
        Assert.NotNull(persisted.EndedAt);
        var history = await client.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN");
        Assert.Single((await Json(history)).RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task End_EnforcesIfMatchAndLeavesRejectedRequestsUntouched()
    {
        var seed = await SeedAsync();
        using var client = Client(seed.UnitId, ManagePermissions());
        var created = await CreateAsync(client, seed.PatientId, seed.GuardianIds[0], Today(), null, false);
        var body = await Json(created);
        var linkId = body.RootElement.GetProperty("guardianLinkId").GetGuid();
        var etag = body.RootElement.GetProperty("etag").GetString()!;
        var absent = await EndAsync(client, seed.PatientId, linkId, Today().AddDays(2), null);
        var stale = await EndAsync(client, seed.PatientId, linkId, Today().AddDays(2), "\"guardian-stale-v1\"");
        var accepted = await EndAsync(client, seed.PatientId, linkId, Today().AddDays(2), etag);
        var repeated = await EndAsync(client, seed.PatientId, linkId, Today().AddDays(2), etag);

        await AssertProblem(absent, (HttpStatusCode)428, "PRECONDITION_REQUIRED");
        await AssertProblem(stale, HttpStatusCode.PreconditionFailed, "CONCURRENCY_CONFLICT");
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        await AssertProblem(repeated, HttpStatusCode.PreconditionFailed, "CONCURRENCY_CONFLICT");
        await using var scope = fixture.CreateScope();
        var persisted = await scope.ServiceProvider.GetRequiredService<PatientsDbContext>().GuardianLinks.SingleAsync(x => x.Id == linkId);
        Assert.Equal(2, persisted.Version);
    }

    [Fact]
    public async Task End_MapsValidationAndDoesNotExposeOtherPatientsLink()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();
        using var client = Client(first.UnitId, ManagePermissions());
        var created = await CreateAsync(client, first.PatientId, first.GuardianIds[0], Today(), null, false);
        var createdBody = await Json(created);
        var linkId = createdBody.RootElement.GetProperty("guardianLinkId").GetGuid();
        var etag = createdBody.RootElement.GetProperty("etag").GetString()!;
        var invalidPatient = await client.PostAsJsonAsync($"/api/v1/patients/bad/guardians/{linkId:D}/end", new EndRequest(Today().AddDays(1)));
        var invalidLink = await client.PostAsJsonAsync($"/api/v1/patients/{first.PatientId:D}/guardians/bad/end", new EndRequest(Today().AddDays(1)));
        var wrongPatient = await EndAsync(client, second.PatientId, linkId, Today().AddDays(1), etag);
        var invalidPeriod = await EndAsync(client, first.PatientId, linkId, Today(), etag);
        var retroactive = await EndAsync(client, first.PatientId, linkId, Today().AddDays(-1), etag);
        var malformed = await client.PostAsync($"/api/v1/patients/{first.PatientId:D}/guardians/{linkId:D}/end", new StringContent("{", Encoding.UTF8, "application/json"));

        await AssertProblem(invalidPatient, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(invalidLink, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
        await AssertProblem(wrongPatient, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertProblem(invalidPeriod, HttpStatusCode.UnprocessableEntity, "BUSINESS_RULE_VIOLATION");
        await AssertProblem(retroactive, HttpStatusCode.UnprocessableEntity, "RETROACTIVE_RELATIONSHIP_NOT_SUPPORTED");
        await AssertProblem(malformed, HttpStatusCode.BadRequest, "VALIDATION_ERROR");
    }

    [Fact]
    public async Task End_ProtectsActiveMinorCoverageAndAllowsContinuousAlternative()
    {
        var uncovered = await SeedAsync(new DateOnly(2015, 1, 1));
        using var client = Client(uncovered.UnitId, ManagePermissions());
        var created = await CreateAsync(client, uncovered.PatientId, uncovered.GuardianIds[0], Today(), null, false);
        var body = await Json(created);
        var denied = await EndAsync(client, uncovered.PatientId, body.RootElement.GetProperty("guardianLinkId").GetGuid(), Today().AddDays(1), body.RootElement.GetProperty("etag").GetString());
        await AssertProblem(denied, HttpStatusCode.UnprocessableEntity, "GUARDIAN_COVERAGE_REQUIRED");

        var covered = await SeedAsync(new DateOnly(2015, 1, 1));
        await AddLinkAsync(covered.PatientId, covered.GuardianIds[1], Today().AddDays(1), new DateOnly(2033, 1, 1), false);
        using var coveredClient = Client(covered.UnitId, ManagePermissions());
        var first = await CreateAsync(coveredClient, covered.PatientId, covered.GuardianIds[0], Today(), null, false);
        var firstBody = await Json(first);
        var allowed = await EndAsync(coveredClient, covered.PatientId, firstBody.RootElement.GetProperty("guardianLinkId").GetGuid(), Today().AddDays(1), firstBody.RootElement.GetProperty("etag").GetString());
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task AllEndpoints_RequireAuthenticationAuthorizationAndScopedAccess()
    {
        var seed = await SeedAsync();
        using var anonymous = fixture.CreateClient();
        var unauthenticatedCreate = await CreateAsync(anonymous, seed.PatientId, seed.GuardianIds[0], Today().AddDays(1), null, false);
        var unauthenticatedList = await anonymous.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN");
        var unauthenticatedEnd = await EndAsync(anonymous, seed.PatientId, Guid.CreateVersion7(), Today().AddDays(1), "\"guardian-x-v1\"");
        using var missingPermission = Client(seed.UnitId, "people.person.read");
        var forbiddenCreate = await CreateAsync(missingPermission, seed.PatientId, seed.GuardianIds[0], Today().AddDays(1), null, false);
        using var missingRead = Client(seed.UnitId, "patients.guardian.manage,people.person.read");
        var forbiddenListByPermission = await missingRead.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN");
        using var denied = Client(seed.UnitId, ManagePermissions(), explicitDeny: true);
        var forbiddenList = await denied.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN");
        using var inactive = Client(seed.UnitId, ManagePermissions(), accountStatus: "INACTIVE");
        var forbiddenEnd = await EndAsync(inactive, seed.PatientId, Guid.CreateVersion7(), Today().AddDays(1), "\"guardian-x-v1\"");
        using var authorized = Client(seed.UnitId, ManagePermissions());
        var created = await CreateAsync(authorized, seed.PatientId, seed.GuardianIds[0], Today(), null, false);
        var createdBody = await Json(created);
        var linkId = createdBody.RootElement.GetProperty("guardianLinkId").GetGuid();
        var etag = createdBody.RootElement.GetProperty("etag").GetString()!;
        using var otherUnit = Client(Guid.CreateVersion7(), ManagePermissions());
        var hiddenByScope = await CreateAsync(otherUnit, seed.PatientId, seed.GuardianIds[0], Today().AddDays(1), null, false);
        var listHiddenByScope = await otherUnit.GetAsync($"/api/v1/patients/{seed.PatientId:D}/relationships?kind=GUARDIAN");
        var endHiddenByScope = await EndAsync(otherUnit, seed.PatientId, linkId, Today().AddDays(1), etag);

        await AssertProblem(unauthenticatedCreate, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        await AssertProblem(unauthenticatedList, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        await AssertProblem(unauthenticatedEnd, HttpStatusCode.Unauthorized, "UNAUTHORIZED");
        await AssertProblem(forbiddenCreate, HttpStatusCode.Forbidden, "FORBIDDEN");
        await AssertProblem(forbiddenListByPermission, HttpStatusCode.Forbidden, "FORBIDDEN");
        await AssertProblem(forbiddenList, HttpStatusCode.Forbidden, "FORBIDDEN");
        await AssertProblem(forbiddenEnd, HttpStatusCode.Forbidden, "FORBIDDEN");
        // API-001 §27.1.5 deliberately hides cross-scope resources as not found.
        await AssertProblem(hiddenByScope, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertProblem(listHiddenByScope, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
        await AssertProblem(endHiddenByScope, HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND");
    }

    private async Task<GuardianSeed> SeedAsync(DateOnly? patientBirthDate = null)
    {
        await using var scope = fixture.CreateScope();
        var people = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        var patients = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        var patient = Person.Create("Paciente Fictício Guardian", patientBirthDate ?? new DateOnly(1980, 1, 1), null, new PhoneNumber("55", "11", "900000001"));
        var guardians = Enumerable.Range(1, 4).Select(i => Person.Create($"Guardião Fictício {i}", new DateOnly(1970 + i, 1, 1), null, new PhoneNumber("55", "11", $"90000000{i + 1}"))).ToArray();
        people.People.AddRange([patient, .. guardians]);
        await people.SaveChangesAsync();
        var profile = PatientProfile.Create(patient.Id, fixture.ActiveUnitId, new DateOnly(2026, 9, 28));
        patients.PatientProfiles.Add(profile);
        await patients.SaveChangesAsync();
        return new(profile.Id, fixture.ActiveUnitId, guardians.Select(x => x.Id).ToArray());
    }

    private async Task AddLinkAsync(Guid patientId, Guid guardianId, DateOnly from, DateOnly? to, bool primary)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PatientsDbContext>();
        db.GuardianLinks.Add(GuardianLink.Create(patientId, guardianId, from, to, primary, ActorId, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
    }

    private async Task MarkPersonInactiveAsync(Guid personId)
    {
        await using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PeopleDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE people.person SET record_state = 'INACTIVE' WHERE id = {personId}");
    }

    private HttpClient Client(Guid unitId, string permissions, bool explicitDeny = false, string accountStatus = "ACTIVE")
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Actor", ActorId.ToString("D"));
        client.DefaultRequestHeaders.Add("X-Test-Permissions", permissions);
        client.DefaultRequestHeaders.Add("X-Test-Units", unitId.ToString("D"));
        client.DefaultRequestHeaders.Add("X-Test-Account-Status", accountStatus);
        if (explicitDeny) client.DefaultRequestHeaders.Add("X-Test-Explicit-Deny", "true");
        return client;
    }

    private static Task<HttpResponseMessage> CreateAsync(HttpClient client, Guid patientId, Guid guardianId, DateOnly from, DateOnly? to, bool primary) =>
        client.PostAsJsonAsync($"/api/v1/patients/{patientId:D}/guardians", new GuardianRequest(guardianId, from, to, primary));

    private static Task<HttpResponseMessage> EndAsync(HttpClient client, Guid patientId, Guid linkId, DateOnly effectiveTo, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/patients/{patientId:D}/guardians/{linkId:D}/end") { Content = JsonContent.Create(new EndRequest(effectiveTo)) };
        if (etag is not null) request.Headers.TryAddWithoutValidation("If-Match", etag);
        return client.SendAsync(request);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
    private static string ManagePermissions() => "patients.guardian.manage,patients.profile.read,people.person.read";
    private static string ReadPermissions() => "patients.profile.read,people.person.read";

    private static async Task<JsonDocument> Json(HttpResponseMessage response) => await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.Ordinal);
        var problem = await Json(response);
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(code, problem.RootElement.GetProperty("code").GetString());
        Assert.StartsWith("https://api.fisiofit.example/problems/", problem.RootElement.GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
        Assert.DoesNotContain("Paciente Fictício", problem.RootElement.GetRawText(), StringComparison.Ordinal);
    }

    private static void AssertStrongEtag(string? etag)
    {
        Assert.False(string.IsNullOrWhiteSpace(etag));
        Assert.StartsWith("\"", etag);
        Assert.EndsWith("\"", etag);
        Assert.DoesNotContain("W/", etag, StringComparison.Ordinal);
    }

    private sealed record GuardianSeed(Guid PatientId, Guid UnitId, IReadOnlyList<Guid> GuardianIds);
    private sealed record GuardianRequest(Guid GuardianPersonId, DateOnly EffectiveFrom, DateOnly? EffectiveTo, bool IsPrimary);
    private sealed record EndRequest(DateOnly EffectiveTo);
}
