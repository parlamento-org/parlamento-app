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
    required this.onDark,
    required this.prominence,
  });

  final String label;
  final bool onDark;
  final _TopicPillProminence prominence;

  @override
  Widget build(BuildContext context) {
    final isParent = prominence == _TopicPillProminence.parent;
    final background =
        onDark
            ? Colors.white.withValues(alpha: isParent ? 0.18 : 0.10)
            : isParent
            ? baseTheme.colorScheme.primary.withValues(alpha: 0.10)
            : Colors.white;
    final borderColor =
        onDark
            ? Colors.white.withValues(alpha: isParent ? 0.56 : 0.30)
            : isParent
            ? baseTheme.colorScheme.primary.withValues(alpha: 0.42)
            : Colors.black12;
    final textColor =
        onDark
            ? Colors.white.withValues(alpha: isParent ? 1 : 0.86)
            : isParent
            ? baseTheme.colorScheme.primary
            : Colors.black54;

    return Container(
      padding: EdgeInsets.fromLTRB(isParent ? 10 : 9, 7, 10, 7),
      decoration: BoxDecoration(
        color: background,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: borderColor),
      ),
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
    );
  }
}
