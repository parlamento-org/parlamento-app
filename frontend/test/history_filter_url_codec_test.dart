import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/pages/history_filter_url_codec.dart';

void main() {
  test('reads history filters from hash-route query parameters', () {
    final uri = Uri.parse(
      'https://example.test/#/history?parentTopicSlug=educacao&search=energia',
    );

    expect(readHistoryFilterQueryParametersFromUri(uri), {
      'parentTopicSlug': 'educacao',
      'search': 'energia',
    });
  });

  test('clears history filters from hash-route query parameters', () {
    final uri = Uri.parse(
      'https://example.test/#/history?parentTopicSlug=educacao&search=energia',
    );

    final nextUri = replaceHistoryFilterQueryParameters(uri, const {});

    expect(nextUri.toString(), 'https://example.test/#/history');
  });

  test(
    'preserves unrelated hash-route query parameters while clearing filters',
    () {
      final uri = Uri.parse(
        'https://example.test/#/history?tab=votes&parentTopicSlug=educacao',
      );

      final nextUri = replaceHistoryFilterQueryParameters(uri, const {});

      expect(nextUri.toString(), 'https://example.test/#/history?tab=votes');
    },
  );
}
