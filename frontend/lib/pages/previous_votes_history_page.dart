import 'dart:async';

import 'package:flutter/material.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/pages/history_filter_url.dart';
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
  static const Duration _searchDebounceDuration = Duration(milliseconds: 650);

  late final VoteController _voteController =
      widget._voteController ?? VoteController();
  late Future<List<ProposalHistoryItem>> _historyFuture;
  final TextEditingController _searchController = TextEditingController();

  final List<ProposalHistoryItem> _history = [];
  List<String> _availableLegislatures = [];
  List<ProposalHistoryProposingParty> _availableProposingParties = [];
  List<ProposalHistoryParentTopic> _availableParentTopics = [];

  _HistoryFilter _filter = _HistoryFilter.all;
  ProposalHistoryFilters _filters = const ProposalHistoryFilters();
  bool _showFilters = false;
  bool _hasLoadedHistory = false;
  bool _isRefreshing = false;
  bool _isLoadingMore = false;
  bool _hasLoadMoreError = false;
  bool _hasNextPage = false;
  int _nextPage = 1;
  int _historyRequestSequence = 0;
  int _totalItems = 0;
  Timer? _searchDebounce;

  @override
  void initState() {
    super.initState();
    _hydrateFiltersFromUrl();
    _searchController.addListener(_onSearchChanged);
    _historyFuture = _loadHistory();
  }

  @override
  void dispose() {
    _searchDebounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _hydrateFiltersFromUrl() {
    final queryParameters = readHistoryFilterQueryParameters();
    final legislature = _queryValue(queryParameters['legislature']);
    final proposingParty = _queryValue(queryParameters['proposingParty']);
    final parentTopicSlug = _queryValue(queryParameters['parentTopicSlug']);
    final search = _queryValue(queryParameters['search']);
    final interactionType = _interactionActionOrNull(
      queryParameters['interactionType'],
    );

    _filter = _historyFilterFromInteractionAction(interactionType);
    _filters = ProposalHistoryFilters(
      legislature: legislature,
      proposingParty: proposingParty,
      parentTopicSlug: parentTopicSlug,
      search: search,
      interactionType: interactionType,
    );
    if (search != null) {
      _searchController.text = search;
    }
    _showFilters =
        _filter != _HistoryFilter.all ||
        legislature != null ||
        proposingParty != null ||
        parentTopicSlug != null;
  }

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: baseTheme.colorScheme.surface,
      child: SafeArea(
        child: FutureBuilder<List<ProposalHistoryItem>>(
          future: _historyFuture,
          initialData: _hasLoadedHistory ? List.unmodifiable(_history) : null,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done &&
                !_hasLoadedHistory) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError && !_hasLoadedHistory) {
              return _HistoryEmptyState(
                icon: Icons.error_outline,
                message: 'Não foi possível carregar o histórico.',
                actionLabel: 'Tentar novamente',
                onPressed: _reloadHistory,
              );
            }

            final history = snapshot.data ?? List.unmodifiable(_history);
            final filteredHistory = history;

            return RefreshIndicator(
              onRefresh: () async => _reloadHistory(),
              child: ListView(
                padding: const EdgeInsets.fromLTRB(18, 14, 18, 28),
                children: [
                  _HistorySearchBar(
                    controller: _searchController,
                    showFilters: _showFilters,
                    activeFilterCount: _activeFilterChips.length,
                    onClearSearch: _clearSearch,
                    onToggleFilters:
                        () => setState(() => _showFilters = !_showFilters),
                  ),
                  if (_isRefreshing) ...[
                    const SizedBox(height: 10),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(999),
                      child: LinearProgressIndicator(
                        minHeight: 3,
                        backgroundColor: baseTheme.colorScheme.primary
                            .withValues(alpha: 0.10),
                        valueColor: AlwaysStoppedAnimation<Color>(
                          baseTheme.colorScheme.primary,
                        ),
                      ),
                    ),
                  ],
                  if (_activeFilterChips.isNotEmpty) ...[
                    const SizedBox(height: 12),
                    _HistoryActiveFilters(
                      chips: _activeFilterChips,
                      onClearAll: _clearFilters,
                    ),
                  ],
                  const SizedBox(height: 12),
                  _HistoryFilterSection(
                    title: 'Voto',
                    child: _HistoryFilterChips(
                      selectedFilter: _filter,
                      onSelected: _changeInteractionFilter,
                    ),
                  ),
                  _HistoryFilterPanel(
                    visible: _showFilters,
                    child: Padding(
                      padding: const EdgeInsets.only(top: 12),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (_availableParentTopics.isNotEmpty) ...[
                            _HistoryFilterSection(
                              title: 'Tópico',
                              child: _HistoryParentTopicChips(
                                parentTopics: _availableParentTopics,
                                selectedParentTopicSlug:
                                    _filters.parentTopicSlug,
                                onSelected: _changeParentTopic,
                              ),
                            ),
                          ],
                          if (_availableLegislatures.isNotEmpty) ...[
                            const SizedBox(height: 12),
                            _HistoryFilterSection(
                              title: 'Legislatura',
                              child: _HistoryLegislatureChips(
                                legislatures: _availableLegislatures,
                                selectedLegislature: _filters.legislature,
                                onSelected: _changeLegislature,
                              ),
                            ),
                          ],
                          if (_availableProposingParties.isNotEmpty) ...[
                            const SizedBox(height: 12),
                            _HistoryFilterSection(
                              title: 'Proponente',
                              child: _HistoryProposingPartyChips(
                                parties: _availableProposingParties,
                                selectedParty: _filters.proposingParty,
                                onSelected: _changeProposingParty,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  if (history.isEmpty)
                    _HistoryEmptyState(
                      icon: Icons.how_to_vote_outlined,
                      message:
                          _hasActiveServerFilter
                              ? 'Sem resultados para a pesquisa ou filtros selecionados.'
                              : 'Ainda não tens votos registados.',
                      actionLabel:
                          _hasActiveServerFilter ? 'Limpar filtros' : null,
                      onPressed: _hasActiveServerFilter ? _clearFilters : null,
                    )
                  else if (filteredHistory.isEmpty)
                    _HistoryEmptyState(
                      icon: Icons.search_off,
                      message: 'Nenhum voto corresponde à pesquisa.',
                      actionLabel: 'Limpar filtros',
                      onPressed: _clearFilters,
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
    final requestSequence = ++_historyRequestSequence;
    try {
      final response = await _voteController.getProposalHistory(
        ProposalHistoryRequest(page: 1, pageSize: _pageSize, filters: _filters),
      );

      if (requestSequence != _historyRequestSequence) {
        return List.unmodifiable(_history);
      }

      _history
        ..clear()
        ..addAll(response.items);
      _availableLegislatures = response.availableLegislatures;
      _availableProposingParties = response.availableProposingParties;
      _availableParentTopics = response.availableParentTopics;
      _nextPage = response.page + 1;
      _totalItems = response.totalItems;
      _hasNextPage = response.hasNextPage;
      _hasLoadMoreError = false;
      _hasLoadedHistory = true;
      _isRefreshing = false;

      return List.unmodifiable(_history);
    } catch (_) {
      if (requestSequence == _historyRequestSequence) {
        _isRefreshing = false;
      }
      rethrow;
    }
  }

  void _reloadHistory() {
    _startHistoryRefresh();
  }

  void _startHistoryRefresh() {
    _writeFilterUrl();
    setState(() {
      _isRefreshing = _hasLoadedHistory;
      _historyFuture = _loadHistory();
    });
  }

  void _writeFilterUrl() {
    writeHistoryFilterQueryParameters(_filters.toQueryParameters());
  }

  void _onSearchChanged() {
    _searchDebounce?.cancel();
    _searchDebounce = Timer(_searchDebounceDuration, () {
      if (!mounted) return;

      final search = _searchController.text.trim();
      final currentSearch = _filters.search?.trim() ?? '';
      if (search == currentSearch) {
        return;
      }

      setState(() {
        _filters = _filters.copyWith(
          search: search,
          clearSearch: search.isEmpty,
        );
      });
      _startHistoryRefresh();
    });
  }

  void _clearSearch() {
    _searchDebounce?.cancel();
    if (_searchController.text.isNotEmpty) {
      _searchController.clear();
    }
    _searchDebounce?.cancel();

    if ((_filters.search ?? '').isEmpty) return;

    setState(() {
      _filters = _filters.copyWith(search: '', clearSearch: true);
    });
    _startHistoryRefresh();
  }

  Future<void> _loadMoreHistory() async {
    if (_isLoadingMore) return;
    if (!_hasNextPage && !_hasLoadMoreError) return;

    setState(() {
      _isLoadingMore = true;
      _hasLoadMoreError = false;
    });
    final requestSequence = _historyRequestSequence;

    try {
      final response = await _voteController.getProposalHistory(
        ProposalHistoryRequest(
          page: _nextPage,
          pageSize: _pageSize,
          filters: _filters,
        ),
      );

      if (!mounted) return;
      if (requestSequence != _historyRequestSequence) {
        setState(() => _isLoadingMore = false);
        return;
      }

      setState(() {
        final existingIds = _history.map((item) => item.interactionId).toSet();
        _history.addAll(
          response.items.where((item) => existingIds.add(item.interactionId)),
        );
        _availableLegislatures = response.availableLegislatures;
        _availableProposingParties = response.availableProposingParties;
        _availableParentTopics = response.availableParentTopics;
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
    });
    _startHistoryRefresh();
  }

  void _changeLegislature(String? legislature) {
    if (_filters.legislature == legislature) return;
    setState(() {
      _filters = _filters.copyWith(
        legislature: legislature,
        clearLegislature: legislature == null,
      );
    });
    _startHistoryRefresh();
  }

  void _changeParentTopic(String? parentTopicSlug) {
    if (_filters.parentTopicSlug == parentTopicSlug) return;
    setState(() {
      _filters = _filters.copyWith(
        parentTopicSlug: parentTopicSlug,
        clearParentTopicSlug: parentTopicSlug == null,
      );
    });
    _startHistoryRefresh();
  }

  void _changeProposingParty(String? proposingParty) {
    if (_filters.proposingParty == proposingParty) return;
    setState(() {
      _filters = _filters.copyWith(
        proposingParty: proposingParty,
        clearProposingParty: proposingParty == null,
      );
    });
    _startHistoryRefresh();
  }

  void _clearFilters() {
    _searchDebounce?.cancel();
    if (_searchController.text.isNotEmpty) {
      _searchController.clear();
    }
    _searchDebounce?.cancel();

    if (!_filters.hasActiveFilters && _filter == _HistoryFilter.all) return;

    setState(() {
      _filter = _HistoryFilter.all;
      _filters = const ProposalHistoryFilters();
    });
    _startHistoryRefresh();
  }

  bool get _hasActiveServerFilter => _filters.hasActiveFilters;

  List<_HistoryActiveFilter> get _activeFilterChips {
    return [
      if ((_filters.search ?? '').trim().isNotEmpty)
        _HistoryActiveFilter(
          label: 'Pesquisa: ${_filters.search!.trim()}',
          onRemove: _clearSearch,
        ),
      if (_filter != _HistoryFilter.all)
        _HistoryActiveFilter(
          label: 'Voto: ${_filter.label}',
          onRemove: () => _changeInteractionFilter(_HistoryFilter.all),
        ),
      if ((_filters.parentTopicSlug ?? '').trim().isNotEmpty)
        _HistoryActiveFilter(
          label: 'Tópico: ${_parentTopicLabel(_filters.parentTopicSlug!)}',
          onRemove: () => _changeParentTopic(null),
        ),
      if ((_filters.legislature ?? '').trim().isNotEmpty)
        _HistoryActiveFilter(
          label: 'Legislatura ${_filters.legislature!.trim()}',
          onRemove: () => _changeLegislature(null),
        ),
      if ((_filters.proposingParty ?? '').trim().isNotEmpty)
        _HistoryActiveFilter(
          label: 'Proponente: ${_filters.proposingParty!.trim()}',
          onRemove: () => _changeProposingParty(null),
        ),
    ];
  }

  String _parentTopicLabel(String slug) {
    for (final topic in _availableParentTopics) {
      if (topic.slug == slug) {
        return topic.label;
      }
    }

    return slug;
  }
}

class _HistorySearchBar extends StatelessWidget {
  const _HistorySearchBar({
    required this.controller,
    required this.showFilters,
    required this.activeFilterCount,
    required this.onClearSearch,
    required this.onToggleFilters,
  });

  final TextEditingController controller;
  final bool showFilters;
  final int activeFilterCount;
  final VoidCallback onClearSearch;
  final VoidCallback onToggleFilters;

  @override
  Widget build(BuildContext context) {
    return ValueListenableBuilder<TextEditingValue>(
      valueListenable: controller,
      builder: (context, value, _) {
        return Row(
          children: [
            Expanded(
              child: TextField(
                controller: controller,
                textInputAction: TextInputAction.search,
                decoration: InputDecoration(
                  hintText: 'Pesquisar nos meus votos',
                  prefixIcon: Icon(
                    Icons.search,
                    color: baseTheme.colorScheme.primary.withValues(
                      alpha: 0.72,
                    ),
                  ),
                  suffixIcon:
                      value.text.isEmpty
                          ? null
                          : IconButton(
                            tooltip: 'Limpar pesquisa',
                            onPressed: onClearSearch,
                            icon: Icon(
                              Icons.close,
                              color: baseTheme.colorScheme.primary.withValues(
                                alpha: 0.72,
                              ),
                            ),
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
              icon: Stack(
                clipBehavior: Clip.none,
                children: [
                  AnimatedRotation(
                    turns: showFilters ? 0.5 : 0,
                    duration: const Duration(milliseconds: 180),
                    curve: Curves.easeOutCubic,
                    child: Icon(
                      Icons.filter_list,
                      color: baseTheme.colorScheme.primary,
                      size: 30,
                    ),
                  ),
                  if (activeFilterCount > 0)
                    Positioned(
                      right: -8,
                      top: -8,
                      child: Container(
                        constraints: const BoxConstraints(
                          minWidth: 18,
                          minHeight: 18,
                        ),
                        alignment: Alignment.center,
                        padding: const EdgeInsets.symmetric(horizontal: 5),
                        decoration: BoxDecoration(
                          color: rejectedRedBold,
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Text(
                          activeFilterCount.toString(),
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 11,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                      ),
                    ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }
}

class _HistoryActiveFilter {
  const _HistoryActiveFilter({required this.label, required this.onRemove});

  final String label;
  final VoidCallback onRemove;
}

class _HistoryActiveFilters extends StatelessWidget {
  const _HistoryActiveFilters({required this.chips, required this.onClearAll});

  final List<_HistoryActiveFilter> chips;
  final VoidCallback onClearAll;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      crossAxisAlignment: WrapCrossAlignment.center,
      children: [
        ...chips.map(
          (chip) => InputChip(
            label: Text(chip.label),
            onDeleted: chip.onRemove,
            deleteIcon: const Icon(Icons.close, size: 16),
            backgroundColor: Colors.white,
            side: BorderSide(
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.36),
            ),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
            labelStyle: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
            ),
          ),
        ),
        TextButton.icon(
          onPressed: onClearAll,
          icon: const Icon(Icons.clear_all, size: 18),
          label: const Text('Limpar filtros'),
          style: TextButton.styleFrom(
            foregroundColor: baseTheme.colorScheme.primary,
            textStyle: const TextStyle(fontWeight: FontWeight.w900),
          ),
        ),
      ],
    );
  }
}

class _HistoryFilterSection extends StatelessWidget {
  const _HistoryFilterSection({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: TextStyle(
            color: baseTheme.colorScheme.primary.withValues(alpha: 0.78),
            fontSize: 12,
            fontWeight: FontWeight.w900,
          ),
        ),
        const SizedBox(height: 7),
        child,
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

class _HistoryParentTopicChips extends StatelessWidget {
  const _HistoryParentTopicChips({
    required this.parentTopics,
    required this.selectedParentTopicSlug,
    required this.onSelected,
  });

  final List<ProposalHistoryParentTopic> parentTopics;
  final String? selectedParentTopicSlug;
  final ValueChanged<String?> onSelected;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        ChoiceChip(
          label: const Text('Todos'),
          selected: selectedParentTopicSlug == null,
          onSelected: (_) => onSelected(null),
          selectedColor: baseTheme.colorScheme.primary,
          labelStyle: TextStyle(
            color:
                selectedParentTopicSlug == null
                    ? Colors.white
                    : baseTheme.colorScheme.primary,
            fontWeight: FontWeight.w800,
          ),
          side: BorderSide(color: baseTheme.colorScheme.primary),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
        ...parentTopics.map(
          (topic) => ChoiceChip(
            label: Text(topic.label),
            selected: selectedParentTopicSlug == topic.slug,
            onSelected: (_) => onSelected(topic.slug),
            selectedColor: baseTheme.colorScheme.primary,
            labelStyle: TextStyle(
              color:
                  selectedParentTopicSlug == topic.slug
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
    final initiativeReference = proposalInitiativeReferenceLabel(
      initiativeType: item.initiativeType,
      initiativeNumber: item.initiativeNumber,
      legislature: item.legislature,
      initiativeSelection: item.initiativeSelection,
    );
    final metaBadges = [
      if (initiativeReference != null) _HistoryMetaBadge(initiativeReference),
    ];

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
                  const SizedBox(height: 10),
                  _HistorySignalRow(
                    action: item.action,
                    generalityVote: item.generalityVote,
                  ),
                  if (metaBadges.isNotEmpty) ...[
                    const SizedBox(height: 8),
                    Wrap(spacing: 6, runSpacing: 6, children: metaBadges),
                  ],
                ],
              ),
            ),
            const SizedBox(width: 12),
            Icon(
              Icons.chevron_right,
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.58),
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

class _HistorySignalRow extends StatelessWidget {
  const _HistorySignalRow({required this.action, required this.generalityVote});

  final ProposalInteractionAction action;
  final ParliamentaryVoteSummary? generalityVote;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 8,
      runSpacing: 8,
      children: [
        _HistorySignalIcon(
          label: 'O teu voto',
          icon: _actionIcon(action),
          color: _actionColor(action),
          tooltip: 'O teu voto: ${_actionLabel(action)}',
        ),
        _HistorySignalIcon(
          label: 'Parlamento (Generalidade)',
          icon: _approvalIcon(generalityVote?.approved),
          color: _approvalColor(generalityVote?.approved),
          tooltip:
              'Parlamento: ${_approvalLabel(generalityVote?.approved, generalityVote?.result)}',
        ),
      ],
    );
  }
}

class _HistorySignalIcon extends StatelessWidget {
  const _HistorySignalIcon({
    required this.label,
    required this.icon,
    required this.color,
    required this.tooltip,
  });

  final String label;
  final IconData icon;
  final Color color;
  final String tooltip;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: Semantics(
        label: tooltip,
        child: Container(
          constraints: const BoxConstraints(minHeight: 34),
          padding: const EdgeInsets.fromLTRB(8, 4, 9, 4),
          decoration: BoxDecoration(
            color: baseTheme.colorScheme.surface,
            borderRadius: BorderRadius.circular(8),
            border: Border.all(
              color: baseTheme.colorScheme.primary.withValues(alpha: 0.28),
            ),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text(
                label,
                style: TextStyle(
                  color: baseTheme.colorScheme.primary,
                  fontSize: 11,
                  fontWeight: FontWeight.w900,
                ),
              ),
              const SizedBox(width: 6),
              Container(
                width: 25,
                height: 25,
                decoration: BoxDecoration(
                  color: color,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(icon, color: Colors.white, size: 18),
              ),
            ],
          ),
        ),
      ),
    );
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

String? _queryValue(String? value) {
  final trimmedValue = value?.trim();
  return trimmedValue == null || trimmedValue.isEmpty ? null : trimmedValue;
}

ProposalInteractionAction? _interactionActionOrNull(String? value) {
  final action = ProposalInteractionAction.fromWireName(_queryValue(value));
  return action == ProposalInteractionAction.unknown ? null : action;
}

_HistoryFilter _historyFilterFromInteractionAction(
  ProposalInteractionAction? action,
) {
  return switch (action) {
    ProposalInteractionAction.support => _HistoryFilter.support,
    ProposalInteractionAction.oppose => _HistoryFilter.oppose,
    ProposalInteractionAction.abstain => _HistoryFilter.abstain,
    ProposalInteractionAction.skip => _HistoryFilter.skip,
    _ => _HistoryFilter.all,
  };
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
    ProposalInteractionAction.skip => Icons.skip_next,
    ProposalInteractionAction.unknown => Icons.help_outline,
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
