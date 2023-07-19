import 'package:frontend/models/voting_block.dart';

class VotingResult {
  bool isUnanimous;
  List<VotingBlock> votingBlocks;

  VotingResult({required this.isUnanimous, required this.votingBlocks});

  factory VotingResult.fromJson(Map<String, dynamic> json) {
    int numberOfVotingBlocks = json['votingBlocks'].length;

    return VotingResult(
        isUnanimous: json['isUninamous'],
        votingBlocks: List.generate(numberOfVotingBlocks,
            (index) => VotingBlock.fromJson(json['votingBlocks'][index])));
  }

  Map<String, dynamic> toJson() => {
        'isUninamous': isUnanimous,
        'votingBlocks': votingBlocks,
      };
}
