import 'package:flutter/material.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/pages/proposal_journey_page.dart';
import 'package:frontend/themes/base_theme.dart';

class ProposalRevealPage extends StatelessWidget {
  const ProposalRevealPage({super.key, required this.reveal});

  final ProposalReveal reveal;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: baseTheme.colorScheme.surface,
      appBar: AppBar(
        backgroundColor: baseTheme.colorScheme.surface,
        foregroundColor: baseTheme.colorScheme.primary,
        elevation: 0,
        title: const Text('Resultados'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(18, 10, 18, 24),
          children: [
            _OutcomePanel(reveal: reveal),
            const SizedBox(height: 14),
            FilledButton.icon(
              style: FilledButton.styleFrom(
                backgroundColor: baseTheme.colorScheme.primary,
                minimumSize: const Size.fromHeight(54),
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
              ),
              onPressed:
                  () => Navigator.of(context).push(
                    MaterialPageRoute(
                      builder:
                          (context) => ProposalJourneyPage(
                            initiativeId: reveal.initiativeId,
                          ),
                    ),
                  ),
              icon: const Icon(Icons.timeline, color: Colors.white),
              label: Text(
                reveal.journey.label,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.center,
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w800,
                ),
              ),
            ),
            const SizedBox(height: 10),
            TextButton(
              style: buttonStyle,
              onPressed: () => Navigator.of(context).pop(true),
              child: const Text(
                'Ver outra iniciativa',
                style: TextStyle(color: Colors.white),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _OutcomePanel extends StatelessWidget {
  const _OutcomePanel({required this.reveal});

  final ProposalReveal reveal;

  @override
  Widget build(BuildContext context) {
    final vote = reveal.generalityVote;
    final approved = vote?.approved;
    final outcomeColor = _outcomeColor(approved);
    final panelColor = _panelColor(approved);
    final partyVotes = vote?.partyVotes ?? [];

    return Stack(
      clipBehavior: Clip.none,
      alignment: Alignment.topCenter,
      children: [
        Container(
          margin: const EdgeInsets.only(top: 24),
          width: double.infinity,
          padding: const EdgeInsets.fromLTRB(18, 64, 18, 24),
          decoration: BoxDecoration(
            color: panelColor,
            borderRadius: BorderRadius.circular(34),
          ),
          child: Column(
            children: [
              _OutcomeHero(
                label: _outcomeLabel(approved, vote?.result),
                color: outcomeColor,
                icon: _outcomeIcon(approved),
              ),
              const SizedBox(height: 34),
              const _FloatingLabel('Proposto por:'),
              const SizedBox(height: 14),
              _ProposerLogoStrip(proposers: reveal.proposers),
              const SizedBox(height: 26),
              _PartyVoteGroups(votes: partyVotes, userVote: reveal.userVote),
              const SizedBox(height: 18),
              Text(
                reveal.title,
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Colors.white.withValues(alpha: 0.92),
                  fontWeight: FontWeight.w700,
                  height: 1.25,
                ),
              ),
            ],
          ),
        ),
        Positioned(
          top: 0,
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 64, vertical: 10),
            decoration: BoxDecoration(
              color: outcomeColor,
              borderRadius: BorderRadius.circular(16),
              boxShadow: const [
                BoxShadow(
                  color: Colors.black26,
                  blurRadius: 4,
                  offset: Offset(0, 2),
                ),
              ],
            ),
            child: const Text(
              'Resultados',
              style: TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.w700,
              ),
            ),
          ),
        ),
      ],
    );
  }
}

class _OutcomeHero extends StatelessWidget {
  const _OutcomeHero({
    required this.label,
    required this.color,
    required this.icon,
  });

  final String label;
  final Color color;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Container(
          width: 72,
          height: 72,
          decoration: BoxDecoration(
            color: color,
            borderRadius: BorderRadius.circular(22),
          ),
          child: Icon(icon, color: Colors.white, size: 46),
        ),
        const SizedBox(width: 22),
        Flexible(
          child: Text(
            label,
            maxLines: 2,
            overflow: TextOverflow.ellipsis,
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
              color: Colors.white,
              fontWeight: FontWeight.w900,
            ),
          ),
        ),
      ],
    );
  }
}

class _ProposerLogoStrip extends StatelessWidget {
  const _ProposerLogoStrip({required this.proposers});

  final List<ProposalProposer> proposers;

  @override
  Widget build(BuildContext context) {
    if (proposers.isEmpty) {
      return const _FallbackLogo(label: '?', size: 112);
    }

    final uniqueProposers = <String, ProposalProposer>{};
    for (final proposer in proposers) {
      final label = proposer.acronym ?? proposer.name ?? '?';
      final key = _partyKey(label);
      uniqueProposers.putIfAbsent(key.isEmpty ? label : key, () => proposer);
    }

    return Wrap(
      alignment: WrapAlignment.center,
      spacing: 18,
      runSpacing: 18,
      children:
          uniqueProposers.values
              .take(2)
              .map(
                (proposer) => _PartyLogo(
                  acronym: proposer.acronym ?? proposer.name ?? '?',
                  size: 152,
                  fallbackLabel: proposer.acronym ?? proposer.name ?? '?',
                ),
              )
              .toList(),
    );
  }
}

class _PartyVoteGroups extends StatelessWidget {
  const _PartyVoteGroups({required this.votes, required this.userVote});

  final List<PartyVote> votes;
  final ProposalInteractionAction userVote;

  @override
  Widget build(BuildContext context) {
    final favor = _byOrientation(ParliamentaryVoteOrientation.inFavor);
    final abstain = _byOrientation(ParliamentaryVoteOrientation.abstaining);
    final against = _byOrientation(ParliamentaryVoteOrientation.against);
    final absent = _byOrientation(ParliamentaryVoteOrientation.absent);
    final splitParties = _splitPartyAcronyms();
    final highlightedOrientation = _orientationForUserVote(userVote);

    if (votes.isEmpty) {
      return const Text(
        'Votos por partido ainda indisponiveis.',
        textAlign: TextAlign.center,
        style: TextStyle(color: Colors.white, fontWeight: FontWeight.w700),
      );
    }

    return Column(
      children: [
        if (favor.isNotEmpty)
          _PartyVoteGroup(
            icon: Icons.check,
            color: approvedGreenBold,
            votes: favor,
            label: 'A favor',
            splitParties: splitParties,
            isHighlighted:
                highlightedOrientation == ParliamentaryVoteOrientation.inFavor,
          ),
        if (abstain.isNotEmpty) ...[
          const _DividerLine(),
          _PartyVoteGroup(
            icon: Icons.remove,
            color: Colors.grey.shade500,
            votes: abstain,
            label: 'Abstencao',
            splitParties: splitParties,
            isHighlighted:
                highlightedOrientation ==
                ParliamentaryVoteOrientation.abstaining,
          ),
        ],
        if (against.isNotEmpty) ...[
          const _DividerLine(),
          _PartyVoteGroup(
            icon: Icons.close,
            color: rejectedRedBold,
            votes: against,
            label: 'Contra',
            splitParties: splitParties,
            isHighlighted:
                highlightedOrientation == ParliamentaryVoteOrientation.against,
          ),
        ],
        if (absent.isNotEmpty) ...[
          const _DividerLine(),
          _PartyVoteGroup(
            icon: Icons.help_outline,
            color: Colors.grey.shade700,
            votes: absent,
            label: 'Ausentes',
            splitParties: splitParties,
            isHighlighted:
                highlightedOrientation == ParliamentaryVoteOrientation.absent,
          ),
        ],
      ],
    );
  }

  List<PartyVote> _byOrientation(ParliamentaryVoteOrientation orientation) {
    return votes.where((vote) => vote.orientation == orientation).toList();
  }

  Set<String> _splitPartyAcronyms() {
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
}

class _PartyVoteGroup extends StatelessWidget {
  const _PartyVoteGroup({
    required this.icon,
    required this.color,
    required this.votes,
    required this.label,
    required this.splitParties,
    required this.isHighlighted,
  });

  final IconData icon;
  final Color color;
  final List<PartyVote> votes;
  final String label;
  final Set<String> splitParties;
  final bool isHighlighted;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.symmetric(vertical: 8),
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
            child: Icon(icon, color: Colors.white, size: isHighlighted ? 54 : 48),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: const TextStyle(
                    fontWeight: FontWeight.w800,
                    color: Colors.black87,
                  ),
                ),
                const SizedBox(height: 8),
                Wrap(
                  spacing: 10,
                  runSpacing: 10,
                  children:
                      votes
                          .map(
                            (vote) => _PartyVoteLogo(
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
    );
  }
}

class _PartyVoteLogo extends StatelessWidget {
  const _PartyVoteLogo({required this.vote, required this.isSplit});

  final PartyVote vote;
  final bool isSplit;

  @override
  Widget build(BuildContext context) {
    final count = vote.numberOfDeputies;

    return Stack(
      clipBehavior: Clip.none,
      children: [
        _PartyLogo(acronym: vote.partyAcronym, size: 76),
        if (count != null)
          Positioned(
            right: -9,
            top: -9,
            child: _SmallBadge(
              label: count.toString(),
              backgroundColor: baseTheme.colorScheme.primary,
            ),
          ),
        if (isSplit)
          Positioned(
            left: -7,
            bottom: -8,
            child: _SmallBadge(
              label: 'div.',
              backgroundColor: Colors.black87,
            ),
          ),
      ],
    );
  }
}

class _SmallBadge extends StatelessWidget {
  const _SmallBadge({required this.label, required this.backgroundColor});

  final String label;
  final Color backgroundColor;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 5, vertical: 2),
      decoration: BoxDecoration(
        color: backgroundColor,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.white, width: 1.5),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Colors.white,
          fontSize: 9,
          fontWeight: FontWeight.w900,
        ),
      ),
    );
  }
}

class _FloatingLabel extends StatelessWidget {
  const _FloatingLabel(this.label);

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 38, vertical: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(18),
        boxShadow: const [
          BoxShadow(
            color: Colors.black26,
            blurRadius: 3,
            offset: Offset(0, 2),
          ),
        ],
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Colors.black87,
          fontSize: 18,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _DividerLine extends StatelessWidget {
  const _DividerLine();

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 1,
      margin: const EdgeInsets.symmetric(horizontal: 34, vertical: 8),
      color: Colors.white.withValues(alpha: 0.72),
    );
  }
}

class _PartyLogo extends StatelessWidget {
  const _PartyLogo({
    required this.acronym,
    required this.size,
    this.fallbackLabel,
  });

  final String acronym;
  final double size;
  final String? fallbackLabel;

  @override
  Widget build(BuildContext context) {
    final asset = _partyLogoAsset(acronym);
    if (asset == null) {
      return _FallbackLogo(
        label: fallbackLabel ?? acronym,
        size: size,
      );
    }

    return Container(
      width: size,
      height: size * 0.68,
      padding: const EdgeInsets.all(2),
      child: Image.asset(
        asset,
        fit: BoxFit.contain,
        errorBuilder:
            (context, error, stackTrace) => _FallbackLogo(
              label: fallbackLabel ?? acronym,
              size: size,
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

Color _outcomeColor(bool? approved) {
  if (approved == true) {
    return approvedGreenBold;
  }
  if (approved == false) {
    return rejectedRedBold;
  }
  return Colors.grey.shade700;
}

Color _panelColor(bool? approved) {
  if (approved == true) {
    return approvedGreenNormal.withValues(alpha: 0.78);
  }
  if (approved == false) {
    return rejectedRedNormal;
  }
  return Colors.grey.shade300;
}

IconData _outcomeIcon(bool? approved) {
  if (approved == true) {
    return Icons.check;
  }
  if (approved == false) {
    return Icons.close;
  }
  return Icons.remove;
}

String _outcomeLabel(bool? approved, String? rawResult) {
  if (approved == true) {
    return 'Aprovado';
  }
  if (approved == false) {
    return 'Rejeitado';
  }
  return rawResult ?? 'Sem resultado';
}

ParliamentaryVoteOrientation? _orientationForUserVote(
  ProposalInteractionAction action,
) {
  return switch (action) {
    ProposalInteractionAction.support => ParliamentaryVoteOrientation.inFavor,
    ProposalInteractionAction.oppose => ParliamentaryVoteOrientation.against,
    ProposalInteractionAction.abstain => ParliamentaryVoteOrientation.abstaining,
    ProposalInteractionAction.skip => null,
    ProposalInteractionAction.unknown => null,
  };
}

String? _partyLogoAsset(String rawAcronym) {
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
