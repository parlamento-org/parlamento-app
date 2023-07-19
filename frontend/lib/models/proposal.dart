import 'package:frontend/models/political_party.dart';
import 'package:frontend/models/voting_result.dart';

enum VotingOutcome {
  RejectedInGenerality,
  RejectedInSpeciality,
  ApprovedInGenerality,
  ApprovedInSpeciality
}

String parseCensoredText(String censoredText) {
  //replace <censored> with CENSURADO
  censoredText = censoredText.replaceAll('<censored>', 'CENSURADO');
  //remove \r
  censoredText = censoredText.replaceAll('\n', '');
  return censoredText.replaceAll(RegExp(r'<[^>]*>|&[^;]+;'), '');
}

class Proposal {
  String title;
  int id;
  String legislatura;
  String voteDate;
  PoliticalParty proposingParty;
  String fullTextUrl;
  String censoredText;
  VotingOutcome votingResult;
  VotingResult votingResultInGenerality;
  VotingResult? votingResultInSpeciality;

  Proposal({
    required this.title,
    required this.id,
    required this.legislatura,
    required this.voteDate,
    required this.proposingParty,
    required this.fullTextUrl,
    required this.censoredText,
    required this.votingResult,
    required this.votingResultInGenerality,
    this.votingResultInSpeciality,
  });

  factory Proposal.fromJson(Map<String, dynamic> json) {
    return Proposal(
      title: json['proposalTitle'],
      id: json['id'],
      legislatura: json['legislatura'],
      voteDate: json['voteDate'],
      proposingParty: PoliticalParty.fromJson(json['proposingParty']),
      fullTextUrl: json['fullProposalTextLink'],
      censoredText: parseCensoredText(json['proposalTextHTML']),
      votingResult: VotingOutcome.values.firstWhere((element) =>
          element.toString() == 'VotingOutcome.' + json['proposalResult']),
      votingResultInGenerality:
          VotingResult.fromJson(json['votingResultGenerality']),
      votingResultInSpeciality: json['votingResultSpeciality'] == null
          ? null
          : VotingResult.fromJson(json['votingResultSpeciality']),
    );
  }
}
