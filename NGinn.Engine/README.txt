TODO
* Parametry zada� - mo�liwo�� bindowania do danych z procesu lub wpisywania wyra�e� w script.net
* bindowanie parametr�w do task�w
  - statyczne (wpisanie warto�ci w xml-u)
  - dynamiczne (warto�� jakiego� wyra�enia).
    - proponujemy ograniczenie dost�pno�ci danych w bindowaniu dynamicznym tylko do danych tego zadania.
      dzi�ki temu nie b�dzie 'niejawnych' zale�no�ci. 
      W tym celu dajemy 'parameter bindings' kt�re binduj� do konkretnych danych na wej�ciu oraz
      na wyj�ciu.
      Konsekwencja: konfiguracja zada� mo�e odby� si� dopiero po wstawieniu danych do zadania !!!
    - alternatywa: dajemy mo�liwo�� bindowania do 'sztucznych' zmiennych, np _task.DelayAmount,
      kt�re reprezentuj� parametry zadania. Jednak to 'zmienia' nam schemat danych.
    
* Transakcje rozproszone (transakcyjne zachowanie ca�o�ci)
* Task shell + multi-instance task shell + implementacja tasku
* Cache stanu proces�w + reader/writer lock na instancji procesu (w celu utrzymywania cache)
* Message-boxy na eventy na kt�re czekaj� taski (jeden task wysy�a request, inny czeka na event z odpowiedzi�)
* Zmiana sposobu zapisu schematu procesu... Czy potrzebujemy osobnych klas do definicji
  r�nych typ�w zada�? Na etapie definicji zadania r�ni� si� tylko zestawem parametr�w oraz
  walidacj�. Cz�� z tych rzeczy mo�na zrobi� w jednej klasie. Reszt� walidacji - w klasie 'task active'
  Pytanie co z 'custom taskami' - jak tam da� mo�liwo�� walidacji?
  S� jeszcze parametry 'custom' czyli takie co nie daj� si� �atwo zapisa� w XML. Wtedy przyda�a by si�
  jaka� klasa 'custom' do tego samego celu.
* OR-join - brakuje sprawdzenia czy warunek OR-joina b�dzie spe�niony po wykonaniu 
  przej�cia z cancel-setem (usuni�cie tokena mo�e spowodowa� �e warunek b�dzie spe�niony).
  To samo przy OR-joinie z tokenem kt�ry mo�e 'wyj��' poza sie� tego OR-joina (np z powodu
  wej�cia w inn� alternatyw�). A co z tokenem kt�ry 'wszed�' z boku - takich nie powinno by�...
* Doko�czy� mechanizm retry dla procesu
  a mo�e tak: retry zrobi� z MessageQueue (i w osobnym w�tku robi� ponowienie pr�by...)
  tylko �e wtedy error status trzeba by jako� dobrze obs�u�y�... nie, sam si� obs�u�y
  
  
  
co dalej
- zadania... niech one b�d� troch� bardziej autonomiczne

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

czy task moze si� sam 'cancelowa�'? na razie nie
sk�d task ma wiedzie� czy si� zako�czy�
ma dosta� event. Event zostanie dostarczony przez PI



Definicja procesu w Boo?

process "TestProcess", 1:
	start_place "start"
	end_place "end"
	place "p1"
	place "p2"
	
	task "t1", EmptyTask:
		split_type XOR
		label "My task"
		variables:
			variable "V1"
		input_bindings:
			binding_for "V1":
				ProcessData.Variable1 + ProcessData.V2 + 10
		output_bindings:
			binding_for "PV1":
				Data.Out1 + Data.Out2
			copy_var "V2", "PV2"
	
	flow "start", "t1"
	flow "t1", "p1":
		label "V1 > 30"
		condition Data.V1 > 30
		
	flow "t1", "p2":
		label "V1 > 30"
		condition Data.V1 <= 30
	
	variables:
		variable "V1", string, Dir.In, required:true
