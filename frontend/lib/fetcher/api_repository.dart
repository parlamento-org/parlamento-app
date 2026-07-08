import 'package:flutter_dotenv/flutter_dotenv.dart';
import 'package:frontend/exceptions/email_has_account.dart';
import 'package:frontend/exceptions/invalid_credentials.dart';
import 'package:frontend/exceptions/username_already_exists.dart';
import 'package:frontend/fetcher/api_client.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/proposal_criteria.dart';
import 'package:frontend/models/proposal_flow.dart';
import 'package:frontend/models/profile.dart';
import 'package:frontend/models/user.dart';
import 'package:frontend/models/vote_model.dart';
import 'package:http/http.dart' as http;

import '../models/proposal.dart';

class APIRepository implements Repository {
  APIRepository({ApiClient? apiClient})
    : _apiClient =
          apiClient ??
          ApiClient(
            baseUrl: Uri.parse(dotenv.env['BACKEND_URL']!),
            httpClient: http.Client(),
          );

  final ApiClient _apiClient;

  @override
  Future<InitiativeFeedCard> getInitiativeFeedCard(
    ProposalFlowFeedRequest request,
  ) async {
    final response = await _apiClient.postJson(
      '/proposal-flow/feed',
      request.toJson(),
    );

    if (response.statusCode == 200) {
      return InitiativeFeedCard.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to load initiative feed card',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<ProposalInteractionResult> recordProposalInteraction(
    ProposalInteractionSubmission submission,
  ) async {
    final response = await _apiClient.postJson(
      '/proposal-flow/interactions',
      submission.toJson(),
    );

    if (response.statusCode == 200) {
      return ProposalInteractionResult.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to record proposal interaction',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<ProposalReveal> getProposalReveal({required int initiativeId}) async {
    final response = await _apiClient.getJson(
      '/proposal-flow/initiatives/$initiativeId/reveal',
    );

    if (response.statusCode == 200) {
      return ProposalReveal.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to load proposal reveal',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<ProposalJourney> getProposalJourney(int initiativeId) async {
    final response = await _apiClient.getJson(
      '/proposal-flow/initiatives/$initiativeId/journey',
    );

    if (response.statusCode == 200) {
      return ProposalJourney.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to load proposal journey',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<ProposalHistoryPage> getProposalHistory(
    ProposalHistoryRequest request,
  ) async {
    final path =
        Uri(
          path: '/proposal-flow/history',
          queryParameters: request.toQueryParameters(),
        ).toString();
    final response = await _apiClient.getJson(path);

    if (response.statusCode == 200) {
      return ProposalHistoryPage.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to load proposal history',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<ProfileStats> getProfileStats() async {
    final response = await _apiClient.getJson('/profile');

    if (response.statusCode == 200) {
      return ProfileStats.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to load profile statistics',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<Proposal> getProposal(ProposalCriteria criteria) async {
    final response = await _apiClient.putJson('/vote', criteria.toJson());

    if (response.statusCode == 200) {
      return Proposal.fromJson(response.jsonObject());
    }

    throw ApiException(
      'Failed to load proposal',
      statusCode: response.statusCode,
    );
  }

  @override
  Future<UserSession> facebookSignInRequest(
    String accessToken,
    String email,
    String name,
    int profilePicId,
  ) async {
    final Map<String, dynamic> data = {
      "facebookAccessToken": accessToken,
      "email": email,
      "userName": name,
      "profilePic": profilePicId,
    };

    final response = await _apiClient.postJson('/fb-login', data);

    if (response.statusCode == 200) {
      return UserSession.fromJson(
        response.jsonObject(),
        userType: UserType.facebook,
      );
    }

    throw InvalidCredentials();
  }

  @override
  Future<UserSession> googleSignInRequest(
    String idToken,
    String email,
    String name,
    int profilePicId,
  ) async {
    final Map<String, dynamic> data = {
      "googleIDToken": idToken,
      "email": email,
      "userName": name,
      "profilePic": profilePicId,
    };

    final response = await _apiClient.postJson('/google-login', data);

    if (response.statusCode == 200) {
      return UserSession.fromJson(
        response.jsonObject(),
        userType: UserType.google,
      );
    }

    throw InvalidCredentials();
  }

  @override
  Future<UserSession> loginRequest(String email, String password) async {
    var identifier = 'userName';
    if (email.contains('@')) {
      identifier = 'email';
    }

    final Map<String, dynamic> data = {identifier: email, "password": password};

    final response = await _apiClient.postJson('/user-login', data);

    if (response.statusCode == 200) {
      return UserSession.fromJson(response.jsonObject());
    }

    throw InvalidCredentials();
  }

  @override
  Future<bool> registerRequest(
    String email,
    String userName,
    String password,
    int profilePicId,
  ) async {
    final Map<String, dynamic> data = {
      "email": email,
      "userName": userName,
      "password": password,
      "profilePic": profilePicId,
    };

    final response = await _apiClient.postJson('/user', data);

    if (response.statusCode == 200) {
      return true;
    } else if (response.statusCode == 401) {
      throw EmailHasAccount();
    } else if (response.statusCode == 402) {
      throw UsernameAlreadyExists();
    }

    return false;
  }

  @override
  Future<void> castUserVote(UserVote userVote) async {
    final response = await _apiClient.postJson('/vote', userVote.toJson());

    if (response.statusCode == 200) {
      return;
    }

    throw ApiException('Failed to cast vote', statusCode: response.statusCode);
  }
}
