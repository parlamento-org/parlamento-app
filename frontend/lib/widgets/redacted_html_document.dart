import 'package:flutter/material.dart';
import 'package:frontend/themes/base_theme.dart';

class RedactedHtmlDocument extends StatelessWidget {
  const RedactedHtmlDocument({super.key, this.html, this.plainText});

  final String? html;
  final String? plainText;

  @override
  Widget build(BuildContext context) {
    final blocks = _parseBlocks(html);
    if (blocks.isEmpty) {
      return Text(
        plainText ?? 'Texto redigido indisponivel.',
        style: Theme.of(context).textTheme.bodyLarge?.copyWith(height: 1.4),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: blocks.map((block) => _DocumentBlock(block: block)).toList(),
    );
  }
}

class _DocumentBlock extends StatelessWidget {
  const _DocumentBlock({required this.block});

  final _ParsedBlock block;

  @override
  Widget build(BuildContext context) {
    final baseStyle = Theme.of(context).textTheme.bodyLarge?.copyWith(
      height: 1.35,
      color: const Color.fromARGB(255, 45, 45, 45),
    );
    final style = switch (block.kind) {
      _BlockKind.heading => baseStyle?.copyWith(
        color: baseTheme.colorScheme.primary,
        fontSize: 18,
        fontWeight: FontWeight.w800,
      ),
      _ => baseStyle,
    };
    final spans = block.spans(style ?? const TextStyle(fontSize: 16));
    final content = Text.rich(TextSpan(children: spans), textAlign: block.align);

    return Padding(
      padding: EdgeInsets.only(
        bottom: block.kind == _BlockKind.heading ? 14 : 10,
        left: block.kind == _BlockKind.listItem ? 10 : 0,
      ),
      child:
          block.kind == _BlockKind.listItem
              ? Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('- ', style: style),
                  Expanded(child: content),
                ],
              )
              : content,
    );
  }
}

class _ParsedBlock {
  _ParsedBlock({
    required this.kind,
    required this.inlineHtml,
    this.align = TextAlign.start,
  });

  final _BlockKind kind;
  final String inlineHtml;
  final TextAlign align;

  List<InlineSpan> spans(TextStyle baseStyle) {
    return _parseInlineSpans(inlineHtml, baseStyle);
  }
}

enum _BlockKind { paragraph, heading, listItem, tableCell }

List<_ParsedBlock> _parseBlocks(String? html) {
  if (html == null || html.trim().isEmpty) {
    return [];
  }

  final blocks = <_ParsedBlock>[];
  final blockPattern = RegExp(
    r'<(h[1-6]|p|li|td)([^>]*)>(.*?)</(?:h[1-6]|p|li|td)>|<hr[^>]*>',
    caseSensitive: false,
    dotAll: true,
  );

  for (final match in blockPattern.allMatches(html)) {
    final tag = match.group(1)?.toLowerCase();
    if (tag == null) {
      continue;
    }

    final content = match.group(3) ?? '';
    if (_stripTags(content).trim().isEmpty && !content.contains('redacted')) {
      continue;
    }

    blocks.add(
      _ParsedBlock(
        kind:
            tag.startsWith('h')
                ? _BlockKind.heading
                : tag == 'li'
                ? _BlockKind.listItem
                : tag == 'td'
                ? _BlockKind.tableCell
                : _BlockKind.paragraph,
        inlineHtml: content,
        align: _alignmentFromAttributes(match.group(2)),
      ),
    );
  }

  return blocks;
}

List<InlineSpan> _parseInlineSpans(String html, TextStyle baseStyle) {
  final spans = <InlineSpan>[];
  var boldDepth = 0;
  var italicDepth = 0;
  var underlineDepth = 0;

  final tokenPattern = RegExp(r'<[^>]+>|[^<]+', dotAll: true);
  for (final tokenMatch in tokenPattern.allMatches(html)) {
    final token = tokenMatch.group(0) ?? '';
    if (token.startsWith('<')) {
      final lower = token.toLowerCase();
      if (lower.startsWith('<strong') || lower.startsWith('<b')) {
        boldDepth++;
      } else if (lower.startsWith('</strong') || lower.startsWith('</b')) {
        boldDepth = boldDepth > 0 ? boldDepth - 1 : 0;
      } else if (lower.startsWith('<em') || lower.startsWith('<i')) {
        italicDepth++;
      } else if (lower.startsWith('</em') || lower.startsWith('</i')) {
        italicDepth = italicDepth > 0 ? italicDepth - 1 : 0;
      } else if (lower.startsWith('<u')) {
        underlineDepth++;
      } else if (lower.startsWith('</u')) {
        underlineDepth = underlineDepth > 0 ? underlineDepth - 1 : 0;
      } else if (lower.startsWith('<br')) {
        spans.add(const TextSpan(text: '\n'));
      } else if (lower.contains('class="redacted"') ||
          lower.contains("class='redacted'")) {
        spans.add(
          WidgetSpan(
            alignment: PlaceholderAlignment.middle,
            child: _RedactionBox(widthEm: _redactionWidthEm(token)),
          ),
        );
      }
      continue;
    }

    final text = _decodeHtmlEntities(token);
    if (text.isEmpty) {
      continue;
    }

    spans.add(
      TextSpan(
        text: text,
        style: baseStyle.copyWith(
          fontWeight: boldDepth > 0 ? FontWeight.w800 : baseStyle.fontWeight,
          fontStyle: italicDepth > 0 ? FontStyle.italic : baseStyle.fontStyle,
          decoration:
              underlineDepth > 0
                  ? TextDecoration.underline
                  : baseStyle.decoration,
        ),
      ),
    );
  }

  return spans;
}

class _RedactionBox extends StatelessWidget {
  const _RedactionBox({required this.widthEm});

  final double widthEm;

  @override
  Widget build(BuildContext context) {
    final fontSize = DefaultTextStyle.of(context).style.fontSize ?? 16;
    return Container(
      width: (widthEm * fontSize).clamp(22, 240).toDouble(),
      height: (fontSize * 0.78).clamp(10, 18).toDouble(),
      margin: const EdgeInsets.symmetric(horizontal: 2),
      decoration: BoxDecoration(
        color: Colors.black,
        borderRadius: BorderRadius.circular(2),
      ),
    );
  }
}

TextAlign _alignmentFromAttributes(String? attributes) {
  if (attributes == null) {
    return TextAlign.start;
  }

  final lower = attributes.toLowerCase();
  if (lower.contains('text-align:center')) {
    return TextAlign.center;
  }
  if (lower.contains('text-align:right')) {
    return TextAlign.right;
  }
  if (lower.contains('text-align:justify')) {
    return TextAlign.justify;
  }

  return TextAlign.start;
}

double _redactionWidthEm(String tag) {
  final match = RegExp(
    r'--redaction-width\s*:\s*([0-9.]+)em',
    caseSensitive: false,
  ).firstMatch(tag);

  return double.tryParse(match?.group(1) ?? '') ?? 4.5;
}

String _stripTags(String value) {
  return _decodeHtmlEntities(value.replaceAll(RegExp(r'<[^>]+>'), ' '));
}

String _decodeHtmlEntities(String value) {
  final decodedNumeric = value.replaceAllMapped(
    RegExp(r'&#(x?[0-9a-fA-F]+);'),
    (match) {
      final raw = match.group(1) ?? '';
      final radix = raw.startsWith('x') || raw.startsWith('X') ? 16 : 10;
      final digits = radix == 16 ? raw.substring(1) : raw;
      final codePoint = int.tryParse(digits, radix: radix);
      return codePoint == null
          ? match.group(0)!
          : String.fromCharCode(codePoint);
    },
  );

  return decodedNumeric
      .replaceAll('&nbsp;', ' ')
      .replaceAll('&amp;', '&')
      .replaceAll('&lt;', '<')
      .replaceAll('&gt;', '>')
      .replaceAll('&quot;', '"')
      .replaceAll('&#39;', "'")
      .replaceAll('&apos;', "'");
}
