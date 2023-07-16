import 'package:flutter/material.dart';
import 'package:frontend/components/my_button.dart';
import 'package:frontend/constants/user_session.dart';
import 'package:frontend/controllers/vote_controller.dart';
import 'package:frontend/models/proposal.dart';
import 'package:flip_card/flip_card.dart';
import 'package:frontend/models/vote_model.dart';
import 'package:frontend/themes/base_theme.dart';

class VotePage extends StatefulWidget {
  const VotePage({super.key});

  @override
  State<VotePage> createState() => _VotePageState();
}

class _VotePageState extends State<VotePage> {
  VoteController voteController = VoteController();
  Proposal? proposal;
  bool _isLoading = true;

  void castUserVote(VoteOrientation votingOrientation) async {
    try {
      setState(() {
        _isLoading = true;
      });
      UserVote userVote = UserVote(
          userID: globalUserSession.userId,
          proposalID: proposal!.id,
          voteOrientation: votingOrientation);
      await voteController.castUserVote(userVote);

      getRandomProposal();
    } catch (e) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Error casting your vote'),
        ),
      );
      setState(() {
        _isLoading = false;
      });
    }
  }

  void getRandomProposal() async {
    try {
      Proposal? proposal = await voteController
          .getRandomProposal(globalUserSession.proposalCriteria!);
      setState(() {
        _isLoading = false;
        this.proposal = proposal;
      });
    } catch (e) {
      print(e);
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
            content: Text(
                'Error getting proposal, you may be offline or they might be a problem with your connection!'),
            duration: Duration(seconds: 2)),
      );
    }
  }

  @override
  initState() {
    super.initState();
    getRandomProposal();
  }

  @override
  Widget build(BuildContext context) {
    //return a box with a border with three buttons at the bottom
    return Container(
      padding: const EdgeInsets.all(16.0),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Expanded(
            child: Container(
              decoration: BoxDecoration(
                border: Border.all(
                  color: Colors.black,
                  width: 2.0,
                ),
              ),
              child: Center(
                child: _isLoading
                    ? const CircularProgressIndicator()
                    : FlipCard(
                        direction: FlipDirection.HORIZONTAL,
                        front: Center(
                          child: Text(
                            proposal!.title,
                            style: const TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.bold,
                                fontFamily: 'Inter'),
                          ),
                        ),
                        back: Center(
                            child: SingleChildScrollView(
                          child: Text(
                            proposal!.censoredText,
                            style: const TextStyle(
                                fontSize: 20,
                                fontWeight: FontWeight.bold,
                                fontFamily: 'Inter'),
                          ),
                        )),
                      ),
              ),
            ),
          ),
          Wrap(
            children: [
              TextButton(
                style: buttonStyle,
                onPressed: () {
                  castUserVote(VoteOrientation.InFavor);
                },
                child: Image.asset('lib/images/voto_favor.png',
                    width: 50, height: 50),
              ),
              TextButton(
                style: buttonStyle,
                onPressed: () {
                  castUserVote(VoteOrientation.Against);
                },
                child: Image.asset('lib/images/voto_contra.png',
                    width: 50, height: 50),
              ),
              TextButton(
                style: buttonStyle,
                onPressed: () {
                  castUserVote(VoteOrientation.Abstaining);
                },
                child: Image.asset('lib/images/voto_abster.png',
                    width: 50, height: 50),
              ),
            ],
          ),
          const SizedBox(height: 10),
          MyButton(
              onTap: () => castUserVote(VoteOrientation.NotInterested),
              text: "Não interessado")
        ],
      ),
    );
  }
}
