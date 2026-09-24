// ignore_for_file: avoid_web_libraries_in_flutter, deprecated_member_use

import 'dart:html' as html;

const _historyFilterQueryKeys = [
  'legislature',
  'proposingParty',
  'parentTopicSlug',
  'search',
  'interactionType',
];

Map<String, String> readHistoryFilterQueryParameters() {
  return Uri.base.queryParameters;
}

void writeHistoryFilterQueryParameters(Map<String, String> queryParameters) {
  final nextQuery = Map<String, String>.from(Uri.base.queryParameters);

  for (final key in _historyFilterQueryKeys) {
    nextQuery.remove(key);
  }
  final nextFilterQuery = Map<String, String>.from(queryParameters)
    ..removeWhere((_, value) => value.trim().isEmpty);
  nextQuery.addAll(nextFilterQuery);

  final nextUri = Uri.base.replace(
    queryParameters: nextQuery.isEmpty ? null : nextQuery,
  );
  html.window.history.replaceState(
    null,
    html.document.title,
    nextUri.toString(),
  );
}
