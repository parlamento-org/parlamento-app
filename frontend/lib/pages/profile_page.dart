import 'package:flutter/material.dart';
import 'package:frontend/controllers/profile_controller.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:frontend/widgets/parliamentary_vote_breakdown.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({super.key, ProfileController? profileController})
    : _profileController = profileController;

  final ProfileController? _profileController;

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  late final ProfileController _profileController =
      widget._profileController ?? ProfileController();
  late Future<ProfileStats> _profileFuture;

  @override
  void initState() {
    super.initState();
    _profileFuture = _profileController.getProfileStats();
  }

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: baseTheme.colorScheme.surface,
      child: SafeArea(
        child: FutureBuilder<ProfileStats>(
          future: _profileFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError) {
              return _ProfileEmptyState(
                icon: Icons.error_outline,
                message: 'Nao foi possivel carregar o perfil.',
                actionLabel: 'Tentar novamente',
                onPressed: _reload,
              );
            }

            final profile = snapshot.data;
            if (profile == null) {
              return _ProfileEmptyState(
                icon: Icons.person_outline,
                message: 'Ainda nao ha estatisticas de perfil.',
                actionLabel: 'Atualizar',
                onPressed: _reload,
              );
            }

            return _ProfileStatsPager(
              profile: profile,
              onRefresh: () async => _reload(),
            );
          },
        ),
      ),
    );
  }

  void _reload() {
    setState(() {
      _profileFuture = _profileController.getProfileStats();
    });
  }
}

class _ProfileStatsPager extends StatefulWidget {
  const _ProfileStatsPager({required this.profile, required this.onRefresh});

  final ProfileStats profile;
  final Future<void> Function() onRefresh;

  @override
  State<_ProfileStatsPager> createState() => _ProfileStatsPagerState();
}

class _ProfileStatsPagerState extends State<_ProfileStatsPager> {
  late final PageController _pageController = PageController();
  int _selectedIndex = 0;

  List<TopicPartyAlignment> get _topicBreakdowns =>
      widget.profile.partyAlignment.topicBreakdowns;

  int get _pageCount => 1 + _topicBreakdowns.length;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(18, 18, 18, 0),
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 1180),
              child: const _ProfileHeader(),
            ),
          ),
        ),
        const SizedBox(height: 14),
        _StatsPageTabs(
          selectedIndex: _selectedIndex,
          labels: [
            'Geral',
            ..._topicBreakdowns.map((topic) => topic.parentTopicLabel),
          ],
          onSelected: _animateToPage,
        ),
        const SizedBox(height: 8),
        Expanded(
          child: PageView.builder(
            controller: _pageController,
            itemCount: _pageCount,
            onPageChanged: (index) {
              setState(() => _selectedIndex = index);
            },
            itemBuilder: (context, index) {
              if (index == 0) {
                return _ProfileStatsPage(
                  onRefresh: widget.onRefresh,
                  child: _GeneralStatsPage(profile: widget.profile),
                );
              }

              final topic = _topicBreakdowns[index - 1];
              return _ProfileStatsPage(
                onRefresh: widget.onRefresh,
                child: _TopicStatsPage(
                  topic: topic,
                  minimumComparableVotes:
                      widget.profile.partyAlignment.minimumTopicComparableVotes,
                ),
              );
            },
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(18, 8, 18, 14),
          child: _StatsPageDots(
            count: _pageCount,
            selectedIndex: _selectedIndex,
          ),
        ),
      ],
    );
  }

  void _animateToPage(int index) {
    _pageController.animateToPage(
      index,
      duration: const Duration(milliseconds: 240),
      curve: Curves.easeOutCubic,
    );
  }
}

class _ProfileStatsPage extends StatelessWidget {
  const _ProfileStatsPage({required this.child, required this.onRefresh});

  final Widget child;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: const EdgeInsets.fromLTRB(18, 8, 18, 24),
        children: [
          Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 1180),
              child: child,
            ),
          ),
        ],
      ),
    );
  }
}

class _GeneralStatsPage extends StatelessWidget {
  const _GeneralStatsPage({required this.profile});

  final ProfileStats profile;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        _OverviewSection(overview: profile.overview),
        const SizedBox(height: 14),
        _OverallPartyAlignmentSection(alignment: profile.partyAlignment),
      ],
    );
  }
}

class _StatsPageTabs extends StatelessWidget {
  const _StatsPageTabs({
    required this.labels,
    required this.selectedIndex,
    required this.onSelected,
  });

  final List<String> labels;
  final int selectedIndex;
  final ValueChanged<int> onSelected;

  @override
  Widget build(BuildContext context) {
    return Material(
      type: MaterialType.transparency,
      child: SizedBox(
        height: 42,
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.symmetric(horizontal: 18),
          itemCount: labels.length,
          separatorBuilder: (_, __) => const SizedBox(width: 8),
          itemBuilder: (context, index) {
            final selected = index == selectedIndex;
            return ChoiceChip(
              selected: selected,
              showCheckmark: false,
              label: Text(
                labels[index],
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              labelStyle: TextStyle(
                color: selected ? Colors.white : baseTheme.colorScheme.primary,
                fontWeight: FontWeight.w900,
                fontSize: 12,
              ),
              selectedColor: baseTheme.colorScheme.primary,
              backgroundColor: Colors.white,
              side: BorderSide(
                color: baseTheme.colorScheme.primary.withValues(alpha: 0.42),
              ),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(8),
              ),
              onSelected: (_) => onSelected(index),
            );
          },
        ),
      ),
    );
  }
}

class _StatsPageDots extends StatelessWidget {
  const _StatsPageDots({required this.count, required this.selectedIndex});

  final int count;
  final int selectedIndex;

  @override
  Widget build(BuildContext context) {
    if (count <= 1) {
      return const SizedBox(height: 8);
    }

    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: List.generate(count, (index) {
        final selected = index == selectedIndex;
        return AnimatedContainer(
          duration: const Duration(milliseconds: 180),
          width: selected ? 22 : 8,
          height: 8,
          margin: const EdgeInsets.symmetric(horizontal: 3),
          decoration: BoxDecoration(
            color:
                selected
                    ? baseTheme.colorScheme.primary
                    : baseTheme.colorScheme.primary.withValues(alpha: 0.24),
            borderRadius: BorderRadius.circular(99),
          ),
        );
      }),
    );
  }
}

class _ProfileHeader extends StatelessWidget {
  const _ProfileHeader();

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            'Perfil',
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
              height: 1,
            ),
          ),
        ),
        Icon(Icons.person, color: baseTheme.colorScheme.primary, size: 52),
      ],
    );
  }
}

class _OverviewSection extends StatelessWidget {
  const _OverviewSection({required this.overview});

  final ProfileOverview overview;

  @override
  Widget build(BuildContext context) {
    final stats = [
      _OverviewStat(
        label: 'Interacoes',
        value: overview.proposalsInteracted.toString(),
        icon: Icons.touch_app,
      ),
      _OverviewStat(
        label: 'A favor',
        value: overview.supportCount.toString(),
        icon: Icons.check,
      ),
      _OverviewStat(
        label: 'Contra',
        value: overview.opposeCount.toString(),
        icon: Icons.close,
      ),
      _OverviewStat(
        label: 'Abstencao',
        value: overview.abstentionCount.toString(),
        icon: Icons.remove,
      ),
      _OverviewStat(
        label: 'Saltadas',
        value: overview.skipCount.toString(),
        icon: Icons.help_outline,
      ),
      _OverviewStat(
        label: 'Taxa apoio',
        value: _formatPercentage(overview.supportRate),
        icon: Icons.trending_up,
      ),
    ];

    return _ProfileSectionCard(
      title: 'Visao geral',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (overview.proposalsInteracted == 0) ...[
            Text(
              'As tuas estatisticas aparecem aqui depois das primeiras interacoes.',
              style: TextStyle(
                color: baseTheme.colorScheme.primary.withValues(alpha: 0.74),
                fontWeight: FontWeight.w700,
                height: 1.35,
              ),
            ),
            const SizedBox(height: 16),
          ],
          LayoutBuilder(
            builder: (context, constraints) {
              final availableWidth = constraints.maxWidth;
              final tileWidth =
                  availableWidth < 340
                      ? availableWidth
                      : availableWidth < 620
                      ? (availableWidth - 10) / 2
                      : 154.0;

              return Wrap(
                spacing: 10,
                runSpacing: 10,
                children:
                    stats
                        .map(
                          (stat) => SizedBox(
                            width: tileWidth,
                            height: 68,
                            child: stat,
                          ),
                        )
                        .toList(),
              );
            },
          ),
        ],
      ),
    );
  }
}

class _OverviewStat extends StatelessWidget {
  const _OverviewStat({
    required this.label,
    required this.value,
    required this.icon,
  });

  final String label;
  final String value;
  final IconData icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: baseTheme.colorScheme.primary.withValues(alpha: 0.20),
        ),
      ),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            decoration: BoxDecoration(
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(icon, color: baseTheme.colorScheme.primary, size: 20),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  value,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: baseTheme.colorScheme.primary,
                    fontSize: 20,
                    fontWeight: FontWeight.w900,
                    height: 1,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  label,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(
                    color: Colors.black54,
                    fontSize: 12,
                    fontWeight: FontWeight.w800,
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

class _OverallPartyAlignmentSection extends StatelessWidget {
  const _OverallPartyAlignmentSection({required this.alignment});

  final PartyAlignmentSection alignment;

  @override
  Widget build(BuildContext context) {
    final visibleParties = alignment.parties
        .where((party) => party.comparableCount > 0)
        .toList(growable: false);

    return _ProfileSectionCard(
      title: 'Alinhamento com votos dos partidos',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Percentagens de concordância entre as tuas escolhas e os votos dos partidos na generalidade.',
            style: TextStyle(
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.76),
              fontWeight: FontWeight.w700,
              height: 1.35,
            ),
          ),
          const SizedBox(height: 16),
          if (!alignment.isUnlocked)
            _AlignmentLockedState(alignment: alignment)
          else if (visibleParties.isEmpty)
            const _ProfileEmptyState(
              icon: Icons.insights_outlined,
              message: 'Ainda nao ha votos parlamentares comparaveis.',
            )
          else
            ...visibleParties.map(
              (party) => Padding(
                padding: const EdgeInsets.only(bottom: 14),
                child: _PartyAlignmentRow(party: party),
              ),
            ),
        ],
      ),
    );
  }
}

class _AlignmentLockedState extends StatelessWidget {
  const _AlignmentLockedState({required this.alignment});

  final PartyAlignmentSection alignment;

  @override
  Widget build(BuildContext context) {
    final remaining =
        alignment.minimumComparableVotes - alignment.totalComparableVotes;

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: baseTheme.colorScheme.primary.withValues(alpha: 0.22),
        ),
      ),
      child: Column(
        children: [
          Icon(Icons.lock_outline, color: baseTheme.colorScheme.primary),
          const SizedBox(height: 10),
          Text(
            'Vota em pelo menos ${alignment.minimumComparableVotes} propostas para desbloquear estas estatisticas.',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w900,
              height: 1.3,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            remaining > 0
                ? 'Faltam $remaining votos comparaveis.'
                : 'Estamos quase a preparar os resultados.',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Colors.black54,
              fontWeight: FontWeight.w700,
            ),
          ),
        ],
      ),
    );
  }
}

class _TopicStatsPage extends StatelessWidget {
  const _TopicStatsPage({
    required this.topic,
    required this.minimumComparableVotes,
  });

  final TopicPartyAlignment topic;
  final int minimumComparableVotes;

  @override
  Widget build(BuildContext context) {
    return _ProfileSectionCard(
      title: topic.parentTopicLabel,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(
                Icons.topic_outlined,
                color: baseTheme.colorScheme.primary,
                size: 20,
              ),
              const SizedBox(width: 7),
              Text(
                '${topic.totalComparableVotes} votos comparáveis',
                style: TextStyle(
                  color: baseTheme.colorScheme.primary,
                  fontSize: 12,
                  fontWeight: FontWeight.w900,
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          if (topic.isLowData)
            _TopicLowDataState(minimumComparableVotes: minimumComparableVotes)
          else
            Column(
              children:
                  topic.parties
                      .map(
                        (party) => Padding(
                          padding: const EdgeInsets.only(bottom: 12),
                          child: _CompactPartyAlignmentRow(party: party),
                        ),
                      )
                      .toList(),
            ),
        ],
      ),
    );
  }
}

class _TopicLowDataState extends StatelessWidget {
  const _TopicLowDataState({required this.minimumComparableVotes});

  final int minimumComparableVotes;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: baseTheme.colorScheme.primary.withValues(alpha: 0.20),
        ),
      ),
      child: Column(
        children: [
          Icon(
            Icons.insights_outlined,
            color: baseTheme.colorScheme.primary,
            size: 32,
          ),
          const SizedBox(height: 10),
          Text(
            'Poucos dados neste tópico',
            textAlign: TextAlign.center,
            style: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            'Precisas de $minimumComparableVotes votos comparáveis para ver a distribuição partidária.',
            textAlign: TextAlign.center,
            style: const TextStyle(
              color: Colors.black54,
              fontSize: 12,
              fontWeight: FontWeight.w700,
              height: 1.3,
            ),
          ),
        ],
      ),
    );
  }
}

class _CompactPartyAlignmentRow extends StatelessWidget {
  const _CompactPartyAlignmentRow({required this.party});

  final PartyAlignment party;

  @override
  Widget build(BuildContext context) {
    final percentage = party.alignmentPercentage.clamp(0, 100).toDouble();

    return Row(
      children: [
        ParliamentaryPartyLogo(
          acronym: party.partyAcronym,
          size: 44,
          fallbackLabel: party.partyAcronym,
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      party.partyAcronym,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: baseTheme.colorScheme.primary,
                        fontSize: 12,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  Text(
                    _formatPercentage(percentage),
                    style: TextStyle(
                      color: baseTheme.colorScheme.primary,
                      fontSize: 12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              ClipRRect(
                borderRadius: BorderRadius.circular(999),
                child: Container(
                  height: 10,
                  color: baseTheme.colorScheme.primary.withValues(alpha: 0.12),
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: FractionallySizedBox(
                      widthFactor: percentage / 100,
                      child: Container(color: baseTheme.colorScheme.primary),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 4),
              Text(
                '${party.alignedCount}/${party.comparableCount} votos comparáveis',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: Colors.black54,
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _PartyAlignmentRow extends StatelessWidget {
  const _PartyAlignmentRow({required this.party});

  final PartyAlignment party;

  @override
  Widget build(BuildContext context) {
    final hasComparableVotes = party.comparableCount > 0;
    final percentage = party.alignmentPercentage.clamp(0, 100).toDouble();

    return Row(
      children: [
        SizedBox(
          width: 74,
          child: ParliamentaryPartyLogo(
            acronym: party.partyAcronym,
            size: 68,
            fallbackLabel: party.partyAcronym,
          ),
        ),
        const SizedBox(width: 10),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      party.partyAcronym,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(
                        color: baseTheme.colorScheme.primary,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                  ),
                  Text(
                    hasComparableVotes
                        ? _formatPercentage(percentage)
                        : 'Sem dados',
                    style: TextStyle(
                      color:
                          hasComparableVotes
                              ? baseTheme.colorScheme.primary
                              : Colors.black45,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 7),
              ClipRRect(
                borderRadius: BorderRadius.circular(999),
                child: Container(
                  height: 14,
                  color: baseTheme.colorScheme.primary.withValues(alpha: 0.12),
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: FractionallySizedBox(
                      widthFactor: percentage / 100,
                      child: Container(color: baseTheme.colorScheme.primary),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 5),
              Text(
                hasComparableVotes
                    ? '${party.alignedCount}/${party.comparableCount} votos comparaveis'
                    : 'Sem votos comparaveis para este partido',
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: const TextStyle(
                  color: Colors.black54,
                  fontSize: 11,
                  fontWeight: FontWeight.w700,
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

class _ProfileSectionCard extends StatelessWidget {
  const _ProfileSectionCard({required this.title, required this.child});

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
        border: Border.all(color: baseTheme.colorScheme.primary, width: 2),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: Theme.of(context).textTheme.titleLarge?.copyWith(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w900,
            ),
          ),
          const SizedBox(height: 14),
          child,
        ],
      ),
    );
  }
}

class _ProfileEmptyState extends StatelessWidget {
  const _ProfileEmptyState({
    required this.icon,
    required this.message,
    this.actionLabel,
    this.onPressed,
  });

  final IconData icon;
  final String message;
  final String? actionLabel;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Container(
        margin: const EdgeInsets.all(18),
        padding: const EdgeInsets.symmetric(horizontal: 18, vertical: 28),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: baseTheme.colorScheme.primary, width: 2),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, color: baseTheme.colorScheme.primary, size: 42),
            const SizedBox(height: 10),
            Text(
              message,
              textAlign: TextAlign.center,
              style: TextStyle(
                color: baseTheme.colorScheme.primary,
                fontWeight: FontWeight.w800,
              ),
            ),
            if (actionLabel != null && onPressed != null) ...[
              const SizedBox(height: 12),
              TextButton(
                style: buttonStyle,
                onPressed: onPressed,
                child: Text(actionLabel!),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

String _formatPercentage(double value) {
  return value % 1 == 0 ? '${value.toInt()}%' : '${value.toStringAsFixed(1)}%';
}
