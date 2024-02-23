import 'package:frontend/fetcher/api_repository.dart';
import 'package:frontend/fetcher/repository.dart';
import 'package:frontend/models/political_party.dart';

class PartyController {
  final Repository _repository = APIRepository();

  Future<List<PoliticalParty>> getPoliticalParties() async {
    return await _repository.getAllPoliticalParties();
  }
}
