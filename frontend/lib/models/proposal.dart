import 'package:frontend/models/political_party.dart';
import 'package:frontend/models/voting_result.dart';

enum VotingOutcome {
  rejectedInGenerality('RejectedInGenerality'),
  rejectedInSpeciality('RejectedInSpeciality'),
  approvedInGenerality('ApprovedInGenerality'),
  approvedInSpeciality('ApprovedInSpeciality');

  const VotingOutcome(this.wireName);

  final String wireName;

  static VotingOutcome fromWireName(String value) {
    return VotingOutcome.values.firstWhere(
      (outcome) => outcome.wireName == value,
    );
  }
}

String parseCensoredText(String censoredText) {
  //replace <censored> with CENSURADO
  censoredText = censoredText.replaceAll('<censored>', 'CENSURADO');
  //remove \r
  censoredText = censoredText.replaceAll('\n', '');
  return censoredText.replaceAll(RegExp(r'<[^>]*>|&[^;]+;'), '');
}

class Proposal {
  final String title;
  final int id;
  final String legislatura;
  final String voteDate;
  final PoliticalParty proposingParty;
  final String fullTextUrl;
  final String censoredText;
  final VotingOutcome votingResult;
  final VotingResult votingResultInGenerality;
  final VotingResult? votingResultInSpeciality;

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
      votingResult: VotingOutcome.fromWireName(json['proposalResult']),
      votingResultInGenerality: VotingResult.fromJson(
        json['votingResultGenerality'],
      ),
      votingResultInSpeciality:
          json['votingResultSpeciality'] == null
              ? null
              : VotingResult.fromJson(json['votingResultSpeciality']),
    );
  }
}
