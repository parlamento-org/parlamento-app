class ProposalCriteria {
  final int userID;

  final List<String>? legislaturas;

  final String? oldestVoteDate;

  final String? newestVoteDate;

  final int lowestScoreAllowed;

  ProposalCriteria({
    required this.userID,
    this.legislaturas,
    this.oldestVoteDate,
    this.newestVoteDate,
    required this.lowestScoreAllowed,
  });

  factory ProposalCriteria.fromJson(Map<String, dynamic> json) {
    return ProposalCriteria(
      userID: json['userID'],
      legislaturas: json['legislaturas'],
      oldestVoteDate: json['oldestVoteDate'],
      newestVoteDate: json['newestVoteDate'],
      lowestScoreAllowed: json['lowestScoreAllowed'],
    );
  }

  Map<String, dynamic> toJson() => {
    'userID': userID,
    'legislaturas': legislaturas,
    'oldestVoteDate': oldestVoteDate,
    'newestVoteDate': newestVoteDate,
    'lowestScoreAllowed': lowestScoreAllowed,
  };
}
