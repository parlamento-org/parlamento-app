import 'package:frontend/models/vote_model.dart';

class VotingBlock {
  final bool isUnanimousWithinParty;
  final int? numberOfDeputados;
  final String politicalParty;
  final VoteOrientation voteOrientation;

  VotingBlock({
    required this.isUnanimousWithinParty,
    required this.numberOfDeputados,
    required this.politicalParty,
    required this.voteOrientation,
  });

  factory VotingBlock.fromJson(Map<String, dynamic> json) {
    return VotingBlock(
      isUnanimousWithinParty: json['isUninamousWithinParty'],
      numberOfDeputados: json['numberOfDeputies'],
      politicalParty: json['politicalPartyAcronym'],
      voteOrientation: VoteOrientation.fromWireName(json['votingOrientation']),
    );
  }

  Map<String, dynamic> toJson() => {
    'isUnanimousWithinParty': isUnanimousWithinParty,
    'numberOfDeputados': numberOfDeputados,
    'politicalParty': politicalParty,
    'voteOrientation': voteOrientation,
  };
}
