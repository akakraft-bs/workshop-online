import CryptoKit
import ImageIO
import SwiftUI
import UIKit

/// Lädt Bilder über einen Disk-Cache, damit bereits gesehene Bilder auch offline erscheinen.
final class ImagePipeline {
    static let shared = ImagePipeline()

    private let session: URLSession
    private let urlCache: URLCache
    private let memory = NSCache<NSURL, UIImage>()

    private init() {
        let directory = FileManager.default.urls(for: .cachesDirectory, in: .userDomainMask)[0]
            .appending(path: "images", directoryHint: .isDirectory)
        urlCache = URLCache(memoryCapacity: 30_000_000, diskCapacity: 400_000_000, directory: directory)
        let configuration = URLSessionConfiguration.default
        configuration.urlCache = urlCache
        configuration.httpCookieAcceptPolicy = .never
        session = URLSession(configuration: configuration)
    }

    func image(for url: URL, offline: Bool) async -> UIImage? {
        if let cached = memory.object(forKey: url as NSURL) { return cached }
        var request = URLRequest(url: url)
        request.cachePolicy = offline ? .returnCacheDataDontLoad : .returnCacheDataElseLoad

        if let cached = urlCache.cachedResponse(for: request), let image = UIImage(data: cached.data) {
            memory.setObject(image, forKey: url as NSURL)
            return image
        }
        guard !offline, let (data, response) = try? await session.data(for: request),
              (response as? HTTPURLResponse)?.statusCode == 200,
              let image = UIImage(data: data)
        else { return previews.image(for: url) }

        urlCache.storeCachedResponse(CachedURLResponse(response: response, data: data), for: request)
        memory.setObject(image, forKey: url as NSURL)
        return image
    }

    /// Lädt ein Bild herunter und legt es verkleinert für die Offline-Ansicht ab.
    /// `keys` sind alle URLs, unter denen das Bild angezeigt wird (z. B. Vorschau- und Originalbild).
    func storePreview(from source: URL, for keys: [URL]) async {
        guard !previews.contains(all: keys),
              let (data, response) = try? await session.data(from: source),
              (response as? HTTPURLResponse)?.statusCode == 200
        else { return }
        await previews.save(data, for: keys)
    }

    func clear() {
        urlCache.removeAllCachedResponses()
        memory.removeAllObjects()
        previews.removeAll()
    }

    var diskUsage: Int { urlCache.currentDiskUsage + previews.diskUsage }

    private let previews = PreviewImageStore()
}

/// Verkleinerte Bilder, die beim Vorladen gespeichert werden. Anders als der
/// URL-Cache werden sie nicht verdrängt, damit Listen und Details offline Bilder zeigen.
nonisolated struct PreviewImageStore: Sendable {
    static let maxPixelSize = 600

    private let directory: URL

    init() {
        directory = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appending(path: "OfflinePreviews", directoryHint: .isDirectory)
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        var values = URLResourceValues()
        values.isExcludedFromBackup = true
        var excluded = directory
        try? excluded.setResourceValues(values)
    }

    func image(for url: URL) -> UIImage? {
        UIImage(contentsOfFile: file(for: url).path())
    }

    func contains(all urls: [URL]) -> Bool {
        urls.allSatisfy { FileManager.default.fileExists(atPath: file(for: $0).path()) }
    }

    @concurrent
    func save(_ data: Data, for urls: [URL]) async {
        guard let source = CGImageSourceCreateWithData(data as CFData, nil),
              let thumbnail = CGImageSourceCreateThumbnailAtIndex(source, 0, [
                  kCGImageSourceCreateThumbnailFromImageAlways: true,
                  kCGImageSourceCreateThumbnailWithTransform: true,
                  kCGImageSourceThumbnailMaxPixelSize: Self.maxPixelSize,
              ] as CFDictionary),
              let jpeg = UIImage(cgImage: thumbnail).jpegData(compressionQuality: 0.7)
        else { return }
        for url in urls {
            try? jpeg.write(to: file(for: url), options: .atomic)
        }
    }

    func removeAll() {
        try? FileManager.default.removeItem(at: directory)
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
    }

    var diskUsage: Int {
        let files = (try? FileManager.default.contentsOfDirectory(at: directory, includingPropertiesForKeys: [.fileSizeKey])) ?? []
        return files.reduce(0) { $0 + ((try? $1.resourceValues(forKeys: [.fileSizeKey]).fileSize) ?? 0) }
    }

    private func file(for url: URL) -> URL {
        let digest = SHA256.hash(data: Data(url.absoluteString.utf8))
        return directory.appending(path: digest.map { String(format: "%02x", $0) }.joined() + ".jpg")
    }
}

struct RemoteImage: View {
    let url: URL?
    var contentMode: ContentMode = .fill
    var placeholderSymbol = "photo"

    @Environment(ConnectivityMonitor.self) private var connectivity
    @State private var image: UIImage?
    @State private var finished = false

    var body: some View {
        ZStack {
            if let image {
                Image(uiImage: image)
                    .resizable()
                    .aspectRatio(contentMode: contentMode)
            } else {
                Rectangle()
                    .fill(.quaternary)
                    .overlay {
                        if finished || url == nil {
                            Image(systemName: placeholderSymbol)
                                .font(.title2)
                                .foregroundStyle(.tertiary)
                        } else {
                            ProgressView()
                        }
                    }
            }
        }
        .task(id: url) {
            finished = false
            guard let url else {
                image = nil
                finished = true
                return
            }
            image = await ImagePipeline.shared.image(for: url, offline: !connectivity.isOnline)
            finished = true
        }
    }
}

struct AvatarView: View {
    let name: String
    var pictureURL: URL?
    var size: CGFloat = 36

    var body: some View {
        Group {
            if let pictureURL {
                RemoteImage(url: pictureURL, placeholderSymbol: "person.fill")
            } else {
                Text(initials)
                    .font(.system(size: size * 0.4, weight: .semibold, design: .rounded))
                    .foregroundStyle(.white)
                    .frame(maxWidth: .infinity, maxHeight: .infinity)
                    .background(Color.accentColor.gradient)
            }
        }
        .frame(width: size, height: size)
        .clipShape(.circle)
        .accessibilityHidden(true)
    }

    private var initials: String {
        name.split(separator: " ").prefix(2).compactMap(\.first).map(String.init).joined().uppercased()
    }
}
