class PoliticalParty {
  String partyAcronym;
  String partyName;
  String logoUrl;

  PoliticalParty({
    required this.partyAcronym,
    required this.partyName,
    required this.logoUrl,
  });

  Map<String, dynamic> toJson() {
    final Map<String, dynamic> data = <String, dynamic>{};
    data['partyAcronym'] = partyAcronym;
    data['partyName'] = partyName;
    data['logoUrl'] = logoUrl;
    return data;
  }

  factory PoliticalParty.fromJson(Map<String, dynamic> json) {
    return PoliticalParty(
      partyAcronym: json['partyAcronym'],
      partyName: json['partyName'],
      logoUrl: json['logoUrl'],
    );
  }
}
