import 'package:uuid/uuid.dart';

const _uuid = Uuid();

/// Returns a new v4 GUID for use as the `x-requestid` header value.
///
/// The backend requires this header on mutation endpoints (POST/PUT) and
/// parses it as a `Guid` to deduplicate idempotent commands.
String newRequestId() => _uuid.v4();
