part of "home_bloc.dart";

final class HomeState extends Equatable {
  final Company? company;
  final AplicationException? exception;

  const HomeState({this.company, this.exception});

  HomeState copyWith({Company? company, AplicationException? exception}) {
    return HomeState(
        company: company ?? this.company,
        exception: exception ?? this.exception);
  }

  @override
  List<Object?> get props => [company, exception];
}
