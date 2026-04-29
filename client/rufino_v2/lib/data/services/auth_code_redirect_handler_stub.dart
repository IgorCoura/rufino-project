import 'pending_web_redirect_result.dart';

/// IO-platform stub — no web redirect ever happens, so this is a no-op.
Future<PendingWebRedirectResult> completePendingAuthCodeRedirect() async =>
    const PendingWebRedirectResult.none();
