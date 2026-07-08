class ProfileStats {
  ProfileStats({required this.overview, required this.partyAlignment});

  final ProfileOverview overview;
  final PartyAlignmentSection partyAlignment;

  factory ProfileStats.fromJson(Map<String, dynamic> json) {
    return ProfileStats(
      overview:
          json['overview'] is Map<String, dynamic>
              ? ProfileOverview.fromJson(json['overview'])
              : ProfileOverview.empty(),
      partyAlignment:
          json['partyAlignment'] is Map<String, dynamic>
              ? PartyAlignmentSection.fromJson(json['partyAlignment'])
              : PartyAlignmentSection.empty(),
    );
  }
}

class ProfileOverview {
  ProfileOverview({
    required this.proposalsInteracted,
    required this.supportCount,
    required this.opposeCount,
    required this.abstentionCount,
    required this.skipCount,
    required this.supportRate,
    required this.skipRate,
  });

  final int proposalsInteracted;
  final int supportCount;
  final int opposeCount;
  final int abstentionCount;
  final int skipCount;
  final double supportRate;
  final double skipRate;

  factory ProfileOverview.empty() {
    return ProfileOverview(
      proposalsInteracted: 0,
      supportCount: 0,
      opposeCount: 0,
      abstentionCount: 0,
      skipCount: 0,
      supportRate: 0,
      skipRate: 0,
    );
  }

  factory ProfileOverview.fromJson(Map<String, dynamic> json) {
    return ProfileOverview(
      proposalsInteracted: _intOrZero(json['proposalsInteracted']),
      supportCount: _intOrZero(json['supportCount']),
      opposeCount: _intOrZero(json['opposeCount']),
      abstentionCount: _intOrZero(json['abstentionCount']),
      skipCount: _intOrZero(json['skipCount']),
      supportRate: _doubleOrZero(json['supportRate']),
      skipRate: _doubleOrZero(json['skipRate']),
    );
  }
}

class PartyAlignmentSection {
  PartyAlignmentSection({
    required this.isUnlocked,
    required this.minimumComparableVotes,
    required this.totalComparableVotes,
    required this.parties,
  });

  final bool isUnlocked;
  final int minimumComparableVotes;
  final int totalComparableVotes;
  final List<PartyAlignment> parties;

  factory PartyAlignmentSection.empty() {
    return PartyAlignmentSection(
      isUnlocked: false,
      minimumComparableVotes: 10,
      totalComparableVotes: 0,
      parties: const [],
    );
  }

  factory PartyAlignmentSection.fromJson(Map<String, dynamic> json) {
    return PartyAlignmentSection(
      isUnlocked: json['isUnlocked'] is bool ? json['isUnlocked'] : false,
      minimumComparableVotes: _intOrFallback(
        json['minimumComparableVotes'],
        10,
      ),
      totalComparableVotes: _intOrZero(json['totalComparableVotes']),
      parties: _objectList(json['parties'], PartyAlignment.fromJson),
    );
  }
}

class PartyAlignment {
  PartyAlignment({
    required this.partyId,
    required this.partyAcronym,
    required this.partyName,
    this.partyLogo,
    required this.alignedCount,
    required this.comparableCount,
    required this.alignmentPercentage,
  });

  final String partyId;
  final String partyAcronym;
  final String partyName;
  final String? partyLogo;
  final int alignedCount;
  final int comparableCount;
  final double alignmentPercentage;

  factory PartyAlignment.fromJson(Map<String, dynamic> json) {
    final acronym = _stringOrFallback(json['partyAcronym'], '');

    return PartyAlignment(
      partyId: _stringOrFallback(json['partyId'], acronym),
      partyAcronym: acronym,
      partyName: _stringOrFallback(json['partyName'], acronym),
      partyLogo: _stringOrNull(json['partyLogo']),
      alignedCount: _intOrZero(json['alignedCount']),
      comparableCount: _intOrZero(json['comparableCount']),
      alignmentPercentage: _doubleOrZero(json['alignmentPercentage']),
    );
  }
}

List<T> _objectList<T>(
  dynamic value,
  T Function(Map<String, dynamic>) fromJson,
) {
  if (value is! List) return [];

  return value
      .whereType<Map<String, dynamic>>()
      .map(fromJson)
      .toList(growable: false);
}

String? _stringOrNull(dynamic value) {
  return value is String && value.trim().isNotEmpty ? value : null;
}

String _stringOrFallback(dynamic value, String fallback) {
  return _stringOrNull(value) ?? fallback;
}

int? _intOrNull(dynamic value) {
  if (value is int) return value;
  if (value is num) return value.toInt();
  if (value is String) return int.tryParse(value);
  return null;
}

int _intOrZero(dynamic value) {
  return _intOrNull(value) ?? 0;
}

int _intOrFallback(dynamic value, int fallback) {
  return _intOrNull(value) ?? fallback;
}

double _doubleOrZero(dynamic value) {
  if (value is num) return value.toDouble();
  if (value is String) return double.tryParse(value) ?? 0;
  return 0;
}
