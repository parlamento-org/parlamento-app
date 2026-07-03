enum ProposalInteractionAction {
  support('Support'),
  oppose('Oppose'),
  abstain('Abstain'),
  skip('Skip'),
  unknown('Unknown');

  const ProposalInteractionAction(this.wireName);

  final String wireName;

  static ProposalInteractionAction fromWireName(String? value) {
    return ProposalInteractionAction.values.firstWhere(
      (action) => action.wireName == value,
      orElse: () => ProposalInteractionAction.unknown,
    );
  }
}

enum ParliamentaryVoteOrientation {
  inFavor('InFavor'),
  against('Against'),
  abstaining('Abstaining'),
  absent('Absent'),
  notInterested('NotInterested'),
  unknown('Unknown');

  const ParliamentaryVoteOrientation(this.wireName);

  final String wireName;

  static ParliamentaryVoteOrientation fromWireName(String? value) {
    return ParliamentaryVoteOrientation.values.firstWhere(
      (orientation) => orientation.wireName == value,
      orElse: () => ParliamentaryVoteOrientation.unknown,
    );
  }
}

class ProposalFlowFeedRequest {
  ProposalFlowFeedRequest({
    required this.userId,
    this.legislatures,
    this.limit = 1,
  });

  final int userId;
  final List<String>? legislatures;
  final int limit;

  Map<String, dynamic> toJson() => {
    'userId': userId,
    if (legislatures != null) 'legislatures': legislatures,
    'limit': limit,
  };
}

class InitiativeFeedCard {
  InitiativeFeedCard({
    required this.initiativeId,
    required this.initiativeType,
    this.initiativeNumber,
    required this.neutralTitle,
    this.summary,
    this.summaryGeneratedAtUtc,
    this.redactedExcerpt,
    this.redactedText,
    this.legislature,
    this.date,
  });

  final int initiativeId;
  final String initiativeType;
  final String? initiativeNumber;
  final String neutralTitle;
  final String? summary;
  final String? summaryGeneratedAtUtc;
  final String? redactedExcerpt;
  final String? redactedText;
  final String? legislature;
  final String? date;

  factory InitiativeFeedCard.fromJson(Map<String, dynamic> json) {
    return InitiativeFeedCard(
      initiativeId: _intOrZero(json['initiativeId']),
      initiativeType: _stringOrFallback(
        json['initiativeType'],
        'Iniciativa parlamentar',
      ),
      initiativeNumber: _stringOrNull(json['initiativeNumber']),
      neutralTitle: _stringOrFallback(
        json['neutralTitle'],
        'Iniciativa sem titulo disponivel',
      ),
      summary: _stringOrNull(json['summary']),
      summaryGeneratedAtUtc: _stringOrNull(json['summaryGeneratedAtUtc']),
      redactedExcerpt: _stringOrNull(json['redactedExcerpt']),
      redactedText: _stringOrNull(json['redactedText']),
      legislature: _stringOrNull(json['legislature']),
      date: _stringOrNull(json['date']),
    );
  }
}

class ProposalInteractionSubmission {
  ProposalInteractionSubmission({
    required this.userId,
    required this.initiativeId,
    required this.action,
    this.idempotencyKey,
  });

  final int userId;
  final int initiativeId;
  final ProposalInteractionAction action;
  final String? idempotencyKey;

  Map<String, dynamic> toJson() => {
    'userId': userId,
    'initiativeId': initiativeId,
    'action': action.wireName,
    if (idempotencyKey != null) 'idempotencyKey': idempotencyKey,
  };
}

class ProposalInteractionResult {
  ProposalInteractionResult({
    required this.interactionId,
    required this.userId,
    required this.initiativeId,
    required this.action,
    this.createdAtUtc,
    required this.isDuplicate,
  });

  final int interactionId;
  final int userId;
  final int initiativeId;
  final ProposalInteractionAction action;
  final String? createdAtUtc;
  final bool isDuplicate;

  factory ProposalInteractionResult.fromJson(Map<String, dynamic> json) {
    return ProposalInteractionResult(
      interactionId: _intOrZero(json['interactionId']),
      userId: _intOrZero(json['userId']),
      initiativeId: _intOrZero(json['initiativeId']),
      action: ProposalInteractionAction.fromWireName(
        _stringOrNull(json['action']),
      ),
      createdAtUtc: _stringOrNull(json['createdAtUtc']),
      isDuplicate: _boolOrFalse(json['isDuplicate']),
    );
  }
}

class ProposalReveal {
  ProposalReveal({
    required this.initiativeId,
    required this.initiativeType,
    this.initiativeNumber,
    required this.title,
    required this.userVote,
    required this.proposers,
    this.generalityVote,
    required this.officialSources,
    required this.journey,
  });

  final int initiativeId;
  final String initiativeType;
  final String? initiativeNumber;
  final String title;
  final ProposalInteractionAction userVote;
  final List<ProposalProposer> proposers;
  final ParliamentaryVoteSummary? generalityVote;
  final List<OfficialSourceLink> officialSources;
  final ProposalJourneyAction journey;

  factory ProposalReveal.fromJson(Map<String, dynamic> json) {
    return ProposalReveal(
      initiativeId: _intOrZero(json['initiativeId']),
      initiativeType: _stringOrFallback(
        json['initiativeType'],
        'Iniciativa parlamentar',
      ),
      initiativeNumber: _stringOrNull(json['initiativeNumber']),
      title: _stringOrFallback(
        json['title'],
        'Iniciativa sem titulo disponivel',
      ),
      userVote: ProposalInteractionAction.fromWireName(
        _stringOrNull(json['userVote']),
      ),
      proposers: _objectList(json['proposers'], ProposalProposer.fromJson),
      generalityVote:
          json['generalityVote'] is Map<String, dynamic>
              ? ParliamentaryVoteSummary.fromJson(json['generalityVote'])
              : null,
      officialSources: _objectList(
        json['officialSources'],
        OfficialSourceLink.fromJson,
      ),
      journey:
          json['journey'] is Map<String, dynamic>
              ? ProposalJourneyAction.fromJson(json['journey'])
              : ProposalJourneyAction.empty(),
    );
  }
}

class ProposalProposer {
  ProposalProposer({required this.kind, this.name, this.acronym});

  final String kind;
  final String? name;
  final String? acronym;

  factory ProposalProposer.fromJson(Map<String, dynamic> json) {
    return ProposalProposer(
      kind: _stringOrFallback(json['kind'], 'Unknown'),
      name: _stringOrNull(json['name']),
      acronym: _stringOrNull(json['acronym']),
    );
  }
}

class ParliamentaryVoteSummary {
  ParliamentaryVoteSummary({
    required this.stageCode,
    required this.stageName,
    this.date,
    this.description,
    this.result,
    this.approved,
    required this.partyVotes,
  });

  final String stageCode;
  final String stageName;
  final String? date;
  final String? description;
  final String? result;
  final bool? approved;
  final List<PartyVote> partyVotes;

  factory ParliamentaryVoteSummary.fromJson(Map<String, dynamic> json) {
    return ParliamentaryVoteSummary(
      stageCode: _stringOrFallback(json['stageCode'], ''),
      stageName: _stringOrFallback(json['stageName'], 'Votacao parlamentar'),
      date: _stringOrNull(json['date']),
      description: _stringOrNull(json['description']),
      result: _stringOrNull(json['result']),
      approved: _boolOrNull(json['approved']),
      partyVotes: _objectList(json['partyVotes'], PartyVote.fromJson),
    );
  }
}

class PartyVote {
  PartyVote({
    required this.partyAcronym,
    required this.orientation,
    this.numberOfDeputies,
    this.isUnanimousWithinParty,
  });

  final String partyAcronym;
  final ParliamentaryVoteOrientation orientation;
  final int? numberOfDeputies;
  final bool? isUnanimousWithinParty;

  factory PartyVote.fromJson(Map<String, dynamic> json) {
    return PartyVote(
      partyAcronym: _stringOrFallback(json['partyAcronym'], ''),
      orientation: ParliamentaryVoteOrientation.fromWireName(
        _stringOrNull(json['orientation']),
      ),
      numberOfDeputies: _intOrNull(json['numberOfDeputies']),
      isUnanimousWithinParty: _boolOrNull(json['isUnanimousWithinParty']),
    );
  }
}

class OfficialSourceLink {
  OfficialSourceLink({
    required this.kind,
    required this.label,
    required this.url,
  });

  final String kind;
  final String label;
  final String url;

  factory OfficialSourceLink.fromJson(Map<String, dynamic> json) {
    return OfficialSourceLink(
      kind: _stringOrFallback(json['kind'], 'Source'),
      label: _stringOrFallback(json['label'], 'Official source'),
      url: _stringOrFallback(json['url'], ''),
    );
  }
}

class ProposalJourneyAction {
  ProposalJourneyAction({required this.label, required this.endpoint});

  final String label;
  final String endpoint;

  factory ProposalJourneyAction.fromJson(Map<String, dynamic> json) {
    return ProposalJourneyAction(
      label: _stringOrFallback(json['label'], "Follow the proposal's journey"),
      endpoint: _stringOrFallback(json['endpoint'], ''),
    );
  }

  factory ProposalJourneyAction.empty() {
    return ProposalJourneyAction(
      label: "Follow the proposal's journey",
      endpoint: '',
    );
  }
}

class ProposalJourney {
  ProposalJourney({
    required this.initiativeId,
    required this.initiativeType,
    this.initiativeNumber,
    required this.title,
    required this.phases,
  });

  final int initiativeId;
  final String initiativeType;
  final String? initiativeNumber;
  final String title;
  final List<ProposalJourneyPhase> phases;

  factory ProposalJourney.fromJson(Map<String, dynamic> json) {
    return ProposalJourney(
      initiativeId: _intOrZero(json['initiativeId']),
      initiativeType: _stringOrFallback(
        json['initiativeType'],
        'Iniciativa parlamentar',
      ),
      initiativeNumber: _stringOrNull(json['initiativeNumber']),
      title: _stringOrFallback(
        json['title'],
        'Iniciativa sem titulo disponivel',
      ),
      phases: _objectList(json['phases'], ProposalJourneyPhase.fromJson),
    );
  }
}

class ProposalJourneyPhase {
  ProposalJourneyPhase({
    this.phaseCode,
    required this.phaseName,
    this.date,
    this.status,
    required this.summary,
    this.observation,
    this.approvedTextId,
    required this.votes,
    required this.documents,
    required this.diaryLinks,
    required this.videos,
    required this.transcripts,
  });

  final String? phaseCode;
  final String phaseName;
  final String? date;
  final String? status;
  final String summary;
  final String? observation;
  final String? approvedTextId;
  final List<ParliamentaryVoteSummary> votes;
  final List<OfficialSourceLink> documents;
  final List<OfficialSourceLink> diaryLinks;
  final List<ProposalJourneyVideo> videos;
  final List<OfficialSourceLink> transcripts;

  factory ProposalJourneyPhase.fromJson(Map<String, dynamic> json) {
    return ProposalJourneyPhase(
      phaseCode: _stringOrNull(json['phaseCode']),
      phaseName: _stringOrFallback(json['phaseName'], 'Fase parlamentar'),
      date: _stringOrNull(json['date']),
      status: _stringOrNull(json['status']),
      summary: _stringOrFallback(
        json['summary'],
        'Parliament recorded a lifecycle phase for this initiative.',
      ),
      observation: _stringOrNull(json['observation']),
      approvedTextId: _stringOrNull(json['approvedTextId']),
      votes: _objectList(json['votes'], ParliamentaryVoteSummary.fromJson),
      documents: _objectList(json['documents'], OfficialSourceLink.fromJson),
      diaryLinks: _objectList(json['diaryLinks'], OfficialSourceLink.fromJson),
      videos: _objectList(json['videos'], ProposalJourneyVideo.fromJson),
      transcripts: _objectList(
        json['transcripts'],
        OfficialSourceLink.fromJson,
      ),
    );
  }
}

class ProposalJourneyVideo {
  ProposalJourneyVideo({
    this.speakerName,
    this.speakerParty,
    this.governmentMemberName,
    this.governmentMemberRole,
    this.date,
    this.startTime,
    this.endTime,
    required this.url,
  });

  final String? speakerName;
  final String? speakerParty;
  final String? governmentMemberName;
  final String? governmentMemberRole;
  final String? date;
  final String? startTime;
  final String? endTime;
  final String url;

  factory ProposalJourneyVideo.fromJson(Map<String, dynamic> json) {
    return ProposalJourneyVideo(
      speakerName: _stringOrNull(json['speakerName']),
      speakerParty: _stringOrNull(json['speakerParty']),
      governmentMemberName: _stringOrNull(json['governmentMemberName']),
      governmentMemberRole: _stringOrNull(json['governmentMemberRole']),
      date: _stringOrNull(json['date']),
      startTime: _stringOrNull(json['startTime']),
      endTime: _stringOrNull(json['endTime']),
      url: _stringOrFallback(json['url'], ''),
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

bool? _boolOrNull(dynamic value) {
  return value is bool ? value : null;
}

bool _boolOrFalse(dynamic value) {
  return _boolOrNull(value) ?? false;
}
