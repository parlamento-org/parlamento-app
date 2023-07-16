import 'package:frontend/models/political_party.dart';

class PartyStats {
  PoliticalParty politicalParty;
  double partyAffectionScore;
  int totalAmountOfProposalsVotedOn;
  double totalAffectionPoints;

  PartyStats({
    required this.politicalParty,
    required this.partyAffectionScore,
    required this.totalAmountOfProposalsVotedOn,
    required this.totalAffectionPoints,
  });

  factory PartyStats.fromJson(Map<String, dynamic> json) {
    return PartyStats(
      politicalParty: PoliticalParty.fromJson(json['politicalParty']),
      partyAffectionScore: json['partyAffectionScore'],
      totalAmountOfProposalsVotedOn: json['totalAmountOfProposalsVotedOn'],
      totalAffectionPoints: json['totalAffectionPoints'],
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'political_party': politicalParty.toJson(),
      'party_affection_score': partyAffectionScore,
      'total_amount_of_proposals_voted_on': totalAmountOfProposalsVotedOn,
      'total_affection_points': totalAffectionPoints,
    };
  }
}
