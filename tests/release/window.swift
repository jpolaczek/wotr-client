import CoreGraphics
import Foundation

let processId = Int32(CommandLine.arguments[1])!
let windows = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID) as? [[String: Any]] ?? []
let matches = windows.filter {
    ($0[kCGWindowOwnerPID as String] as? Int32) == processId &&
    ($0[kCGWindowLayer as String] as? Int) == 0
}
let result = matches.compactMap { window -> [String: Any]? in
    guard let id = window[kCGWindowNumber as String],
          let bounds = window[kCGWindowBounds as String] else { return nil }
    return ["id": id, "bounds": bounds]
}
let data = try JSONSerialization.data(withJSONObject: result)
print(String(data: data, encoding: .utf8)!)
