import 'package:flutter/material.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/themes/base_theme.dart';

class ProposalTopicChips extends StatelessWidget {
  const ProposalTopicChips({
    super.key,
    required this.topics,
    this.onDark = false,
    this.alignment = WrapAlignment.start,
  });

  final List<ProposalTopicAssignment> topics;
  final bool onDark;
  final WrapAlignment alignment;

  @override
  Widget build(BuildContext context) {
    if (topics.isEmpty) {
      return const SizedBox.shrink();
    }

    return Wrap(
      alignment: alignment,
      spacing: 8,
      runSpacing: 8,
      children:
          topics
              .map((topic) => _ProposalTopicChip(topic: topic, onDark: onDark))
              .toList(),
    );
  }
}

class _ProposalTopicChip extends StatelessWidget {
  const _ProposalTopicChip({required this.topic, required this.onDark});

  final ProposalTopicAssignment topic;
  final bool onDark;

  @override
  Widget build(BuildContext context) {
    final swatch = _topicColor(topic.parentTopicSlug);
    final background =
        onDark
            ? Colors.white.withValues(alpha: 0.16)
            : swatch.withValues(alpha: 0.10);
    final borderColor =
        onDark
            ? Colors.white.withValues(alpha: 0.44)
            : swatch.withValues(alpha: 0.46);
    final primaryText = onDark ? Colors.white : baseTheme.colorScheme.primary;
    final secondaryText =
        onDark ? Colors.white.withValues(alpha: 0.80) : Colors.black54;

    return Container(
      constraints: const BoxConstraints(maxWidth: 360),
      padding: const EdgeInsets.fromLTRB(10, 7, 11, 7),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: borderColor),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 7,
            height: 7,
            margin: const EdgeInsets.only(top: 5),
            decoration: BoxDecoration(color: swatch, shape: BoxShape.circle),
          ),
          const SizedBox(width: 8),
          Flexible(
            child: Text.rich(
              TextSpan(
                children: [
                  TextSpan(
                    text: '${topic.parentTopicLabel}: ',
                    style: TextStyle(
                      color: primaryText,
                      fontSize: 12.5,
                      fontWeight: FontWeight.w900,
                    ),
                  ),
                  TextSpan(
                    text: topic.subtopicLabel,
                    style: TextStyle(
                      color: secondaryText,
                      fontSize: 11.5,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                ],
              ),
              style: const TextStyle(height: 1.18),
              softWrap: true,
            ),
          ),
        ],
      ),
    );
  }
}

Color _topicColor(String slug) {
  const colors = [
    Color(0xff006D77),
    Color(0xff7D4E57),
    Color(0xff3A6EA5),
    Color(0xff5A7D35),
    Color(0xff8F5B29),
    Color(0xff6D597A),
  ];

  final hash = slug.codeUnits.fold<int>(0, (value, unit) => value + unit);
  return colors[hash % colors.length];
}
