const historyFilterQueryKeys = [
  'legislature',
  'proposingParty',
  'parentTopicSlug',
  'search',
  'interactionType',
];

Map<String, String> readHistoryFilterQueryParametersFromUri(Uri uri) {
  final filters = _historyFilterQueryParameters(uri.queryParameters);
  final fragmentUri = _fragmentUri(uri);

  if (fragmentUri != null && _usesFragmentQuery(uri, fragmentUri)) {
    filters.addAll(_historyFilterQueryParameters(fragmentUri.queryParameters));
  }

  return filters;
}

Uri replaceHistoryFilterQueryParameters(
  Uri uri,
  Map<String, String> queryParameters,
) {
  final nextFilterQuery = _sanitizedHistoryFilterQuery(queryParameters);
  final topLevelQuery = Map<String, String>.from(uri.queryParameters)
    ..removeWhere((key, _) => historyFilterQueryKeys.contains(key));
  final fragmentUri = _fragmentUri(uri);

  if (fragmentUri != null && _usesFragmentQuery(uri, fragmentUri)) {
    final fragmentQuery = Map<String, String>.from(fragmentUri.queryParameters)
      ..removeWhere((key, _) => historyFilterQueryKeys.contains(key));
    fragmentQuery.addAll(nextFilterQuery);

    final nextFragmentUri =
        fragmentQuery.isEmpty
            ? _withoutQuery(fragmentUri)
            : fragmentUri.replace(queryParameters: fragmentQuery);

    final nextTopLevelUri =
        topLevelQuery.isEmpty
            ? _withoutQuery(uri)
            : uri.replace(queryParameters: topLevelQuery);
    return nextTopLevelUri.replace(fragment: nextFragmentUri.toString());
  }

  topLevelQuery.addAll(nextFilterQuery);
  return topLevelQuery.isEmpty
      ? _withoutQuery(uri)
      : uri.replace(queryParameters: topLevelQuery);
}

Uri _withoutQuery(Uri uri) {
  final fragment = uri.hasFragment ? uri.fragment : null;
  if (!uri.hasScheme && uri.host.isEmpty && uri.userInfo.isEmpty) {
    return Uri(path: uri.path, fragment: fragment);
  }

  return Uri(
    scheme: uri.scheme,
    userInfo: uri.userInfo,
    host: uri.host,
    port: uri.hasPort ? uri.port : null,
    path: uri.path,
    fragment: fragment,
  );
}

Map<String, String> _historyFilterQueryParameters(
  Map<String, String> queryParameters,
) {
  return Map<String, String>.fromEntries(
    queryParameters.entries.where(
      (entry) => historyFilterQueryKeys.contains(entry.key),
    ),
  );
}

Map<String, String> _sanitizedHistoryFilterQuery(
  Map<String, String> queryParameters,
) {
  return _historyFilterQueryParameters(queryParameters)
    ..removeWhere((_, value) => value.trim().isEmpty);
}

Uri? _fragmentUri(Uri uri) {
  if (uri.fragment.isEmpty) {
    return null;
  }

  return Uri.parse(uri.fragment);
}

bool _usesFragmentQuery(Uri uri, Uri fragmentUri) {
  return uri.fragment.startsWith('/') ||
      fragmentUri.queryParameters.keys.any(historyFilterQueryKeys.contains);
}
