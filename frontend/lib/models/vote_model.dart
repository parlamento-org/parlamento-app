enum VoteOrientation {
  inFavor('InFavor'),
  against('Against'),
  abstaining('Abstaining'),
  notInterested('NotInterested');

  const VoteOrientation(this.wireName);

  final String wireName;

  static VoteOrientation fromWireName(String value) {
    return VoteOrientation.values.firstWhere(
      (orientation) => orientation.wireName == value,
    );
  }
}

class UserVote {
  final int? userID;
  final int proposalID;
  final VoteOrientation voteOrientation;
  final String? voteDate;

  UserVote({
    this.userID,
    required this.proposalID,
    required this.voteOrientation,
    this.voteDate,
  });

  factory UserVote.fromJson(Map<String, dynamic> json) {
    return UserVote(
      userID: json['userID'],
      proposalID: json['projectLawID'],
      voteOrientation: VoteOrientation.fromWireName(json['votingOrientation']),
      voteDate: json['voteDate'],
    );
  }

  Map<String, dynamic> toJson() => {
    'userID': userID,
    'projectLawID': proposalID,
    'votingOrientation': voteOrientation.wireName,
  };
}
