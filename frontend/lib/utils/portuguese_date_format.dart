String formatPortugueseLongDate(String rawDate) {
  final date = _parseDate(rawDate);
  if (date == null) {
    return rawDate;
  }

  return '${date.day} de ${_monthNames[date.month - 1]} de ${date.year}';
}

DateTime? _parseDate(String rawDate) {
  final value = rawDate.trim();
  if (value.isEmpty) {
    return null;
  }

  final dayFirstMatch = RegExp(
    r'^(\d{1,2})[-/](\d{1,2})[-/](\d{4})$',
  ).firstMatch(value);
  if (dayFirstMatch != null) {
    return _dateOrNull(
      int.parse(dayFirstMatch.group(3)!),
      int.parse(dayFirstMatch.group(2)!),
      int.parse(dayFirstMatch.group(1)!),
    );
  }

  final yearFirstMatch = RegExp(
    r'^(\d{4})[-/](\d{1,2})[-/](\d{1,2})$',
  ).firstMatch(value);
  if (yearFirstMatch != null) {
    return _dateOrNull(
      int.parse(yearFirstMatch.group(1)!),
      int.parse(yearFirstMatch.group(2)!),
      int.parse(yearFirstMatch.group(3)!),
    );
  }

  final parsed = DateTime.tryParse(value);
  if (parsed != null) {
    return DateTime(parsed.year, parsed.month, parsed.day);
  }

  return null;
}

DateTime? _dateOrNull(int year, int month, int day) {
  if (month < 1 || month > 12 || day < 1) {
    return null;
  }

  final date = DateTime(year, month, day);
  if (date.year != year || date.month != month || date.day != day) {
    return null;
  }

  return date;
}

const _monthNames = [
  'janeiro',
  'fevereiro',
  'março',
  'abril',
  'maio',
  'junho',
  'julho',
  'agosto',
  'setembro',
  'outubro',
  'novembro',
  'dezembro',
];
