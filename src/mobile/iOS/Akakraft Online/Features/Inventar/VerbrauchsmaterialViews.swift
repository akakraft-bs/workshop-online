import SwiftUI

struct VerbrauchsmaterialListView: View {
    @Environment(AppSession.self) private var session
    @State private var remote = Remote<[Verbrauchsmaterial]>("verbrauchsmaterial")
    @State private var locations = Remote<[StorageLocation]>("storage-locations")
    @State private var searchText = ""
    @State private var category: String?
    @State private var onlyLowStock = false
    @State private var showCreate = false

    var body: some View {
        RemoteContent(remote: remote) { items in
            let visible = filtered(items)
            List {
                if visible.isEmpty {
                    if searchText.isEmpty {
                        ContentUnavailableView("Kein Material", systemImage: "shippingbox")
                    } else {
                        ContentUnavailableView.search(text: searchText)
                    }
                }
                ForEach(visible) { item in
                    NavigationLink(value: item) {
                        MaterialRow(item: item, locationColor: colorMap.color(for: item.storageLocation))
                    }
                }
                Section {} footer: { LastUpdatedFooter(date: remote.lastUpdated) }
            }
            .listStyle(.plain)
        }
        .searchable(text: $searchText, prompt: "Material suchen")
        .refreshable { await reload() }
        .task { await reload() }
        .navigationDestination(for: Verbrauchsmaterial.self) { item in
            VerbrauchsmaterialDetailView(item: item, locationColor: colorMap.color(for: item.storageLocation)) {
                await remote.load(session)
            }
        }
        .toolbar {
            ToolbarItem(placement: .topBarTrailing) {
                Menu {
                    Toggle("Bestand niedrig", systemImage: "exclamationmark.triangle", isOn: $onlyLowStock)
                    Picker("Kategorie", selection: $category) {
                        Text("Alle Kategorien").tag(String?.none)
                        ForEach(categories, id: \.self) { Text($0).tag(Optional($0)) }
                    }
                    .pickerStyle(.menu)
                } label: {
                    Label("Filter", systemImage: onlyLowStock || category != nil
                          ? "line.3.horizontal.decrease.circle.fill"
                          : "line.3.horizontal.decrease.circle")
                }
            }
            if session.permissions.isPrivileged {
                ToolbarItem(placement: .topBarTrailing) {
                    Button("Neues Material", systemImage: "plus") { showCreate = true }
                        .requiresOnline()
                }
            }
        }
        .sheet(isPresented: $showCreate) {
            VerbrauchsmaterialFormView(item: nil, categories: categories, locations: locations.value ?? []) {
                await remote.load(session)
            }
        }
    }

    private var colorMap: [String: Color] { (locations.value ?? []).colorMap }
    private var categories: [String] { Array(Set((remote.value ?? []).map(\.category))).sorted() }

    private func filtered(_ items: [Verbrauchsmaterial]) -> [Verbrauchsmaterial] {
        let query = searchText.trimmed.lowercased()
        return items
            .filter { !onlyLowStock || $0.isLowStock }
            .filter { category == nil || $0.category == category }
            .filter {
                query.isEmpty || $0.name.lowercased().contains(query)
                    || $0.description.lowercased().contains(query) || $0.category.lowercased().contains(query)
            }
            .sorted { $0.name.localizedStandardCompare($1.name) == .orderedAscending }
    }

    private func reload() async {
        async let items: Void = remote.load(session)
        async let places: Void = locations.load(session)
        _ = await (items, places)
    }
}

struct MaterialRow: View {
    let item: Verbrauchsmaterial
    let locationColor: Color?

    var body: some View {
        HStack(spacing: 12) {
            RemoteImage(url: (item.thumbnailUrl ?? item.imageUrl).asURL, placeholderSymbol: "shippingbox")
                .frame(width: 56, height: 56)
                .clipShape(.rect(cornerRadius: 10))
            VStack(alignment: .leading, spacing: 3) {
                Text(item.name).font(.headline).lineLimit(1)
                HStack(spacing: 8) {
                    Text(item.category)
                    if let location = item.storageLocation.nonEmpty {
                        StorageLocationLabel(name: location, color: locationColor)
                    }
                }
                .font(.subheadline)
                .foregroundStyle(.secondary)
                .lineLimit(1)
                if item.isNachbestellt {
                    StatusPill(text: "Nachbestellt", color: .blue, systemImage: "shippingbox.and.arrow.backward")
                }
            }
            Spacer()
            VStack(alignment: .trailing) {
                Text("\(item.quantity)")
                    .font(.title3.monospacedDigit().bold())
                    .foregroundStyle(item.isLowStock ? .red : .primary)
                Text(item.unit).font(.caption).foregroundStyle(.secondary)
            }
        }
        .accessibilityElement(children: .combine)
    }
}

struct VerbrauchsmaterialDetailView: View {
    let locationColor: Color?
    let onChange: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var item: Verbrauchsmaterial
    @State private var action = ActionRunner()
    @State private var showEdit = false
    @State private var confirmDelete = false

    init(item: Verbrauchsmaterial, locationColor: Color?, onChange: @escaping () async -> Void) {
        self.locationColor = locationColor
        self.onChange = onChange
        _item = State(initialValue: item)
    }

    private var canManage: Bool { session.permissions.isPrivileged }

    var body: some View {
        List {
            Section {
                RemoteImage(url: item.imageUrl.asURL, contentMode: .fit, placeholderSymbol: "shippingbox")
                    .frame(maxWidth: .infinity, minHeight: 200, maxHeight: 320)
                    .listRowInsets(EdgeInsets())
            }

            Section {
                VStack(alignment: .leading, spacing: 6) {
                    Text(item.name).font(.title2.bold())
                    Text(item.category).foregroundStyle(.secondary)
                    if let description = item.description.nonEmpty { Text(description).padding(.top, 4) }
                }
                .padding(.vertical, 4)
                if let location = item.storageLocation.nonEmpty {
                    LabeledContent {
                        StorageLocationLabel(name: location, color: locationColor)
                    } label: {
                        Label("Ablageort", systemImage: "mappin.and.ellipse")
                    }
                }
            }

            Section("Bestand") {
                HStack {
                    if canManage {
                        Button("Weniger", systemImage: "minus") { Task { await adjust(-1) } }
                            .labelStyle(.iconOnly)
                            .buttonStyle(.glass)
                            .disabled(item.quantity <= 0 || action.isRunning)
                            .requiresOnline()
                    }
                    Spacer()
                    VStack {
                        Text("\(item.quantity)")
                            .font(.largeTitle.monospacedDigit().bold())
                            .foregroundStyle(item.isLowStock ? .red : .primary)
                            .contentTransition(.numericText())
                        Text(item.unit).foregroundStyle(.secondary)
                    }
                    Spacer()
                    if canManage {
                        Button("Mehr", systemImage: "plus") { Task { await adjust(1) } }
                            .labelStyle(.iconOnly)
                            .buttonStyle(.glass)
                            .disabled(action.isRunning)
                            .requiresOnline()
                    }
                }
                .padding(.vertical, 4)

                if let min = item.minQuantity {
                    DetailRow(title: "Mindestbestand", value: "\(min) \(item.unit)", systemImage: "gauge.with.dots.needle.33percent")
                }
                if item.isLowStock {
                    Label("Bestand niedrig", systemImage: "exclamationmark.triangle.fill").foregroundStyle(.red)
                }
            }

            Section("Nachbestellung") {
                if item.isNachbestellt {
                    Label {
                        Text("Nachbestellt\(item.nachbestelltVonName.map { " von \($0)" } ?? "")\(item.nachbestelltAt.map { " am \($0.dayString)" } ?? "")")
                    } icon: {
                        Image(systemName: "shippingbox.and.arrow.backward.fill").foregroundStyle(.blue)
                    }
                } else if canManage {
                    Button("Als nachbestellt markieren", systemImage: "cart.badge.plus") {
                        Task { await markOrdered() }
                    }
                    .requiresOnline()
                } else {
                    Text("Noch nicht nachbestellt.").foregroundStyle(.secondary)
                }
            }
        }
        .navigationTitle(item.name)
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            if canManage {
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
            VerbrauchsmaterialFormView(item: item, categories: [item.category], locations: []) {
                await onChange()
                dismiss()
            }
        }
        .confirmationDialog("„\(item.name)“ löschen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Löschen", role: .destructive) {
                Task {
                    if await action.run({ try await session.client.send(.delete, "verbrauchsmaterial/\(item.id)") }) {
                        await onChange()
                        dismiss()
                    }
                }
            }
        }
        .actionAlert(action)
    }

    private struct DeltaBody: Encodable { let delta: Int }

    private func adjust(_ delta: Int) async {
        await action.run {
            let updated = try await session.client.send(
                .patch, "verbrauchsmaterial/\(item.id)/quantity",
                body: DeltaBody(delta: delta), decoding: Verbrauchsmaterial.self
            )
            withAnimation { item = updated }
        }
        await onChange()
    }

    private func markOrdered() async {
        await action.run {
            try await session.client.send(.post, "verbrauchsmaterial/\(item.id)/nachbestellen")
            item.isNachbestellt = true
            item.nachbestelltVonName = session.user?.shownName
            item.nachbestelltAt = .now
        }
        await onChange()
    }
}

struct VerbrauchsmaterialFormView: View {
    let item: Verbrauchsmaterial?
    let categories: [String]
    let locations: [StorageLocation]
    let onSaved: () async -> Void

    @Environment(AppSession.self) private var session
    @Environment(\.dismiss) private var dismiss
    @State private var input: VerbrauchsmaterialInput
    @State private var hasMinimum: Bool
    @State private var action = ActionRunner()
    @State private var units = Remote<[String]>("verbrauchsmaterial/units")
    @State private var allLocations = Remote<[StorageLocation]>("storage-locations")

    init(item: Verbrauchsmaterial?, categories: [String], locations: [StorageLocation], onSaved: @escaping () async -> Void) {
        self.item = item
        self.categories = categories
        self.locations = locations
        self.onSaved = onSaved
        _input = State(initialValue: VerbrauchsmaterialInput(
            name: item?.name ?? "", description: item?.description ?? "", category: item?.category ?? "",
            unit: item?.unit ?? "Stück", quantity: item?.quantity ?? 0, minQuantity: item?.minQuantity,
            imageUrl: item?.imageUrl, thumbnailUrl: item?.thumbnailUrl, storageLocation: item?.storageLocation
        ))
        _hasMinimum = State(initialValue: item?.minQuantity != nil)
    }

    var body: some View {
        NavigationStack {
            Form {
                Section {
                    TextField("Name", text: $input.name)
                    SuggestingTextField(title: "Kategorie", text: $input.category, suggestions: categories)
                    TextField("Beschreibung", text: $input.description, axis: .vertical).lineLimit(3...8)
                }
                Section("Bestand") {
                    Stepper("Menge: \(input.quantity)", value: $input.quantity, in: 0...100_000)
                    SuggestingTextField(title: "Einheit", text: $input.unit, suggestions: units.value ?? [])
                    Toggle("Mindestbestand", isOn: $hasMinimum)
                    if hasMinimum {
                        Stepper("Mindestens: \(input.minQuantity ?? 0)", value: Binding(
                            get: { input.minQuantity ?? 0 }, set: { input.minQuantity = $0 }
                        ), in: 0...100_000)
                    }
                }
                Section {
                    SuggestingTextField(title: "Ablageort", text: optional($input.storageLocation),
                                        suggestions: (allLocations.value ?? locations).map(\.name))
                }
                PhotoUploadSection(uploadPath: "uploads/verbrauchsmaterial", imageUrl: $input.imageUrl, thumbnailUrl: $input.thumbnailUrl)
            }
            .navigationTitle(item == nil ? "Neues Material" : "Material bearbeiten")
            .navigationBarTitleDisplayMode(.inline)
            .task {
                await units.load(session)
                await allLocations.load(session)
            }
            .toolbar {
                ToolbarItem(placement: .cancellationAction) { Button("Abbrechen", role: .cancel) { dismiss() } }
                ToolbarItem(placement: .confirmationAction) {
                    Button("Sichern", role: .confirm) { Task { await save() } }
                        .disabled(input.name.trimmed.isEmpty || input.category.trimmed.isEmpty || input.unit.trimmed.isEmpty || action.isRunning)
                        .requiresOnline()
                }
            }
            .actionAlert(action)
        }
    }

    private func save() async {
        var payload = input
        if !hasMinimum { payload.minQuantity = nil }
        payload.storageLocation = input.storageLocation.nonEmpty
        let success = await action.run {
            if let item {
                try await session.client.send(.put, "verbrauchsmaterial/\(item.id)", body: payload)
            } else {
                try await session.client.send(.post, "verbrauchsmaterial", body: payload)
            }
        }
        if success {
            await onSaved()
            dismiss()
        }
    }
}
