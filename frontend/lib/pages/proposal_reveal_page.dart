import 'package:flutter/material.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/pages/proposal_journey_page.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:frontend/widgets/parliamentary_vote_breakdown.dart';

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
              ParliamentaryVoteBreakdown(
                votes: partyVotes,
                isUnanimous: vote?.isUnanimous == true,
                highlightedOrientation: orientationForUserVote(reveal.userVote),
                style: ParliamentaryVoteBreakdownStyle.reveal,
                showAbsent: false,
                emptyLabel: 'Votos por partido ainda indisponíveis.',
              ),
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
      return const ParliamentaryPartyLogo(
        acronym: '?',
        size: 112,
        fallbackLabel: '?',
      );
    }

    final uniqueProposers = <String, ProposalProposer>{};
    for (final proposer in proposers) {
      final label = proposer.acronym ?? proposer.name ?? '?';
      final key = _partyKey(label);
      uniqueProposers.putIfAbsent(key.isEmpty ? label : key, () => proposer);
    }

    final knownLogoProposers =
        uniqueProposers.values.where((proposer) {
          final label = proposer.acronym ?? proposer.name ?? '';
          return partyLogoAsset(label) != null;
        }).toList();
    final visibleProposers =
        knownLogoProposers.isNotEmpty
            ? knownLogoProposers
            : uniqueProposers.values.toList();

    return Wrap(
      alignment: WrapAlignment.center,
      spacing: 18,
      runSpacing: 18,
      children:
          visibleProposers
              .take(2)
              .map(
                (proposer) => ParliamentaryPartyLogo(
                  acronym: proposer.acronym ?? proposer.name ?? '?',
                  size: 152,
                  fallbackLabel: proposer.acronym ?? proposer.name ?? '?',
                ),
              )
              .toList(),
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
          BoxShadow(color: Colors.black26, blurRadius: 3, offset: Offset(0, 2)),
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

String _partyKey(String value) {
  return value.toUpperCase().replaceAll(RegExp(r'[^A-Z0-9]'), '');
}
