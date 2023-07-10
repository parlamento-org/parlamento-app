class UserVote {
  final String id;
  final String voteId;
  final String voteOptionId;
  final String partyId;

  UserVote(
      {required this.id,
      required this.voteId,
      required this.voteOptionId,
      required this.partyId});

  factory UserVote.fromJson(Map<String, dynamic> json) {
    return UserVote(
      id: json['id'],
      voteId: json['voteId'],
      voteOptionId: json['voteOptionId'],
      partyId: json['partyId'],
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'voteId': voteId,
        'voteOptionId': voteOptionId,
        'partyId': partyId,
      };
}
