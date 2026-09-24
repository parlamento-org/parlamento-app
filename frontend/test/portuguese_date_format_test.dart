import 'package:flutter_test/flutter_test.dart';
import 'package:frontend/utils/portuguese_date_format.dart';

void main() {
  group('formatPortugueseLongDate', () {
    test('formats day-first dates with Portuguese month names', () {
      expect(formatPortugueseLongDate('28-09-2019'), '28 de setembro de 2019');
    });

    test('formats year-first dates', () {
      expect(formatPortugueseLongDate('2026-06-30'), '30 de junho de 2026');
    });

    test('falls back to the original value when parsing fails', () {
      expect(formatPortugueseLongDate('sem data'), 'sem data');
    });

    test('falls back when the calendar date is invalid', () {
      expect(formatPortugueseLongDate('31-02-2026'), '31-02-2026');
    });
  });
}
