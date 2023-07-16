import 'package:frontend/models/user.dart';

UserSession globalUserSession = UserSession(
    userId: 7,
    name: 'João',
    email: 'rr',
    password: 'rr',
    profilePictureId: 1,
    partyStats: [],
    userType: UserType.email,
    userVotes: List.empty());
