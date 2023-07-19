import 'package:frontend/models/vote_model.dart';

class VotingBlock {
  bool isUnanimousWithinParty;
  int? numberOfDeputados;
  String politicalParty;
  VoteOrientation voteOrientation;

  VotingBlock(
      {required this.isUnanimousWithinParty,
      required this.numberOfDeputados,
      required this.politicalParty,
      required this.voteOrientation});

  factory VotingBlock.fromJson(Map<String, dynamic> json) {
    return VotingBlock(
      isUnanimousWithinParty: json['isUninamousWithinParty'],
      numberOfDeputados: json['numberOfDeputies'],
      politicalParty: json['politicalPartyAcronym'],
      voteOrientation: VoteOrientation.values.firstWhere((element) =>
          element.toString() == 'VoteOrientation.' + json['votingOrientation']),
    );
  }

  Map<String, dynamic> toJson() => {
        'isUnanimousWithinParty': isUnanimousWithinParty,
        'numberOfDeputados': numberOfDeputados,
        'politicalParty': politicalParty,
        'voteOrientation': voteOrientation,
      };
}
