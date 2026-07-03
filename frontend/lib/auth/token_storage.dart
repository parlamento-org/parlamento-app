import 'dart:async';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:frontend/models/user.dart';

class StoredAuthToken {
  StoredAuthToken({required this.accessToken, required this.expiresAtUtc});

  final String accessToken;
  final DateTime expiresAtUtc;

  bool get isExpired => !expiresAtUtc.isAfter(DateTime.now().toUtc());
}

abstract class TokenStorage {
  Future<void> save(StoredAuthToken token);

  Future<StoredAuthToken?> read();

  Future<void> clear();
}

class SecureTokenStorage implements TokenStorage {
  SecureTokenStorage({FlutterSecureStorage? storage})
    : _storage = storage ?? const FlutterSecureStorage();

  static const _accessTokenKey = 'parlamento.accessToken';
  static const _expiresAtKey = 'parlamento.expiresAtUtc';

  final FlutterSecureStorage _storage;

  @override
  Future<void> save(StoredAuthToken token) async {
    await _storage.write(key: _accessTokenKey, value: token.accessToken);
    await _storage.write(
      key: _expiresAtKey,
      value: token.expiresAtUtc.toUtc().toIso8601String(),
    );
  }

  @override
  Future<StoredAuthToken?> read() async {
    final accessToken = await _storage.read(key: _accessTokenKey);
    final expiresAtValue = await _storage.read(key: _expiresAtKey);
    final expiresAtUtc =
        expiresAtValue == null ? null : DateTime.tryParse(expiresAtValue);

    if (accessToken == null || accessToken.isEmpty || expiresAtUtc == null) {
      return null;
    }

    return StoredAuthToken(
      accessToken: accessToken,
      expiresAtUtc: expiresAtUtc.toUtc(),
    );
  }

  @override
  Future<void> clear() async {
    await _storage.delete(key: _accessTokenKey);
    await _storage.delete(key: _expiresAtKey);
  }
}

class InMemoryTokenStorage implements TokenStorage {
  StoredAuthToken? _token;

  @override
  Future<void> save(StoredAuthToken token) async {
    _token = token;
  }

  @override
  Future<StoredAuthToken?> read() async => _token;

  @override
  Future<void> clear() async {
    _token = null;
  }
}

class AppTokenStore {
  static TokenStorage storage = SecureTokenStorage();
  static FutureOr<void> Function()? onUnauthorized;

  static Future<void> saveSession(UserSession session) {
    return storage.save(
      StoredAuthToken(
        accessToken: session.accessToken,
        expiresAtUtc: session.expiresAtUtc,
      ),
    );
  }

  static Future<String?> currentAccessToken() async {
    final token = await storage.read();
    if (token == null) {
      return null;
    }

    if (token.isExpired) {
      await handleUnauthorized();
      return null;
    }

    return token.accessToken;
  }

  static Future<void> clear() {
    return storage.clear();
  }

  static Future<void> handleUnauthorized() async {
    await storage.clear();
    final handler = onUnauthorized;
    if (handler != null) {
      await Future<void>.value(handler());
    }
  }
}
