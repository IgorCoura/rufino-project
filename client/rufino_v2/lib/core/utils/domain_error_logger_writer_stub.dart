/// No-op implementation for platforms without file I/O (web).
void appendLogEntry(Map<String, dynamic> entry) {
  // File writing is not available on web — domain errors are only
  // logged to a JSON file on desktop/mobile dev builds.
}
