part of "home_bloc.dart";

final class HomeState extends Equatable {
  final Company? company;

  const HomeState({this.company});

  HomeState copyWith({Company? company}) {
    return HomeState(company: company ?? this.company);
  }

  @override
  List<Object> get props => [];
}
