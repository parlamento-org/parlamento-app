import 'package:flutter/material.dart';
import 'package:frontend/controllers/auth_controller.dart';
import 'package:frontend/pages/profile_page.dart';
import 'package:frontend/pages/previous_votes_history_page.dart';
import 'package:frontend/pages/voting_section.dart';
import 'package:frontend/themes/base_theme.dart';
import 'package:provider/provider.dart';

class MainPage extends StatefulWidget {
  const MainPage({super.key});

  @override
  State<StatefulWidget> createState() => MainPageState();
}

class MainPageState extends State<MainPage> {
  int _currentIndex = 0;
  late final PageController _pageController;

  @override
  void initState() {
    super.initState();
    _pageController = PageController(initialPage: _currentIndex);
  }

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    //create a page with a bottom bar with three options: home, search and profile
    return Scaffold(
      appBar: AppBar(
        backgroundColor: baseTheme.colorScheme.surface,
        automaticallyImplyLeading: false,
        leading: IconButton(
          icon: Icon(Icons.logout, color: baseTheme.colorScheme.primary),
          onPressed: () async {
            await context.read<AuthController>().logout();
          },
        ),
        title: Center(
          child: Image.asset('lib/images/logo.png', height: 50, width: 50),
        ),
        actions: [
          IconButton(
            icon: Icon(Icons.settings, color: baseTheme.colorScheme.primary),
            onPressed: () {
              Navigator.pushNamedAndRemoveUntil(
                context,
                '/settings',
                (route) => false,
              );
            },
          ),
        ],
      ),
      body: PageView(
        controller: _pageController,
        onPageChanged: (value) => setState(() => _currentIndex = value),
        children: const [
          ProfilePage(),
          VotingSection(),
          PreviousVotesHistoryPage(),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        backgroundColor: baseTheme.colorScheme.surface,
        type: BottomNavigationBarType.fixed,
        items: <BottomNavigationBarItem>[
          BottomNavigationBarItem(
            activeIcon: const _NavIcon(icon: Icons.speaker, selected: true),
            icon: const _NavIcon(icon: Icons.speaker),
            label: 'Perfil',
            backgroundColor: baseTheme.colorScheme.primary.withValues(
              alpha: fadedPrimaryOpacity,
            ),
          ),
          BottomNavigationBarItem(
            activeIcon: const _NavIcon(icon: Icons.how_to_vote, selected: true),
            icon: const _NavIcon(icon: Icons.how_to_vote),
            label: 'Vota',
            backgroundColor: baseTheme.colorScheme.primary.withValues(
              alpha: fadedPrimaryOpacity,
            ),
          ),
          BottomNavigationBarItem(
            activeIcon: const _NavIcon(icon: Icons.check_box, selected: true),
            icon: const _NavIcon(icon: Icons.check_box),
            label: 'Histórico',
            backgroundColor: baseTheme.colorScheme.primary.withValues(
              alpha: fadedPrimaryOpacity,
            ),
          ),
        ],
        onTap: (index) {
          setState(() {
            _currentIndex = index;
            _pageController.animateToPage(
              index,
              duration: const Duration(milliseconds: 200),
              curve: Curves.easeIn,
            );
          });
        },
        currentIndex: _currentIndex,
        selectedItemColor: baseTheme.colorScheme.primary,
        unselectedItemColor: baseTheme.colorScheme.primary.withValues(
          alpha: fadedPrimaryOpacity,
        ),
        selectedLabelStyle: const TextStyle(fontWeight: FontWeight.w900),
        unselectedLabelStyle: const TextStyle(fontWeight: FontWeight.w700),
      ),
    );
  }
}

class _NavIcon extends StatelessWidget {
  const _NavIcon({required this.icon, this.selected = false});

  final IconData icon;
  final bool selected;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 44,
      height: 34,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: selected ? baseTheme.colorScheme.primary : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
      ),
      child: Icon(
        icon,
        color:
            selected
                ? Colors.white
                : baseTheme.colorScheme.primary.withValues(
                  alpha: fadedPrimaryOpacity,
                ),
        size: selected ? 25 : 24,
      ),
    );
  }
}
