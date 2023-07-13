import 'package:frontend/models/party_stats.dart';
import 'package:frontend/models/vote_model.dart';

enum UserType { google, facebook, email }

class UserSession {
  String name;
  int userId;
  String email;
  String password;
  int profilePictureId = 0;
  List<PartyStats> partyStats;
  List<UserVote> userVotes;
  UserType userType;
  UserSession.empty()
      : name = '',
        userId = 0,
        email = '',
        password = '',
        profilePictureId = 0,
        userType = UserType.email,
        partyStats = [],
        userVotes = [];

  UserSession({
    required this.name,
    required this.userId,
    required this.email,
    required this.password,
    required this.profilePictureId,
    required this.partyStats,
    required this.userVotes,
    required this.userType,
  });

  bool get isLoggedIn => userId != 0;

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = <String, dynamic>{};
    // data['name'] = this.name;
    data['email'] = email;
    data['password'] = password;
    // data['token'] = this.token;
    return data;
  }

  factory UserSession.fromJson(Map<String, dynamic> json) {
    UserType userType = UserType.email;
    if (json['googleIDToken'] != null) {
      userType = UserType.google;
    } else if (json['facebookIDToken'] != null) {
      userType = UserType.facebook;
    }
    return UserSession(
        name: json['userName'],
        userId: json['id'],
        email: json['email'],
        password: json['password'],
        profilePictureId: json['profilePic'],
        userType: userType,
        partyStats: List<PartyStats>.from(json['partyStats']),
        userVotes: List<UserVote>.from(json['votes']));
  }
}
