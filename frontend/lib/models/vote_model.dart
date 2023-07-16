enum VoteOrientation { InFavor, Against, Abstaining, NotInterested }

class UserVote {
  final int? userID;
  final int proposalID;
  final VoteOrientation voteOrientation;
  final String? voteDate;

  UserVote(
      {this.userID,
      required this.proposalID,
      required this.voteOrientation,
      this.voteDate});

  factory UserVote.fromJson(Map<String, dynamic> json) {
    return UserVote(
      userID: json['userID'],
      proposalID: json['projectLawID'],
      voteOrientation: VoteOrientation.values.firstWhere((element) =>
          element.toString() == "VoteOrientation." + json['votingOrientation']),
      voteDate: json['voteDate'],
    );
  }

  Map<String, dynamic> toJson() => {
        'userID': userID,
        'projectLawID': proposalID,
        'votingOrientation':
            voteOrientation.toString().replaceAll("VoteOrientation.", ""),
      };
}
