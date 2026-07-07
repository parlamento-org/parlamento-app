import 'package:flutter/material.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/pages/proposal_journey_page.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:frontend/widgets/parliamentary_vote_breakdown.dart';

enum _HistoryFilter {
  all('Todos'),
  support('A favor'),
  oppose('Contra'),
  abstain('Abstenção'),
  skip('Saltados');

  const _HistoryFilter(this.label);

  final String label;
}

class PreviousVotesHistoryPage extends StatefulWidget {
  const PreviousVotesHistoryPage({super.key, VoteController? voteController})
    : _voteController = voteController;

  final VoteController? _voteController;

  @override
  State<PreviousVotesHistoryPage> createState() =>
      _PreviousVotesHistoryPageState();
}

class _PreviousVotesHistoryPageState extends State<PreviousVotesHistoryPage> {
  late final VoteController _voteController =
      widget._voteController ?? VoteController();
  late Future<List<ProposalHistoryItem>> _historyFuture = _loadHistory();
  final TextEditingController _searchController = TextEditingController();

  _HistoryFilter _filter = _HistoryFilter.all;
  bool _showFilters = false;
  String _searchText = '';

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() => _searchText = _searchController.text.trim());
    });
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: baseTheme.colorScheme.surface,
      child: SafeArea(
        child: FutureBuilder<List<ProposalHistoryItem>>(
          future: _historyFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError) {
              return _HistoryEmptyState(
                icon: Icons.error_outline,
                message: 'Não foi possível carregar o histórico.',
                actionLabel: 'Tentar novamente',
                onPressed: _reloadHistory,
              );
            }

            final history = snapshot.data ?? [];
            final filteredHistory = _filterHistory(history);

            return RefreshIndicator(
              onRefresh: () async => _reloadHistory(),
              child: ListView(
                padding: const EdgeInsets.fromLTRB(18, 18, 18, 28),
                children: [
                  const _HistoryHeader(),
                  const SizedBox(height: 22),
                  _HistorySearchBar(
                    controller: _searchController,
                    showFilters: _showFilters,
                    onToggleFilters:
                        () => setState(() => _showFilters = !_showFilters),
                  ),
                  if (_showFilters) ...[
                    const SizedBox(height: 12),
                    _HistoryFilterChips(
                      selectedFilter: _filter,
                      onSelected: (filter) => setState(() => _filter = filter),
                    ),
                  ],
                  const SizedBox(height: 24),
                  if (history.isEmpty)
                    const _HistoryEmptyState(
                      icon: Icons.how_to_vote_outlined,
                      message: 'Ainda não tens votos registados.',
                    )
                  else if (filteredHistory.isEmpty)
                    const _HistoryEmptyState(
                      icon: Icons.search_off,
                      message: 'Nenhum voto corresponde à pesquisa.',
                    )
                  else
                    ...filteredHistory.map(
                      (item) => Padding(
                        padding: const EdgeInsets.only(bottom: 14),
                        child: _HistoryCard(item: item),
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

  Future<List<ProposalHistoryItem>> _loadHistory() {
    return _voteController.getProposalHistory();
  }

  void _reloadHistory() {
    setState(() => _historyFuture = _loadHistory());
  }

  List<ProposalHistoryItem> _filterHistory(List<ProposalHistoryItem> history) {
    final normalizedSearch = _searchText.toLowerCase();
    return history
        .where((item) {
          final matchesFilter = switch (_filter) {
            _HistoryFilter.all => true,
            _HistoryFilter.support =>
              item.action == ProposalInteractionAction.support,
            _HistoryFilter.oppose =>
              item.action == ProposalInteractionAction.oppose,
            _HistoryFilter.abstain =>
              item.action == ProposalInteractionAction.abstain,
            _HistoryFilter.skip =>
              item.action == ProposalInteractionAction.skip,
          };

          if (!matchesFilter) {
            return false;
          }

          if (normalizedSearch.isEmpty) {
            return true;
          }

          final searchableText =
              [
                item.title,
                item.initiativeType,
                if (item.initiativeNumber != null) item.initiativeNumber!,
                ...item.proposers.expand(
                  (proposer) => [
                    if (proposer.acronym != null) proposer.acronym!,
                    if (proposer.name != null) proposer.name!,
                  ],
                ),
              ].join(' ').toLowerCase();

          return searchableText.contains(normalizedSearch);
        })
        .toList(growable: false);
  }
}

class _HistoryHeader extends StatelessWidget {
  const _HistoryHeader();

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Text(
            'Os Meus Votos',
            style: Theme.of(context).textTheme.headlineMedium?.copyWith(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
              height: 1,
            ),
          ),
        ),
        Icon(Icons.edit, color: baseTheme.colorScheme.primary, size: 52),
      ],
    );
  }
}

class _HistorySearchBar extends StatelessWidget {
  const _HistorySearchBar({
    required this.controller,
    required this.showFilters,
    required this.onToggleFilters,
  });

  final TextEditingController controller;
  final bool showFilters;
  final VoidCallback onToggleFilters;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: TextField(
            controller: controller,
            textInputAction: TextInputAction.search,
            decoration: InputDecoration(
              hintText: 'Pesquisar votos',
              prefixIcon: Icon(
                Icons.search,
                color: baseTheme.colorScheme.primary.withValues(alpha: 0.72),
              ),
              filled: true,
              fillColor: Colors.white,
              contentPadding: const EdgeInsets.symmetric(vertical: 16),
              enabledBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(24),
                borderSide: BorderSide(
                  color: baseTheme.colorScheme.primary,
                  width: 2,
                ),
              ),
              focusedBorder: OutlineInputBorder(
                borderRadius: BorderRadius.circular(24),
                borderSide: BorderSide(
                  color: baseTheme.colorScheme.primary,
                  width: 3,
                ),
              ),
            ),
          ),
        ),
        const SizedBox(width: 12),
        IconButton(
          tooltip: showFilters ? 'Ocultar filtros' : 'Mostrar filtros',
          onPressed: onToggleFilters,
          icon: Icon(
            Icons.filter_list,
            color: baseTheme.colorScheme.primary,
            size: 30,
          ),
        ),
      ],
    );
  }
}

class _HistoryFilterChips extends StatelessWidget {
  const _HistoryFilterChips({
    required this.selectedFilter,
    required this.onSelected,
  });

  final _HistoryFilter selectedFilter;
  final ValueChanged<_HistoryFilter> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children:
          _HistoryFilter.values
              .map(
                (filter) => ChoiceChip(
                  label: Text(filter.label),
                  selected: selectedFilter == filter,
                  onSelected: (_) => onSelected(filter),
                  selectedColor: baseTheme.colorScheme.primary,
                  labelStyle: TextStyle(
                    color:
                        selectedFilter == filter
                            ? Colors.white
                            : baseTheme.colorScheme.primary,
                    fontWeight: FontWeight.w800,
                  ),
                  side: BorderSide(color: baseTheme.colorScheme.primary),
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
              )
              .toList(),
    );
  }
}

class _HistoryCard extends StatelessWidget {
  const _HistoryCard({required this.item});

  final ProposalHistoryItem item;

  @override
  Widget build(BuildContext context) {
    final proposerAcronym = _firstKnownProposer(item.proposers);
    final actionColor = _actionColor(item.action);

    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap:
          () => Navigator.of(context).push(
            MaterialPageRoute(
              builder:
                  (context) =>
                      ProposalJourneyPage(initiativeId: item.initiativeId),
            ),
          ),
      child: Ink(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: baseTheme.colorScheme.primary, width: 2),
        ),
        child: Row(
          children: [
            SizedBox(
              width: 104,
              child:
                  proposerAcronym == null
                      ? _UnknownProposerLabel(label: item.initiativeType)
                      : ParliamentaryPartyLogo(
                        acronym: proposerAcronym,
                        size: 92,
                        fallbackLabel: proposerAcronym,
                      ),
            ),
            const SizedBox(width: 14),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    item.title,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: TextStyle(
                      color: baseTheme.colorScheme.primary,
                      fontSize: 19,
                      height: 1.12,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 6,
                    runSpacing: 6,
                    children: [
                      _HistoryMetaBadge(_actionLabel(item.action)),
                      if (item.initiativeNumber != null)
                        _HistoryMetaBadge(item.initiativeNumber!),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(width: 12),
            Container(
              width: 54,
              height: 54,
              decoration: BoxDecoration(
                color: actionColor,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(
                _actionIcon(item.action),
                color: Colors.white,
                size: 38,
              ),
            ),
          ],
        ),
      ),
    );
  }

  static String? _firstKnownProposer(List<ProposalProposer> proposers) {
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
}

class _HistoryMetaBadge extends StatelessWidget {
  const _HistoryMetaBadge(this.label);

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.surface,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(
          color: baseTheme.colorScheme.primary.withValues(alpha: 0.28),
        ),
      ),
      child: Text(
        label,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
        style: TextStyle(
          color: baseTheme.colorScheme.primary,
          fontSize: 11,
          fontWeight: FontWeight.w800,
        ),
      ),
    );
  }
}

class _UnknownProposerLabel extends StatelessWidget {
  const _UnknownProposerLabel({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Text(
      label,
      maxLines: 2,
      overflow: TextOverflow.ellipsis,
      textAlign: TextAlign.center,
      style: TextStyle(
        color: baseTheme.colorScheme.primary,
        fontSize: 12,
        fontWeight: FontWeight.w900,
      ),
    );
  }
}

class _HistoryEmptyState extends StatelessWidget {
  const _HistoryEmptyState({
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
    return Container(
      width: double.infinity,
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
    );
  }
}

String _actionLabel(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => 'A favor',
    ProposalInteractionAction.oppose => 'Contra',
    ProposalInteractionAction.abstain => 'Abstenção',
    ProposalInteractionAction.skip => 'Saltado',
    ProposalInteractionAction.unknown => 'Registado',
  };
}

IconData _actionIcon(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => Icons.check,
    ProposalInteractionAction.oppose => Icons.close,
    ProposalInteractionAction.abstain => Icons.remove,
    ProposalInteractionAction.skip => Icons.help_outline,
    ProposalInteractionAction.unknown => Icons.check,
  };
}

Color _actionColor(ProposalInteractionAction action) {
  return switch (action) {
    ProposalInteractionAction.support => approvedGreenBold,
    ProposalInteractionAction.oppose => rejectedRedBold,
    ProposalInteractionAction.abstain => Colors.grey.shade700,
    ProposalInteractionAction.skip => baseTheme.colorScheme.secondary,
    ProposalInteractionAction.unknown => baseTheme.colorScheme.primary,
  };
}
