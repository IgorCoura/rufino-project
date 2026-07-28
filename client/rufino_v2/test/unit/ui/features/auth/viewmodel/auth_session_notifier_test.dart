import 'package:flutter_test/flutter_test.dart';
import 'package:rufino_v2/ui/features/auth/viewmodel/auth_session_notifier.dart';

void main() {
  group('AuthSessionNotifier', () {
    test('starts with the session not expired', () {
      final notifier = AuthSessionNotifier();
      expect(notifier.sessionExpired, isFalse);
    });

    test('flags the session as expired and notifies listeners', () {
      final notifier = AuthSessionNotifier();
      var notifications = 0;
      notifier.addListener(() => notifications++);

      notifier.notifySessionExpired();

      expect(notifier.sessionExpired, isTrue);
      expect(notifications, 1);
    });

    test('collapses repeated expirations into a single notification', () {
      final notifier = AuthSessionNotifier();
      var notifications = 0;
      notifier.addListener(() => notifications++);

      notifier.notifySessionExpired();
      notifier.notifySessionExpired();
      notifier.notifySessionExpired();

      expect(notifications, 1);
    });

    test('reset clears the flag and allows a new expiration to notify again',
        () {
      final notifier = AuthSessionNotifier();
      var notifications = 0;
      notifier.addListener(() => notifications++);

      notifier.notifySessionExpired();
      notifier.reset();
      notifier.notifySessionExpired();

      expect(notifier.sessionExpired, isTrue);
      expect(notifications, 3);
    });

    test('reset without a pending expiration does not notify', () {
      final notifier = AuthSessionNotifier();
      var notifications = 0;
      notifier.addListener(() => notifications++);

      notifier.reset();

      expect(notifications, 0);
    });
  });

  group('AuthSessionNotifier.redirectFor', () {
    test('allows every route while the session is healthy', () {
      final notifier = AuthSessionNotifier();

      expect(notifier.redirectFor('/'), isNull);
      expect(notifier.redirectFor('/login'), isNull);
      expect(notifier.redirectFor('/home'), isNull);
      expect(notifier.redirectFor('/employee'), isNull);
    });

    test('sends protected routes to login while the session is expired', () {
      final notifier = AuthSessionNotifier()..notifySessionExpired();

      expect(notifier.redirectFor('/home'), '/login');
      expect(notifier.redirectFor('/employee'), '/login');
      expect(notifier.redirectFor('/batch-download'), '/login');
    });

    test('keeps splash and login reachable while the session is expired', () {
      final notifier = AuthSessionNotifier()..notifySessionExpired();

      expect(notifier.redirectFor('/'), isNull);
      expect(notifier.redirectFor('/login'), isNull);
    });

    test('stops redirecting after reset', () {
      final notifier = AuthSessionNotifier()..notifySessionExpired();
      notifier.reset();

      expect(notifier.redirectFor('/home'), isNull);
    });
  });
}
