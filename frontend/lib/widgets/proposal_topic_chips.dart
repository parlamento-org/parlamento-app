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
    final chevronColor =
        onDark
            ? Colors.white.withValues(alpha: 0.68)
            : baseTheme.colorScheme.primary.withValues(alpha: 0.56);

    return ConstrainedBox(
      constraints: const BoxConstraints(maxWidth: 520),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.center,
        children: [
          Flexible(
            flex: 9,
            child: _TopicPill(
              label: topic.parentTopicLabel,
              swatch: swatch,
              onDark: onDark,
              prominence: _TopicPillProminence.parent,
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 5),
            child: Icon(Icons.chevron_right, size: 16, color: chevronColor),
          ),
          Flexible(
            flex: 11,
            child: _TopicPill(
              label: topic.subtopicLabel,
              swatch: swatch,
              onDark: onDark,
              prominence: _TopicPillProminence.child,
            ),
          ),
        ],
      ),
    );
  }
}

enum _TopicPillProminence { parent, child }

class _TopicPill extends StatelessWidget {
  const _TopicPill({
    required this.label,
    required this.swatch,
    required this.onDark,
    required this.prominence,
  });

  final String label;
  final Color swatch;
  final bool onDark;
  final _TopicPillProminence prominence;

  @override
  Widget build(BuildContext context) {
    final isParent = prominence == _TopicPillProminence.parent;
    final background =
        onDark
            ? Colors.white.withValues(alpha: isParent ? 0.20 : 0.10)
            : swatch.withValues(alpha: isParent ? 0.15 : 0.07);
    final borderColor =
        onDark
            ? Colors.white.withValues(alpha: isParent ? 0.58 : 0.30)
            : swatch.withValues(alpha: isParent ? 0.58 : 0.30);
    final textColor =
        onDark
            ? Colors.white.withValues(alpha: isParent ? 1 : 0.86)
            : isParent
            ? baseTheme.colorScheme.primary
            : Colors.black54;
    final markerSize = isParent ? 7.0 : 5.0;

    return Container(
      padding: EdgeInsets.fromLTRB(isParent ? 10 : 9, 7, 10, 7),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: borderColor),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: markerSize,
            height: markerSize,
            margin: const EdgeInsets.only(top: 5),
            decoration: BoxDecoration(
              color:
                  isParent
                      ? swatch
                      : onDark
                      ? Colors.white.withValues(alpha: 0.62)
                      : swatch.withValues(alpha: 0.48),
              shape: BoxShape.circle,
            ),
          ),
          const SizedBox(width: 7),
          Flexible(
            child: Text(
              label,
              softWrap: true,
              style: TextStyle(
                color: textColor,
                fontSize: isParent ? 12.5 : 11.5,
                fontWeight: isParent ? FontWeight.w900 : FontWeight.w700,
                height: 1.18,
              ),
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
