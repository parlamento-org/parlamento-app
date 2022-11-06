import 'package:flutter/material.dart';
import 'package:frontend/external_login/create_user.dart';
import 'package:frontend/external_login/facebook_sign_in.dart';
import 'package:frontend/external_login/google_sign_in.dart';
import 'package:frontend/login_widgets/initial_login_method_button.dart';
import 'package:frontend/login_widgets/login_form.dart';

import 'package:frontend/themes/base_theme.dart';
import 'package:flutter/foundation.dart' show kIsWeb;

class LoginMethodChoice extends StatelessWidget {
  /// The main title to show in the top left of the view

  const LoginMethodChoice({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Padding(
        padding: EdgeInsets.only(
            left: MediaQuery.of(context).size.width / 3,
            right: MediaQuery.of(context).size.width / 3),
        child: ListView(
          shrinkWrap: true,
          children: [
            Icon(
              Icons.account_balance_outlined,
              size: 150.0,
              color: baseTheme.colorScheme.primary,
            ),
            smallSeparator,
            Text(
              "Bem vinde!",
              style: mainTitleStyle,
              textAlign: TextAlign.center,
            ),
            smallSeparator,
            Text(
              "A nossa app é bue fixe venham usá-la e tals",
              style: secondaryTextStyle,
              textAlign: TextAlign.center,
            ),
            smallSeparator,
            LoginMethodButton(
              onPressed: () => {
                Navigator.of(context).push(MaterialPageRoute(
                    builder: (context) => const LoginPage(
                          title: "Bem vinde!",
                        )))
              },
              buttonText: "Entrar com e-mail",
              buttonIcon: Icons.alternate_email,
            ),
            smallSeparator,
            LoginMethodButton(
              onPressed: () => {
                if (kIsWeb)
                  {
                    signInWithGoogleWeb().then((credential) {
                      createUser(credential.user!);
                      Navigator.of(context).push(MaterialPageRoute(
                          builder: (context) => const Scaffold(
                                body: Text("Logged in with Google!"),
                              )));
                    })
                  }
                else
                  {
                    signInWithGoogle().then((credential) {
                      createUser(credential.user!);
                      Navigator.of(context).push(MaterialPageRoute(
                          builder: (context) => const Scaffold(
                                body: Text("Logged in with Google!"),
                              )));
                    })
                  }
              },
              buttonText: "Entrar com Google",
              buttonIcon: Icons.g_mobiledata,
            ),
            smallSeparator,
            LoginMethodButton(
              onPressed: () => {
                if (kIsWeb)
                  {
                    signInWithFacebookWeb().then((credential) {
                      createUser(credential.user!);
                      Navigator.of(context).push(MaterialPageRoute(
                          builder: (context) => const Scaffold(
                                body: Text("Logged in with Facebook!"),
                              )));
                    })
                  }
                else
                  {
                    signInWithFacebook().then((credential) {
                      createUser(credential.user!);
                      Navigator.of(context).push(MaterialPageRoute(
                          builder: (context) => const Scaffold(
                                body: Text("Logged in with Facebook!"),
                              )));
                    })
                  }
              },
              buttonText: "Entrar com Facebook",
              buttonIcon: Icons.facebook,
            )
          ],
        ));
  }
}
