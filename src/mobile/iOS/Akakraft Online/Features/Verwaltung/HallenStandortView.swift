import CoreLocation
import MapKit
import SwiftUI

/// Admin: Position der Halle für den Standort-Check-in (Geofence, 200 m Umkreis).
struct HallenStandortView: View {
    @Environment(AppSession.self) private var session
    @Environment(HallenGeofence.self) private var geofence
    @State private var remote = Remote<HallenStandort?>("halle/standort")
    @State private var coordinate: CLLocationCoordinate2D?
    @State private var latitude = ""
    @State private var longitude = ""
    @State private var camera: MapCameraPosition = .automatic
    @State private var locating = false
    @State private var confirmDelete = false
    @State private var action = ActionRunner()
    @State private var saved = false

    private static let radius: CLLocationDistance = 200

    var body: some View {
        Form {
            Section {
                MapReader { proxy in
                    Map(position: $camera) {
                        if let coordinate {
                            Marker("Halle", systemImage: "wrench.and.screwdriver", coordinate: coordinate)
                            MapCircle(center: coordinate, radius: Self.radius)
                                .foregroundStyle(.tint.opacity(0.2))
                                .stroke(.tint, lineWidth: 2)
                        }
                        UserAnnotation()
                    }
                    .mapControls { MapUserLocationButton() }
                    .onTapGesture { point in
                        if let tapped = proxy.convert(point, from: .local) { set(tapped, moveCamera: false) }
                    }
                }
                .frame(height: 300)
                .listRowInsets(EdgeInsets())
            } footer: {
                Text("Tippe auf die Karte, um die Halle zu markieren. Der Kreis zeigt den Umkreis von 200 m, in dem die Apps zum Einchecken fragen.")
            }

            Section("Koordinaten") {
                TextField("Breitengrad", text: $latitude)
                    .keyboardType(.numbersAndPunctuation)
                    .onSubmit(applyTypedCoordinates)
                TextField("Längengrad", text: $longitude)
                    .keyboardType(.numbersAndPunctuation)
                    .onSubmit(applyTypedCoordinates)
                Button {
                    Task { await useCurrentLocation() }
                } label: {
                    HStack {
                        Label("Aktuellen Standort übernehmen", systemImage: "location")
                        if locating {
                            Spacer()
                            ProgressView()
                        }
                    }
                }
                .disabled(locating)
            }

            if remote.value??.latitude != nil {
                Section {
                    Button("Standort entfernen", systemImage: "trash", role: .destructive) { confirmDelete = true }
                        .requiresOnline()
                } footer: {
                    Text("Ohne Standort ist der Check-in per Standort für alle aus.")
                }
            }
        }
        .navigationTitle("Hallen-Standort")
        .navigationBarTitleDisplayMode(.inline)
        .toolbar {
            ToolbarItem(placement: .confirmationAction) {
                Button(saved ? "Gesichert" : "Sichern", role: .confirm) { Task { await save() } }
                    .disabled(coordinate == nil || action.isRunning)
                    .requiresOnline()
            }
        }
        .task {
            await remote.load(session)
            if let standort = remote.value ?? nil {
                set(CLLocationCoordinate2D(latitude: standort.latitude, longitude: standort.longitude), moveCamera: true)
            }
        }
        .confirmationDialog("Standort entfernen?", isPresented: $confirmDelete, titleVisibility: .visible) {
            Button("Entfernen", role: .destructive) { Task { await delete() } }
        }
        .actionAlert(action)
    }

    private func set(_ new: CLLocationCoordinate2D, moveCamera: Bool) {
        coordinate = new
        latitude = new.latitude.formatted(.number.precision(.fractionLength(6)).grouping(.never).locale(Locale(identifier: "en_US_POSIX")))
        longitude = new.longitude.formatted(.number.precision(.fractionLength(6)).grouping(.never).locale(Locale(identifier: "en_US_POSIX")))
        saved = false
        if moveCamera {
            camera = .region(MKCoordinateRegion(center: new, latitudinalMeters: Self.radius * 5, longitudinalMeters: Self.radius * 5))
        }
    }

    private func applyTypedCoordinates() {
        let parse = { (text: String) in Double(text.trimmed.replacingOccurrences(of: ",", with: ".")) }
        guard let lat = parse(latitude), let lon = parse(longitude),
              (-90...90).contains(lat), (-180...180).contains(lon)
        else { return }
        set(CLLocationCoordinate2D(latitude: lat, longitude: lon), moveCamera: true)
    }

    private func useCurrentLocation() async {
        locating = true
        defer { locating = false }
        geofence.requestPermissions()
        do {
            for try await update in CLLocationUpdate.liveUpdates() {
                if let location = update.location, location.horizontalAccuracy <= 50 {
                    set(location.coordinate, moveCamera: true)
                    return
                }
                if update.authorizationDenied { return }
            }
        } catch {
            action.errorMessage = "Standort konnte nicht ermittelt werden."
        }
    }

    private func save() async {
        applyTypedCoordinates()
        guard let coordinate else { return }
        let body = HallenStandortInput(latitude: coordinate.latitude, longitude: coordinate.longitude)
        if await action.run({ try await session.client.send(.put, "halle/standort", body: body) }) {
            saved = true
            await remote.load(session)
            await geofence.refreshStandort()
        }
    }

    private func delete() async {
        if await action.run({ try await session.client.send(.delete, "halle/standort") }) {
            coordinate = nil
            latitude = ""
            longitude = ""
            await remote.load(session)
            await geofence.refreshStandort()
        }
    }
}

private nonisolated struct HallenStandortInput: Encodable, Sendable {
    let latitude: Double
    let longitude: Double
}
