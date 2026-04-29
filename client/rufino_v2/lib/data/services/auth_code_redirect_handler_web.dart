import 'oauth_login_strategy_web.dart' as web_strategy;
import 'pending_web_redirect_result.dart';

/// Web-platform implementation — delegates to the strategy file's
/// `completePendingWebRedirect` so we don't import `package:web` from
/// IO builds.
Future<PendingWebRedirectResult> completePendingAuthCodeRedirect() =>
    web_strategy.completePendingWebRedirect();
