import 'package:flutter/material.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/pages/proposal_journey_page.dart';
import 'package:frontend/themes/base_theme.dart';

class ProposalRevealPage extends StatelessWidget {
  const ProposalRevealPage({super.key, required this.reveal});

  final ProposalReveal reveal;

  @override
  Widget build(BuildContext context) {
    final generalityVote = reveal.generalityVote;
    final textTheme = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: baseTheme.colorScheme.surface,
      appBar: AppBar(
        backgroundColor: baseTheme.colorScheme.surface,
        foregroundColor: baseTheme.colorScheme.primary,
        elevation: 0,
        title: const Text('Resultado'),
      ),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.fromLTRB(18, 10, 18, 24),
          children: [
            _RevealHeader(reveal: reveal),
            const SizedBox(height: 18),
            _Section(
              title: 'A tua posicao',
              child: _ValueRow(
                icon: _userVoteIcon(reveal.userVote),
                label: _userVoteLabel(reveal.userVote),
                color: _userVoteColor(reveal.userVote),
              ),
            ),
            const SizedBox(height: 12),
            _Section(
              title: 'Proponente',
              child:
                  reveal.proposers.isEmpty
                      ? Text('Nao identificado', style: textTheme.bodyMedium)
                      : Wrap(
                        spacing: 8,
                        runSpacing: 8,
                        children:
                            reveal.proposers
                                .map((proposer) => _ProposerChip(proposer))
                                .toList(),
                      ),
            ),
            const SizedBox(height: 12),
            _Section(
              title: 'Votacao na generalidade',
              child:
                  generalityVote == null
                      ? Text(
                        'Ainda sem resultado parlamentar disponivel.',
                        style: textTheme.bodyMedium,
                      )
                      : _GeneralityVoteSummary(vote: generalityVote),
            ),
            if (generalityVote?.partyVotes.isNotEmpty ?? false) ...[
              const SizedBox(height: 12),
              _Section(
                title: 'Voto por partido',
                child: _PartyVoteList(votes: generalityVote!.partyVotes),
              ),
            ],
            const SizedBox(height: 18),
            FilledButton.icon(
              style: FilledButton.styleFrom(
                backgroundColor: baseTheme.colorScheme.primary,
                shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(8),
                ),
                minimumSize: const Size.fromHeight(54),
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
                  fontWeight: FontWeight.w700,
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

class _RevealHeader extends StatelessWidget {
  const _RevealHeader({required this.reveal});

  final ProposalReveal reveal;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: baseTheme.colorScheme.primary, width: 2),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _Badge(label: reveal.initiativeType),
              if (reveal.initiativeNumber != null)
                _Badge(label: reveal.initiativeNumber!),
            ],
          ),
          const SizedBox(height: 14),
          Text(
            reveal.title,
            style: textTheme.headlineSmall?.copyWith(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
              height: 1.15,
            ),
          ),
        ],
      ),
    );
  }
}

class _Section extends StatelessWidget {
  const _Section({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.black12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
            ),
          ),
          const SizedBox(height: 10),
          child,
        ],
      ),
    );
  }
}

class _ValueRow extends StatelessWidget {
  const _ValueRow({
    required this.icon,
    required this.label,
    required this.color,
  });

  final IconData icon;
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        CircleAvatar(
          radius: 18,
          backgroundColor: color,
          child: Icon(icon, color: Colors.white, size: 20),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Text(
            label,
            style: const TextStyle(fontWeight: FontWeight.w700),
          ),
        ),
      ],
    );
  }
}

class _ProposerChip extends StatelessWidget {
  const _ProposerChip(this.proposer);

  final ProposalProposer proposer;

  @override
  Widget build(BuildContext context) {
    final labels = <String>[];
    if (proposer.acronym != null) {
      labels.add(proposer.acronym!);
    }
    if (proposer.name != null) {
      labels.add(proposer.name!);
    }
    final label = labels.join(' - ');

    return Chip(
      backgroundColor: baseTheme.colorScheme.surface,
      side: BorderSide(color: baseTheme.colorScheme.primary),
      label: Text(
        label.isEmpty ? 'Proponente' : label,
        style: TextStyle(
          color: baseTheme.colorScheme.primary,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _GeneralityVoteSummary extends StatelessWidget {
  const _GeneralityVoteSummary({required this.vote});

  final ParliamentaryVoteSummary vote;

  @override
  Widget build(BuildContext context) {
    final approved = vote.approved;
    final color =
        approved == true
            ? approvedGreenBold
            : approved == false
            ? rejectedRedBold
            : Colors.grey.shade700;
    final icon =
        approved == true
            ? Icons.check
            : approved == false
            ? Icons.close
            : Icons.remove;
    final label = vote.result ?? vote.stageName;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _ValueRow(icon: icon, label: label, color: color),
        if (vote.date != null) ...[
          const SizedBox(height: 8),
          Text(vote.date!, style: Theme.of(context).textTheme.bodySmall),
        ],
      ],
    );
  }
}

class _PartyVoteList extends StatelessWidget {
  const _PartyVoteList({required this.votes});

  final List<PartyVote> votes;

  @override
  Widget build(BuildContext context) {
    return Column(
      children:
          votes
              .map(
                (vote) => Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Row(
                    children: [
                      SizedBox(
                        width: 64,
                        child: Text(
                          vote.partyAcronym,
                          style: const TextStyle(fontWeight: FontWeight.w800),
                        ),
                      ),
                      Expanded(
                        child: Text(_partyVoteLabel(vote.orientation)),
                      ),
                      if (vote.numberOfDeputies != null)
                        Text('${vote.numberOfDeputies}'),
                    ],
                  ),
                ),
              )
              .toList(),
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.primary,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Colors.white,
          fontSize: 12,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

IconData _userVoteIcon(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => Icons.check,
    ProposalInteractionAction.oppose => Icons.close,
    ProposalInteractionAction.abstain => Icons.remove,
    ProposalInteractionAction.skip => Icons.help_outline,
    ProposalInteractionAction.unknown => Icons.how_to_vote,
  };
}

Color _userVoteColor(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => approvedGreenBold,
    ProposalInteractionAction.oppose => rejectedRedBold,
    ProposalInteractionAction.abstain => Colors.grey.shade700,
    ProposalInteractionAction.skip => baseTheme.colorScheme.secondary,
    ProposalInteractionAction.unknown => baseTheme.colorScheme.primary,
  };
}

String _userVoteLabel(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => 'Apoiaste em principio',
    ProposalInteractionAction.oppose => 'Opuseste-te em principio',
    ProposalInteractionAction.abstain => 'Abstiveste-te em principio',
    ProposalInteractionAction.skip => 'Saltaste a iniciativa',
    ProposalInteractionAction.unknown => 'Escolha registada',
  };
}

String _partyVoteLabel(ParliamentaryVoteOrientation orientation) {
  return switch (orientation) {
    ParliamentaryVoteOrientation.inFavor => 'A favor',
    ParliamentaryVoteOrientation.against => 'Contra',
    ParliamentaryVoteOrientation.abstaining => 'Abstencao',
    ParliamentaryVoteOrientation.absent => 'Ausente',
    ParliamentaryVoteOrientation.notInterested => 'Sem voto',
    ParliamentaryVoteOrientation.unknown => 'Nao indicado',
  };
}
