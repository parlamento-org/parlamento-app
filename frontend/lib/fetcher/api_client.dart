import 'dart:async';
import 'dart:convert';

import 'package:frontend/auth/token_storage.dart';
import 'package:http/http.dart' as http;

class ApiException implements Exception {
  ApiException(this.message, {this.statusCode, this.cause});

  final String message;
  final int? statusCode;
  final Object? cause;

  @override
  String toString() => message;
}

class ApiResponse {
  ApiResponse({required this.statusCode, required this.body});

  final int statusCode;
  final String body;

  bool get isSuccessful => statusCode >= 200 && statusCode < 300;

  Map<String, dynamic> jsonObject() {
    try {
      return jsonDecode(body) as Map<String, dynamic>;
    } on FormatException catch (error) {
      throw ApiException('Invalid JSON response', cause: error);
    } on TypeError catch (error) {
      throw ApiException('Expected a JSON object response', cause: error);
    }
  }

  List<dynamic> jsonArray() {
    try {
      return jsonDecode(body) as List<dynamic>;
    } on FormatException catch (error) {
      throw ApiException('Invalid JSON response', cause: error);
    } on TypeError catch (error) {
      throw ApiException('Expected a JSON array response', cause: error);
    }
  }
}

class ApiClient {
  ApiClient({
    required this.baseUrl,
    required http.Client httpClient,
    this.timeout = const Duration(seconds: 15),
    FutureOr<String?> Function()? accessTokenProvider,
    FutureOr<void> Function()? onUnauthorized,
  }) : _httpClient = httpClient,
       _accessTokenProvider =
           accessTokenProvider ?? AppTokenStore.currentAccessToken,
       _onUnauthorized = onUnauthorized ?? AppTokenStore.handleUnauthorized;

  final Uri baseUrl;
  final Duration timeout;
  final http.Client _httpClient;
  final FutureOr<String?> Function()? _accessTokenProvider;
  final FutureOr<void> Function()? _onUnauthorized;

  Future<ApiResponse> getJson(String path) {
    return _sendJson('GET', path);
  }

  Future<ApiResponse> postJson(String path, Map<String, dynamic> body) {
    return _sendJson('POST', path, body: body);
  }

  Future<ApiResponse> putJson(String path, Map<String, dynamic> body) {
    return _sendJson('PUT', path, body: body);
  }

  Future<ApiResponse> _sendJson(
    String method,
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final request = http.Request(method, baseUrl.resolve(path))
      ..headers['Content-Type'] = 'application/json';

    final accessToken = await Future<String?>.value(
      _accessTokenProvider?.call(),
    );
    if (accessToken != null && accessToken.isNotEmpty) {
      request.headers['Authorization'] = 'Bearer $accessToken';
    }

    if (body != null) {
      request.body = jsonEncode(body);
    }

    try {
      final streamedResponse = await _httpClient.send(request).timeout(timeout);
      final response = await http.Response.fromStream(streamedResponse);
      if (response.statusCode == 401) {
        await Future<void>.value(_onUnauthorized?.call());
      }

      return ApiResponse(statusCode: response.statusCode, body: response.body);
    } on TimeoutException catch (error) {
      throw ApiException('Request timed out', cause: error);
    } on http.ClientException catch (error) {
      throw ApiException('Network request failed', cause: error);
    }
  }
}
