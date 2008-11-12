TODO
* Parametry zadañ - mo¿liwoœæ bindowania do danych z procesu lub wpisywania wyra¿eñ w script.net
* bindowanie parametrów do tasków
  - statyczne (wpisanie wartoœci w xml-u)
  - dynamiczne (wartoœæ jakiegoœ wyra¿enia).
    - proponujemy ograniczenie dostêpnoœci danych w bindowaniu dynamicznym tylko do danych tego zadania.
      dziêki temu nie bêdzie 'niejawnych' zale¿noœci. 
      W tym celu dajemy 'parameter bindings' które binduj¹ do konkretnych danych na wejœciu oraz
      na wyjœciu.
      Konsekwencja: konfiguracja zadañ mo¿e odbyæ siê dopiero po wstawieniu danych do zadania !!!
    - alternatywa: dajemy mo¿liwoœæ bindowania do 'sztucznych' zmiennych, np _task.DelayAmount,
      które reprezentuj¹ parametry zadania. Jednak to 'zmienia' nam schemat danych.
    
* Transakcje rozproszone (transakcyjne zachowanie ca³oœci)
* Task shell + multi-instance task shell + implementacja tasku
* Cache stanu procesów + reader/writer lock na instancji procesu (w celu utrzymywania cache)
* Message-boxy na eventy na które czekaj¹ taski (jeden task wysy³a request, inny czeka na event z odpowiedzi¹)
* Zmiana sposobu zapisu schematu procesu... Czy potrzebujemy osobnych klas do definicji
  ró¿nych typów zadañ? Na etapie definicji zadania ró¿ni¹ siê tylko zestawem parametrów oraz
  walidacj¹. Czêœæ z tych rzeczy mo¿na zrobiæ w jednej klasie. Resztê walidacji - w klasie 'task active'
  Pytanie co z 'custom taskami' - jak tam daæ mo¿liwoœæ walidacji?
  S¹ jeszcze parametry 'custom' czyli takie co nie daj¹ siê ³atwo zapisaæ w XML. Wtedy przyda³a by siê
  jakaœ klasa 'custom' do tego samego celu.
* OR-join - brakuje sprawdzenia czy warunek OR-joina bêdzie spe³niony po wykonaniu 
  przejœcia z cancel-setem (usuniêcie tokena mo¿e spowodowaæ ¿e warunek bêdzie spe³niony).
  To samo przy OR-joinie z tokenem który mo¿e 'wyjœæ' poza sieæ tego OR-joina (np z powodu
  wejœcia w inn¹ alternatywê). A co z tokenem który 'wszed³' z boku - takich nie powinno byæ...
* Dokoñczyæ mechanizm retry dla procesu
  a mo¿e tak: retry zrobiæ z MessageQueue (i w osobnym w¹tku robiæ ponowienie próby...)
  tylko ¿e wtedy error status trzeba by jakoœ dobrze obs³u¿yæ... nie, sam siê obs³u¿y
  
  
  
co dalej
- zadania... niech one bêd¹ trochê bardziej autonomiczne

ProcessInstance
- enableTask
- cancelTask
Task callback
- taskEnabled
- taskCompleted
- taskCancelled

PI
--> enableTask
<-- taskStarted (opcjonalne) 
<-- taskCompleted (wymagane)
.

--> cancelTask
.

<-- taskCancelled (na razie nie implementujemy)

czy task moze siê sam 'cancelowaæ'? na razie nie
sk¹d task ma wiedzieæ czy siê zakoñczy³
ma dostaæ event. Event zostanie dostarczony przez PI
