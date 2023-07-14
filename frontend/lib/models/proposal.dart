import 'package:frontend/models/political_party.dart';

enum VotingResult {
  approvedInGenereality,
  approvedInSpeciality,
  rejectedInGenereality,
  rejectedInSpeciality
}

class Proposal {
  String title;
  int id;
  String legislatura;
  String voteDate;
  PoliticalParty proposingParty;
  String fullTextUrl;
  String censoredText;
  VotingResult votingResult;
  VotingResult votingResultInGenerality;
  VotingResult votingResultInSpeciality;

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
    required this.votingResultInSpeciality,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = <String, dynamic>{};
    data['title'] = title;
    data['id'] = id;
    data['legislatura'] = legislatura;
    data['voteDate'] = voteDate;
    data['proposingParty'] = proposingParty.toJson();
    data['fullTextUrl'] = fullTextUrl;
    data['censoredText'] = censoredText;
    data['votingResult'] = votingResult;
    data['votingResultInGenerality'] = votingResultInGenerality;
    data['votingResultInSpeciality'] = votingResultInSpeciality;
    return data;
  }

  factory Proposal.fromJson(Map<String, dynamic> json) {
    return Proposal(
      title: json['title'],
      id: json['id'],
      legislatura: json['legislatura'],
      voteDate: json['voteDate'],
      proposingParty: PoliticalParty.fromJson(json['proposingParty']),
      fullTextUrl: json['fullTextUrl'],
      censoredText: json['censoredText'],
      votingResult: json['votingResult'],
      votingResultInGenerality: json['votingResultInGenerality'],
      votingResultInSpeciality: json['votingResultInSpeciality'],
    );
  }
}
