import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/themes/base_theme.dart';

class ProposalJourneyPage extends StatefulWidget {
  const ProposalJourneyPage({
    super.key,
    required this.initiativeId,
    VoteController? voteController,
  }) : _voteController = voteController;

  final int initiativeId;
  final VoteController? _voteController;

  @override
  State<ProposalJourneyPage> createState() => _ProposalJourneyPageState();
}

class _ProposalJourneyPageState extends State<ProposalJourneyPage> {
  late final VoteController _voteController =
      widget._voteController ?? VoteController();
  late final Future<ProposalJourney> _journeyFuture = _voteController
      .getProposalJourney(widget.initiativeId);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: baseTheme.colorScheme.surface,
      appBar: AppBar(
        backgroundColor: baseTheme.colorScheme.surface,
        foregroundColor: baseTheme.colorScheme.primary,
        elevation: 0,
        title: const Text('Percurso'),
      ),
      body: SafeArea(
        child: FutureBuilder<ProposalJourney>(
          future: _journeyFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError || snapshot.data == null) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text(
                    'Nao foi possivel carregar o percurso da proposta.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                ),
              );
            }

            return _JourneyContent(journey: snapshot.data!);
          },
        ),
      ),
    );
  }
}

class _JourneyContent extends StatelessWidget {
  const _JourneyContent({required this.journey});

  final ProposalJourney journey;

  @override
  Widget build(BuildContext context) {
    final phases = journey.phases;

    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 10, 18, 24),
      children: [
        _JourneyHeader(journey: journey),
        const SizedBox(height: 18),
        if (phases.isEmpty)
          const _EmptyTimeline()
        else
          _JourneyTimeline(phases: phases),
      ],
    );
  }
}

class _JourneyHeader extends StatelessWidget {
  const _JourneyHeader({required this.journey});

  final ProposalJourney journey;

  @override
  Widget build(BuildContext context) {
    final meta = [
      journey.initiativeType,
      if (journey.initiativeNumber != null) journey.initiativeNumber!,
    ];

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: meta.map((label) => _MetaBadge(label)).toList(),
        ),
        const SizedBox(height: 12),
        Text(
          journey.title,
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
            color: baseTheme.colorScheme.primary,
            fontWeight: FontWeight.w900,
            height: 1.15,
          ),
        ),
      ],
    );
  }
}

class _JourneyTimeline extends StatefulWidget {
  const _JourneyTimeline({required this.phases});

  final List<ProposalJourneyPhase> phases;

  @override
  State<_JourneyTimeline> createState() => _JourneyTimelineState();
}

class _JourneyTimelineState extends State<_JourneyTimeline>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;

  @override
  void initState() {
    super.initState();
    final duration = 700 + widget.phases.length * 180;
    _controller = AnimationController(
      vsync: this,
      duration: Duration(milliseconds: duration.clamp(1000, 2400)),
    )..forward();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return AnimatedBuilder(
      animation: _controller,
      builder: (context, child) {
        final progress = Curves.easeOutCubic.transform(_controller.value);

        return Stack(
          children: [
            Positioned.fill(
              left: 23,
              right: null,
              child: SizedBox(
                width: 2,
                child: CustomPaint(
                  painter: _TimelineLinePainter(progress: progress),
                ),
              ),
            ),
            Column(
              children: [
                for (var index = 0; index < widget.phases.length; index++)
                  _AnimatedPhaseRow(
                    phase: widget.phases[index],
                    index: index,
                    total: widget.phases.length,
                    timelineProgress: progress,
                  ),
              ],
            ),
          ],
        );
      },
    );
  }
}

class _AnimatedPhaseRow extends StatelessWidget {
  const _AnimatedPhaseRow({
    required this.phase,
    required this.index,
    required this.total,
    required this.timelineProgress,
  });

  final ProposalJourneyPhase phase;
  final int index;
  final int total;
  final double timelineProgress;

  @override
  Widget build(BuildContext context) {
    final phaseStart = total <= 1 ? 0.0 : index / total;
    final localProgress = ((timelineProgress - phaseStart) * total * 1.35)
        .clamp(0.0, 1.0);
    final curved = Curves.easeOutCubic.transform(localProgress);

    return Opacity(
      opacity: curved,
      child: Transform.translate(
        offset: Offset(0, 18 * (1 - curved)),
        child: Padding(
          padding: const EdgeInsets.only(bottom: 14),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              SizedBox(
                width: 48,
                child: _TimelineDot(
                  isReached: timelineProgress >= phaseStart,
                  icon: _phaseIcon(phase),
                ),
              ),
              Expanded(
                child: _JourneyPhaseTile(
                  phase: phase,
                  initiallyExpanded: index == 0,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _JourneyPhaseTile extends StatelessWidget {
  const _JourneyPhaseTile({
    required this.phase,
    required this.initiallyExpanded,
  });

  final ProposalJourneyPhase phase;
  final bool initiallyExpanded;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.black12),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.05),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Theme(
        data: Theme.of(context).copyWith(dividerColor: Colors.transparent),
        child: ExpansionTile(
          initiallyExpanded: initiallyExpanded,
          tilePadding: const EdgeInsets.fromLTRB(14, 8, 10, 8),
          childrenPadding: const EdgeInsets.fromLTRB(14, 0, 14, 14),
          iconColor: baseTheme.colorScheme.primary,
          collapsedIconColor: baseTheme.colorScheme.primary,
          title: Text(
            phase.phaseName,
            maxLines: 3,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w900,
              height: 1.15,
            ),
          ),
          subtitle: Padding(
            padding: const EdgeInsets.only(top: 6),
            child: _PhaseSubtitle(phase: phase),
          ),
          children: [_ExpandedPhaseDetails(phase: phase)],
        ),
      ),
    );
  }
}

class _PhaseSubtitle extends StatelessWidget {
  const _PhaseSubtitle({required this.phase});

  final ProposalJourneyPhase phase;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        if (phase.date != null) _MetaBadge(phase.date!),
        if (phase.status != null) _MetaBadge(phase.status!),
        if (phase.phaseCode != null) _MetaBadge('Fase ${phase.phaseCode}'),
      ],
    );
  }
}

class _ExpandedPhaseDetails extends StatelessWidget {
  const _ExpandedPhaseDetails({required this.phase});

  final ProposalJourneyPhase phase;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(height: 4),
        _DetailText(label: 'Resumo', value: phase.summary),
        if (phase.observation != null)
          _DetailText(label: 'Observacao', value: phase.observation!),
        if (phase.approvedTextId != null)
          _DetailText(label: 'Texto aprovado', value: phase.approvedTextId!),
        _VoteSection(votes: phase.votes),
        _LinksSection(
          title: 'Documentos oficiais',
          icon: Icons.description_outlined,
          links: phase.documents,
          emptyLabel: 'Sem documentos associados a esta fase.',
        ),
        _LinksSection(
          title: 'Diario da Assembleia',
          icon: Icons.article_outlined,
          links: phase.diaryLinks,
          emptyLabel: 'Sem links de diario nesta fase.',
        ),
        _LinksSection(
          title: 'Transcricoes de debate',
          icon: Icons.forum_outlined,
          links: phase.transcripts,
          emptyLabel: 'Sem transcricoes associadas.',
        ),
        _VideosSection(videos: phase.videos),
      ],
    );
  }
}

class _VoteSection extends StatelessWidget {
  const _VoteSection({required this.votes});

  final List<ParliamentaryVoteSummary> votes;

  @override
  Widget build(BuildContext context) {
    return _DetailSection(
      title: 'Votacao parlamentar',
      icon: Icons.how_to_vote_outlined,
      child:
          votes.isEmpty
              ? const _EmptyDetail('Sem votacao registada nesta fase.')
              : Column(
                children:
                    votes
                        .map(
                          (vote) => Padding(
                            padding: const EdgeInsets.only(bottom: 10),
                            child: _VoteSummaryBlock(vote: vote),
                          ),
                        )
                        .toList(),
              ),
    );
  }
}

class _VoteSummaryBlock extends StatelessWidget {
  const _VoteSummaryBlock({required this.vote});

  final ParliamentaryVoteSummary vote;

  @override
  Widget build(BuildContext context) {
    final groups = <ParliamentaryVoteOrientation, List<PartyVote>>{};
    for (final partyVote in vote.partyVotes) {
      groups.putIfAbsent(partyVote.orientation, () => []).add(partyVote);
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _MetaBadge(vote.stageName),
            if (vote.date != null) _MetaBadge(vote.date!),
            if (vote.result != null) _MetaBadge(vote.result!),
          ],
        ),
        if (vote.description != null) ...[
          const SizedBox(height: 8),
          Text(vote.description!, style: Theme.of(context).textTheme.bodySmall),
        ],
        const SizedBox(height: 10),
        if (groups.isEmpty)
          const _EmptyDetail('Sem votos por partido disponiveis.')
        else
          Column(
            children:
                _voteOrientationOrder
                    .where(
                      (orientation) => groups[orientation]?.isNotEmpty == true,
                    )
                    .map(
                      (orientation) => _PartyVoteBreakdown(
                        orientation: orientation,
                        votes: groups[orientation]!,
                      ),
                    )
                    .toList(),
          ),
      ],
    );
  }
}

class _PartyVoteBreakdown extends StatelessWidget {
  const _PartyVoteBreakdown({required this.orientation, required this.votes});

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

class _LinksSection extends StatelessWidget {
  const _LinksSection({
    required this.title,
    required this.icon,
    required this.links,
    required this.emptyLabel,
  });

  final String title;
  final IconData icon;
  final List<OfficialSourceLink> links;
  final String emptyLabel;

  @override
  Widget build(BuildContext context) {
    return _DetailSection(
      title: title,
      icon: icon,
      child:
          links.isEmpty
              ? _EmptyDetail(emptyLabel)
              : Column(
                children:
                    links
                        .map(
                          (link) => _CopyableLinkRow(
                            label: link.label,
                            detail: link.kind,
                            url: link.url,
                          ),
                        )
                        .toList(),
              ),
    );
  }
}

class _VideosSection extends StatelessWidget {
  const _VideosSection({required this.videos});

  final List<ProposalJourneyVideo> videos;

  @override
  Widget build(BuildContext context) {
    return _DetailSection(
      title: 'Videos de debate',
      icon: Icons.play_circle_outline,
      child:
          videos.isEmpty
              ? const _EmptyDetail('Sem videos associados.')
              : Column(
                children:
                    videos
                        .map(
                          (video) => _CopyableLinkRow(
                            label: _videoLabel(video),
                            detail: _videoDetail(video),
                            url: video.url,
                          ),
                        )
                        .toList(),
              ),
    );
  }
}

class _DetailSection extends StatelessWidget {
  const _DetailSection({
    required this.title,
    required this.icon,
    required this.child,
  });

  final String title;
  final IconData icon;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, color: baseTheme.colorScheme.primary, size: 20),
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  title,
                  style: TextStyle(
                    color: baseTheme.colorScheme.primary,
                    fontWeight: FontWeight.w900,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          child,
        ],
      ),
    );
  }
}

class _DetailText extends StatelessWidget {
  const _DetailText({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 10),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 4),
          Text(value, style: Theme.of(context).textTheme.bodyMedium),
        ],
      ),
    );
  }
}

class _CopyableLinkRow extends StatelessWidget {
  const _CopyableLinkRow({
    required this.label,
    required this.detail,
    required this.url,
  });

  final String label;
  final String detail;
  final String url;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap: () async {
        await Clipboard.setData(ClipboardData(text: url));
        if (!context.mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Link copiado.'),
            duration: Duration(seconds: 1),
          ),
        );
      },
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 7),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(
              Icons.link,
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.75),
              size: 18,
            ),
            const SizedBox(width: 8),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    label,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Colors.black87,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    detail,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(
                      context,
                    ).textTheme.bodySmall?.copyWith(color: Colors.black54),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    url,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: baseTheme.colorScheme.primary,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Icon(Icons.copy, color: Colors.grey.shade600, size: 18),
          ],
        ),
      ),
    );
  }
}

class _TimelineDot extends StatelessWidget {
  const _TimelineDot({required this.isReached, required this.icon});

  final bool isReached;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return AnimatedContainer(
      duration: const Duration(milliseconds: 180),
      width: 34,
      height: 34,
      margin: const EdgeInsets.only(top: 16, left: 6),
      decoration: BoxDecoration(
        color: isReached ? baseTheme.colorScheme.primary : Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: isReached ? baseTheme.colorScheme.primary : Colors.black26,
          width: 2,
        ),
      ),
      child: Icon(
        icon,
        size: 19,
        color: isReached ? Colors.white : baseTheme.colorScheme.primary,
      ),
    );
  }
}

class _TimelineLinePainter extends CustomPainter {
  _TimelineLinePainter({required this.progress});

  final double progress;

  @override
  void paint(Canvas canvas, Size size) {
    final basePaint =
        Paint()
          ..color = Colors.black.withValues(alpha: 0.12)
          ..strokeWidth = 2
          ..strokeCap = StrokeCap.round;
    final activePaint =
        Paint()
          ..color = baseTheme.colorScheme.primary
          ..strokeWidth = 3
          ..strokeCap = StrokeCap.round;
    const start = Offset(1, 18);
    final end = Offset(1, size.height - 18);
    final activeEnd = Offset.lerp(start, end, progress)!;

    canvas.drawLine(start, end, basePaint);
    canvas.drawLine(start, activeEnd, activePaint);
  }

  @override
  bool shouldRepaint(covariant _TimelineLinePainter oldDelegate) {
    return oldDelegate.progress != progress;
  }
}

class _MetaBadge extends StatelessWidget {
  const _MetaBadge(this.label);

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(minHeight: 28),
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: Colors.white,
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

class _EmptyDetail extends StatelessWidget {
  const _EmptyDetail(this.message);

  final String message;

  @override
  Widget build(BuildContext context) {
    return Text(
      message,
      style: Theme.of(context).textTheme.bodySmall?.copyWith(
        color: Colors.black54,
        fontWeight: FontWeight.w600,
      ),
    );
  }
}

class _EmptyTimeline extends StatelessWidget {
  const _EmptyTimeline();

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.black12),
      ),
      child: const Text(
        'Ainda nao ha fases parlamentares disponiveis para esta iniciativa.',
        textAlign: TextAlign.center,
      ),
    );
  }
}

const _voteOrientationOrder = [
  ParliamentaryVoteOrientation.inFavor,
  ParliamentaryVoteOrientation.abstaining,
  ParliamentaryVoteOrientation.against,
  ParliamentaryVoteOrientation.absent,
  ParliamentaryVoteOrientation.notInterested,
  ParliamentaryVoteOrientation.unknown,
];

IconData _phaseIcon(ProposalJourneyPhase phase) {
  final code = phase.phaseCode;
  if (code == '250' || code == '310' || code == '320') {
    return Icons.how_to_vote_outlined;
  }
  if (code == '580') {
    return Icons.gavel_outlined;
  }
  if (phase.documents.isNotEmpty) {
    return Icons.description_outlined;
  }
  return Icons.flag_outlined;
}

String _orientationLabel(ParliamentaryVoteOrientation orientation) {
  return switch (orientation) {
    ParliamentaryVoteOrientation.inFavor => 'A favor',
    ParliamentaryVoteOrientation.abstaining => 'Abstencao',
    ParliamentaryVoteOrientation.against => 'Contra',
    ParliamentaryVoteOrientation.absent => 'Ausencias',
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

String _videoLabel(ProposalJourneyVideo video) {
  if (video.speakerName != null) {
    return video.speakerParty == null
        ? video.speakerName!
        : '${video.speakerName!} (${video.speakerParty!})';
  }

  if (video.governmentMemberName != null) {
    return video.governmentMemberRole == null
        ? video.governmentMemberName!
        : '${video.governmentMemberName!} (${video.governmentMemberRole!})';
  }

  return 'Video de debate';
}

String _videoDetail(ProposalJourneyVideo video) {
  final parts = [
    if (video.date != null) video.date!,
    if (video.startTime != null && video.endTime != null)
      '${video.startTime!}-${video.endTime!}'
    else if (video.startTime != null)
      video.startTime!,
  ];

  return parts.isEmpty ? 'Video' : parts.join(' · ');
}
