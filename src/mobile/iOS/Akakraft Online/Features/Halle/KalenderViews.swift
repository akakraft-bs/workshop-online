import SwiftUI

struct KalenderView: View {
    @AppStorage("kalender.kind") private var kind: CalendarKind = .hallenbelegung

    var body: some View {
        CalendarEventsView(kind: kind)
            .id(kind)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .principal) {
                    Picker("Kalender", selection: $kind) {
                        ForEach(CalendarKind.allCases) { Text($0.title).tag($0) }
                    }
                    .pickerStyle(.segmented)
                    .frame(width: 280)
                }
            }
    }
}

/// Monatsliste der Termine eines Kalendertyps.
struct CalendarEventsView: View {
    let kind: CalendarKind

    @Environment(AppSession.self) private var session
    @State private var month = Calendar.current.dateInterval(of: .month, for: .now)?.start ?? .now
    @State private var events: Remote<[CalendarEvent]>
    @State private var configs = Remote<[CalendarConfig]>("calendar/configs")
    @State private var showCreate = false

    init(kind: CalendarKind) {
        self.kind = kind
        let start = Calendar.current.dateInterval(of: .month, for: .now)?.start ?? .now
        _events = State(initialValue: Remote("calendar/events", query: Self.query(for: start, kind: kind)))
    }

    private var writableCalendars: [CalendarConfig] {
        (configs.value ?? [])
            .filter { $0.calendarType == kind.rawValue && $0.canWrite(session.permissions) }
            .sorted { $0.sortOrder < $1.sortOrder }
    }

    var body: some View {
        RemoteContent(remote: events) { items in
            let days = grouped(items)
            List {
                if days.isEmpty {
                    ContentUnavailableView("Keine Termine", systemImage: "calendar",
                                           description: Text("Im \(month.formatted(.dateTime.month(.wide))) ist nichts eingetragen."))
                }
                ForEach(days, id: \.day) { group in
                    Section {
                        ForEach(group.events) { event in
                            NavigationLink(value: event) { CalendarEventRow(event: event) }
                        }
                    } header: {
                        Text(group.day.formatted(.dateTime.weekday(.wide).day().month(.wide)))
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: events.lastUpdated) }
            }
        }
        .safeAreaInset(edge: .top, spacing: 0) { monthBar }
        .refreshable { await reload() }
        .task { await reload() }
        .navigationTitle(kind.title)
        .navigationDestination(for: CalendarEvent.self) { event in
            CalendarEventDetailView(event: event, config: configs.value?.first { $0.googleCalendarId == event.calendarId }) {
                await events.load(session)
            }
        }
        .toolbar {
            if !writableCalendars.isEmpty {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Termin eintragen", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            CalendarEventFormView(event: nil, calendars: writableCalendars, kind: kind, suggestedDay: month) {
                await events.load(session)
            }
        }
    }

    private var monthBar: some View {
        HStack {
            Button("Vorheriger Monat", systemImage: "chevron.left") { shift(-1) }
                .labelStyle(.iconOnly)
            Spacer()
            Button {
                month = Calendar.current.dateInterval(of: .month, for: .now)?.start ?? .now
                Task { await reload() }
            } label: {
                Text(month.formatted(.dateTime.month(.wide).year()))
                    .font(.headline)
                    .foregroundStyle(.primary)
            }
            Spacer()
            Button("Nächster Monat", systemImage: "chevron.right") { shift(1) }
                .labelStyle(.iconOnly)
        }
        .buttonStyle(.glass)
        .padding(.horizontal)
        .padding(.vertical, 6)
    }

    private func shift(_ months: Int) {
        month = Calendar.current.date(byAdding: .month, value: months, to: month) ?? month
        Task { await reload() }
    }

    private func reload() async {
        async let list: Void = events.load(session, path: "calendar/events", query: Self.query(for: month, kind: kind))
        async let cfg: Void = configs.load(session)
        _ = await (list, cfg)
    }

    private static func query(for month: Date, kind: CalendarKind) -> [URLQueryItem] {
        let end = Calendar.current.date(byAdding: .month, value: 1, to: month) ?? month
        return [
            URLQueryItem(name: "from", value: DateParsing.string(from: month)),
            URLQueryItem(name: "to", value: DateParsing.string(from: end)),
            URLQueryItem(name: "type", value: kind.rawValue),
        ]
    }

    private func grouped(_ items: [CalendarEvent]) -> [(day: Date, events: [CalendarEvent])] {
        let dict = Dictionary(grouping: items.filter { $0.start != nil }) { ($0.start ?? .now).startOfDay }
        return dict.keys.sorted().map { day in
            (day, dict[day, default: []].sorted { ($0.isAllDay ? 0 : 1, $0.start ?? .now) < ($1.isAllDay ? 0 : 1, $1.start ?? .now) })
        }
    }
}

struct CalendarEventRow: View {
    let event: CalendarEvent

    var body: some View {
        HStack(spacing: 12) {
            RoundedRectangle(cornerRadius: 2)
                .fill(Color(hex: event.calendarColor) ?? .accentColor)
                .frame(width: 4)
            VStack(alignment: .leading, spacing: 3) {
                Text(event.title).font(.headline)
                HStack(spacing: 6) {
                    Text(event.timeText)
                    if let creator = event.creatorName.nonEmpty { Text("· \(creator)") }
                }
                .font(.subheadline)
                .foregroundStyle(.secondary)
                if let fahrzeug = event.fahrzeugLabel.nonEmpty {
                    Label(fahrzeug, systemImage: "car").font(.caption).foregroundStyle(.secondary)
                }
            }
        }
        .padding(.vertical, 2)
        .accessibilityElement(children: .combine)
    }
}

struct CalendarEventDetailView: View {
    let event: CalendarEvent
    let config: CalendarConfig?
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var showEdit = false
    @State private var confirmDelete = false
    @State private var action = ActionRunner()

    private var canWrite: Bool { config?.canWrite(session.permissions) ?? session.permissions.isPrivileged }

    var body: some View {
        List {
            Section {
                VStack(alignment: .leading, spacing: 8) {
                    HStack(spacing: 6) {
                        Circle().fill(Color(hex: event.calendarColor) ?? .accentColor).frame(width: 10, height: 10)
                        Text(event.calendarName).font(.subheadline).foregroundStyle(.secondary)
                    }
                    Text(event.title).font(.title2.bold())
                }
                .padding(.vertical, 4)
                if let start = event.start {
                    DetailRow(title: "Datum", value: start.formatted(.dateTime.weekday(.wide).day().month(.wide).year()), systemImage: "calendar")
                    DetailRow(title: "Zeit", value: event.timeText, systemImage: "clock")
                }
                if let location = event.location.nonEmpty {
                    DetailRow(title: "Ort", value: location, systemImage: "mappin.and.ellipse")
                }
                if let fahrzeug = event.fahrzeugLabel.nonEmpty {
                    DetailRow(title: "Fahrzeug", value: fahrzeug, systemImage: "car")
                }
                if let creator = event.creatorName.nonEmpty {
                    DetailRow(title: "Eingetragen von", value: creator, systemImage: "person")
                }
                if let url = event.url.asURL {
                    Link(destination: url) { Label(url.host() ?? url.absoluteString, systemImage: "link") }
                }
            }
            if let description = event.description.nonEmpty {
                Section("Beschreibung") { Text(description) }
            }
        }
        .navigationTitle("Termin")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if canWrite {
                ToolbarItem(placement: .topBarTrailing) {
                    Menu("Verwalten", systemImage: "ellipsis") {
                        Button("Bearbeiten", systemImage: "pencil") { showEdit = true }
                        Button("Löschen", systemImage: "trash", role: .destructive) { confirmDelete = true }
                    }
                    .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showEdit) {
            CalendarEventFormView(event: event, calendars: config.map { [$0] } ?? [], kind: CalendarKind(rawValue: config?.calendarType ?? "") ?? .hallenbelegung, suggestedDay: event.start ?? .now) {
                await onChange()
                dismiss()
            }
        }
        .confirmationDialog("Termin löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    let path = "calendar/events/\(event.calendarId)/\(event.id)"
                    if await action.run({ try await session.client.send(.delete, path) }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        }
        .actionAlert(action)
    }
}

struct CalendarEventFormView: View {
    let event: CalendarEvent?
    let calendars: [CalendarConfig]
    let kind: CalendarKind
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var calendarId: String
    @State private var title: String
    @State private var isAllDay: Bool
    @State private var start: Date
    @State private var end: Date
    @State private var location: String
    @State private var details: String
    @State private var link: String
    @State private var fahrzeugId: String?
    @State private var fahrzeuge = Remote<[Fahrzeug]>("fahrzeuge")
    @State private var action = ActionRunner()

    init(event: CalendarEvent?, calendars: [CalendarConfig], kind: CalendarKind, suggestedDay: Date, onSaved: @escaping () async -> Void) {
        self.event = event
        self.calendars = calendars
        self.kind = kind
        self.onSaved = onSaved
        let defaultStart: Date = {
            let base = Calendar.current.isDate(suggestedDay, equalTo: .now, toGranularity: .month) ? Date.now : suggestedDay
            return Calendar.current.date(bySettingHour: 18, minute: 0, second: 0, of: base) ?? base
        }()
        _calendarId = State(initialValue: event?.calendarId ?? calendars.first?.googleCalendarId ?? "")
        _title = State(initialValue: event?.title ?? "")
        _isAllDay = State(initialValue: event?.isAllDay ?? false)
        _start = State(initialValue: event?.start ?? defaultStart)
        _end = State(initialValue: event?.end ?? defaultStart.addingTimeInterval(3 * 3600))
        _location = State(initialValue: event?.location ?? "")
        _details = State(initialValue: event?.description ?? "")
        _link = State(initialValue: event?.url ?? "")
        _fahrzeugId = State(initialValue: event?.fahrzeugId?.lowercased())
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Titel", text: $title)
                    if event == nil && calendars.count > 1 {
                        Picker("Kalender", selection: $calendarId) {
                            ForEach(calendars) { Text($0.name).tag($0.googleCalendarId) }
                        }
                    }
                }
                Section {
                    Toggle("Ganztägig", isOn: $isAllDay)
                    DatePicker("Beginn", selection: $start, displayedComponents: isAllDay ? .date : [.date, .hourAndMinute])
                    DatePicker("Ende", selection: $end, in: start..., displayedComponents: isAllDay ? .date : [.date, .hourAndMinute])
                }
                if kind == .hallenbelegung || event?.fahrzeugId != nil {
                    Section("Fahrzeug") {
                        Picker("Fahrzeug", selection: $fahrzeugId) {
                            Text("Keins").tag(String?.none)
                            ForEach(fahrzeuge.value ?? []) { Text($0.label).tag(Optional($0.id.lowercased())) }
                        }
                    }
                }
                Section {
                    TextField("Ort", text: $location)
                    TextField("Link", text: $link).keyboardType(.URL).textInputAutocapitalization(.never)
                    TextField("Beschreibung", text: $details, axis: .vertical).lineLimit(3...8)
                }
            }
            .navigationTitle(event == nil ? "Neuer Termin" : "Termin bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .task { await fahrzeuge.load(session) }
            .onChange(of: start) { old, new in
                if end <= new { end = new.addingTimeInterval(max(end.timeIntervalSince(old), 3600)) }
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(title.trimmed.isEmpty || calendarId.isEmpty || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private func save() async {
        let startValue = isAllDay ? start.utcMidnightOfLocalDay : start
        let endValue = isAllDay
            ? (Calendar.current.date(byAdding: .day, value: 1, to: end.startOfDay) ?? end).utcMidnightOfLocalDay
            : end
        let input = CalendarEventInput(
            calendarId: event == nil ? calendarId : nil,
            title: title.trimmed, start: startValue, end: endValue, isAllDay: isAllDay,
            description: details.nonEmpty, location: location.nonEmpty, url: link.nonEmpty, fahrzeugId: fahrzeugId
        )
        let success = await action.run {
            if let event {
                try await session.client.send(.put, "calendar/events/\(event.calendarId)/\(event.id)", body: input)
            } else {
                try await session.client.send(.post, "calendar/events", body: input)
            }
        }
        if success {
            await onSaved()
            dismiss()
        }
    }
}
