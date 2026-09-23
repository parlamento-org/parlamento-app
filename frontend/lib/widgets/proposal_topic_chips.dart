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

    final textColor = onDark ? Colors.white : baseTheme.colorScheme.primary;

    return Column(
      crossAxisAlignment:
          alignment == WrapAlignment.center
              ? CrossAxisAlignment.center
              : CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.topic_outlined, size: 18, color: textColor),
            const SizedBox(width: 6),
            Text(
              'Tópicos',
              style: TextStyle(
                color: textColor,
                fontSize: 12,
                fontWeight: FontWeight.w900,
              ),
            ),
          ],
        ),
        const SizedBox(height: 8),
        Wrap(
          alignment: alignment,
          spacing: 8,
          runSpacing: 8,
          children:
              topics
                  .map(
                    (topic) => _ProposalTopicChip(topic: topic, onDark: onDark),
                  )
                  .toList(),
        ),
      ],
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
        onDark ? Colors.white.withValues(alpha: 0.82) : Colors.black54;

    return Container(
      constraints: const BoxConstraints(maxWidth: 260),
      padding: const EdgeInsets.fromLTRB(10, 8, 10, 8),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: borderColor),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            width: 8,
            height: 34,
            decoration: BoxDecoration(
              color: swatch,
              borderRadius: BorderRadius.circular(99),
            ),
          ),
          const SizedBox(width: 9),
          Flexible(
            child: Column(
              mainAxisSize: MainAxisSize.min,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  topic.parentTopicLabel,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: primaryText,
                    fontSize: 12,
                    fontWeight: FontWeight.w900,
                    height: 1.1,
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  topic.subtopicLabel,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(
                    color: secondaryText,
                    fontSize: 11,
                    fontWeight: FontWeight.w700,
                    height: 1.1,
                  ),
                ),
              ],
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
