import 'package:flutter/material.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/themes/base_theme.dart';

enum ParliamentaryVoteBreakdownStyle { reveal, compact }

class ParliamentaryVoteBreakdown extends StatelessWidget {
  const ParliamentaryVoteBreakdown({
    super.key,
    required this.votes,
    this.isUnanimous = false,
    this.highlightedOrientation,
    this.showHighlightedWhenEmpty = false,
    this.style = ParliamentaryVoteBreakdownStyle.compact,
    this.showAbsent = true,
    this.emptyLabel = 'Votos por partido ainda indisponíveis.',
  });

  final List<PartyVote> votes;
  final bool isUnanimous;
  final ParliamentaryVoteOrientation? highlightedOrientation;
  final bool showHighlightedWhenEmpty;
  final ParliamentaryVoteBreakdownStyle style;
  final bool showAbsent;
  final String emptyLabel;

  @override
  Widget build(BuildContext context) {
    final groupedVotes = _groupVotes(votes, showAbsent: showAbsent);
    final visibleGroupedVotes = _visibleGroupedVotes(groupedVotes, style);
    final hasVisibleGroupedVotes = visibleGroupedVotes.values.any(
      (orientationVotes) => orientationVotes.isNotEmpty,
    );
    final shouldShowEmptyHighlightedGroup =
        style == ParliamentaryVoteBreakdownStyle.reveal &&
        showHighlightedWhenEmpty &&
        !isUnanimous &&
        highlightedOrientation != null &&
        visibleGroupedVotes[highlightedOrientation!]?.isNotEmpty != true;

    if (!hasVisibleGroupedVotes &&
        !isUnanimous &&
        !shouldShowEmptyHighlightedGroup) {
      return Text(
        emptyLabel,
        textAlign: TextAlign.center,
        style: TextStyle(
          color:
              style == ParliamentaryVoteBreakdownStyle.reveal
                  ? Colors.white
                  : Colors.black54,
          fontWeight: FontWeight.w700,
        ),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        if (isUnanimous) ...[
          const UnanimousVoteNotice(),
          if (hasVisibleGroupedVotes) const SizedBox(height: 10),
        ],
        for (final orientation in _voteOrientationOrder)
          if (visibleGroupedVotes[orientation]?.isNotEmpty == true ||
              shouldShowEmptyHighlightedGroup &&
                  orientation == highlightedOrientation)
            _buildGroup(orientation, visibleGroupedVotes[orientation] ?? []),
      ],
    );
  }

  Widget _buildGroup(
    ParliamentaryVoteOrientation orientation,
    List<PartyVote> orientationVotes,
  ) {
    return switch (style) {
      ParliamentaryVoteBreakdownStyle.reveal => _RevealPartyVoteGroup(
        orientation: orientation,
        votes: orientationVotes,
        isHighlighted: highlightedOrientation == orientation,
        splitParties: _splitPartyAcronyms(votes),
      ),
      ParliamentaryVoteBreakdownStyle.compact => _CompactPartyVoteGroup(
        orientation: orientation,
        votes: orientationVotes,
      ),
    };
  }
}

class UnanimousVoteNotice extends StatelessWidget {
  const UnanimousVoteNotice({super.key});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.9),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: approvedGreenBold.withValues(alpha: 0.45)),
      ),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            decoration: BoxDecoration(
              color: approvedGreenBold,
              borderRadius: BorderRadius.circular(8),
            ),
            child: const Icon(Icons.done_all, color: Colors.white, size: 22),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Votação unânime',
                  style: TextStyle(
                    color: baseTheme.colorScheme.primary,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 2),
                const Text(
                  'Todos os grupos parlamentares votaram da mesma forma.',
                  style: TextStyle(
                    color: Colors.black87,
                    fontSize: 12,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _RevealPartyVoteGroup extends StatelessWidget {
  const _RevealPartyVoteGroup({
    required this.orientation,
    required this.votes,
    required this.isHighlighted,
    required this.splitParties,
  });

  final ParliamentaryVoteOrientation orientation;
  final List<PartyVote> votes;
  final bool isHighlighted;
  final Set<String> splitParties;

  @override
  Widget build(BuildContext context) {
    final color = _orientationColor(orientation);

    return Stack(
      clipBehavior: Clip.none,
      alignment: Alignment.topCenter,
      children: [
        Container(
          width: double.infinity,
          margin: EdgeInsets.only(top: isHighlighted ? 22 : 8, bottom: 8),
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: Colors.white.withValues(alpha: isHighlighted ? 0.78 : 0.55),
            borderRadius: BorderRadius.circular(28),
            border:
                isHighlighted
                    ? Border.all(color: Colors.white, width: 3)
                    : Border.all(color: Colors.white.withValues(alpha: 0.18)),
            boxShadow:
                isHighlighted
                    ? [
                      BoxShadow(
                        color: color.withValues(alpha: 0.28),
                        blurRadius: 18,
                        spreadRadius: 2,
                      ),
                    ]
                    : null,
          ),
          child: Row(
            children: [
              Container(
                width: isHighlighted ? 86 : 78,
                height: isHighlighted ? 86 : 78,
                decoration: BoxDecoration(color: color, shape: BoxShape.circle),
                child: Icon(
                  _orientationIcon(orientation),
                  color: Colors.white,
                  size: isHighlighted ? 54 : 48,
                ),
              ),
              const SizedBox(width: 14),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (!isHighlighted) ...[
                      Text(
                        _orientationLabel(orientation),
                        style: const TextStyle(
                          fontWeight: FontWeight.w800,
                          color: Colors.black87,
                        ),
                      ),
                      const SizedBox(height: 8),
                    ],
                    Wrap(
                      spacing: 10,
                      runSpacing: 10,
                      children:
                          votes
                              .map(
                                (vote) => _PartyLogoWithBadges(
                                  vote: vote,
                                  isSplit: splitParties.contains(
                                    _partyKey(vote.partyAcronym),
                                  ),
                                ),
                              )
                              .toList(),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
        if (isHighlighted) const Positioned(top: 0, child: _VoteWithLabel()),
      ],
    );
  }
}

class _CompactPartyVoteGroup extends StatelessWidget {
  const _CompactPartyVoteGroup({
    required this.orientation,
    required this.votes,
  });

  final ParliamentaryVoteOrientation orientation;
  final List<PartyVote> votes;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 34,
            height: 34,
            decoration: BoxDecoration(
              color: _orientationColor(orientation),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(_orientationIcon(orientation), color: Colors.white),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  _orientationLabel(orientation),
                  style: const TextStyle(
                    color: Colors.black87,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 6),
                Wrap(
                  spacing: 6,
                  runSpacing: 6,
                  children:
                      votes.map((vote) => _PartyVoteChip(vote: vote)).toList(),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _VoteWithLabel extends StatelessWidget {
  const _VoteWithLabel();

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 42, vertical: 7),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        boxShadow: const [
          BoxShadow(color: Colors.black26, blurRadius: 3, offset: Offset(0, 2)),
        ],
      ),
      child: const Text(
        'Votaste com:',
        style: TextStyle(
          color: Colors.black87,
          fontSize: 14,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _PartyLogoWithBadges extends StatelessWidget {
  const _PartyLogoWithBadges({required this.vote, required this.isSplit});

  final PartyVote vote;
  final bool isSplit;

  @override
  Widget build(BuildContext context) {
    final count = vote.numberOfDeputies;

    return Stack(
      clipBehavior: Clip.none,
      children: [
        ParliamentaryPartyLogo(acronym: vote.partyAcronym, size: 76),
        if (count != null)
          Positioned(
            right: -11,
            top: -12,
            child: _SmallBadge(
              label: count.toString(),
              backgroundColor: baseTheme.colorScheme.primary,
              isProminent: true,
            ),
          ),
        if (isSplit)
          const Positioned(
            left: -7,
            bottom: -8,
            child: _SmallBadge(label: 'div.', backgroundColor: Colors.black87),
          ),
      ],
    );
  }
}

class ParliamentaryPartyLogo extends StatelessWidget {
  const ParliamentaryPartyLogo({
    super.key,
    required this.acronym,
    required this.size,
    this.fallbackLabel,
  });

  final String acronym;
  final double size;
  final String? fallbackLabel;

  @override
  Widget build(BuildContext context) {
    final asset = partyLogoAsset(acronym);
    if (asset == null) {
      return _FallbackLogo(label: fallbackLabel ?? acronym, size: size);
    }

    return Container(
      width: size,
      height: size * 0.68,
      padding: const EdgeInsets.all(2),
      child: Image.asset(
        asset,
        fit: BoxFit.contain,
        errorBuilder:
            (context, error, stackTrace) =>
                _FallbackLogo(label: fallbackLabel ?? acronym, size: size),
      ),
    );
  }
}

class _PartyVoteChip extends StatelessWidget {
  const _PartyVoteChip({required this.vote});

  final PartyVote vote;

  @override
  Widget build(BuildContext context) {
    final count = vote.numberOfDeputies;
    final label =
        count == null ? vote.partyAcronym : '${vote.partyAcronym} $count';

    return Container(
      constraints: const BoxConstraints(minHeight: 30),
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.black12),
      ),
      child: Text(
        label,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: baseTheme.colorScheme.primary,
          fontSize: 12,
          fontWeight: FontWeight.w900,
        ),
      ),
    );
  }
}

class _SmallBadge extends StatelessWidget {
  const _SmallBadge({
    required this.label,
    required this.backgroundColor,
    this.isProminent = false,
  });

  final String label;
  final Color backgroundColor;
  final bool isProminent;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: BoxConstraints(
        minWidth: isProminent ? 24 : 0,
        minHeight: isProminent ? 22 : 0,
      ),
      alignment: Alignment.center,
      padding: EdgeInsets.symmetric(
        horizontal: isProminent ? 7 : 5,
        vertical: isProminent ? 3 : 2,
      ),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(isProminent ? 12 : 8),
        border: Border.all(color: Colors.white, width: isProminent ? 2 : 1.5),
      ),
      child: Text(
        label,
        style: TextStyle(
          color: Colors.white,
          fontSize: isProminent ? 13 : 9,
          fontWeight: FontWeight.w900,
        ),
      ),
    );
  }
}

class _FallbackLogo extends StatelessWidget {
  const _FallbackLogo({required this.label, required this.size});

  final String label;
  final double size;

  @override
  Widget build(BuildContext context) {
    final text = label.trim().isEmpty ? '?' : label.trim();
    return Container(
      width: size,
      height: size * 0.62,
      alignment: Alignment.center,
      padding: const EdgeInsets.symmetric(horizontal: 6),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(4),
        border: Border.all(color: Colors.black12),
      ),
      child: Text(
        text,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        textAlign: TextAlign.center,
        style: TextStyle(
          color: baseTheme.colorScheme.primary,
          fontWeight: FontWeight.w900,
          fontSize: size > 70 ? 22 : 12,
        ),
      ),
    );
  }
}

Map<ParliamentaryVoteOrientation, List<PartyVote>> _visibleGroupedVotes(
  Map<ParliamentaryVoteOrientation, List<PartyVote>> groupedVotes,
  ParliamentaryVoteBreakdownStyle style,
) {
  if (style != ParliamentaryVoteBreakdownStyle.reveal) {
    return groupedVotes;
  }

  return groupedVotes.map(
    (orientation, orientationVotes) => MapEntry(
      orientation,
      orientationVotes
          .where((vote) => partyLogoAsset(vote.partyAcronym) != null)
          .toList(),
    ),
  );
}

Map<ParliamentaryVoteOrientation, List<PartyVote>> _groupVotes(
  List<PartyVote> votes, {
  required bool showAbsent,
}) {
  final groupedVotes = <ParliamentaryVoteOrientation, List<PartyVote>>{};
  for (final vote in votes) {
    if (!showAbsent &&
        vote.orientation == ParliamentaryVoteOrientation.absent) {
      continue;
    }
    if (vote.partyAcronym.trim().isEmpty) {
      continue;
    }

    groupedVotes.putIfAbsent(vote.orientation, () => []).add(vote);
  }
  return groupedVotes;
}

Set<String> _splitPartyAcronyms(List<PartyVote> votes) {
  final orientationsByParty = <String, Set<ParliamentaryVoteOrientation>>{};
  for (final vote in votes) {
    final key = _partyKey(vote.partyAcronym);
    if (key.isEmpty) {
      continue;
    }

    orientationsByParty
        .putIfAbsent(key, () => <ParliamentaryVoteOrientation>{})
        .add(vote.orientation);
  }

  return orientationsByParty.entries
      .where((entry) => entry.value.length > 1)
      .map((entry) => entry.key)
      .toSet();
}

const _voteOrientationOrder = [
  ParliamentaryVoteOrientation.inFavor,
  ParliamentaryVoteOrientation.abstaining,
  ParliamentaryVoteOrientation.against,
  ParliamentaryVoteOrientation.absent,
  ParliamentaryVoteOrientation.notInterested,
  ParliamentaryVoteOrientation.unknown,
];

String _orientationLabel(ParliamentaryVoteOrientation orientation) {
  return switch (orientation) {
    ParliamentaryVoteOrientation.inFavor => 'A favor',
    ParliamentaryVoteOrientation.abstaining => 'Abstenção',
    ParliamentaryVoteOrientation.against => 'Contra',
    ParliamentaryVoteOrientation.absent => 'Ausências',
    ParliamentaryVoteOrientation.notInterested => 'Sem interesse',
    ParliamentaryVoteOrientation.unknown => 'Outro',
  };
}

IconData _orientationIcon(ParliamentaryVoteOrientation orientation) {
  return switch (orientation) {
    ParliamentaryVoteOrientation.inFavor => Icons.check,
    ParliamentaryVoteOrientation.abstaining => Icons.remove,
    ParliamentaryVoteOrientation.against => Icons.close,
    ParliamentaryVoteOrientation.absent => Icons.person_off_outlined,
    ParliamentaryVoteOrientation.notInterested => Icons.block_outlined,
    ParliamentaryVoteOrientation.unknown => Icons.help_outline,
  };
}

Color _orientationColor(ParliamentaryVoteOrientation orientation) {
  return switch (orientation) {
    ParliamentaryVoteOrientation.inFavor => approvedGreenBold,
    ParliamentaryVoteOrientation.abstaining => Colors.grey.shade600,
    ParliamentaryVoteOrientation.against => rejectedRedBold,
    ParliamentaryVoteOrientation.absent => Colors.grey.shade700,
    ParliamentaryVoteOrientation.notInterested => Colors.black54,
    ParliamentaryVoteOrientation.unknown => baseTheme.colorScheme.secondary,
  };
}

ParliamentaryVoteOrientation? orientationForUserVote(
  ProposalInteractionAction action,
) {
  return switch (action) {
    ProposalInteractionAction.support => ParliamentaryVoteOrientation.inFavor,
    ProposalInteractionAction.oppose => ParliamentaryVoteOrientation.against,
    ProposalInteractionAction.abstain =>
      ParliamentaryVoteOrientation.abstaining,
    ProposalInteractionAction.skip => null,
    ProposalInteractionAction.unknown => null,
  };
}

String? partyLogoAsset(String rawAcronym) {
  final acronym = _partyKey(rawAcronym);
  if (acronym.contains('CDSPP')) {
    return 'lib/images/CDSPP_logo.png';
  }
  if (acronym.contains('PSD') || acronym.contains('PPDPSD')) {
    return 'lib/images/PSD_logo.png';
  }
  if (acronym.contains('PCP')) {
    return 'lib/images/PCP_logo.png';
  }
  if (acronym == 'PS') {
    return 'lib/images/PS_logo.png';
  }
  if (acronym == 'BE') {
    return 'lib/images/BE_logo.png';
  }
  if (acronym == 'CH' || acronym.contains('CHEGA')) {
    return 'lib/images/CH_logo.png';
  }
  if (acronym == 'IL') {
    return 'lib/images/IL_logo.png';
  }
  if (acronym == 'PAN') {
    return 'lib/images/PAN_logo.png';
  }
  if (acronym == 'L') {
    return 'lib/images/L_logo.png';
  }
  return null;
}

String _partyKey(String value) {
  return value.toUpperCase().replaceAll(RegExp(r'[^A-Z0-9]'), '');
}
