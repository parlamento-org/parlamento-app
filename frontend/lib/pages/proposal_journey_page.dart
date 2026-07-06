import 'package:flutter/material.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/themes/base_theme.dart';

class ProposalJourneyPage extends StatefulWidget {
  const ProposalJourneyPage({
    super.key,
    required this.initiativeId,
    VoteController? voteController,
  }) : _voteController = voteController;

  final int initiativeId;
  final VoteController? _voteController;

  @override
  State<ProposalJourneyPage> createState() => _ProposalJourneyPageState();
}

class _ProposalJourneyPageState extends State<ProposalJourneyPage> {
  late final VoteController _voteController =
      widget._voteController ?? VoteController();
  late final Future<ProposalJourney> _journeyFuture = _voteController
      .getProposalJourney(widget.initiativeId);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: baseTheme.colorScheme.surface,
      appBar: AppBar(
        backgroundColor: baseTheme.colorScheme.surface,
        foregroundColor: baseTheme.colorScheme.primary,
        elevation: 0,
        title: const Text('Percurso'),
      ),
      body: SafeArea(
        child: FutureBuilder<ProposalJourney>(
          future: _journeyFuture,
          builder: (context, snapshot) {
            if (snapshot.connectionState != ConnectionState.done) {
              return const Center(child: CircularProgressIndicator());
            }

            if (snapshot.hasError || snapshot.data == null) {
              return Center(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: Text(
                    'Nao foi possivel carregar o percurso da proposta.',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                ),
              );
            }

            final journey = snapshot.data!;
            return ListView(
              padding: const EdgeInsets.fromLTRB(18, 10, 18, 24),
              children: [
                Text(
                  journey.title,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    color: baseTheme.colorScheme.primary,
                    fontWeight: FontWeight.w800,
                  ),
                ),
                const SizedBox(height: 14),
                ...journey.phases.map(_JourneyPhaseTile.new),
              ],
            );
          },
        ),
      ),
    );
  }
}

class _JourneyPhaseTile extends StatelessWidget {
  const _JourneyPhaseTile(this.phase);

  final ProposalJourneyPhase phase;

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.black12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            phase.phaseName,
            style: TextStyle(
              color: baseTheme.colorScheme.primary,
              fontWeight: FontWeight.w800,
            ),
          ),
          if (phase.date != null) ...[
            const SizedBox(height: 4),
            Text(phase.date!, style: Theme.of(context).textTheme.bodySmall),
          ],
          const SizedBox(height: 8),
          Text(phase.summary),
        ],
      ),
    );
  }
}
