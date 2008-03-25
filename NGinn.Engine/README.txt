kickanie ¿ywego tokena
we: token w statusie 'ready'

1.	dla kazdego wychodzacego taska
		sprawdz czy moze odpalic
			jesli tak, to odpal
	jesli zaden task sie nie odpalil-przestaw token w 'czekanie na sasiadow'

wy: token w statusie
a) waiting jesli wystawiono choc jeden task
b) error jesli task dalby sie wystawic ale cos sie schrzanilo
c) waiting2 jesli nie mogl sie kicknac bo przejscie nie odpalilo

Do procesowania jednego tokena potrzebujemy znac stan wszystkich tokenow w procesie,
a przynajmniej tych ktore sa na wejsciu tranzycji wychodzacych z miejsca w ktorym jest token

2. DEFERRED CHOICE
transition (task) moze byc 
'disabled' - gdy nie moze odpalic
'enabled' - gdy moze odpalic
'error' - gdy nie uda sie go 'enablowac'

Generalnie, mozemy 'task' enablowaæ raz i dopiero jak odpali to mozna go enablowaæ kolejne razy. To opcja 'bezpieczna'.
Ale s¹ warunki w których mozemy task enablowaæ wiele razy jednoczesnie.
Kazda instancja 'enablowanego' taska bierze jeden token z place. Jesli ten task ma tylko jedno wejœcie, to wszystko w porzadku i tak
mozemy zrobic. 
Gorzej gdy ktorys task ma kilka wejsæ synchronizowanych. Wtedy proponujemy rozw
a) enablowanie taska tylko raz
b) podzia³ tokenow miedzy enablowane taski tak, aby nie bylo tokenow wspolnych. (ten na razie odrzucmy)

3. STATUSY TOKENA
- jesli dopiero wpadl w place, to READY
- jesli enablowa³ task, to WAITING_TASK 
- jesli nic nie moze enablowac (bo brak innych tokenów albo juz jest task enablowany przez inny token) to WAITING
