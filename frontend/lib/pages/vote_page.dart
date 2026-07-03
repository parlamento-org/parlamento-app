import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:provider/provider.dart';

class VotePage extends StatefulWidget {
  const VotePage({super.key, VoteController? voteController})
    : _voteController = voteController;

  final VoteController? _voteController;

  @override
  State<VotePage> createState() => _VotePageState();
}

class _VotePageState extends State<VotePage> {
  late final VoteController _voteController =
      widget._voteController ?? VoteController();

  InitiativeFeedCard? _card;
  bool _isLoading = true;
  bool _isSubmitting = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _loadNextCard();
  }

  Future<void> _loadNextCard() async {
    final session = context.read<AuthController>().session;
    if (session == null) {
      setState(() {
        _isLoading = false;
        _errorMessage = 'Inicia sessao para votar nas iniciativas.';
      });
      return;
    }

    setState(() {
      _isLoading = true;
      _errorMessage = null;
    });

    try {
      final card = await _voteController.getInitiativeFeedCard(
        ProposalFlowFeedRequest(
          userId: session.userId,
          legislatures: session.proposalCriteria?.legislaturas,
        ),
      );

      if (!mounted) return;
      setState(() {
        _card = card;
        _isLoading = false;
      });
    } catch (error) {
      if (!mounted) return;
      setState(() {
        _isLoading = false;
        _errorMessage = 'Nao foi possivel carregar a proxima iniciativa.';
      });
    }
  }

  Future<void> _recordInteraction(ProposalInteractionAction action) async {
    final card = _card;
    final session = context.read<AuthController>().session;
    if (card == null || session == null || _isSubmitting) {
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      await _voteController.recordProposalInteraction(
        ProposalInteractionSubmission(
          userId: session.userId,
          initiativeId: card.initiativeId,
          action: action,
        ),
      );

      if (!mounted) return;
      _showInteractionMessage(action);
      await _loadNextCard();
    } catch (error) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Nao foi possivel registar a tua escolha.'),
        ),
      );
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }

  void _showInteractionMessage(ProposalInteractionAction action) {
    final message = switch (action) {
      ProposalInteractionAction.support => 'Apoio registado.',
      ProposalInteractionAction.oppose => 'Oposicao registada.',
      ProposalInteractionAction.abstain => 'Abstencao registada.',
      ProposalInteractionAction.skip => 'Iniciativa saltada.',
      ProposalInteractionAction.unknown => 'Escolha registada.',
    };

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message), duration: const Duration(seconds: 1)),
    );
  }

  @override
  Widget build(BuildContext context) {
    return ColoredBox(
      color: baseTheme.colorScheme.surface,
      child: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(18, 12, 18, 16),
          child: Column(
            children: [
              Expanded(child: _buildBody(context)),
              const SizedBox(height: 14),
              _VoteActions(
                enabled: !_isLoading && !_isSubmitting && _card != null,
                onAction: _recordInteraction,
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildBody(BuildContext context) {
    if (_isLoading) {
      return const Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            CircularProgressIndicator(),
            SizedBox(height: 14),
            Text('A carregar a proxima iniciativa'),
          ],
        ),
      );
    }

    if (_errorMessage != null) {
      return _EmptyState(message: _errorMessage!, onRetry: _loadNextCard);
    }

    final card = _card;
    if (card == null) {
      return _EmptyState(
        message: 'Nao ha iniciativas elegiveis neste momento.',
        onRetry: _loadNextCard,
      );
    }

    return _AnonymizedProposalCard(card: card);
  }
}

class _AnonymizedProposalCard extends StatelessWidget {
  const _AnonymizedProposalCard({required this.card});

  final InitiativeFeedCard card;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Container(
      width: double.infinity,
      decoration: BoxDecoration(
        color: Colors.white,
        border: Border.all(color: baseTheme.colorScheme.primary, width: 2),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(18, 18, 18, 10),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _Badge(label: card.initiativeType),
                    if (card.date != null) _Badge(label: card.date!),
                    if (card.legislature != null)
                      _Badge(label: 'Legislatura ${card.legislature}'),
                  ],
                ),
                const SizedBox(height: 18),
                Text(
                  card.neutralTitle,
                  style: textTheme.headlineSmall?.copyWith(
                    color: baseTheme.colorScheme.primary,
                    fontWeight: FontWeight.w800,
                    height: 1.15,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  'Identity cues are hidden until you vote.',
                  style: textTheme.bodySmall?.copyWith(
                    color: Colors.black54,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(18, 4, 18, 18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (card.summary != null) ...[
                    const _SectionLabel('AI-generated summary'),
                    const SizedBox(height: 8),
                    Text(card.summary!, style: textTheme.bodyLarge),
                    const SizedBox(height: 18),
                  ],
                  const _SectionLabel('Redacted introduced text'),
                  const SizedBox(height: 8),
                  Text(
                    card.redactedExcerpt ??
                        card.redactedText ??
                        'Texto redigido indisponivel para esta iniciativa.',
                    style: textTheme.bodyLarge?.copyWith(height: 1.35),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _VoteActions extends StatelessWidget {
  const _VoteActions({required this.enabled, required this.onAction});

  final bool enabled;
  final ValueChanged<ProposalInteractionAction> onAction;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Row(
          children: [
            Expanded(
              child: _ActionButton(
                label: 'Oppose',
                icon: Icons.close,
                color: rejectedRedBold,
                enabled: enabled,
                onPressed: () => onAction(ProposalInteractionAction.oppose),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _ActionButton(
                label: 'Abstain',
                icon: Icons.remove,
                color: Colors.grey.shade700,
                enabled: enabled,
                onPressed: () => onAction(ProposalInteractionAction.abstain),
              ),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: _ActionButton(
                label: 'Support',
                icon: Icons.check,
                color: approvedGreenBold,
                enabled: enabled,
                onPressed: () => onAction(ProposalInteractionAction.support),
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        SizedBox(
          width: double.infinity,
          child: TextButton.icon(
            style: buttonStyle,
            onPressed:
                enabled ? () => onAction(ProposalInteractionAction.skip) : null,
            icon: const Icon(Icons.help_outline, color: Colors.white),
            label: const Text(
              'Skip / Need more info',
              style: TextStyle(color: Colors.white, fontSize: 16),
            ),
          ),
        ),
      ],
    );
  }
}

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.icon,
    required this.color,
    required this.enabled,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final Color color;
  final bool enabled;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 72,
      child: FilledButton(
        style: FilledButton.styleFrom(
          backgroundColor: color,
          disabledBackgroundColor: color.withValues(alpha: 0.35),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
          padding: const EdgeInsets.symmetric(horizontal: 8),
        ),
        onPressed: enabled ? onPressed : null,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(icon, color: Colors.white),
            const SizedBox(height: 4),
            Text(
              label,
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
              style: const TextStyle(color: Colors.white),
            ),
          ],
        ),
      ),
    );
  }
}

class _Badge extends StatelessWidget {
  const _Badge({required this.label});

  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
      decoration: BoxDecoration(
        color: baseTheme.colorScheme.primary,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Text(
        label,
        style: const TextStyle(
          color: Colors.white,
          fontSize: 12,
          fontWeight: FontWeight.w700,
        ),
      ),
    );
  }
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.label);

  final String label;

  @override
  Widget build(BuildContext context) {
    return Text(
      label,
      style: TextStyle(
        color: baseTheme.colorScheme.primary,
        fontWeight: FontWeight.w800,
      ),
    );
  }
}

class _EmptyState extends StatelessWidget {
  const _EmptyState({required this.message, required this.onRetry});

  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Text(message, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          TextButton(
            style: buttonStyle,
            onPressed: onRetry,
            child: const Text('Try again'),
          ),
        ],
      ),
    );
  }
}
