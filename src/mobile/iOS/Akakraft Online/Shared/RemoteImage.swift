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
        else { return nil }

        urlCache.storeCachedResponse(CachedURLResponse(response: response, data: data), for: request)
        memory.setObject(image, forKey: url as NSURL)
        return image
    }

    func clear() {
        urlCache.removeAllCachedResponses()
        memory.removeAllObjects()
    }

    var diskUsage: Int { urlCache.currentDiskUsage }
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
