import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:frontend/utils/portuguese_date_format.dart';
import 'package:frontend/widgets/parliamentary_vote_breakdown.dart';
import 'package:frontend/widgets/proposal_topic_chips.dart';
import 'package:url_launcher/url_launcher.dart';

class ProposalJourneyPage extends StatefulWidget {
  const ProposalJourneyPage({
    super.key,
    required this.initiativeId,
    this.initialProposers = const [],
    this.initialTopicAssignments = const [],
    VoteController? voteController,
  }) : _voteController = voteController;

  final int initiativeId;
  final List<ProposalProposer> initialProposers;
  final List<ProposalTopicAssignment> initialTopicAssignments;
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
                    'Não foi possível carregar o percurso da proposta.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                ),
              );
            }

            return _JourneyContent(
              journey: snapshot.data!,
              initialProposers: widget.initialProposers,
              initialTopicAssignments: widget.initialTopicAssignments,
            );
          },
        ),
      ),
    );
  }
}

class _JourneyContent extends StatelessWidget {
  const _JourneyContent({
    required this.journey,
    required this.initialProposers,
    required this.initialTopicAssignments,
  });

  final ProposalJourney journey;
  final List<ProposalProposer> initialProposers;
  final List<ProposalTopicAssignment> initialTopicAssignments;

  @override
  Widget build(BuildContext context) {
    final phases = journey.phases;

    return ListView(
      padding: const EdgeInsets.fromLTRB(18, 10, 18, 24),
      children: [
        _JourneyHeader(
          journey: journey,
          initialProposers: initialProposers,
          initialTopicAssignments: initialTopicAssignments,
        ),
        const SizedBox(height: 18),
        if (phases.isEmpty)
          const _EmptyTimeline()
        else
          _JourneyTimeline(phases: phases, userVote: journey.userVote),
      ],
    );
  }
}

class _JourneyHeader extends StatelessWidget {
  const _JourneyHeader({
    required this.journey,
    required this.initialProposers,
    required this.initialTopicAssignments,
  });

  final ProposalJourney journey;
  final List<ProposalProposer> initialProposers;
  final List<ProposalTopicAssignment> initialTopicAssignments;

  @override
  Widget build(BuildContext context) {
    final meta = [
      journey.initiativeType,
      if (journey.initiativeNumber != null) journey.initiativeNumber!,
    ];
    final topicAssignments =
        journey.topicAssignments.isNotEmpty
            ? journey.topicAssignments
            : initialTopicAssignments;
    final proposers =
        journey.proposers.isNotEmpty ? journey.proposers : initialProposers;
    final proposerLabel = _firstKnownProposer(proposers);
    final originalProposalUrl = journey.fullProposalTextLink?.trim();

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        _JourneyOutcomeHero(
          userVote: journey.userVote,
          generalityVote: journey.generalityVote,
        ),
        const SizedBox(height: 12),
        _HeaderMetaRow(meta: meta, topicAssignments: topicAssignments),
        const SizedBox(height: 12),
        _JourneyTitleRow(title: journey.title, proposerLabel: proposerLabel),
        if (originalProposalUrl != null && originalProposalUrl.isNotEmpty) ...[
          const SizedBox(height: 16),
          _OriginalProposalSection(url: originalProposalUrl),
        ],
      ],
    );
  }
}

class _JourneyOutcomeHero extends StatelessWidget {
  const _JourneyOutcomeHero({
    required this.userVote,
    required this.generalityVote,
  });

  final ProposalInteractionAction? userVote;
  final ParliamentaryVoteSummary? generalityVote;

  @override
  Widget build(BuildContext context) {
    final vote = userVote ?? ProposalInteractionAction.unknown;
    final approved = generalityVote?.approved;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.primary,
        borderRadius: BorderRadius.circular(8),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final userSignal = _JourneyOutcomeSignal(
            title: 'O teu voto',
            detail: 'Na proposta',
            icon: _actionIcon(vote),
            color: _actionColor(vote),
            semanticLabel: _actionLabel(vote),
          );
          final parliamentSignal = _JourneyOutcomeSignal(
            title: 'Parlamento',
            detail: 'Generalidade',
            icon: _approvalIcon(approved),
            color: _approvalColor(approved),
            semanticLabel: _approvalLabel(approved, generalityVote?.result),
          );

          if (constraints.maxWidth < 520) {
            return Column(
              children: [
                userSignal,
                const SizedBox(height: 10),
                const _HeroDivider(isVertical: false),
                const SizedBox(height: 10),
                parliamentSignal,
              ],
            );
          }

          return Row(
            children: [
              Expanded(child: userSignal),
              const _HeroDivider(isVertical: true),
              Expanded(child: parliamentSignal),
            ],
          );
        },
      ),
    );
  }
}

class _JourneyOutcomeSignal extends StatelessWidget {
  const _JourneyOutcomeSignal({
    required this.title,
    required this.detail,
    required this.icon,
    required this.color,
    required this.semanticLabel,
  });

  final String title;
  final String detail;
  final IconData icon;
  final Color color;
  final String semanticLabel;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: '$title: $semanticLabel',
      child: Tooltip(
        message: semanticLabel,
        child: Row(
          children: [
            Container(
              width: 58,
              height: 58,
              decoration: BoxDecoration(
                color: color,
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.white, width: 2),
              ),
              child: Icon(icon, color: Colors.white, size: 36),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    title,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w900,
                      fontSize: 15,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    detail,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: Colors.white.withValues(alpha: 0.76),
                      fontWeight: FontWeight.w700,
                      fontSize: 12,
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _HeroDivider extends StatelessWidget {
  const _HeroDivider({required this.isVertical});

  final bool isVertical;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: isVertical ? 1 : double.infinity,
      height: isVertical ? 54 : 1,
      margin:
          isVertical
              ? const EdgeInsets.symmetric(horizontal: 12)
              : EdgeInsets.zero,
      color: Colors.white.withValues(alpha: 0.24),
    );
  }
}

class _HeaderMetaRow extends StatelessWidget {
  const _HeaderMetaRow({required this.meta, required this.topicAssignments});

  final List<String> meta;
  final List<ProposalTopicAssignment> topicAssignments;

  @override
  Widget build(BuildContext context) {
    final metaBadges = meta.map((label) => _MetaBadge(label)).toList();
    if (topicAssignments.isEmpty) {
      return Wrap(spacing: 8, runSpacing: 8, children: metaBadges);
    }

    return LayoutBuilder(
      builder: (context, constraints) {
        if (constraints.maxWidth < 680) {
          return Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              ...metaBadges,
              ProposalTopicChips(topics: topicAssignments),
            ],
          );
        }

        return Row(
          crossAxisAlignment: CrossAxisAlignment.center,
          children: [
            Expanded(
              child: Wrap(spacing: 8, runSpacing: 8, children: metaBadges),
            ),
            const SizedBox(width: 12),
            Flexible(
              child: Align(
                alignment: Alignment.centerRight,
                child: ProposalTopicChips(
                  topics: topicAssignments,
                  alignment: WrapAlignment.end,
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _JourneyTitleRow extends StatelessWidget {
  const _JourneyTitleRow({required this.title, required this.proposerLabel});

  final String title;
  final String? proposerLabel;

  @override
  Widget build(BuildContext context) {
    final titleText = Text(
      title,
      style: Theme.of(context).textTheme.headlineSmall?.copyWith(
        color: baseTheme.colorScheme.primary,
        fontWeight: FontWeight.w900,
        height: 1.15,
      ),
    );

    if (proposerLabel == null) {
      return titleText;
    }

    return Row(
      crossAxisAlignment: CrossAxisAlignment.center,
      children: [
        SizedBox(
          width: 74,
          child: ParliamentaryPartyLogo(
            acronym: proposerLabel!,
            size: 68,
            fallbackLabel: proposerLabel!,
          ),
        ),
        const SizedBox(width: 12),
        Expanded(child: titleText),
      ],
    );
  }
}

class _OriginalProposalSection extends StatelessWidget {
  const _OriginalProposalSection({required this.url});

  final String url;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: baseTheme.colorScheme.primary.withValues(alpha: 0.18),
        ),
      ),
      child: LayoutBuilder(
        builder: (context, constraints) {
          final button = FilledButton.icon(
            onPressed: () => _openExternalLink(context, url),
            icon: const Icon(Icons.description_outlined, size: 18),
            label: const Text('Abrir proposta original'),
            style: FilledButton.styleFrom(
              backgroundColor: baseTheme.colorScheme.primary,
              foregroundColor: Colors.white,
              padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
              textStyle: const TextStyle(fontWeight: FontWeight.w900),
            ),
          );

          final copy = Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Proposta original',
                style: TextStyle(
                  color: baseTheme.colorScheme.primary,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(height: 3),
              Text(
                'Texto introduzido no Parlamento.',
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: baseTheme.colorScheme.primary.withValues(alpha: 0.72),
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          );

          if (constraints.maxWidth < 420) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                copy,
                const SizedBox(height: 10),
                SizedBox(width: double.infinity, child: button),
              ],
            );
          }

          return Row(
            children: [
              Expanded(child: copy),
              const SizedBox(width: 12),
              button,
            ],
          );
        },
      ),
    );
  }
}

class _JourneyTimeline extends StatefulWidget {
  const _JourneyTimeline({required this.phases, required this.userVote});

  final List<ProposalJourneyPhase> phases;
  final ProposalInteractionAction? userVote;

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
                    userVote: widget.userVote,
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
    required this.userVote,
    required this.index,
    required this.total,
    required this.timelineProgress,
  });

  final ProposalJourneyPhase phase;
  final ProposalInteractionAction? userVote;
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
                  userVote: userVote,
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
    required this.userVote,
    required this.initiallyExpanded,
  });

  final ProposalJourneyPhase phase;
  final ProposalInteractionAction? userVote;
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
          children: [_ExpandedPhaseDetails(phase: phase, userVote: userVote)],
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
        if (phase.date != null)
          _MetaBadge(formatPortugueseLongDate(phase.date!)),
        if (phase.status != null)
          _VoteResultBadge(
            label: phase.status!,
            approved: _approvedFromResultLabel(phase.status!),
          ),
      ],
    );
  }
}

class _ExpandedPhaseDetails extends StatelessWidget {
  const _ExpandedPhaseDetails({required this.phase, required this.userVote});

  final ProposalJourneyPhase phase;
  final ProposalInteractionAction? userVote;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const SizedBox(height: 4),
        _DetailText(label: 'Resumo', value: phase.summary),
        if (phase.observation != null)
          _DetailText(label: 'Observação', value: phase.observation!),
        _VoteSection(
          votes: phase.votes,
          userVote: _phase250UserVote(phase, userVote),
        ),
        _LinksSection(
          title: 'Documentos oficiais',
          icon: Icons.description_outlined,
          links: phase.documents,
          emptyLabel: 'Sem documentos associados a esta fase.',
        ),
        _LinksSection(
          title: 'Diário da Assembleia',
          icon: Icons.article_outlined,
          links: phase.diaryLinks,
          emptyLabel: 'Sem ligações ao Diário nesta fase.',
        ),
        _LinksSection(
          title: 'Transcrições de debate',
          icon: Icons.forum_outlined,
          links: phase.transcripts,
          emptyLabel: 'Sem transcrições associadas.',
        ),
        _VideosSection(videos: phase.videos),
      ],
    );
  }
}

class _VoteSection extends StatelessWidget {
  const _VoteSection({required this.votes, required this.userVote});

  final List<ParliamentaryVoteSummary> votes;
  final ProposalInteractionAction? userVote;

  @override
  Widget build(BuildContext context) {
    return _DetailSection(
      title: 'Votação parlamentar',
      icon: Icons.how_to_vote_outlined,
      child:
          votes.isEmpty && userVote == null
              ? const _EmptyDetail('Sem votação registada nesta fase.')
              : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (votes.isEmpty)
                    ParliamentaryVoteBreakdown(
                      votes: const [],
                      userOrientation: orientationForUserVote(userVote!),
                      style: ParliamentaryVoteBreakdownStyle.compact,
                      emptyLabel: 'Sem votos por partido disponíveis.',
                    )
                  else
                    ...votes.map(
                      (vote) => Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: _VoteSummaryBlock(
                          vote: vote,
                          userVote: userVote,
                        ),
                      ),
                    ),
                ],
              ),
    );
  }
}

class _VoteSummaryBlock extends StatelessWidget {
  const _VoteSummaryBlock({required this.vote, required this.userVote});

  final ParliamentaryVoteSummary vote;
  final ProposalInteractionAction? userVote;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Wrap(
          spacing: 8,
          runSpacing: 8,
          children: [
            _MetaBadge(vote.stageName),
            if (vote.date != null)
              _MetaBadge(formatPortugueseLongDate(vote.date!)),
            if (vote.result != null)
              _VoteResultBadge(label: vote.result!, approved: vote.approved),
          ],
        ),
        if (vote.description != null) ...[
          const SizedBox(height: 8),
          Text(vote.description!, style: Theme.of(context).textTheme.bodySmall),
        ],
        const SizedBox(height: 10),
        ParliamentaryVoteBreakdown(
          votes: vote.partyVotes,
          isUnanimous: vote.isUnanimous,
          userOrientation:
              userVote == null ? null : orientationForUserVote(userVote!),
          style: ParliamentaryVoteBreakdownStyle.compact,
          emptyLabel: 'Sem votos por partido disponíveis.',
        ),
      ],
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
      title: 'Vídeos de debate',
      icon: Icons.play_circle_outline,
      child:
          videos.isEmpty
              ? const _EmptyDetail('Sem vídeos associados.')
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

Future<void> _openExternalLink(BuildContext context, String url) async {
  final uri = Uri.tryParse(url);
  final launched =
      uri != null && await launchUrl(uri, mode: LaunchMode.externalApplication);
  if (!context.mounted) return;

  if (launched) {
    return;
  }

  await Clipboard.setData(ClipboardData(text: url));
  if (!context.mounted) return;
  ScaffoldMessenger.of(context).showSnackBar(
    const SnackBar(
      content: Text('Nao foi possivel abrir. Link copiado.'),
      duration: Duration(seconds: 1),
    ),
  );
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
        final uri = Uri.tryParse(url);
        final launched =
            uri != null &&
            await launchUrl(uri, mode: LaunchMode.externalApplication);
        if (!context.mounted) return;

        if (launched) {
          return;
        }

        await Clipboard.setData(ClipboardData(text: url));
        if (!context.mounted) return;
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Não foi possível abrir. Link copiado.'),
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
            Icon(Icons.open_in_new, color: Colors.grey.shade600, size: 18),
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

class _VoteResultBadge extends StatelessWidget {
  const _VoteResultBadge({required this.label, required this.approved});

  final String label;
  final bool? approved;

  @override
  Widget build(BuildContext context) {
    final color = _approvalColor(approved);
    final icon = _approvalIcon(approved);

    return Tooltip(
      message: label,
      child: Container(
        constraints: const BoxConstraints(minHeight: 28),
        padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 6),
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: color, width: 2),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: Colors.white, size: 15),
            const SizedBox(width: 5),
            Flexible(
              child: Text(
                label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ),
          ],
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
        'Ainda não há fases parlamentares disponíveis para esta iniciativa.',
        textAlign: TextAlign.center,
      ),
    );
  }
}

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

IconData _actionIcon(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => Icons.check,
    ProposalInteractionAction.oppose => Icons.close,
    ProposalInteractionAction.abstain => Icons.remove,
    ProposalInteractionAction.skip => Icons.skip_next,
    ProposalInteractionAction.unknown => Icons.help_outline,
  };
}

ProposalInteractionAction? _phase250UserVote(
  ProposalJourneyPhase phase,
  ProposalInteractionAction? userVote,
) {
  if (userVote == null || !_isUserVoteAction(userVote)) {
    return null;
  }

  return phase.phaseCode == '250' ? userVote : null;
}

bool _isUserVoteAction(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support ||
    ProposalInteractionAction.oppose ||
    ProposalInteractionAction.abstain => true,
    ProposalInteractionAction.skip ||
    ProposalInteractionAction.unknown => false,
  };
}

Color _actionColor(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => approvedGreenBold,
    ProposalInteractionAction.oppose => rejectedRedBold,
    ProposalInteractionAction.abstain => Colors.grey.shade700,
    ProposalInteractionAction.skip => baseTheme.colorScheme.secondary,
    ProposalInteractionAction.unknown => Colors.grey.shade700,
  };
}

String _actionLabel(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => 'A favor',
    ProposalInteractionAction.oppose => 'Contra',
    ProposalInteractionAction.abstain => 'Abstenção',
    ProposalInteractionAction.skip => 'Saltado',
    ProposalInteractionAction.unknown => 'Sem voto registado',
  };
}

IconData _approvalIcon(bool? approved) {
  if (approved == true) {
    return Icons.check;
  }
  if (approved == false) {
    return Icons.close;
  }
  return Icons.remove;
}

Color _approvalColor(bool? approved) {
  if (approved == true) {
    return approvedGreenBold;
  }
  if (approved == false) {
    return rejectedRedBold;
  }
  return Colors.grey.shade700;
}

String _approvalLabel(bool? approved, String? rawResult) {
  if (approved == true) {
    return 'Aprovado';
  }
  if (approved == false) {
    return 'Rejeitado';
  }
  return rawResult ?? 'Sem resultado';
}

bool? _approvedFromResultLabel(String label) {
  final normalized = label.toLowerCase();
  if (normalized.contains('aprovad')) {
    return true;
  }
  if (normalized.contains('rejeitad')) {
    return false;
  }
  return null;
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

  return 'Vídeo de debate';
}

String _videoDetail(ProposalJourneyVideo video) {
  final parts = [
    if (video.date != null) formatPortugueseLongDate(video.date!),
    if (video.startTime != null && video.endTime != null)
      '${video.startTime!}-${video.endTime!}'
    else if (video.startTime != null)
      video.startTime!,
  ];

  return parts.isEmpty ? 'Vídeo' : parts.join(' · ');
}

String? _firstKnownProposer(List<ProposalProposer> proposers) {
  for (final proposer in proposers) {
    final acronym = proposer.acronym ?? proposer.name;
    if (acronym != null && partyLogoAsset(acronym) != null) {
      return acronym;
    }
  }

  for (final proposer in proposers) {
    final label = proposer.acronym ?? proposer.name;
    if (label != null && label.trim().isNotEmpty) {
      return label;
    }
  }

  return null;
}
