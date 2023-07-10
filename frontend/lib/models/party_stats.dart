class PartyStats {
  int id;
  int partyId;
  int totalVotes;
  int totalVotesPercentage;

  PartyStats({
    required this.id,
    required this.partyId,
    required this.totalVotes,
    required this.totalVotesPercentage,
  });

  factory PartyStats.fromJson(Map<String, dynamic> json) {
    return PartyStats(
      id: json['id'],
      partyId: json['party_id'],
      totalVotes: json['total_votes'],
      totalVotesPercentage: json['total_votes_percentage'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'id': id,
      'party_id': partyId,
      'total_votes': totalVotes,
      'total_votes_percentage': totalVotesPercentage,
    };
  }
}
