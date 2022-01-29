import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/ppe_manager/bloc/ppe_manager_bloc.dart';
import 'package:rufino_smart_app/ppe_manager/page/components/tile_component.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class PpeManagerHomePage extends StatelessWidget {
  final ScrollController _scrollcontroller = ScrollController();
  List myList = [];
  PpeManagerHomePage({Key? key}) : super(key: key) {
    Modular.get<PpeManagerBloc>().add(PpeManagerLoadDataEvent(10));
    myList = List.generate(10, (index) => "Item: ${index + 1}");
    _scrollcontroller.addListener(() {
      if (_scrollcontroller.position.pixels ==
          _scrollcontroller.position.maxScrollExtent) {
        _getMoreData();
      }
    });
  }

  _getMoreData() {
    Modular.get<PpeManagerBloc>().add(PpeManagerLoadDataEvent(10));
    print("more data");
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: kBackGroundColor,
      appBar: AppBar(
        actions: [
          IconButton(onPressed: () {}, icon: const Icon(Icons.search)),
        ],
        backgroundColor: kPrimaryColor,
        title: const Text("EPI's"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/home'),
        ),
      ),
      body: Center(
        child: SizedBox(
          width: 700,
          child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: BlocBuilder<PpeManagerBloc, PpeManagerState>(
                bloc: Modular.get<PpeManagerBloc>(),
                builder: (context, state) {
                  if (state is PpeManagerInitial) {
                    return const Text("Load Data");
                  }
                  if (state is PpeManagerHasData) {
                    return ListView.builder(
                        controller: _scrollcontroller,
                        itemCount: myList.length + 1,
                        itemBuilder: (context, index) {
                          if (index == state.workers.length) {
                            return const CircularProgressIndicator();
                          } else {
                            var worker = state.workers[index];
                            return TileComponent(
                              name: worker.name,
                              days: worker.getValidity(),
                              onTap: () {
                                Modular.to.navigate(
                                  "/ppemanageremployee/${worker.id}",
                                );
                              },
                            );
                          }
                        });
                  }
                  return Container();
                },
              )),
        ),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {},
        backgroundColor: kPrimaryColor,
        child: Icon(Icons.download),
      ),
    );
  }
}
