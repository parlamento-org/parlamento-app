class ProposalCriteria {
  final List<String>? legislaturas;

  ProposalCriteria({
    this.legislaturas,
  });

  factory ProposalCriteria.fromJson(Map<String, dynamic> json) {
    return ProposalCriteria(
      legislaturas: json['legislaturas'],
    );
  }

  Map<String, dynamic> toJson() => {
    'legislaturas': legislaturas,
  };
}
