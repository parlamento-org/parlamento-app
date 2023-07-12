import 'package:frontend/models/party_stats.dart';
import 'package:frontend/models/vote_model.dart';

class UserSession {
  String name;
  int userId;
  String email;
  String password;
  int profilePictureId = 0;
  List<PartyStats> partyStats;
  List<UserVote> userVotes;
  UserSession.empty()
      : name = '',
        userId = 0,
        email = '',
        password = '',
        profilePictureId = 0,
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
    return UserSession(
        name: json['userName'],
        userId: json['id'],
        email: json['email'],
        password: json['password'],
        profilePictureId: json['profilePic'],
        partyStats: List<PartyStats>.from(json['partyStats']),
        userVotes: List<UserVote>.from(json['votes']));
  }
}
