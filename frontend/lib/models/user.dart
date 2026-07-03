import 'package:frontend/models/party_stats.dart';
import 'package:frontend/models/proposal_criteria.dart';
import 'package:frontend/models/vote_model.dart';

enum UserType { google, facebook, email }

class UserSession {
  final String name;
  final int userId;
  final String email;
  final int profilePictureId;
  final List<PartyStats> partyStats;
  final List<UserVote> userVotes;
  final UserType userType;
  final ProposalCriteria? proposalCriteria;
  final String accessToken;
  final DateTime expiresAtUtc;

  UserSession.empty()
    : name = '',
      userId = 0,
      email = '',
      profilePictureId = 0,
      userType = UserType.email,
      partyStats = [],
      userVotes = [],
      accessToken = '',
      expiresAtUtc = DateTime.fromMillisecondsSinceEpoch(0, isUtc: true),
      proposalCriteria = null;

  UserSession({
    required this.name,
    required this.userId,
    required this.email,
    required this.profilePictureId,
    required this.partyStats,
    required this.userVotes,
    required this.userType,
    required this.accessToken,
    required this.expiresAtUtc,
  }) : proposalCriteria = ProposalCriteria(
         userID: userId,
         lowestScoreAllowed: 0,
       );

  bool get hasValidToken =>
      accessToken.isNotEmpty && expiresAtUtc.isAfter(DateTime.now().toUtc());

  bool get isLoggedIn => userId != 0 && hasValidToken;

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = <String, dynamic>{};
    // data['name'] = this.name;
    data['email'] = email;
    // data['token'] = this.token;
    return data;
  }

  factory UserSession.fromJson(
    Map<String, dynamic> json, {
    UserType userType = UserType.email,
  }) {
    final userJson =
        json['user'] is Map<String, dynamic>
            ? json['user'] as Map<String, dynamic>
            : json;
    final expiresAtUtc =
        DateTime.tryParse((json['expiresAtUtc'] ?? '').toString()) ??
        DateTime.fromMillisecondsSinceEpoch(0, isUtc: true);

    return UserSession(
      name: _stringOrFallback(userJson['userName'], ''),
      userId: _intOrZero(userJson['id']),
      email: _stringOrFallback(userJson['email'], ''),
      profilePictureId: _intOrZero(userJson['profilePic']),
      userType: userType,
      accessToken: _stringOrFallback(json['accessToken'], ''),
      expiresAtUtc: expiresAtUtc.toUtc(),
      partyStats: _objectList(userJson['partyStats'], PartyStats.fromJson),
      userVotes: _objectList(userJson['votes'], UserVote.fromJson),
    );
  }
}

List<T> _objectList<T>(
  dynamic value,
  T Function(Map<String, dynamic>) fromJson,
) {
  if (value is! List) return [];

  return value
      .whereType<Map<String, dynamic>>()
      .map(fromJson)
      .toList(growable: false);
}

String? _stringOrNull(dynamic value) {
  return value is String && value.trim().isNotEmpty ? value : null;
}

String _stringOrFallback(dynamic value, String fallback) {
  return _stringOrNull(value) ?? fallback;
}

int? _intOrNull(dynamic value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  if (value is String) return int.tryParse(value);
  return null;
}

int _intOrZero(dynamic value) {
  return _intOrNull(value) ?? 0;
}
