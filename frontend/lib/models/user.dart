import 'package:frontend/models/party_stats.dart';
import 'package:frontend/models/proposal_criteria.dart';
import 'package:frontend/models/vote_model.dart';

enum UserType { google, facebook, email }

class UserSession {
  String name;
  int userId;
  String email;
  int profilePictureId = 0;
  List<PartyStats> partyStats;
  List<UserVote> userVotes;
  UserType userType;
  ProposalCriteria? proposalCriteria;

  UserSession.empty()
    : name = '',
      userId = 0,
      email = '',
      profilePictureId = 0,
      userType = UserType.email,
      partyStats = [],
      userVotes = [];

  UserSession({
    required this.name,
    required this.userId,
    required this.email,
    required this.profilePictureId,
    required this.partyStats,
    required this.userVotes,
    required this.userType,
  }) {
    proposalCriteria = ProposalCriteria(userID: userId, lowestScoreAllowed: 0);
  }

  bool get isLoggedIn => userId != 0;

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
    return UserSession(
      name: json['userName'],
      userId: json['id'],
      email: json['email'],
      profilePictureId: json['profilePic'],
      userType: userType,
      partyStats: List.generate(
        json['partyStats'].length,
        (index) => PartyStats.fromJson(json['partyStats'][index]),
      ),
      userVotes: List.generate(
        json['votes'].length,
        (index) => UserVote.fromJson(json['votes'][index]),
      ),
    );
  }
}
