import Foundation
import Testing
@testable import Akakraft_Online

struct DateParsingTests {
    @Test(arguments: [
        "2026-10-14T12:34:56.1234567Z",
        "2026-10-14T12:34:56.123Z",
        "2026-10-14T12:34:56Z",
        "2026-10-14T12:34:56.1234567",
        "2026-10-14T14:34:56.1234567+02:00",
    ])
    func parsesDotNetFormats(_ raw: String) throws {
        let date = try #require(DateParsing.parse(raw))
        let utc = Calendar(identifier: .gregorian).dateComponents(in: .gmt, from: date)
        #expect(utc.year == 2026)
        #expect(utc.month == 10)
        #expect(utc.day == 14)
        #expect(utc.hour == 12)
        #expect(utc.minute == 34)
    }

    @Test func roundTripsThroughEncoder() throws {
        struct Payload: Codable { let at: Date }
        let original = Payload(at: Date(timeIntervalSince1970: 1_800_000_000.25))
        let data = try JSONCoding.makeEncoder().encode(original)
        let decoded = try JSONCoding.makeDecoder().decode(Payload.self, from: data)
        #expect(abs(decoded.at.timeIntervalSince(original.at)) < 0.001)
    }

    @Test func rejectsGarbage() {
        #expect(DateParsing.parse("gestern") == nil)
    }
}

struct ServerConfigurationTests {
    @Test func defaultServerUsesApiPath() {
        #expect(ServerConfiguration.akakraft.apiBaseURL.absoluteString == "https://app.akakraft-bs.de/api")
        #expect(ServerConfiguration.akakraft.webURL(route: "admin/kalender").absoluteString == "https://app.akakraft-bs.de/app/admin/kalender")
    }

    @Test func normalizesUserInput() throws {
        let server = try #require(ServerConfiguration.fromUserInput("  verein.example.org/irgendwas "))
        #expect(server.baseURL.absoluteString == "https://verein.example.org")
        #expect(server.id == ServerConfiguration.fromUserInput("HTTPS://Verein.Example.org")?.id)
    }

    @Test func localServerHasNoApiPrefix() throws {
        #expect(ServerConfiguration.local.apiBaseURL.absoluteString == "http://localhost:5153")
        let typed = try #require(ServerConfiguration.fromUserInput("http://localhost:5153"))
        #expect(typed == ServerConfiguration.local)
        #expect(typed.displayName == "localhost:5153")
        #expect(APIClient(server: .local, isOnline: { true }).url(for: "werkzeug").absoluteString == "http://localhost:5153/werkzeug")
    }

    @Test func debugBuildDefaultsToLocalServer() {
        #if DEBUG
        #expect(ServerConfiguration.standard == .local)
        #else
        #expect(ServerConfiguration.standard == .akakraft)
        #endif
    }

    @Test func rejectsInvalidInput() {
        #expect(ServerConfiguration.fromUserInput("") == nil)
        #expect(ServerConfiguration.fromUserInput("ftp://host") == nil)
    }
}

struct PermissionsTests {
    private func user(_ roles: [Role], id: String = "ABC") -> User {
        User(id: id, email: "a@b.de", name: "Test", displayName: nil, pictureUrl: nil, createdAt: .now, roles: roles)
    }

    @Test func memberIsNotPrivileged() {
        let permissions = Permissions(user: user([.member]))
        #expect(permissions.isMember)
        #expect(!permissions.isPrivileged)
        #expect(!permissions.canSeeVorstandsbereich)
    }

    @Test func noneHasNoAccess() {
        #expect(!Permissions(user: user([.none])).isMember)
    }

    @Test(arguments: [Role.hallenwart, .treasurer, .chairman, .moderator, .admin])
    func privilegedRoles(_ role: Role) {
        #expect(Permissions(user: user([role])).isPrivileged)
    }

    @Test func moderatorDoesNotSeeVorstandsbereich() {
        #expect(!Permissions(user: user([.moderator])).canSeeVorstandsbereich)
    }

    @Test func ownershipIgnoresGuidCase() {
        let permissions = Permissions(user: user([.member], id: "1A2B"))
        #expect(permissions.isOwner("1a2b"))
        #expect(!permissions.isOwner(nil))
        #expect(permissions.isOwnerOrPrivileged("1A2B"))
        #expect(!permissions.isOwnerOrPrivileged("other"))
    }

    @Test func calendarWriteRoles() {
        let config = CalendarConfig(
            id: "1", googleCalendarId: "cal", name: "Halle", color: "#ff0000", isVisible: true, sortOrder: 0,
            calendarType: "Hallenbelegung", grantsParkplatzBerechtigung: false, writeRoles: ["Member"]
        )
        #expect(config.canWrite(Permissions(user: user([.member]))))
        #expect(!config.canWrite(Permissions(user: user([.none]))))
    }
}

struct DecodingTests {
    @Test func unknownEnumValuesDoNotBreakDecoding() throws {
        let json = Data(#"["Offen","IrgendwasNeues"]"#.utf8)
        let decoded = try JSONCoding.makeDecoder().decode([MangelStatus].self, from: json)
        #expect(decoded == [.offen, .unknown])
    }

    @Test func decodesWerkzeugFromApi() throws {
        let json = Data("""
        {"id":"7f1c","name":"Akkuschrauber","description":"18V","category":"Elektro","imageUrl":null,
         "thumbnailUrl":null,"dimensions":null,"storageLocation":"Regal 1","isAvailable":false,
         "borrowedByUserId":"u1","borrowedByName":"Alex","borrowedAt":"2026-10-01T08:00:00.123456Z",
         "expectedReturnAt":"2026-10-05T00:00:00Z","returnedAt":null,"createdAt":"2026-01-01T00:00:00Z",
         "anleitungDokumentId":null,"anleitungFileName":null,"anleitungFileUrl":null}
        """.utf8)
        let werkzeug = try JSONCoding.makeDecoder().decode(Werkzeug.self, from: json)
        #expect(werkzeug.name == "Akkuschrauber")
        #expect(!werkzeug.isAvailable)
        #expect(werkzeug.isOverdue)
    }
}

struct PKCETests {
    /// Referenzwert unabhängig berechnet: base64url(sha256(verifier)) ohne Padding.
    @Test func matchesIndependentS256Computation() {
        let pkce = PKCE(verifier: "dBjftJeZ4CVP-mB92K9uhvYYxW7Ux7G8Y6RjKZkOJRo")
        #expect(pkce.challenge == "I_TtvbLuE3KeUZwf-eHxhGU1utkiJuRlxVwJDtfkrN8")
    }

    @Test func randomChallengeHasBackendFormat() {
        let pkce = PKCE()
        #expect(pkce.challenge.count == 43)
        #expect(pkce.verifier.count == 43)
        #expect(pkce.challenge.allSatisfy { $0.isLetter || $0.isNumber || $0 == "-" || $0 == "_" })
    }

    @Test func googleLoginURLCarriesClientAndChallenge() throws {
        let client = APIClient(server: .akakraft, isOnline: { true })
        let url = client.googleLoginURL(challenge: "abc")
        #expect(url.absoluteString == "https://app.akakraft-bs.de/api/auth/login/google?client=ios&code_challenge=abc")
    }
}

struct GreetingTests {
    private func date(day: Int, hour: Int, _ calendar: Calendar) throws -> Date {
        try #require(calendar.date(from: DateComponents(year: 2026, month: 10, day: day, hour: hour, minute: 30)))
    }

    private var calendar: Calendar {
        var calendar = Calendar(identifier: .gregorian)
        calendar.timeZone = .gmt
        return calendar
    }

    @Test(arguments: [(4, 3), (5, 0), (10, 0), (11, 1), (17, 1), (18, 2), (22, 2), (23, 3), (0, 3)])
    func salutationMatchesTimeOfDay(_ hour: Int, _ period: Int) throws {
        let texts = period < 3 ? DashboardView.salutations[period].texts : DashboardView.nightSalutations
        #expect(texts.contains(DashboardView.salutation(at: try date(day: 15, hour: hour, calendar), calendar: calendar)))
    }

    @Test func salutationIsStableWithinADayAndVariesAcrossDays() throws {
        let morning = try date(day: 15, hour: 6, calendar)
        let later = try date(day: 15, hour: 9, calendar)
        #expect(DashboardView.salutation(at: morning, calendar: calendar) == DashboardView.salutation(at: later, calendar: calendar))
        let week = try (15..<19).map { DashboardView.salutation(at: try date(day: $0, hour: 6, calendar), calendar: calendar) }
        #expect(Set(week).count > 1)
    }

    @Test func noMahlzeit() {
        let all = DashboardView.salutations.flatMap(\.texts) + DashboardView.nightSalutations
        #expect(!all.contains("Mahlzeit"))
    }
}
