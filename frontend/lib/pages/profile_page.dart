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

            return RefreshIndicator(
              onRefresh: () async => _reload(),
              child: ListView(
                padding: const EdgeInsets.fromLTRB(18, 18, 18, 28),
                children: [
                  Center(
                    child: ConstrainedBox(
                      constraints: const BoxConstraints(maxWidth: 1180),
                      child: Column(
                        children: [
                          const _ProfileHeader(),
                          const SizedBox(height: 18),
                          _OverviewSection(overview: profile.overview),
                          const SizedBox(height: 14),
                          _PartyAlignmentSection(
                            alignment: profile.partyAlignment,
                          ),
                        ],
                      ),
                    ),
                  ),
                ],
              ),
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

class _PartyAlignmentSection extends StatelessWidget {
  const _PartyAlignmentSection({required this.alignment});

  final PartyAlignmentSection alignment;

  @override
  Widget build(BuildContext context) {
    final visibleParties =
        alignment.parties
            .where((party) => party.comparableCount > 0)
            .toList(growable: false);

    return _ProfileSectionCard(
      title: 'Alinhamento com votos dos partidos',
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Percentagens de concordancia entre as tuas escolhas e os votos dos partidos na generalidade.',
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
