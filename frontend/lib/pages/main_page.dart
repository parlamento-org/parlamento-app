import 'package:flutter/material.dart';
import 'package:frontend/pages/vote_page.dart';
import 'package:frontend/themes/base_theme.dart';

import '../controllers/user_controller.dart';
import 'login.dart';

class MainPage extends StatefulWidget {
  const MainPage({super.key});

  @override
  State<StatefulWidget> createState() => MainPageState();
}

class MainPageState extends State<MainPage> {
  int _currentIndex = 0;
  final UserController _userController = UserController();
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
        backgroundColor: baseTheme.colorScheme.background,
        automaticallyImplyLeading: false,
        leading: IconButton(
          icon: Icon(
            Icons.logout,
            color: baseTheme.colorScheme.primary,
          ),
          onPressed: () {
            _userController.logout();
            Navigator.push(context,
                MaterialPageRoute(builder: (context) => const LoginPage()));
          },
        ),
        title: Center(
            child: Image.asset(
          'lib/images/logo_parlamento.png',
          height: 50,
          width: 50,
        )),
        actions: [
          IconButton(
            icon: Icon(Icons.settings, color: baseTheme.colorScheme.primary),
            onPressed: () {
              Navigator.pushNamedAndRemoveUntil(
                  context, '/settings', (route) => false);
            },
          ),
        ],
      ),
      body: PageView(
        controller: _pageController,
        onPageChanged: (value) => setState(() => _currentIndex = value),
        children: const [
          Center(child: Text('Partidos')),
          VotePage(),
          Center(child: Text('Profile')),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        backgroundColor: baseTheme.colorScheme.background,
        items: <BottomNavigationBarItem>[
          BottomNavigationBarItem(
            activeIcon: Icon(
              Icons.speaker,
              color: baseTheme.colorScheme.primary,
              weight: 5.0,
              size: 30.0,
            ),
            icon: Icon(Icons.speaker,
                color: baseTheme.colorScheme.primary.withOpacity(0.66)),
            label: 'Partidos',
            backgroundColor: baseTheme.colorScheme.primary.withOpacity(0.66),
          ),
          BottomNavigationBarItem(
            activeIcon: Icon(
              Icons.how_to_vote,
              color: baseTheme.colorScheme.primary,
              weight: 5.0,
              size: 30.0,
            ),
            icon: Icon(
              Icons.how_to_vote,
              color: baseTheme.colorScheme.primary.withOpacity(0.66),
            ),
            label: 'Vota',
            backgroundColor: baseTheme.colorScheme.primary.withOpacity(0.66),
          ),
          BottomNavigationBarItem(
            activeIcon: Icon(
              Icons.check_box,
              color: baseTheme.colorScheme.primary,
              weight: 5.0,
              size: 30.0,
            ),
            icon: Icon(Icons.check_box,
                color: baseTheme.colorScheme.primary.withOpacity(0.66)),
            label: 'Histórico',
            backgroundColor: baseTheme.colorScheme.primary.withOpacity(0.66),
          ),
        ],
        onTap: (index) {
          setState(() {
            _currentIndex = index;
            _pageController.animateToPage(index,
                duration: const Duration(milliseconds: 200),
                curve: Curves.easeIn);
          });
        },
        currentIndex: _currentIndex,
        selectedItemColor: baseTheme.colorScheme.primary,
      ),
    );
  }
}
