import 'package:rufino/modules/employee/domain/model/vote_id/number-vote_id.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';

class VoteId extends ModelBase {
  final NumberVoteId number;

  const VoteId(this.number, {super.isLoading, super.isLazyLoading});
  const VoteId.empty(
      {this.number = const NumberVoteId.empty(),
      super.isLoading,
      super.isLazyLoading});

  const VoteId.loading(
      {this.number = const NumberVoteId.empty(),
      super.isLoading = true,
      super.isLazyLoading});

  VoteId copyWith(
      {NumberVoteId? number,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    switch (generic.runtimeType) {
      case const (NumberVoteId):
        number = generic as NumberVoteId?;
    }
    return VoteId(
      number ?? this.number,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory VoteId.fromJson(Map<String, dynamic> json) {
    return VoteId(NumberVoteId.createFormatted(json["voteIdNumber"]));
  }
  Map<String, dynamic> toJson(String employeeId) {
    return {"employeeId": employeeId, "voteIdNumber": number.value};
  }

  @override
  List<Object?> get props => [number, isLoading, isLazyLoading];
}
