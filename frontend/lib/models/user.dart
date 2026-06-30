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

  UserSession.empty()
    : name = '',
      userId = 0,
      email = '',
      profilePictureId = 0,
      userType = UserType.email,
      partyStats = [],
      userVotes = [],
      proposalCriteria = null;

  UserSession({
    required this.name,
    required this.userId,
    required this.email,
    required this.profilePictureId,
    required this.partyStats,
    required this.userVotes,
    required this.userType,
  }) : proposalCriteria = ProposalCriteria(
         userID: userId,
         lowestScoreAllowed: 0,
       );

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
