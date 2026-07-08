import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/profile.dart';

class ProfileController {
  ProfileController({Repository? repository})
    : _repository = repository ?? APIRepository();

  final Repository _repository;

  Future<ProfileStats> getProfileStats() {
    return _repository.getProfileStats();
  }
}
