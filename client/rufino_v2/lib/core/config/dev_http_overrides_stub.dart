/// Stub implementation for platforms where `dart:io` is unavailable (web).
///
/// This file is imported via conditional import when compiling for the web
/// target. It provides a no-op [applyDevHttpOverrides] so that calling code
/// does not need platform checks.
void applyDevHttpOverrides() {
  // No-op on web — HttpOverrides is not available.
}
