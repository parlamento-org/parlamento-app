// ignore_for_file: avoid_web_libraries_in_flutter, deprecated_member_use

import 'dart:html' as html;

import 'history_filter_url_codec.dart';

Map<String, String> readHistoryFilterQueryParameters() {
  return readHistoryFilterQueryParametersFromUri(Uri.base);
}

void writeHistoryFilterQueryParameters(Map<String, String> queryParameters) {
  final nextUri = replaceHistoryFilterQueryParameters(
    Uri.base,
    queryParameters,
  );
  html.window.history.replaceState(
    null,
    html.document.title,
    nextUri.toString(),
  );
}
