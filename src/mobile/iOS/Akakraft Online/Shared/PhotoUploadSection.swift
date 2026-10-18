import PhotosUI
import SwiftUI
import UIKit

/// Formularabschnitt zum Auswählen und Hochladen eines Fotos.
struct PhotoUploadSection: View {
    var title = "Foto"
    let uploadPath: String
    @Binding var imageUrl: String?
    var thumbnailUrl: Binding<String?>?

    @Environment(AppSession.self) private var session
    @State private var selection: PhotosPickerItem?
    @State private var isUploading = false
    @State private var errorMessage: String?

    var body: some View {
        Section(title) {
            if let url = imageUrl.asURL {
                RemoteImage(url: url)
                    .frame(height: 180)
                    .frame(maxWidth: .infinity)
                    .clipShape(.rect(cornerRadius: 12))
                    .listRowInsets(EdgeInsets(top: 8, leading: 8, bottom: 8, trailing: 8))
            }
            PhotosPicker(selection: $selection, matching: .images) {
                Label(imageUrl == nil ? "Foto auswählen" : "Foto ersetzen", systemImage: "photo.on.rectangle.angled")
            }
            .requiresOnline()
            if imageUrl != nil {
                Button("Foto entfernen", systemImage: "trash", role: .destructive) {
                    imageUrl = nil
                    thumbnailUrl?.wrappedValue = nil
                }
            }
            if isUploading {
                HStack {
                    ProgressView()
                    Text("Wird hochgeladen …").foregroundStyle(.secondary)
                }
            }
            if let errorMessage {
                Text(errorMessage).font(.footnote).foregroundStyle(.red)
            }
        }
        .onChange(of: selection) { _, item in
            guard item != nil else { return }
            Task { await upload() }
        }
    }

    private func upload() async {
        guard let selection else { return }
        isUploading = true
        errorMessage = nil
        defer {
            isUploading = false
            self.selection = nil
        }
        do {
            guard let data = try await selection.loadTransferable(type: Data.self),
                  let jpeg = ImageCompression.jpeg(from: data)
            else { throw APIError.transport("Das Bild konnte nicht gelesen werden.") }
            let response = try await session.client.upload(uploadPath, fileData: jpeg, fileName: "foto.jpg", mimeType: "image/jpeg")
            let result = try session.client.decode(ImageUploadResult.self, from: response)
            imageUrl = result.primaryURL
            thumbnailUrl?.wrappedValue = result.thumbnailUrl
        } catch {
            errorMessage = APIError.from(error).localizedDescription
        }
    }
}

enum ImageCompression {
    /// Verkleinert auf max. 2048 px Kantenlänge, damit Uploads unter dem 10-MB-Limit bleiben.
    static func jpeg(from data: Data, maxDimension: CGFloat = 2048) -> Data? {
        guard let image = UIImage(data: data) else { return nil }
        let largest = max(image.size.width, image.size.height)
        guard largest > maxDimension else { return image.jpegData(compressionQuality: 0.82) }
        let scale = maxDimension / largest
        let size = CGSize(width: image.size.width * scale, height: image.size.height * scale)
        let format = UIGraphicsImageRendererFormat.default()
        format.scale = 1
        let resized = UIGraphicsImageRenderer(size: size, format: format).image { _ in
            image.draw(in: CGRect(origin: .zero, size: size))
        }
        return resized.jpegData(compressionQuality: 0.82)
    }
}

extension Date {
    /// Kalendertag als 00:00 UTC – so speichert auch das Web-Frontend reine Datumswerte.
    var utcMidnightOfLocalDay: Date {
        let parts = Calendar.current.dateComponents([.year, .month, .day], from: self)
        var utc = Calendar(identifier: .gregorian)
        utc.timeZone = .gmt
        return utc.date(from: parts) ?? self
    }

    /// Gegenstück zu `utcMidnightOfLocalDay` für die Anzeige in DatePickern.
    var localDayFromUTCMidnight: Date {
        var utc = Calendar(identifier: .gregorian)
        utc.timeZone = .gmt
        let parts = utc.dateComponents([.year, .month, .day], from: self)
        return Calendar.current.date(from: parts) ?? self
    }
}
