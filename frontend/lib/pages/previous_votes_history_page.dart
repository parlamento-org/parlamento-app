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

extension _HistoryFilterInteractionType on _HistoryFilter {
  ProposalInteractionAction? get interactionType {
    return switch (this) {
      _HistoryFilter.all => null,
      _HistoryFilter.support => ProposalInteractionAction.support,
      _HistoryFilter.oppose => ProposalInteractionAction.oppose,
      _HistoryFilter.abstain => ProposalInteractionAction.abstain,
      _HistoryFilter.skip => ProposalInteractionAction.skip,
    };
  }
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
  static const int _pageSize = 20;

  late final VoteController _voteController =
      widget._voteController ?? VoteController();
  late Future<List<ProposalHistoryItem>> _historyFuture;
  final TextEditingController _searchController = TextEditingController();

  final List<ProposalHistoryItem> _history = [];
  List<String> _availableLegislatures = [];
  List<ProposalHistoryProposingParty> _availableProposingParties = [];

  _HistoryFilter _filter = _HistoryFilter.all;
  ProposalHistoryFilters _filters = const ProposalHistoryFilters();
  bool _showFilters = false;
  bool _isLoadingMore = false;
  bool _hasLoadMoreError = false;
  bool _hasNextPage = false;
  int _nextPage = 1;
  int _totalItems = 0;
  String _searchText = '';

  @override
  void initState() {
    super.initState();
    _searchController.addListener(() {
      setState(() => _searchText = _searchController.text.trim());
    });
    _historyFuture = _loadHistory();
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
                  _HistoryFilterPanel(
                    visible: _showFilters,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const SizedBox(height: 12),
                        _HistoryFilterChips(
                          selectedFilter: _filter,
                          onSelected: _changeInteractionFilter,
                        ),
                        if (_availableLegislatures.isNotEmpty) ...[
                          const SizedBox(height: 10),
                          _HistoryLegislatureChips(
                            legislatures: _availableLegislatures,
                            selectedLegislature: _filters.legislature,
                            onSelected: _changeLegislature,
                          ),
                        ],
                        if (_availableProposingParties.isNotEmpty) ...[
                          const SizedBox(height: 10),
                          _HistoryProposingPartyChips(
                            parties: _availableProposingParties,
                            selectedParty: _filters.proposingParty,
                            onSelected: _changeProposingParty,
                          ),
                        ],
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  if (history.isEmpty)
                    _HistoryEmptyState(
                      icon: Icons.how_to_vote_outlined,
                      message:
                          _hasActiveServerFilter
                              ? 'Sem votos para o filtro selecionado.'
                              : 'Ainda não tens votos registados.',
                    )
                  else if (filteredHistory.isEmpty)
                    const _HistoryEmptyState(
                      icon: Icons.search_off,
                      message: 'Nenhum voto corresponde à pesquisa.',
                    )
                  else ...[
                    ...filteredHistory.map(
                      (item) => Padding(
                        padding: const EdgeInsets.only(bottom: 14),
                        child: _HistoryCard(item: item),
                      ),
                    ),
                    _HistoryPaginationFooter(
                      isLoading: _isLoadingMore,
                      hasNextPage: _hasNextPage,
                      hasError: _hasLoadMoreError,
                      loadedItems: history.length,
                      totalItems: _totalItems,
                      onLoadMore: _loadMoreHistory,
                    ),
                  ],
                ],
              ),
            );
          },
        ),
      ),
    );
  }

  Future<List<ProposalHistoryItem>> _loadHistory() async {
    final response = await _voteController.getProposalHistory(
      ProposalHistoryRequest(page: 1, pageSize: _pageSize, filters: _filters),
    );

    _history
      ..clear()
      ..addAll(response.items);
    _availableLegislatures = response.availableLegislatures;
    _availableProposingParties = response.availableProposingParties;
    _nextPage = response.page + 1;
    _totalItems = response.totalItems;
    _hasNextPage = response.hasNextPage;
    _hasLoadMoreError = false;

    return List.unmodifiable(_history);
  }

  void _reloadHistory() {
    setState(() => _historyFuture = _loadHistory());
  }

  Future<void> _loadMoreHistory() async {
    if (_isLoadingMore) return;
    if (!_hasNextPage && !_hasLoadMoreError) return;

    setState(() {
      _isLoadingMore = true;
      _hasLoadMoreError = false;
    });

    try {
      final response = await _voteController.getProposalHistory(
        ProposalHistoryRequest(
          page: _nextPage,
          pageSize: _pageSize,
          filters: _filters,
        ),
      );

      if (!mounted) return;
      setState(() {
        final existingIds = _history.map((item) => item.interactionId).toSet();
        _history.addAll(
          response.items.where((item) => existingIds.add(item.interactionId)),
        );
        _availableLegislatures = response.availableLegislatures;
        _availableProposingParties = response.availableProposingParties;
        _nextPage = response.page + 1;
        _totalItems = response.totalItems;
        _hasNextPage = response.hasNextPage;
        _isLoadingMore = false;
        _historyFuture = Future.value(List.unmodifiable(_history));
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _hasLoadMoreError = true;
        _isLoadingMore = false;
      });
    }
  }

  void _changeInteractionFilter(_HistoryFilter filter) {
    if (_filter == filter) return;
    setState(() {
      _filter = filter;
      _filters = _filters.copyWith(
        interactionType: filter.interactionType,
        clearInteractionType: filter == _HistoryFilter.all,
      );
      _historyFuture = _loadHistory();
    });
  }

  void _changeLegislature(String? legislature) {
    if (_filters.legislature == legislature) return;
    setState(() {
      _filters = _filters.copyWith(
        legislature: legislature,
        clearLegislature: legislature == null,
      );
      _historyFuture = _loadHistory();
    });
  }

  void _changeProposingParty(String? proposingParty) {
    if (_filters.proposingParty == proposingParty) return;
    setState(() {
      _filters = _filters.copyWith(
        proposingParty: proposingParty,
        clearProposingParty: proposingParty == null,
      );
      _historyFuture = _loadHistory();
    });
  }

  bool get _hasActiveServerFilter => _filters.hasActiveFilters;

  List<ProposalHistoryItem> _filterHistory(List<ProposalHistoryItem> history) {
    final normalizedSearch = _searchText.toLowerCase();
    return history
        .where((item) {
          if (normalizedSearch.isEmpty) {
            return true;
          }

          final searchableText =
              [
                item.title,
                item.initiativeType,
                if (item.initiativeNumber != null) item.initiativeNumber!,
                if (item.legislature != null) item.legislature!,
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
          icon: AnimatedRotation(
            turns: showFilters ? 0.5 : 0,
            duration: const Duration(milliseconds: 180),
            curve: Curves.easeOutCubic,
            child: Icon(
              Icons.filter_list,
              color: baseTheme.colorScheme.primary,
              size: 30,
            ),
          ),
        ),
      ],
    );
  }
}

class _HistoryFilterPanel extends StatelessWidget {
  const _HistoryFilterPanel({required this.visible, required this.child});

  final bool visible;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return AnimatedSwitcher(
      duration: const Duration(milliseconds: 220),
      reverseDuration: const Duration(milliseconds: 180),
      switchInCurve: Curves.easeOutCubic,
      switchOutCurve: Curves.easeInCubic,
      transitionBuilder: (child, animation) {
        return FadeTransition(
          opacity: animation,
          child: SizeTransition(
            sizeFactor: animation,
            axisAlignment: -1,
            child: child,
          ),
        );
      },
      child:
          visible
              ? KeyedSubtree(key: const ValueKey('filters'), child: child)
              : const SizedBox(
                key: ValueKey('emptyFilters'),
                width: double.infinity,
              ),
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

class _HistoryLegislatureChips extends StatelessWidget {
  const _HistoryLegislatureChips({
    required this.legislatures,
    required this.selectedLegislature,
    required this.onSelected,
  });

  final List<String> legislatures;
  final String? selectedLegislature;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) {
    final values = [null, ...legislatures];

    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children:
          values
              .map(
                (legislature) => ChoiceChip(
                  label: Text(legislature ?? 'Todas'),
                  selected: selectedLegislature == legislature,
                  onSelected: (_) => onSelected(legislature),
                  selectedColor: baseTheme.colorScheme.primary,
                  labelStyle: TextStyle(
                    color:
                        selectedLegislature == legislature
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

class _HistoryProposingPartyChips extends StatelessWidget {
  const _HistoryProposingPartyChips({
    required this.parties,
    required this.selectedParty,
    required this.onSelected,
  });

  final List<ProposalHistoryProposingParty> parties;
  final String? selectedParty;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        ChoiceChip(
          label: const Text('Todos'),
          selected: selectedParty == null,
          onSelected: (_) => onSelected(null),
          selectedColor: baseTheme.colorScheme.primary,
          labelStyle: TextStyle(
            color:
                selectedParty == null
                    ? Colors.white
                    : baseTheme.colorScheme.primary,
            fontWeight: FontWeight.w800,
          ),
          side: BorderSide(color: baseTheme.colorScheme.primary),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        ...parties.map(
          (party) => ChoiceChip(
            label: Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                ParliamentaryPartyLogo(
                  acronym: party.acronym,
                  size: 34,
                  fallbackLabel: party.acronym,
                ),
                const SizedBox(width: 6),
                Text(party.acronym),
              ],
            ),
            selected: selectedParty == party.acronym,
            onSelected: (_) => onSelected(party.acronym),
            selectedColor: baseTheme.colorScheme.primary,
            labelStyle: TextStyle(
              color:
                  selectedParty == party.acronym
                      ? Colors.white
                      : baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
            ),
            side: BorderSide(color: baseTheme.colorScheme.primary),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
        ),
      ],
    );
  }
}

class _HistoryPaginationFooter extends StatelessWidget {
  const _HistoryPaginationFooter({
    required this.isLoading,
    required this.hasNextPage,
    required this.hasError,
    required this.loadedItems,
    required this.totalItems,
    required this.onLoadMore,
  });

  final bool isLoading;
  final bool hasNextPage;
  final bool hasError;
  final int loadedItems;
  final int totalItems;
  final VoidCallback onLoadMore;

  @override
  Widget build(BuildContext context) {
    if (!hasNextPage && !hasError) {
      return Padding(
        padding: const EdgeInsets.only(top: 4),
        child: Center(
          child: Text(
            totalItems == 0 ? '' : '$loadedItems de $totalItems votos',
            style: TextStyle(
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.72),
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
      );
    }

    return Padding(
      padding: const EdgeInsets.only(top: 4),
      child: Center(
        child:
            isLoading
                ? const CircularProgressIndicator()
                : TextButton(
                  style: buttonStyle,
                  onPressed: onLoadMore,
                  child: Text(
                    hasError ? 'Tentar novamente' : 'Carregar mais',
                    style: const TextStyle(color: Colors.white),
                  ),
                ),
      ),
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
