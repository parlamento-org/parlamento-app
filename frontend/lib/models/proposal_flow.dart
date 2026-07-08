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
  ProposalFlowFeedRequest({this.legislatures, this.limit = 1});

  final List<String>? legislatures;
  final int limit;

  Map<String, dynamic> toJson() => {
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
    this.summaryBulletPoints = const [],
    this.summaryGeneratedAtUtc,
    this.redactedExcerpt,
    this.redactedText,
    this.redactedHtml,
    this.legislature,
    this.date,
  });

  final int initiativeId;
  final String initiativeType;
  final String? initiativeNumber;
  final String neutralTitle;
  final String? summary;
  final List<String> summaryBulletPoints;
  final String? summaryGeneratedAtUtc;
  final String? redactedExcerpt;
  final String? redactedText;
  final String? redactedHtml;
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
        'Iniciativa sem título disponível',
      ),
      summary: _stringOrNull(json['summary']),
      summaryBulletPoints: _stringList(json['summaryBulletPoints']),
      summaryGeneratedAtUtc: _stringOrNull(json['summaryGeneratedAtUtc']),
      redactedExcerpt: _stringOrNull(json['redactedExcerpt']),
      redactedText: _stringOrNull(json['redactedText']),
      redactedHtml: _stringOrNull(json['redactedHtml']),
      legislature: _stringOrNull(json['legislature']),
      date: _stringOrNull(json['date']),
    );
  }
}

List<String> _stringList(dynamic value) {
  if (value is! List) return [];

  return value
      .whereType<String>()
      .map((item) => item.trim())
      .where((item) => item.isNotEmpty)
      .toList(growable: false);
}

class ProposalInteractionSubmission {
  ProposalInteractionSubmission({
    required this.initiativeId,
    required this.action,
    this.idempotencyKey,
  });

  final int initiativeId;
  final ProposalInteractionAction action;
  final String? idempotencyKey;

  Map<String, dynamic> toJson() => {
    'initiativeId': initiativeId,
    'action': action.wireName,
    if (idempotencyKey != null) 'idempotencyKey': idempotencyKey,
  };
}

class ProposalHistoryRequest {
  ProposalHistoryRequest({
    this.page = 1,
    this.pageSize = 20,
    this.legislature,
    this.interactionType,
  });

  final int page;
  final int pageSize;
  final String? legislature;
  final ProposalInteractionAction? interactionType;

  Map<String, String> toQueryParameters() {
    return {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      if (legislature != null && legislature!.trim().isNotEmpty)
        'legislature': legislature!.trim(),
      if (interactionType != null &&
          interactionType != ProposalInteractionAction.unknown)
        'interactionType': interactionType!.wireName,
    };
  }
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
        'Iniciativa sem título disponível',
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
    required this.isUnanimous,
    required this.partyVotes,
  });

  final String stageCode;
  final String stageName;
  final String? date;
  final String? description;
  final String? result;
  final bool? approved;
  final bool isUnanimous;
  final List<PartyVote> partyVotes;

  factory ParliamentaryVoteSummary.fromJson(Map<String, dynamic> json) {
    return ParliamentaryVoteSummary(
      stageCode: _stringOrFallback(json['stageCode'], ''),
      stageName: _stringOrFallback(json['stageName'], 'Votação parlamentar'),
      date: _stringOrNull(json['date']),
      description: _stringOrNull(json['description']),
      result: _stringOrNull(json['result']),
      approved: _boolOrNull(json['approved']),
      isUnanimous: _boolOrFalse(json['isUnanimous']),
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
      kind: _stringOrFallback(json['kind'], 'Fonte'),
      label: _stringOrFallback(json['label'], 'Fonte oficial'),
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
      label: _stringOrFallback(
        json['label'],
        'Acompanhar percurso da proposta',
      ),
      endpoint: _stringOrFallback(json['endpoint'], ''),
    );
  }

  factory ProposalJourneyAction.empty() {
    return ProposalJourneyAction(
      label: 'Acompanhar percurso da proposta',
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
        'Iniciativa sem título disponível',
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
        'A Assembleia registou uma fase do percurso desta iniciativa.',
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

class ProposalHistoryItem {
  ProposalHistoryItem({
    required this.interactionId,
    required this.initiativeId,
    required this.initiativeType,
    this.initiativeNumber,
    required this.title,
    this.legislature,
    required this.action,
    this.createdAtUtc,
    required this.proposers,
  });

  final int interactionId;
  final int initiativeId;
  final String initiativeType;
  final String? initiativeNumber;
  final String title;
  final String? legislature;
  final ProposalInteractionAction action;
  final String? createdAtUtc;
  final List<ProposalProposer> proposers;

  factory ProposalHistoryItem.fromJson(Map<String, dynamic> json) {
    return ProposalHistoryItem(
      interactionId: _intOrZero(json['interactionId']),
      initiativeId: _intOrZero(json['initiativeId']),
      initiativeType: _stringOrFallback(
        json['initiativeType'],
        'Iniciativa parlamentar',
      ),
      initiativeNumber: _stringOrNull(json['initiativeNumber']),
      title: _stringOrFallback(
        json['title'],
        'Iniciativa sem título disponível',
      ),
      action: ProposalInteractionAction.fromWireName(
        _stringOrNull(json['action']),
      ),
      legislature: _stringOrNull(json['legislature']),
      createdAtUtc: _stringOrNull(json['createdAtUtc']),
      proposers: _objectList(json['proposers'], ProposalProposer.fromJson),
    );
  }
}

class ProposalHistoryPage {
  ProposalHistoryPage({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.totalItems,
    required this.totalPages,
    required this.hasNextPage,
    required this.hasPreviousPage,
    required this.availableLegislatures,
  });

  final List<ProposalHistoryItem> items;
  final int page;
  final int pageSize;
  final int totalItems;
  final int totalPages;
  final bool hasNextPage;
  final bool hasPreviousPage;
  final List<String> availableLegislatures;

  factory ProposalHistoryPage.fromJson(Map<String, dynamic> json) {
    return ProposalHistoryPage(
      items: _objectList(json['items'], ProposalHistoryItem.fromJson),
      page: _intOrFallback(json['page'], 1),
      pageSize: _intOrFallback(json['pageSize'], 20),
      totalItems: _intOrZero(json['totalItems']),
      totalPages: _intOrZero(json['totalPages']),
      hasNextPage: _boolOrFalse(json['hasNextPage']),
      hasPreviousPage: _boolOrFalse(json['hasPreviousPage']),
      availableLegislatures: _stringList(json['availableLegislatures']),
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

bool? _boolOrNull(dynamic value) {
  return value is bool ? value : null;
}

bool _boolOrFalse(dynamic value) {
  return _boolOrNull(value) ?? false;
}
