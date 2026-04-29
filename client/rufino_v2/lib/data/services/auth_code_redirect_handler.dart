import 'auth_code_redirect_handler_stub.dart'
    if (dart.library.js_interop) 'auth_code_redirect_handler_web.dart'
    as impl;
import 'pending_web_redirect_result.dart';

export 'pending_web_redirect_result.dart';

/// Detects an in-flight web Authorization Code redirect and completes
/// it. Returns [PendingWebRedirectResult.none] on every non-Web platform.
///
/// Call from `main()` before `runApp`.
Future<PendingWebRedirectResult> completePendingAuthCodeRedirect() =>
    impl.completePendingAuthCodeRedirect();
